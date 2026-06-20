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
- Care plan, reminders, measurements, patient summary/profile, and timeline are not protected yet.

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
| 84F | Patient Summary, Timeline, Patient Profile | Summary aggregates multiple sections; timeline exposes clinical/operational history. |
| 84G | Access Management | Grant/revoke endpoints must be protected and audited when implemented. |

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
| Care Plan | `/api/health-core/patients/{patientId}/care-plan` | GET | List care plan | Route | `ViewCarePlan` | Read | Check item sensitivity. | Yes | Summary or always for restricted | `View` | `CarePlanItem` | Care plan can expose diagnoses/actions. |
| Care Plan | `/api/health-core/patients/{patientId}/care-plan` | POST | Create item | Route | `CreateCarePlanItem` | Write | Request sensitivity. | Yes | Always | `Create` | `CarePlanItem` | DueAt can auto-create reminder; audit generated reminder separately later if integrated. |
| Care Plan | `/api/health-core/care-plan-items/{itemId}` | GET | Get item | Load item | `ViewCarePlan` | Read | Entity sensitivity. | Yes | Summary or always for restricted | `View` | `CarePlanItem` | Must resolve patient id from item. |
| Care Plan | `/api/health-core/care-plan-items/{itemId}` | PUT | Update/complete/cancel item | Load item | `EditCarePlanItem`, `CompleteCarePlanItem`, or `CancelCarePlanItem` by status change | Write | Existing/requested sensitivity. | Yes | Always | `Update` | `CarePlanItem` | Need action-specific permission logic for completion/cancel. |
| Care Plan | `/api/health-core/care-plan-items/{itemId}` | DELETE | Soft-delete item | Load item | `EditCarePlanItem` or future delete permission | Write | Entity sensitivity. | Yes | Always | `Delete` | `CarePlanItem` | No dedicated delete permission yet. |
| Reminders | `/api/health-core/patients/{patientId}/reminders` | GET | List reminders | Route | `ViewReminders` | Read | Check reminder sensitivity. | Yes | Summary or always for restricted | `View` | `Reminder` | Reminders may reveal care details. |
| Reminders | `/api/health-core/patients/{patientId}/reminders` | POST | Create reminder | Route | `CreateReminder` | Write | Request sensitivity. | Yes | Always | `Create` | `Reminder` | System-generated reminders should audit as `SystemAction` later. |
| Reminders | `/api/health-core/reminders/{reminderId}` | GET | Get reminder | Load reminder | `ViewReminders` | Read | Entity sensitivity. | Yes | Summary or always for restricted | `View` | `Reminder` | Must resolve patient id from reminder. |
| Reminders | `/api/health-core/reminders/{reminderId}` | PUT | Update/complete/cancel reminder | Load reminder | `EditReminder`, `CompleteReminder`, or `CancelReminder` by status change | Write | Existing/requested sensitivity. | Yes | Always | `Update` | `Reminder` | Need action-specific permission logic for done/cancel. |
| Reminders | `/api/health-core/reminders/{reminderId}` | DELETE | Soft-delete reminder | Load reminder | `EditReminder` or future delete permission | Write | Entity sensitivity. | Yes | Always | `Delete` | `Reminder` | No dedicated delete permission yet. |
| Measurements | `/api/health-core/patients/{patientId}/measurements` | GET | List measurements | Route | `ViewMeasurements` | Read | Check per-record sensitivity; abnormal filter may require `ViewAbnormalMeasurements`. | Yes | Summary or always for restricted/abnormal | `View` | `Measurement` | Trends can reveal sensitive state. |
| Measurements | `/api/health-core/patients/{patientId}/measurements` | POST | Create measurement | Route | `CreateMeasurement` | Write | Request sensitivity; abnormal flag should audit. | Yes | Always | `Create` | `Measurement` | Future lab-generated measurements should audit system source. |
| Measurements | `/api/health-core/measurements/{measurementId}` | GET | Get measurement | Load measurement | `ViewMeasurements` | Read | Entity sensitivity; abnormal may require `ViewAbnormalMeasurements`. | Yes | Summary or always for restricted/abnormal | `View` | `Measurement` | Must resolve patient id from measurement. |
| Measurements | `/api/health-core/measurements/{measurementId}` | PUT | Update measurement | Load measurement | `EditMeasurement` | Write | Existing/requested sensitivity; abnormal/target changes. | Yes | Always | `Update` | `Measurement` | Priority pin management may later use `ManagePriorityMeasurements`. |
| Measurements | `/api/health-core/measurements/{measurementId}` | DELETE | Soft-delete measurement | Load measurement | `EditMeasurement` or future delete permission | Write | Entity sensitivity. | Yes | Always | `Delete` | `Measurement` | No dedicated delete permission yet. |

