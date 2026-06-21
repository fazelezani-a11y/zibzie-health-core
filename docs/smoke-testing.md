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

The security smoke does not directly verify `AuditLog` rows because no public
AuditLog read endpoint exists. See `docs/security/security-smoke-test-plan.md`
for database-query verification options.

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
