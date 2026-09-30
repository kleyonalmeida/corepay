using System.Globalization;
using System.Security.Cryptography;
using Core.Domain;
using Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Seed;

public sealed record LegacySeedFileReport(string FileName, string Sha256, int Rows);

public sealed record LegacySeedEntityReport(int Read, int Created, int Existing);

public sealed record LegacySeedError(string Entity, int? Row, string Message);

public sealed record PendingModification(
    string Entity,
    int Row,
    Guid TargetId,
    string Field,
    string Reason);

public sealed record LegacySeedReport(
    bool Succeeded,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset FinishedAtUtc,
    IReadOnlyList<LegacySeedFileReport> Files,
    IReadOnlyDictionary<string, LegacySeedEntityReport> Entities,
    IReadOnlyList<LegacySeedError> Errors,
    IReadOnlyList<PendingModification> PendingModifications);

public sealed class LegacyDataSeeder(AppDbContext db)
{
    private const string DepartmentFile = "Department_export.csv";
    private const string CareerLevelFile = "CareerLevel_export.csv";
    private const string CollaboratorFile = "Collaborator_export.csv";

    private static readonly string[] DepartmentHeaders =
    [
        "name", "calculation_type", "low_revenue_bonus_pct", "description",
        "goal_bonus_percentage", "low_revenue_threshold", "id", "created_date",
        "updated_date", "created_by_id", "created_by", "is_sample"
    ];

    private static readonly string[] CareerLevelHeaders =
    [
        "commission_with_goal", "commission_without_goal", "sales_bonus_value",
        "traffic_cpa_blaze", "traffic_cpa_betano", "traffic_cpa_betfair",
        "traffic_cpa_novibet", "sup_rev_pct", "ftd_rate_base", "net_revenue_factor",
        "base_salary", "traffic_cpa_hiperbet", "sales_pct_one_goal",
        "traffic_cpa_esportiva", "traffic_cpa_betmgm", "ftd_bonus_every",
        "ftd_bonus_value", "default_cpa_value", "sup_ftd_superbet_with_goal",
        "net_revenue_pct_with_goal", "traffic_cpa_stake", "sup_sales_pct_no_goal",
        "goal_bonus_value", "sales_pct_both_goals", "ftd_superbet_rate",
        "sup_ftd_others_with_goal", "traffic_sup_commission_pct",
        "sup_sales_pct_with_goal", "department_id", "commission_with_super_goal",
        "ftd_rate_one_goal", "traffic_sup_bonus", "traffic_cpa_superbet",
        "net_revenue_pct_no_goal", "group_commission_per_percent",
        "group_commission_per_20_percent", "sup_ftd_superbet_no_goal",
        "traffic_investment_commission_pct", "name", "ftd_rate_both_goals",
        "sup_ftd_others_no_goal", "sales_bonus_every", "rev_commission_pct",
        "sales_pct_base", "id", "created_date", "updated_date", "created_by_id",
        "created_by", "is_sample"
    ];

    private static readonly string[] CollaboratorHeaders =
    [
        "pix_key", "base_salary", "is_active", "department_id", "admission_date",
        "name", "dismissal_date", "photo_url", "job_title", "email",
        "career_level_id", "id", "created_date", "updated_date", "created_by_id",
        "created_by", "is_sample"
    ];

