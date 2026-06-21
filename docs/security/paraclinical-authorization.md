# Paraclinical Results Authorization

Phase 84C protects the Paraclinical Results endpoint group with authorization and audit logging.

Documents were protected in Phase 84B2. Other endpoint groups remain unchanged in this phase.

## Protected Routes

| Route | Method | Permission | Audit success action | Audit resource |
|---|---:|---|---|---|
| `/api/health-core/patients/{patientId}/paraclinical-results` | GET | `ViewParaclinicalResults` | `View` | `ParaclinicalResult` |
| `/api/health-core/patients/{patientId}/paraclinical-results` | POST | `EditParaclinicalResults` | `Create` | `ParaclinicalResult` |
| `/api/health-core/paraclinical-results/{resultId}` | GET | `ViewParaclinicalResults` | `View` | `ParaclinicalResult` |
| `/api/health-core/paraclinical-results/{resultId}` | PUT | `EditParaclinicalResults` | `Update` | `ParaclinicalResult` |
| `/api/health-core/paraclinical-results/{resultId}` | DELETE | `EditParaclinicalResults` | `Delete` | `ParaclinicalResult` |

There are no separate lab-item endpoints yet. Lab items are returned and created as part of paraclinical result payloads.

## Audit Logging

Successful actions are audited with:

- `AuditActionTypes.View`
- `AuditActionTypes.Create`
- `AuditActionTypes.Update`
- `AuditActionTypes.Delete`

Denied attempts are audited with:

- `AuditActionTypes.AccessDenied`
- `AuditResourceTypes.ParaclinicalResult`
- attempted permission
- patient id when available
- result id when available
- request context metadata
- authorization denial reason

## Sensitivity and Abnormal Results

Paraclinical endpoints call the authorization service through section-aware methods:

- reads use `CanViewPatientSectionAsync`
- writes use `CanEditPatientSectionAsync`

Single-result routes use the result's current `SensitivityLevel`.

Create uses the requested `SensitivityLevel`.

Update uses the requested `SensitivityLevel` when supplied; otherwise it uses the existing result sensitivity.

List uses the optional `sensitivityLevel` query value when provided. A future phase may add per-record filtering/redaction for mixed-sensitivity result lists.

Abnormal and follow-up flags are included in ordinary paraclinical result responses today. Baseline access uses `ViewParaclinicalResults`. If a future endpoint specifically exposes abnormal-result work queues or high-risk summaries, it should require `ViewAbnormalResults`.

## Development Fallback

JWT bearer authentication and internal admin login are now available, but real production identity/frontend integration is still incomplete.

The Phase 84B1 request-context fallback remains active:

- `ProductCode = InternalAdmin`
- `ProductRole = HealthCoreAdmin`
- `ServiceAccountId = dev-admin`

This keeps the development admin panel usable. It is marked as fallback context and is not production-safe.

## Not Included Yet

- This document only describes the Paraclinical enforcement phase; later phases protected additional endpoint groups.
- No frontend changes.
- No production identity rollout or frontend JWT integration.
- No access grant creation workflow.
