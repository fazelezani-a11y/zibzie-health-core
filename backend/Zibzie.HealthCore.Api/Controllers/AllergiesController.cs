using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zibzie.HealthCore.Application.MedicalHistory;
using Zibzie.HealthCore.Application.Security;
using Zibzie.HealthCore.Domain.Common;
using Zibzie.HealthCore.Domain.Entities;
using Zibzie.HealthCore.Domain.Security;
using Zibzie.HealthCore.Infrastructure.Persistence;

namespace Zibzie.HealthCore.Api.Controllers;

[ApiController]
public class AllergiesController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly IHealthCoreAuthorizationService _authorizationService;
    private readonly IHealthCoreRequestContextProvider _requestContextProvider;
    private readonly IAuditLogService _auditLogService;

    public AllergiesController(
        AppDbContext dbContext,
        IHealthCoreAuthorizationService authorizationService,
        IHealthCoreRequestContextProvider requestContextProvider,
        IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _authorizationService = authorizationService;
        _requestContextProvider = requestContextProvider;
        _auditLogService = auditLogService;
    }

    [HttpGet("api/health-core/patients/{patientId:guid}/allergies")]
    public async Task<ActionResult<List<AllergyDto>>> GetPatientAllergies(Guid patientId)
    {
        var requestContext = _requestContextProvider.GetCurrent();
        var authorizationContext = _requestContextProvider.CreateAuthorizationContext(patientId);
        var accessDecision = await _authorizationService.CanViewPatientSectionAsync(
            authorizationContext,
            HealthPermissions.ViewMedicalHistory);

        if (!accessDecision.IsAllowed)
        {
            await LogAllergyAuditAsync(
                requestContext,
                patientId,
                null,
                AuditActionTypes.AccessDenied,
                HealthPermissions.ViewMedicalHistory,
                false,
                accessDecision);

            return AccessDenied();
        }

        var patientExists = await _dbContext.PatientProfiles
            .AnyAsync(x => x.Id == patientId && x.IsActive);

        if (!patientExists)
        {
            return NotFound(new
            {
                message = "Patient not found."
            });
        }

        var allergies = await _dbContext.Allergies
            .Where(x => x.PatientProfileId == patientId && !x.IsDeleted)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new AllergyDto
            {
                Id = x.Id,
                PatientProfileId = x.PatientProfileId,
                Allergen = x.Allergen,
                AllergyType = x.AllergyType,
                Severity = x.Severity,
                ReactionDescription = x.ReactionDescription,
                ClinicianNote = x.ClinicianNote,
                SourceType = x.SourceType,
                VerificationStatus = x.VerificationStatus,
                SensitivityLevel = x.SensitivityLevel,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync();

        await LogAllergyAuditAsync(
            requestContext,
            patientId,
            null,
            AuditActionTypes.View,
            HealthPermissions.ViewMedicalHistory,
            true,
            accessDecision);

        return Ok(allergies);
    }

    [HttpPost("api/health-core/patients/{patientId:guid}/allergies")]
    public async Task<ActionResult<AllergyDto>> CreateAllergy(
        Guid patientId,
        CreateAllergyRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Allergen))
        {
            return BadRequest(new
            {
                message = "Allergen is required."
            });
        }

        var requestContext = _requestContextProvider.GetCurrent();
        var authorizationContext = _requestContextProvider.CreateAuthorizationContext(patientId);
        var accessDecision = await _authorizationService.CanEditPatientSectionAsync(
            authorizationContext,
            HealthPermissions.EditMedicalHistory,
            request.SensitivityLevel);

        if (!accessDecision.IsAllowed)
        {
            await LogAllergyAuditAsync(
                requestContext,
                patientId,
                null,
                AuditActionTypes.AccessDenied,
                HealthPermissions.EditMedicalHistory,
                false,
                accessDecision);

            return AccessDenied();
        }

        var patientExists = await _dbContext.PatientProfiles
            .AnyAsync(x => x.Id == patientId && x.IsActive);

        if (!patientExists)
        {
            return NotFound(new
            {
                message = "Patient not found."
            });
        }

        var now = DateTime.UtcNow;

        var allergy = new Allergy
        {
            Id = Guid.NewGuid(),
            PatientProfileId = patientId,
            Allergen = request.Allergen.Trim(),
            AllergyType = string.IsNullOrWhiteSpace(request.AllergyType) ? null : request.AllergyType.Trim(),
            Severity = string.IsNullOrWhiteSpace(request.Severity) ? null : request.Severity.Trim(),
            ReactionDescription = string.IsNullOrWhiteSpace(request.ReactionDescription) ? null : request.ReactionDescription.Trim(),
            ClinicianNote = string.IsNullOrWhiteSpace(request.ClinicianNote) ? null : request.ClinicianNote.Trim(),
            SourceType = string.IsNullOrWhiteSpace(request.SourceType) ? SourceTypes.PatientSelfReport : request.SourceType.Trim(),
            VerificationStatus = string.IsNullOrWhiteSpace(request.VerificationStatus) ? VerificationStatuses.SelfReported : request.VerificationStatus.Trim(),
            SensitivityLevel = string.IsNullOrWhiteSpace(request.SensitivityLevel) ? SensitivityLevels.Normal : request.SensitivityLevel.Trim(),
            CreatedAt = now
        };

        _dbContext.Allergies.Add(allergy);
        await _dbContext.SaveChangesAsync();

        var dto = ToDto(allergy);

        await LogAllergyAuditAsync(
            requestContext,
            patientId,
            allergy.Id,
            AuditActionTypes.Create,
            HealthPermissions.EditMedicalHistory,
            true,
            accessDecision);

        return CreatedAtAction(
            nameof(GetPatientAllergies),
            new { patientId },
            dto);
    }

    [HttpPut("api/health-core/allergies/{allergyId:guid}")]
    public async Task<ActionResult<AllergyDto>> UpdateAllergy(
        Guid allergyId,
        UpdateAllergyRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Allergen))
        {
            return BadRequest(new
            {
                message = "Allergen is required."
            });
        }

        var allergy = await _dbContext.Allergies
            .FirstOrDefaultAsync(x => x.Id == allergyId && !x.IsDeleted);

        if (allergy is null)
        {
            return NotFound(new
            {
                message = "Allergy not found."
            });
        }

        var requestContext = _requestContextProvider.GetCurrent();
        var authorizationContext = _requestContextProvider.CreateAuthorizationContext(allergy.PatientProfileId);
        var accessDecision = await _authorizationService.CanEditPatientSectionAsync(
            authorizationContext,
            HealthPermissions.EditMedicalHistory,
            request.SensitivityLevel ?? allergy.SensitivityLevel);

        if (!accessDecision.IsAllowed)
        {
            await LogAllergyAuditAsync(
                requestContext,
                allergy.PatientProfileId,
                allergy.Id,
                AuditActionTypes.AccessDenied,
                HealthPermissions.EditMedicalHistory,
                false,
                accessDecision);

            return AccessDenied();
        }

        allergy.Allergen = request.Allergen.Trim();
        allergy.AllergyType = string.IsNullOrWhiteSpace(request.AllergyType) ? null : request.AllergyType.Trim();
        allergy.Severity = string.IsNullOrWhiteSpace(request.Severity) ? null : request.Severity.Trim();
        allergy.ReactionDescription = string.IsNullOrWhiteSpace(request.ReactionDescription) ? null : request.ReactionDescription.Trim();
        allergy.ClinicianNote = string.IsNullOrWhiteSpace(request.ClinicianNote) ? null : request.ClinicianNote.Trim();
        allergy.SourceType = string.IsNullOrWhiteSpace(request.SourceType) ? SourceTypes.PatientSelfReport : request.SourceType.Trim();
        allergy.VerificationStatus = string.IsNullOrWhiteSpace(request.VerificationStatus) ? VerificationStatuses.SelfReported : request.VerificationStatus.Trim();
        allergy.SensitivityLevel = string.IsNullOrWhiteSpace(request.SensitivityLevel) ? SensitivityLevels.Normal : request.SensitivityLevel.Trim();
        allergy.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        await LogAllergyAuditAsync(
            requestContext,
            allergy.PatientProfileId,
            allergy.Id,
            AuditActionTypes.Update,
            HealthPermissions.EditMedicalHistory,
            true,
            accessDecision);

        return Ok(ToDto(allergy));
    }

    [HttpDelete("api/health-core/allergies/{allergyId:guid}")]
    public async Task<IActionResult> DeleteAllergy(Guid allergyId)
    {
        var allergy = await _dbContext.Allergies
            .FirstOrDefaultAsync(x => x.Id == allergyId && !x.IsDeleted);

        if (allergy is null)
        {
            return NotFound(new
            {
                message = "Allergy not found."
            });
        }

        var requestContext = _requestContextProvider.GetCurrent();
        var authorizationContext = _requestContextProvider.CreateAuthorizationContext(allergy.PatientProfileId);
        var accessDecision = await _authorizationService.CanEditPatientSectionAsync(
            authorizationContext,
            HealthPermissions.EditMedicalHistory,
            allergy.SensitivityLevel);

        if (!accessDecision.IsAllowed)
        {
            await LogAllergyAuditAsync(
                requestContext,
                allergy.PatientProfileId,
                allergy.Id,
                AuditActionTypes.AccessDenied,
                HealthPermissions.EditMedicalHistory,
                false,
                accessDecision);

            return AccessDenied();
        }

        allergy.IsDeleted = true;
        allergy.DeletedAt = DateTime.UtcNow;
        allergy.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        await LogAllergyAuditAsync(
            requestContext,
            allergy.PatientProfileId,
            allergy.Id,
            AuditActionTypes.Delete,
            HealthPermissions.EditMedicalHistory,
            true,
            accessDecision);

        return NoContent();
    }

    private async Task LogAllergyAuditAsync(
        HealthCoreRequestContext requestContext,
        Guid? patientId,
        Guid? allergyId,
        string actionType,
        string permission,
        bool succeeded,
        AccessDecision? accessDecision)
    {
        await _auditLogService.LogAsync(new AuditLogRequest
        {
            UserId = requestContext.UserId,
            ServiceAccountId = requestContext.ServiceAccountId,
            PatientId = patientId,
            ProductCode = requestContext.ProductCode,
            ProductRole = requestContext.ProductRole,
            ActionType = actionType,
            ResourceType = AuditResourceTypes.Allergy,
            ResourceId = allergyId,
            Permission = permission,
            AccessScope = accessDecision?.MatchedScope,
            Succeeded = succeeded,
            FailureReason = succeeded ? null : accessDecision?.DenialReason ?? "Access denied.",
            IpAddress = requestContext.IpAddress,
            UserAgent = requestContext.UserAgent,
            CorrelationId = requestContext.CorrelationId,
            RequestPath = requestContext.RequestPath,
            HttpMethod = requestContext.HttpMethod
        });
    }

    private ObjectResult AccessDenied()
    {
        return StatusCode(StatusCodes.Status403Forbidden, new
        {
            message = "Access denied."
        });
    }

    private static AllergyDto ToDto(Allergy allergy)
    {
        return new AllergyDto
        {
            Id = allergy.Id,
            PatientProfileId = allergy.PatientProfileId,
            Allergen = allergy.Allergen,
            AllergyType = allergy.AllergyType,
            Severity = allergy.Severity,
            ReactionDescription = allergy.ReactionDescription,
            ClinicianNote = allergy.ClinicianNote,
            SourceType = allergy.SourceType,
            VerificationStatus = allergy.VerificationStatus,
            SensitivityLevel = allergy.SensitivityLevel,
            CreatedAt = allergy.CreatedAt,
            UpdatedAt = allergy.UpdatedAt
        };
    }
}
