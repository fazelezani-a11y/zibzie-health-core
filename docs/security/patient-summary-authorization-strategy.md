# Patient Summary Authorization Strategy

Phase 84E4 is a strategy-only phase for Patient Summary / Overview authorization.

No endpoint enforcement is added in this phase.

## Current Route

| Route | Controller | Service | Current behavior |
|---|---|---|---|
| `GET /api/health-core/patients/{patientId}/summary` | `PatientsController.GetPatientSummary` | `PatientSummaryService.GetPatientSummaryAsync` | Returns the current patient summary or `404` when the patient is not active/found. |

## Current Backend Summary Shape

`PatientSummaryDto` currently contains:

- patient identity: `Id`, `FirstName`, `LastName`, computed `FullName`
- basic profile facts: `BirthDate`, `NationalCode`, `Gender`, `BloodType`
- contact info: `MobileNumber`, `Email`, `EmergencyContactName`, `EmergencyContactPhone`, `HomeAddress`, `WorkAddress`
- medical history lists:
  - all non-deleted conditions
  - all non-deleted allergies
  - current non-deleted medications only

The backend summary currently does not include:

- documents
- paraclinical results
- care plan items
- reminders
- measurements
- timeline events

The frontend Patient Record Shell receives this summary first, then loads extra overview data through separate endpoints for care plan, reminders, measurements, and paraclinical results. Those separate endpoints are already protected in earlier phases.

## Field Permission Matrix

| Summary field / section | Source domain | Sensitivity risk | Required permission | If permission missing | Audit recommendation | Notes |
|---|---|---|---|---|---|---|
| `Id` | Patient profile | Low by itself, but still patient scoped | `ViewPatientProfile` plus future `ViewPatientSummary` | Deny whole summary in first enforcement; later include only in redacted shell if needed | Audit as `View` / `PatientSummary` | Required for frontend section routing and follow-up calls. |
| `FirstName`, `LastName`, `FullName` | Patient profile | Identity data | `ViewPatientProfile` plus future `ViewPatientSummary` | Deny whole summary in first enforcement | Audit as `View` / `PatientSummary` | May be visible to many roles, but still centrally controlled. |
| `BirthDate`, `Gender`, `BloodType` | Patient profile | Clinical/demographic profile | `ViewPatientProfile` plus future `ViewPatientSummary` | Deny whole summary in first enforcement | Audit as `View` / `PatientSummary` | Blood type can be clinically relevant and should not be exposed to non-clinical roles by accident. |
| `NationalCode` | Patient profile/contact identity | High identity/privacy risk | `ViewPatientProfile` and likely `ViewPatientContactInfo` plus future `ViewPatientSummary` | Redact later; deny whole summary in first enforcement | Audit as `View` / `PatientSummary` | Consider treating national code as contact/identity-sensitive. |
| `MobileNumber`, `Email` | Contact info | Contact/privacy risk | `ViewPatientContactInfo` plus future `ViewPatientSummary` | Redact later; deny whole summary in first enforcement | Audit as `View` / `PatientSummary` | Clinic queue and logistics roles may need contact without clinical data. |
| `EmergencyContactName`, `EmergencyContactPhone` | Contact info | Family/contact privacy risk | `ViewPatientContactInfo` plus future `ViewPatientSummary` | Redact later; deny whole summary in first enforcement | Audit as `View` / `PatientSummary` | May require special handling for family/privacy later. |
| `HomeAddress`, `WorkAddress` | Contact info | Location/privacy risk | `ViewPatientContactInfo` plus future `ViewPatientSummary` | Redact later; deny whole summary in first enforcement | Audit as `View` / `PatientSummary` | HomeVisit may need address; many clinical viewers do not need work address. |
| `Conditions` | Medical history | Clinical and potentially restricted | `ViewMedicalHistory` plus future `ViewPatientSummary` | Omit/redact later; deny whole summary in first enforcement | Audit as `View` / `PatientSummary`; consider metadata flag when redacted later | Current list includes treatment summary, clinician note, source, verification, sensitivity. |
| `Allergies` | Medical history | Safety-critical but still clinical | `ViewMedicalHistory` plus future `ViewPatientSummary` | Omit/redact later; deny whole summary in first enforcement | Audit as `View` / `PatientSummary`; consider metadata flag when redacted later | Some products may need allergy-only safety summaries later. |
| `CurrentMedications` | Medical history | Medication and disease-control signal | `ViewMedicalHistory` plus future `ViewPatientSummary` | Omit/redact later; deny whole summary in first enforcement | Audit as `View` / `PatientSummary`; consider metadata flag when redacted later | Current medications can reveal diagnosis and treatment status. |
| Frontend extra care plan overview data | Care Plan endpoint | Operational health actions | `ViewCarePlan` | Already handled by protected care-plan endpoint; frontend currently tolerates partial fetch failure | Audited by care-plan endpoint | Not returned by backend summary today. |
| Frontend extra reminder overview data | Reminders endpoint | Follow-up, medication, care details | `ViewReminders` | Already handled by protected reminder endpoint; frontend currently tolerates partial fetch failure | Audited by reminder endpoint | Not returned by backend summary today. |
| Frontend extra measurement overview data | Measurements endpoint | Trendable clinical/lifestyle data | `ViewMeasurements` | Already handled by protected measurement endpoint; frontend currently tolerates partial fetch failure | Audited by measurement endpoint | Not returned by backend summary today. |
| Frontend extra paraclinical overview data | Paraclinical endpoint | Clinical results and abnormal flags | `ViewParaclinicalResults` | Already handled by protected paraclinical endpoint; frontend currently tolerates partial fetch failure | Audited by paraclinical endpoint | Not returned by backend summary today. |
| Timeline/recent events if added later | Timeline | Clinical/operational history | `ViewTimeline` plus future `ViewPatientSummary` if embedded | Omit/redact later | Audit summary view and/or timeline endpoint view | Not returned by backend summary today. |

