using Core.Domain;
using Core.Domain.PayrollCalculation;
using FluentAssertions;

namespace Core.Tests.PayrollCalculation;

public class TrafficCpaRateHelperTests
{
    public static IEnumerable<object[]> SupervisedHouseRates =>
        TrafficHouse.All.Select(house => new object[] { house, ExpectedSupervisedRate(house) });

    public static IEnumerable<object[]> ManagerHouseRates =>
        TrafficHouse.All.Select(house => new object[] { house, ExpectedSeniorRate(house) });

    [Theory]
    [MemberData(nameof(SupervisedHouseRates))]
    public void GetTrafficCpaRate_Supervised_ShouldUseCurrentLevel(string house, decimal expectedRate)
    {
        var currentLevel = CreateLevelWithDistinctRates();
        var seniorLevel = CreateSeniorLevel();

        var result = TrafficCpaRateHelper.GetTrafficCpaRate(
            house,
            TrafficCpaKind.Supervised,
            currentLevel,
            seniorLevel);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(expectedRate);
    }

    [Theory]
    [MemberData(nameof(ManagerHouseRates))]
    public void GetTrafficCpaRate_Manager_ShouldUseSeniorLevel(string house, decimal expectedRate)
    {
        var currentLevel = CreateLevelWithDistinctRates();
        var seniorLevel = CreateSeniorLevel();

        var result = TrafficCpaRateHelper.GetTrafficCpaRate(
            house,
            TrafficCpaKind.Manager,
            currentLevel,
            seniorLevel);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(expectedRate);
    }

    [Fact]
    public void GetTrafficCpaRate_Manager_ShouldFail_WhenSeniorLevelMissing()
    {
        var currentLevel = CreateLevelWithDistinctRates();

        var result = TrafficCpaRateHelper.GetTrafficCpaRate(
            TrafficHouse.Betano,
            TrafficCpaKind.Manager,
            currentLevel,
            seniorLevel: null);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("traffic.senior_level_not_found");
    }

    [Fact]
    public void GetTrafficCpaRate_ShouldFail_ForInvalidCpaKind()
    {
        var currentLevel = CreateLevelWithDistinctRates();
        var invalidKind = (TrafficCpaKind)99;

        var result = TrafficCpaRateHelper.GetTrafficCpaRate(
            TrafficHouse.Betano,
            invalidKind,
            currentLevel,
            seniorLevel: null);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("traffic.invalid_cpa_kind");
    }

    [Fact]
    public void GetTrafficCpaRate_ShouldFail_ForInvalidHouse()
    {
        var currentLevel = CreateLevelWithDistinctRates();

        var result = TrafficCpaRateHelper.GetTrafficCpaRate(
            "casa-inexistente",
            TrafficCpaKind.Supervised,
            currentLevel,
            seniorLevel: null);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("traffic.invalid_house");
    }

    [Fact]
    public void GetTrafficCpaRate_Manager_Betano_ShouldNotUseCurrentLevelRate()
    {
        var currentLevel = CreateLevelWithDistinctRates();
        currentLevel.TrafficCpaBetano = 999m;
        var seniorLevel = CreateSeniorLevel();
        seniorLevel.TrafficCpaBetano = 50m;

        var result = TrafficCpaRateHelper.GetTrafficCpaRate(
            TrafficHouse.Betano,
            TrafficCpaKind.Manager,
            currentLevel,
            seniorLevel);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(50m);
    }

    private static CareerLevel CreateLevelWithDistinctRates() =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = "Pleno",
            Profile = CalculationProfile.PaidTraffic,
            TrafficCpaEsportiva = 10m,
            TrafficCpaStake = 20m,
            TrafficCpaBetano = 30m,
            TrafficCpaBetMgm = 40m,
            TrafficCpaNovibet = 50m,
            TrafficCpaBetFair = 60m,
            TrafficCpaBlaze = 70m,
            TrafficCpaSuperbet = 80m,
            TrafficCpaHiperbet = 90m
        };

    private static CareerLevel CreateSeniorLevel() =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = "Sênior",
            Profile = CalculationProfile.PaidTraffic,
            TrafficCpaEsportiva = 100m,
            TrafficCpaStake = 110m,
            TrafficCpaBetano = 120m,
            TrafficCpaBetMgm = 130m,
            TrafficCpaNovibet = 140m,
            TrafficCpaBetFair = 150m,
            TrafficCpaBlaze = 160m,
            TrafficCpaSuperbet = 170m,
            TrafficCpaHiperbet = 180m
        };

    private static decimal ExpectedSupervisedRate(string house) =>
        house switch
        {
            TrafficHouse.Esportiva => 10m,
            TrafficHouse.Stake => 20m,
            TrafficHouse.Betano => 30m,
            TrafficHouse.BetMgm => 40m,
            TrafficHouse.Novibet => 50m,
            TrafficHouse.BetFair => 60m,
            TrafficHouse.Blaze => 70m,
            TrafficHouse.Superbet => 80m,
            TrafficHouse.Hiperbet => 90m,
            _ => throw new ArgumentOutOfRangeException(nameof(house), house, null)
        };

    private static decimal ExpectedSeniorRate(string house) =>
        house switch
        {
            TrafficHouse.Esportiva => 100m,
            TrafficHouse.Stake => 110m,
            TrafficHouse.Betano => 120m,
            TrafficHouse.BetMgm => 130m,
            TrafficHouse.Novibet => 140m,
            TrafficHouse.BetFair => 150m,
            TrafficHouse.Blaze => 160m,
            TrafficHouse.Superbet => 170m,
            TrafficHouse.Hiperbet => 180m,
            _ => throw new ArgumentOutOfRangeException(nameof(house), house, null)
        };
}
