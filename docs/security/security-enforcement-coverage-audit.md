# Security Enforcement Coverage Audit

Phase 84G reviews current Health Core authorization and audit coverage after the first endpoint enforcement phases.

## 1. Executive summary

Health-record domains with the highest current clinical/privacy surface are now protected with `IHealthCoreAuthorizationService`, request context from `IHealthCoreRequestContextProvider`, and audit logging through `IAuditLogService`.

Protected endpoint groups:

- Documents
- Paraclinical results
- Medical history: conditions, allergies, medications
- Care plan
- Reminders
- Measurements
- Patient summary
- Timeline

The main remaining unprotected implemented API surface is standalone patient profile management in `PatientsController`: patient list/search, patient detail, create, update, and delete/deactivate. This is risky because those endpoints expose or modify patient identity, contact, and lifecycle data, but the patient list/search scope strategy needs care before enforcement. Phase 84H added the patient profile and directory authorization strategy; enforcement remains pending.

No AuditLog read/admin endpoints are currently implemented. No access grant creation/revocation workflows are currently implemented.

## 2. Controller and endpoint inventory

| Controller | Endpoint group | Current classification | Notes |
| --- | --- | --- | --- |
| `PatientDocumentsController` | Patient documents list, create, detail/update/delete where implemented | Protected with authorization + audit | Uses document permissions and `AuditResourceTypes.Document`. |
| `ParaclinicalResultsController` | Patient paraclinical results list, create, detail/update/delete where implemented | Protected with authorization + audit | Uses paraclinical permissions and `AuditResourceTypes.ParaclinicalResult`. |
| `ConditionsController` | Patient conditions list/create/detail/update/delete where implemented | Protected with authorization + audit | Uses medical-history permissions and `AuditResourceTypes.Condition`. |
| `AllergiesController` | Patient allergies list/create/detail/update/delete where implemented | Protected with authorization + audit | Uses medical-history permissions and `AuditResourceTypes.Allergy`. |
| `MedicationsController` | Patient medications list/create/detail/update/delete where implemented | Protected with authorization + audit | Uses medical-history permissions and `AuditResourceTypes.Medication`. |
| `CarePlanItemsController` | Patient care plan items list/create/detail/update/status/delete where implemented | Protected with authorization + audit | Uses care-plan permissions and `AuditResourceTypes.CarePlanItem`. Existing reminder/timeline side effects are unchanged. |
| `PatientRemindersController` | Patient reminders list/create/detail/update/status/delete where implemented | Protected with authorization + audit | Uses reminder permissions and `AuditResourceTypes.Reminder`. Existing automation behavior is unchanged. |
| `PatientMeasurementsController` | Patient measurements list/create/detail/update/delete/trend-style routes where implemented | Protected with authorization + audit | Uses measurement permissions and `AuditResourceTypes.Measurement`. Graph behavior is unchanged. |
| `TimelineEventsController` | Patient timeline list/create/detail/update/delete where implemented | Protected with authorization + audit | Uses timeline permissions and `AuditResourceTypes.TimelineEvent`. Timeline remains separate from AuditLog. |
| `PatientsController` | `GET /api/health-core/patients/{patientId}/summary` | Protected with authorization + audit | Uses `HealthPermissions.ViewPatientSummary` and `AuditResourceTypes.PatientSummary`. All-or-nothing summary behavior is preserved. |
| `PatientsController` | Patient list/search | Unprotected and risky | Needs scoped list/search strategy before enforcement. Should not leak patients outside grants/scope. |
| `PatientsController` | Patient detail/profile | Unprotected and risky | Should require patient-profile/contact permissions and audit views. |
| `PatientsController` | Patient create | Unprotected and risky | Needs bootstrap/assignment decision and audit create. |
| `PatientsController` | Patient update | Unprotected and risky | Should require profile/contact edit permissions and audit update. |
| `PatientsController` | Patient delete/deactivate | Unprotected and risky | Should require a narrow patient lifecycle permission or an agreed existing edit/manage permission and audit delete/update. |

## 3. Protected endpoint coverage

| Group | Authorization context | Permission usage | Success audit | Denied audit | Audit resource | Minimal tests |
| --- | --- | --- | --- | --- | --- | --- |
| Documents | Yes | `ViewDocuments`, `UploadDocuments`, `EditDocuments`, `DeleteDocuments` | Yes | Yes | `Document` | Yes |
| Paraclinical results | Yes | `ViewParaclinicalResults`, `EditParaclinicalResults` | Yes | Yes | `ParaclinicalResult` | Yes |
| Conditions | Yes | `ViewMedicalHistory`, `EditMedicalHistory` | Yes | Yes | `Condition` | Yes |
| Allergies | Yes | `ViewMedicalHistory`, `EditMedicalHistory` | Yes | Yes | `Allergy` | Yes |
| Medications | Yes | `ViewMedicalHistory`, `EditMedicalHistory` | Yes | Yes | `Medication` | Yes |
| Care plan | Yes | `ViewCarePlan`, `CreateCarePlanItem`, `EditCarePlanItem`, `CompleteCarePlanItem`, `CancelCarePlanItem` | Yes | Yes | `CarePlanItem` | Yes |
| Reminders | Yes | `ViewReminders`, `CreateReminder`, `EditReminder`, `CompleteReminder`, `CancelReminder` | Yes | Yes | `Reminder` | Yes |
| Measurements | Yes | `ViewMeasurements`, `CreateMeasurement`, `EditMeasurement` | Yes | Yes | `Measurement` | Yes |
| Patient summary | Yes | `ViewPatientSummary` | Yes | Yes | `PatientSummary` | Yes |
| Timeline | Yes | `ViewTimeline`, `CreateTimelineEvent`, `EditTimelineEvent`, `DeleteTimelineEvent` | Yes | Yes | `TimelineEvent` | Yes |

