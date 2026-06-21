namespace Zibzie.HealthCore.Api.Security;

public sealed record AdminAccessToken
{
    public string AccessToken { get; init; } = string.Empty;

    public string TokenType { get; init; } = "Bearer";

    public DateTimeOffset ExpiresAt { get; init; }
}
