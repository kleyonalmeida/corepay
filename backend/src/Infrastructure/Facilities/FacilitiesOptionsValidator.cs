using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Infrastructure.Facilities;

public sealed class FacilitiesOptionsValidator : IValidateOptions<FacilitiesOptions>
{
    private readonly IHostEnvironment _hostEnvironment;

    public FacilitiesOptionsValidator(IHostEnvironment hostEnvironment)
    {
        _hostEnvironment = hostEnvironment;
    }

    public ValidateOptionsResult Validate(string? name, FacilitiesOptions options)
    {
        if (_hostEnvironment.IsEnvironment("Testing"))
        {
            return ValidateOptionsResult.Success;
        }

        return ValidateSecret(options.WebhookSecret);
    }

    public static ValidateOptionsResult ValidateSecret(string? webhookSecret)
    {
        if (string.IsNullOrWhiteSpace(webhookSecret))
        {
            return ValidateOptionsResult.Fail("Facilities:WebhookSecret is required.");
        }

        if (webhookSecret.Length < 32)
        {
            return ValidateOptionsResult.Fail("Facilities:WebhookSecret must be at least 32 characters long.");
        }

        return ValidateOptionsResult.Success;
    }
}
