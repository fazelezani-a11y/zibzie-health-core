namespace Zibzie.HealthCore.Application.Documents;

public interface IPatientDocumentService
{
    Task<PatientDocumentDto?> CreatePatientDocumentAsync(
        Guid patientId,
        CreatePatientDocumentRequest request,
        CancellationToken cancellationToken = default);
}
