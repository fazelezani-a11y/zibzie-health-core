using System.Text;

namespace Zibzie.HealthCore.Api.Security;

public static class HealthCoreSecurityStartupValidation
{
    private const int MinimumSigningKeyBytes = 32;

    public static void Validate(
        IHostEnvironment environment,
        HealthCoreAuthOptions authOptions,
        JwtOptions jwtOptions,
        AdminAuthOptions adminAuthOptions)
    {
        if (!environment.IsProduction())
        {
            return;
        }

        if (authOptions.AllowHeaderFallback || authOptions.AllowDefaultDevFallback)
        {
            throw new InvalidOperationException("Health Core auth fallback must be disabled in Production.");
        }

        if (adminAuthOptions.BootstrapAdmin.Enabled)
        {
            throw new InvalidOperationException("Admin bootstrap must be disabled in Production.");
        }

        var signingKey = jwtOptions.EffectiveSigningKey;

        if (string.IsNullOrWhiteSpace(jwtOptions.Authority) && string.IsNullOrWhiteSpace(signingKey))
        {
            throw new InvalidOperationException("JWT authority or signing key must be configured in Production.");
        }

        if (!string.IsNullOrWhiteSpace(signingKey)
            && Encoding.UTF8.GetByteCount(signingKey) < MinimumSigningKeyBytes)
        {
            throw new InvalidOperationException("JWT signing key is too short for Production.");
        }
    }
}
