using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using WebAPI.Tests.Common;

namespace WebAPI.Tests.Cashflow;

internal static class FacilitiesWebhookTestHelper
{
    public const string WebhookPath = "/api/v1/webhooks/facilities/cashflow";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false) }
    };

    public static string ComputeSignature(string secret, string timestamp, string rawBody)
    {
        var payload = $"{timestamp}.{rawBody}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return $"sha256={Convert.ToHexString(hash).ToLowerInvariant()}";
    }

    public static string BuildPayload(
        Guid lancamentoId,
        string evento = "lancamento.criado",
        string tipo = "entrada",
        string categoria = "plataforma",
        decimal valor = 500m,
        string dataLancamento = "2026-09-10",
        Guid? projectId = null,
        Guid? departmentId = null,
        Guid? paymentMethodId = null,
        string? solicitante = null,
        string? localCompra = null,
        string? attachmentUrl = null,
        string? notes = null) =>
        JsonSerializer.Serialize(new
        {
            evento,
            lancamento_id = lancamentoId,
            dados = new
            {
                tipo,
                categoria,
                valor,
                data_lancamento = dataLancamento,
                projectId,
                departmentId,
                paymentMethodId,
                solicitante,
                localCompra,
                attachmentUrl,
                notes
            }
        }, JsonOptions);

    public static HttpRequestMessage CreateSignedPost(
        string rawBody,
        Guid idempotencyKey,
        string? timestamp = null,
        string? signatureOverride = null)
    {
        timestamp ??= DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var signature = signatureOverride
            ?? ComputeSignature(CorePayWebApplicationFactory.TestFacilitiesWebhookSecret, timestamp, rawBody);

        var request = new HttpRequestMessage(HttpMethod.Post, WebhookPath)
        {
            Content = new StringContent(rawBody, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("X-Facilities-Timestamp", timestamp);
        request.Headers.Add("X-Facilities-Signature", signature);
        request.Headers.Add("X-Idempotency-Key", idempotencyKey.ToString());
        return request;
    }

    public static HttpRequestMessage CreateSignedPostFromPayload(
        Guid lancamentoId,
        string evento = "lancamento.criado",
        string tipo = "entrada",
        string categoria = "plataforma",
        decimal valor = 500m,
        string dataLancamento = "2026-09-10",
        Guid? projectId = null,
        Guid? departmentId = null,
        Guid? paymentMethodId = null,
        string? timestamp = null,
        string? signatureOverride = null)
    {
        var body = BuildPayload(
            lancamentoId,
            evento,
            tipo,
            categoria,
            valor,
            dataLancamento,
            projectId,
            departmentId,
            paymentMethodId);
        return CreateSignedPost(body, lancamentoId, timestamp, signatureOverride);
    }
}
