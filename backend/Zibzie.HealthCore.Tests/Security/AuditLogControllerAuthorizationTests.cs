using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zibzie.HealthCore.Api.Controllers;
using Zibzie.HealthCore.Application.Security;
using Zibzie.HealthCore.Domain.Entities;
using Zibzie.HealthCore.Domain.Security;
using Zibzie.HealthCore.Infrastructure.Persistence;

namespace Zibzie.HealthCore.Tests.Security;

public class AuditLogControllerAuthorizationTests
{
    [Fact]
    public async Task GetAuditLog_WhenAuthorizationDenied_ReturnsForbiddenAndAuditsDenied()
    {
        await using var dbContext = CreateDbContext();
        var patientId = Guid.NewGuid();
        var auditLog = new FakeAuditLogService();
        var controller = CreateController(
            dbContext,
            new FakeAuthorizationService(AccessDecision.Deny("Audit review denied.")),
            auditLog);

        var result = await controller.GetAuditLog(
            new AuditLogQueryRequest
            {
                PatientId = patientId
            },
            CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(403, objectResult.StatusCode);

        var auditRequest = Assert.Single(auditLog.Requests);
        Assert.Equal(AuditActionTypes.AccessDenied, auditRequest.ActionType);
        Assert.Equal(AuditResourceTypes.AuditLog, auditRequest.ResourceType);
        Assert.Equal(HealthPermissions.ViewAuditLog, auditRequest.Permission);
        Assert.Equal(patientId, auditRequest.PatientId);
        Assert.False(auditRequest.Succeeded);
        Assert.Equal("Audit review denied.", auditRequest.FailureReason);
    }

    [Fact]
    public async Task GetAuditLog_WhenAuthorizationAllowed_ReturnsFilteredDtosAndAuditsSuccess()
    {
        await using var dbContext = CreateDbContext();
        var patientId = Guid.NewGuid();
        var otherPatientId = Guid.NewGuid();
        var newestEntry = AuditEntry(patientId, AuditActionTypes.AccessDenied, false, DateTimeOffset.UtcNow);
        var olderEntry = AuditEntry(patientId, AuditActionTypes.View, true, DateTimeOffset.UtcNow.AddMinutes(-5));
        dbContext.AuditLogEntries.AddRange(
            olderEntry,
            newestEntry,
            AuditEntry(otherPatientId, AuditActionTypes.View, true, DateTimeOffset.UtcNow.AddMinutes(-1)));
        await dbContext.SaveChangesAsync();

        var auditLog = new FakeAuditLogService();
        var controller = CreateController(
            dbContext,
            new FakeAuthorizationService(AccessDecision.Allow(
                HealthPermissions.ViewAuditLog,
                AccessScopes.AllPatients)),
            auditLog);

        var result = await controller.GetAuditLog(
            new AuditLogQueryRequest
            {
                PatientId = patientId,
                Page = 1,
                PageSize = 10
            },
            CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<AuditLogQueryResponse>(okResult.Value);
        Assert.Equal(2, response.TotalCount);
        Assert.Equal(1, response.Page);
        Assert.Equal(10, response.PageSize);
        Assert.Collection(
            response.Items,
            first =>
            {
                Assert.Equal(newestEntry.Id, first.Id);
                Assert.Equal(AuditActionTypes.AccessDenied, first.ActionType);
                Assert.Equal(patientId, first.PatientId);
                Assert.Equal("Denied.", first.FailureReason);
            },
            second =>
            {
                Assert.Equal(olderEntry.Id, second.Id);
                Assert.Equal(AuditActionTypes.View, second.ActionType);
                Assert.Equal(patientId, second.PatientId);
            });

        var auditRequest = Assert.Single(auditLog.Requests);
        Assert.Equal(AuditActionTypes.View, auditRequest.ActionType);
        Assert.Equal(AuditResourceTypes.AuditLog, auditRequest.ResourceType);
        Assert.Equal(HealthPermissions.ViewAuditLog, auditRequest.Permission);
        Assert.Equal(patientId, auditRequest.PatientId);
        Assert.True(auditRequest.Succeeded);
        Assert.Equal(AccessScopes.AllPatients, auditRequest.AccessScope);
    }

    [Fact]
    public async Task GetAuditLog_BoundsPageSizeAndFiltersBySucceeded()
    {
        await using var dbContext = CreateDbContext();
        var patientId = Guid.NewGuid();
        dbContext.AuditLogEntries.AddRange(
            AuditEntry(patientId, AuditActionTypes.View, true, DateTimeOffset.UtcNow),
            AuditEntry(patientId, AuditActionTypes.AccessDenied, false, DateTimeOffset.UtcNow.AddMinutes(-1)));
        await dbContext.SaveChangesAsync();

        var controller = CreateController(
            dbContext,
            new FakeAuthorizationService(AccessDecision.Allow(
                HealthPermissions.ViewAuditLog,
                AccessScopes.AllPatients)),
            new FakeAuditLogService());

        var result = await controller.GetAuditLog(
            new AuditLogQueryRequest
            {
                PatientId = patientId,
                Succeeded = false,
                PageSize = 500
            },
            CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<AuditLogQueryResponse>(okResult.Value);
        var item = Assert.Single(response.Items);
        Assert.False(item.Succeeded);
        Assert.Equal(100, response.PageSize);
    }

    [Fact]
    public async Task GetAuditLog_WhenFromIsAfterTo_ReturnsBadRequest()
    {
        await using var dbContext = CreateDbContext();
        var controller = CreateController(
            dbContext,
            new FakeAuthorizationService(AccessDecision.Allow(
                HealthPermissions.ViewAuditLog,
                AccessScopes.AllPatients)),
            new FakeAuditLogService());

        var result = await controller.GetAuditLog(
            new AuditLogQueryRequest
            {
                From = DateTimeOffset.UtcNow,
                To = DateTimeOffset.UtcNow.AddDays(-1)
            },
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    private static AuditLogController CreateController(
        AppDbContext dbContext,
        FakeAuthorizationService authorizationService,
        FakeAuditLogService auditLogService)
    {
        return new AuditLogController(
            dbContext,
            authorizationService,
            new FakeRequestContextProvider(),
            auditLogService);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static AuditLogEntry AuditEntry(
        Guid patientId,
        string actionType,
        bool succeeded,
        DateTimeOffset createdAt)
    {
        return new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            UserId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            PatientId = patientId,
            ProductCode = ProductCodes.InternalAdmin,
            ProductRole = ProductRoles.HealthCoreAdmin,
            ActionType = actionType,
            ResourceType = AuditResourceTypes.PatientProfile,
            Permission = HealthPermissions.ViewPatientProfile,
            AccessScope = AccessScopes.AllPatients,
            Succeeded = succeeded,
            FailureReason = succeeded ? null : "Denied.",
            IpAddress = "127.0.0.1",
            UserAgent = "test-agent",
            CorrelationId = "test-correlation",
            RequestPath = "/api/health-core/patients",
            HttpMethod = "GET",
            MetadataJson = "{\"raw\":\"not returned by review dto\"}",
            CreatedAt = createdAt
        };
    }

    private sealed class FakeAuthorizationService : IHealthCoreAuthorizationService
    {
        private readonly AccessDecision _decision;

        public FakeAuthorizationService(AccessDecision decision)
        {
            _decision = decision;
        }

        public Task<AccessDecision> CanAccessPatientAsync(
            HealthCoreAuthorizationContext context,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_decision);
        }

        public Task<AccessDecision> HasPermissionAsync(
            HealthCoreAuthorizationContext context,
            string permission,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_decision);
        }

        public Task<AccessDecision> CanViewPatientSectionAsync(
            HealthCoreAuthorizationContext context,
            string sectionPermission,
            string? sensitivityLevel = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_decision);
        }

        public Task<AccessDecision> CanEditPatientSectionAsync(
            HealthCoreAuthorizationContext context,
            string sectionPermission,
            string? sensitivityLevel = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_decision);
        }

        public Task<AccessDecision> CanViewSensitivityLevelAsync(
            HealthCoreAuthorizationContext context,
            string? sensitivityLevel,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_decision);
        }
    }

