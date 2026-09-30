namespace WebAPI.Tests.Cashflow;

internal static class CashflowTestHelper
{
    public static object CreateEntradaPayload(
        decimal amount = 1000m,
        int month = 9,
        int year = 2026,
        string category = "plataforma",
        Guid? projectId = null,
        string? notes = null) =>
        new
        {
            type = "entrada",
            category,
            amount,
            transactionDate = $"{year:D4}-{month:D2}-10",
            month,
            year,
            projectId,
            paymentMethodId = (Guid?)null,
            departmentId = (Guid?)null,
            requester = (string?)null,
            purchaseLocation = (string?)null,
            installmentTotal = 1,
            attachmentUrl = (string?)null,
            notes
        };

    public static object CreateSaidaPayload(
        Guid paymentMethodId,
        Guid departmentId,
        decimal amount = 300m,
        int month = 9,
        int year = 2026,
        string category = "folhaPagamento",
        Guid? projectId = null,
        int installmentTotal = 1,
        string? requester = "Solicitante Teste",
        string? purchaseLocation = "Loja Teste",
        string? notes = null) =>
        new
        {
            type = "saida",
            category,
            amount,
            transactionDate = $"{year:D4}-{month:D2}-10",
            month,
            year,
            projectId,
            paymentMethodId,
            departmentId,
            requester,
            purchaseLocation,
            installmentTotal,
            attachmentUrl = (string?)null,
            notes
        };

    public static object CreateUpdateEntradaPayload(
        decimal amount = 1000m,
        int month = 9,
        int year = 2026,
        string category = "plataforma",
        Guid? projectId = null,
        string? notes = null) =>
        new
        {
            type = "entrada",
            category,
            amount,
            transactionDate = $"{year:D4}-{month:D2}-10",
            month,
            year,
            projectId,
            paymentMethodId = (Guid?)null,
            departmentId = (Guid?)null,
            requester = (string?)null,
            purchaseLocation = (string?)null,
            attachmentUrl = (string?)null,
            notes
        };
}
