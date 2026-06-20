namespace Zibzie.HealthCore.Domain.Security;

public static class AuditResourceTypes
{
    public const string PatientProfile = "PatientProfile";
    public const string MedicalHistory = "MedicalHistory";
    public const string Condition = "Condition";
    public const string Allergy = "Allergy";
    public const string Medication = "Medication";
    public const string Document = "Document";
    public const string ParaclinicalResult = "ParaclinicalResult";
    public const string LabResultItem = "LabResultItem";
    public const string CarePlanItem = "CarePlanItem";
    public const string Reminder = "Reminder";
    public const string Measurement = "Measurement";
    public const string TimelineEvent = "TimelineEvent";
    public const string PatientAccessGrant = "PatientAccessGrant";
    public const string AuditLog = "AuditLog";
    public const string ProductAccessProfile = "ProductAccessProfile";
    public const string SecuritySettings = "SecuritySettings";
    public const string PatientSummary = "PatientSummary";

    public static readonly IReadOnlyCollection<string> All = new[]
    {
        PatientProfile,
        MedicalHistory,
        Condition,
        Allergy,
        Medication,
        Document,
        ParaclinicalResult,
        LabResultItem,
        CarePlanItem,
        Reminder,
        Measurement,
        TimelineEvent,
        PatientAccessGrant,
        AuditLog,
        ProductAccessProfile,
        SecuritySettings,
        PatientSummary,
    };
}
