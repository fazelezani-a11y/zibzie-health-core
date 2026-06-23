using Zibzie.HealthCore.Domain.Security;

namespace Zibzie.HealthCore.Api.Security;

public sealed class AdminAuthOptions
{
    public BootstrapAdminOptions BootstrapAdmin { get; set; } = new();

    public AdminLoginThrottleOptions LoginThrottle { get; set; } = new();
}

public sealed class BootstrapAdminOptions
{
    public bool Enabled { get; set; }

    public string? Username { get; set; }

    public string? Password { get; set; }

    public string? DisplayName { get; set; }

    public string ProductRole { get; set; } = ProductRoles.HealthCoreAdmin;
}

public sealed class AdminLoginThrottleOptions
{
    public bool Enabled { get; set; } = true;

    public int MaxFailedAttempts { get; set; } = 5;

    public int WindowMinutes { get; set; } = 15;

    public int LockoutMinutes { get; set; } = 5;
}
