# Authorization Endpoint Matrix

Phase 84A mapped the current Health Core API surface to future authorization and audit requirements.

This document is now maintained as the endpoint authorization rollout matrix. Enforcement status is tracked below.

## Current API Surface

Current patient-record controllers:

- `PatientsController`
- `ConditionsController`
- `AllergiesController`
- `MedicationsController`
- `PatientDocumentsController`
- `ParaclinicalResultsController`
- `CarePlanItemsController`
- `PatientRemindersController`
- `PatientMeasurementsController`
- `TimelineEventsController`

No access-management, export, share, download, or security admin endpoints exist yet.

## Current Enforcement Status

- Phase 84B2 protects Documents endpoints.
- Phase 84C protects Paraclinical Results endpoints.
- Phase 84D protects current Medical History endpoints: Conditions, Allergies, and Medications.
- Phase 84E1 protects Care Plan endpoints.
- Phase 84E2 protects Reminder endpoints.
- Phase 84E3 protects Measurement endpoints.
- Phase 84E4 completed Patient Summary authorization strategy.
- Phase 84E5 protects Patient Summary endpoint.
- Phase 84F protects Timeline endpoints.
- Phase 84G completed security enforcement coverage audit.
- Phase 84H completed Patient Profile / Patient Directory authorization strategy.
- Phase 84H2 protects Patient Profile / Patient Directory read endpoints.
- Standalone patient profile write enforcement is still pending for create, update, and deactivate.

## Request Context Gap

The API currently has no real authentication/current-user infrastructure.

Phase 84B1 added a request-context provider with a temporary development fallback. That fallback keeps the current admin panel usable but is not production-safe.

Before endpoint enforcement, the API needs a lightweight request context that can resolve:

- `UserId` or `ServiceAccountId`
- `ProductCode`
- `ProductRole`
- `PatientId`
- correlation id
- IP address
- user agent
- request path
- HTTP method

Suggested future abstraction:

- `IHealthCoreRequestContext`
- `HealthCoreRequestContext`
- `HealthCoreRequestContextMiddleware` or a small scoped accessor

This should initially read from the future authenticated principal and/or trusted product headers. Until a real identity provider exists, any temporary local headers must be explicitly limited to development/test environments.

## Patient Id Resolution

Endpoint enforcement needs a reliable patient id before calling `IHealthCoreAuthorizationService`.

Patterns found:

- Collection routes use `{patientId}` directly.
- Single-record routes, update routes, and delete routes often use `{documentId}`, `{resultId}`, `{itemId}`, `{reminderId}`, `{measurementId}`, `{eventId}`, `{conditionId}`, `{allergyId}`, or `{medicationId}`. These must load the record first to resolve the patient id.
- Patient list/search has no single patient id and needs product/role/scoped list strategy later.

## Rollout Plan

| Phase | Area | Rationale |
|---|---|---|
| 84B1 | Request Context foundation | Required before any endpoint can safely call authorization/audit services. |
| 84B2 | Documents | Highest evidence/privacy risk; document metadata may include file locations and sensitive content. |
| 84C | Paraclinical Results | Lab/result data can be sensitive and abnormal findings need careful audit. |
| 84D | Medical History | Conditions, allergies, and medications are core clinical data. |
| 84E | Care Plan, Reminders, Measurements | Operational health actions, sensitive reminders, and trendable measurements. |
| 84F | Timeline | Timeline exposes clinical/operational history and is distinct from AuditLog. |
| 84G | Security enforcement coverage audit | Verify protected groups and identify remaining gaps. |
| 84H | Patient Profile / Patient Directory strategy | Patient directory can leak patient existence and needs scoped list/search planning. |
| 84H2 | Patient Profile read enforcement | Protect patient list/search and detail/profile reads. |
| 84H3 | Patient Profile write enforcement | Protect create, update, and deactivate operations. |
| 84I | Access Management | Grant/revoke endpoints must be protected and audited when implemented. |

## Endpoint Access Matrix

### 84B2: Documents

