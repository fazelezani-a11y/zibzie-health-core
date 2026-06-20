namespace Zibzie.HealthCore.Domain.Security;

public static class AuthorizationReasons
{
    public const string ActiveCare = "ActiveCare";
    public const string SecondOpinion = "SecondOpinion";
    public const string HomeVisit = "HomeVisit";
    public const string PatientShared = "PatientShared";
    public const string Emergency = "Emergency";
    public const string InternalAdmin = "InternalAdmin";
    public const string CareTeamOperation = "CareTeamOperation";
    public const string SystemAutomation = "SystemAutomation";

    public static readonly IReadOnlyCollection<string> All = new[]
    {
        ActiveCare,
        SecondOpinion,
        HomeVisit,
        PatientShared,
        Emergency,
        InternalAdmin,
        CareTeamOperation,
        SystemAutomation,
    };
}
