using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Blazor.Components.Payroll;
using WebApp.Blazor.Components.Payroll.Blocks;
using WebApp.Blazor.Services;
using WebApp.Blazor.Tests.Auth;

namespace WebApp.Blazor.Tests.Payroll;

public class PayrollEntryEditorTests : BlazorComponentTestContext
{
    private readonly StubHttpMessageHandler _httpHandler;
    private static readonly Guid PayrollId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid EntryId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid ProjectA = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid ProjectB = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

    public PayrollEntryEditorTests()
    {
        _httpHandler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        Services.AddScoped(_ => new HttpClient(_httpHandler) { BaseAddress = new Uri("http://localhost:5000") });
        Services.AddScoped<IPayrollApiService, PayrollApiService>();
    }

    [Fact]
    public void CommercialAnalystEntryBlock_ShouldRenderHublaHint()
    {
        var state = CreateCommercialState();
        var options = CreateOptions(ProjectB, ProjectPlatform.Hubla);
        state.CommercialProjectEntries.Add(new CommercialAnalystProjectEntryDto(
            ProjectB, ProjectPlatform.Hubla, 0, 0, false, false, 0, 0m, false, false, 0m));

        var cut = Render<CommercialAnalystEntryBlock>(parameters => parameters
            .Add(p => p.State, state)
            .Add(p => p.Options, options));

        cut.Markup.Should().Contain("teto máximo de 4%");
    }

    [Fact]
    public void CommercialAnalystEntryBlock_ShouldShowComplementErrorWhenSumNot100()
    {
        var state = CreateCommercialState();
        state.ComplementPayingProjects.Add(new ComplementPayingProjectDto(ProjectA, 80m));
        state.ComplementPayingProjects.Add(new ComplementPayingProjectDto(ProjectB, 30m));

        var cut = Render<CommercialAnalystEntryBlock>(parameters => parameters
            .Add(p => p.State, state)
            .Add(p => p.Options, CreateOptions(ProjectA, ProjectPlatform.Lastlink, ProjectB, ProjectPlatform.Hubla)));

        cut.Markup.Should().Contain("A soma deve ser 100%");
        cut.Find(".payroll-nested-block--error").Should().NotBeNull();
    }

    [Fact]
    public void ProfileBlocks_ShouldRenderDistinctMarkupPerProfile()
    {
        var options = CreateOptions(ProjectA, ProjectPlatform.Lastlink);
        var commercial = Render<CommercialAnalystEntryBlock>(p => p
            .Add(x => x.State, new PayrollEntryEditState(CreateEditorEntry(CalculationProfile.CommercialAnalyst)))
            .Add(x => x.Options, options));
        var supervisor = Render<CommercialSupervisorEntryBlock>(p => p
            .Add(x => x.State, new PayrollEntryEditState(CreateEditorEntry(CalculationProfile.CommercialSupervisor)))
            .Add(x => x.Options, options));
        var traffic = Render<PaidTrafficEntryBlock>(p => p
            .Add(x => x.State, new PayrollEntryEditState(CreateEditorEntry(CalculationProfile.PaidTraffic)))
            .Add(x => x.Options, options));
        var management = Render<ManagementEntryBlock>(p => p
            .Add(x => x.State, new PayrollEntryEditState(CreateEditorEntry(CalculationProfile.Management)))
            .Add(x => x.Options, options));
        var leader = Render<ProjectLeaderEntryBlock>(p => p
            .Add(x => x.State, new PayrollEntryEditState(CreateEditorEntry(CalculationProfile.ProjectLeader)))
            .Add(x => x.Options, options));
        var tipster = Render<FixedBonusSectionEntryBlock>(p => p
            .Add(x => x.State, new PayrollEntryEditState(CreateEditorEntry(CalculationProfile.Tipster)))
            .Add(x => x.Options, options));
        var allocated = Render<FixedBonusSectionEntryBlock>(p => p
            .Add(x => x.State, new PayrollEntryEditState(CreateEditorEntry(CalculationProfile.AllocatedFixed)))
            .Add(x => x.Options, options));
        var fixedBonus = Render<FixedBonusSectionEntryBlock>(p => p
            .Add(x => x.State, new PayrollEntryEditState(CreateEditorEntry(CalculationProfile.FixedBonus)))
            .Add(x => x.Options, options));
        var commissionOnly = Render<CommissionProfilesEntryBlock>(p => p
            .Add(x => x.State, new PayrollEntryEditState(CreateEditorEntry(CalculationProfile.CommissionOnly)))
            .Add(x => x.Options, options));
        var fixedCommission = Render<CommissionProfilesEntryBlock>(p => p
            .Add(x => x.State, new PayrollEntryEditState(CreateEditorEntry(CalculationProfile.FixedCommission)))
            .Add(x => x.Options, options));
        var affiliates = Render<CommissionProfilesEntryBlock>(p => p
            .Add(x => x.State, new PayrollEntryEditState(CreateEditorEntry(CalculationProfile.FixedCommissionBonus)))
            .Add(x => x.Options, options));

        commercial.Markup.Should().Contain("Projetos comerciais");
        supervisor.Markup.Should().Contain("Projetos supervisor");
        traffic.Markup.Should().Contain("Projetos de tráfego");
        management.Markup.Should().Contain("Faturamento líquido");
        leader.Markup.Should().Contain("Faturamento por projeto");
        tipster.Markup.Should().Contain("Tipster");
        allocated.Markup.Should().Contain("Rateado");
        fixedBonus.Markup.Should().Contain("Fixo + bônus");
        commissionOnly.Markup.Should().Contain("Comissão pura");
        fixedCommission.Markup.Should().Contain("Fixo + comissão");
        affiliates.Markup.Should().Contain("Affiliates");
    }

