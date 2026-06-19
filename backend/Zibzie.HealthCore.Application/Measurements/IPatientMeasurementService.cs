namespace Zibzie.HealthCore.Application.Measurements;

public interface IPatientMeasurementService
{
    Task<PatientMeasurementDto?> CreatePatientMeasurementAsync(
        Guid patientId,
        CreatePatientMeasurementRequest request,
        CancellationToken cancellationToken = default);
}
