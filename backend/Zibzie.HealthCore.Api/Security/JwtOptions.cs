namespace Zibzie.HealthCore.Api.Security;

public sealed class JwtOptions
{
    public string? Authority { get; set; }

    public string? Issuer { get; set; }

    public string? Audience { get; set; }

    public bool RequireHttpsMetadata { get; set; } = true;

    public bool ValidateIssuer { get; set; } = true;

    public bool ValidateAudience { get; set; } = true;

    public bool ValidateLifetime { get; set; } = true;

    public bool ValidateIssuerSigningKey { get; set; } = true;

    public string? SigningKey { get; set; }

    public string? Key { get; set; }

    public int AccessTokenMinutes { get; set; } = 60;

    public string? EffectiveSigningKey => string.IsNullOrWhiteSpace(SigningKey)
        ? Key
        : SigningKey;
}
