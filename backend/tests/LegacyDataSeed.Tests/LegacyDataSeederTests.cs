using Core.Domain;
using FluentAssertions;
using Infrastructure;
using Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace LegacyDataSeed.Tests;

public sealed class LegacyDataSeederTests
{
    [Fact]
    public void DeterministicGuid_IsStableTypedAndRfc4122()
    {
        var first = DeterministicGuid.Create("department", "source-key");
        var second = DeterministicGuid.Create("department", "source-key");
        var otherType = DeterministicGuid.Create("collaborator", "source-key");

        first.Should().Be(second);
        first.Should().NotBe(otherType);
        var canonicalBytes = first.ToByteArray(bigEndian: true);
        (canonicalBytes[6] >> 4).Should().Be(5);
        (canonicalBytes[8] & 0xc0).Should().Be(0x80);
    }

    [Fact]
    public void StrictCsvParser_HandlesQuotedFieldsAndEmbeddedNewlines()
    {
        var document = StrictCsvParser.Parse(
            "\"name\",\"description\"\r\n\"A, B\",\"linha 1\r\nlinha \"\"2\"\"\"\r\n");

        document.Rows.Should().ContainSingle();
        document.Rows[0]["name"].Should().Be("A, B");
        document.Rows[0]["description"].Should().Be("linha 1\r\nlinha \"2\"");
    }

    [Theory]
    [InlineData("\"a\",\"b\"\n\"x\"oops,\"y\"")]
    [InlineData("\"a\",\"b\"\n\"x\",\"y")]
    [InlineData("\"a\",\"b\"\n\"x\"")]
    public void StrictCsvParser_RejectsMalformedCsv(string csv)
    {
        var action = () => StrictCsvParser.Parse(csv);
        action.Should().Throw<CsvFormatException>();
    }

    [Fact]
    public async Task Seed_CreatesProductionDataAndIsIdempotent()
    {
        var root = FindWorkspaceRoot();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        await using var db = new AppDbContext(options);
        var seeder = new LegacyDataSeeder(db);

        var first = await seeder.SeedAsync(root);
        var second = await seeder.SeedAsync(root);

        first.Succeeded.Should().BeTrue();
        first.Entities["departments"].Created.Should().Be(16);
        first.Entities["careerLevels"].Created.Should().Be(73);
        first.Entities["collaborators"].Created.Should().Be(194);
        second.Entities.Values.Should().OnlyContain(x => x.Created == 0);
        second.Entities.Values.Should().OnlyContain(x => x.Existing > 0);
        (await db.Departments.CountAsync()).Should().Be(16);
        (await db.CareerLevels.CountAsync()).Should().Be(73);
        (await db.Collaborators.CountAsync()).Should().Be(194);
        first.PendingModifications.Should().Contain(x =>
            x.Field == nameof(Collaborator.AdmissionDate)
            && x.Reason == "Data de admissão não informada.");
        first.PendingModifications.Count(x =>
                x.Field == nameof(Collaborator.DismissalDate)
                && x.Reason == "Data de desligamento anterior à admissão.")
            .Should().Be(8);
        first.PendingModifications.Count(x =>
                x.Field == nameof(Collaborator.DismissalDate)
                && x.Reason == "Colaborador inativo sem data de desligamento.")
            .Should().Be(8);
        second.PendingModifications.Should().BeEquivalentTo(first.PendingModifications);
        (await db.CareerLevels.CountAsync(x => x.BetanoInternaValue == 0m)).Should().Be(73);
        var junior = await db.CareerLevels.SingleAsync(x => x.Name == "Analista junior 2");
        junior.CommissionWithGoalPct.Should().Be(0m);
        junior.TrafficCpaBetano.Should().Be(0m);

        (await db.Departments.SingleAsync(x => x.Name == "Comercial"))
            .CalculationType.Should().Be(CalculationProfile.CommercialAnalyst);
        (await db.Departments.SingleAsync(x => x.Name == "Tráfego Pago"))
            .CalculationType.Should().Be(CalculationProfile.PaidTraffic);
        (await db.CareerLevels.SingleAsync(x => x.Name == "Supervisor Analista Comercial"))
            .Profile.Should().Be(CalculationProfile.CommercialSupervisor);
        (await db.CareerLevels.SingleAsync(x => x.Name == "Gestor de tráfego - Senior"))
            .TrafficCpaBetano.Should().NotBe(0);
        (await db.Collaborators.CountAsync(x => x.PhotoUrl != null)).Should().BeGreaterThan(0);
    }

    [Fact]
    public void BusinessEntities_ShouldNotPersistBase44Identifiers()
    {
        foreach (var type in new[] { typeof(Department), typeof(CareerLevel), typeof(Collaborator) })
        {
            type.GetProperties()
                .Select(property => property.Name)
                .Should().NotContain(name =>
                    name.Contains("Legacy", StringComparison.OrdinalIgnoreCase)
                    || name.Contains("Base44", StringComparison.OrdinalIgnoreCase));
        }
    }

    private static string FindWorkspaceRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Department_export.csv")))
                return directory.FullName;
            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Raiz do workspace não encontrada.");
    }
}
