# Security Smoke Test Plan

Phase 86 defines a practical local security smoke and E2E testing plan for Health Core authorization and audit behavior.

## 1. Purpose

Security smoke tests should quickly verify that protected Health Core endpoints:

- allow the local internal admin development context
- deny unknown product/role contexts
- keep current frontend-compatible response shapes for allowed callers
- return `403 Forbidden` for denied callers
- continue to write AuditLog entries through endpoint/controller paths

This plan does not replace unit/controller tests. It gives developers a repeatable local checklist and a path toward future production-style E2E tests.

## 2. Assumptions

- The backend API is running locally.
- The PostgreSQL database is running through Docker Compose or an equivalent local setup.
- The request-context development fallback still exists.
- Protected endpoints currently support a header-based development context.
- No public AuditLog read endpoint exists.
- The existing API smoke script can create a test patient and exercise module workflows.

## 3. Local Prerequisites

Start the backend:

```powershell
dotnet run --project .\backend\Zibzie.HealthCore.Api\Zibzie.HealthCore.Api.csproj
```

Run the regular positive-path API smoke if local data is needed:

```powershell
.\scripts\smoke-healthcore.ps1
```

Run the security smoke:

```powershell
.\scripts\smoke-security-healthcore.ps1
```

Use another API URL:

```powershell
.\scripts\smoke-security-healthcore.ps1 -BaseUrl http://localhost:5230
```

Create a patient only if none exists:

```powershell
.\scripts\smoke-security-healthcore.ps1 -CreatePatientIfMissing
```

## 4. Protected Endpoint Groups

Current protected groups:

- Patients list/detail/create/update/deactivate
- Patient summary
- Documents
- Paraclinical results
- Medical history: conditions, allergies, medications
- Care plan
- Reminders
- Measurements
- Timeline

## 5. Test Personas / Contexts

### Internal Admin / Dev Fallback

Headers:

```text
X-HealthCore-Product: InternalAdmin
X-HealthCore-Product-Role: HealthCoreAdmin
X-HealthCore-Service-Account: dev-admin
X-Correlation-ID: generated per run
```

Expected behavior:

- allowed for current local admin panel flows
- no PatientAccessGrant required because of the narrow InternalAdmin exception
- not production-safe

### Denied / Unknown Context

Headers:

```text
X-HealthCore-Product: UnknownProduct
X-HealthCore-Product-Role: UnknownRole
X-HealthCore-Service-Account: denied-smoke
X-Correlation-ID: generated per run
```

Expected behavior:

- protected endpoints return `403 Forbidden`
- denied access is audited

### Narrow Product Role

Example:

```text
X-HealthCore-Product: ClinicQueue
X-HealthCore-Product-Role: ClinicQueueReceptionist
```

Expected behavior:

- no broad clinical access
- future tests should verify denied access for documents, paraclinical results, medical history, care plan, reminders, measurements, summary, and timeline unless a valid grant and permission allow the action

### Future Grant-Scoped External Role

Examples:

- HomeVisit doctor
- Second Opinion specialist
- Personal Health Record shared provider

Expected future behavior:

- access only patients with active `PatientAccessGrant`
- no broad directory or longitudinal record browsing by default

## 6. Positive-Path Tests

Minimum local smoke:

- `GET /api/health-core/patients` with InternalAdmin headers returns `200`.
- If a patient exists:
  - `GET /api/health-core/patients/{id}/summary` with InternalAdmin headers returns `200`.
  - `GET /api/health-core/patients/{id}/documents` with InternalAdmin headers returns `200`.

Extended positive path:

- Run `scripts/smoke-healthcore.ps1` first to create a test patient and records.
- Run `scripts/smoke-security-healthcore.ps1`.
- Confirm protected patient-scoped reads succeed with InternalAdmin headers.

## 7. Denied-Path Tests

Minimum local smoke:

- `GET /api/health-core/patients` with unknown product/role returns `403`.
- If a patient exists:
  - `GET /api/health-core/patients/{id}/summary` with unknown product/role returns `403`.
  - `GET /api/health-core/patients/{id}/documents` with unknown product/role returns `403`.

Future expanded denied tests should cover every protected endpoint group:

