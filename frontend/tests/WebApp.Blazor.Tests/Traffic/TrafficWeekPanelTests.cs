using FluentAssertions;
using WebApp.Blazor.Components.Traffic;
using WebApp.Blazor.Services;

namespace WebApp.Blazor.Tests.Traffic;

public class TrafficWeekPanelTests : BlazorComponentTestContext
{
    [Fact]
    public void TrafficWeekPanel_NoDeposits_ShowsEmptyState()
    {
        var state = new TrafficWeekEditState { WeekNumber = 1 };

        var cut = Render<TrafficWeekPanel>(parameters => parameters
            .Add(p => p.WeekNumber, 1)
            .Add(p => p.State, state)
            .Add(p => p.CanWrite, true));

        cut.Markup.Should().Contain("Nenhum depósito cadastrado");
        cut.Markup.Should().Contain("empty-state");
        cut.Markup.Should().Contain("Adicionar");
    }

    [Fact]
    public void TrafficWeekPanel_WithDeposits_DoesNotShowEmptyState()
    {
        var state = new TrafficWeekEditState { WeekNumber = 1 };
        state.Deposits.Add(new TrafficWeekDepositEditState());

        var cut = Render<TrafficWeekPanel>(parameters => parameters
            .Add(p => p.WeekNumber, 1)
            .Add(p => p.State, state)
            .Add(p => p.CanWrite, true));

        cut.Markup.Should().NotContain("Nenhum depósito cadastrado");
        cut.Markup.Should().Contain("Solicitado");
    }

    [Fact]
    public void TrafficWeekPanel_WithCalculatedWeek_RendersSemanticStatTones()
    {
        var state = new TrafficWeekEditState { WeekNumber = 1 };
        var calculatedWeek = new TrafficWeekDto(
            1,
            500m,
            400m,
            1000m,
            121.50m,
            1121.50m,
            -721.50m,
            2121.50m,
            [],
            []);

        var cut = Render<TrafficWeekPanel>(parameters => parameters
            .Add(p => p.WeekNumber, 1)
            .Add(p => p.State, state)
            .Add(p => p.CalculatedWeek, calculatedWeek)
            .Add(p => p.CanWrite, true));

        cut.Find(".stat-card__icon--yellow").Should().NotBeNull();
        cut.FindAll(".stat-card__icon--emerald").Count.Should().BeGreaterThanOrEqualTo(3);
        cut.Find(".stat-card__icon--purple").Should().NotBeNull();
        cut.Find(".stat-card__icon--orange").Should().NotBeNull();
    }
}
