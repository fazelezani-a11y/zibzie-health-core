using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zibzie.HealthCore.Api.Controllers;
using Zibzie.HealthCore.Application.Documents;
using Zibzie.HealthCore.Application.Security;
using Zibzie.HealthCore.Domain.Common;
using Zibzie.HealthCore.Domain.Entities;
using Zibzie.HealthCore.Domain.Security;
using Zibzie.HealthCore.Infrastructure.Persistence;

namespace Zibzie.HealthCore.Tests.Documents;

public class PatientDocumentsControllerAuthorizationTests
{
    [Fact]
    public async Task GetPatientDocuments_WhenAuthorizationDenied_ReturnsForbiddenAndAuditsDenied()
    {
        await using var dbContext = CreateDbContext();
        var patientId = Guid.NewGuid();
        var auditLog = new FakeAuditLogService();
        var controller = CreateController(
            dbContext,
            new FakePatientDocumentService(),
            new FakeAuthorizationService(AccessDecision.Deny("No document access.")),
            auditLog);

        var result = await controller.GetPatientDocuments(patientId);

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(403, objectResult.StatusCode);

        var auditRequest = Assert.Single(auditLog.Requests);
        Assert.Equal(AuditActionTypes.AccessDenied, auditRequest.ActionType);
        Assert.Equal(AuditResourceTypes.Document, auditRequest.ResourceType);
        Assert.Equal(HealthPermissions.ViewDocuments, auditRequest.Permission);
        Assert.Equal(patientId, auditRequest.PatientId);
        Assert.False(auditRequest.Succeeded);
        Assert.Equal("No document access.", auditRequest.FailureReason);
    }

    [Fact]
    public async Task CreatePatientDocument_WhenAuthorizationDenied_ReturnsForbiddenAuditsDeniedAndDoesNotCallService()
    {
        await using var dbContext = CreateDbContext();
        var patientId = Guid.NewGuid();
        var documentService = new FakePatientDocumentService();
        var auditLog = new FakeAuditLogService();
        var controller = CreateController(
            dbContext,
            documentService,
            new FakeAuthorizationService(AccessDecision.Deny("No upload access.")),
            auditLog);

        var result = await controller.CreatePatientDocument(patientId, CreateDocumentRequest());

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(403, objectResult.StatusCode);
        Assert.Equal(0, documentService.CallCount);

        var auditRequest = Assert.Single(auditLog.Requests);
        Assert.Equal(AuditActionTypes.AccessDenied, auditRequest.ActionType);
        Assert.Equal(HealthPermissions.UploadDocuments, auditRequest.Permission);
        Assert.Equal(patientId, auditRequest.PatientId);
        Assert.False(auditRequest.Succeeded);
        Assert.Equal("No upload access.", auditRequest.FailureReason);
    }

    [Fact]
    public async Task GetPatientDocuments_WhenAuthorizationAllowed_ReturnsDocumentsAndAuditsSuccess()
    {
        await using var dbContext = CreateDbContext();
        var patient = await SeedPatientAsync(dbContext);
        var document = await SeedDocumentAsync(dbContext, patient.Id);
        var auditLog = new FakeAuditLogService();
        var controller = CreateController(
            dbContext,
            new FakePatientDocumentService(),
            new FakeAuthorizationService(AccessDecision.Allow(HealthPermissions.ViewDocuments, AccessScopes.AllPatients)),
            auditLog);

        var result = await controller.GetPatientDocuments(patient.Id);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var documents = Assert.IsType<List<PatientDocumentDto>>(okResult.Value);
        var dto = Assert.Single(documents);
        Assert.Equal(document.Id, dto.Id);

        var auditRequest = Assert.Single(auditLog.Requests);
        Assert.Equal(AuditActionTypes.View, auditRequest.ActionType);
        Assert.Equal(AuditResourceTypes.Document, auditRequest.ResourceType);
        Assert.Equal(HealthPermissions.ViewDocuments, auditRequest.Permission);
        Assert.Equal(patient.Id, auditRequest.PatientId);
        Assert.True(auditRequest.Succeeded);
        Assert.Equal(AccessScopes.AllPatients, auditRequest.AccessScope);
    }

