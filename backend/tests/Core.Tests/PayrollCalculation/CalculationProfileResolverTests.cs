using Core.Domain;
using Core.Domain.PayrollCalculation;
using FluentAssertions;

namespace Core.Tests.PayrollCalculation;

public class CalculationProfileResolverTests
{
    [Fact]
    public void Resolve_ShouldUseCollaboratorOverride_WhenPresent()
    {
        var department = CreateDepartment(CalculationProfile.CommercialAnalyst);
        var level = CreateLevel(CalculationProfile.CommercialSupervisor);
        var collaborator = CreateCollaborator(CalculationProfileOverride: CalculationProfile.PaidTraffic);

        CalculationProfileResolver.Resolve(collaborator, department, level)
            .Should().Be(CalculationProfile.PaidTraffic);
    }

    [Fact]
    public void Resolve_ShouldUseCareerLevelProfile_WhenNoOverride()
    {
        var department = CreateDepartment(CalculationProfile.CommercialAnalyst);
        var level = CreateLevel(CalculationProfile.CommercialSupervisor);
        var collaborator = CreateCollaborator();

        CalculationProfileResolver.Resolve(collaborator, department, level)
            .Should().Be(CalculationProfile.CommercialSupervisor);
    }

    [Fact]
    public void Resolve_ShouldUseDepartmentCalculationType_WhenNoLevelAndNoOverride()
    {
        var department = CreateDepartment(CalculationProfile.Management);
        var collaborator = CreateCollaborator();

        CalculationProfileResolver.Resolve(collaborator, department, careerLevel: null)
            .Should().Be(CalculationProfile.Management);
    }

    [Theory]
    [InlineData(CalculationProfile.CommercialAnalyst)]
    [InlineData(CalculationProfile.CommercialSupervisor)]
    [InlineData(CalculationProfile.PaidTraffic)]
    [InlineData(CalculationProfile.Management)]
    [InlineData(CalculationProfile.ProjectLeader)]
    [InlineData(CalculationProfile.Tipster)]
    [InlineData(CalculationProfile.AllocatedFixed)]
    public void Resolve_ShouldReturnExplicitDepartmentProfile_ForFixtureProfiles(CalculationProfile profile)
    {
        var department = CreateDepartment(profile);
        department.Name = "Nome Renomeado Sem Regex";
        var collaborator = CreateCollaborator();

        CalculationProfileResolver.Resolve(collaborator, department, careerLevel: null)
            .Should().Be(profile);
    }

    [Fact]
    public void Resolve_ShouldIgnoreRenamedDepartmentName()
    {
        var department = CreateDepartment(CalculationProfile.PaidTraffic);
        department.Name = "Setor Renomeado Sem Regex";
        var level = CreateLevel(CalculationProfile.PaidTraffic);
        level.Name = "Cargo Renomeado";
        var collaborator = CreateCollaborator();
        collaborator.Name = "Colaborador Renomeado";

        CalculationProfileResolver.Resolve(collaborator, department, level)
            .Should().Be(CalculationProfile.PaidTraffic);
    }

    [Fact]
    public void Resolve_ShouldPreferOverrideOverRenamedLevelAndDepartment()
    {
        var department = CreateDepartment(CalculationProfile.CommercialAnalyst);
        department.Name = "Analistas Comerciais Renomeado";
        var level = CreateLevel(CalculationProfile.CommercialAnalyst);
        level.Name = "Analista Comercial Júnior Renomeado";
        var collaborator = CreateCollaborator(CalculationProfileOverride: CalculationProfile.Tipster);

        CalculationProfileResolver.Resolve(collaborator, department, level)
            .Should().Be(CalculationProfile.Tipster);
    }

    private static Department CreateDepartment(CalculationProfile profile) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = "Setor Teste",
            CalculationType = profile
        };

    private static CareerLevel CreateLevel(CalculationProfile profile) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = "Nível Teste",
            Profile = profile
        };

    private static Collaborator CreateCollaborator(CalculationProfile? CalculationProfileOverride = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = "Colaborador Teste",
            DepartmentId = Guid.NewGuid(),
            CalculationProfileOverride = CalculationProfileOverride
        };
}
