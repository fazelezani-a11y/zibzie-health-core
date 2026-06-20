namespace Zibzie.HealthCore.Domain.Security;

public static class ProductCodes
{
    public const string InternalAdmin = "InternalAdmin";
    public const string DigiCare = "DigiCare";
    public const string HomeVisit = "HomeVisit";
    public const string SecondOpinion = "SecondOpinion";
    public const string PersonalHealthRecord = "PersonalHealthRecord";
    public const string ClinicQueue = "ClinicQueue";

    public static readonly IReadOnlyCollection<string> All = new[]
    {
        InternalAdmin,
        DigiCare,
        HomeVisit,
        SecondOpinion,
        PersonalHealthRecord,
        ClinicQueue,
    };
}
