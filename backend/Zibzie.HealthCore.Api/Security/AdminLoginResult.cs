using Zibzie.HealthCore.Domain.Security;

namespace Zibzie.HealthCore.Api.Security;

public sealed record AdminLoginResult
{
    public bool Succeeded { get; init; }

    public string? FailureReason { get; init; }

    public Guid? AdminUserId { get; init; }

    public string? Username { get; init; }

    public string? DisplayName { get; init; }

    public string ProductCode { get; init; } = ProductCodes.InternalAdmin;

    public string? ProductRole { get; init; }

    public AdminAccessToken? Token { get; init; }

    public static AdminLoginResult Failed(string failureReason)
    {
        return new AdminLoginResult
        {
            Succeeded = false,
            FailureReason = failureReason,
            ProductCode = ProductCodes.InternalAdmin
        };
    }
}
