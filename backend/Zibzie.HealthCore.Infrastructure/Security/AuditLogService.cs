using Zibzie.HealthCore.Application.Security;
using Zibzie.HealthCore.Domain.Entities;
using Zibzie.HealthCore.Infrastructure.Persistence;

namespace Zibzie.HealthCore.Infrastructure.Security;

public class AuditLogService : IAuditLogService
{
    private readonly AppDbContext _dbContext;

    public AuditLogService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task LogAsync(AuditLogRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var actionType = Required(request.ActionType, nameof(request.ActionType));
        var resourceType = Required(request.ResourceType, nameof(request.ResourceType));

        var entry = new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            ServiceAccountId = TrimToNull(request.ServiceAccountId),
            PatientId = request.PatientId,
            ProductCode = TrimToNull(request.ProductCode),
            ProductRole = TrimToNull(request.ProductRole),
            ActionType = actionType,
            ResourceType = resourceType,
            ResourceId = request.ResourceId,
            Permission = TrimToNull(request.Permission),
            AccessScope = TrimToNull(request.AccessScope),
            AuthorizationReason = TrimToNull(request.AuthorizationReason),
            Succeeded = request.Succeeded,
            FailureReason = TrimToNull(request.FailureReason),
            IpAddress = TrimToNull(request.IpAddress),
            UserAgent = TrimToNull(request.UserAgent),
            CorrelationId = TrimToNull(request.CorrelationId),
            RequestPath = TrimToNull(request.RequestPath),
            HttpMethod = TrimToNull(request.HttpMethod),
            MetadataJson = TrimToNull(request.MetadataJson),
            CreatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.AuditLogEntries.Add(entry);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string Required(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{parameterName} is required.", parameterName);
        }

        return value.Trim();
    }

    private static string? TrimToNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
