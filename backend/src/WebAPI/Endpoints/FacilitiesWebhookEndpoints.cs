using System.Text.Json;
using System.Text.Json.Serialization;
using Asp.Versioning.Builder;
using Core.Application.Cashflow;
using Infrastructure.Facilities;
using MediatR;
using WebAPI.Extensions;

namespace WebAPI.Endpoints;

public static class FacilitiesWebhookEndpoints
{
    private const int MaxBodyBytes = 256 * 1024;

    private static readonly JsonSerializerOptions PayloadJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false) }
    };

    public static IEndpointRouteBuilder MapFacilitiesWebhookEndpoints(this IEndpointRouteBuilder app)
    {
        var versionSet = app.NewApiVersionSet()
            .HasApiVersion(new Asp.Versioning.ApiVersion(1, 0))
            .ReportApiVersions()
            .Build();

        var group = app.MapGroup("/api/v{version:apiVersion}/webhooks/facilities")
            .WithApiVersionSet(versionSet)
            .HasApiVersion(new Asp.Versioning.ApiVersion(1, 0))
            .WithTags("Facilities Webhooks");

        group.MapPost("/cashflow", async (
            HttpRequest httpRequest,
            IFacilitiesWebhookSignatureValidator signatureValidator,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            if (httpRequest.ContentLength is > MaxBodyBytes)
            {
                return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);
            }

            httpRequest.EnableBuffering();
            using var memoryStream = new MemoryStream();
            var buffer = new byte[8192];
            var totalRead = 0;

            while (true)
            {
                var read = await httpRequest.Body.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
                if (read == 0)
                {
                    break;
                }

                totalRead += read;
                if (totalRead > MaxBodyBytes)
                {
                    return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);
                }

                memoryStream.Write(buffer, 0, read);
            }

            var rawBody = memoryStream.ToArray();

            if (!WebhookHeaderReader.TryReadSingleHeader(
                    httpRequest.Headers,
                    "X-Facilities-Timestamp",
                    WebhookHeaderReader.TimestampMaxLength,
                    out var timestamp,
                    out var timestampErrorCode,
                    out var timestampErrorMessage))
            {
                return Results.BadRequest(new { error = timestampErrorCode, message = timestampErrorMessage });
            }

            if (!WebhookHeaderReader.TryReadSingleHeader(
                    httpRequest.Headers,
                    "X-Facilities-Signature",
                    WebhookHeaderReader.SignatureMaxLength,
                    out var signature,
                    out var signatureErrorCode,
                    out var signatureErrorMessage))
            {
                return Results.BadRequest(new { error = signatureErrorCode, message = signatureErrorMessage });
            }

            if (!WebhookHeaderReader.TryReadSingleHeader(
                    httpRequest.Headers,
                    "X-Idempotency-Key",
                    WebhookHeaderReader.IdempotencyKeyMaxLength,
                    out var idempotencyKeyHeader,
                    out var idempotencyErrorCode,
                    out var idempotencyErrorMessage))
            {
                return Results.BadRequest(new { error = idempotencyErrorCode, message = idempotencyErrorMessage });
            }

            var signatureResult = signatureValidator.Validate(timestamp, signature, rawBody);
            if (signatureResult.IsFailure)
            {
                return signatureResult.ToHttpResult();
            }

            if (!Guid.TryParse(idempotencyKeyHeader, out var idempotencyKey))
            {
                return Results.BadRequest(new
                {
                    error = "facilities.idempotency_key_invalid",
                    message = "X-Idempotency-Key must be a valid GUID."
                });
            }

            FacilitiesCashflowWebhookRequest? payload;
            try
            {
                payload = JsonSerializer.Deserialize<FacilitiesCashflowWebhookRequest>(rawBody, PayloadJsonOptions);
            }
            catch (JsonException)
            {
                return Results.BadRequest(new
                {
                    error = "facilities.invalid_payload",
                    message = "Webhook payload is invalid JSON."
                });
            }

            if (payload is null || payload.Dados is null)
            {
                return Results.BadRequest(new
                {
                    error = "facilities.invalid_payload",
                    message = "Webhook payload is missing required fields."
                });
            }

            var result = await mediator.Send(
                new ProcessFacilitiesCashflowWebhookCommand(payload, idempotencyKey),
                cancellationToken);

            return result.ToHttpResult();
        })
        .AllowAnonymous()
        .RequireRateLimiting("webhook")
        .WithName("FacilitiesCashflowWebhook");

        group.MapGet("/cashflow", () => Results.StatusCode(StatusCodes.Status405MethodNotAllowed))
            .AllowAnonymous()
            .WithName("FacilitiesCashflowWebhookMethodNotAllowed");

        return app;
    }
}
