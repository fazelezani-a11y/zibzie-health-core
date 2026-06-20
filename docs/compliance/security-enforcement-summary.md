# Security Enforcement Summary

This document summarizes current endpoint enforcement coverage without duplicating the full endpoint matrix.

## Protected Endpoint Groups

| Area | Main permissions | Audit resource |
| --- | --- | --- |
| Patient directory/list | `ViewPatientDirectory` | `PatientProfile` |
| Patient detail/profile | `ViewPatientProfile` | `PatientProfile` |
| Patient create | `CreatePatient` | `PatientProfile` |
| Patient update | `EditPatientProfile` | `PatientProfile` |
| Patient soft deactivate | `DeactivatePatient` | `PatientProfile` |
| Patient summary | `ViewPatientSummary` | `PatientSummary` |
| Documents | `ViewDocuments`, `UploadDocuments`, `EditDocuments`, `DeleteDocuments` | `Document` |
| Paraclinical results | `ViewParaclinicalResults`, `EditParaclinicalResults` | `ParaclinicalResult` |
| Conditions | `ViewMedicalHistory`, `EditMedicalHistory` | `Condition` |
| Allergies | `ViewMedicalHistory`, `EditMedicalHistory` | `Allergy` |
| Medications | `ViewMedicalHistory`, `EditMedicalHistory` | `Medication` |
| Care plan | `ViewCarePlan`, `CreateCarePlanItem`, `EditCarePlanItem`, `CompleteCarePlanItem`, `CancelCarePlanItem` | `CarePlanItem` |
| Reminders | `ViewReminders`, `CreateReminder`, `EditReminder`, `CompleteReminder`, `CancelReminder` | `Reminder` |
| Measurements | `ViewMeasurements`, `CreateMeasurement`, `EditMeasurement` | `Measurement` |
| Timeline | `ViewTimeline`, `CreateTimelineEvent`, `EditTimelineEvent`, `DeleteTimelineEvent` | `TimelineEvent` |

## Audit Behavior

Protected endpoints audit:

- successful reads as `View`
- successful creates as `Create`
- successful updates/status changes as `Update`
- successful soft deletes/deletes as `Delete` or `Update` depending endpoint semantics
- denied access as `AccessDenied`

Audit entries include product context, role, user/service identity when available, patient id where applicable, resource id where applicable, permission, result, failure reason for denial, and request metadata.

## Test Coverage

Focused backend tests were added through the security rollout for:

- authorization service rules
- request context provider
- audit log service
- Documents authorization
- Paraclinical authorization
- Medical History authorization
- Care Plan authorization
- Reminders authorization
- Measurements authorization
- Patient Summary authorization
- Timeline authorization
- Patient read/list/detail authorization
- Patient create/update/deactivate authorization

Most recent reported backend verification after Phase 84H3:

- `dotnet build backend\Zibzie.HealthCore.sln`: passed, 0 warnings, 0 errors
- `dotnet test backend\Zibzie.HealthCore.sln`: passed, 74/74 tests

## Remaining Deferred Items

- Production JWT or service-to-service authentication.
- PatientAccessGrant creation and revocation workflows.
- Grant-scoped patient directory filtering for external products.
- DTO minimization for patient list/search.
- Contact-specific redaction and write split.
- Patient Summary partial filtering/redaction.
- Sensitivity/visibility filtering for mixed list endpoints.
- Audit volume optimization for frequent dashboard reads.
- AuditLog admin/reporting endpoints, if needed, with strict admin-only protection.
- Security smoke tests and E2E tests.

References:

- [Security enforcement coverage audit](../security/security-enforcement-coverage-audit.md)
- [Authorization endpoint matrix](../security/authorization-endpoint-matrix.md)
- [Permission catalog](../security/permission-catalog.md)
- [Product access profiles](../security/product-access-profiles.md)
