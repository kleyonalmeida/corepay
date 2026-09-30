using Core.Domain;
using Core.Domain.PayrollCalculation;
using FluentAssertions;

namespace Core.Tests.PayrollCalculation;

public class RoleChangePeriodSplitterTests
{
    private const int March = 3;
    private const int Year = 2025;

    [Fact]
    public void Build_ShouldSplitIntoFifteenAndSixteenDays_WhenChangeIsMidMonth()
    {
        var input = CreateInput(
            admissionDate: new DateOnly(Year, March, 1),
            roleChanges:
            [
                new RoleChangeEntryInput(
                    new DateOnly(Year, March, 16),
                    CreateRoleSnapshot(baseSalary: 2000m))
            ]);

        var result = RoleChangePeriodSplitter.Build(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value[0].StartInclusive.Should().Be(new DateOnly(Year, March, 1));
        result.Value[0].EndInclusive.Should().Be(new DateOnly(Year, March, 15));
        result.Value[1].StartInclusive.Should().Be(new DateOnly(Year, March, 16));
        result.Value[1].EndInclusive.Should().Be(new DateOnly(Year, March, 31));
    }

    [Fact]
    public void Build_ShouldCreateThreePeriods_WhenTwoChangesInMonth()
    {
        var input = CreateInput(
            roleChanges:
            [
                new RoleChangeEntryInput(
                    new DateOnly(Year, March, 10),
                    CreateRoleSnapshot(baseSalary: 1000m)),
                new RoleChangeEntryInput(
                    new DateOnly(Year, March, 20),
                    CreateRoleSnapshot(baseSalary: 2000m))
            ]);

        var result = RoleChangePeriodSplitter.Build(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
        result.Value[0].EndInclusive.Should().Be(new DateOnly(Year, March, 9));
        result.Value[1].StartInclusive.Should().Be(new DateOnly(Year, March, 10));
        result.Value[1].EndInclusive.Should().Be(new DateOnly(Year, March, 19));
        result.Value[2].StartInclusive.Should().Be(new DateOnly(Year, March, 20));
        result.Value[2].EndInclusive.Should().Be(new DateOnly(Year, March, 31));
    }

    [Fact]
    public void Build_ShouldSortUnorderedChanges_ByChangeDate()
    {
        var input = CreateInput(
            roleChanges:
            [
                new RoleChangeEntryInput(
                    new DateOnly(Year, March, 20),
                    CreateRoleSnapshot(baseSalary: 2000m)),
                new RoleChangeEntryInput(
                    new DateOnly(Year, March, 10),
                    CreateRoleSnapshot(baseSalary: 1000m))
            ]);

        var result = RoleChangePeriodSplitter.Build(input);

        result.IsSuccess.Should().BeTrue();
        result.Value[1].StartInclusive.Should().Be(new DateOnly(Year, March, 10));
        result.Value[2].StartInclusive.Should().Be(new DateOnly(Year, March, 20));
    }

    [Fact]
    public void Build_ShouldEndLastPeriodOnDismissal_WhenDismissalIsInMonth()
    {
        var input = CreateInput(
            dismissalDate: new DateOnly(Year, March, 25),
            roleChanges:
            [
                new RoleChangeEntryInput(
                    new DateOnly(Year, March, 16),
                    CreateRoleSnapshot(baseSalary: 2000m))
            ]);

        var result = RoleChangePeriodSplitter.Build(input);

        result.IsSuccess.Should().BeTrue();
        result.Value[^1].EndInclusive.Should().Be(new DateOnly(Year, March, 25));
    }

    [Fact]
    public void Build_ShouldSkipMainPeriod_WhenChangeIsFirstDayOfMonth()
    {
        var input = CreateInput(
            roleChanges:
            [
                new RoleChangeEntryInput(
                    new DateOnly(Year, March, 1),
                    CreateRoleSnapshot(baseSalary: 2000m))
            ]);

        var result = RoleChangePeriodSplitter.Build(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        result.Value[0].StartInclusive.Should().Be(new DateOnly(Year, March, 1));
    }

    [Fact]
    public void Build_ShouldFail_WhenDuplicateChangeDates()
    {
        var input = CreateInput(
            roleChanges:
            [
                new RoleChangeEntryInput(
                    new DateOnly(Year, March, 16),
                    CreateRoleSnapshot(baseSalary: 1000m)),
                new RoleChangeEntryInput(
                    new DateOnly(Year, March, 16),
                    CreateRoleSnapshot(baseSalary: 2000m))
            ]);

        var result = RoleChangePeriodSplitter.Build(input);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("payroll.role_change_duplicate_date");
    }

    [Fact]
    public void ToPeriodInput_ShouldApplyClipAndClearManualAdjustments()
    {
        var source = CreateInput(
            roleChanges:
            [
                new RoleChangeEntryInput(
                    new DateOnly(Year, March, 16),
                    CreateRoleSnapshot(baseSalary: 2000m))
            ],
            bonuses: [new BonusEntryInput(Guid.NewGuid(), 50m)],
            deductions: [new DeductionEntryInput(100m)]);

        var periods = RoleChangePeriodSplitter.Build(source).Value;
        var periodInput = RoleChangePeriodSplitter.ToPeriodInput(source, periods[0]);

        periodInput.PeriodClipStart.Should().Be(new DateOnly(Year, March, 1));
        periodInput.PeriodClipEnd.Should().Be(new DateOnly(Year, March, 15));
        periodInput.RoleChanges.Should().BeEmpty();
        periodInput.BonusEntries.Should().BeEmpty();
        periodInput.DeductionEntries.Should().BeEmpty();
    }

    private static PayrollEntryInput CreateInput(
        DateOnly? admissionDate = null,
        DateOnly? dismissalDate = null,
        IReadOnlyList<RoleChangeEntryInput>? roleChanges = null,
        IReadOnlyList<BonusEntryInput>? bonuses = null,
        IReadOnlyList<DeductionEntryInput>? deductions = null)
    {
        var department = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Setor A",
            CalculationType = CalculationProfile.FixedBonus
        };

        var level = new CareerLevel
        {
            Id = Guid.NewGuid(),
            Profile = CalculationProfile.FixedBonus,
            BaseSalary = 3100m
        };

        var collaborator = new Collaborator
        {
            Id = Guid.NewGuid(),
            Name = "Colaborador",
            DepartmentId = department.Id,
            AdmissionDate = admissionDate,
            DismissalDate = dismissalDate
        };

        return new PayrollEntryInput
        {
            Month = March,
            Year = Year,
            Department = department,
            CareerLevel = level,
            Collaborator = collaborator,
            FullBaseSalary = 3100m,
            RoleChanges = roleChanges ?? [],
            BonusEntries = bonuses ?? [],
            DeductionEntries = deductions ?? []
        };
    }

    private static PayrollRoleSnapshot CreateRoleSnapshot(decimal baseSalary) =>
        new()
        {
            Department = new Department
            {
                Id = Guid.NewGuid(),
                Name = "Setor B",
                CalculationType = CalculationProfile.FixedBonus
            },
            CareerLevel = new CareerLevel
            {
                Id = Guid.NewGuid(),
                Profile = CalculationProfile.FixedBonus,
                BaseSalary = baseSalary
            },
            FullBaseSalary = baseSalary
        };
}
