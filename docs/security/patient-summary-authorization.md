# Patient Summary Authorization

Phase 84E5 protects the Patient Summary endpoint with authorization and audit logging.

This phase is intentionally limited to:

- `GET /api/health-core/patients/{patientId}/summary`

It does not protect standalone Patient Profile endpoints or Timeline endpoints.

## Protected Route

| Route | Method | Permission | Audit success action | Audit resource |
|---|---:|---|---|---|
| `/api/health-core/patients/{patientId}/summary` | GET | `ViewPatientSummary` | `View` | `PatientSummary` |

## Current Strategy

Phase 84E5 uses the conservative all-or-nothing strategy recommended in Phase 84E4.

Allowed callers receive the existing `PatientSummaryDto` response shape unchanged.

Denied callers receive `403 Forbidden`.

No partial section-level filtering or redaction is implemented in this phase.

## Current Summary Contents

The current backend summary includes:

- patient identity/profile fields
- contact information
- conditions
- allergies
- current medications

It does not currently include documents, paraclinical results, care plan, reminders, measurements, or timeline events.

The frontend Patient Record Shell loads care plan, reminders, measurements, and paraclinical results through their own endpoints. Those endpoint groups are already protected and audited.

## Audit Logging

Successful summary views are audited with:

- `AuditActionTypes.View`
- `AuditResourceTypes.PatientSummary`
- `HealthPermissions.ViewPatientSummary`
- patient id
- request context metadata

Denied attempts are audited with:

- `AuditActionTypes.AccessDenied`
- `AuditResourceTypes.PatientSummary`
- `HealthPermissions.ViewPatientSummary`
- patient id
- request context metadata
- authorization denial reason

## Audit Volume

The Patient Summary endpoint may be called frequently by the admin overview.

For Phase 84E5, successful reads are audited following the current protected endpoint pattern. If audit volume becomes high, a future phase may aggregate or summarize frequent successful summary reads. Denied attempts must always be logged.

## Development Fallback

JWT bearer authentication and internal admin login are now available, but real production identity/frontend integration is still incomplete.

The Phase 84B1 request-context fallback remains active:

- `ProductCode = InternalAdmin`
- `ProductRole = HealthCoreAdmin`
- `ServiceAccountId = dev-admin`

This keeps the development admin panel usable. It is marked as fallback context and is not production-safe.

## Deferred Work

Phase 84E6 should handle partial summary filtering/redaction if product-specific access requires it.

Future work should:

- add explicit summary section redaction/omission metadata
- update frontend handling so "not authorized" is not confused with "no records"
- add audit metadata for returned and redacted sections

Timeline and standalone Patient Profile endpoints remain separate future enforcement phases.
