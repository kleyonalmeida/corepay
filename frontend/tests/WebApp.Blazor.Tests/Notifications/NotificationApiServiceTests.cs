using System.Net;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Blazor.Services;
using WebApp.Blazor.Tests.Auth;

namespace WebApp.Blazor.Tests.Notifications;

public sealed class NotificationApiServiceTests : BlazorComponentTestContext
{
    private readonly StubHttpMessageHandler _httpHandler;

    public NotificationApiServiceTests()
    {
        _httpHandler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        Services.AddScoped(_ => new HttpClient(_httpHandler)
        {
            BaseAddress = new Uri("http://localhost:5000")
        });
        Services.AddScoped<INotificationApiService, NotificationApiService>();
    }

    [Fact]
    public void BuildListUrl_UsesEndpointOnly()
    {
        NotificationApiService.BuildListUrl().Should().Be("api/v1/notifications");
    }

    [Fact]
    public void BuildMarkReadUrl_UsesNotificationId()
    {
        var id = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6");
        NotificationApiService.BuildMarkReadUrl(id)
            .Should().Be("api/v1/notifications/3fa85f64-5717-4562-b3fc-2c963f66afa6/read");
    }

    [Fact]
    public async Task GetAsync_Success_DeserializesItemsAndUnreadCount()
    {
        ConfigureGetResponse(HttpStatusCode.OK, """
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
            """);

        var result = await Services.GetRequiredService<INotificationApiService>().GetAsync();

        result.Status.Should().Be(NotificationApiStatus.Success);
        result.Data!.UnreadCount.Should().Be(1);
        result.Data.Items.Single().Type.Should().Be("payroll_submitted");
    }

    [Fact]
    public async Task MarkReadAsync_Success_ReturnsSuccess()
    {
        ConfigureMarkReadResponse(HttpStatusCode.NoContent);

        var result = await Services.GetRequiredService<INotificationApiService>()
            .MarkReadAsync(Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6"));

        result.Status.Should().Be(NotificationApiStatus.Success);
    }

    [Fact]
    public async Task MarkReadAsync_NotFound_ReturnsNotFound()
    {
        ConfigureMarkReadResponse(HttpStatusCode.NotFound, """
            { "error": "notifications.not_found", "message": "Notificação não encontrada." }
            """);

        var result = await Services.GetRequiredService<INotificationApiService>()
            .MarkReadAsync(Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6"));

        result.Status.Should().Be(NotificationApiStatus.NotFound);
        result.ErrorCode.Should().Be("notifications.not_found");
    }

    private void ConfigureGetResponse(HttpStatusCode statusCode, string body)
    {
        _httpHandler.Configure(request =>
        {
            if (request.Method == HttpMethod.Get
                && request.RequestUri!.AbsolutePath == "/api/v1/notifications")
            {
                return new HttpResponseMessage(statusCode)
                {
                    Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
                };
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });
    }

    private void ConfigureMarkReadResponse(HttpStatusCode statusCode, string? body = null)
    {
        _httpHandler.Configure(request =>
        {
            if (request.Method == HttpMethod.Put
                && request.RequestUri!.AbsolutePath.EndsWith("/read", StringComparison.Ordinal))
            {
                return new HttpResponseMessage(statusCode)
                {
                    Content = body is null
                        ? null
                        : new StringContent(body, System.Text.Encoding.UTF8, "application/json")
                };
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });
    }
}
