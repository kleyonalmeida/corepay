using FluentAssertions;
using WebApp.Blazor.Components.Traffic;
using WebApp.Blazor.Services;

namespace WebApp.Blazor.Tests.Traffic;

public class TrafficInvestmentFormDialogTests : BlazorComponentTestContext
{
    private static readonly Guid ProjectId = Guid.Parse("8fa85f64-5717-4562-b3fc-2c963f66afa7");

    [Fact]
    public void TrafficInvestmentFormDialog_WithTotals_RendersSemanticStatCards()
    {
        var editTarget = CreateInvestment();

        var cut = Render<TrafficInvestmentFormDialog>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.EditTarget, editTarget)
            .Add(p => p.CanWrite, true));

        cut.Find(".stat-card__icon--yellow").Should().NotBeNull();
        cut.FindAll(".stat-card__icon--emerald").Count.Should().BeGreaterThanOrEqualTo(3);
        cut.Find(".stat-card__icon--purple").Should().NotBeNull();
        cut.Find(".stat-card__icon--orange").Should().NotBeNull();
        cut.Markup.Should().Contain("Solicitado");
        cut.Markup.Should().Contain("Imposto");
    }

    private static TrafficInvestmentDto CreateInvestment() =>
        new(
            Guid.NewGuid(),
            ProjectId,
            "Projeto Demo",
            9,
            2026,
            4000m,
            [
                new TrafficWeekDto(
                    1,
                    500m,
                    400m,
                    1000m,
                    121.50m,
                    1121.50m,
                    -721.50m,
                    2121.50m,
                    [],
                    [])
            ],
            new TrafficInvestmentMonthlyTotalsDto(
                500m,
                400m,
                1000m,
                121.50m,
                1121.50m,
                -721.50m));
}
