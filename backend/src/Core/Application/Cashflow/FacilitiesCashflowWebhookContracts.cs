using System.Text.Json.Serialization;
using Core.Domain;

namespace Core.Application.Cashflow;

public static class FacilitiesCashflowWebhookEvents
{
    public const string LancamentoCriado = "lancamento.criado";
}

public sealed record FacilitiesCashflowWebhookRequest(
    [property: JsonPropertyName("evento")] string Evento,
    [property: JsonPropertyName("lancamento_id")] Guid LancamentoId,
    [property: JsonPropertyName("dados")] FacilitiesCashflowWebhookData Dados);

public sealed record FacilitiesCashflowWebhookData(
    [property: JsonPropertyName("tipo")] ProjectCostType Tipo,
    [property: JsonPropertyName("categoria")] ProjectCostCategory Categoria,
    [property: JsonPropertyName("valor")] decimal Valor,
    [property: JsonPropertyName("data_lancamento")] DateOnly DataLancamento,
    [property: JsonPropertyName("projectId")] Guid? ProjectId,
    [property: JsonPropertyName("departmentId")] Guid? DepartmentId,
    [property: JsonPropertyName("paymentMethodId")] Guid? PaymentMethodId,
    [property: JsonPropertyName("solicitante")] string? Solicitante,
    [property: JsonPropertyName("localCompra")] string? LocalCompra,
    [property: JsonPropertyName("attachmentUrl")] string? AttachmentUrl,
    [property: JsonPropertyName("notes")] string? Notes);

public sealed record FacilitiesCashflowWebhookResponse(
    Guid Id,
    Guid LancamentoId,
    bool Idempotent);
