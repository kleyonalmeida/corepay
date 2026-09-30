using Microsoft.Extensions.Options;

namespace WebAPI.Auth;

public sealed class JwtOptionsValidator : IValidateOptions<JwtOptions>
{
    public ValidateOptionsResult Validate(string? name, JwtOptions options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.Issuer))
        {
            failures.Add("Jwt:Issuer is required.");
        }

        if (string.IsNullOrWhiteSpace(options.Audience))
        {
            failures.Add("Jwt:Audience is required.");
        }

        if (string.IsNullOrWhiteSpace(options.Key))
        {
            failures.Add("Jwt:Key is required.");
        }
        else if (options.Key.Length < 32)
        {
            failures.Add("Jwt:Key must be at least 32 characters long.");
        }

        if (options.ExpirationMinutes <= 0)
        {
            failures.Add("Jwt:ExpirationMinutes must be greater than zero.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
