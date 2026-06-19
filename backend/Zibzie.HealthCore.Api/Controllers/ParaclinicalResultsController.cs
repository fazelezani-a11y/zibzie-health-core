using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zibzie.HealthCore.Application.ParaclinicalResults;
using Zibzie.HealthCore.Domain.Entities;
using Zibzie.HealthCore.Infrastructure.Persistence;

namespace Zibzie.HealthCore.Api.Controllers;

[ApiController]
public class ParaclinicalResultsController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly IParaclinicalResultService _paraclinicalResultService;

    public ParaclinicalResultsController(
        AppDbContext dbContext,
        IParaclinicalResultService paraclinicalResultService)
    {
        _dbContext = dbContext;
        _paraclinicalResultService = paraclinicalResultService;
    }

    [HttpGet("api/health-core/patients/{patientId:guid}/paraclinical-results")]
    public async Task<ActionResult<List<ParaclinicalResultDto>>> GetPatientParaclinicalResults(
        Guid patientId,
        [FromQuery] string? resultType = null,
        [FromQuery] string? verificationStatus = null,
        [FromQuery] string? sensitivityLevel = null,
        [FromQuery] bool? requiresFollowUp = null)
    {
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

        var now = DateTimeOffset.UtcNow;

        result.IsDeleted = true;
        result.DeletedAt = now;
        result.UpdatedAt = now;

        await _dbContext.SaveChangesAsync();

        return NoContent();
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
