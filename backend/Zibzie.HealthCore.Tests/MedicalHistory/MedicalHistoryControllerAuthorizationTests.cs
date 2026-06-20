using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zibzie.HealthCore.Api.Controllers;
using Zibzie.HealthCore.Application.MedicalHistory;
using Zibzie.HealthCore.Application.Security;
using Zibzie.HealthCore.Domain.Common;
using Zibzie.HealthCore.Domain.Entities;
using Zibzie.HealthCore.Domain.Security;
using Zibzie.HealthCore.Infrastructure.Persistence;

namespace Zibzie.HealthCore.Tests.MedicalHistory;

public class MedicalHistoryControllerAuthorizationTests
{
    [Fact]
    public async Task GetPatientConditions_WhenAuthorizationDenied_ReturnsForbiddenAndAuditsDenied()
    {
        await using var dbContext = CreateDbContext();
        var patientId = Guid.NewGuid();
        var auditLog = new FakeAuditLogService();
        var controller = CreateConditionsController(
            dbContext,
            new FakeAuthorizationService(AccessDecision.Deny("No medical history access.")),
            auditLog);

        var result = await controller.GetPatientConditions(patientId);

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(403, objectResult.StatusCode);

        var auditRequest = Assert.Single(auditLog.Requests);
        Assert.Equal(AuditActionTypes.AccessDenied, auditRequest.ActionType);
        Assert.Equal(AuditResourceTypes.Condition, auditRequest.ResourceType);
        Assert.Equal(HealthPermissions.ViewMedicalHistory, auditRequest.Permission);
        Assert.Equal(patientId, auditRequest.PatientId);
        Assert.False(auditRequest.Succeeded);
        Assert.Equal("No medical history access.", auditRequest.FailureReason);
    }

    [Fact]
    public async Task CreateCondition_WhenAuthorizationDenied_ReturnsForbiddenAuditsDeniedAndDoesNotCreateRecord()
    {
        await using var dbContext = CreateDbContext();
        var patientId = Guid.NewGuid();
        var auditLog = new FakeAuditLogService();
        var controller = CreateConditionsController(
            dbContext,
            new FakeAuthorizationService(AccessDecision.Deny("No medical history edit access.")),
            auditLog);

        var result = await controller.CreateCondition(patientId, CreateConditionRequest());

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(403, objectResult.StatusCode);
        Assert.Empty(await dbContext.Conditions.ToListAsync());

        var auditRequest = Assert.Single(auditLog.Requests);
        Assert.Equal(AuditActionTypes.AccessDenied, auditRequest.ActionType);
        Assert.Equal(AuditResourceTypes.Condition, auditRequest.ResourceType);
        Assert.Equal(HealthPermissions.EditMedicalHistory, auditRequest.Permission);
        Assert.Equal(patientId, auditRequest.PatientId);
        Assert.False(auditRequest.Succeeded);
    }

    [Fact]
    public async Task GetPatientConditions_WhenAuthorizationAllowed_ReturnsRecordsAndAuditsSuccess()
    {
        await using var dbContext = CreateDbContext();
        var patient = await SeedPatientAsync(dbContext);
        var condition = await SeedConditionAsync(dbContext, patient.Id);
        var auditLog = new FakeAuditLogService();
        var controller = CreateConditionsController(
            dbContext,
            new FakeAuthorizationService(AccessDecision.Allow(HealthPermissions.ViewMedicalHistory, AccessScopes.AllPatients)),
            auditLog);

        var result = await controller.GetPatientConditions(patient.Id);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var records = Assert.IsType<List<ConditionDto>>(okResult.Value);
        var dto = Assert.Single(records);
        Assert.Equal(condition.Id, dto.Id);

        var auditRequest = Assert.Single(auditLog.Requests);
        Assert.Equal(AuditActionTypes.View, auditRequest.ActionType);
        Assert.Equal(AuditResourceTypes.Condition, auditRequest.ResourceType);
        Assert.Equal(HealthPermissions.ViewMedicalHistory, auditRequest.Permission);
        Assert.Equal(patient.Id, auditRequest.PatientId);
        Assert.True(auditRequest.Succeeded);
        Assert.Equal(AccessScopes.AllPatients, auditRequest.AccessScope);
    }