    public async Task<LegacySeedReport> SeedAsync(
        string inputDirectory,
        CancellationToken cancellationToken = default)
    {
        var started = DateTimeOffset.UtcNow;
        var errors = new List<LegacySeedError>();
        var pendingModifications = new List<PendingModification>();
        var fileReports = new List<LegacySeedFileReport>();
        var entityReports = new Dictionary<string, LegacySeedEntityReport>(StringComparer.Ordinal);

        CsvDocument departmentsCsv;
        CsvDocument levelsCsv;
        CsvDocument collaboratorsCsv;
        try
        {
            departmentsCsv = await ReadFileAsync(inputDirectory, DepartmentFile, DepartmentHeaders, fileReports, cancellationToken);
            levelsCsv = await ReadFileAsync(inputDirectory, CareerLevelFile, CareerLevelHeaders, fileReports, cancellationToken);
            collaboratorsCsv = await ReadFileAsync(inputDirectory, CollaboratorFile, CollaboratorHeaders, fileReports, cancellationToken);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or FormatException)
        {
            errors.Add(new LegacySeedError("arquivo", null, SanitizeException(exception)));
            return Finish(false);
        }

        var departments = ParseRows("setor", departmentsCsv, ParseDepartment, errors);
        var levels = ParseRows("nível", levelsCsv, ParseCareerLevel, errors);
        var collaborators = ParseRows("colaborador", collaboratorsCsv, ParseCollaborator, errors);

        ValidateUniqueKeys("setor", departments.Select(x => x.SourceKey), errors);
        ValidateUniqueKeys("nível", levels.Select(x => x.SourceKey), errors);
        ValidateUniqueKeys("colaborador", collaborators.Select(x => x.SourceKey), errors);
        ValidateUniqueKeys("setor.nome", departments.Select(x => x.Name), errors);
        ValidateUniqueKeys("nível.nome", levels.Select(x => $"{x.DepartmentSourceKey}|{x.Name}"), errors);

        var departmentKeys = departments.Select(x => x.SourceKey).ToHashSet(StringComparer.Ordinal);
        var levelKeys = levels.Select(x => x.SourceKey).ToHashSet(StringComparer.Ordinal);
        foreach (var level in levels.Where(x => x.DepartmentSourceKey is not null))
            if (!departmentKeys.Contains(level.DepartmentSourceKey!))
                errors.Add(new LegacySeedError("nível", level.Row, "referência de setor não encontrada."));
        foreach (var collaborator in collaborators)
        {
            if (!departmentKeys.Contains(collaborator.DepartmentSourceKey))
                errors.Add(new LegacySeedError("colaborador", collaborator.Row, "referência de setor não encontrada."));
            if (collaborator.CareerLevelSourceKey is not null && !levelKeys.Contains(collaborator.CareerLevelSourceKey))
                errors.Add(new LegacySeedError("colaborador", collaborator.Row, "referência de nível não encontrada."));
        }

        if (errors.Count > 0)
            return Finish(false);

        var departmentIds = departments.ToDictionary(
            x => x.SourceKey,
            x => DeterministicGuid.Create("department", x.SourceKey),
            StringComparer.Ordinal);
        var levelIds = levels.ToDictionary(
            x => x.SourceKey,
            x => DeterministicGuid.Create("career-level", x.SourceKey),
            StringComparer.Ordinal);
        var collaboratorIds = collaborators.ToDictionary(
            x => x.SourceKey,
            x => DeterministicGuid.Create("collaborator", x.SourceKey),
            StringComparer.Ordinal);
        var departmentProfiles = departments.ToDictionary(
            x => x.SourceKey,
            ResolveDepartmentProfile,
            StringComparer.Ordinal);

        CollectDatePendingModifications(collaborators, collaboratorIds, pendingModifications);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            entityReports["departments"] = await UpsertDepartmentsAsync(
                departments, departmentIds, departmentProfiles, cancellationToken);
            entityReports["careerLevels"] = await UpsertCareerLevelsAsync(
                levels, levelIds, departmentIds, departmentProfiles, collaborators, cancellationToken);
            entityReports["collaborators"] = await UpsertCollaboratorsAsync(
                collaborators, collaboratorIds, departmentIds, levelIds, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            db.ChangeTracker.Clear();
            errors.Add(new LegacySeedError(
                "seed",
                null,
                exception is DbUpdateException
                    ? "falha ao persistir dados; transação revertida."
                    : "falha durante a seed; transação revertida."));
            return Finish(false);
        }

        return Finish(true);

        LegacySeedReport Finish(bool succeeded) =>
            new(succeeded, started, DateTimeOffset.UtcNow, fileReports, entityReports, errors,
                pendingModifications);
    }

    private static void CollectDatePendingModifications(
        IEnumerable<CollaboratorSource> sources,
        IReadOnlyDictionary<string, Guid> ids,
        ICollection<PendingModification> pending)
    {
        foreach (var source in sources)
        {
            var id = ids[source.SourceKey];
            if (source.AdmissionDate is null)
                pending.Add(new("colaborador", source.Row, id, nameof(Collaborator.AdmissionDate),
                    "Data de admissão não informada."));
            if (!source.IsActive && source.DismissalDate is null)
                pending.Add(new("colaborador", source.Row, id, nameof(Collaborator.DismissalDate),
                    "Colaborador inativo sem data de desligamento."));
            if (source.AdmissionDate is not null && source.DismissalDate < source.AdmissionDate)
                pending.Add(new("colaborador", source.Row, id, nameof(Collaborator.DismissalDate),
                    "Data de desligamento anterior à admissão."));
            if (source.IsActive && source.DismissalDate is not null)
                pending.Add(new("colaborador", source.Row, id, nameof(Collaborator.DismissalDate),
                    "Colaborador ativo possui data de desligamento."));
        }
    }

