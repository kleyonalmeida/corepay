namespace WebAPI.Tests.Collaborators;

internal static class CollaboratorTestHelper
{
    public static object CreateCollaboratorPayload(
        string name = "Colaborador Teste",
        Guid? departmentId = null,
        Guid? careerLevelId = null,
        string? jobTitle = "Cargo Teste",
        string? admissionDate = "2024-01-15",
        string? dismissalDate = null,
        string? pixKey = "11999990099",
        decimal? baseSalary = 3000m,
        string? email = "colaborador.teste@corepay.test",
        string? photoUrl = null,
        bool isActive = true,
        string? calculationProfileOverride = null) =>
        new
        {
            name,
            departmentId,
            careerLevelId,
            jobTitle,
            admissionDate,
            dismissalDate,
            pixKey,
            baseSalary,
            email,
            photoUrl,
            isActive,
            calculationProfileOverride
        };
}
