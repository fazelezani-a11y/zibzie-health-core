namespace Zibzie.HealthCore.Domain.Security;

public static class ProductAccessProfiles
{
    private static readonly IReadOnlyCollection<string> DigiCareClinicalPermissions = new[]
    {
        HealthPermissions.ViewPatientProfile,
        HealthPermissions.ViewPatientContactInfo,
        HealthPermissions.ViewPatientSummary,
        HealthPermissions.ViewMedicalHistory,
        HealthPermissions.EditMedicalHistory,
        HealthPermissions.ViewSensitiveMedicalHistory,
        HealthPermissions.VerifyMedicalHistory,
        HealthPermissions.ViewDocuments,
        HealthPermissions.UploadDocuments,
        HealthPermissions.VerifyDocuments,
        HealthPermissions.ViewParaclinicalResults,
        HealthPermissions.EditParaclinicalResults,
        HealthPermissions.VerifyParaclinicalResults,
        HealthPermissions.ViewAbnormalResults,
        HealthPermissions.ViewCarePlan,
        HealthPermissions.CreateCarePlanItem,
        HealthPermissions.EditCarePlanItem,
        HealthPermissions.CompleteCarePlanItem,
        HealthPermissions.CancelCarePlanItem,
        HealthPermissions.VerifyCarePlanItem,
        HealthPermissions.ViewReminders,
        HealthPermissions.CreateReminder,
        HealthPermissions.EditReminder,
        HealthPermissions.ViewMeasurements,
        HealthPermissions.CreateMeasurement,
        HealthPermissions.EditMeasurement,
        HealthPermissions.ViewAbnormalMeasurements,
        HealthPermissions.ViewTimeline,
        HealthPermissions.CreateTimelineEvent,
        HealthPermissions.EditTimelineEvent,
    };

    private static readonly IReadOnlyCollection<string> DigiCareLifestyleSpecialistPermissions = new[]
    {
        HealthPermissions.ViewPatientProfile,
        HealthPermissions.ViewPatientContactInfo,
        HealthPermissions.ViewPatientSummary,
        HealthPermissions.ViewMedicalHistory,
        HealthPermissions.ViewDocuments,
        HealthPermissions.ViewParaclinicalResults,
        HealthPermissions.ViewCarePlan,
        HealthPermissions.CreateCarePlanItem,
        HealthPermissions.EditCarePlanItem,
        HealthPermissions.ViewReminders,
        HealthPermissions.CreateReminder,
        HealthPermissions.EditReminder,
        HealthPermissions.ViewMeasurements,
        HealthPermissions.CreateMeasurement,
        HealthPermissions.ViewTimeline,
    };