    [Fact]
    public async Task CreateCondition_WhenAuthorizationAllowed_CreatesRecordAndAuditsSuccess()
    {
        await using var dbContext = CreateDbContext();
        var patient = await SeedPatientAsync(dbContext);
        var auditLog = new FakeAuditLogService();
        var controller = CreateConditionsController(
            dbContext,
            new FakeAuthorizationService(AccessDecision.Allow(HealthPermissions.EditMedicalHistory, AccessScopes.AllPatients)),
            auditLog);

        var result = await controller.CreateCondition(patient.Id, CreateConditionRequest());

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var dto = Assert.IsType<ConditionDto>(createdResult.Value);
        Assert.Equal("Diabetes", dto.Name);
        Assert.Single(await dbContext.Conditions.ToListAsync());

        var auditRequest = Assert.Single(auditLog.Requests);
        Assert.Equal(AuditActionTypes.Create, auditRequest.ActionType);
        Assert.Equal(AuditResourceTypes.Condition, auditRequest.ResourceType);
        Assert.Equal(HealthPermissions.EditMedicalHistory, auditRequest.Permission);
        Assert.Equal(patient.Id, auditRequest.PatientId);
        Assert.Equal(dto.Id, auditRequest.ResourceId);
        Assert.True(auditRequest.Succeeded);
    }

    [Fact]
    public async Task UpdateCondition_WhenAuthorizationDenied_AuditsResolvedPatientAndConditionIds()
    {
        await using var dbContext = CreateDbContext();
        var patient = await SeedPatientAsync(dbContext);
        var condition = await SeedConditionAsync(dbContext, patient.Id);
        var auditLog = new FakeAuditLogService();
        var controller = CreateConditionsController(
            dbContext,
            new FakeAuthorizationService(AccessDecision.Deny("No condition update access.")),
            auditLog);

        var result = await controller.UpdateCondition(condition.Id, new UpdateConditionRequest
        {
            Name = "Updated diabetes"
        });

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(403, objectResult.StatusCode);

        var auditRequest = Assert.Single(auditLog.Requests);
        Assert.Equal(AuditActionTypes.AccessDenied, auditRequest.ActionType);
        Assert.Equal(AuditResourceTypes.Condition, auditRequest.ResourceType);
        Assert.Equal(HealthPermissions.EditMedicalHistory, auditRequest.Permission);
        Assert.Equal(patient.Id, auditRequest.PatientId);
        Assert.Equal(condition.Id, auditRequest.ResourceId);
        Assert.False(auditRequest.Succeeded);
    }

    [Fact]
    public async Task GetPatientAllergies_WhenAuthorizationDenied_ReturnsForbiddenAndAuditsDenied()
    {
        await using var dbContext = CreateDbContext();
        var patientId = Guid.NewGuid();
        var auditLog = new FakeAuditLogService();
        var controller = CreateAllergiesController(
            dbContext,
            new FakeAuthorizationService(AccessDecision.Deny("No allergy access.")),
            auditLog);

        var result = await controller.GetPatientAllergies(patientId);

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(403, objectResult.StatusCode);

        var auditRequest = Assert.Single(auditLog.Requests);
        Assert.Equal(AuditActionTypes.AccessDenied, auditRequest.ActionType);
        Assert.Equal(AuditResourceTypes.Allergy, auditRequest.ResourceType);
        Assert.Equal(HealthPermissions.ViewMedicalHistory, auditRequest.Permission);
        Assert.Equal(patientId, auditRequest.PatientId);
        Assert.False(auditRequest.Succeeded);
    }

    [Fact]
    public async Task CreateAllergy_WhenAuthorizationAllowed_CreatesRecordAndAuditsSuccess()
    {
        await using var dbContext = CreateDbContext();
        var patient = await SeedPatientAsync(dbContext);
        var auditLog = new FakeAuditLogService();
        var controller = CreateAllergiesController(
            dbContext,
            new FakeAuthorizationService(AccessDecision.Allow(HealthPermissions.EditMedicalHistory, AccessScopes.AllPatients)),
            auditLog);

        var result = await controller.CreateAllergy(patient.Id, CreateAllergyRequest());

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var dto = Assert.IsType<AllergyDto>(createdResult.Value);
        Assert.Equal("Penicillin", dto.Allergen);
        Assert.Single(await dbContext.Allergies.ToListAsync());

        var auditRequest = Assert.Single(auditLog.Requests);
        Assert.Equal(AuditActionTypes.Create, auditRequest.ActionType);
        Assert.Equal(AuditResourceTypes.Allergy, auditRequest.ResourceType);
        Assert.Equal(HealthPermissions.EditMedicalHistory, auditRequest.Permission);
        Assert.Equal(patient.Id, auditRequest.PatientId);
        Assert.Equal(dto.Id, auditRequest.ResourceId);
        Assert.True(auditRequest.Succeeded);
    }