| Area | Controller / Route | Method | Action | PatientId source | Required permission | Type | Sensitivity | Grant? | Audit? | Audit action | Audit resource | Notes / risks |
|---|---|---:|---|---|---|---|---|---|---|---|---|---|
| Documents | `/api/health-core/patients/{patientId}/documents` | GET | List documents | Route | `ViewDocuments` | Read | Check requested/returned `SensitivityLevel`; restricted later needs `ViewRestrictedData`. | Yes, except internal admin. | Always | `View` | `Document` | Avoid over-logging repeated polling, but document list is high-risk enough to audit. |
| Documents | `/api/health-core/patients/{patientId}/documents` | POST | Create metadata | Route | `UploadDocuments` | Write | Use request `SensitivityLevel`; sensitive upload should be audited. | Yes | Always | `Create` | `Document` | Create service also writes timeline; timeline is not audit. |
| Documents | `/api/health-core/documents/{documentId}` | GET | Get document metadata | Load document | `ViewDocuments` | Read | Check entity `SensitivityLevel`. | Yes | Always | `View` | `Document` | Must load record to resolve patient id before authorizing response. |
| Documents | `/api/health-core/documents/{documentId}` | PUT | Update document metadata | Load document | `EditDocuments` | Write | Check existing and requested sensitivity. | Yes | Always | `Update` | `Document` | If sensitivity changes to restricted, require restricted permission. |
| Documents | `/api/health-core/documents/{documentId}` | DELETE | Soft-delete document | Load document | `DeleteDocuments` | Write | Check entity `SensitivityLevel`. | Yes | Always | `Delete` | `Document` | Delete is soft-delete but still security-significant. |

### 84C: Paraclinical Results

| Area | Controller / Route | Method | Action | PatientId source | Required permission | Type | Sensitivity | Grant? | Audit? | Audit action | Audit resource | Notes / risks |
|---|---|---:|---|---|---|---|---|---|---|---|---|---|
| Paraclinical | `/api/health-core/patients/{patientId}/paraclinical-results` | GET | List results/lab items | Route | `ViewParaclinicalResults` | Read | Check result sensitivity; abnormal results may also require `ViewAbnormalResults` when `requiresFollowUp`/abnormal. | Yes | Always | `View` | `ParaclinicalResult` | Lab items are embedded in result response. |
| Paraclinical | `/api/health-core/patients/{patientId}/paraclinical-results` | POST | Create result with lab items | Route | `EditParaclinicalResults` | Write | Request sensitivity; linked document sensitivity should be considered. | Yes | Always | `Create` | `ParaclinicalResult` | Linked document validation already exists; authorization must also cover linked document view/use. |
| Paraclinical | `/api/health-core/paraclinical-results/{resultId}` | GET | Get result | Load result | `ViewParaclinicalResults` | Read | Entity sensitivity; abnormal result may require `ViewAbnormalResults`. | Yes | Always | `View` | `ParaclinicalResult` | Must resolve patient id from result. |
| Paraclinical | `/api/health-core/paraclinical-results/{resultId}` | PUT | Update result | Load result | `EditParaclinicalResults` | Write | Existing and requested sensitivity; abnormal flag changes should audit. | Yes | Always | `Update` | `ParaclinicalResult` | Future verify action may need `VerifyParaclinicalResults`. |
| Paraclinical | `/api/health-core/paraclinical-results/{resultId}` | DELETE | Soft-delete result | Load result | `EditParaclinicalResults` | Write | Entity sensitivity. | Yes | Always | `Delete` | `ParaclinicalResult` | No explicit delete permission exists yet for results; consider adding later. |

### 84D: Medical History

