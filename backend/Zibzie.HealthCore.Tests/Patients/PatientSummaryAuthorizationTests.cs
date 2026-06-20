using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zibzie.HealthCore.Api.Controllers;
using Zibzie.HealthCore.Application.MedicalHistory;
using Zibzie.HealthCore.Application.Patients;
using Zibzie.HealthCore.Application.Security;
using Zibzie.HealthCore.Domain.Common;
using Zibzie.HealthCore.Domain.Security;
using Zibzie.HealthCore.Infrastructure.Persistence;

namespace Zibzie.HealthCore.Tests.Patients;

public class PatientSummaryAuthorizationTests
{
    [Fact]
    public async Task GetPatientSummary_WhenAuthorizationDenied_ReturnsForbiddenAuditsDeniedAndDoesNotCallService()
    {
        await using var dbContext = CreateDbContext();
        var patientId = Guid.NewGuid();
        var summaryService = new FakePatientSummaryService();
        var auditLog = new FakeAuditLogService();
        var controller = CreateController(
            dbContext,
            summaryService,
            new FakeAuthorizationService(AccessDecision.Deny("No summary access.")),
            auditLog);

        var result = await controller.GetPatientSummary(patientId);

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(403, objectResult.StatusCode);
        Assert.Equal(0, summaryService.CallCount);

        var auditRequest = Assert.Single(auditLog.Requests);
        Assert.Equal(AuditActionTypes.AccessDenied, auditRequest.ActionType);
        Assert.Equal(AuditResourceTypes.PatientSummary, auditRequest.ResourceType);
        Assert.Equal(HealthPermissions.ViewPatientSummary, auditRequest.Permission);
        Assert.Equal(patientId, auditRequest.PatientId);
        Assert.False(auditRequest.Succeeded);
        Assert.Equal("No summary access.", auditRequest.FailureReason);
    }

    [Fact]
    public async Task GetPatientSummary_WhenAuthorizationAllowed_ReturnsSummaryAndAuditsSuccess()
    {
        await using var dbContext = CreateDbContext();
        var patientId = Guid.NewGuid();
        var expectedSummary = CreateSummary(patientId);
        var summaryService = new FakePatientSummaryService
        {
            Result = expectedSummary
        };
        var auditLog = new FakeAuditLogService();
        var controller = CreateController(
            dbContext,
            summaryService,
            new FakeAuthorizationService(AccessDecision.Allow(HealthPermissions.ViewPatientSummary, AccessScopes.AllPatients)),
            auditLog);

        var result = await controller.GetPatientSummary(patientId);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<PatientSummaryDto>(okResult.Value);
        Assert.Equal(expectedSummary.Id, dto.Id);
        Assert.Equal(expectedSummary.FirstName, dto.FirstName);
        Assert.Single(dto.Conditions);
        Assert.Single(dto.Allergies);
        Assert.Single(dto.CurrentMedications);
        Assert.Equal(1, summaryService.CallCount);

        var auditRequest = Assert.Single(auditLog.Requests);
        Assert.Equal(AuditActionTypes.View, auditRequest.ActionType);
        Assert.Equal(AuditResourceTypes.PatientSummary, auditRequest.ResourceType);
        Assert.Equal(HealthPermissions.ViewPatientSummary, auditRequest.Permission);
        Assert.Equal(patientId, auditRequest.PatientId);
        Assert.True(auditRequest.Succeeded);
        Assert.Equal(AccessScopes.AllPatients, auditRequest.AccessScope);
        Assert.Equal(ProductCodes.InternalAdmin, auditRequest.ProductCode);
        Assert.Equal(ProductRoles.HealthCoreAdmin, auditRequest.ProductRole);
        Assert.Equal("test-service", auditRequest.ServiceAccountId);
    }

    private static PatientsController CreateController(
        AppDbContext dbContext,
        FakePatientSummaryService summaryService,
        FakeAuthorizationService authorizationService,
        FakeAuditLogService auditLogService)
    {
        return new PatientsController(
            dbContext,
            summaryService,
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

    private static PatientSummaryDto CreateSummary(Guid patientId)
    {
        return new PatientSummaryDto
        {
            Id = patientId,
            FirstName = "Test",
            LastName = "Patient",
            BirthDate = new DateOnly(1984, 1, 1),
            NationalCode = "0012345678",
            Gender = "Male",
            BloodType = "O+",
            MobileNumber = "09120000000",
            Email = "test@example.com",
            Conditions = new List<ConditionDto>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    PatientProfileId = patientId,
                    Name = "Diabetes",
                    Status = "Active",
                    SourceType = SourceTypes.PatientSelfReport,
                    VerificationStatus = VerificationStatuses.SelfReported,
                    SensitivityLevel = SensitivityLevels.Normal
                }
            },
            Allergies = new List<AllergyDto>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    PatientProfileId = patientId,
                    Allergen = "Penicillin",
                    Severity = "High",
                    SourceType = SourceTypes.PatientSelfReport,
                    VerificationStatus = VerificationStatuses.SelfReported,
                    SensitivityLevel = SensitivityLevels.Normal
                }
            },
            CurrentMedications = new List<MedicationDto>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    PatientProfileId = patientId,
                    Name = "Metformin",
                    IsCurrent = true,
                    SourceType = SourceTypes.PatientSelfReport,
                    VerificationStatus = VerificationStatuses.SelfReported,
                    SensitivityLevel = SensitivityLevels.Normal
                }
            }
        };
    }

    private sealed class FakePatientSummaryService : IPatientSummaryService
    {
        public int CallCount { get; private set; }

        public PatientSummaryDto? Result { get; init; }

        public Task<PatientSummaryDto?> GetPatientSummaryAsync(
            Guid patientId,
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
