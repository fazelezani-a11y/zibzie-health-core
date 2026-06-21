using Zibzie.HealthCore.Application.Security;

namespace Zibzie.HealthCore.Api.Security;

public interface IAdminAuthService
{
    Task<AdminLoginResult> LoginAsync(
        string? username,
        string? password,
        HealthCoreRequestContext requestContext,
        CancellationToken cancellationToken = default);
}
