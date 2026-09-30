using Core.Domain;
using Core.Domain.PayrollCalculation;
using FluentAssertions;

namespace Core.Tests.PayrollCalculation;

public class ProportionalFactorTests
{
    private const int March = 3;
    private const int Year = 2025;

    [Fact]
    public void Calculate_ShouldReturnOne_WhenBothDatesAreEmpty()
    {
        ProportionalFactor.Calculate(null, null, March, Year)
            .Should().Be(1m);
    }

    [Fact]
    public void Calculate_ShouldReturnOne_WhenAdmissionIsFirstDayOfMonth()
    {
        var admission = new DateOnly(Year, March, 1);

        ProportionalFactor.Calculate(admission, null, March, Year)
            .Should().Be(1m);
    }

    [Fact]
    public void Calculate_ShouldReturnSixteenOverThirtyOne_WhenAdmissionIsMidMonth()
    {
        var admission = new DateOnly(Year, March, 16);

        ProportionalFactor.Calculate(admission, null, March, Year)
            .Should().Be(16m / 31m);
    }

    [Fact]
    public void Calculate_ShouldReturnTenOverThirtyOne_WhenDismissalIsMidMonth()
    {
        var dismissal = new DateOnly(Year, March, 10);

        ProportionalFactor.Calculate(null, dismissal, March, Year)
            .Should().Be(10m / 31m);
    }

    [Fact]
    public void Calculate_ShouldReturnSixOverThirtyOne_WhenAdmissionAndDismissalInSameMonth()
    {
        var admission = new DateOnly(Year, March, 5);
        var dismissal = new DateOnly(Year, March, 10);

        ProportionalFactor.Calculate(admission, dismissal, March, Year)
            .Should().Be(6m / 31m);
    }

    [Fact]
    public void Calculate_ShouldReturnZero_WhenDismissalIsBeforeReferenceMonth()
    {
        var dismissal = new DateOnly(Year, 2, 28);

        ProportionalFactor.Calculate(null, dismissal, March, Year)
            .Should().Be(0m);
    }

    [Fact]
    public void Calculate_ShouldReturnOne_WhenAdmissionIsBeforeReferenceMonth()
    {
        var admission = new DateOnly(Year, 2, 10);

        ProportionalFactor.Calculate(admission, null, March, Year)
            .Should().Be(1m);
    }

    [Fact]
    public void Calculate_ShouldReturnFifteenOverThirtyOne_WhenClipEndsMidMonth()
    {
        var admission = new DateOnly(Year, March, 1);
        var clipEnd = new DateOnly(Year, March, 15);

        ProportionalFactor.Calculate(admission, null, March, Year, clipEnd: clipEnd)
            .Should().Be(15m / 31m);
    }

    [Fact]
    public void Calculate_ShouldReturnSixteenOverThirtyOne_WhenClipStartsMidMonth()
    {
        var admission = new DateOnly(Year, March, 1);
        var clipStart = new DateOnly(Year, March, 16);

        ProportionalFactor.Calculate(admission, null, March, Year, clipStart: clipStart)
            .Should().Be(16m / 31m);
    }

    [Fact]
    public void CalculateForEntry_ShouldAlwaysUseCollaboratorDismissal_NotRoleSnapshot()
    {
        var input = new PayrollEntryInput
        {
            Month = March,
            Year = Year,
            Collaborator = new Collaborator
            {
                Id = Guid.NewGuid(),
                Name = "Colaborador",
                DepartmentId = Guid.NewGuid(),
                AdmissionDate = new DateOnly(Year, March, 1),
                DismissalDate = new DateOnly(Year, March, 20)
            },
            PeriodClipStart = new DateOnly(Year, March, 16),
            PeriodClipEnd = new DateOnly(Year, March, 31)
        };

        ProportionalFactor.CalculateForEntry(input).Should().Be(5m / 31m);
    }
}
