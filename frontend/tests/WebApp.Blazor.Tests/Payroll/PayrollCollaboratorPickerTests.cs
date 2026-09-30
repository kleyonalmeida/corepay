using System.Net;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Blazor.Components.Payroll;
using WebApp.Blazor.Services;
using WebApp.Blazor.Tests.Auth;
using WebApp.Blazor.Tests.Common;

namespace WebApp.Blazor.Tests.Payroll;

public class PayrollCollaboratorPickerTests : BlazorComponentTestContext
{
    private readonly StubHttpMessageHandler _httpHandler;
    private static readonly Guid DepartmentId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public PayrollCollaboratorPickerTests()
    {
        _httpHandler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        Services.AddScoped(_ => new HttpClient(_httpHandler)
        {
            BaseAddress = new Uri("http://localhost:5000")
        });
        Services.AddScoped<ICollaboratorApiService, CollaboratorApiService>();
    }

    [Fact]
    public async Task Picker_NoEligibleCollaborators_ShowsEmptyState()
    {
        ConfigureCollaborators(CollaboratorTestJson.WrapList([]));

        var cut = Render<PayrollCollaboratorPicker>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.DepartmentId, DepartmentId));

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Nenhum colaborador elegível");
        });
    }

    [Fact]
    public async Task Picker_SearchWithoutResults_ShowsNoResultsEmptyState()
    {
        ConfigureCollaborators(CollaboratorTestJson.WrapList(
        [
            new
            {
                id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                name = "Ana Comercial",
                departmentId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                departmentName = "Comercial",
                careerLevelName = "Júnior",
                isActive = true
            }
        ]));

        var cut = Render<PayrollCollaboratorPicker>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.DepartmentId, DepartmentId));

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Ana Comercial"));
        cut.Find("input[placeholder='Nome do colaborador']").Input("Inexistente");

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Nenhum resultado");
            cut.Markup.Should().Contain("Nenhum colaborador encontrado para a busca informada.");
        });
    }

    [Fact]
    public async Task Picker_LoadError_ShowsErrorMessage()
    {
        _httpHandler.Configure(request =>
        {
            if (request.Method == HttpMethod.Get
                && request.RequestUri!.AbsolutePath == "/api/v1/collaborators")
            {
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        var cut = Render<PayrollCollaboratorPicker>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.DepartmentId, DepartmentId));

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Não foi possível carregar os colaboradores.");
        });
    }

    private void ConfigureCollaborators(string body)
    {
        _httpHandler.Configure(request =>
        {
            if (request.Method == HttpMethod.Get
                && request.RequestUri!.AbsolutePath == "/api/v1/collaborators")
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
                };
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });
    }
}
