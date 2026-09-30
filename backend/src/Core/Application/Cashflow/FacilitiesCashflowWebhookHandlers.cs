using BuildingBlocks.Results;
using MediatR;

namespace Core.Application.Cashflow;

public sealed class ProcessFacilitiesCashflowWebhookHandler(ICashflowStore store)
    : IRequestHandler<ProcessFacilitiesCashflowWebhookCommand, Result<FacilitiesCashflowWebhookResponse>>
{
    public Task<Result<FacilitiesCashflowWebhookResponse>> Handle(
        ProcessFacilitiesCashflowWebhookCommand request,
        CancellationToken cancellationToken)
    {
        if (request.IdempotencyKey != request.Payload.LancamentoId)
        {
            return Task.FromResult(Result<FacilitiesCashflowWebhookResponse>.Failure(
                Error.Validation(
                    "facilities.idempotency_key_mismatch",
                    "X-Idempotency-Key must match lancamento_id.")));
        }

        if (!string.Equals(
                request.Payload.Evento,
                FacilitiesCashflowWebhookEvents.LancamentoCriado,
                StringComparison.Ordinal))
        {
            return Task.FromResult(Result<FacilitiesCashflowWebhookResponse>.Failure(
                Error.UnprocessableEntity(
                    "facilities.unsupported_event",
                    "Only lancamento.criado events are supported.")));
        }

        return store.CreateFromFacilitiesWebhookAsync(request.Payload, cancellationToken);
    }
}
