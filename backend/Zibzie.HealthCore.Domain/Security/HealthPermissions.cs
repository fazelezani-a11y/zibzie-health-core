namespace Zibzie.HealthCore.Domain.Security;

public static class HealthPermissions
{
    // Patient Profile
    public const string ViewPatientProfile = "ViewPatientProfile";
    public const string EditPatientProfile = "EditPatientProfile";
    public const string ViewPatientContactInfo = "ViewPatientContactInfo";
    public const string EditPatientContactInfo = "EditPatientContactInfo";
    public const string ViewPatientSummary = "ViewPatientSummary";
    public const string ViewPatientDirectory = "ViewPatientDirectory";

    // Medical History
    public const string ViewMedicalHistory = "ViewMedicalHistory";
    public const string EditMedicalHistory = "EditMedicalHistory";
    public const string ViewSensitiveMedicalHistory = "ViewSensitiveMedicalHistory";
    public const string VerifyMedicalHistory = "VerifyMedicalHistory";

    // Documents / Evidence
    public const string ViewDocuments = "ViewDocuments";
    public const string UploadDocuments = "UploadDocuments";
    public const string EditDocuments = "EditDocuments";
    public const string DeleteDocuments = "DeleteDocuments";
    public const string VerifyDocuments = "VerifyDocuments";
    public const string ShareDocuments = "ShareDocuments";

    // Paraclinical Results
    public const string ViewParaclinicalResults = "ViewParaclinicalResults";
    public const string EditParaclinicalResults = "EditParaclinicalResults";
    public const string VerifyParaclinicalResults = "VerifyParaclinicalResults";
    public const string ViewAbnormalResults = "ViewAbnormalResults";

    // Care Plan
    public const string ViewCarePlan = "ViewCarePlan";
    public const string CreateCarePlanItem = "CreateCarePlanItem";
    public const string EditCarePlanItem = "EditCarePlanItem";
    public const string CompleteCarePlanItem = "CompleteCarePlanItem";
    public const string CancelCarePlanItem = "CancelCarePlanItem";
    public const string VerifyCarePlanItem = "VerifyCarePlanItem";

    // Reminders / Alerts
    public const string ViewReminders = "ViewReminders";
    public const string CreateReminder = "CreateReminder";
    public const string EditReminder = "EditReminder";
    public const string CompleteReminder = "CompleteReminder";
    public const string CancelReminder = "CancelReminder";

    // Measurements / Graphs
    public const string ViewMeasurements = "ViewMeasurements";
    public const string CreateMeasurement = "CreateMeasurement";
    public const string EditMeasurement = "EditMeasurement";
    public const string ViewAbnormalMeasurements = "ViewAbnormalMeasurements";
    public const string ManagePriorityMeasurements = "ManagePriorityMeasurements";

    // Timeline
    public const string ViewTimeline = "ViewTimeline";
    public const string CreateTimelineEvent = "CreateTimelineEvent";
    public const string EditTimelineEvent = "EditTimelineEvent";
    public const string DeleteTimelineEvent = "DeleteTimelineEvent";

    // Audit / Compliance
    public const string ViewAuditLog = "ViewAuditLog";
    public const string ExportAuditLog = "ExportAuditLog";

    // Access Management
    public const string ManageAccess = "ManageAccess";
    public const string ManageConsent = "ManageConsent";
    public const string GrantPatientAccess = "GrantPatientAccess";
    public const string RevokePatientAccess = "RevokePatientAccess";

    // Record Sharing / Export
    public const string ExportPatientRecord = "ExportPatientRecord";
    public const string SharePatientRecord = "SharePatientRecord";

    // System / Product
    public const string ManageProductAccessProfiles = "ManageProductAccessProfiles";
    public const string ManageSecuritySettings = "ManageSecuritySettings";

    // Restricted
    public const string ViewRestrictedData = "ViewRestrictedData";
    public const string EditRestrictedData = "EditRestrictedData";
    public const string EmergencyAccess = "EmergencyAccess";

    public static readonly IReadOnlyCollection<string> All = new[]
    {
        ViewPatientProfile,
        EditPatientProfile,
        ViewPatientContactInfo,
        EditPatientContactInfo,
        ViewPatientSummary,
        ViewPatientDirectory,
        ViewMedicalHistory,
        EditMedicalHistory,
        ViewSensitiveMedicalHistory,
        VerifyMedicalHistory,
        ViewDocuments,
        UploadDocuments,
        EditDocuments,
        DeleteDocuments,
        VerifyDocuments,
        ShareDocuments,
        ViewParaclinicalResults,
        EditParaclinicalResults,
        VerifyParaclinicalResults,
        ViewAbnormalResults,
        ViewCarePlan,
        CreateCarePlanItem,
        EditCarePlanItem,
        CompleteCarePlanItem,
        CancelCarePlanItem,
        VerifyCarePlanItem,
        ViewReminders,
        CreateReminder,
        EditReminder,
        CompleteReminder,
        CancelReminder,
        ViewMeasurements,
        CreateMeasurement,
        EditMeasurement,
        ViewAbnormalMeasurements,
        ManagePriorityMeasurements,
        ViewTimeline,
        CreateTimelineEvent,
        EditTimelineEvent,
        DeleteTimelineEvent,
        ViewAuditLog,
        ExportAuditLog,
        ManageAccess,
        ManageConsent,
        GrantPatientAccess,
        RevokePatientAccess,
        ExportPatientRecord,
        SharePatientRecord,
        ManageProductAccessProfiles,
        ManageSecuritySettings,
        ViewRestrictedData,
        EditRestrictedData,
        EmergencyAccess,
    };

    public static readonly IReadOnlyCollection<string> ClinicalReadPermissions = new[]
    {
        ViewPatientProfile,
        ViewPatientContactInfo,
        ViewPatientSummary,
        ViewPatientDirectory,
        ViewMedicalHistory,
        ViewSensitiveMedicalHistory,
        ViewDocuments,
        ViewParaclinicalResults,
        ViewAbnormalResults,
        ViewCarePlan,
        ViewReminders,
        ViewMeasurements,
        ViewAbnormalMeasurements,
        ViewTimeline,
    };

    public static readonly IReadOnlyCollection<string> ClinicalWritePermissions = new[]
    {
        EditPatientProfile,
        EditPatientContactInfo,
        EditMedicalHistory,
        UploadDocuments,
        EditDocuments,
        DeleteDocuments,
        EditParaclinicalResults,
        CreateCarePlanItem,
        EditCarePlanItem,
        CompleteCarePlanItem,
        CancelCarePlanItem,
        CreateReminder,
        EditReminder,
        CompleteReminder,
        CancelReminder,
        CreateMeasurement,
        EditMeasurement,
        ManagePriorityMeasurements,
        CreateTimelineEvent,
        EditTimelineEvent,
        DeleteTimelineEvent,
    };

    public static readonly IReadOnlyCollection<string> AdministrativePermissions = new[]
    {
        VerifyMedicalHistory,
        VerifyDocuments,
        ShareDocuments,
        VerifyParaclinicalResults,
        VerifyCarePlanItem,
        ViewAuditLog,
        ExportAuditLog,
        ManageAccess,
        ManageConsent,
        GrantPatientAccess,
        RevokePatientAccess,
        ExportPatientRecord,
        SharePatientRecord,
        ManageProductAccessProfiles,
        ManageSecuritySettings,
        ViewRestrictedData,
        EditRestrictedData,
        EmergencyAccess,
    };
}