    [Fact]
    public async Task PayrollEntryEditor_ShouldUpdateTotalAfterPreview()
    {
        var previewCalls = 0;
        _httpHandler.Configure(request =>
        {
            if (request.RequestUri!.AbsolutePath.Contains("/preview"))
            {
                previewCalls++;
                var sales = previewCalls == 1 ? 10_000m : 20_000m;
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(new
                    {
                        result = new
                        {
                            totalAmount = sales / 10m,
                            baseSalary = 0m,
                            commissionAmount = sales / 10m,
                            goalBonusAmount = 0m,
                            groupCommissionAmount = 0m,
                            platformTotal = 400m
                        },
                        projectTotals = Array.Empty<object>()
                    }))
                };
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        var entry = CreateEditorEntry(
            CalculationProfile.CommercialAnalyst,
            commercialProjects:
            [
                new CommercialAnalystProjectEntryDto(
                    ProjectA, ProjectPlatform.Lastlink, 0, 0, false, false, 0, 10_000m, false, false, 0m)
            ]);
        var state = new PayrollEntryEditState(entry);

        var cut = Render<PayrollEntryEditor>(parameters => parameters
            .Add(p => p.State, state)
            .Add(p => p.Options, CreateOptions(ProjectA, ProjectPlatform.Lastlink))
            .Add(p => p.PayrollId, PayrollId));

        cut.FindAll("input.ui-input").First().Input("2");

        cut.WaitForAssertion(() =>
        {
            previewCalls.Should().BeGreaterThan(0);
            state.LastResult.Should().NotBeNull();
            cut.Markup.Should().Contain("Total");
        }, TimeSpan.FromSeconds(5));
    }

    private static PayrollEntryEditState CreateCommercialState()
    {
        var entry = CreateEditorEntry(CalculationProfile.CommercialAnalyst);
        return new PayrollEntryEditState(entry);
    }

    private static PayrollEntryEditorDto CreateEditorEntry(
        CalculationProfile profile,
        IReadOnlyList<CommercialAnalystProjectEntryDto>? commercialProjects = null) =>
        new(
            EntryId,
            Guid.NewGuid(),
            "Ana Comercial",
            "Analista",
            "11999990001",
            new DateOnly(2024, 1, 1),
            3500m,
            profile,
            IsApproved: false,
            GoalTier: GoalTier.None,
            Payload: new PayrollEntryPayloadDto([], [], commercialProjects ?? [], [], [], [], [], [], [], []));

    private static PayrollEditorOptionsDto CreateOptions(
        Guid projectA,
        ProjectPlatform platformA,
        Guid? projectB = null,
        ProjectPlatform platformB = ProjectPlatform.Lastlink) =>
        new(
            projectB is null
                ? [new PayrollEditorProjectOptionDto(projectA, "Projeto A", platformA, false, false, false)]
                :
                [
                    new PayrollEditorProjectOptionDto(projectA, "Projeto A", platformA, false, false, false),
                    new PayrollEditorProjectOptionDto(projectB.Value, "Projeto B", platformB, false, false, false)
                ],
            [new PayrollEditorDepartmentOptionDto(Guid.NewGuid(), "Comercial")],
            [new PayrollEditorCareerLevelOptionDto(Guid.NewGuid(), "Analista", Guid.NewGuid(), CalculationProfile.CommercialAnalyst)]);
}
