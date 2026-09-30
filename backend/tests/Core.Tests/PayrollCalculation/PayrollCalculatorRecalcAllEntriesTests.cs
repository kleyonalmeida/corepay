using Core.Domain;
using Core.Domain.PayrollCalculation;
using FluentAssertions;

namespace Core.Tests.PayrollCalculation;

public class PayrollCalculatorRecalcAllEntriesTests
{
    private readonly PayrollCalculator _calculator = new();

    [Fact]
    public void RecalcAllEntries_ShouldSumMixedProfiles_InOrder()
    {
        var commercial = CreateCommercialAnalystInput();
        var fixedBonus = CreateFixedBonusInput();
        var traffic = CreatePaidTrafficInput();

        var commercialSingle = _calculator.CalcEntry(commercial).Value;
        var fixedSingle = _calculator.CalcEntry(fixedBonus).Value;
        var trafficSingle = _calculator.CalcEntry(traffic).Value;

        var expectedTotal = commercialSingle.TotalAmount
            + fixedSingle.TotalAmount
            + trafficSingle.TotalAmount;

        var result = _calculator.RecalcAllEntries([commercial, fixedBonus, traffic]);

        result.IsSuccess.Should().BeTrue();
        result.Value.EntryResults.Should().HaveCount(3);
        result.Value.EntryResults[0].Should().BeEquivalentTo(commercialSingle);
        result.Value.EntryResults[1].Should().BeEquivalentTo(fixedSingle);
        result.Value.EntryResults[2].Should().BeEquivalentTo(trafficSingle);
        result.Value.TotalAmount.Should().Be(expectedTotal);
    }

    [Fact]
    public void RecalcAllEntries_ShouldReturnZero_WhenListIsEmpty()
    {
        var result = _calculator.RecalcAllEntries([]);

        result.IsSuccess.Should().BeTrue();
        result.Value.EntryResults.Should().BeEmpty();
        result.Value.TotalAmount.Should().Be(0m);
    }

    [Fact]
    public void RecalcAllEntries_ShouldIncludeZeroEntry_WhenDepartmentIsMissing()
    {
        var input = new PayrollEntryInput
        {
            Month = 3,
            Year = 2025,
            Department = null,
            Collaborator = CreateCollaborator()
        };

        var result = _calculator.RecalcAllEntries([input]);

        result.IsSuccess.Should().BeTrue();
        result.Value.EntryResults.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(PayrollEntryResult.Zero);
        result.Value.TotalAmount.Should().Be(0m);
    }

    [Fact]
    public void RecalcAllEntries_ShouldFailFast_WhenSecondEntryFails()
    {
        var valid = CreateFixedBonusInput();
        var invalidTraffic = CreatePaidTrafficInputWithManualRateio();

        var result = _calculator.RecalcAllEntries([valid, invalidTraffic]);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("traffic.manual_rateio_not_allowed");
    }

    [Fact]
    public void RecalcAllEntries_ShouldThrow_WhenEntriesIsNull()
    {
        var act = () => _calculator.RecalcAllEntries(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    private static PayrollEntryInput CreateCommercialAnalystInput() =>
        new()
        {
            Month = 3,
            Year = 2025,
            Department = new Department
            {
                Id = Guid.NewGuid(),
                Name = "Comercial",
                CalculationType = CalculationProfile.CommercialAnalyst
            },
            CareerLevel = new CareerLevel
            {
                Id = Guid.NewGuid(),
                Profile = CalculationProfile.CommercialAnalyst,
                BaseSalary = 1500m,
                FtdRateBase = 2m,
                FtdSuperbetRate = 5m,
                SalesPctBase = 4m
            },
            Collaborator = CreateCollaborator(),
            FullBaseSalary = 1500m,
            CommercialProjectEntries =
            [
                new CommercialAnalystProjectEntryInput(
                    Guid.NewGuid(),
                    ProjectPlatform.Lastlink,
                    FtdTotal: 100,
                    FtdSuperbet: 10,
                    false, false, 0,
                    SalesAmount: 10_000m,
                    false, false,
                    Rev: 0m)
            ]
        };

    private static PayrollEntryInput CreateFixedBonusInput() =>
        new()
        {
            Month = 3,
            Year = 2025,
            Department = new Department
            {
                Id = Guid.NewGuid(),
                Name = "Administrativo",
                CalculationType = CalculationProfile.FixedBonus,
                GoalBonusPercentage = 10m
            },
            CareerLevel = new CareerLevel
            {
                Id = Guid.NewGuid(),
                Profile = CalculationProfile.FixedBonus,
                BaseSalary = 3000m
            },
            Collaborator = CreateCollaborator(),
            FullBaseSalary = 3000m,
            GoalTier = GoalTier.Goal,
            ProjectEntries = [new ProjectEntryInput(Guid.NewGuid(), 0m)]
        };

    private static PayrollEntryInput CreatePaidTrafficInput() =>
        new()
        {
            Month = 3,
            Year = 2025,
            Department = new Department
            {
                Id = Guid.NewGuid(),
                Name = "Tráfego Pago",
                CalculationType = CalculationProfile.PaidTraffic
            },
            CareerLevel = new CareerLevel
            {
                Id = Guid.NewGuid(),
                Profile = CalculationProfile.PaidTraffic,
                BaseSalary = 1000m,
                TrafficInvestmentCommissionPct = 2m,
                TrafficCpaBetano = 50m
            },
            Collaborator = CreateCollaborator(),
            FullBaseSalary = 1000m,
            TrafficProjectEntries =
            [
                new TrafficProjectEntryInput(
                    Guid.NewGuid(),
                    10_000m,
                    [new TrafficCpaEntryInput(TrafficHouse.Betano, TrafficCpaKind.Supervised, 3)])
            ]
        };

    private static PayrollEntryInput CreatePaidTrafficInputWithManualRateio() =>
        new()
        {
            Month = 3,
            Year = 2025,
            Department = new Department
            {
                Id = Guid.NewGuid(),
                CalculationType = CalculationProfile.PaidTraffic
            },
            CareerLevel = new CareerLevel
            {
                Id = Guid.NewGuid(),
                Profile = CalculationProfile.PaidTraffic,
                BaseSalary = 1000m,
                TrafficInvestmentCommissionPct = 2m,
                TrafficCpaBetano = 50m
            },
            Collaborator = CreateCollaborator(),
            FullBaseSalary = 1000m,
            RateioProjectEntries = [new RateioProjectEntryInput(Guid.NewGuid(), RateioValue: 500m)]
        };

    private static Collaborator CreateCollaborator() =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = "Colaborador Teste",
            DepartmentId = Guid.NewGuid()
        };
}
