using Core.Domain;

namespace WebAPI.Tests.MasterData;

internal static class MasterDataTestHelper
{
    public static object CreateDepartmentPayload(
        string name = "Setor Teste",
        string calculationType = "commercialAnalyst",
        decimal goalBonusPercentage = 0m,
        decimal lowRevenueThreshold = 200_000m,
        decimal lowRevenueBonusPct = 0.4m,
        string? description = null,
        bool isActive = true,
        bool isAllocatedFixed = false,
        bool routesFixedToLimaKarttos = false) =>
        new
        {
            name,
            calculationType,
            goalBonusPercentage,
            lowRevenueThreshold,
            lowRevenueBonusPct,
            description,
            isActive,
            isAllocatedFixed,
            routesFixedToLimaKarttos
        };

    public static object CreateCareerLevelPayload(
        string name = "Nível Teste",
        Guid? departmentId = null,
        string profile = "commercialAnalyst",
        bool isActive = true,
        decimal baseSalary = 1500m,
        decimal ftdRateBase = 2m,
        decimal salesPctBase = 4m,
        int ftdBonusEvery = 250,
        decimal ftdBonusValue = 350m) =>
        new
        {
            name,
            departmentId,
            profile,
            isActive,
            baseSalary,
            commissionWithoutGoalPct = 0m,
            commissionWithGoalPct = 0m,
            commissionWithSuperGoalPct = 0m,
            groupCommissionPerPercent = 0m,
            groupCommissionPer20Percent = 0m,
            defaultCpaValue = 0m,
            goalBonusValue = 0m,
            ftdRateBase,
            ftdRateWithGoal = 2.5m,
            ftdRateWithSuperGoal = 3m,
            ftdSuperbetRate = 5m,
            ftdBonusEvery,
            ftdBonusValue,
            salesPctBase,
            salesPctWithGoal = 5m,
            salesPctWithSuperGoal = 6m,
            salesBonusEvery = 20_000m,
            salesBonusValue = 250m,
            revPct = 1m,
            betanoInternaValue = 200m,
            betanoMundoBetValue = 70m,
            supFtdSuperbetNoGoal = 4m,
            supFtdSuperbetWithGoal = 5m,
            supFtdOtherNoGoal = 0.3m,
            supFtdOtherWithGoal = 0.5m,
            supSalesPctNoGoal = 0.5m,
            supSalesPctWithGoal = 0.8m,
            supRevPct = 10m,
            netRevenueFactor = 50m,
            netRevenuePctNoGoal = 1.2m,
            netRevenuePctWithGoal = 1.5m,
            trafficInvestmentCommissionPct = 0m,
            trafficCpaEsportiva = 0m,
            trafficCpaStake = 0m,
            trafficCpaBetano = 0m,
            trafficCpaBetMgm = 0m,
            trafficCpaNovibet = 0m,
            trafficCpaBetFair = 0m,
            trafficCpaBlaze = 0m,
            trafficCpaSuperbet = 0m,
            trafficCpaHiperbet = 0m,
            trafficSupBonus = 0m,
            trafficSupCommissionPct = 0m
        };

    public static object CreateProjectPayload(
        string name = "Projeto Teste",
        string? client = "Cliente Teste",
        string platform = "lastlink",
        bool isActive = true,
        bool isDefaultAllocationTarget = false,
        bool excludesGoalBonus = false,
        bool excludesSupervisorFixedAllocation = false) =>
        new
        {
            name,
            client,
            platform,
            isActive,
            isDefaultAllocationTarget,
            excludesGoalBonus,
            excludesSupervisorFixedAllocation
        };

    public static object CreatePaymentMethodPayload(
        string name = "PIX",
        bool isActive = true) =>
        new { name, isActive };
}
