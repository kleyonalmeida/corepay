using BuildingBlocks.Results;
using MediatR;

namespace Core.Application.Cashflow;

public sealed record ProcessFacilitiesCashflowWebhookCommand(
    FacilitiesCashflowWebhookRequest Payload,
    Guid IdempotencyKey)
    : IRequest<Result<FacilitiesCashflowWebhookResponse>>;
