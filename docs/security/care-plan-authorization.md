# Care Plan Authorization

Phase 84E1 protects the Care Plan endpoint group with authorization and audit logging.

Documents, Paraclinical Results, and current Medical History endpoints were protected in earlier phases. This phase is limited to Care Plan items.

## Protected Routes

| Route | Method | Permission | Audit success action | Audit resource |
|---|---:|---|---|---|
| `/api/health-core/patients/{patientId}/care-plan` | GET | `ViewCarePlan` | `View` | `CarePlanItem` |
| `/api/health-core/patients/{patientId}/care-plan` | POST | `CreateCarePlanItem` | `Create` | `CarePlanItem` |
| `/api/health-core/care-plan-items/{itemId}` | GET | `ViewCarePlan` | `View` | `CarePlanItem` |
| `/api/health-core/care-plan-items/{itemId}` | PUT | `EditCarePlanItem`, `CompleteCarePlanItem`, or `CancelCarePlanItem` | `Update` | `CarePlanItem` |
| `/api/health-core/care-plan-items/{itemId}` | DELETE | `EditCarePlanItem` | `Delete` | `CarePlanItem` |

Update uses `CompleteCarePlanItem` when the requested status is `Completed`, and `CancelCarePlanItem` when the requested status is `Cancelled`. Other updates use `EditCarePlanItem`.

## Audit Logging

Successful actions are audited with:

- `AuditActionTypes.View`
- `AuditActionTypes.Create`
- `AuditActionTypes.Update`
- `AuditActionTypes.Delete`

Denied attempts are audited with:

- `AuditActionTypes.AccessDenied`
- `AuditResourceTypes.CarePlanItem`
- attempted permission
- patient id when available
- care plan item id when available
- request context metadata
- authorization denial reason

## Sensitivity Handling

Care Plan endpoints call the authorization service through section-aware methods:

- reads use `CanViewPatientSectionAsync`
- writes use `CanEditPatientSectionAsync`

Single-item routes use the care plan item's current `SensitivityLevel`.

Create uses the requested `SensitivityLevel`.

Update uses the requested `SensitivityLevel` when supplied; otherwise it uses the existing item sensitivity.

List endpoints use baseline `ViewCarePlan` because they can return mixed-sensitivity items and do not perform per-record redaction yet. A future phase may add filtering or redaction for mixed-sensitivity lists if needed.

## Operational Sensitivity

Care Plan is operationally sensitive because it can drive follow-up, reminders, tasks, and care-team decisions.

Phase 84E1 only adds endpoint authorization and audit logging. Existing automation and timeline side effects are unchanged:

- care plan creation can still create a due reminder when existing rules apply
- care plan creation still preserves existing timeline behavior
- no reminder generation logic was changed
- no timeline behavior was changed

## Development Fallback

JWT bearer authentication and internal admin login are now available, but real production identity/frontend integration is still incomplete.

The Phase 84B1 request-context fallback remains active:

- `ProductCode = InternalAdmin`
- `ProductRole = HealthCoreAdmin`
- `ServiceAccountId = dev-admin`

This keeps the development admin panel usable. It is marked as fallback context and is not production-safe.

## Not Included Yet

- This document only describes the Care Plan enforcement phase; later phases protected additional endpoint groups.
- No frontend changes.
- No production identity rollout or frontend JWT integration.
- No access grant creation workflow.
