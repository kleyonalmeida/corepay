using Core.Domain;
using Core.Domain.PayrollCalculation;
using FluentAssertions;

namespace Core.Tests.PayrollCalculation;

public class PaidTrafficCalculatorTests
{
    private const int March = 3;
    private const int Year = 2025;

    [Fact]
    public void Calculate_ShouldCombineBaseAndAutoCommission()
    {
        var input = CreateInput(
            baseSalary: 1000m,
            invested: 10_000m,
            investmentPct: 2m,
            cpaCount: 3,
            cpaRate: 50m);

        var result = PaidTrafficCalculator.Calculate(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.BaseSalary.Should().Be(1000m);
        result.Value.CommissionAmount.Should().Be(350m);
        result.Value.TotalAmount.Should().Be(1350m);
    }

    [Fact]
    public void Calculate_ShouldIncludeManualBonusesInCommissionAmount()
    {
        var input = CreateInput(
            baseSalary: 1000m,
            invested: 10_000m,
            investmentPct: 2m,
            cpaCount: 3,
            cpaRate: 50m,
            bonuses: [100m]);

        var result = PaidTrafficCalculator.Calculate(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.CommissionAmount.Should().Be(450m);
        result.Value.TotalAmount.Should().Be(1450m);
    }

    [Fact]
    public void Calculate_ShouldApplyProportionalFactor_ToBaseOnly()
    {
        var collaborator = CreateCollaborator();
        collaborator.AdmissionDate = new DateOnly(Year, March, 16);

        var input = CreateInput(
            baseSalary: 3100m,
            invested: 0m,
            investmentPct: 0m,
            collaborator: collaborator);

        var expectedBase = decimal.Round(3100m * (16m / 31m), 2, MidpointRounding.AwayFromZero);

        var result = PaidTrafficCalculator.Calculate(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.BaseSalary.Should().Be(expectedBase);
    }

    [Fact]
    public void Calculate_ShouldSubtractDeductions()
    {
        var input = CreateInput(
            baseSalary: 1000m,
            invested: 10_000m,
            investmentPct: 2m,
            cpaCount: 3,
            cpaRate: 50m,
            deductions: [50m]);

        var result = PaidTrafficCalculator.Calculate(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalAmount.Should().Be(1300m);
    }

    [Fact]
    public void Calculate_ShouldIgnoreTrafficSupFields()
    {
        var level = CreateLevel(2m, 50m);
        level.TrafficSupBonus = 500m;
        level.TrafficSupCommissionPct = 10m;

        var input = CreateInput(
            baseSalary: 0m,
            invested: 10_000m,
            investmentPct: 2m,
            cpaCount: 3,
            cpaRate: 50m,
            level: level);

        var result = PaidTrafficCalculator.Calculate(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.CommissionAmount.Should().Be(350m);
    }

    [Fact]
    public void Calculate_ShouldIgnoreGoalTier()
    {
        var input = CreateInput(
            baseSalary: 0m,
            invested: 10_000m,
            investmentPct: 2m,
            cpaCount: 0,
            cpaRate: 50m,
            goalTier: GoalTier.Goal);

        var result = PaidTrafficCalculator.Calculate(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.CommissionAmount.Should().Be(200m);
    }

    [Fact]
    public void Calculate_ShouldFail_WhenManualRateioValueIsProvided()
    {
        var input = CreateInput(
            baseSalary: 1000m,
            invested: 0m,
            investmentPct: 0m,
            rateioEntries: [new RateioProjectEntryInput(Guid.NewGuid(), RateioValue: 500m)]);

        var result = PaidTrafficCalculator.Calculate(input);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("traffic.manual_rateio_not_allowed");
    }

    private static PayrollEntryInput CreateInput(
        decimal baseSalary,
        decimal invested,
        decimal investmentPct,
        int cpaCount = 0,
        decimal cpaRate = 50m,
        CareerLevel? level = null,
        Collaborator? collaborator = null,
        GoalTier goalTier = GoalTier.None,
        decimal[]? bonuses = null,
        decimal[]? deductions = null,
        IReadOnlyList<RateioProjectEntryInput>? rateioEntries = null)
    {
        var careerLevel = level ?? CreateLevel(investmentPct, cpaRate);
        var trafficEntries = invested > 0m || cpaCount > 0
            ? new[]
            {
                new TrafficProjectEntryInput(
                    Guid.NewGuid(),
                    invested,
                    cpaCount > 0
                        ? [new TrafficCpaEntryInput(TrafficHouse.Betano, TrafficCpaKind.Supervised, cpaCount)]
                        : [])
            }
            : Array.Empty<TrafficProjectEntryInput>();

        return new PayrollEntryInput
        {
            Month = March,
            Year = Year,
            Department = CreateDepartment(),
            CareerLevel = careerLevel,
            Collaborator = collaborator ?? CreateCollaborator(),
            FullBaseSalary = baseSalary,
            GoalTier = goalTier,
            TrafficProjectEntries = trafficEntries,
            RateioProjectEntries = rateioEntries ?? [],
            BonusEntries = (bonuses ?? []).Select(value => new BonusEntryInput(null, value)).ToList(),
            DeductionEntries = (deductions ?? []).Select(value => new DeductionEntryInput(value)).ToList()
        };
    }

    private static Department CreateDepartment() =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = "Tráfego Pago",
            CalculationType = CalculationProfile.PaidTraffic
        };

    private static CareerLevel CreateLevel(decimal investmentPct, decimal betanoRate) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = "Pleno",
            Profile = CalculationProfile.PaidTraffic,
            TrafficInvestmentCommissionPct = investmentPct,
            TrafficCpaBetano = betanoRate
        };

    private static Collaborator CreateCollaborator() =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = "Colaborador Tráfego",
            DepartmentId = Guid.NewGuid()
        };
}