## Authorization Model Options

### Option 1: All-or-nothing summary

Require a single broad summary permission and return either the full summary or `403`.

Positive:

- Simple to implement.
- Safer than accidentally leaking mixed domains.
- Minimal frontend impact.

Tradeoff:

- Too restrictive for product-specific roles that can view demographics but not clinical history.
- Too permissive if the summary permission is granted without checking underlying domains.

### Option 2: Partial summary filtering

Return only sections the caller can view.

Positive:

- Correct long-term model for product-specific access.
- Supports least privilege and scoped product views.

Tradeoff:

- Current DTO has no explicit redaction/omission metadata.
- Frontend may interpret omitted arrays as "no records" instead of "not authorized".
- Requires careful UI and API contract changes.

### Option 3: Two-layer model

Require a baseline summary permission, then authorize each included section with its domain permission.

Recommended target model:

- baseline permission: future `ViewPatientSummary`
- profile/basic demographics: `ViewPatientProfile`
- contact fields: `ViewPatientContactInfo`
- conditions/allergies/medications: `ViewMedicalHistory`
- documents if embedded later: `ViewDocuments`
- paraclinical if embedded later: `ViewParaclinicalResults`
- care plan if embedded later: `ViewCarePlan`
- reminders if embedded later: `ViewReminders`
- measurements/graphs if embedded later: `ViewMeasurements`
- timeline/recent events if embedded later: `ViewTimeline`

If a section is unauthorized, future behavior should either omit the section with explicit redaction metadata or return a redacted marker. The endpoint must not leak counts from unauthorized sections unless a product-specific permission explicitly allows count-only metadata.

## Recommended Decision

Use Option 3 as the target model.

For the first enforcement phase, use a conservative all-or-nothing implementation because the current DTO and frontend do not distinguish "empty" from "redacted".

Recommended Phase 84E5 behavior:

