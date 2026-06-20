namespace Zibzie.HealthCore.Domain.Security;

public static class AccessScopes
{
    public const string AllPatients = "AllPatients";
    public const string AssignedPatientsOnly = "AssignedPatientsOnly";
    public const string OwnRecordOnly = "OwnRecordOnly";
    public const string FamilyAuthorizedRecords = "FamilyAuthorizedRecords";
    public const string InvitedCasesOnly = "InvitedCasesOnly";
    public const string OrganizationPatients = "OrganizationPatients";
    public const string TemporaryAccess = "TemporaryAccess";
    public const string EmergencyAccess = "EmergencyAccess";
    public const string CreatedByMe = "CreatedByMe";

    public static readonly IReadOnlyCollection<string> All = new[]
    {
        AllPatients,
        AssignedPatientsOnly,
        OwnRecordOnly,
        FamilyAuthorizedRecords,
        InvitedCasesOnly,
        OrganizationPatients,
        TemporaryAccess,
        EmergencyAccess,
        CreatedByMe,
    };
}
