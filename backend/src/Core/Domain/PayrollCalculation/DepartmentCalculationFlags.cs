namespace Core.Domain.PayrollCalculation;

/// <summary>
/// Semântica explícita de setores especiais para o motor (substitui regex §11).
/// </summary>
public static class DepartmentCalculationFlags
{
    public static bool IsAllocatedFixed(Department department)
    {
        ArgumentNullException.ThrowIfNull(department);
        return department.IsAllocatedFixed;
    }

    public static bool RoutesFixedToLimaKarttos(Department department)
    {
        ArgumentNullException.ThrowIfNull(department);
        return department.RoutesFixedToLimaKarttos;
    }

    public static bool IsAffiliatesDepartment(Department department)
    {
        ArgumentNullException.ThrowIfNull(department);
        return department.CalculationType == CalculationProfile.FixedCommissionBonus;
    }
}
