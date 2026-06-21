namespace Zibzie.HealthCore.Domain.Entities;

public class AdminUser
{
    public Guid Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public string? DisplayName { get; set; }

    public string PasswordHash { get; set; } = string.Empty;

    public string ProductRole { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? LastLoginAt { get; set; }
}
