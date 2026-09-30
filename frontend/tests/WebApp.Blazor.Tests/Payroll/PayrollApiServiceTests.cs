using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Blazor.Services;
using WebApp.Blazor.Tests.Auth;

namespace WebApp.Blazor.Tests.Payroll;

public class PayrollApiServiceTests : BlazorComponentTestContext
{
    private readonly StubHttpMessageHandler _httpHandler;

    public PayrollApiServiceTests()
    {
        _httpHandler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        Services.AddScoped(_ => new HttpClient(_httpHandler)
        {
            BaseAddress = new Uri("http://localhost:5000")
        });
        Services.AddScoped<IPayrollApiService, PayrollApiService>();
    }

    [Fact]
    public async Task GetPayrollsAsync_WrappedPayload_ReturnsPayrolls()
    {
        _httpHandler.Configure(request =>
        {
            request.Method.Should().Be(HttpMethod.Get);
            request.RequestUri!.AbsolutePath.Should().Be("/api/v1/payrolls");
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(CreatePayrollListJson())
            };
        });

        var service = Services.GetRequiredService<IPayrollApiService>();
        var result = await service.GetPayrollsAsync();

        result.Status.Should().Be(PayrollApiStatus.Success);
        result.Payrolls.Should().ContainSingle();
        result.Payrolls![0].DepartmentName.Should().Be("Analistas Comerciais");
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetPayrollsAsync_LegacyArrayPayload_ReturnsPayrolls()
    {
        _httpHandler.Configure(request =>
        {
            request.Method.Should().Be(HttpMethod.Get);
            request.RequestUri!.AbsolutePath.Should().Be("/api/v1/payrolls");
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent($"[{CreatePayrollListItemJson()}]")
            };
        });

        var service = Services.GetRequiredService<IPayrollApiService>();
        var result = await service.GetPayrollsAsync();

