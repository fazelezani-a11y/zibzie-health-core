# Patient Profile and Directory Authorization

Phase 84H2 protects patient read endpoints:

- `GET /api/health-core/patients`
- `GET /api/health-core/patients/{id}`

Phase 84H3 protects patient write/admin endpoints:

- `POST /api/health-core/patients`
- `PUT /api/health-core/patients/{id}`
- `DELETE /api/health-core/patients/{id}`

The patient summary endpoint was already protected in Phase 84E5.

## Required permissions

| Endpoint | Permission | Audit resource | Notes |
| --- | --- | --- | --- |
| `GET /api/health-core/patients` | `ViewPatientDirectory` | `PatientProfile` | Protects patient directory/list/search. |
| `GET /api/health-core/patients/{id}` | `ViewPatientProfile` | `PatientProfile` | Protects patient detail/profile read. |
| `POST /api/health-core/patients` | `CreatePatient` | `PatientProfile` | Protects patient creation. |
| `PUT /api/health-core/patients/{id}` | `EditPatientProfile` | `PatientProfile` | Protects profile/contact update as an all-or-nothing write for now. |
| `DELETE /api/health-core/patients/{id}` | `DeactivatePatient` | `PatientProfile` | Protects soft deactivation. No hard-delete behavior was added. |

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

Create audit:

- `ActionType`: `Create` or `AccessDenied`
- `ResourceType`: `PatientProfile`
- `PatientId`: created patient id on success; null on denied access
- `ResourceId`: created patient id on success; null on denied access
- `Permission`: `CreatePatient`

Update audit:

- `ActionType`: `Update` or `AccessDenied`
- `ResourceType`: `PatientProfile`
- `PatientId`: route id
- `ResourceId`: route id
- `Permission`: `EditPatientProfile`

Deactivate audit:

- `ActionType`: `Update` or `AccessDenied`
- `ResourceType`: `PatientProfile`
- `PatientId`: route id
- `ResourceId`: route id
- `Permission`: `DeactivatePatient`
- The endpoint remains soft deactivation by setting `IsActive = false`; no hard-delete semantics were introduced.

## Development fallback caveat

The current request-context development fallback remains in place:

- `ProductCode`: `InternalAdmin`
- `ProductRole`: `HealthCoreAdmin`
- `ServiceAccountId`: `dev-admin`

This keeps the local admin panel usable while real authentication is not implemented. It is not production-safe.

## Deferred work

- Add DTO minimization for directory/list results.
- Add grant-scoped patient directory filtering for external products.
- Add contact-level partial redaction or split detail endpoints if needed.
- Replace development fallback with production JWT or service-to-service authentication.
