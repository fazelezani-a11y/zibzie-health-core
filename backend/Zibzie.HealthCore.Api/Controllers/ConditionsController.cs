using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zibzie.HealthCore.Application.MedicalHistory;
using Zibzie.HealthCore.Domain.Common;
using Zibzie.HealthCore.Domain.Entities;
using Zibzie.HealthCore.Infrastructure.Persistence;

namespace Zibzie.HealthCore.Api.Controllers;

[ApiController]
public class ConditionsController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public ConditionsController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("api/health-core/patients/{patientId:guid}/conditions")]
    public async Task<ActionResult<List<ConditionDto>>> GetPatientConditions(Guid patientId)
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

        var conditions = await _dbContext.Conditions
            .Where(x => x.PatientProfileId == patientId && !x.IsDeleted)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new ConditionDto
            {
                Id = x.Id,
                PatientProfileId = x.PatientProfileId,
                Name = x.Name,
                Status = x.Status,
                StartedYear = x.StartedYear,
                TreatmentSummary = x.TreatmentSummary,
                ClinicianNote = x.ClinicianNote,
                SourceType = x.SourceType,
                VerificationStatus = x.VerificationStatus,
                SensitivityLevel = x.SensitivityLevel,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync();

        return Ok(conditions);
    }

    [HttpPost("api/health-core/patients/{patientId:guid}/conditions")]
    public async Task<ActionResult<ConditionDto>> CreateCondition(
        Guid patientId,
        CreateConditionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new
            {
                message = "Condition name is required."
            });
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

        var condition = new Condition
        {
            Id = Guid.NewGuid(),
            PatientProfileId = patientId,
            Name = request.Name.Trim(),
            Status = string.IsNullOrWhiteSpace(request.Status) ? null : request.Status.Trim(),
            StartedYear = request.StartedYear,
            TreatmentSummary = string.IsNullOrWhiteSpace(request.TreatmentSummary) ? null : request.TreatmentSummary.Trim(),
            ClinicianNote = string.IsNullOrWhiteSpace(request.ClinicianNote) ? null : request.ClinicianNote.Trim(),
            SourceType = string.IsNullOrWhiteSpace(request.SourceType) ? SourceTypes.PatientSelfReport : request.SourceType.Trim(),
            VerificationStatus = string.IsNullOrWhiteSpace(request.VerificationStatus) ? VerificationStatuses.SelfReported : request.VerificationStatus.Trim(),
            SensitivityLevel = string.IsNullOrWhiteSpace(request.SensitivityLevel) ? SensitivityLevels.Normal : request.SensitivityLevel.Trim(),
            CreatedAt = now
        };

        _dbContext.Conditions.Add(condition);
        await _dbContext.SaveChangesAsync();

        var dto = ToDto(condition);

        return CreatedAtAction(
            nameof(GetPatientConditions),
            new { patientId },
            dto);
    }

    [HttpPut("api/health-core/conditions/{conditionId:guid}")]
    public async Task<ActionResult<ConditionDto>> UpdateCondition(
        Guid conditionId,
        UpdateConditionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new
            {
                message = "Condition name is required."
            });
        }

        var condition = await _dbContext.Conditions
            .FirstOrDefaultAsync(x => x.Id == conditionId && !x.IsDeleted);

        if (condition is null)
        {
            return NotFound(new
            {
                message = "Condition not found."
            });
        }

        condition.Name = request.Name.Trim();
        condition.Status = string.IsNullOrWhiteSpace(request.Status) ? null : request.Status.Trim();
        condition.StartedYear = request.StartedYear;
        condition.TreatmentSummary = string.IsNullOrWhiteSpace(request.TreatmentSummary) ? null : request.TreatmentSummary.Trim();
        condition.ClinicianNote = string.IsNullOrWhiteSpace(request.ClinicianNote) ? null : request.ClinicianNote.Trim();
        condition.SourceType = string.IsNullOrWhiteSpace(request.SourceType) ? SourceTypes.PatientSelfReport : request.SourceType.Trim();
        condition.VerificationStatus = string.IsNullOrWhiteSpace(request.VerificationStatus) ? VerificationStatuses.SelfReported : request.VerificationStatus.Trim();
        condition.SensitivityLevel = string.IsNullOrWhiteSpace(request.SensitivityLevel) ? SensitivityLevels.Normal : request.SensitivityLevel.Trim();
        condition.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        return Ok(ToDto(condition));
    }

    [HttpDelete("api/health-core/conditions/{conditionId:guid}")]
    public async Task<IActionResult> DeleteCondition(Guid conditionId)
    {
        var condition = await _dbContext.Conditions
            .FirstOrDefaultAsync(x => x.Id == conditionId && !x.IsDeleted);

        if (condition is null)
        {
            return NotFound(new
            {
                message = "Condition not found."
            });
        }

        condition.IsDeleted = true;
        condition.DeletedAt = DateTime.UtcNow;
        condition.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        return NoContent();
    }

    private static ConditionDto ToDto(Condition condition)
    {
        return new ConditionDto
        {
            Id = condition.Id,
            PatientProfileId = condition.PatientProfileId,
            Name = condition.Name,
            Status = condition.Status,
            StartedYear = condition.StartedYear,
            TreatmentSummary = condition.TreatmentSummary,
            ClinicianNote = condition.ClinicianNote,
            SourceType = condition.SourceType,
            VerificationStatus = condition.VerificationStatus,
            SensitivityLevel = condition.SensitivityLevel,
            CreatedAt = condition.CreatedAt,
            UpdatedAt = condition.UpdatedAt
        };
    }
}
