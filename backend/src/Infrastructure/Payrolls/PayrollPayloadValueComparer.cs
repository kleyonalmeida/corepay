using System.Text.Json;
using Core.Domain;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Infrastructure.Payrolls;

internal static class PayrollPayloadValueComparer
{
    public static ValueComparer<PayrollCollaboratorEntryPayload> Instance { get; } = new(
        (left, right) => Serialize(left) == Serialize(right),
        value => Serialize(value).GetHashCode(StringComparison.Ordinal),
        value => JsonSerializer.Deserialize<PayrollCollaboratorEntryPayload>(
            Serialize(value),
            PayrollJsonOptions.Instance) ?? new PayrollCollaboratorEntryPayload());

    private static string Serialize(PayrollCollaboratorEntryPayload? value) =>
        JsonSerializer.Serialize(value ?? new PayrollCollaboratorEntryPayload(), PayrollJsonOptions.Instance);
}
