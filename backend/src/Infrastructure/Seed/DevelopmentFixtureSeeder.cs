using Core.Domain;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Seed;

/// <summary>Idempotent master data used by integration tests and opt-in Development startup.</summary>
public sealed class DevelopmentFixtureSeeder
{
    private readonly AppDbContext _db;

    public DevelopmentFixtureSeeder(AppDbContext db) => _db = db;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        foreach (var item in Departments)
            await UpsertDepartmentAsync(item, cancellationToken);

        var commercial = await IdAsync(SeedKeys.Departments.CommercialAnalysts, cancellationToken);
        var traffic = await IdAsync(SeedKeys.Departments.PaidTraffic, cancellationToken);
        foreach (var item in Levels(commercial, traffic))
            await UpsertLevelAsync(item, cancellationToken);

        foreach (var item in Projects)
            await UpsertProjectAsync(item, cancellationToken);

        var commercialLevel = await IdAsync(SeedKeys.CareerLevels.CommercialAnalystJunior, cancellationToken);
        var trafficLevel = await IdAsync(SeedKeys.CareerLevels.PaidTrafficSenior, cancellationToken);
        foreach (var item in Collaborators(commercial, traffic, commercialLevel, trafficLevel))
            await UpsertCollaboratorAsync(item, cancellationToken);
    }

    private async Task<Guid> SeedIdAsync(string key, CancellationToken ct)
    {
        var mapping = await _db.SeedEntities.SingleOrDefaultAsync(x => x.Key == key, ct);
        if (mapping is not null) return mapping.EntityId;
        mapping = new SeedEntity { Key = key, EntityId = Guid.NewGuid() };
        _db.SeedEntities.Add(mapping);
        await _db.SaveChangesAsync(ct);
        return mapping.EntityId;
    }

    private async Task<Guid> IdAsync(string key, CancellationToken ct) =>
        (await _db.SeedEntities.SingleAsync(x => x.Key == key, ct)).EntityId;

    private async Task UpsertDepartmentAsync(DepartmentDef d, CancellationToken ct)
    {
        var id = await SeedIdAsync(d.Key, ct);
        var entity = await _db.Departments.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) { entity = new Department { Id = id }; _db.Departments.Add(entity); }
        entity.Name = d.Name; entity.CalculationType = d.Profile; entity.IsAllocatedFixed = d.Allocated;
        entity.RoutesFixedToLimaKarttos = d.RoutesToLima; entity.IsActive = true;
        await _db.SaveChangesAsync(ct);
    }

    private async Task UpsertLevelAsync(LevelDef d, CancellationToken ct)
    {
        var id = await SeedIdAsync(d.Key, ct);
        var entity = await _db.CareerLevels.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) { entity = new CareerLevel { Id = id }; _db.CareerLevels.Add(entity); }
        entity.Name = d.Name; entity.DepartmentId = d.DepartmentId; entity.Profile = d.Profile; entity.IsActive = true;
        entity.BaseSalary = d.BaseSalary; entity.FtdRateBase = d.FtdRateBase; entity.SalesPctBase = d.SalesPctBase;
        entity.FtdBonusEvery = d.FtdBonusEvery; entity.FtdBonusValue = d.FtdBonusValue;
        entity.TrafficInvestmentCommissionPct = d.TrafficCommission; entity.TrafficCpaBetano = d.TrafficCpa;
        await _db.SaveChangesAsync(ct);
    }

    private async Task UpsertProjectAsync(ProjectDef d, CancellationToken ct)
    {
        var id = await SeedIdAsync(d.Key, ct);
        var entity = await _db.Projects.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) { entity = new Project { Id = id }; _db.Projects.Add(entity); }
        entity.Name = d.Name; entity.Platform = d.Platform; entity.IsDefaultAllocationTarget = d.Default;
        entity.ExcludesGoalBonus = d.ExcludesGoal; entity.ExcludesSupervisorFixedAllocation = d.ExcludesSupervisor;
        entity.IsActive = true;
        await _db.SaveChangesAsync(ct);
    }

    private async Task UpsertCollaboratorAsync(CollaboratorDef d, CancellationToken ct)
    {
        var id = await SeedIdAsync(d.Key, ct);
        var entity = await _db.Collaborators.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) { entity = new Collaborator { Id = id }; _db.Collaborators.Add(entity); }
        entity.Name = d.Name; entity.DepartmentId = d.DepartmentId; entity.CareerLevelId = d.LevelId;
        entity.JobTitle = d.Title; entity.AdmissionDate = d.Admission; entity.DismissalDate = d.Dismissal;
        entity.Email = d.Email; entity.PixKey = d.Pix; entity.BaseSalary = d.Salary; entity.IsActive = d.Active;
        await _db.SaveChangesAsync(ct);
    }

    private static readonly DepartmentDef[] Departments =
    [
        new(SeedKeys.Departments.Tipster, "Setor Tipster", CalculationProfile.Tipster),
        new(SeedKeys.Departments.PaidTraffic, "Tráfego Pago", CalculationProfile.PaidTraffic),
        new(SeedKeys.Departments.ProjectLeaders, "Líderes de Projetos", CalculationProfile.ProjectLeader),
        new(SeedKeys.Departments.CommercialAnalysts, "Analistas Comerciais", CalculationProfile.CommercialAnalyst),
        new(SeedKeys.Departments.Management, "Gerência", CalculationProfile.Management),
        new(SeedKeys.Departments.Affiliates, "Affiliates", CalculationProfile.FixedCommissionBonus),
        new(SeedKeys.Departments.Administrative, "Administrativo", CalculationProfile.AllocatedFixed, true),
        new(SeedKeys.Departments.Automation, "IA Automação", CalculationProfile.AllocatedFixed, true, true),
        new(SeedKeys.Departments.Contingency, "Contingência", CalculationProfile.AllocatedFixed, true, true),
        new(SeedKeys.Departments.Support, "Suporte", CalculationProfile.AllocatedFixed, true)
    ];

    private static LevelDef[] Levels(Guid commercial, Guid traffic) =>
    [
        new(SeedKeys.CareerLevels.CommercialAnalystJunior, "Analista Comercial Júnior", commercial,
            CalculationProfile.CommercialAnalyst, 3500m, 2m, 4m, 250, 350m),
        new(SeedKeys.CareerLevels.CommercialSupervisor, "Supervisor", commercial, CalculationProfile.CommercialSupervisor, 6000m),
        new(SeedKeys.CareerLevels.PaidTrafficSenior, "Sênior", traffic, CalculationProfile.PaidTraffic,
            4200m, TrafficCommission: 2m, TrafficCpa: 50m)
    ];

    private static readonly ProjectDef[] Projects =
    [
        new(SeedKeys.Projects.LimaKarttos, "Projeto Demo Lima", ProjectPlatform.Lastlink, Default: true),
        new(SeedKeys.Projects.FeiraX, "Projeto Demo Feira", ProjectPlatform.Lastlink, ExcludesGoal: true),
        new(SeedKeys.Projects.ThreeCSports, "Projeto Demo 3C", ProjectPlatform.Lastlink, ExcludesSupervisor: true),
        new(SeedKeys.Projects.Affiliates, "Projeto Demo Affiliates", ProjectPlatform.Lastlink),
        new(SeedKeys.Projects.LastlinkSample, "Projeto Lastlink Demo", ProjectPlatform.Lastlink),
        new(SeedKeys.Projects.HublaSample, "Projeto Hubla Demo", ProjectPlatform.Hubla)
    ];

    private static CollaboratorDef[] Collaborators(Guid commercial, Guid traffic, Guid commercialLevel, Guid trafficLevel) =>
    [
        new(SeedKeys.Collaborators.CommercialAnalystActive, "Colaborador Demo Ativo", commercial, commercialLevel,
            "Analista Comercial Demo", new(2024, 3, 1), null, "ana.comercial@corepay.test", "pix-demo-ativo@corepay.test", 3500m, true),
        new(SeedKeys.Collaborators.PaidTrafficInactive, "Colaborador Demo Inativo", traffic, trafficLevel,
            "Especialista Tráfego Demo", new(2023, 6, 15), new(2025, 3, 15),
            "bruno.trafego@corepay.test", "pix-demo-inativo@corepay.test", 4200m, false)
    ];

    private sealed record DepartmentDef(string Key, string Name, CalculationProfile Profile, bool Allocated = false, bool RoutesToLima = false);
    private sealed record LevelDef(string Key, string Name, Guid DepartmentId, CalculationProfile Profile, decimal BaseSalary = 0,
        decimal FtdRateBase = 0, decimal SalesPctBase = 0, int FtdBonusEvery = 0, decimal FtdBonusValue = 0,
        decimal TrafficCommission = 0, decimal TrafficCpa = 0);
    private sealed record ProjectDef(string Key, string Name, ProjectPlatform Platform, bool Default = false,
        bool ExcludesGoal = false, bool ExcludesSupervisor = false);
    private sealed record CollaboratorDef(string Key, string Name, Guid DepartmentId, Guid LevelId, string Title,
        DateOnly Admission, DateOnly? Dismissal, string Email, string Pix, decimal Salary, bool Active);
}
