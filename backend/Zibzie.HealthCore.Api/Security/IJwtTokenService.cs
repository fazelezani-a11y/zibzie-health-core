using Zibzie.HealthCore.Domain.Entities;

namespace Zibzie.HealthCore.Api.Security;

public interface IJwtTokenService
{
    AdminAccessToken CreateAdminAccessToken(AdminUser adminUser);
}
