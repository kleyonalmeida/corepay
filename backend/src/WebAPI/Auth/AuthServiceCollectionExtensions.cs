using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace WebAPI.Auth;

public static class AuthServiceCollectionExtensions
{
    public const string AdminRolePolicy = "admin-role";
    public const string SuperAdminRolePolicy = "superadmin-role";

    public static IServiceCollection AddCorePayJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<JwtOptions>, JwtOptionsValidator>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserAuthorizationStateProvider, CurrentUserAuthorizationStateProvider>();

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtOptions.SchemeName;
                options.DefaultChallengeScheme = JwtOptions.SchemeName;
            })
            .AddJwtBearer(options =>
            {
                var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
                    ?? throw new InvalidOperationException("Jwt configuration section is missing.");

                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtOptions.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
            });

        services.AddCorePayPermissionPolicies();
        services.AddScoped<JwtTokenService>();
        services.AddScoped<LoginService>();

        return services;
    }
}
