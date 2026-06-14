using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zibzie.HealthCore.Application.MedicalHistory;
using Zibzie.HealthCore.Domain.Entities;
using Zibzie.HealthCore.Infrastructure.Persistence;

namespace Zibzie.HealthCore.Api.Controllers;

[ApiController]
public class MedicationsController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public MedicationsController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("api/health-core/patients/{patientId:guid}/medications")]
    public async Task<ActionResult<List<MedicationDto>>> GetPatientMedications(Guid patientId)
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

        var medications = await _dbContext.Medications
            .Where(x => x.PatientProfileId == patientId && !x.IsDeleted)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new MedicationDto
            {
                Id = x.Id,
                PatientProfileId = x.PatientProfileId,
                Name = x.Name,
                Dose = x.Dose,
                Frequency = x.Frequency,
                Route = x.Route,
                Reason = x.Reason,
                StartDate = x.StartDate,
                EndDate = x.EndDate,
                IsCurrent = x.IsCurrent,
                ClinicianNote = x.ClinicianNote,
                SourceType = x.SourceType,
                VerificationStatus = x.VerificationStatus,
                SensitivityLevel = x.SensitivityLevel,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync();

        return Ok(medications);
    }

    [HttpPost("api/health-core/patients/{patientId:guid}/medications")]
    public async Task<ActionResult<MedicationDto>> CreateMedication(
        Guid patientId,
        CreateMedicationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new
            {
                message = "Medication name is required."
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

        var medication = new Medication
        {
            Id = Guid.NewGuid(),
            PatientProfileId = patientId,
            Name = request.Name.Trim(),
            Dose = string.IsNullOrWhiteSpace(request.Dose) ? null : request.Dose.Trim(),
            Frequency = string.IsNullOrWhiteSpace(request.Frequency) ? null : request.Frequency.Trim(),
            Route = string.IsNullOrWhiteSpace(request.Route) ? null : request.Route.Trim(),
            Reason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim(),
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            IsCurrent = request.IsCurrent,
            ClinicianNote = string.IsNullOrWhiteSpace(request.ClinicianNote) ? null : request.ClinicianNote.Trim(),
            SourceType = string.IsNullOrWhiteSpace(request.SourceType) ? "PatientSelfReport" : request.SourceType.Trim(),
            VerificationStatus = string.IsNullOrWhiteSpace(request.VerificationStatus) ? "SelfReported" : request.VerificationStatus.Trim(),
            SensitivityLevel = string.IsNullOrWhiteSpace(request.SensitivityLevel) ? "Normal" : request.SensitivityLevel.Trim(),
            CreatedAt = now
        };

        _dbContext.Medications.Add(medication);
        await _dbContext.SaveChangesAsync();

        var dto = ToDto(medication);

        return CreatedAtAction(
            nameof(GetPatientMedications),
            new { patientId },
            dto);
    }

    [HttpPut("api/health-core/medications/{medicationId:guid}")]
    public async Task<ActionResult<MedicationDto>> UpdateMedication(
        Guid medicationId,
        UpdateMedicationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new
            {
                message = "Medication name is required."
            });
        }

        var medication = await _dbContext.Medications
            .FirstOrDefaultAsync(x => x.Id == medicationId && !x.IsDeleted);

        if (medication is null)
        {
            return NotFound(new
            {
                message = "Medication not found."
            });
        }

        medication.Name = request.Name.Trim();
        medication.Dose = string.IsNullOrWhiteSpace(request.Dose) ? null : request.Dose.Trim();
        medication.Frequency = string.IsNullOrWhiteSpace(request.Frequency) ? null : request.Frequency.Trim();
        medication.Route = string.IsNullOrWhiteSpace(request.Route) ? null : request.Route.Trim();
        medication.Reason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim();
        medication.StartDate = request.StartDate;
        medication.EndDate = request.EndDate;
        medication.IsCurrent = request.IsCurrent;
        medication.ClinicianNote = string.IsNullOrWhiteSpace(request.ClinicianNote) ? null : request.ClinicianNote.Trim();
        medication.SourceType = string.IsNullOrWhiteSpace(request.SourceType) ? "PatientSelfReport" : request.SourceType.Trim();
        medication.VerificationStatus = string.IsNullOrWhiteSpace(request.VerificationStatus) ? "SelfReported" : request.VerificationStatus.Trim();
        medication.SensitivityLevel = string.IsNullOrWhiteSpace(request.SensitivityLevel) ? "Normal" : request.SensitivityLevel.Trim();
        medication.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        return Ok(ToDto(medication));
    }

    [HttpDelete("api/health-core/medications/{medicationId:guid}")]
    public async Task<IActionResult> DeleteMedication(Guid medicationId)
    {
        var medication = await _dbContext.Medications
            .FirstOrDefaultAsync(x => x.Id == medicationId && !x.IsDeleted);

        if (medication is null)
        {
            return NotFound(new
            {
                message = "Medication not found."
            });
        }

        medication.IsDeleted = true;
        medication.DeletedAt = DateTime.UtcNow;
        medication.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        return NoContent();
    }

    private static MedicationDto ToDto(Medication medication)
    {
        return new MedicationDto
        {
            Id = medication.Id,
            PatientProfileId = medication.PatientProfileId,
            Name = medication.Name,
            Dose = medication.Dose,
            Frequency = medication.Frequency,
            Route = medication.Route,
            Reason = medication.Reason,
            StartDate = medication.StartDate,
            EndDate = medication.EndDate,
            IsCurrent = medication.IsCurrent,
            ClinicianNote = medication.ClinicianNote,
            SourceType = medication.SourceType,
            VerificationStatus = medication.VerificationStatus,
            SensitivityLevel = medication.SensitivityLevel,
            CreatedAt = medication.CreatedAt,
            UpdatedAt = medication.UpdatedAt
        };
    }
}
