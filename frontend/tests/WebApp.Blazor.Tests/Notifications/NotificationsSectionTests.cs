using System.Net;
using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Blazor.Components.Notifications;
using WebApp.Blazor.Services;
using WebApp.Blazor.Tests.Auth;

namespace WebApp.Blazor.Tests.Notifications;

public sealed class NotificationsSectionTests : BlazorComponentTestContext
{
    private readonly StubHttpMessageHandler _httpHandler;

    public NotificationsSectionTests()
    {
        _httpHandler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        Services.AddScoped(_ => new HttpClient(_httpHandler)
        {
            BaseAddress = new Uri("http://localhost:5000")
        });
        Services.AddScoped<INotificationApiService, NotificationApiService>();
        Services.AddScoped<NotificationState>();
    }

    [Fact]
    public void NotificationsSection_ShowsListWithSemanticClasses()
    {
        ConfigureGetResponse(CreateNotificationsJson());

        var cut = Render<NotificationsSection>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Folha submetida");
            cut.Markup.Should().Contain("notification-row__icon--yellow");
            cut.Markup.Should().Contain("Marcar como lida");
        });
    }

    [Fact]
    public void NotificationsSection_Empty_ShowsEmptyState()
    {
        ConfigureGetResponse("""
            { "items": [], "unreadCount": 0 }
            """);

        var cut = Render<NotificationsSection>();

        cut.WaitForAssertion(() =>
            cut.Markup.Should().Contain("Nenhuma notificação"));
    }

    [Fact]
    public void NotificationsSection_Error_ShowsErrorState()
    {
        _httpHandler.Configure(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var cut = Render<NotificationsSection>();

        cut.WaitForAssertion(() =>
            cut.Markup.Should().Contain("Erro ao carregar notificações"));
    }

    [Fact]
    public void NotificationsSection_ClickRow_NavigatesToPayroll()
    {
        ConfigureGetAndMarkReadResponses();

        var navigation = Services.GetRequiredService<NavigationManager>();
        var cut = Render<NotificationsSection>();

        cut.WaitForAssertion(() =>
        {
            cut.Find(".notification-row").Click();
            navigation.Uri.Should().EndWith("/payrolls/11111111-1111-1111-1111-111111111111");
        });
    }

    private void ConfigureGetAndMarkReadResponses()
    {
        _httpHandler.Configure(request =>
        {
            if (request.Method == HttpMethod.Get
                && request.RequestUri!.AbsolutePath == "/api/v1/notifications")
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        CreateNotificationsJson(),
                        System.Text.Encoding.UTF8,
                        "application/json")
                };
            }

            if (request.Method == HttpMethod.Put
                && request.RequestUri!.AbsolutePath.EndsWith("/read", StringComparison.Ordinal))
            {
                return new HttpResponseMessage(HttpStatusCode.NoContent);
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });
    }

    private void ConfigureGetResponse(string body)
    {
        _httpHandler.Configure(request =>
        {
            if (request.Method == HttpMethod.Get
                && request.RequestUri!.AbsolutePath == "/api/v1/notifications")
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
                };
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });
    }

    private void ConfigureMarkReadResponse(HttpStatusCode statusCode)
    {
        _httpHandler.Configure(request =>
        {
            if (request.Method == HttpMethod.Put
                && request.RequestUri!.AbsolutePath.EndsWith("/read", StringComparison.Ordinal))
            {
                return new HttpResponseMessage(statusCode);
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });
    }

    private static string CreateNotificationsJson() => """
        {
          "items": [
            {
              "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
              "type": "payroll_submitted",
              "title": "Folha submetida",
              "message": "Analistas — 03/2026 aguardando aprovação.",
              "payrollId": "11111111-1111-1111-1111-111111111111",
              "isRead": false,
              "createdAt": "2026-03-10T15:00:00+00:00"
            }
          ],
          "unreadCount": 1
        }
        """;
}
