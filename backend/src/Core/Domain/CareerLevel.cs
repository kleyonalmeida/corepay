namespace Core.Domain;

public sealed class CareerLevel
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public Guid? DepartmentId { get; set; }

    public Department? Department { get; set; }

    public CalculationProfile Profile { get; set; }

    public bool IsActive { get; set; } = true;

    // Comum
    public decimal BaseSalary { get; set; }

    public decimal CommissionWithoutGoalPct { get; set; }

    public decimal CommissionWithGoalPct { get; set; }

    public decimal CommissionWithSuperGoalPct { get; set; }

    public decimal GroupCommissionPerPercent { get; set; }

    public decimal GroupCommissionPer20Percent { get; set; }

    public decimal DefaultCpaValue { get; set; }

    public decimal GoalBonusValue { get; set; }

    // Analista comercial
    public decimal FtdRateBase { get; set; }

    public decimal FtdRateWithGoal { get; set; }

    public decimal FtdRateWithSuperGoal { get; set; }

    public decimal FtdSuperbetRate { get; set; }

    public int FtdBonusEvery { get; set; }

    public decimal FtdBonusValue { get; set; }

    public decimal SalesPctBase { get; set; }

    public decimal SalesPctWithGoal { get; set; }

    public decimal SalesPctWithSuperGoal { get; set; }

    public decimal SalesBonusEvery { get; set; }

    public decimal SalesBonusValue { get; set; }

    public decimal RevPct { get; set; }

    public decimal BetanoInternaValue { get; set; }

    public decimal BetanoMundoBetValue { get; set; }

    // Supervisor
    public decimal SupFtdSuperbetNoGoal { get; set; }

    public decimal SupFtdSuperbetWithGoal { get; set; }

    public decimal SupFtdOtherNoGoal { get; set; }

    public decimal SupFtdOtherWithGoal { get; set; }

    public decimal SupSalesPctNoGoal { get; set; }

    public decimal SupSalesPctWithGoal { get; set; }

    public decimal SupRevPct { get; set; }

    // Gerência
    public decimal NetRevenueFactor { get; set; }

    public decimal NetRevenuePctNoGoal { get; set; }

    public decimal NetRevenuePctWithGoal { get; set; }

    // Tráfego pago
    public decimal TrafficInvestmentCommissionPct { get; set; }

    public decimal TrafficCpaEsportiva { get; set; }

    public decimal TrafficCpaStake { get; set; }

    public decimal TrafficCpaBetano { get; set; }

    public decimal TrafficCpaBetMgm { get; set; }

    public decimal TrafficCpaNovibet { get; set; }

    public decimal TrafficCpaBetFair { get; set; }

    public decimal TrafficCpaBlaze { get; set; }

    public decimal TrafficCpaSuperbet { get; set; }

    public decimal TrafficCpaHiperbet { get; set; }

    /// <summary>Cadastrado para paridade; não entra no cálculo automático (§13.1).</summary>
    public decimal TrafficSupBonus { get; set; }

    /// <summary>Cadastrado para paridade; não entra no cálculo automático (§13.1).</summary>
    public decimal TrafficSupCommissionPct { get; set; }
}
