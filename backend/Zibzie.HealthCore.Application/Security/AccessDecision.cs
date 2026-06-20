namespace Zibzie.HealthCore.Application.Security;

public sealed record AccessDecision(
    bool IsAllowed,
    string? DenialReason = null,
    string? MatchedPermission = null,
    string? MatchedScope = null,
    Guid? MatchedGrantId = null)
{
    public static AccessDecision Allow(
        string? matchedPermission = null,
        string? matchedScope = null,
        Guid? matchedGrantId = null)
    {
        return new AccessDecision(true, null, matchedPermission, matchedScope, matchedGrantId);
    }

    public static AccessDecision Deny(string denialReason)
    {
        return new AccessDecision(false, denialReason);
    }
}
