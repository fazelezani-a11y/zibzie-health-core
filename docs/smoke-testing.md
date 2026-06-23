# Health Core Smoke Testing

This smoke test verifies the local Health Core API across the current MVP modules.
It is intended for developer use after the database and backend are already running.

## Prerequisites

- Docker PostgreSQL database is running.
- Backend API is running locally.
- No frontend server is required.

Start the backend:

```powershell
dotnet run --project .\backend\Zibzie.HealthCore.Api\Zibzie.HealthCore.Api.csproj
```

By default, the smoke script targets:

```text
http://localhost:5230
```

## Run

From the repository root:

```powershell
.\scripts\smoke-healthcore.ps1
```

If local execution policy blocks direct script execution, run it without changing
machine policy:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\smoke-healthcore.ps1
```

Use a different backend URL:

```powershell
.\scripts\smoke-healthcore.ps1 -BaseUrl http://localhost:5230
```

## Security Smoke

Phase 86 adds a smaller security-focused smoke script:

```powershell
.\scripts\smoke-security-healthcore.ps1
```

If local execution policy blocks direct script execution:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\smoke-security-healthcore.ps1
```

Use a different backend URL:

```powershell
.\scripts\smoke-security-healthcore.ps1 -BaseUrl http://localhost:5230
```

The security smoke is non-destructive by default. It verifies:

- `/health` is healthy.
- `GET /api/health-core/patients` is allowed with InternalAdmin development headers.
- `GET /api/health-core/patients` is denied with an unknown product/role.
- If a patient exists, patient summary and documents list are allowed for InternalAdmin and denied for the unknown product/role.

If no local patient exists, either run the full smoke script first or allow the
security smoke to create a local test patient:

```powershell
.\scripts\smoke-security-healthcore.ps1 -CreatePatientIfMissing
```

Fallback mode does not directly verify `AuditLog` rows. JWT mode can verify the
protected AuditLog review endpoint when a patient is available. See
`docs/security/security-smoke-test-plan.md` for deeper database-query
verification options.

## Fallback-Off JWT Smoke

For production-like local or staging verification, run the backend with
development auth fallback disabled:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:HealthCoreAuth__AllowHeaderFallback = "false"
$env:HealthCoreAuth__AllowDefaultDevFallback = "false"
dotnet run --project .\backend\Zibzie.HealthCore.Api\Zibzie.HealthCore.Api.csproj
```

Then run JWT-required smoke from another PowerShell session:

```powershell
.\scripts\smoke-security-healthcore.ps1 `
  -Mode Jwt `
  -BaseUrl http://localhost:5230 `
  -AdminUsername "<local-admin-username>" `
  -AdminPassword "<local-admin-password>"
```

Do not commit or document real usernames, passwords, or JWTs.

JWT mode verifies:

- `/health` is healthy.
- unauthenticated patient directory is denied.
- InternalAdmin development headers are denied when fallback is off.
- admin login returns a bearer token without printing it.
- `/api/health-core/auth/admin/me` works with the bearer token.
- patient directory works with the bearer token.
- if a patient exists, summary, documents, access-grant list, and audit-log review work with the bearer token.
- if a patient exists, unauthenticated access-grant and audit-log review requests are denied.

Use `-PatientId "<existing-patient-id>"` for deterministic patient-scoped checks,
or `-CreatePatientIfMissing` only in local development when creating a test
patient is acceptable.

Phase 99 status:

- script parser check passed on 2026-06-23
- live JWT smoke was not run because no local backend was listening on `http://localhost:5230`
- no admin username, password, or JWT was recorded
- staging/prod-like execution evidence is still required before production use

## Coverage

The script creates a unique test patient and verifies:

- Patient create, list/search, and summary.
- Condition, allergy, and medication create plus summary inclusion.
- Manual timeline event create/list.
- Document metadata create/list and timeline auto-event.
- Paraclinical result with one lab item create/list and timeline auto-event.
- Care plan item create/list/update to `Completed` and timeline auto-event.
- Reminder create/list/update to `Done` and timeline auto-event.
- Two `Weight` measurements create/list/filter and timeline auto-events.

The script prints `PASS` or `FAIL` for each section and exits with a non-zero code
if a required check fails.

## Data Cleanup

This first version does not delete data automatically. It only creates records under
the unique smoke-test patient it creates, and prints that patient id at the end.
