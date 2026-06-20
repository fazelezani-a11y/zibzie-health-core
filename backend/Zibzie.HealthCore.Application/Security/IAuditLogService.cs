namespace Zibzie.HealthCore.Application.Security;

public interface IAuditLogService
{
    Task LogAsync(AuditLogRequest request, CancellationToken cancellationToken = default);
}
