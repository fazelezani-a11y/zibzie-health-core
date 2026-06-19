using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zibzie.HealthCore.Application.MedicalHistory;
using Zibzie.HealthCore.Domain.Common;
using Zibzie.HealthCore.Domain.Entities;
using Zibzie.HealthCore.Infrastructure.Persistence;

namespace Zibzie.HealthCore.Api.Controllers;

[ApiController]
public class AllergiesController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public AllergiesController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("api/health-core/patients/{patientId:guid}/allergies")]
    public async Task<ActionResult<List<AllergyDto>>> GetPatientAllergies(Guid patientId)
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

        allergy.IsDeleted = true;
        allergy.DeletedAt = DateTime.UtcNow;
        allergy.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        return NoContent();
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