| Area | Controller / Route | Method | Action | PatientId source | Required permission | Type | Sensitivity | Grant? | Audit? | Audit action | Audit resource | Notes / risks |
|---|---|---:|---|---|---|---|---|---|---|---|---|---|
| Conditions | `/api/health-core/patients/{patientId}/conditions` | GET | List conditions | Route | `ViewMedicalHistory` | Read | Check per-record `SensitivityLevel`. | Yes | Always or summary | `View` | `Condition` | Frequent overview polling may need summary audit instead of per-card audit. |
| Conditions | `/api/health-core/patients/{patientId}/conditions` | POST | Create condition | Route | `EditMedicalHistory` | Write | Request sensitivity. | Yes | Always | `Create` | `Condition` | Verification-related flows may later require `VerifyMedicalHistory`. |
| Conditions | `/api/health-core/conditions/{conditionId}` | PUT | Update condition | Load condition | `EditMedicalHistory` | Write | Existing/requested sensitivity. | Yes | Always | `Update` | `Condition` | Must resolve patient id from record. |
| Conditions | `/api/health-core/conditions/{conditionId}` | DELETE | Soft-delete condition | Load condition | `EditMedicalHistory` | Write | Entity sensitivity. | Yes | Always | `Delete` | `Condition` | Consider dedicated delete permission later if needed. |
| Allergies | `/api/health-core/patients/{patientId}/allergies` | GET | List allergies | Route | `ViewMedicalHistory` | Read | Check per-record `SensitivityLevel`. | Yes | Always or summary | `View` | `Allergy` | Allergies are safety-critical but still sensitive. |
| Allergies | `/api/health-core/patients/{patientId}/allergies` | POST | Create allergy | Route | `EditMedicalHistory` | Write | Request sensitivity. | Yes | Always | `Create` | `Allergy` | Severity/reaction may be clinically sensitive. |
| Allergies | `/api/health-core/allergies/{allergyId}` | PUT | Update allergy | Load allergy | `EditMedicalHistory` | Write | Existing/requested sensitivity. | Yes | Always | `Update` | `Allergy` | Must resolve patient id from record. |
| Allergies | `/api/health-core/allergies/{allergyId}` | DELETE | Soft-delete allergy | Load allergy | `EditMedicalHistory` | Write | Entity sensitivity. | Yes | Always | `Delete` | `Allergy` | Consider dedicated delete permission later. |
| Medications | `/api/health-core/patients/{patientId}/medications` | GET | List medications | Route | `ViewMedicalHistory` | Read | Check per-record `SensitivityLevel`. | Yes | Always or summary | `View` | `Medication` | Current meds should be protected even when used for safety. |
| Medications | `/api/health-core/patients/{patientId}/medications` | POST | Create medication | Route | `EditMedicalHistory` | Write | Request sensitivity. | Yes | Always | `Create` | `Medication` | Medication automation later should also audit system writes. |
| Medications | `/api/health-core/medications/{medicationId}` | PUT | Update medication | Load medication | `EditMedicalHistory` | Write | Existing/requested sensitivity. | Yes | Always | `Update` | `Medication` | Must resolve patient id from record. |
| Medications | `/api/health-core/medications/{medicationId}` | DELETE | Soft-delete medication | Load medication | `EditMedicalHistory` | Write | Entity sensitivity. | Yes | Always | `Delete` | `Medication` | Consider dedicated delete permission later. |

### 84E: Care Plan, Reminders, Measurements

