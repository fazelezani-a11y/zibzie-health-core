namespace Zibzie.HealthCore.Application.Reminders;

public interface IPatientReminderService
{
    Task<PatientReminderDto?> CreatePatientReminderAsync(
        Guid patientId,
        CreatePatientReminderRequest request,
        CancellationToken cancellationToken = default);
}
