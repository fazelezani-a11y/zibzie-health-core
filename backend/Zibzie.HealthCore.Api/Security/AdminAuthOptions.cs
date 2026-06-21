using Zibzie.HealthCore.Domain.Security;

namespace Zibzie.HealthCore.Api.Security;

public sealed class AdminAuthOptions
{
    public BootstrapAdminOptions BootstrapAdmin { get; set; } = new();
}

public sealed class BootstrapAdminOptions
{
    public bool Enabled { get; set; }

    public string? Username { get; set; }

    public string? Password { get; set; }

    public string? DisplayName { get; set; }

    public string ProductRole { get; set; } = ProductRoles.HealthCoreAdmin;
}
