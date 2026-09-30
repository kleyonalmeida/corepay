using Core.Domain;
using Core.Domain.PayrollCalculation;
using FluentAssertions;

namespace Core.Tests.PayrollCalculation;

public class ProjectAndDepartmentFlagsTests
{
    private static readonly Guid LimaKarttosId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid FeiraId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid ThreeCSportsId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid AffiliatesId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

    [Fact]
    public void ProjectFlags_ShouldIdentifySpecialProjectsByIdAndFlags_NotByName()
    {
        var limaKarttos = new Project
        {
            Id = LimaKarttosId,
            Name = "Nome Qualquer Lima",
            IsDefaultAllocationTarget = true
        };
        var feira = new Project
        {
            Id = FeiraId,
            Name = "Evento X Renomeado",
            ExcludesGoalBonus = true
        };
        var threeCSports = new Project
        {
            Id = ThreeCSportsId,
            Name = "Esporte 3C Renomeado",
            ExcludesSupervisorFixedAllocation = true
        };
        var affiliates = new Project
        {
            Id = AffiliatesId,
            Name = "Parceiros Renomeado"
        };

        ProjectCalculationFlags.IsDefaultAllocationTarget(limaKarttos).Should().BeTrue();
        ProjectCalculationFlags.IsLimaKarttosProject(limaKarttos, LimaKarttosId).Should().BeTrue();
        ProjectCalculationFlags.ExcludesGoalBonus(feira).Should().BeTrue();
        ProjectCalculationFlags.IsFeiraProject(feira, FeiraId).Should().BeTrue();
        ProjectCalculationFlags.ExcludesSupervisorFixedAllocation(threeCSports).Should().BeTrue();
        ProjectCalculationFlags.IsThreeCSportsProject(threeCSports, ThreeCSportsId).Should().BeTrue();
        ProjectCalculationFlags.IsAffiliatesProject(affiliates, AffiliatesId).Should().BeTrue();
    }

    [Fact]
    public void ProjectFlags_ShouldNotMatchSpecialProject_WhenIdDiffers()
    {
        var otherProject = new Project
        {
            Id = Guid.NewGuid(),
            Name = "Lima Karttos",
            IsDefaultAllocationTarget = false,
            ExcludesGoalBonus = false
        };

        ProjectCalculationFlags.IsLimaKarttosProject(otherProject, LimaKarttosId).Should().BeFalse();
        ProjectCalculationFlags.IsFeiraProject(otherProject, FeiraId).Should().BeFalse();
        ProjectCalculationFlags.IsAffiliatesProject(otherProject, AffiliatesId).Should().BeFalse();
    }

    [Theory]
    [InlineData(CalculationProfile.Tipster, false, false)]
    [InlineData(CalculationProfile.AllocatedFixed, true, false)]
    [InlineData(CalculationProfile.AllocatedFixed, true, true)]
    [InlineData(CalculationProfile.FixedCommissionBonus, false, false)]
    public void DepartmentFlags_ShouldUseExplicitCalculationTypeAndFlags(
        CalculationProfile profile,
        bool isAllocatedFixed,
        bool routesToLimaKarttos)
    {
        var department = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Setor Renomeado",
            CalculationType = profile,
            IsAllocatedFixed = isAllocatedFixed,
            RoutesFixedToLimaKarttos = routesToLimaKarttos
        };

        DepartmentCalculationFlags.IsAllocatedFixed(department).Should().Be(isAllocatedFixed);
        DepartmentCalculationFlags.RoutesFixedToLimaKarttos(department).Should().Be(routesToLimaKarttos);
        DepartmentCalculationFlags.IsAffiliatesDepartment(department)
            .Should().Be(profile == CalculationProfile.FixedCommissionBonus);
    }
}
