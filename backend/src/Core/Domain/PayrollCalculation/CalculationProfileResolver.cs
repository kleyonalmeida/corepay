namespace Core.Domain.PayrollCalculation;

/// <summary>
/// Resolve o perfil efetivo de cálculo sem inspecionar nomes de setor/cargo (paridade §11 legado).
/// Precedência: override do colaborador → perfil do nível → perfil do setor.
/// </summary>
public static class CalculationProfileResolver
{
    public static CalculationProfile Resolve(
        Collaborator collaborator,
        Department department,
        CareerLevel? careerLevel)
    {
        ArgumentNullException.ThrowIfNull(collaborator);
        ArgumentNullException.ThrowIfNull(department);

        if (collaborator.CalculationProfileOverride.HasValue)
        {
            return collaborator.CalculationProfileOverride.Value;
        }

        if (careerLevel is not null)
        {
            return careerLevel.Profile;
        }

        return department.CalculationType;
    }
}