    private sealed class FakeRequestContextProvider : IHealthCoreRequestContextProvider
    {
        private readonly HealthCoreRequestContext _requestContext = new()
        {
            UserId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            ProductCode = ProductCodes.InternalAdmin,
            ProductRole = ProductRoles.HealthCoreAdmin,
            CorrelationId = "test-correlation",
            IpAddress = "127.0.0.1",
            UserAgent = "test-agent",
            RequestPath = "/api/health-core/audit-log",
            HttpMethod = "GET",
            IsAuthenticated = true
        };

        public HealthCoreRequestContext GetCurrent()
        {
            return _requestContext;
        }

        public HealthCoreAuthorizationContext CreateAuthorizationContext(Guid patientId)
        {
            return new HealthCoreAuthorizationContext
            {
                UserId = _requestContext.UserId,
                ServiceAccountId = _requestContext.ServiceAccountId,
                PatientId = patientId,
                ProductCode = _requestContext.ProductCode ?? string.Empty,
                ProductRole = _requestContext.ProductRole ?? string.Empty
            };
        }
    }

    private sealed class FakeAuditLogService : IAuditLogService
    {
        public List<AuditLogRequest> Requests { get; } = new();

        public Task LogAsync(AuditLogRequest request, CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            return Task.CompletedTask;
        }
    }
}