- Add `ViewPatientSummary` to the permission catalog and product access profiles.
- Protect `GET /api/health-core/patients/{patientId}/summary`.
- Require an active patient access grant through the existing authorization service.
- For the current DTO, require:
  - `ViewPatientSummary`
  - `ViewPatientProfile`
  - `ViewPatientContactInfo`
  - `ViewMedicalHistory`
- Return `403` if any required current-summary permission is missing.
- Preserve the current response shape for allowed callers.
- Audit success and denial.

Recommended later Phase 84E6 behavior:

- Add a summary DTO contract that can explicitly represent omitted/redacted sections.
- Apply section-level filtering instead of all-or-nothing denial.
- Add frontend handling for redacted sections.
- Add `MetadataJson` audit details describing which sections were returned or redacted.

## Permission Catalog Gap Check

Existing permissions:

- `ViewPatientProfile` exists.
- `ViewPatientContactInfo` exists.
- `ViewMedicalHistory` exists.
- `ViewDocuments` exists.
- `ViewParaclinicalResults` exists.
- `ViewCarePlan` exists.
- `ViewReminders` exists.
- `ViewMeasurements` exists.
- `ViewTimeline` exists.

Missing permissions:

- `ViewPatientSummary` does not exist yet.
- `ViewBasicPatientInfo` does not exist yet.

Recommendation:

- Add `ViewPatientSummary` in the next coding phase.
- Do not add `ViewBasicPatientInfo` yet unless a real product role needs demographics without contact or clinical summary.

## Audit Strategy

Denied Patient Summary access should always be audited:

- `AuditActionTypes.AccessDenied`
- `AuditResourceTypes.PatientSummary`
- attempted permission, preferably `ViewPatientSummary`
- patient id
- product code and product role
- user id or service account id
- denial reason
- request metadata

Successful Patient Summary access should be audited:

- `AuditActionTypes.View`
- `AuditResourceTypes.PatientSummary`
- patient id
- product code and product role
- matched access scope when available
- request metadata

Frequent dashboard polling can create audit noise. A future optimization may aggregate or summarize successful summary reads, but denied access and restricted-data access must always be logged.

If partial summary filtering is implemented later, the audit entry should include redaction/returned-section metadata in `MetadataJson`, for example:

- `returnedSections`
- `redactedSections`
- `isPartialSummary`

## Frontend Impact

Current frontend behavior:

- `frontend/src/app/patients/[id]/page.tsx` server-loads `getPatientSummary`.
- If the summary returns `403`, the current page treats it as a generic summary load error, not a section-level redaction.
- `PatientRecordShell` expects patient identity, contact fields, conditions, allergies, and current medications to exist in the initial summary.
- The overview then calls care plan, reminders, measurements, and paraclinical APIs separately using `Promise.allSettled`.
- Those supplemental overview calls can fail independently and show a calm informational message.

Impact of first enforcement:

- Internal admin/dev fallback should continue to see the full summary if product profiles include the required permissions.
- Non-internal products without full current-summary permissions would receive `403`.
- This is safer than silently returning empty clinical arrays.

Impact of partial filtering later:

- Frontend must handle missing/redacted sections explicitly.
- UI copy should distinguish "not authorized" from "no records".
- External product UIs should not assume every summary section is present.

## Enforcement Rollout Proposal

Recommended next coding phase: **84E5 Patient Summary authorization foundation**.

Scope:

- Add `ViewPatientSummary`.
- Update product access profiles conservatively.
- Protect only `GET /api/health-core/patients/{patientId}/summary`.
- Audit success and denied attempts.
- Keep current response shape for allowed callers.
- Use conservative all-or-nothing current-summary permission checks.
- Do not implement partial filtering yet.

Later phase: **84E6 Partial summary filtering**.

Scope:

- Introduce explicit redaction/omission contract.
- Add section-level checks.
- Update frontend handling.
- Add audit metadata for returned/redacted sections.

Do not protect Timeline or Patient Profile list/detail in Phase 84E5 unless explicitly scoped by a separate phase.