## 4. Permission catalog consistency findings

- All permissions used by protected controllers exist in `HealthPermissions`.
- `HealthPermissions.All` includes the protected read, write, status, summary, and timeline permissions.
- Clinical read collections include the protected read surfaces, including `ViewPatientSummary` and `ViewTimeline`.
- Clinical write collections include current protected create/edit/status operations, including timeline create/edit/delete.
- Administrative permissions include audit, access management, export/share, security settings, restricted data, and emergency access.
- No obvious typo or constant mismatch was found between controller usage and permission constants.
- Some delete actions intentionally reuse an edit-style permission where no narrower delete permission exists, such as measurements using `EditMeasurement`. Documents and timeline have explicit delete permissions.
- Patient profile lifecycle permissions remain incomplete for standalone patient endpoints. Before protecting delete/deactivate, decide whether to add a dedicated permission or use an existing manage/edit permission.

## 5. Product profile consistency findings

- `InternalAdmin` / `HealthCoreAdmin` can still use the admin panel during dev fallback because the role has broad permissions through the existing profile.
- Narrow logistics roles remain conservative. Clinic queue and transport-style roles are not given deep medical history, document, paraclinical, measurement, or broad summary access by default.
- Clinical and care-team roles have appropriate domain read/write access for currently protected surfaces.
- Patient, family, and shared-provider profiles are not broadly expanded beyond their intended own/shared record patterns.
- Timeline write access is limited. Broad internal roles have it, DigiCare case/care-team roles can create/edit as intended, and timeline delete remains narrower.
- `ViewPatientSummary` is not assigned to the narrowest roles that should not see the current summary shape.
- HomeVisit doctor summary access should be revisited before production because the current summary includes contact and medical-history fields. It is reasonable for temporary active care, but should rely on real grants and product context.

## 6. Audit logging consistency findings

- Protected endpoints use `View`, `Create`, `Update`, `Delete`, and `AccessDenied` consistently with the endpoint action.
- Audit resource types match endpoint domains: document, paraclinical result, condition, allergy, medication, care plan item, reminder, measurement, patient summary, and timeline event.
- Patient id is included where available. Record-scoped routes resolve patient id before authorization when needed.
- Resource id is included for single-record actions when available. List endpoints correctly leave resource id empty.
- Request metadata is included through the request context pattern: product code, product role, user/service identity, correlation id, IP, user agent, path, and method.
- Denied paths include the authorization failure reason when available.
- Timeline events are not used as security audit records. AuditLog and Timeline remain separate concepts.
- Successful high-frequency read auditing is currently consistent, but may need aggregation or summarization later for dashboards and graph polling.

## 7. Known intentional gaps

- Real production JWT or service-to-service authentication is not implemented. The current development/header fallback remains temporary and not production-safe.
- PatientAccessGrant creation, revocation, and assignment workflows are not implemented.
- Standalone patient profile endpoints are not yet protected. Phase 84H strategy is complete; read/write enforcement is pending.
- Patient summary uses all-or-nothing authorization. Section-level filtering/redaction is intentionally deferred.
- Sensitivity and visibility filtering can be improved for mixed list endpoints and timeline events.
- AuditLog read/admin/reporting endpoints are not implemented. If exposed later, they must be strictly admin-only and audited.
- Audit volume management is not implemented for frequent successful reads.
- Future medical-history modules such as surgery, hospitalization, vaccination, family history, and social history are not implemented.
- Security smoke tests and end-to-end authorization tests should be added as coverage grows.

## 8. Risk-ranked remaining work

1. Replace development/header fallback with real JWT or service-to-service authentication before production use.
2. Implement PatientAccessGrant creation, revocation, assignment, and emergency-access workflows with audit logging.
3. Protect standalone patient profile endpoints, following the Phase 84H strategy and starting with patient list/search and detail/profile reads.
4. Add strict admin-only AuditLog read/reporting endpoints only when operationally needed.
5. Implement partial Patient Summary filtering/redaction so product-specific roles do not receive unauthorized section data.
6. Strengthen sensitivity and visibility filtering for list endpoints and timeline views.
7. Add audit volume controls for frequent successful reads while always auditing denied access.
8. Apply the same authorization/audit pattern to future medical-history modules.
9. Add security smoke tests and end-to-end tests for protected endpoint groups.

## 9. Recommended next phase

The next endpoint phase should focus on `PatientsController` standalone patient profile read endpoints. Add patient-directory permissions, then protect list/search and detail/profile reads with audit logging. Write endpoints should follow in a separate phase for create, update, and deactivate.

In parallel, production readiness should prioritize real identity integration and PatientAccessGrant workflows, because the current development fallback is intentionally temporary.
