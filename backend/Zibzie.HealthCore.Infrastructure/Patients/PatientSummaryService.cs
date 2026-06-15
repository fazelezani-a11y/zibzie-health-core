using Microsoft.EntityFrameworkCore;
using Zibzie.HealthCore.Application.MedicalHistory;
using Zibzie.HealthCore.Application.Patients;
using Zibzie.HealthCore.Infrastructure.Persistence;

namespace Zibzie.HealthCore.Infrastructure.Patients;

public class PatientSummaryService : IPatientSummaryService
{
    private readonly AppDbContext _dbContext;

    public PatientSummaryService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PatientSummaryDto?> GetPatientSummaryAsync(
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        var patient = await _dbContext.PatientProfiles
            .Include(x => x.ContactInfo)
            .FirstOrDefaultAsync(x => x.Id == patientId && x.IsActive, cancellationToken);

        if (patient is null)
        {
            return null;
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
            .ToListAsync(cancellationToken);

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
            .ToListAsync(cancellationToken);

        var currentMedications = await _dbContext.Medications
            .Where(x => x.PatientProfileId == patientId && !x.IsDeleted && x.IsCurrent)
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
            .ToListAsync(cancellationToken);

        return new PatientSummaryDto
        {
            Id = patient.Id,
            FirstName = patient.FirstName,
            LastName = patient.LastName,
            BirthDate = patient.BirthDate,
            NationalCode = patient.NationalCode,
            Gender = patient.Gender,
            BloodType = patient.BloodType,
            MobileNumber = patient.ContactInfo?.MobileNumber ?? string.Empty,
            Email = patient.ContactInfo?.Email,
            EmergencyContactName = patient.ContactInfo?.EmergencyContactName,
            EmergencyContactPhone = patient.ContactInfo?.EmergencyContactPhone,
            HomeAddress = patient.ContactInfo?.HomeAddress,
            WorkAddress = patient.ContactInfo?.WorkAddress,
            Conditions = conditions,
            Allergies = allergies,
            CurrentMedications = currentMedications
        };
    }
}