    private async Task<LegacySeedEntityReport> UpsertDepartmentsAsync(
        IReadOnlyList<DepartmentSource> sources,
        IReadOnlyDictionary<string, Guid> ids,
        IReadOnlyDictionary<string, CalculationProfile> profiles,
        CancellationToken cancellationToken)
    {
        var expectedIds = ids.Values.ToArray();
        var existing = await db.Departments
            .Where(x => expectedIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        var created = 0;

        foreach (var source in sources)
        {
            var id = ids[source.SourceKey];
            if (!existing.TryGetValue(id, out var entity))
            {
                entity = new Department { Id = id };
                db.Departments.Add(entity);
                created++;
            }
            else
            {
                continue;
            }

            var profile = profiles[source.SourceKey];
            entity.Name = source.Name;
            entity.CalculationType = profile;
            entity.LowRevenueBonusPct = source.LowRevenueBonusPct ?? 0.4m;
            entity.Description = source.Description;
            entity.GoalBonusPercentage = source.GoalBonusPercentage ?? 0m;
            entity.LowRevenueThreshold = source.LowRevenueThreshold ?? 200_000m;
            entity.IsActive = true;
            entity.IsAllocatedFixed = profile == CalculationProfile.AllocatedFixed;
            entity.RoutesFixedToLimaKarttos = profile == CalculationProfile.AllocatedFixed
                && source.Name is "I.A/Automação" or "Contingência";
        }

        await db.SaveChangesAsync(cancellationToken);
        return new(sources.Count, created, sources.Count - created);
    }

    private async Task<LegacySeedEntityReport> UpsertCareerLevelsAsync(
        IReadOnlyList<CareerLevelSource> sources,
        IReadOnlyDictionary<string, Guid> ids,
        IReadOnlyDictionary<string, Guid> departmentIds,
        IReadOnlyDictionary<string, CalculationProfile> departmentProfiles,
        IReadOnlyList<CollaboratorSource> collaborators,
        CancellationToken cancellationToken)
    {
        var expectedIds = ids.Values.ToArray();
        var existing = await db.CareerLevels
            .Where(x => expectedIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        var inferredProfiles = InferOrphanLevelProfiles(sources, collaborators, departmentProfiles);
        var created = 0;

        foreach (var source in sources)
        {
            var id = ids[source.SourceKey];
            if (!existing.TryGetValue(id, out var entity))
            {
                entity = new CareerLevel { Id = id };
                db.CareerLevels.Add(entity);
                created++;
            }
            else
            {
                continue;
            }

            entity.Name = source.Name;
            entity.DepartmentId = source.DepartmentSourceKey is null ? null : departmentIds[source.DepartmentSourceKey];
            entity.Profile = source.DepartmentSourceKey is null
                ? inferredProfiles.GetValueOrDefault(source.SourceKey, CalculationProfile.FixedBonus)
                : ResolveCareerProfile(source.Name, departmentProfiles[source.DepartmentSourceKey]);
            entity.IsActive = true;
            ApplyCareerLevelValues(entity, source);
        }

        await db.SaveChangesAsync(cancellationToken);
        return new(sources.Count, created, sources.Count - created);
    }

    private async Task<LegacySeedEntityReport> UpsertCollaboratorsAsync(
        IReadOnlyList<CollaboratorSource> sources,
        IReadOnlyDictionary<string, Guid> ids,
        IReadOnlyDictionary<string, Guid> departmentIds,
        IReadOnlyDictionary<string, Guid> levelIds,
        CancellationToken cancellationToken)
    {
        var expectedIds = ids.Values.ToArray();
        var existing = await db.Collaborators
            .Where(x => expectedIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        var created = 0;

        foreach (var source in sources)
        {
            var id = ids[source.SourceKey];
            if (!existing.TryGetValue(id, out var entity))
            {
                entity = new Collaborator { Id = id };
                db.Collaborators.Add(entity);
                created++;
            }
            else
            {
                continue;
            }

            entity.Name = source.Name;
            entity.DepartmentId = departmentIds[source.DepartmentSourceKey];
            entity.CareerLevelId = source.CareerLevelSourceKey is null ? null : levelIds[source.CareerLevelSourceKey];
            entity.JobTitle = source.JobTitle;
            entity.AdmissionDate = source.AdmissionDate;
            entity.DismissalDate = source.DismissalDate;
            entity.PixKey = source.PixKey;
            entity.BaseSalary = source.BaseSalary;
            entity.Email = source.Email;
            entity.PhotoUrl = source.PhotoUrl;
            entity.IsActive = source.IsActive;
            entity.CalculationProfileOverride = null;
        }

        await db.SaveChangesAsync(cancellationToken);
        return new(sources.Count, created, sources.Count - created);
    }

    private static void ApplyCareerLevelValues(CareerLevel entity, CareerLevelSource source)
    {
        entity.BaseSalary = source.BaseSalary ?? 0m;
        entity.CommissionWithoutGoalPct = source.CommissionWithoutGoalPct ?? 0m;
        entity.CommissionWithGoalPct = source.CommissionWithGoalPct ?? 0m;
        entity.CommissionWithSuperGoalPct = source.CommissionWithSuperGoalPct ?? 0m;
        entity.GroupCommissionPerPercent = source.GroupCommissionPerPercent ?? 0m;
        entity.GroupCommissionPer20Percent = source.GroupCommissionPer20Percent ?? 0m;
        entity.DefaultCpaValue = source.DefaultCpaValue ?? 0m;
        entity.GoalBonusValue = source.GoalBonusValue ?? 0m;
        entity.FtdRateBase = source.FtdRateBase ?? 0m;
        entity.FtdRateWithGoal = source.FtdRateWithGoal ?? 0m;
        entity.FtdRateWithSuperGoal = source.FtdRateWithSuperGoal ?? 0m;
        entity.FtdSuperbetRate = source.FtdSuperbetRate ?? 0m;
        entity.FtdBonusEvery = source.FtdBonusEvery ?? 0;
        entity.FtdBonusValue = source.FtdBonusValue ?? 0m;
        entity.SalesPctBase = source.SalesPctBase ?? 0m;
        entity.SalesPctWithGoal = source.SalesPctWithGoal ?? 0m;
        entity.SalesPctWithSuperGoal = source.SalesPctWithSuperGoal ?? 0m;
        entity.SalesBonusEvery = source.SalesBonusEvery ?? 0m;
        entity.SalesBonusValue = source.SalesBonusValue ?? 0m;
        entity.RevPct = source.RevPct ?? 0m;
        entity.BetanoInternaValue = 0m;
        entity.BetanoMundoBetValue = 0m;
        entity.SupFtdSuperbetNoGoal = source.SupFtdSuperbetNoGoal ?? 0m;
        entity.SupFtdSuperbetWithGoal = source.SupFtdSuperbetWithGoal ?? 0m;
        entity.SupFtdOtherNoGoal = source.SupFtdOtherNoGoal ?? 0m;
        entity.SupFtdOtherWithGoal = source.SupFtdOtherWithGoal ?? 0m;
        entity.SupSalesPctNoGoal = source.SupSalesPctNoGoal ?? 0m;
        entity.SupSalesPctWithGoal = source.SupSalesPctWithGoal ?? 0m;
        entity.SupRevPct = source.SupRevPct ?? 0m;
        entity.NetRevenueFactor = source.NetRevenueFactor ?? 0m;
        entity.NetRevenuePctNoGoal = source.NetRevenuePctNoGoal ?? 0m;
        entity.NetRevenuePctWithGoal = source.NetRevenuePctWithGoal ?? 0m;
        entity.TrafficInvestmentCommissionPct = source.TrafficInvestmentCommissionPct ?? 0m;
        entity.TrafficCpaEsportiva = source.TrafficCpaEsportiva ?? 0m;
        entity.TrafficCpaStake = source.TrafficCpaStake ?? 0m;
        entity.TrafficCpaBetano = source.TrafficCpaBetano ?? 0m;
        entity.TrafficCpaBetMgm = source.TrafficCpaBetMgm ?? 0m;
        entity.TrafficCpaNovibet = source.TrafficCpaNovibet ?? 0m;
        entity.TrafficCpaBetFair = source.TrafficCpaBetFair ?? 0m;
        entity.TrafficCpaBlaze = source.TrafficCpaBlaze ?? 0m;
        entity.TrafficCpaSuperbet = source.TrafficCpaSuperbet ?? 0m;
        entity.TrafficCpaHiperbet = source.TrafficCpaHiperbet ?? 0m;
        entity.TrafficSupBonus = source.TrafficSupBonus ?? 0m;
        entity.TrafficSupCommissionPct = source.TrafficSupCommissionPct ?? 0m;
    }

    private static Dictionary<string, CalculationProfile> InferOrphanLevelProfiles(
        IReadOnlyList<CareerLevelSource> levels,
        IReadOnlyList<CollaboratorSource> collaborators,
        IReadOnlyDictionary<string, CalculationProfile> departmentProfiles)
    {
        var result = new Dictionary<string, CalculationProfile>(StringComparer.Ordinal);
        foreach (var level in levels.Where(x => x.DepartmentSourceKey is null))
        {
            var profiles = collaborators
                .Where(x => x.CareerLevelSourceKey == level.SourceKey)
                .Select(x => departmentProfiles[x.DepartmentSourceKey])
                .Distinct()
                .ToArray();
            if (profiles.Length == 1)
                result[level.SourceKey] = ResolveCareerProfile(level.Name, profiles[0]);
        }

        return result;
    }

    private static CalculationProfile ResolveDepartmentProfile(DepartmentSource source) =>
        source.CalculationType switch
        {
            "commission" => CalculationProfile.PaidTraffic,
            "fixed_commission" => CalculationProfile.CommercialAnalyst,
            "fixed_commission_super_goal" => CalculationProfile.ProjectLeader,
            "management" => CalculationProfile.Management,
            "fixed_commission_bonus" => CalculationProfile.FixedCommissionBonus,
            "fixed_bonus" when source.Name == "Tipster" => CalculationProfile.Tipster,
            "fixed_bonus" when source.Name is "Administrativo" or "I.A/Automação" or "Contingência" or "Suporte"
                => CalculationProfile.AllocatedFixed,
            "fixed_bonus" => CalculationProfile.FixedBonus,
            _ => throw new FormatException("tipo de cálculo não suportado.")
        };

    private static CalculationProfile ResolveCareerProfile(string name, CalculationProfile departmentProfile) =>
        departmentProfile == CalculationProfile.CommercialAnalyst
            && name.Contains("Supervisor", StringComparison.OrdinalIgnoreCase)
                ? CalculationProfile.CommercialSupervisor
                : departmentProfile;

    private static async Task<CsvDocument> ReadFileAsync(
        string directory,
        string fileName,
        IReadOnlyCollection<string> expectedHeaders,
        ICollection<LegacySeedFileReport> reports,
        CancellationToken cancellationToken)
    {
        var path = Path.Combine(directory, fileName);
        var hash = await ComputeHashAsync(path, cancellationToken);
        var document = await StrictCsvParser.ParseFileAsync(path, cancellationToken);
        ValidateHeaders(document.Headers, expectedHeaders);
        reports.Add(new LegacySeedFileReport(fileName, hash, document.Rows.Count));
        return document;
    }

    private static void ValidateHeaders(
        IReadOnlyCollection<string> actual,
        IReadOnlyCollection<string> expected)
    {
        if (actual.Count != expected.Count
            || actual.Except(expected, StringComparer.Ordinal).Any()
            || expected.Except(actual, StringComparer.Ordinal).Any())
            throw new FormatException("headers incompatíveis com o schema esperado.");
    }

    private static List<T> ParseRows<T>(
        string entity,
        CsvDocument document,
        Func<CsvRow, T> parser,
        ICollection<LegacySeedError> errors)
    {
        var result = new List<T>(document.Rows.Count);
        foreach (var row in document.Rows)
        {
            try
            {
                result.Add(parser(row));
            }
            catch (FormatException exception)
            {
                errors.Add(new LegacySeedError(entity, row.LineNumber, SanitizeException(exception)));
            }
        }

        return result;
    }

    private static DepartmentSource ParseDepartment(CsvRow row) =>
        new(
            row.LineNumber,
            RequiredKey(row["id"]),
            RequiredText(row["name"]),
            CalculationType(row["calculation_type"]),
            DecimalOrNull(row["low_revenue_bonus_pct"]),
            TextOrNull(row["description"]),
            DecimalOrNull(row["goal_bonus_percentage"]),
            DecimalOrNull(row["low_revenue_threshold"]));

    private static CareerLevelSource ParseCareerLevel(CsvRow row) =>
        new(
            row.LineNumber, RequiredKey(row["id"]), RequiredText(row["name"]),
            KeyOrNull(row["department_id"]),
            DecimalOrNull(row["base_salary"]),
            DecimalOrNull(row["commission_without_goal"]),
            DecimalOrNull(row["commission_with_goal"]),
            DecimalOrNull(row["commission_with_super_goal"]),
            DecimalOrNull(row["group_commission_per_percent"]),
            DecimalOrNull(row["group_commission_per_20_percent"]),
            DecimalOrNull(row["default_cpa_value"]),
            DecimalOrNull(row["goal_bonus_value"]),
            DecimalOrNull(row["ftd_rate_base"]),
            DecimalOrNull(row["ftd_rate_one_goal"]),
            DecimalOrNull(row["ftd_rate_both_goals"]),
            DecimalOrNull(row["ftd_superbet_rate"]),
            IntOrNull(row["ftd_bonus_every"]),
            DecimalOrNull(row["ftd_bonus_value"]),
            DecimalOrNull(row["sales_pct_base"]),
            DecimalOrNull(row["sales_pct_one_goal"]),
            DecimalOrNull(row["sales_pct_both_goals"]),
            DecimalOrNull(row["sales_bonus_every"]),
            DecimalOrNull(row["sales_bonus_value"]),
            DecimalOrNull(row["rev_commission_pct"]),
            DecimalOrNull(row["sup_ftd_superbet_no_goal"]),
            DecimalOrNull(row["sup_ftd_superbet_with_goal"]),
            DecimalOrNull(row["sup_ftd_others_no_goal"]),
            DecimalOrNull(row["sup_ftd_others_with_goal"]),
            DecimalOrNull(row["sup_sales_pct_no_goal"]),
            DecimalOrNull(row["sup_sales_pct_with_goal"]),
            DecimalOrNull(row["sup_rev_pct"]),
            DecimalOrNull(row["net_revenue_factor"]),
            DecimalOrNull(row["net_revenue_pct_no_goal"]),
            DecimalOrNull(row["net_revenue_pct_with_goal"]),
            DecimalOrNull(row["traffic_investment_commission_pct"]),
            DecimalOrNull(row["traffic_cpa_esportiva"]),
            DecimalOrNull(row["traffic_cpa_stake"]),
            DecimalOrNull(row["traffic_cpa_betano"]),
            DecimalOrNull(row["traffic_cpa_betmgm"]),
            DecimalOrNull(row["traffic_cpa_novibet"]),
            DecimalOrNull(row["traffic_cpa_betfair"]),
            DecimalOrNull(row["traffic_cpa_blaze"]),
            DecimalOrNull(row["traffic_cpa_superbet"]),
            DecimalOrNull(row["traffic_cpa_hiperbet"]),
            DecimalOrNull(row["traffic_sup_bonus"]),
            DecimalOrNull(row["traffic_sup_commission_pct"]));

    private static CollaboratorSource ParseCollaborator(CsvRow row) =>
        new(
            row.LineNumber, RequiredKey(row["id"]), RequiredText(row["name"]),
            RequiredKey(row["department_id"]), KeyOrNull(row["career_level_id"]),
            TextOrNull(row["job_title"]), DateOrNull(row["admission_date"]),
            DateOrNull(row["dismissal_date"]), TextOrNull(row["pix_key"]),
            DecimalOrNull(row["base_salary"]), TextOrNull(row["email"]),
            TextOrNull(row["photo_url"]), Bool(row["is_active"]));

    private static void ValidateUniqueKeys(
        string entity,
        IEnumerable<string> keys,
        ICollection<LegacySeedError> errors)
    {
        if (keys.GroupBy(x => x, StringComparer.Ordinal).Any(x => x.Count() > 1))
            errors.Add(new LegacySeedError(entity, null, "há chaves de origem duplicadas."));
    }

    private static string RequiredKey(string value) =>
        string.IsNullOrWhiteSpace(value) ? throw new FormatException("chave de origem obrigatória ausente.") : value.Trim();

    private static string? KeyOrNull(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string RequiredText(string value) =>
        TextOrNull(value) ?? throw new FormatException("campo obrigatório vazio.");

    private static string CalculationType(string value)
    {
        var parsed = RequiredText(value);
        return parsed is "commission"
            or "fixed_commission"
            or "fixed_commission_super_goal"
            or "management"
            or "fixed_commission_bonus"
            or "fixed_bonus"
                ? parsed
                : throw new FormatException("tipo de cálculo não suportado.");
    }

    private static string? TextOrNull(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static decimal? DecimalOrNull(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
            throw new FormatException("valor decimal inválido.");
        return parsed;
    }

    private static int? IntOrNull(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            throw new FormatException("valor inteiro inválido.");
        return parsed;
    }

    private static DateOnly? DateOrNull(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        if (!DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            throw new FormatException("data inválida.");
        return parsed;
    }

    private static bool Bool(string value)
    {
        if (!bool.TryParse(value, out var parsed))
            throw new FormatException("booleano inválido.");
        return parsed;
    }

    private static async Task<string> ComputeHashAsync(string path, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexStringLower(hash);
    }

    private static string SanitizeException(Exception exception) =>
        exception switch
        {
            CsvFormatException csv => $"CSV inválido na linha {csv.LineNumber}.",
            FileNotFoundException => "arquivo obrigatório não encontrado.",
            UnauthorizedAccessException => "acesso ao arquivo negado.",
            IOException => "falha ao ler arquivo.",
            _ => exception.Message
        };

    private sealed record DepartmentSource(
        int Row, string SourceKey, string Name, string CalculationType,
        decimal? LowRevenueBonusPct, string? Description,
        decimal? GoalBonusPercentage, decimal? LowRevenueThreshold);

    private sealed record CareerLevelSource(
        int Row, string SourceKey, string Name, string? DepartmentSourceKey,
        decimal? BaseSalary, decimal? CommissionWithoutGoalPct,
        decimal? CommissionWithGoalPct, decimal? CommissionWithSuperGoalPct,
        decimal? GroupCommissionPerPercent, decimal? GroupCommissionPer20Percent,
        decimal? DefaultCpaValue, decimal? GoalBonusValue, decimal? FtdRateBase,
        decimal? FtdRateWithGoal, decimal? FtdRateWithSuperGoal,
        decimal? FtdSuperbetRate, int? FtdBonusEvery, decimal? FtdBonusValue,
        decimal? SalesPctBase, decimal? SalesPctWithGoal,
        decimal? SalesPctWithSuperGoal, decimal? SalesBonusEvery,
        decimal? SalesBonusValue, decimal? RevPct,
        decimal? SupFtdSuperbetNoGoal, decimal? SupFtdSuperbetWithGoal,
        decimal? SupFtdOtherNoGoal, decimal? SupFtdOtherWithGoal,
        decimal? SupSalesPctNoGoal, decimal? SupSalesPctWithGoal,
        decimal? SupRevPct, decimal? NetRevenueFactor,
        decimal? NetRevenuePctNoGoal, decimal? NetRevenuePctWithGoal,
        decimal? TrafficInvestmentCommissionPct, decimal? TrafficCpaEsportiva,
        decimal? TrafficCpaStake, decimal? TrafficCpaBetano,
        decimal? TrafficCpaBetMgm, decimal? TrafficCpaNovibet,
        decimal? TrafficCpaBetFair, decimal? TrafficCpaBlaze,
        decimal? TrafficCpaSuperbet, decimal? TrafficCpaHiperbet,
        decimal? TrafficSupBonus, decimal? TrafficSupCommissionPct);

    private sealed record CollaboratorSource(
        int Row, string SourceKey, string Name, string DepartmentSourceKey,
        string? CareerLevelSourceKey, string? JobTitle, DateOnly? AdmissionDate,
        DateOnly? DismissalDate, string? PixKey, decimal? BaseSalary,
        string? Email, string? PhotoUrl, bool IsActive);
}
