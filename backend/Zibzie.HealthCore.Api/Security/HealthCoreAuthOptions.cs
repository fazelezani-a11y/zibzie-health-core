using Zibzie.HealthCore.Domain.Security;

namespace Zibzie.HealthCore.Api.Security;

public sealed class HealthCoreAuthOptions
{
    public bool AllowHeaderFallback { get; set; }

    public bool AllowDefaultDevFallback { get; set; }

    public string DefaultDevProductCode { get; set; } = ProductCodes.InternalAdmin;

    public string DefaultDevProductRole { get; set; } = ProductRoles.HealthCoreAdmin;

    public string DefaultDevServiceAccountId { get; set; } = "dev-admin";
}
