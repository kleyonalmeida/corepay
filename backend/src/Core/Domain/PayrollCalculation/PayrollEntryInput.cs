namespace Core.Domain.PayrollCalculation;

/// <summary>
/// Snapshot de entrada da folha para cálculo de uma linha de colaborador.
/// </summary>
public sealed record PayrollEntryInput
{
    public int Month { get; init; }

    public int Year { get; init; }

    public Department? Department { get; init; }

    public CareerLevel? CareerLevel { get; init; }

    public Collaborator Collaborator { get; init; } = null!;

    /// <summary>Salário-base informado na entrada; sobrescreve o do nível quando presente.</summary>
    public decimal? FullBaseSalary { get; init; }

    public GoalTier GoalTier { get; init; }

    /// <summary>Salário travado pós-aprovação (Affiliates §6.8).</summary>
    public decimal? FinalSalary { get; init; }

    /// <summary>Registros de faturamento líquido (Gerência §6.4).</summary>
    public IReadOnlyList<ManagementRevenueEntryInput> ManagementRevenueEntries { get; init; } = [];

    /// <summary>Nível Sênior do setor de tráfego — snapshot da camada de aplicação (§6.3 CPA manager).</summary>
    public CareerLevel? TrafficSeniorLevel { get; init; }

    /// <summary>Projetos de comissão de tráfego: investimento + CPA por casa (§6.3).</summary>
    public IReadOnlyList<TrafficProjectEntryInput> TrafficProjectEntries { get; init; } = [];

    /// <summary>Rev da época de analista — valor único na entrada (Supervisor §6.2).</summary>
    public decimal SupervisorAnalystRevenue { get; init; }

    /// <summary>Projetos do supervisor comercial: FTD, vendas, Rev analistas (§6.2).</summary>
    public IReadOnlyList<SupervisorProjectEntryInput> SupervisorProjectEntries { get; init; } = [];

    /// <summary>Projetos do analista comercial: FTD, vendas, CPA, Rev (§6.1).</summary>
    public IReadOnlyList<CommercialAnalystProjectEntryInput> CommercialProjectEntries { get; init; } = [];

    /// <summary>Quantidade Betano Interna — conta para o mínimo (§6.1).</summary>
    public int BetanoInternaCount { get; init; }

    /// <summary>Quantidade Betano Mundo Bet — não conta para o mínimo (§6.1).</summary>
    public int BetanoMundoBetCount { get; init; }

    /// <summary>Faturamento por projeto (e % de grupo no Tipster).</summary>
    public IReadOnlyList<ProjectEntryInput> ProjectEntries { get; init; } = [];

    /// <summary>Rateio do fixo; se vazio, usa <see cref="ProjectEntries"/>.</summary>
    public IReadOnlyList<RateioProjectEntryInput> RateioProjectEntries { get; init; } = [];

    /// <summary>Flags de projeto para Feira, Lima Karttos etc. (sem lookup em banco).</summary>
    public IReadOnlyList<ProjectCalculationSnapshot> ProjectSnapshots { get; init; } = [];

    public IReadOnlyList<BonusEntryInput> BonusEntries { get; init; } = [];

    public IReadOnlyList<DeductionEntryInput> DeductionEntries { get; init; } = [];

    /// <summary>Projeto pagador quando comissão do projeto &lt; R$ 100 (Analista Comercial §6.1).</summary>
    public Guid? CommissionPayingProjectId { get; init; }

    /// <summary>Rateio de complemento/Betano/plataforma por % (soma deve ser 100).</summary>
    public IReadOnlyList<ComplementPayingProjectInput> ComplementPayingProjects { get; init; } = [];

    /// <summary>Mudanças de cargo na competência (REGRAS §5.2).</summary>
    public IReadOnlyList<RoleChangeEntryInput> RoleChanges { get; init; } = [];

    /// <summary>Recorte inclusivo do proporcional por sub-período (§5.2).</summary>
    public DateOnly? PeriodClipStart { get; init; }

    /// <summary>Recorte inclusivo do proporcional por sub-período (§5.2).</summary>
    public DateOnly? PeriodClipEnd { get; init; }
}

/// <summary>Metadados de projeto necessários ao motor §6.9.</summary>
public sealed record ProjectCalculationSnapshot(
    Guid ProjectId,
    bool ExcludesGoalBonus = false,
    bool IsDefaultAllocationTarget = false,
    bool ExcludesSupervisorFixedAllocation = false);

/// <summary>Alocação manual de custo por projeto (Gerência §6.4).</summary>
public sealed record ManagementProjectBreakdownInput(Guid ProjectId, decimal Amount);

/// <summary>Faturamento líquido informado pelo usuário (Gerência §6.4) + breakdown manual por projeto.</summary>
public sealed record ManagementRevenueEntryInput(
    decimal NetRevenue,
    IReadOnlyList<ManagementProjectBreakdownInput>? ProjectBreakdown = null)
{
    public IReadOnlyList<ManagementProjectBreakdownInput> ProjectBreakdown { get; init; } = ProjectBreakdown ?? [];
}

/// <summary>Projeto + percentual para rateio de complemento comercial (soma = 100).</summary>
public sealed record ComplementPayingProjectInput(Guid ProjectId, decimal Percentage);

/// <summary>Comissão de tráfego por projeto: valor investido + CPAs (§6.3).</summary>
public sealed record TrafficProjectEntryInput(
    Guid ProjectId,
    decimal InvestedAmount,
    IReadOnlyList<TrafficCpaEntryInput>? CpaEntries = null)
{
    public IReadOnlyList<TrafficCpaEntryInput> CpaEntries { get; init; } = CpaEntries ?? [];
}

/// <summary>CPA por casa de apostas (legado: betting_house + cpa_count → uma entrada).</summary>
public sealed record TrafficCpaEntryInput(
    string HouseKey,
    TrafficCpaKind Kind,
    int Count);

/// <summary>Entrada por projeto do analista comercial (§6.1).</summary>
public sealed record CommercialAnalystProjectEntryInput(
    Guid ProjectId,
    ProjectPlatform Platform,
    int FtdTotal,
    int FtdSuperbet,
    bool IsFtdGoalReached,
    bool IsProjectFtdGoalReached,
    int CpaCount,
    decimal SalesAmount,
    bool IsSalesGoalReached,
    bool IsProjectSalesGoalReached,
    decimal Rev);

/// <summary>Entrada por projeto do supervisor comercial (§6.2).</summary>
public sealed record SupervisorProjectEntryInput(
    Guid ProjectId,
    int FtdTotal,
    int FtdSuperbet,
    decimal SalesAmount,
    decimal AnalystRev,
    bool IsProjectFtdGoalReached,
    bool IsProjectSalesGoalReached,
    decimal DeviceRecharge = 0m,
    decimal BonusCpa = 0m);

public sealed record ProjectEntryInput(
    Guid ProjectId,
    decimal Value,
    decimal GroupPercentage = 0m);

public sealed record RateioProjectEntryInput(
    Guid ProjectId,
    decimal? RateioValue = null);

public sealed record BonusEntryInput(Guid? ProjectId, decimal Value, string? Justification = null);

public sealed record DeductionEntryInput(decimal Value, string? Description = null);
