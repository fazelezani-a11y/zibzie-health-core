namespace Zibzie.HealthCore.Domain.Security;

public static class ProductRoles
{
    // Internal / Admin
    public const string SuperAdmin = "SuperAdmin";
    public const string HealthCoreAdmin = "HealthCoreAdmin";
    public const string ReadOnlyAuditor = "ReadOnlyAuditor";
    public const string SupportOperator = "SupportOperator";

    // DigiCare
    public const string DigiCareCaseManager = "DigiCareCaseManager";
    public const string DigiCareCareTeamManager = "DigiCareCareTeamManager";
    public const string DigiCareClinician = "DigiCareClinician";
    public const string DigiCarePersonalDoctor = "DigiCarePersonalDoctor";
    public const string DigiCarePersonalCounselor = "DigiCarePersonalCounselor";
    public const string DigiCareNutritionSpecialist = "DigiCareNutritionSpecialist";
    public const string DigiCareExerciseSpecialist = "DigiCareExerciseSpecialist";
    public const string DigiCareOperations = "DigiCareOperations";
    public const string DigiCareTransportCoordinator = "DigiCareTransportCoordinator";

    // HomeVisit
    public const string HomeVisitDoctor = "HomeVisitDoctor";
    public const string HomeVisitDispatcher = "HomeVisitDispatcher";
    public const string HomeVisitPatient = "HomeVisitPatient";

    // Second Opinion
    public const string SecondOpinionCaseManager = "SecondOpinionCaseManager";
    public const string SecondOpinionLeadSpecialist = "SecondOpinionLeadSpecialist";
    public const string SecondOpinionInvitedSpecialist = "SecondOpinionInvitedSpecialist";
    public const string SecondOpinionPatient = "SecondOpinionPatient";

    // Personal Health Record
    public const string PersonalHealthRecordOwner = "PersonalHealthRecordOwner";
    public const string PersonalHealthRecordFamilyViewer = "PersonalHealthRecordFamilyViewer";
    public const string PersonalHealthRecordSharedProvider = "PersonalHealthRecordSharedProvider";

    // Clinic Queue
    public const string ClinicQueueReceptionist = "ClinicQueueReceptionist";
    public const string ClinicQueueClinicAdmin = "ClinicQueueClinicAdmin";
    public const string ClinicQueuePatient = "ClinicQueuePatient";

    public static readonly IReadOnlyCollection<string> InternalAdminRoles = new[]
    {
        SuperAdmin,
        HealthCoreAdmin,
        ReadOnlyAuditor,
        SupportOperator,
    };

    public static readonly IReadOnlyCollection<string> DigiCareRoles = new[]
    {
        DigiCareCaseManager,
        DigiCareCareTeamManager,
        DigiCareClinician,
        DigiCarePersonalDoctor,
        DigiCarePersonalCounselor,
        DigiCareNutritionSpecialist,
        DigiCareExerciseSpecialist,
        DigiCareOperations,
        DigiCareTransportCoordinator,
    };

    public static readonly IReadOnlyCollection<string> HomeVisitRoles = new[]
    {
        HomeVisitDoctor,
        HomeVisitDispatcher,
        HomeVisitPatient,
    };

    public static readonly IReadOnlyCollection<string> SecondOpinionRoles = new[]
    {
        SecondOpinionCaseManager,
        SecondOpinionLeadSpecialist,
        SecondOpinionInvitedSpecialist,
        SecondOpinionPatient,
    };

    public static readonly IReadOnlyCollection<string> PersonalHealthRecordRoles = new[]
    {
        PersonalHealthRecordOwner,
        PersonalHealthRecordFamilyViewer,
        PersonalHealthRecordSharedProvider,
    };

    public static readonly IReadOnlyCollection<string> ClinicQueueRoles = new[]
    {
        ClinicQueueReceptionist,
        ClinicQueueClinicAdmin,
        ClinicQueuePatient,
    };

    public static readonly IReadOnlyCollection<string> All = InternalAdminRoles
        .Concat(DigiCareRoles)
        .Concat(HomeVisitRoles)
        .Concat(SecondOpinionRoles)
        .Concat(PersonalHealthRecordRoles)
        .Concat(ClinicQueueRoles)
        .ToArray();
}
