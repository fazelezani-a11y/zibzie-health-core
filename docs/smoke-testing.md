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
