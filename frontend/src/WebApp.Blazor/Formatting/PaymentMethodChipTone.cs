namespace WebApp.Blazor.Formatting;

public static class PaymentMethodChipTone
{
    public static string ResolveCssClass(string? paymentMethodName)
    {
        if (string.IsNullOrWhiteSpace(paymentMethodName))
        {
            return "payment-method-chip--muted";
        }

        var normalized = paymentMethodName.ToLowerInvariant();
        if (normalized.Contains("cartão") || normalized.Contains("cartao"))
        {
            return "payment-method-chip--blue";
        }

        if (normalized.Contains("dinheiro"))
        {
            return "payment-method-chip--emerald";
        }

        if (normalized.Contains("pix"))
        {
            return "payment-method-chip--purple";
        }

        if (normalized.Contains("boleto"))
        {
            return "payment-method-chip--orange";
        }

        if (normalized.Contains("transferência") || normalized.Contains("transferencia"))
        {
            return "payment-method-chip--yellow";
        }

        return "payment-method-chip--muted";
    }
}
