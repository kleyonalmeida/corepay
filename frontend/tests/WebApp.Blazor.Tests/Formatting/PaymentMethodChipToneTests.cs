using FluentAssertions;
using WebApp.Blazor.Formatting;

namespace WebApp.Blazor.Tests.Formatting;

public class PaymentMethodChipToneTests
{
    [Theory]
    [InlineData("Cartão corporativo", "payment-method-chip--blue")]
    [InlineData("Pix", "payment-method-chip--purple")]
    [InlineData("Boleto bancário", "payment-method-chip--orange")]
    public void ResolveCssClass_MatchesVisualRules(string name, string expectedClass)
    {
        PaymentMethodChipTone.ResolveCssClass(name).Should().Be(expectedClass);
    }
}
