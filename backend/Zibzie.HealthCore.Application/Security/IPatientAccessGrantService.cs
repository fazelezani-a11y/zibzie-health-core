namespace Zibzie.HealthCore.Application.Security;

public interface IPatientAccessGrantService
{
    Task<PatientAccessGrantServiceResult<IReadOnlyCollection<PatientAccessGrantDto>>> ListForPatientAsync(
        Guid patientId,
        CancellationToken cancellationToken = default);

    Task<PatientAccessGrantServiceResult<PatientAccessGrantDto>> GetByIdAsync(
        Guid grantId,
        CancellationToken cancellationToken = default);

    Task<PatientAccessGrantServiceResult<PatientAccessGrantDto>> CreateAsync(
        Guid patientId,
        CreatePatientAccessGrantRequest request,
        HealthCoreRequestContext requestContext,
        CancellationToken cancellationToken = default);

    Task<PatientAccessGrantServiceResult<PatientAccessGrantDto>> RevokeAsync(
        Guid grantId,
        RevokePatientAccessGrantRequest request,
        HealthCoreRequestContext requestContext,
        CancellationToken cancellationToken = default);
}