| Area | Controller / Route | Method | Action | PatientId source | Required permission | Type | Sensitivity | Grant? | Audit? | Audit action | Audit resource | Notes / risks |
|---|---|---:|---|---|---|---|---|---|---|---|---|---|
| Care Plan | `/api/health-core/patients/{patientId}/care-plan` | GET | List care plan | Route | `ViewCarePlan` | Read | Baseline list check now; future mixed-sensitivity redaction may be needed. | Yes | Always | `View` | `CarePlanItem` | Protected in Phase 84E1. Care plan can expose diagnoses/actions. |
| Care Plan | `/api/health-core/patients/{patientId}/care-plan` | POST | Create item | Route | `CreateCarePlanItem` | Write | Request sensitivity. | Yes | Always | `Create` | `CarePlanItem` | Protected in Phase 84E1. DueAt automation/timeline behavior unchanged. |
| Care Plan | `/api/health-core/care-plan-items/{itemId}` | GET | Get item | Load item | `ViewCarePlan` | Read | Entity sensitivity. | Yes | Summary or always for restricted | `View` | `CarePlanItem` | Must resolve patient id from item. |
| Care Plan | `/api/health-core/care-plan-items/{itemId}` | PUT | Update/complete/cancel item | Load item | `EditCarePlanItem`, `CompleteCarePlanItem`, or `CancelCarePlanItem` by status change | Write | Existing/requested sensitivity. | Yes | Always | `Update` | `CarePlanItem` | Protected in Phase 84E1 with status-specific permission selection. |
| Care Plan | `/api/health-core/care-plan-items/{itemId}` | DELETE | Soft-delete item | Load item | `EditCarePlanItem` or future delete permission | Write | Entity sensitivity. | Yes | Always | `Delete` | `CarePlanItem` | Protected in Phase 84E1. No dedicated delete permission yet. |
| Reminders | `/api/health-core/patients/{patientId}/reminders` | GET | List reminders | Route | `ViewReminders` | Read | Baseline list check now; future mixed-sensitivity redaction may be needed. | Yes | Always | `View` | `Reminder` | Protected in Phase 84E2. Reminders may reveal care details. |
| Reminders | `/api/health-core/patients/{patientId}/reminders` | POST | Create reminder | Route | `CreateReminder` | Write | Request sensitivity. | Yes | Always | `Create` | `Reminder` | Protected in Phase 84E2. System-generated reminder logic unchanged. |
| Reminders | `/api/health-core/reminders/{reminderId}` | GET | Get reminder | Load reminder | `ViewReminders` | Read | Entity sensitivity. | Yes | Always | `View` | `Reminder` | Protected in Phase 84E2. Must resolve patient id from reminder. |
| Reminders | `/api/health-core/reminders/{reminderId}` | PUT | Update/complete/cancel reminder | Load reminder | `EditReminder`, `CompleteReminder`, or `CancelReminder` by status change | Write | Existing/requested sensitivity. | Yes | Always | `Update` | `Reminder` | Protected in Phase 84E2 with status-specific permission selection. |
| Reminders | `/api/health-core/reminders/{reminderId}` | DELETE | Soft-delete reminder | Load reminder | `EditReminder` or future delete permission | Write | Entity sensitivity. | Yes | Always | `Delete` | `Reminder` | Protected in Phase 84E2. No dedicated delete permission yet. |
| Measurements | `/api/health-core/patients/{patientId}/measurements` | GET | List measurements | Route | `ViewMeasurements` | Read | Optional query sensitivity now; future mixed-sensitivity redaction may be needed. | Yes | Always for now; future aggregation may reduce graph polling volume | `View` | `Measurement` | Protected in Phase 84E3. Trends can reveal sensitive state. |
| Measurements | `/api/health-core/patients/{patientId}/measurements` | POST | Create measurement | Route | `CreateMeasurement` | Write | Request sensitivity; abnormal flag should audit. | Yes | Always | `Create` | `Measurement` | Protected in Phase 84E3. Future lab-generated measurements should audit system source. |
| Measurements | `/api/health-core/measurements/{measurementId}` | GET | Get measurement | Load measurement | `ViewMeasurements` | Read | Entity sensitivity; abnormal may require `ViewAbnormalMeasurements` in future specialized endpoints. | Yes | Always | `View` | `Measurement` | Protected in Phase 84E3. Must resolve patient id from measurement. |
| Measurements | `/api/health-core/measurements/{measurementId}` | PUT | Update measurement | Load measurement | `EditMeasurement` | Write | Existing/requested sensitivity; abnormal/target changes. | Yes | Always | `Update` | `Measurement` | Protected in Phase 84E3. Priority pin management may later use `ManagePriorityMeasurements`. |
| Measurements | `/api/health-core/measurements/{measurementId}` | DELETE | Soft-delete measurement | Load measurement | `EditMeasurement` or future delete permission | Write | Entity sensitivity. | Yes | Always | `Delete` | `Measurement` | Protected in Phase 84E3. No dedicated delete permission yet. |

