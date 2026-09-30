using WebApp.Blazor.Services;

namespace WebApp.Blazor.Formatting;

public static class PaymentMethodErrorMessages
{
    public static string ForList(PaymentMethodListResult result) =>
        result.Status switch
        {
            PaymentMethodApiStatus.Forbidden => "Você não tem permissão para visualizar formas de pagamento.",
            _ => "Não foi possível carregar as formas de pagamento."
        };

    public static string ForMutation(PaymentMethodMutationResult result) =>
        result.Status switch
        {
            PaymentMethodApiStatus.Forbidden => "Você não tem permissão para alterar formas de pagamento.",
            PaymentMethodApiStatus.Conflict => "Já existe uma forma de pagamento com este nome.",
            PaymentMethodApiStatus.ValidationError => result.Message ?? "Verifique os dados informados.",
            _ => "Não foi possível salvar a forma de pagamento."
        };

    public static string ForDelete(PaymentMethodDeleteResult result) =>
        result.Status switch
        {
            PaymentMethodApiStatus.Forbidden => "Você não tem permissão para excluir formas de pagamento.",
            PaymentMethodApiStatus.Conflict => "Esta forma de pagamento está em uso no fluxo de caixa.",
            PaymentMethodApiStatus.NotFound => "Forma de pagamento não encontrada.",
            _ => "Não foi possível excluir a forma de pagamento."
        };
}