    [Fact]
    public async Task GetPatientMedications_WhenAuthorizationDenied_ReturnsForbiddenAndAuditsDenied()
    {
        await using var dbContext = CreateDbContext();
        var patientId = Guid.NewGuid();
        var auditLog = new FakeAuditLogService();
        var controller = CreateMedicationsController(
            dbContext,
            new FakeAuthorizationService(AccessDecision.Deny("No medication access.")),
            auditLog);

        var result = await controller.GetPatientMedications(patientId);

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(403, objectResult.StatusCode);

        var auditRequest = Assert.Single(auditLog.Requests);
        Assert.Equal(AuditActionTypes.AccessDenied, auditRequest.ActionType);
        Assert.Equal(AuditResourceTypes.Medication, auditRequest.ResourceType);
        Assert.Equal(HealthPermissions.ViewMedicalHistory, auditRequest.Permission);
        Assert.Equal(patientId, auditRequest.PatientId);
        Assert.False(auditRequest.Succeeded);
    }

    [Fact]
    public async Task CreateMedication_WhenAuthorizationAllowed_CreatesRecordAndAuditsSuccess()
    {
        await using var dbContext = CreateDbContext();
        var patient = await SeedPatientAsync(dbContext);
        var auditLog = new FakeAuditLogService();
        var controller = CreateMedicationsController(
            dbContext,
            new FakeAuthorizationService(AccessDecision.Allow(HealthPermissions.EditMedicalHistory, AccessScopes.AllPatients)),
            auditLog);

        var result = await controller.CreateMedication(patient.Id, CreateMedicationRequest());

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var dto = Assert.IsType<MedicationDto>(createdResult.Value);
        Assert.Equal("Metformin", dto.Name);
        Assert.Single(await dbContext.Medications.ToListAsync());

        var auditRequest = Assert.Single(auditLog.Requests);
        Assert.Equal(AuditActionTypes.Create, auditRequest.ActionType);
        Assert.Equal(AuditResourceTypes.Medication, auditRequest.ResourceType);
        Assert.Equal(HealthPermissions.EditMedicalHistory, auditRequest.Permission);
        Assert.Equal(patient.Id, auditRequest.PatientId);
        Assert.Equal(dto.Id, auditRequest.ResourceId);
        Assert.True(auditRequest.Succeeded);
    }

    private static ConditionsController CreateConditionsController(
        AppDbContext dbContext,
        FakeAuthorizationService authorizationService,
        FakeAuditLogService auditLogService)
    {
        return new ConditionsController(
            dbContext,
            authorizationService,
            new FakeRequestContextProvider(),
            auditLogService);
    }

    private static AllergiesController CreateAllergiesController(
        AppDbContext dbContext,
        FakeAuthorizationService authorizationService,
        FakeAuditLogService auditLogService)
    {
        return new AllergiesController(
            dbContext,
            authorizationService,
            new FakeRequestContextProvider(),
            auditLogService);
    }

    private static MedicationsController CreateMedicationsController(
        AppDbContext dbContext,
        FakeAuthorizationService authorizationService,
        FakeAuditLogService auditLogService)
    {
        return new MedicationsController(
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

    private static CreateConditionRequest CreateConditionRequest()
    {
        return new CreateConditionRequest
        {
            Name = "Diabetes",
            Status = "Active",
            SourceType = SourceTypes.PatientSelfReport,
            VerificationStatus = VerificationStatuses.SelfReported,
            SensitivityLevel = SensitivityLevels.Normal
        };
    }

    private static CreateAllergyRequest CreateAllergyRequest()
    {
        return new CreateAllergyRequest
        {
            Allergen = "Penicillin",
            Severity = "High",
            SourceType = SourceTypes.PatientSelfReport,
            VerificationStatus = VerificationStatuses.SelfReported,
            SensitivityLevel = SensitivityLevels.Normal
        };
    }

    private static CreateMedicationRequest CreateMedicationRequest()
    {
        return new CreateMedicationRequest
        {
            Name = "Metformin",
            Dose = "500mg",
            Frequency = "Twice daily",
            IsCurrent = true,
            SourceType = SourceTypes.PatientSelfReport,
            VerificationStatus = VerificationStatuses.SelfReported,
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

    private static async Task<Condition> SeedConditionAsync(AppDbContext dbContext, Guid patientId)
    {
        var condition = new Condition
        {
            Id = Guid.NewGuid(),
            PatientProfileId = patientId,
            Name = "Diabetes",
            Status = "Active",
            SourceType = SourceTypes.PatientSelfReport,
            VerificationStatus = VerificationStatuses.SelfReported,
            SensitivityLevel = SensitivityLevels.Normal,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Conditions.Add(condition);
        await dbContext.SaveChangesAsync();

        return condition;
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