### 84F: Patient Profile, Summary, Timeline

| Area | Controller / Route | Method | Action | PatientId source | Required permission | Type | Sensitivity | Grant? | Audit? | Audit action | Audit resource | Notes / risks |
|---|---|---:|---|---|---|---|---|---|---|---|---|---|
| Patients | `/api/health-core/patients` | GET | List/search patients | None per row | `ViewPatientProfile` | Read | Contact info may require `ViewPatientContactInfo`. | TBD | Summary/sampled | `View` | `PatientProfile` | Needs scoped list strategy: assigned patients, organization patients, own record, etc. |
| Patients | `/api/health-core/patients/{id}` | GET | Get patient details | Route | `ViewPatientProfile` plus `ViewPatientContactInfo` for contact fields | Read | Demographics/contact info. | Yes | Always or summary | `View` | `PatientProfile` | Could split contact permission later. |
| Patients | `/api/health-core/patients/{patientId}/summary` | GET | Aggregated summary | Route | Composite permissions or staged summary permission | Read | Aggregates medical history, documents/results, care plan, reminders, measurements. | Yes | Consider summary audit | `View` | `PatientSummary` | Highest integration complexity; may need section-aware redaction. |
| Patients | `/api/health-core/patients` | POST | Create patient | None until created | `EditPatientProfile` | Write | Contact identity data. | Maybe internal/admin profile | Always | `Create` | `PatientProfile` | Grant creation/bootstrap should be decided with patient creation workflow. |
| Patients | `/api/health-core/patients/{id}` | PUT | Update patient/contact | Route | `EditPatientProfile` and possibly `EditPatientContactInfo` | Write | Contact identity data. | Yes | Always | `Update` | `PatientProfile` | Contact info should probably require contact-specific permission. |
| Patients | `/api/health-core/patients/{id}` | DELETE | Deactivate patient | Route | Future admin/delete permission or `EditPatientProfile` | Write | Whole patient record availability. | Yes/internal | Always | `Delete` | `PatientProfile` | High-risk operational action; consider separate permission later. |
| Timeline | `/api/health-core/patients/{patientId}/timeline` | GET | List timeline | Route | `ViewTimeline` | Read | Event sensitivity and internal visibility. | Yes | Summary or always for internal/restricted | `View` | `TimelineEvent` | Timeline is not AuditLog but may expose sensitive history. |
| Timeline | `/api/health-core/patients/{patientId}/timeline` | POST | Create manual event | Route | Future create timeline permission or `EditMedicalHistory`/module-specific permission | Write | Request sensitivity/visibility. | Yes | Always | `Create` | `TimelineEvent` | Consider restricting manual timeline creation in admin UI. |
| Timeline | `/api/health-core/timeline-events/{eventId}` | PUT | Update event | Load event | Future edit timeline permission | Write | Existing/requested sensitivity. | Yes | Always | `Update` | `TimelineEvent` | No timeline write permission exists yet. |
| Timeline | `/api/health-core/timeline-events/{eventId}` | DELETE | Soft-delete event | Load event | Future edit/delete timeline permission | Write | Entity sensitivity. | Yes | Always | `Delete` | `TimelineEvent` | Avoid confusing timeline deletion with audit deletion. |

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

## Recommended Next Coding Phase

Recommended next step: **84E Care Plan, Reminders, and Measurements enforcement and audit logging**.

Keep the rollout narrow and module-by-module:

- Protect Care Plan first because writes affect care operations and can generate reminders.
- Protect Reminders next because reminder content can reveal sensitive care details.
- Protect Measurements after that, including abnormal/restricted measurement considerations.
- Keep the temporary development fallback documented until real production authentication/JWT integration exists.
