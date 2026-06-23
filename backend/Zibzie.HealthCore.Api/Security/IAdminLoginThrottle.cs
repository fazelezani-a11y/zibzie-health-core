using Zibzie.HealthCore.Application.Security;

namespace Zibzie.HealthCore.Api.Security;

public interface IAdminLoginThrottle
{
    bool IsBlocked(
        string? normalizedUsername,
        HealthCoreRequestContext requestContext,
        DateTimeOffset now);

    void RecordFailure(
        string? normalizedUsername,
        HealthCoreRequestContext requestContext,
        DateTimeOffset now);

    void RecordSuccess(
        string? normalizedUsername,
        HealthCoreRequestContext requestContext);
}
