namespace Zibzie.HealthCore.Application.Patients;

public interface IPatientSummaryService
{
    Task<PatientSummaryDto?> GetPatientSummaryAsync(
        Guid patientId,
        CancellationToken cancellationToken = default);
}