        result.Status.Should().Be(PayrollApiStatus.Success);
        result.Payrolls.Should().ContainSingle();
        result.TotalCount.Should().Be(1);
        result.Page.Should().Be(1);
    }

    [Fact]
    public async Task CreatePayrollAsync_ShouldDeserializeDetailResponse()
    {
        var payrollId = Guid.NewGuid();
        _httpHandler.Configure(request =>
        {
            request.Method.Should().Be(HttpMethod.Post);
            request.RequestUri!.AbsolutePath.Should().Be("/api/v1/payrolls");
            return new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent(CreateDetailJson(payrollId))
            };
        });

        var service = Services.GetRequiredService<IPayrollApiService>();
        var result = await service.CreatePayrollAsync(new CreatePayrollRequestDto(
            Guid.NewGuid(),
            3,
            2026,
            []));

        result.Status.Should().Be(PayrollApiStatus.Success);
        result.Payroll!.Id.Should().Be(payrollId);
        result.Payroll.Entries.Should().ContainSingle();
    }

    [Fact]
    public async Task CreatePayrollAsync_WhenDuplicateCompetence_ShouldReturnConflict()
    {
        _httpHandler.Configure(_ => new HttpResponseMessage(HttpStatusCode.Conflict)
        {
            Content = new StringContent("""{"error":"payrolls.competence_duplicate","message":"duplicate"}""")
        });

        var service = Services.GetRequiredService<IPayrollApiService>();
        var result = await service.CreatePayrollAsync(new CreatePayrollRequestDto(
            Guid.NewGuid(),
            3,
            2026,
            []));

        result.Status.Should().Be(PayrollApiStatus.Conflict);
        result.ErrorCode.Should().Be("payrolls.competence_duplicate");
    }

    [Fact]
    public async Task GetFormOptionsAsync_ShouldDeserializeDepartments()
    {
        var departmentId = Guid.NewGuid();
        _httpHandler.Configure(request =>
        {
            request.RequestUri!.AbsolutePath.Should().Be("/api/v1/payrolls/form-options");
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(new
                {
                    departments = new[]
                    {
                        new { id = departmentId, name = "Analistas Comerciais" }
                    }
                }))
            };
        });

        var service = Services.GetRequiredService<IPayrollApiService>();
        var result = await service.GetFormOptionsAsync();

        result.Status.Should().Be(PayrollApiStatus.Success);
        result.Options!.Departments.Should().ContainSingle();
        result.Options.Departments[0].Id.Should().Be(departmentId);
    }

    [Fact]
    public async Task UpdatePayrollAsync_WithEntries_ShouldDeserializeDetailResponse()
    {
        var payrollId = Guid.NewGuid();
        var entryId = Guid.NewGuid();
        _httpHandler.Configure(request =>
        {
            request.Method.Should().Be(HttpMethod.Put);
            request.RequestUri!.AbsolutePath.Should().Be($"/api/v1/payrolls/{payrollId}");
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(CreateDetailJson(payrollId, entryId, totalAmount: 4300m))
            };
        });

        var service = Services.GetRequiredService<IPayrollApiService>();
        var result = await service.UpdatePayrollAsync(
            payrollId,
            new UpdatePayrollRequestDto(
                [Guid.NewGuid()],
                [new UpdatePayrollEntryRequestDto(entryId)]));

        result.Status.Should().Be(PayrollApiStatus.Success);
        result.Payroll!.TotalAmount.Should().Be(4300m);
    }

    [Fact]
    public async Task SubmitPayrollAsync_ShouldReturnPendingApproval()
    {
        var payrollId = Guid.NewGuid();
        _httpHandler.Configure(request =>
        {
            request.Method.Should().Be(HttpMethod.Post);
            request.RequestUri!.AbsolutePath.Should().Be($"/api/v1/payrolls/{payrollId}/submit");
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(CreateDetailJson(payrollId, status: "pendingApproval", submittedBy: "Test Manager"))
            };
        });

        var service = Services.GetRequiredService<IPayrollApiService>();
        var result = await service.SubmitPayrollAsync(payrollId);

        result.Status.Should().Be(PayrollApiStatus.Success);
        result.Payroll!.Status.Should().Be("pendingApproval");
        result.Payroll.SubmittedBy.Should().Be("Test Manager");
    }

    [Fact]
    public async Task ApprovePayrollAsync_ShouldDeserializeApprovedResponse()
    {
        var payrollId = Guid.NewGuid();
        _httpHandler.Configure(request =>
        {
            request.Method.Should().Be(HttpMethod.Post);
            request.RequestUri!.AbsolutePath.Should().Be($"/api/v1/payrolls/{payrollId}/approve");
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(CreateDetailJson(payrollId, status: "approved", approvedBy: "Director Test"))
            };
        });

        var service = Services.GetRequiredService<IPayrollApiService>();
        var result = await service.ApprovePayrollAsync(payrollId);

        result.Status.Should().Be(PayrollApiStatus.Success);
        result.Payroll!.Status.Should().Be("approved");
        result.Payroll.ApprovedBy.Should().Be("Director Test");
    }

    [Fact]
    public async Task SubmitPayrollAsync_WhenStatusNotEditable_ShouldReturnConflict()
    {
        var payrollId = Guid.NewGuid();
        _httpHandler.Configure(_ => new HttpResponseMessage(HttpStatusCode.Conflict)
        {
            Content = new StringContent("""{"error":"payrolls.status_not_editable","message":"conflict"}""")
        });

        var service = Services.GetRequiredService<IPayrollApiService>();
        var result = await service.SubmitPayrollAsync(payrollId);

        result.Status.Should().Be(PayrollApiStatus.Conflict);
        result.ErrorCode.Should().Be("payrolls.status_not_editable");
    }

    [Fact]
    public async Task AddCollaboratorEntryAsync_ShouldPostAndDeserializeDetail()
    {
        var payrollId = Guid.NewGuid();
        var collaboratorId = Guid.NewGuid();
        _httpHandler.Configure(request =>
        {
            request.Method.Should().Be(HttpMethod.Post);
            request.RequestUri!.AbsolutePath.Should().Be($"/api/v1/payrolls/{payrollId}/entries");
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(CreateDetailJson(payrollId))
            };
        });

        var service = Services.GetRequiredService<IPayrollApiService>();
        var result = await service.AddCollaboratorEntryAsync(
            payrollId,
            new AddCollaboratorEntryRequestDto(collaboratorId));

        result.Status.Should().Be(PayrollApiStatus.Success);
        result.Payroll!.Id.Should().Be(payrollId);
    }

    [Fact]
    public async Task AddCollaboratorEntryAsync_WhenDuplicate_ShouldReturnConflict()
    {
        var payrollId = Guid.NewGuid();
        _httpHandler.Configure(_ => new HttpResponseMessage(HttpStatusCode.Conflict)
        {
            Content = new StringContent("""{"error":"payrolls.collaborator_already_in_payroll","message":"duplicate"}""")
        });

        var service = Services.GetRequiredService<IPayrollApiService>();
        var result = await service.AddCollaboratorEntryAsync(
            payrollId,
            new AddCollaboratorEntryRequestDto(Guid.NewGuid()));

        result.Status.Should().Be(PayrollApiStatus.Conflict);
        result.ErrorCode.Should().Be("payrolls.collaborator_already_in_payroll");
    }

    private static string CreatePayrollListJson() =>
        JsonSerializer.Serialize(new
        {
            items = new[] { CreatePayrollListItemPayload() },
            totalCount = 1,
            page = 1,
            pageSize = 30
        });

    private static string CreatePayrollListItemJson() =>
        JsonSerializer.Serialize(CreatePayrollListItemPayload());

    private static object CreatePayrollListItemPayload() =>
        new
        {
            id = Guid.NewGuid(),
            departmentId = Guid.NewGuid(),
            departmentName = "Analistas Comerciais",
            month = 3,
            year = 2026,
            status = "draft",
            totalAmount = 3500m,
            entryCount = 1,
            submittedBy = (string?)null
        };

    private static string CreateDetailJson(
        Guid payrollId,
        Guid? entryId = null,
        decimal totalAmount = 0m,
        string status = "draft",
        string? submittedBy = null,
        string? approvedBy = null) =>
        JsonSerializer.Serialize(new
        {
            id = payrollId,
            departmentId = Guid.NewGuid(),
            departmentName = "Analistas Comerciais",
            month = 3,
            year = 2026,
            status,
            totalAmount,
            entryCount = 1,
            submittedBy,
            rejectionComment = (string?)null,
            approvedBy,
            approvedAt = approvedBy is null ? (DateTimeOffset?)null : DateTimeOffset.Parse("2026-03-10T12:00:00Z"),
            entries = new[]
            {
                new
                {
                    id = entryId ?? Guid.NewGuid(),
                    collaboratorId = Guid.NewGuid(),
                    collaboratorName = "Ana Comercial",
                    careerLevelName = "Analista Comercial Júnior",
                    pixKey = "11999990001",
                    admissionDate = "2024-03-01",
                    fullBaseSalary = 3500m,
                    calculationProfile = "commercialAnalyst",
                    isApproved = false
                }
            }
        });
}