    public static readonly IReadOnlyCollection<ProductAccessProfile> All = new[]
    {
        Profile(
            ProductCodes.InternalAdmin,
            Role(
                ProductCodes.InternalAdmin,
                ProductRoles.SuperAdmin,
                AccessScopes.AllPatients,
                AuthorizationReasons.InternalAdmin,
                HealthPermissions.All),
            Role(
                ProductCodes.InternalAdmin,
                ProductRoles.HealthCoreAdmin,
                AccessScopes.AllPatients,
                AuthorizationReasons.InternalAdmin,
                AllExcept(HealthPermissions.EmergencyAccess)),
            Role(
                ProductCodes.InternalAdmin,
                ProductRoles.ReadOnlyAuditor,
                AccessScopes.AllPatients,
                AuthorizationReasons.InternalAdmin,
                HealthPermissions.ViewAuditLog,
                HealthPermissions.ExportAuditLog,
                HealthPermissions.ViewTimeline),
            Role(
                ProductCodes.InternalAdmin,
                ProductRoles.SupportOperator,
                AccessScopes.AssignedPatientsOnly,
                AuthorizationReasons.InternalAdmin,
                HealthPermissions.ViewPatientProfile,
                HealthPermissions.ViewPatientContactInfo,
                HealthPermissions.ViewTimeline)),

        Profile(
            ProductCodes.DigiCare,
            Role(
                ProductCodes.DigiCare,
                ProductRoles.DigiCareCaseManager,
                AccessScopes.AssignedPatientsOnly,
                AuthorizationReasons.CareTeamOperation,
                HealthPermissions.ViewPatientProfile,
                HealthPermissions.ViewPatientDirectory,
                HealthPermissions.EditPatientProfile,
                HealthPermissions.ViewPatientContactInfo,
                HealthPermissions.EditPatientContactInfo,
                HealthPermissions.ViewPatientSummary,
                HealthPermissions.ViewMedicalHistory,
                HealthPermissions.ViewDocuments,
                HealthPermissions.UploadDocuments,
                HealthPermissions.ViewParaclinicalResults,
                HealthPermissions.ViewCarePlan,
                HealthPermissions.CreateCarePlanItem,
                HealthPermissions.EditCarePlanItem,
                HealthPermissions.ViewReminders,
                HealthPermissions.CreateReminder,
                HealthPermissions.EditReminder,
                HealthPermissions.ViewTimeline,
                HealthPermissions.CreateTimelineEvent,
                HealthPermissions.EditTimelineEvent,
                HealthPermissions.SharePatientRecord),
            Role(
                ProductCodes.DigiCare,
                ProductRoles.DigiCareCareTeamManager,
                AccessScopes.AssignedPatientsOnly,
                AuthorizationReasons.CareTeamOperation,
                HealthPermissions.ViewPatientProfile,
                HealthPermissions.ViewPatientDirectory,
                HealthPermissions.EditPatientProfile,
                HealthPermissions.ViewPatientContactInfo,
                HealthPermissions.EditPatientContactInfo,
                HealthPermissions.ViewPatientSummary,
                HealthPermissions.ViewMedicalHistory,
                HealthPermissions.ViewDocuments,
                HealthPermissions.UploadDocuments,
                HealthPermissions.ViewParaclinicalResults,
                HealthPermissions.ViewCarePlan,
                HealthPermissions.CreateCarePlanItem,
                HealthPermissions.EditCarePlanItem,
                HealthPermissions.CompleteCarePlanItem,
                HealthPermissions.CancelCarePlanItem,
                HealthPermissions.ViewReminders,
                HealthPermissions.CreateReminder,
                HealthPermissions.EditReminder,
                HealthPermissions.CompleteReminder,
                HealthPermissions.CancelReminder,
                HealthPermissions.ViewTimeline,
                HealthPermissions.CreateTimelineEvent,
                HealthPermissions.EditTimelineEvent,
                HealthPermissions.DeleteTimelineEvent,
                HealthPermissions.ManageAccess,
                HealthPermissions.GrantPatientAccess,
                HealthPermissions.RevokePatientAccess,
                HealthPermissions.SharePatientRecord),
            Role(
                ProductCodes.DigiCare,
                ProductRoles.DigiCareClinician,
                AccessScopes.AssignedPatientsOnly,
                AuthorizationReasons.ActiveCare,
                DigiCareClinicalPermissions),
            Role(
                ProductCodes.DigiCare,
                ProductRoles.DigiCarePersonalDoctor,
                AccessScopes.AssignedPatientsOnly,
                AuthorizationReasons.ActiveCare,
                DigiCareClinicalPermissions),
            Role(
                ProductCodes.DigiCare,
                ProductRoles.DigiCarePersonalCounselor,
                AccessScopes.AssignedPatientsOnly,
                AuthorizationReasons.ActiveCare,
                HealthPermissions.ViewPatientProfile,
                HealthPermissions.ViewPatientContactInfo,
                HealthPermissions.ViewPatientSummary,
                HealthPermissions.ViewMedicalHistory,
                HealthPermissions.ViewCarePlan,
                HealthPermissions.CreateCarePlanItem,
                HealthPermissions.EditCarePlanItem,
                HealthPermissions.ViewReminders,
                HealthPermissions.CreateReminder,
                HealthPermissions.EditReminder,
                HealthPermissions.ViewMeasurements,
                HealthPermissions.ViewTimeline),
            Role(
                ProductCodes.DigiCare,
                ProductRoles.DigiCareNutritionSpecialist,
                AccessScopes.AssignedPatientsOnly,
                AuthorizationReasons.ActiveCare,
                DigiCareLifestyleSpecialistPermissions),
            Role(
                ProductCodes.DigiCare,
                ProductRoles.DigiCareExerciseSpecialist,
                AccessScopes.AssignedPatientsOnly,
                AuthorizationReasons.ActiveCare,
                DigiCareLifestyleSpecialistPermissions),
            Role(
                ProductCodes.DigiCare,
                ProductRoles.DigiCareOperations,
                AccessScopes.AssignedPatientsOnly,
                AuthorizationReasons.CareTeamOperation,
                HealthPermissions.ViewPatientProfile,
                HealthPermissions.ViewPatientContactInfo,
                HealthPermissions.ViewCarePlan,
                HealthPermissions.ViewReminders,
                HealthPermissions.ViewTimeline),
            Role(
                ProductCodes.DigiCare,
                ProductRoles.DigiCareTransportCoordinator,
                AccessScopes.AssignedPatientsOnly,
                AuthorizationReasons.CareTeamOperation,
                HealthPermissions.ViewPatientProfile,
                HealthPermissions.ViewPatientContactInfo,
                HealthPermissions.ViewCarePlan)),

        Profile(
            ProductCodes.HomeVisit,
            Role(
                ProductCodes.HomeVisit,
                ProductRoles.HomeVisitDoctor,
                AccessScopes.TemporaryAccess,
                AuthorizationReasons.HomeVisit,
                HealthPermissions.ViewPatientProfile,
                HealthPermissions.ViewPatientContactInfo,
                HealthPermissions.ViewPatientSummary,
                HealthPermissions.ViewMedicalHistory,
                HealthPermissions.ViewSensitiveMedicalHistory,
                HealthPermissions.ViewCarePlan,
                HealthPermissions.ViewMeasurements,
                HealthPermissions.CreateMeasurement),
            Role(
                ProductCodes.HomeVisit,
                ProductRoles.HomeVisitDispatcher,
                AccessScopes.AssignedPatientsOnly,
                AuthorizationReasons.HomeVisit,
                HealthPermissions.ViewPatientProfile,
                HealthPermissions.ViewPatientContactInfo),
            Role(
                ProductCodes.HomeVisit,
                ProductRoles.HomeVisitPatient,
                AccessScopes.OwnRecordOnly,
                AuthorizationReasons.HomeVisit,
                HealthPermissions.ViewPatientProfile,
                HealthPermissions.ViewPatientContactInfo)),

        Profile(
            ProductCodes.SecondOpinion,
            Role(
                ProductCodes.SecondOpinion,
                ProductRoles.SecondOpinionCaseManager,
                AccessScopes.InvitedCasesOnly,
                AuthorizationReasons.SecondOpinion,
                HealthPermissions.ViewPatientProfile,
                HealthPermissions.ViewPatientContactInfo,
                HealthPermissions.ViewPatientSummary,
                HealthPermissions.ViewMedicalHistory,
                HealthPermissions.ViewDocuments,
                HealthPermissions.UploadDocuments,
                HealthPermissions.EditDocuments,
                HealthPermissions.ViewParaclinicalResults,
                HealthPermissions.ViewCarePlan,
                HealthPermissions.ViewTimeline,
                HealthPermissions.ManageAccess,
                HealthPermissions.GrantPatientAccess,
                HealthPermissions.RevokePatientAccess,
                HealthPermissions.SharePatientRecord),
            Role(
                ProductCodes.SecondOpinion,
                ProductRoles.SecondOpinionLeadSpecialist,
                AccessScopes.InvitedCasesOnly,
                AuthorizationReasons.SecondOpinion,
                HealthPermissions.ViewPatientProfile,
                HealthPermissions.ViewMedicalHistory,
                HealthPermissions.ViewSensitiveMedicalHistory,
                HealthPermissions.ViewDocuments,
                HealthPermissions.ViewParaclinicalResults,
                HealthPermissions.ViewAbnormalResults,
                HealthPermissions.ViewCarePlan,
                HealthPermissions.ViewMeasurements,
                HealthPermissions.ViewTimeline),
            Role(
                ProductCodes.SecondOpinion,
                ProductRoles.SecondOpinionInvitedSpecialist,
                AccessScopes.InvitedCasesOnly,
                AuthorizationReasons.SecondOpinion,
                HealthPermissions.ViewPatientProfile,
                HealthPermissions.ViewMedicalHistory,
                HealthPermissions.ViewDocuments,
                HealthPermissions.ViewParaclinicalResults,
                HealthPermissions.ViewAbnormalResults,
                HealthPermissions.ViewMeasurements),
            Role(
                ProductCodes.SecondOpinion,
                ProductRoles.SecondOpinionPatient,
                AccessScopes.OwnRecordOnly,
                AuthorizationReasons.SecondOpinion,
                HealthPermissions.ViewPatientProfile,
                HealthPermissions.ViewDocuments,
                HealthPermissions.UploadDocuments,
                HealthPermissions.ViewParaclinicalResults)),

        Profile(
            ProductCodes.PersonalHealthRecord,
            Role(
                ProductCodes.PersonalHealthRecord,
                ProductRoles.PersonalHealthRecordOwner,
                AccessScopes.OwnRecordOnly,
                AuthorizationReasons.PatientShared,
                HealthPermissions.ViewPatientProfile,
                HealthPermissions.EditPatientProfile,
                HealthPermissions.ViewPatientContactInfo,
                HealthPermissions.EditPatientContactInfo,
                HealthPermissions.ViewPatientSummary,
                HealthPermissions.ViewMedicalHistory,
                HealthPermissions.ViewDocuments,
                HealthPermissions.UploadDocuments,
                HealthPermissions.ViewParaclinicalResults,
                HealthPermissions.ViewCarePlan,
                HealthPermissions.ViewReminders,
                HealthPermissions.CompleteReminder,
                HealthPermissions.ViewMeasurements,
                HealthPermissions.CreateMeasurement,
                HealthPermissions.ViewTimeline,
                HealthPermissions.ExportPatientRecord,
                HealthPermissions.SharePatientRecord),
            Role(
                ProductCodes.PersonalHealthRecord,
                ProductRoles.PersonalHealthRecordFamilyViewer,
                AccessScopes.FamilyAuthorizedRecords,
                AuthorizationReasons.PatientShared,
                HealthPermissions.ViewPatientProfile,
                HealthPermissions.ViewMedicalHistory,
                HealthPermissions.ViewDocuments,
                HealthPermissions.ViewParaclinicalResults,
                HealthPermissions.ViewCarePlan,
                HealthPermissions.ViewMeasurements,
                HealthPermissions.ViewTimeline),
            Role(
                ProductCodes.PersonalHealthRecord,
                ProductRoles.PersonalHealthRecordSharedProvider,
                AccessScopes.TemporaryAccess,
                AuthorizationReasons.PatientShared,
                HealthPermissions.ViewPatientProfile,
                HealthPermissions.ViewMedicalHistory,
                HealthPermissions.ViewDocuments,
                HealthPermissions.ViewParaclinicalResults,
                HealthPermissions.ViewCarePlan,
                HealthPermissions.ViewMeasurements,
                HealthPermissions.ViewTimeline)),

        Profile(
            ProductCodes.ClinicQueue,
            Role(
                ProductCodes.ClinicQueue,
                ProductRoles.ClinicQueueReceptionist,
                AccessScopes.OrganizationPatients,
                AuthorizationReasons.CareTeamOperation,
                HealthPermissions.ViewPatientProfile,
                HealthPermissions.ViewPatientContactInfo),
            Role(
                ProductCodes.ClinicQueue,
                ProductRoles.ClinicQueueClinicAdmin,
                AccessScopes.OrganizationPatients,
                AuthorizationReasons.CareTeamOperation,
                HealthPermissions.ViewPatientProfile,
                HealthPermissions.ViewPatientContactInfo),
            Role(
                ProductCodes.ClinicQueue,
                ProductRoles.ClinicQueuePatient,
                AccessScopes.OwnRecordOnly,
                AuthorizationReasons.PatientShared,
                HealthPermissions.ViewPatientProfile)),
    };