- patients create/update/deactivate
- paraclinical results
- conditions
- allergies
- medications
- care plan
- reminders
- measurements
- timeline

## 8. Protected Endpoint Test Matrix

| Endpoint group | Example route | Required permission | Allowed context | Denied context | Success status | Denied status | Audit on success | Audit on denied |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Patients list | `GET /api/health-core/patients` | `ViewPatientDirectory` | InternalAdmin / HealthCoreAdmin | Unknown product/role | `200` | `403` | Yes, `View` / `PatientProfile` | Yes, `AccessDenied` / `PatientProfile` |
| Patient detail | `GET /api/health-core/patients/{id}` | `ViewPatientProfile` | InternalAdmin / HealthCoreAdmin | Unknown product/role | `200` or existing `404` | `403` | Yes, `View` / `PatientProfile` | Yes, `AccessDenied` / `PatientProfile` |
| Patient writes | `POST`, `PUT`, `DELETE /api/health-core/patients` | `CreatePatient`, `EditPatientProfile`, `DeactivatePatient` | InternalAdmin / HealthCoreAdmin | Unknown product/role | Existing create/update/deactivate status | `403` | Yes, create/update action | Yes, `AccessDenied` / `PatientProfile` |
| Patient summary | `GET /api/health-core/patients/{id}/summary` | `ViewPatientSummary` | InternalAdmin / HealthCoreAdmin | Unknown product/role | `200` | `403` | Yes, `View` / `PatientSummary` | Yes, `AccessDenied` / `PatientSummary` |
| Documents | `GET /api/health-core/patients/{id}/documents` | `ViewDocuments` for reads; document write permissions for writes | InternalAdmin / HealthCoreAdmin | Unknown product/role | `200` for reads | `403` | Yes, `View` / `Document` | Yes, `AccessDenied` / `Document` |
| Paraclinical results | `GET /api/health-core/patients/{id}/paraclinical-results` | `ViewParaclinicalResults` for reads; `EditParaclinicalResults` for writes | InternalAdmin / HealthCoreAdmin | Unknown product/role | `200` for reads | `403` | Yes, `View` / `ParaclinicalResult` | Yes, `AccessDenied` / `ParaclinicalResult` |
| Conditions | `GET /api/health-core/patients/{id}/conditions` | `ViewMedicalHistory` for reads; `EditMedicalHistory` for writes | InternalAdmin / HealthCoreAdmin | Unknown product/role | `200` for reads | `403` | Yes, `View` / `Condition` | Yes, `AccessDenied` / `Condition` |
| Allergies | `GET /api/health-core/patients/{id}/allergies` | `ViewMedicalHistory` for reads; `EditMedicalHistory` for writes | InternalAdmin / HealthCoreAdmin | Unknown product/role | `200` for reads | `403` | Yes, `View` / `Allergy` | Yes, `AccessDenied` / `Allergy` |
| Medications | `GET /api/health-core/patients/{id}/medications` | `ViewMedicalHistory` for reads; `EditMedicalHistory` for writes | InternalAdmin / HealthCoreAdmin | Unknown product/role | `200` for reads | `403` | Yes, `View` / `Medication` | Yes, `AccessDenied` / `Medication` |
| Care plan | `GET /api/health-core/patients/{id}/care-plan` | `ViewCarePlan` for reads; care-plan write permissions for writes | InternalAdmin / HealthCoreAdmin | Unknown product/role | `200` for reads | `403` | Yes, `View` / `CarePlanItem` | Yes, `AccessDenied` / `CarePlanItem` |
| Reminders | `GET /api/health-core/patients/{id}/reminders` | `ViewReminders` for reads; reminder write permissions for writes | InternalAdmin / HealthCoreAdmin | Unknown product/role | `200` for reads | `403` | Yes, `View` / `Reminder` | Yes, `AccessDenied` / `Reminder` |
| Measurements | `GET /api/health-core/patients/{id}/measurements` | `ViewMeasurements` for reads; `CreateMeasurement` / `EditMeasurement` for writes | InternalAdmin / HealthCoreAdmin | Unknown product/role | `200` for reads | `403` | Yes, `View` / `Measurement` | Yes, `AccessDenied` / `Measurement` |
| Timeline | `GET /api/health-core/patients/{id}/timeline` | `ViewTimeline` for reads; timeline write permissions for writes | InternalAdmin / HealthCoreAdmin | Unknown product/role | `200` for reads | `403` | Yes, `View` / `TimelineEvent` | Yes, `AccessDenied` / `TimelineEvent` |

