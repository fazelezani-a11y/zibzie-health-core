using Microsoft.EntityFrameworkCore;
using Zibzie.HealthCore.Application.Security;
using Zibzie.HealthCore.Domain.Security;
using Zibzie.HealthCore.Infrastructure.Persistence;
using Zibzie.HealthCore.Infrastructure.Security;

namespace Zibzie.HealthCore.Tests.Security;

public class AuditLogServiceTests
{
    [Fact]
    public async Task LogAsync_CreatesAuditEntryWithRequiredFields()
    {
        await using var dbContext = CreateDbContext();
        var service = new AuditLogService(dbContext);
        var before = DateTimeOffset.UtcNow;

        await service.LogAsync(new AuditLogRequest
        {
            ActionType = AuditActionTypes.View,
            ResourceType = AuditResourceTypes.PatientSummary,
            Succeeded = true
        });

        var after = DateTimeOffset.UtcNow;
        var entry = await dbContext.AuditLogEntries.SingleAsync();

        Assert.Equal(AuditActionTypes.View, entry.ActionType);
        Assert.Equal(AuditResourceTypes.PatientSummary, entry.ResourceType);
        Assert.True(entry.Succeeded);
        Assert.True(entry.CreatedAt >= before);
        Assert.True(entry.CreatedAt <= after);
    }

    [Fact]
    public async Task LogAsync_StoresPatientUserProductActionAndResourceContext()
    {
        await using var dbContext = CreateDbContext();
        var service = new AuditLogService(dbContext);
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();

        await service.LogAsync(new AuditLogRequest
        {
            UserId = userId,
            PatientId = patientId,
            ProductCode = ProductCodes.DigiCare,
            ProductRole = ProductRoles.DigiCareClinician,
            ActionType = AuditActionTypes.Update,
            ResourceType = AuditResourceTypes.MedicalHistory,
            ResourceId = resourceId,
            Permission = HealthPermissions.EditMedicalHistory,
            AccessScope = AccessScopes.AssignedPatientsOnly,
            AuthorizationReason = AuthorizationReasons.ActiveCare,
            Succeeded = true,
            IpAddress = "127.0.0.1",
            UserAgent = "test-agent",
            CorrelationId = "correlation-1",
            RequestPath = "/api/test",
            HttpMethod = "PUT",
            MetadataJson = "{\"source\":\"test\"}"
        });

        var entry = await dbContext.AuditLogEntries.SingleAsync();

        Assert.Equal(userId, entry.UserId);
        Assert.Equal(patientId, entry.PatientId);
        Assert.Equal(ProductCodes.DigiCare, entry.ProductCode);
        Assert.Equal(ProductRoles.DigiCareClinician, entry.ProductRole);
        Assert.Equal(AuditActionTypes.Update, entry.ActionType);
        Assert.Equal(AuditResourceTypes.MedicalHistory, entry.ResourceType);
        Assert.Equal(resourceId, entry.ResourceId);
        Assert.Equal(HealthPermissions.EditMedicalHistory, entry.Permission);
        Assert.Equal(AccessScopes.AssignedPatientsOnly, entry.AccessScope);
        Assert.Equal(AuthorizationReasons.ActiveCare, entry.AuthorizationReason);
        Assert.Equal("127.0.0.1", entry.IpAddress);
        Assert.Equal("test-agent", entry.UserAgent);
        Assert.Equal("correlation-1", entry.CorrelationId);
        Assert.Equal("/api/test", entry.RequestPath);
        Assert.Equal("PUT", entry.HttpMethod);
        Assert.Equal("{\"source\":\"test\"}", entry.MetadataJson);
    }

    [Fact]
    public async Task LogAsync_StoresFailedAccessWithFailureReason()
    {
        await using var dbContext = CreateDbContext();
        var service = new AuditLogService(dbContext);

        await service.LogAsync(new AuditLogRequest
        {
            ActionType = AuditActionTypes.AccessDenied,
            ResourceType = AuditResourceTypes.Document,
            Permission = HealthPermissions.ViewDocuments,
            Succeeded = false,
            FailureReason = "No active patient access grant was found."
        });

        var entry = await dbContext.AuditLogEntries.SingleAsync();

        Assert.False(entry.Succeeded);
        Assert.Equal(AuditActionTypes.AccessDenied, entry.ActionType);
        Assert.Equal("No active patient access grant was found.", entry.FailureReason);
    }

    [Fact]
    public async Task LogAsync_AllowsOptionalFieldsToBeNull()
    {
        await using var dbContext = CreateDbContext();
        var service = new AuditLogService(dbContext);

        await service.LogAsync(new AuditLogRequest
        {
            ActionType = AuditActionTypes.SystemAction,
            ResourceType = AuditResourceTypes.SecuritySettings,
            Succeeded = true
        });

        var entry = await dbContext.AuditLogEntries.SingleAsync();

        Assert.Null(entry.UserId);
        Assert.Null(entry.ServiceAccountId);
        Assert.Null(entry.PatientId);
        Assert.Null(entry.ProductCode);
        Assert.Null(entry.ProductRole);
        Assert.Null(entry.ResourceId);
        Assert.Null(entry.MetadataJson);
    }

    [Fact]
    public async Task LogAsync_WithMissingRequiredActionOrResource_Throws()
    {
        await using var dbContext = CreateDbContext();
        var service = new AuditLogService(dbContext);

        await Assert.ThrowsAsync<ArgumentException>(() => service.LogAsync(new AuditLogRequest
        {
            ActionType = "",
            ResourceType = AuditResourceTypes.AuditLog,
            Succeeded = false
        }));

        await Assert.ThrowsAsync<ArgumentException>(() => service.LogAsync(new AuditLogRequest
        {
            ActionType = AuditActionTypes.Create,
            ResourceType = " ",
            Succeeded = false
        }));
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