### 84F: Patient Profile, Summary, Timeline

| Area | Controller / Route | Method | Action | PatientId source | Required permission | Type | Sensitivity | Grant? | Audit? | Audit action | Audit resource | Notes / risks |
|---|---|---:|---|---|---|---|---|---|---|---|---|---|
| Patients | `/api/health-core/patients` | GET | List/search patients | None per row | `ViewPatientDirectory` | Read | Current DTO still includes national code and mobile for allowed callers; future DTO minimization should split strong identifiers. | Product-scope filtering deferred | Always for denied; success audited with safe metadata | `View` | `PatientProfile` | Protected in Phase 84H2. Grant-scoped directory filtering remains future work. |
| Patients | `/api/health-core/patients/{id}` | GET | Get patient details | Route | `ViewPatientProfile` | Read | Current DTO includes contact fields all-or-nothing; contact split/redaction deferred. | Yes except internal admin | Always | `View` | `PatientProfile` | Protected in Phase 84H2. Future contact-specific redaction may require `ViewPatientContactInfo`. |
| Patients | `/api/health-core/patients/{patientId}/summary` | GET | Current backend summary | Route | `ViewPatientSummary` | Read | Current backend summary includes profile/contact plus conditions, allergies, and current medications. Frontend overview loads care plan/reminders/measurements/paraclinical separately. | Yes | Always for denied; audit successful reads as `View` with possible future aggregation | `View` | `PatientSummary` | Protected in Phase 84E5 with conservative all-or-nothing summary access. Partial section filtering/redaction is deferred. |
| Patients | `/api/health-core/patients` | POST | Create patient | None until created | Future `CreatePatient` | Write | Contact identity data. | Maybe internal/admin profile | Always | `Create` | `PatientProfile` | Grant creation/bootstrap should be decided with patient creation workflow. |
| Patients | `/api/health-core/patients/{id}` | PUT | Update patient/contact | Route | `EditPatientProfile` and possibly `EditPatientContactInfo` | Write | Contact identity data. | Yes | Always | `Update` | `PatientProfile` | Contact info should probably require contact-specific permission. |
| Patients | `/api/health-core/patients/{id}` | DELETE | Deactivate patient | Route | Future `DeactivatePatient` | Write | Whole patient record availability. | Yes/internal | Always | `Delete` or `Update` | `PatientProfile` | Current behavior is soft deactivation, not hard delete. |
| Timeline | `/api/health-core/patients/{patientId}/timeline` | GET | List timeline | Route | `ViewTimeline` | Read | Baseline list check now; future sensitivity/visibility redaction may be needed. | Yes | Always for now; future aggregation may reduce UI polling volume | `View` | `TimelineEvent` | Protected in Phase 84F. Timeline is not AuditLog but may expose sensitive history. |
| Timeline | `/api/health-core/patients/{patientId}/timeline` | POST | Create manual event | Route | `CreateTimelineEvent` | Write | Request sensitivity/visibility. | Yes | Always | `Create` | `TimelineEvent` | Protected in Phase 84F. Manual timeline creation remains a patient-facing history event, not an audit log entry. |
| Timeline | `/api/health-core/timeline-events/{eventId}` | PUT | Update event | Load event | `EditTimelineEvent` | Write | Existing/requested sensitivity. | Yes | Always | `Update` | `TimelineEvent` | Protected in Phase 84F. Must resolve patient id from event. |
| Timeline | `/api/health-core/timeline-events/{eventId}` | DELETE | Soft-delete event | Load event | `DeleteTimelineEvent` | Write | Entity sensitivity. | Yes | Always | `Delete` | `TimelineEvent` | Protected in Phase 84F. Avoid confusing timeline deletion with audit deletion. |

### 84G: Future Access Management