    [Fact]
    public async Task CreatePatientDocument_WhenAuthorizationAllowed_ReturnsCreatedAndAuditsSuccess()
    {
        await using var dbContext = CreateDbContext();
        var patientId = Guid.NewGuid();
        var createdDocumentId = Guid.NewGuid();
        var documentService = new FakePatientDocumentService
        {
            Result = new PatientDocumentDto
            {
                Id = createdDocumentId,
                PatientProfileId = patientId,
                DocumentType = "LabReport",
                Title = "CBC",
                SourceType = SourceTypes.Manual,
                VerificationStatus = VerificationStatuses.Unverified,
                SensitivityLevel = SensitivityLevels.Normal,
                CreatedAt = DateTimeOffset.UtcNow
            }
        };
        var auditLog = new FakeAuditLogService();
        var controller = CreateController(
            dbContext,
            documentService,
            new FakeAuthorizationService(AccessDecision.Allow(HealthPermissions.UploadDocuments, AccessScopes.AllPatients)),
            auditLog);

        var result = await controller.CreatePatientDocument(patientId, CreateDocumentRequest());

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var dto = Assert.IsType<PatientDocumentDto>(createdResult.Value);
        Assert.Equal(createdDocumentId, dto.Id);
        Assert.Equal(1, documentService.CallCount);

        var auditRequest = Assert.Single(auditLog.Requests);
        Assert.Equal(AuditActionTypes.Create, auditRequest.ActionType);
        Assert.Equal(AuditResourceTypes.Document, auditRequest.ResourceType);
        Assert.Equal(HealthPermissions.UploadDocuments, auditRequest.Permission);
        Assert.Equal(patientId, auditRequest.PatientId);
        Assert.Equal(createdDocumentId, auditRequest.ResourceId);
        Assert.True(auditRequest.Succeeded);
    }

    private static PatientDocumentsController CreateController(
        AppDbContext dbContext,
        FakePatientDocumentService documentService,
        FakeAuthorizationService authorizationService,
        FakeAuditLogService auditLogService)
    {
        return new PatientDocumentsController(
            dbContext,
            documentService,
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

    private static CreatePatientDocumentRequest CreateDocumentRequest()
    {
        return new CreatePatientDocumentRequest
        {
            DocumentType = "LabReport",
            Title = "CBC",
            SourceType = SourceTypes.Manual,
            VerificationStatus = VerificationStatuses.Unverified,
            SensitivityLevel = SensitivityLevels.Normal
        };
    }

    private static async Task<PatientProfile> SeedPatientAsync(AppDbContext dbContext)
    {
        var patient = new PatientProfile
        {
            Id = Guid.NewGuid(),
            FirstName = "Test",
            LastName = "Patient",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.PatientProfiles.Add(patient);
        await dbContext.SaveChangesAsync();

        return patient;
    }

    private static async Task<PatientDocument> SeedDocumentAsync(AppDbContext dbContext, Guid patientId)
    {
        var document = new PatientDocument
        {
            Id = Guid.NewGuid(),
            PatientProfileId = patientId,
            DocumentType = "LabReport",
            Title = "CBC",
            SourceType = SourceTypes.Manual,
            VerificationStatus = VerificationStatuses.Unverified,
            SensitivityLevel = SensitivityLevels.Normal,
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.PatientDocuments.Add(document);
        await dbContext.SaveChangesAsync();

        return document;
    }

    private sealed class FakePatientDocumentService : IPatientDocumentService
    {
        public int CallCount { get; private set; }

        public PatientDocumentDto? Result { get; init; }

        public Task<PatientDocumentDto?> CreatePatientDocumentAsync(
            Guid patientId,
            CreatePatientDocumentRequest request,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(Result);
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
            ServiceAccountId = "test-service",
            ProductCode = ProductCodes.InternalAdmin,
            ProductRole = ProductRoles.HealthCoreAdmin,
            CorrelationId = "test-correlation",
            IpAddress = "127.0.0.1",
            UserAgent = "test-agent",
            RequestPath = "/api/test",
            HttpMethod = "GET",
            IsFallbackContext = true
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
