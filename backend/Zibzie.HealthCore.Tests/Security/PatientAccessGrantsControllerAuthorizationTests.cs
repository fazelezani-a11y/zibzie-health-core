using Microsoft.AspNetCore.Mvc;
using Zibzie.HealthCore.Api.Controllers;
using Zibzie.HealthCore.Application.Security;
using Zibzie.HealthCore.Domain.Security;

namespace Zibzie.HealthCore.Tests.Security;

public class PatientAccessGrantsControllerAuthorizationTests
{
    [Fact]
    public async Task GetPatientAccessGrants_WhenAuthorizationDenied_ReturnsForbiddenAndAuditsDenied()
    {
        var patientId = Guid.NewGuid();
        var service = new FakePatientAccessGrantService();
        var auditLog = new FakeAuditLogService();
        var controller = CreateController(
            service,
            new FakeAuthorizationService(AccessDecision.Deny("Grant view denied.")),
            auditLog);

        var result = await controller.GetPatientAccessGrants(patientId, CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(403, objectResult.StatusCode);
        Assert.Equal(0, service.ListCallCount);

        var auditRequest = Assert.Single(auditLog.Requests);
        Assert.Equal(AuditActionTypes.AccessDenied, auditRequest.ActionType);
        Assert.Equal(AuditResourceTypes.PatientAccessGrant, auditRequest.ResourceType);
        Assert.Equal(HealthPermissions.ViewPatientAccessGrants, auditRequest.Permission);
        Assert.Equal(patientId, auditRequest.PatientId);
        Assert.False(auditRequest.Succeeded);
        Assert.Equal("Grant view denied.", auditRequest.FailureReason);
    }

    [Fact]
    public async Task GetPatientAccessGrants_WhenAuthorizationAllowed_ReturnsGrantsAndAuditsSuccess()
    {
        var patientId = Guid.NewGuid();
        var service = new FakePatientAccessGrantService
        {
            ListResult = PatientAccessGrantServiceResult<IReadOnlyCollection<PatientAccessGrantDto>>.Success(
                new[]
                {
                    GrantDto(patientId)
                })
        };
        var auditLog = new FakeAuditLogService();
        var controller = CreateController(
            service,
            new FakeAuthorizationService(AccessDecision.Allow(
                HealthPermissions.ViewPatientAccessGrants,
                AccessScopes.AllPatients)),
            auditLog);

        var result = await controller.GetPatientAccessGrants(patientId, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var grants = Assert.IsAssignableFrom<IReadOnlyCollection<PatientAccessGrantDto>>(okResult.Value);
        Assert.Single(grants);

        var auditRequest = Assert.Single(auditLog.Requests);
        Assert.Equal(AuditActionTypes.View, auditRequest.ActionType);
        Assert.True(auditRequest.Succeeded);
        Assert.Equal(AccessScopes.AllPatients, auditRequest.AccessScope);
    }

    [Fact]
    public async Task CreatePatientAccessGrant_WhenAuthorizationDenied_ReturnsForbiddenAuditsDeniedAndDoesNotCallService()
    {
        var patientId = Guid.NewGuid();
        var service = new FakePatientAccessGrantService();
        var auditLog = new FakeAuditLogService();
        var controller = CreateController(
            service,
            new FakeAuthorizationService(AccessDecision.Deny("Grant create denied.")),
            auditLog);

        var result = await controller.CreatePatientAccessGrant(
            patientId,
            CreateGrantRequest(),
            CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(403, objectResult.StatusCode);
        Assert.Equal(0, service.CreateCallCount);

        var auditRequest = Assert.Single(auditLog.Requests);
        Assert.Equal(AuditActionTypes.AccessDenied, auditRequest.ActionType);
        Assert.Equal(HealthPermissions.CreatePatientAccessGrant, auditRequest.Permission);
        Assert.Equal(patientId, auditRequest.PatientId);
        Assert.False(auditRequest.Succeeded);
    }

    [Fact]
    public async Task CreatePatientAccessGrant_WhenAuthorizationAllowed_ReturnsCreatedAndAuditsGrantAccess()
    {
        var patientId = Guid.NewGuid();
        var grant = GrantDto(patientId);
        var service = new FakePatientAccessGrantService
        {
            CreateResult = PatientAccessGrantServiceResult<PatientAccessGrantDto>.Success(grant)
        };
        var auditLog = new FakeAuditLogService();
        var controller = CreateController(
            service,
            new FakeAuthorizationService(AccessDecision.Allow(
                HealthPermissions.CreatePatientAccessGrant,
                AccessScopes.AllPatients)),
            auditLog);

        var result = await controller.CreatePatientAccessGrant(
            patientId,
            CreateGrantRequest(),
            CancellationToken.None);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var dto = Assert.IsType<PatientAccessGrantDto>(createdResult.Value);
        Assert.Equal(grant.Id, dto.Id);
        Assert.Equal(1, service.CreateCallCount);

        var auditRequest = Assert.Single(auditLog.Requests);
        Assert.Equal(AuditActionTypes.GrantAccess, auditRequest.ActionType);
        Assert.Equal(AuditResourceTypes.PatientAccessGrant, auditRequest.ResourceType);
        Assert.Equal(HealthPermissions.CreatePatientAccessGrant, auditRequest.Permission);
        Assert.Equal(grant.Id, auditRequest.ResourceId);
        Assert.True(auditRequest.Succeeded);
        Assert.NotNull(auditRequest.MetadataJson);
    }

    [Fact]
    public async Task RevokePatientAccessGrant_WhenAuthorizationDenied_ReturnsForbiddenAuditsDeniedAndDoesNotRevoke()
    {
        var grant = GrantDto(Guid.NewGuid());
        var service = new FakePatientAccessGrantService
        {
            GetResult = PatientAccessGrantServiceResult<PatientAccessGrantDto>.Success(grant)
        };
        var auditLog = new FakeAuditLogService();
        var controller = CreateController(
            service,
            new FakeAuthorizationService(AccessDecision.Deny("Grant revoke denied.")),
            auditLog);

        var result = await controller.RevokePatientAccessGrant(
            grant.Id,
            new RevokePatientAccessGrantRequest(),
            CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(403, objectResult.StatusCode);
        Assert.Equal(0, service.RevokeCallCount);

        var auditRequest = Assert.Single(auditLog.Requests);
        Assert.Equal(AuditActionTypes.AccessDenied, auditRequest.ActionType);
        Assert.Equal(HealthPermissions.RevokePatientAccessGrant, auditRequest.Permission);
        Assert.Equal(grant.Id, auditRequest.ResourceId);
        Assert.Equal(grant.PatientId, auditRequest.PatientId);
        Assert.False(auditRequest.Succeeded);
    }

    [Fact]
    public async Task RevokePatientAccessGrant_WhenAuthorizationAllowed_ReturnsOkAndAuditsRevokeAccess()
    {
        var grant = GrantDto(Guid.NewGuid());
        grant.IsActive = false;
        grant.RevokedAt = DateTimeOffset.UtcNow;
        var service = new FakePatientAccessGrantService
        {
            GetResult = PatientAccessGrantServiceResult<PatientAccessGrantDto>.Success(grant),
            RevokeResult = PatientAccessGrantServiceResult<PatientAccessGrantDto>.Success(grant)
        };
        var auditLog = new FakeAuditLogService();
        var controller = CreateController(
            service,
            new FakeAuthorizationService(AccessDecision.Allow(
                HealthPermissions.RevokePatientAccessGrant,
                AccessScopes.AllPatients)),
            auditLog);

        var result = await controller.RevokePatientAccessGrant(
            grant.Id,
            new RevokePatientAccessGrantRequest(),
            CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<PatientAccessGrantDto>(okResult.Value);
        Assert.Equal(grant.Id, dto.Id);
        Assert.Equal(1, service.RevokeCallCount);

        var auditRequest = Assert.Single(auditLog.Requests);
        Assert.Equal(AuditActionTypes.RevokeAccess, auditRequest.ActionType);
        Assert.Equal(AuditResourceTypes.PatientAccessGrant, auditRequest.ResourceType);
        Assert.Equal(HealthPermissions.RevokePatientAccessGrant, auditRequest.Permission);
        Assert.Equal(grant.Id, auditRequest.ResourceId);
        Assert.True(auditRequest.Succeeded);
    }

    private static PatientAccessGrantsController CreateController(
        FakePatientAccessGrantService service,
        FakeAuthorizationService authorizationService,
        FakeAuditLogService auditLogService)
    {
        return new PatientAccessGrantsController(
            service,
            authorizationService,
            new FakeRequestContextProvider(),
            auditLogService);
    }

    private static CreatePatientAccessGrantRequest CreateGrantRequest()
    {
        return new CreatePatientAccessGrantRequest
        {
            ServiceAccountId = "digicare-careteam-service",
            ProductCode = ProductCodes.DigiCare,
            ProductRole = ProductRoles.DigiCareCaseManager,
            Scope = AccessScopes.AssignedPatientsOnly,
            Reason = AuthorizationReasons.CareTeamOperation
        };
    }

    private static PatientAccessGrantDto GrantDto(Guid patientId)
    {
        return new PatientAccessGrantDto
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            ProductCode = ProductCodes.DigiCare,
            ProductRole = ProductRoles.DigiCareCaseManager,
            Scope = AccessScopes.AssignedPatientsOnly,
            Reason = AuthorizationReasons.CareTeamOperation,
            ServiceAccountId = "digicare-careteam-service",
            IsActive = true,
            ValidFrom = DateTimeOffset.UtcNow.AddMinutes(-5),
            GrantedAt = DateTimeOffset.UtcNow.AddMinutes(-5)
        };
    }

    private sealed class FakePatientAccessGrantService : IPatientAccessGrantService
    {
        public int ListCallCount { get; private set; }

        public int CreateCallCount { get; private set; }

        public int RevokeCallCount { get; private set; }

        public PatientAccessGrantServiceResult<IReadOnlyCollection<PatientAccessGrantDto>> ListResult { get; init; }
            = PatientAccessGrantServiceResult<IReadOnlyCollection<PatientAccessGrantDto>>.Success(
                Array.Empty<PatientAccessGrantDto>());

        public PatientAccessGrantServiceResult<PatientAccessGrantDto> GetResult { get; init; }
            = PatientAccessGrantServiceResult<PatientAccessGrantDto>.Failure(
                PatientAccessGrantServiceError.NotFound,
                "Patient access grant not found.");

        public PatientAccessGrantServiceResult<PatientAccessGrantDto> CreateResult { get; init; }
            = PatientAccessGrantServiceResult<PatientAccessGrantDto>.Failure(
                PatientAccessGrantServiceError.Validation,
                "Invalid grant.");

        public PatientAccessGrantServiceResult<PatientAccessGrantDto> RevokeResult { get; init; }
            = PatientAccessGrantServiceResult<PatientAccessGrantDto>.Failure(
                PatientAccessGrantServiceError.Validation,
                "Invalid grant.");

        public Task<PatientAccessGrantServiceResult<IReadOnlyCollection<PatientAccessGrantDto>>> ListForPatientAsync(
            Guid patientId,
            CancellationToken cancellationToken = default)
        {
            ListCallCount++;
            return Task.FromResult(ListResult);
        }

        public Task<PatientAccessGrantServiceResult<PatientAccessGrantDto>> GetByIdAsync(
            Guid grantId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(GetResult);
        }

        public Task<PatientAccessGrantServiceResult<PatientAccessGrantDto>> CreateAsync(
            Guid patientId,
            CreatePatientAccessGrantRequest request,
            HealthCoreRequestContext requestContext,
            CancellationToken cancellationToken = default)
        {
            CreateCallCount++;
            return Task.FromResult(CreateResult);
        }

        public Task<PatientAccessGrantServiceResult<PatientAccessGrantDto>> RevokeAsync(
            Guid grantId,
            RevokePatientAccessGrantRequest request,
            HealthCoreRequestContext requestContext,
            CancellationToken cancellationToken = default)
        {
            RevokeCallCount++;
            return Task.FromResult(RevokeResult);
        }
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
            RequestPath = "/api/test",
            HttpMethod = "GET"
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
