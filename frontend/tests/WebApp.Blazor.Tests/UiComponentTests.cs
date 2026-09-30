using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using WebApp.Blazor.Components.Ui;
using WebApp.Blazor.Formatting;

namespace WebApp.Blazor.Tests;

public class UiComponentTests : BlazorComponentTestContext
{

    [Theory]
    [InlineData(ButtonVariant.Default, "ui-button--default")]
    [InlineData(ButtonVariant.Outline, "ui-button--outline")]
    [InlineData(ButtonVariant.Ghost, "ui-button--ghost")]
    [InlineData(ButtonVariant.Destructive, "ui-button--destructive")]
    public void Button_RendersVariantClass(ButtonVariant variant, string expectedClass)
    {
        var cut = Render<Button>(p => p
            .Add(x => x.Variant, variant)
            .Add(x => x.ChildContent, "Salvar"));

        cut.Find("button").ClassList.Should().Contain(expectedClass);
        cut.Find("button").ClassList.Should().Contain("ui-button");
    }

    [Fact]
    public void Button_DefaultIsNotPill()
    {
        var cut = Render<Button>(p => p
            .Add(x => x.Variant, ButtonVariant.Default)
            .Add(x => x.ChildContent, "Nova Folha"));

        cut.Find("button").ClassList.Should().Contain("ui-button--default");
        cut.Find("button").ClassList.Should().NotContain("ui-button--pill");
    }

    [Fact]
    public void Button_MergesAdditionalClassWithVariant()
    {
        var cut = Render<Button>(p => p
            .Add(x => x.Variant, ButtonVariant.Default)
            .Add(x => x.ChildContent, "Entrar")
            .Add(x => x.AdditionalAttributes, new Dictionary<string, object> { ["class"] = "auth-submit" }));

        var button = cut.Find("button");
        button.ClassList.Should().Contain("ui-button--default");
        button.ClassList.Should().Contain("auth-submit");
    }

