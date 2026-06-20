using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zibzie.HealthCore.Application.ParaclinicalResults;
using Zibzie.HealthCore.Application.Security;
using Zibzie.HealthCore.Domain.Entities;
using Zibzie.HealthCore.Domain.Security;
using Zibzie.HealthCore.Infrastructure.Persistence;

namespace Zibzie.HealthCore.Api.Controllers;

[ApiController]
public class ParaclinicalResultsController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly IParaclinicalResultService _paraclinicalResultService;
    private readonly IHealthCoreAuthorizationService _authorizationService;
    private readonly IHealthCoreRequestContextProvider _requestContextProvider;
    private readonly IAuditLogService _auditLogService;

    public ParaclinicalResultsController(
        AppDbContext dbContext,
        IParaclinicalResultService paraclinicalResultService,
        IHealthCoreAuthorizationService authorizationService,
        IHealthCoreRequestContextProvider requestContextProvider,
        IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _paraclinicalResultService = paraclinicalResultService;
        _authorizationService = authorizationService;
        _requestContextProvider = requestContextProvider;
        _auditLogService = auditLogService;
    }

    [HttpGet("api/health-core/patients/{patientId:guid}/paraclinical-results")]
    public async Task<ActionResult<List<ParaclinicalResultDto>>> GetPatientParaclinicalResults(
        Guid patientId,
        [FromQuery] string? resultType = null,
        [FromQuery] string? verificationStatus = null,
        [FromQuery] string? sensitivityLevel = null,
        [FromQuery] bool? requiresFollowUp = null)
    {
        var requestContext = _requestContextProvider.GetCurrent();
        var authorizationContext = _requestContextProvider.CreateAuthorizationContext(patientId);
        var accessDecision = await _authorizationService.CanViewPatientSectionAsync(
            authorizationContext,
            HealthPermissions.ViewParaclinicalResults,
            sensitivityLevel);

        if (!accessDecision.IsAllowed)
        {
            await LogParaclinicalAuditAsync(
                requestContext,
                patientId,
                null,
                AuditActionTypes.AccessDenied,
                HealthPermissions.ViewParaclinicalResults,
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

        var query = _dbContext.PatientParaclinicalResults
            .Where(x => x.PatientProfileId == patientId && !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(resultType))
        {
            var normalizedResultType = resultType.Trim();
            query = query.Where(x => x.ResultType == normalizedResultType);
        }

        if (!string.IsNullOrWhiteSpace(verificationStatus))
        {
            var normalizedVerificationStatus = verificationStatus.Trim();
            query = query.Where(x => x.VerificationStatus == normalizedVerificationStatus);
        }

        if (!string.IsNullOrWhiteSpace(sensitivityLevel))
        {
            var normalizedSensitivityLevel = sensitivityLevel.Trim();
            query = query.Where(x => x.SensitivityLevel == normalizedSensitivityLevel);
        }

        if (requiresFollowUp.HasValue)
        {
            query = query.Where(x => x.RequiresFollowUp == requiresFollowUp.Value);
        }

        var results = await query
            .Include(x => x.LabItems)
            .OrderBy(x => x.ResultDate == null)
            .ThenByDescending(x => x.ResultDate)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync();

        await LogParaclinicalAuditAsync(
            requestContext,
            patientId,
            null,
            AuditActionTypes.View,
            HealthPermissions.ViewParaclinicalResults,
            true,
            accessDecision);

        return Ok(results.Select(ToDto).ToList());
    }

    [HttpPost("api/health-core/patients/{patientId:guid}/paraclinical-results")]
    public async Task<ActionResult<ParaclinicalResultDto>> CreateParaclinicalResult(
        Guid patientId,
        CreateParaclinicalResultRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ResultType))
        {
            return BadRequest(new
            {
                message = "Result type is required."
            });
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest(new
            {
                message = "Paraclinical result title is required."
            });
        }

        var requestContext = _requestContextProvider.GetCurrent();
        var authorizationContext = _requestContextProvider.CreateAuthorizationContext(patientId);
        var accessDecision = await _authorizationService.CanEditPatientSectionAsync(
            authorizationContext,
            HealthPermissions.EditParaclinicalResults,
            request.SensitivityLevel);

        if (!accessDecision.IsAllowed)
        {
            await LogParaclinicalAuditAsync(
                requestContext,
                patientId,
                null,
                AuditActionTypes.AccessDenied,
                HealthPermissions.EditParaclinicalResults,
                false,
                accessDecision);

            return AccessDenied();
        }

        var createResult = await _paraclinicalResultService.CreateParaclinicalResultAsync(patientId, request);

        if (createResult.Status == CreateParaclinicalResultStatus.PatientNotFound)
        {
            return NotFound(new
            {
                message = "Patient not found."
            });
        }

        if (createResult.Status == CreateParaclinicalResultStatus.LinkedDocumentNotFound)
        {
            return BadRequest(new
            {
                message = "Linked document was not found for this patient."
            });
        }

        if (createResult.Status == CreateParaclinicalResultStatus.LabItemTestNameRequired)
        {
            return BadRequest(new
            {
                message = "Lab item test name is required."
            });
        }

        var dto = createResult.Result!;

        await LogParaclinicalAuditAsync(
            requestContext,
            patientId,
            dto.Id,
            AuditActionTypes.Create,
            HealthPermissions.EditParaclinicalResults,
            true,
            accessDecision);

        return CreatedAtAction(
            nameof(GetParaclinicalResult),
            new { resultId = dto.Id },
            dto);
    }

    [HttpGet("api/health-core/paraclinical-results/{resultId:guid}")]
    public async Task<ActionResult<ParaclinicalResultDto>> GetParaclinicalResult(Guid resultId)
    {
        var result = await _dbContext.PatientParaclinicalResults
            .Include(x => x.LabItems)
            .FirstOrDefaultAsync(x => x.Id == resultId && !x.IsDeleted);

        if (result is null)
        {
            return NotFound(new
            {
                message = "Paraclinical result not found."
            });
        }

        var requestContext = _requestContextProvider.GetCurrent();
        var authorizationContext = _requestContextProvider.CreateAuthorizationContext(result.PatientProfileId);
        var accessDecision = await _authorizationService.CanViewPatientSectionAsync(
            authorizationContext,
            HealthPermissions.ViewParaclinicalResults,
            result.SensitivityLevel);

        if (!accessDecision.IsAllowed)
        {
            await LogParaclinicalAuditAsync(
                requestContext,
                result.PatientProfileId,
                result.Id,
                AuditActionTypes.AccessDenied,
                HealthPermissions.ViewParaclinicalResults,
                false,
                accessDecision);

            return AccessDenied();
        }

        await LogParaclinicalAuditAsync(
            requestContext,
            result.PatientProfileId,
            result.Id,
            AuditActionTypes.View,
            HealthPermissions.ViewParaclinicalResults,
            true,
            accessDecision);

        return Ok(ToDto(result));
    }

    [HttpPut("api/health-core/paraclinical-results/{resultId:guid}")]
    public async Task<ActionResult<ParaclinicalResultDto>> UpdateParaclinicalResult(
        Guid resultId,
        UpdateParaclinicalResultRequest request)
    {
        var result = await _dbContext.PatientParaclinicalResults
            .Include(x => x.LabItems)
            .FirstOrDefaultAsync(x => x.Id == resultId && !x.IsDeleted);

        if (result is null)
        {
            return NotFound(new
            {
                message = "Paraclinical result not found."
            });
        }

        var requestContext = _requestContextProvider.GetCurrent();
        var authorizationContext = _requestContextProvider.CreateAuthorizationContext(result.PatientProfileId);
        var accessDecision = await _authorizationService.CanEditPatientSectionAsync(
            authorizationContext,
            HealthPermissions.EditParaclinicalResults,
            request.SensitivityLevel ?? result.SensitivityLevel);

        if (!accessDecision.IsAllowed)
        {
            await LogParaclinicalAuditAsync(
                requestContext,
                result.PatientProfileId,
                result.Id,
                AuditActionTypes.AccessDenied,
                HealthPermissions.EditParaclinicalResults,
                false,
                accessDecision);

            return AccessDenied();
        }

        if (request.ResultType is not null)
        {
            if (string.IsNullOrWhiteSpace(request.ResultType))
            {
                return BadRequest(new
                {
                    message = "Result type is required."
                });
            }

            result.ResultType = request.ResultType.Trim();
        }

        if (request.Title is not null)
        {
            if (string.IsNullOrWhiteSpace(request.Title))
            {
                return BadRequest(new
                {
                    message = "Paraclinical result title is required."
                });
            }

            result.Title = request.Title.Trim();
        }

        if (request.SourceType is not null)
        {
            if (string.IsNullOrWhiteSpace(request.SourceType))
            {
                return BadRequest(new
                {
                    message = "Source type is required."
                });
            }

            result.SourceType = request.SourceType.Trim();
        }

        if (request.VerificationStatus is not null)
        {
            if (string.IsNullOrWhiteSpace(request.VerificationStatus))
            {
                return BadRequest(new
                {
                    message = "Verification status is required."
                });
            }

            result.VerificationStatus = request.VerificationStatus.Trim();
        }

        if (request.SensitivityLevel is not null)
        {
            if (string.IsNullOrWhiteSpace(request.SensitivityLevel))
            {
                return BadRequest(new
                {
                    message = "Sensitivity level is required."
                });
            }

            result.SensitivityLevel = request.SensitivityLevel.Trim();
        }

        if (request.LinkedDocumentId.HasValue)
        {
            if (!await LinkedDocumentBelongsToPatientAsync(result.PatientProfileId, request.LinkedDocumentId.Value))
            {
                return BadRequest(new
                {
                    message = "Linked document was not found for this patient."
                });
            }

            result.LinkedDocumentId = request.LinkedDocumentId;
        }

        if (request.PerformedAt.HasValue)
        {
            result.PerformedAt = request.PerformedAt.Value.ToUniversalTime();
        }

        if (request.ResultDate.HasValue)
        {
            result.ResultDate = request.ResultDate.Value.ToUniversalTime();
        }

        if (request.Description is not null)
        {
            result.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        }

        if (request.ProviderName is not null)
        {
            result.ProviderName = string.IsNullOrWhiteSpace(request.ProviderName) ? null : request.ProviderName.Trim();
        }

        if (request.Summary is not null)
        {
            result.Summary = string.IsNullOrWhiteSpace(request.Summary) ? null : request.Summary.Trim();
        }

        if (request.Interpretation is not null)
        {
            result.Interpretation = string.IsNullOrWhiteSpace(request.Interpretation) ? null : request.Interpretation.Trim();
        }

        if (request.IsAbnormal.HasValue)
        {
            result.IsAbnormal = request.IsAbnormal;
        }

        if (request.RequiresFollowUp.HasValue)
        {
            result.RequiresFollowUp = request.RequiresFollowUp.Value;
        }

        if (request.FollowUpNote is not null)
        {
            result.FollowUpNote = string.IsNullOrWhiteSpace(request.FollowUpNote) ? null : request.FollowUpNote.Trim();
        }

        result.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync();

        await LogParaclinicalAuditAsync(
            requestContext,
            result.PatientProfileId,
            result.Id,
            AuditActionTypes.Update,
            HealthPermissions.EditParaclinicalResults,
            true,
            accessDecision);

        return Ok(ToDto(result));
    }

    [HttpDelete("api/health-core/paraclinical-results/{resultId:guid}")]
    public async Task<IActionResult> DeleteParaclinicalResult(Guid resultId)
    {
        var result = await _dbContext.PatientParaclinicalResults
            .FirstOrDefaultAsync(x => x.Id == resultId && !x.IsDeleted);

        if (result is null)
        {
            return NotFound(new
            {
                message = "Paraclinical result not found."
            });
        }

        var requestContext = _requestContextProvider.GetCurrent();
        var authorizationContext = _requestContextProvider.CreateAuthorizationContext(result.PatientProfileId);
        var accessDecision = await _authorizationService.CanEditPatientSectionAsync(
            authorizationContext,
            HealthPermissions.EditParaclinicalResults,
            result.SensitivityLevel);

        if (!accessDecision.IsAllowed)
        {
            await LogParaclinicalAuditAsync(
                requestContext,
                result.PatientProfileId,
                result.Id,
                AuditActionTypes.AccessDenied,
                HealthPermissions.EditParaclinicalResults,
                false,
                accessDecision);

            return AccessDenied();
        }

        var now = DateTimeOffset.UtcNow;

        result.IsDeleted = true;
        result.DeletedAt = now;
        result.UpdatedAt = now;

        await _dbContext.SaveChangesAsync();

        await LogParaclinicalAuditAsync(
            requestContext,
            result.PatientProfileId,
            result.Id,
            AuditActionTypes.Delete,
            HealthPermissions.EditParaclinicalResults,
            true,
            accessDecision);

        return NoContent();
    }

    private async Task LogParaclinicalAuditAsync(
        HealthCoreRequestContext requestContext,
        Guid? patientId,
        Guid? resultId,
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
            ResourceType = AuditResourceTypes.ParaclinicalResult,
            ResourceId = resultId,
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

    private async Task<bool> LinkedDocumentBelongsToPatientAsync(
        Guid patientId,
        Guid linkedDocumentId)
    {
        return await _dbContext.PatientDocuments
            .AnyAsync(x =>
                x.Id == linkedDocumentId &&
                x.PatientProfileId == patientId &&
                !x.IsDeleted);
    }

    private static ParaclinicalResultDto ToDto(PatientParaclinicalResult result)
    {
        return new ParaclinicalResultDto
        {
            Id = result.Id,
            PatientProfileId = result.PatientProfileId,
            ResultType = result.ResultType,
            Title = result.Title,
            Description = result.Description,
            PerformedAt = result.PerformedAt,
            ResultDate = result.ResultDate,
            ProviderName = result.ProviderName,
            LinkedDocumentId = result.LinkedDocumentId,
            Summary = result.Summary,
            Interpretation = result.Interpretation,
            IsAbnormal = result.IsAbnormal,
            RequiresFollowUp = result.RequiresFollowUp,
            FollowUpNote = result.FollowUpNote,
            SourceType = result.SourceType,
            VerificationStatus = result.VerificationStatus,
            SensitivityLevel = result.SensitivityLevel,
            CreatedAt = result.CreatedAt,
            UpdatedAt = result.UpdatedAt,
            LabItems = result.LabItems
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.CreatedAt)
                .Select(ToDto)
                .ToList()
        };
    }

    private static LabResultItemDto ToDto(PatientLabResultItem item)
    {
        return new LabResultItemDto
        {
            Id = item.Id,
            PatientParaclinicalResultId = item.PatientParaclinicalResultId,
            TestName = item.TestName,
            Value = item.Value,
            NumericValue = item.NumericValue,
            Unit = item.Unit,
            ReferenceRange = item.ReferenceRange,
            IsAbnormal = item.IsAbnormal,
            Interpretation = item.Interpretation,
            DisplayOrder = item.DisplayOrder,
            CreatedAt = item.CreatedAt
        };
    }
}
