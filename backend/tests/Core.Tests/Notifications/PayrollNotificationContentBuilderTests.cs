using Core.Application.Notifications;
using Core.Domain;
using FluentAssertions;

namespace Core.Tests.Notifications;

public class PayrollNotificationContentBuilderTests
{
    [Fact]
    public void BuildSubmitted_ShouldIncludeDepartmentAndCompetence()
    {
        var payroll = CreatePayroll();

        var (title, message) = PayrollNotificationContentBuilder.BuildSubmitted(payroll);

        title.Should().Be("Folha submetida");
        message.Should().Be("Analistas Comerciais — 03/2026 aguardando aprovação.");
    }

    [Fact]
    public void BuildApproved_ShouldIncludeDepartmentAndCompetence()
    {
        var payroll = CreatePayroll();

        var (title, message) = PayrollNotificationContentBuilder.BuildApproved(payroll);

        title.Should().Be("Folha aprovada");
        message.Should().Be("Analistas Comerciais — 03/2026 foi aprovada.");
    }

    [Fact]
    public void BuildRejected_ShouldIncludeRejectionComment()
    {
        var payroll = CreatePayroll();
        payroll.RejectionComment = "Ajustar FTD";

        var (title, message) = PayrollNotificationContentBuilder.BuildRejected(payroll);

        title.Should().Be("Folha reprovada");
        message.Should().Be("Analistas Comerciais — 03/2026: Ajustar FTD");
    }

    private static Core.Domain.Payroll CreatePayroll() =>
        new()
        {
            Id = Guid.NewGuid(),
            Department = new Department { Name = "Analistas Comerciais" },
            Month = 3,
            Year = 2026
        };
}
