using Core.Domain;
using Core.Domain.PayrollCalculation;
using FluentAssertions;

namespace Core.Tests.PayrollCalculation;

public class TrafficProjectCommissionCalculatorTests
{
    [Fact]
    public void Calculate_ShouldMatchRoadmapAcceptance_InvestedAndCpa()
    {
        var level = CreateLevel(investmentPct: 2m, betanoRate: 50m);
        var entries = new[]
        {
            new TrafficProjectEntryInput(
                Guid.NewGuid(),
                10_000m,
                [new TrafficCpaEntryInput(TrafficHouse.Betano, TrafficCpaKind.Supervised, 3)])
        };

        var result = TrafficProjectCommissionCalculator.Calculate(entries, level, trafficSeniorLevel: null);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(350m);
    }

    [Fact]
    public void Calculate_ManagerCpa_ShouldUseSeniorLevelRate()
    {
        var level = CreateLevel(investmentPct: 0m, betanoRate: 999m);
        var senior = CreateLevel(investmentPct: 0m, betanoRate: 50m);
        var entries = new[]
        {
            new TrafficProjectEntryInput(
                Guid.NewGuid(),
                0m,
                [new TrafficCpaEntryInput(TrafficHouse.Betano, TrafficCpaKind.Manager, 3)])
        };

        var result = TrafficProjectCommissionCalculator.Calculate(entries, level, senior);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(150m);
    }

    [Fact]
    public void Calculate_ShouldSumMultipleProjects()
    {
        var level = CreateLevel(investmentPct: 2m, betanoRate: 50m);
        var entries = new[]
        {
            new TrafficProjectEntryInput(Guid.NewGuid(), 5_000m, []),
            new TrafficProjectEntryInput(
                Guid.NewGuid(),
                0m,
                [new TrafficCpaEntryInput(TrafficHouse.Betano, TrafficCpaKind.Supervised, 2)])
        };

        var result = TrafficProjectCommissionCalculator.Calculate(entries, level, trafficSeniorLevel: null);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(200m);
    }

    [Fact]
    public void Calculate_ShouldReturnZero_WhenNoEntriesOrLevel()
    {
        var level = CreateLevel(2m, 50m);

        TrafficProjectCommissionCalculator.Calculate([], level, null).Value.Should().Be(0m);
        TrafficProjectCommissionCalculator.Calculate(
            [new TrafficProjectEntryInput(Guid.NewGuid(), 1000m, [])],
            careerLevel: null,
            trafficSeniorLevel: null).Value.Should().Be(0m);
    }

    [Fact]
    public void Calculate_ShouldFail_WhenHouseIsInvalid()
    {
        var level = CreateLevel(2m, 50m);
        var entries = new[]
        {
            new TrafficProjectEntryInput(
                Guid.NewGuid(),
                0m,
                [new TrafficCpaEntryInput("casa-invalida", TrafficCpaKind.Supervised, 1)])
        };

        var result = TrafficProjectCommissionCalculator.Calculate(entries, level, null);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("traffic.invalid_house");
    }

    [Fact]
    public void Calculate_ShouldFail_WhenManagerCpaWithoutSeniorLevel()
    {
        var level = CreateLevel(0m, 30m);
        var entries = new[]
        {
            new TrafficProjectEntryInput(
                Guid.NewGuid(),
                0m,
                [new TrafficCpaEntryInput(TrafficHouse.Betano, TrafficCpaKind.Manager, 1)])
        };

        var result = TrafficProjectCommissionCalculator.Calculate(entries, level, trafficSeniorLevel: null);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("traffic.senior_level_not_found");
    }

    private static CareerLevel CreateLevel(decimal investmentPct, decimal betanoRate) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = "Pleno",
            Profile = CalculationProfile.PaidTraffic,
            TrafficInvestmentCommissionPct = investmentPct,
            TrafficCpaBetano = betanoRate
        };
}