The current script intentionally exercises only the safest read checks. Write checks should stay in controller/unit tests or future isolated E2E data setup because create/update/deactivate flows need stable cleanup rules.

## 9. Audit-Log Verification Strategy

AuditLog is not exposed through a public endpoint and must not be exposed casually through Timeline or patient-facing APIs.

Verification options:

1. Unit/controller tests:
   - current focused tests verify audit service calls for allowed and denied paths.
2. Local database query:
   - useful for developer verification.
   - do not store secrets in scripts or docs.
3. Future strict admin-only AuditLog endpoint:
   - if implemented, must require `ViewAuditLog`.
   - access to audit logs must itself be audited.

Example local SQL for recent denied access checks:

```sql
select
    "CreatedAt",
    "ProductCode",
    "ProductRole",
    "ActionType",
    "ResourceType",
    "Permission",
    "Succeeded",
    "FailureReason",
    "CorrelationId"
from "AuditLogEntries"
where "ActionType" = 'AccessDenied'
order by "CreatedAt" desc
limit 20;
```

Example local SQL by correlation id:

```sql
select
    "CreatedAt",
    "ActionType",
    "ResourceType",
    "Permission",
    "Succeeded",
    "PatientId",
    "ResourceId"
from "AuditLogEntries"
where "CorrelationId" = '<correlation-id-from-smoke-output>'
order by "CreatedAt";
```

## 10. Dev Fallback Caveat

The header/default development fallback exists to keep the local admin panel usable before production authentication exists.

As of Phase 87B, fallback is controlled by `HealthCoreAuth` configuration. Base/default configuration disables fallback, Development enables it, and the request-context provider ignores fallback in Production even if configuration accidentally enables it.

The local security smoke script uses header fallback and is therefore a Development/local test tool. Future production tests must use signed JWT or trusted service identity rather than arbitrary headers.

## 11. Future Production JWT Test Plan

See [Production auth and JWT strategy](production-auth-jwt-strategy.md) for the proposed claim contract, product context model, and environment fallback policy.

See [Admin login and frontend JWT integration strategy](admin-login-frontend-integration-strategy.md) for the planned admin UI token flow.

JWT bearer authentication is wired as of Phase 87C, and Phase 87E1 adds internal admin login/JWT issuance. The current local security smoke still uses Development header fallback. Future production-style tests should verify:

- admin login returns a JWT with `InternalAdmin` product context
- frontend `/login` stores the token and browser-side API calls attach `Authorization: Bearer`
- valid JWT/service identity with product claims is allowed only within grants/scopes
- missing JWT is denied
- invalid signature is denied before endpoint logic
- expired token is denied
- product role without permission is denied
- active grant allows scoped access
- expired/revoked grant denies access
- emergency access requires explicit emergency grant/reason
- access denied is always audited

## 12. E2E Automation Roadmap

Recommended stages:

1. Keep current PowerShell local security smoke for quick developer checks.
2. Add database-level AuditLog verification for local/dev CI where DB access is available.
3. Add API integration tests with a real test server and test authentication handler.
4. Add product-specific E2E tests for DigiCare, HomeVisit, Second Opinion, Personal Health Record, and Clinic Queue.
5. Add grant lifecycle tests after grant creation/revocation APIs exist.
6. Add CI-friendly security smoke that provisions test data and cleans it up safely.

## 13. Known Limitations

- Current security smoke script does not verify AuditLog rows directly.
- Current script does not test every protected endpoint group.
- Current script depends on local dev/header fallback.
- Production JWT/service identity and frontend token flows are not complete yet.
- Current frontend token storage is temporary `localStorage`; server-rendered pages still need a cookie/session or proxy strategy before fallback can be removed.
- See [Server-side admin auth and session strategy](server-side-admin-auth-session-strategy.md) for the planned SSR/session test path.
- No grant creation/revocation workflow exists yet.
- No grant-scoped patient directory filtering exists yet.
- Patient Summary partial filtering/redaction is deferred.
- Audit volume optimization is deferred.