| Area | Controller / Route | Method | Action | PatientId source | Required permission | Type | Sensitivity | Grant? | Audit? | Audit action | Audit resource | Notes / risks |
|---|---|---:|---|---|---|---|---|---|---|---|---|---|
| Access Grants | Not implemented | POST | Grant patient access | Request | `GrantPatientAccess` and/or `ManageAccess` | Access admin | Security-sensitive | Internal/admin grant required | Always | `GrantAccess` | `PatientAccessGrant` | Must record granted by, reason, validity, product context. |
| Access Grants | Not implemented | POST/DELETE | Revoke patient access | Request/load grant | `RevokePatientAccess` and/or `ManageAccess` | Access admin | Security-sensitive | Internal/admin grant required | Always | `RevokeAccess` | `PatientAccessGrant` | Must never be ordinary patient timeline. |
| Audit Log | Not implemented | GET | View audit log | Query | `ViewAuditLog` | Compliance | Restricted | Internal admin only | Always | `View` | `AuditLog` | Audit log UI/API must be highly restricted. |
| Export/Share | Not implemented | POST | Export/share record | Request | `ExportPatientRecord` / `SharePatientRecord` | Export/share | Depends on included sections | Yes | Always | `Export` / `Share` | `PatientSummary` or section resource | Should support explicit reason and recipient metadata. |

## Audit Logging Levels

Recommended audit policy:

- Always audit document access and document writes.
- Always audit paraclinical result access and writes.
- Always audit all writes to medical history, care plan, reminders, measurements, profile/contact, timeline, access grants, exports, and shares.
- Always audit access denied decisions for protected endpoints.
- Always audit restricted data access.
- Consider summary or sampled audit for frequent overview/dashboard polling to avoid excessive audit volume.
- Patient timeline events must not be used as technical audit evidence.

## PatientAccessGrant Bootstrapping

Endpoint enforcement will deny most non-internal users unless grants exist.

Grant creation paths should be designed before enforcement:

- Internal admin assignment for care teams.
- DigiCare care-team assignment.
- Second Opinion case invitation.
- HomeVisit temporary visit access.
- Patient sharing for family/provider access.
- Emergency access with explicit reason, expiry, and audit.

No grant creation endpoint exists yet.

## Risks Before Enforcement

- Current frontend does not send product context or product role.
- No real authentication/current-user model exists yet.
- No default grants exist for local/test users.
- Immediate enforcement would break the current admin panel.
- Patient summary aggregates many modules and needs redaction or composite permission strategy.
- Audit volume could become noisy if every dashboard poll creates many rows.
- Sensitivity values currently include `Normal` and `Sensitive`; `Restricted` is planned but not yet a first-class constant.
- The InternalAdmin no-grant exception must stay narrow.
- Single-record routes need patient id resolution before authorization can be decided.

## Patient Profile / Directory Strategy Status

Phase 84H completed the patient profile and directory authorization strategy.
Phase 84H2 protects patient read/list/detail endpoints.

Recommended permissions for the next coding phases:

- `ViewPatientDirectory` for list/search.
- Existing `ViewPatientProfile` for basic detail/profile reads.
- Existing `ViewPatientContactInfo` for phone, email, address, and emergency contact fields.
- `CreatePatient` for patient creation.
- Existing `EditPatientProfile` and `EditPatientContactInfo` for updates.
- `DeactivatePatient` for the current soft-deactivate `DELETE` endpoint.

Write enforcement remains pending. The recommended rollout is:

- 84H3: protect write endpoints: create, update, deactivate.
- Future: optional `SearchPatients` split, DTO minimization, and grant-scoped directory filtering.

## Recommended Next Coding Phase

Recommended next step: **84H3 Patient Profile write enforcement**.

Keep the rollout narrow and module-by-module:

- Add patient create/deactivate permissions.
- Protect patient create, update, and deactivate.
- Keep current admin behavior working through the InternalAdmin development fallback.
- Audit successful and denied patient profile writes.
- Keep the temporary development fallback documented until real production authentication/JWT integration exists.