    public static ProductAccessProfile? GetByProductCode(string? productCode)
    {
        if (string.IsNullOrWhiteSpace(productCode))
        {
            return null;
        }

        return All.FirstOrDefault(profile =>
            string.Equals(profile.ProductCode, productCode, StringComparison.Ordinal));
    }

    public static ProductRoleAccessProfile? GetRoleProfile(string? productCode, string? roleCode)
    {
        if (string.IsNullOrWhiteSpace(productCode) || string.IsNullOrWhiteSpace(roleCode))
        {
            return null;
        }

        return GetByProductCode(productCode)?.Roles.FirstOrDefault(role =>
            string.Equals(role.RoleCode, roleCode, StringComparison.Ordinal));
    }

    private static ProductAccessProfile Profile(
        string productCode,
        params ProductRoleAccessProfile[] roles)
    {
        return new ProductAccessProfile(productCode, roles);
    }

    private static ProductRoleAccessProfile Role(
        string productCode,
        string roleCode,
        string accessScope,
        string authorizationReason,
        params string[] permissions)
    {
        return Role(productCode, roleCode, accessScope, authorizationReason, (IReadOnlyCollection<string>)permissions);
    }

    private static ProductRoleAccessProfile Role(
        string productCode,
        string roleCode,
        string accessScope,
        string authorizationReason,
        IReadOnlyCollection<string> permissions)
    {
        return new ProductRoleAccessProfile(roleCode, productCode, accessScope, authorizationReason, permissions.ToArray());
    }

    private static IReadOnlyCollection<string> AllExcept(params string[] excludedPermissions)
    {
        return HealthPermissions.All
            .Where(permission => !excludedPermissions.Contains(permission))
            .ToArray();
    }
}
