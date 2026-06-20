# Patient Profile and Directory Authorization

Phase 84H2 protects patient read endpoints:

- `GET /api/health-core/patients`
- `GET /api/health-core/patients/{id}`

The patient summary endpoint was already protected in Phase 84E5. Patient create, update, and deactivate remain pending for Phase 84H3.

## Required permissions

| Endpoint | Permission | Audit resource | Notes |
| --- | --- | --- | --- |
| `GET /api/health-core/patients` | `ViewPatientDirectory` | `PatientProfile` | Protects patient directory/list/search. |
| `GET /api/health-core/patients/{id}` | `ViewPatientProfile` | `PatientProfile` | Protects patient detail/profile read. |

`ViewPatientContactInfo` already exists, but this phase does not implement partial contact redaction. The current detail DTO includes contact fields, so the detail endpoint remains all-or-nothing under `ViewPatientProfile`. Contact-level splitting is deferred.

## Patient existence leakage

Patient list/search can reveal whether a person exists in Health Core. Phase 84H2 blocks unauthorized callers before returning list/detail data.

For the list endpoint, the current DTO and search behavior are preserved for allowed callers. No DTO minimization was added in this phase. Future phases should avoid returning national code, mobile number, email, or address in broad directory results unless the caller also has stronger contact/identifier permissions.

For the detail endpoint, unauthorized callers receive `403 Forbidden`. Allowed callers still receive the existing response shape, and not-found behavior remains unchanged after authorization succeeds.

## Audit logging

Successful and denied patient directory/detail reads are audited.

List/search audit:

- `ActionType`: `View` or `AccessDenied`
- `ResourceType`: `PatientProfile`
- `Permission`: `ViewPatientDirectory`
- `PatientId`: null
- `MetadataJson`: page, page size, and whether search was present
- Raw search text is not stored because it may contain national code, phone, or email.

Detail audit:

- `ActionType`: `View` or `AccessDenied`
- `ResourceType`: `PatientProfile`
- `PatientId`: route id
- `ResourceId`: route id
- `Permission`: `ViewPatientProfile`

## Development fallback caveat

The current request-context development fallback remains in place:

- `ProductCode`: `InternalAdmin`
- `ProductRole`: `HealthCoreAdmin`
- `ServiceAccountId`: `dev-admin`

This keeps the local admin panel usable while real authentication is not implemented. It is not production-safe.

## Deferred work

- Protect patient create/update/deactivate in Phase 84H3.
- Add DTO minimization for directory/list results.
- Add grant-scoped patient directory filtering for external products.
- Add contact-level partial redaction or split detail endpoints if needed.
- Replace development fallback with production JWT or service-to-service authentication.