    [Fact]
    public void Button_IconOnly_HasAccessibleLabel()
    {
        var cut = Render<Button>(p => p
            .Add(x => x.Size, ButtonSize.Icon)
            .Add(x => x.AriaLabel, "Adicionar")
            .Add(x => x.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<Icon>(0);
                builder.AddAttribute(1, nameof(Icon.Kind), IconKind.Plus);
                builder.CloseComponent();
            })));

        var button = cut.Find("button");
        button.GetAttribute("aria-label").Should().Be("Adicionar");
        button.ClassList.Should().Contain("ui-button--icon");
        cut.Find("button svg").GetAttribute("fill").Should().Be("none");
    }

    [Fact]
    public void Button_Disabled_DoesNotInvokeClick()
    {
        var clicked = false;
        var cut = Render<Button>(p => p
            .Add(x => x.Disabled, true)
            .Add(x => x.OnClick, EventCallback.Factory.Create<MouseEventArgs>(this, () => clicked = true))
            .Add(x => x.ChildContent, "Salvar"));

        cut.Find("button").HasAttribute("disabled").Should().BeTrue();
        cut.Find("button").Click();
        clicked.Should().BeFalse();
    }

    [Theory]
    [InlineData(StatusKind.Draft, "Rascunho", "status-badge--draft")]
    [InlineData(StatusKind.PendingApproval, "Aguard. Aprovação", "status-badge--pending-approval")]
    [InlineData(StatusKind.Approved, "Aprovada", "status-badge--approved")]
    [InlineData(StatusKind.Rejected, "Reprovada", "status-badge--rejected")]
    [InlineData(StatusKind.Paid, "Paga", "status-badge--paid")]
    [InlineData(StatusKind.Pending, "Pendente", "status-badge--pending")]
    [InlineData(StatusKind.Active, "Ativo", "status-badge--active")]
    [InlineData(StatusKind.Inactive, "Inativo", "status-badge--inactive")]
    [InlineData(StatusKind.NfSent, "Enviada", "status-badge--nf-sent")]
    [InlineData(StatusKind.NfPending, "Pendente", "status-badge--nf-pending")]
    public void StatusBadge_RendersLabelAndTone(StatusKind kind, string label, string expectedClass)
    {
        var cut = Render<StatusBadge>(p => p.Add(x => x.Kind, kind));

        var badge = cut.Find(".status-badge");
        badge.ClassList.Should().Contain("status-badge--pill");
        badge.ClassList.Should().Contain(expectedClass);
        badge.TextContent.Should().Contain(label);
    }

    [Fact]
    public void StatCard_RendersLabelAndFormattedValue()
    {
        var cut = Render<StatCard>(p => p
            .Add(x => x.Label, "Total a pagar")
            .Add(x => x.Value, 1234.56m)
            .Add(x => x.Tone, SemanticTone.Primary));

        cut.Find(".text-label").TextContent.Should().Contain("Total a pagar");
        cut.Find(".text-stat-value").TextContent.Should().Be("R$ 1.234,56");
        cut.Find(".stat-card__icon").ClassList.Should().Contain("stat-card__icon--primary");
        cut.Find(".stat-card__icon svg").GetAttribute("fill").Should().Be("none");
    }

    [Fact]
    public void Card_RendersSurfaceWithBorder()
    {
        var cut = Render<Card>(p => p.Add(x => x.ChildContent, builder => builder.AddContent(0, "Conteúdo")));

        cut.Find(".ui-card").ClassList.Should().Contain("ui-card");
        cut.Find(".ui-card").TextContent.Should().Contain("Conteúdo");
    }

    [Fact]
    public void Input_RendersDenseFieldWithLabel()
    {
        var cut = Render<Input>(p => p
            .Add(x => x.Label, "Nome")
            .Add(x => x.Placeholder, "Digite o nome"));

        cut.Find("label").TextContent.Should().Contain("Nome");
        cut.Find("input.ui-input").ClassList.Should().Contain("ui-input");
    }

    [Fact]
    public void Input_MoneyVariant_RendersCurrencyPrefix()
    {
        var cut = Render<Input>(p => p
            .Add(x => x.IsMoney, true)
            .Add(x => x.Label, "Valor"));

        cut.Find(".ui-input-wrapper--money").Should().NotBeNull();
        cut.Find(".ui-input-prefix").TextContent.Should().Contain("R$");
        cut.Find("input").GetAttribute("inputmode").Should().Be("numeric");
    }

    [Fact]
    public void Input_MoneyVariant_FormatsTypedDigitsAsCurrency()
    {
        string? emitted = null;
        var cut = Render<Input>(p => p
            .Add(x => x.IsMoney, true)
            .Add(x => x.Label, "iGaming")
            .Add(x => x.ValueChanged, EventCallback.Factory.Create<string?>(this, value => emitted = value)));

        cut.Find("input").Input("123456");

        cut.Find("input").GetAttribute("value").Should().Be("1.234,56");
        emitted.Should().Be("1.234,56");
    }

    [Fact]
    public void Input_MoneyVariant_StripsLettersFromTypedValue()
    {
        string? emitted = null;
        var cut = Render<Input>(p => p
            .Add(x => x.IsMoney, true)
            .Add(x => x.Label, "iGaming")
            .Add(x => x.ValueChanged, EventCallback.Factory.Create<string?>(this, value => emitted = value)));

        cut.Find("input").Input("0asdasd");

        cut.Find("input").GetAttribute("value").Should().Be("0,00");
        emitted.Should().Be("0,00");
    }

    [Fact]
    public void Input_IntegerVariant_UsesNumericInputMode()
    {
        var cut = Render<Input>(p => p
            .Add(x => x.Kind, NumericInputKind.Integer)
            .Add(x => x.Label, "FTD total"));

        cut.Find("input").GetAttribute("inputmode").Should().Be("numeric");
    }

    [Fact]
    public void Input_PercentVariant_UsesDecimalInputMode()
    {
        var cut = Render<Input>(p => p
            .Add(x => x.Kind, NumericInputKind.Percent)
            .Add(x => x.Label, "% grupo"));

        cut.Find("input").GetAttribute("inputmode").Should().Be("decimal");
    }

    [Fact]
    public void Input_PasswordVariant_RendersVisibilityToggle()
    {
        var cut = Render<Input>(p => p
            .Add(x => x.Label, "Senha")
            .Add(x => x.Type, "password"));

        var input = cut.Find("input.ui-input--password-field");
        input.GetAttribute("type").Should().Be("text");
        input.ClassList.Should().Contain("ui-input--password-masked");
        cut.Find(".ui-input__password-toggle").GetAttribute("aria-label").Should().Be("Mostrar senha");

        cut.Find(".ui-input__password-toggle").Click();

        input.ClassList.Should().NotContain("ui-input--password-masked");
        cut.Find(".ui-input__password-toggle").GetAttribute("aria-label").Should().Be("Ocultar senha");
    }

    [Fact]
    public void Select_RendersStyledNativeSelect()
    {
        var cut = Render<Select>(p => p
            .Add(x => x.Label, "Status")
            .Add(x => x.ChildContent, builder =>
            {
                builder.OpenElement(0, "option");
                builder.AddContent(1, "Rascunho");
                builder.CloseElement();
            }));

        cut.Find("select.ui-select").Should().NotBeNull();
        cut.Find("label").TextContent.Should().Contain("Status");
    }

    [Fact]
    public void Tabs_RendersActiveTabWithAriaSelected()
    {
        var cut = Render<Tabs>(p => p
            .Add(x => x.Items, new[] { "Colaboradores", "Projetos" })
            .Add(x => x.ActiveTab, 0)
            .Add(x => x.ActiveTabChanged, EventCallback.Factory.Create<int>(this, _ => { })));

        var tabs = cut.FindAll("[role='tab']");
        tabs.Should().HaveCount(2);
        tabs[0].GetAttribute("aria-selected").Should().Be("true");
        tabs[1].GetAttribute("aria-selected").Should().Be("false");
    }

    [Fact]
    public void FormDialogActions_RendersSubmitAndCancelWithIcons()
    {
        var cut = Render<FormDialogActions>(p => p
            .Add(x => x.SubmitLabel, "Registrar")
            .Add(x => x.OnSubmit, EventCallback.Factory.Create(this, () => { }))
            .Add(x => x.OnCancel, EventCallback.Factory.Create(this, () => { })));

        cut.FindAll(".settings-form__actions button").Should().HaveCount(2);
        cut.Markup.Should().Contain("Registrar");
        cut.Markup.Should().Contain("Cancelar");
    }

    [Fact]
    public void Dialog_WhenOpen_RendersAccessibleModal()
    {
        var cut = Render<Dialog>(p => p
            .Add(x => x.IsOpen, true)
            .Add(x => x.Title, "Confirmar exclusão")
            .Add(x => x.ChildContent, builder => builder.AddContent(0, "Tem certeza?")));

        var dialog = cut.Find("[role='dialog']");
        dialog.GetAttribute("aria-modal").Should().Be("true");
        dialog.TextContent.Should().Contain("Confirmar exclusão");
        cut.Find(".ui-dialog__close").GetAttribute("aria-label").Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Dialog_WhenClosed_DoesNotRender()
    {
        var cut = Render<Dialog>(p => p
            .Add(x => x.IsOpen, false)
            .Add(x => x.Title, "Confirmar"));

        cut.FindAll("[role='dialog']").Should().BeEmpty();
    }

    [Fact]
    public void GoalToggle_WhenChecked_UsesEmeraldTone()
    {
        var cut = Render<GoalToggle>(p => p
            .Add(x => x.Label, "Meta atingida")
            .Add(x => x.Value, true)
            .Add(x => x.ValueChanged, EventCallback.Factory.Create<bool>(this, _ => { })));

        var toggle = cut.Find(".goal-toggle");
        toggle.ClassList.Should().Contain("goal-toggle--checked");
        toggle.ClassList.Should().NotContain("goal-toggle--primary");
    }

    [Fact]
    public void GoalToggle_HasSwitchSemantics()
    {
        var cut = Render<GoalToggle>(p => p
            .Add(x => x.Label, "Meta")
            .Add(x => x.Value, false)
            .Add(x => x.ValueChanged, EventCallback.Factory.Create<bool>(this, _ => { })));

        var toggle = cut.Find("[role='switch']");
        toggle.GetAttribute("aria-checked").Should().Be("false");
    }

    [Theory]
    [InlineData("Ana Silva", "AS")]
    [InlineData("João", "J")]
    [InlineData("Maria Clara Souza", "MC")]
    public void Avatar_RendersInitialsFromName(string name, string expectedInitials)
    {
        var cut = Render<Avatar>(p => p
            .Add(x => x.Name, name)
            .Add(x => x.Size, AvatarSize.Md));

        cut.Find(".ui-avatar__initials").TextContent.Should().Be(expectedInitials);
    }

    [Fact]
    public void Avatar_WithImage_RendersImage()
    {
        var cut = Render<Avatar>(p => p
            .Add(x => x.Name, "Ana Silva")
            .Add(x => x.ImageUrl, "https://example.com/photo.jpg"));

        cut.Find(".ui-avatar__image").GetAttribute("src").Should().Be("https://example.com/photo.jpg");
    }

    [Fact]
    public void Spinner_RendersAccessibleLoadingIndicator()
    {
        var cut = Render<Spinner>();

        var spinner = cut.Find(".ui-spinner");
        spinner.GetAttribute("role").Should().Be("status");
        spinner.GetAttribute("aria-label").Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void EmptyState_RendersTitleAndDescription()
    {
        var cut = Render<EmptyState>(p => p
            .Add(x => x.Title, "Nenhum registro")
            .Add(x => x.Description, "Crie o primeiro item para começar."));

        cut.Find(".empty-state").Should().NotBeNull();
        cut.Find(".text-empty-title").TextContent.Should().Contain("Nenhum registro");
        cut.Find(".empty-state__description").TextContent.Should().Contain("Crie o primeiro item");
        cut.Find(".empty-state__icon svg").GetAttribute("width").Should().Be("32");
    }

    [Fact]
    public void ThemeToggle_RendersLucideSunOrMoon()
    {
        var cut = Render<ThemeToggle>();

        cut.Find(".theme-toggle svg").GetAttribute("fill").Should().Be("none");
    }

    [Fact]
    public void Dialog_CloseButton_RendersLucideX()
    {
        var cut = Render<Dialog>(p => p
            .Add(x => x.IsOpen, true)
            .Add(x => x.Title, "Confirmar")
            .Add(x => x.ChildContent, builder => builder.AddContent(0, "Conteúdo")));

        cut.Find(".ui-dialog__close svg").GetAttribute("fill").Should().Be("none");
    }
}
