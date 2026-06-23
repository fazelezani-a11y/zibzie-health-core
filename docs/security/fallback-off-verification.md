# Fallback-Off Verification

Phase 87E3 defines how to verify the real admin JWT/session/proxy flow while keeping Development fallback available for ordinary local work.

## Purpose

Health Core still supports Development fallback so the admin panel remains easy to run locally. Before that fallback can be retired outside local development, the team needs a repeatable way to prove that:

- unauthenticated protected requests are denied
- admin login issues a valid JWT
- Next.js stores that JWT in the `zibzie_admin_access_token` httpOnly cookie
- server components and browser API calls can reach Health Core through the cookie-backed session path
- logout clears the session

This phase does not remove fallback and does not change endpoint permissions.

## Current Fallback Behavior

Request context is resolved in this order:

1. authenticated JWT claims
2. configured header fallback
3. configured default Development fallback

Base `appsettings.json` disables fallback:

```json
"HealthCoreAuth": {
  "AllowHeaderFallback": false,
  "AllowDefaultDevFallback": false
}
```

`appsettings.Development.json` keeps fallback enabled for normal local development:

```json
"HealthCoreAuth": {
  "AllowHeaderFallback": true,
  "AllowDefaultDevFallback": true
}
```

The provider ignores fallback in Production even if configuration accidentally enables it.

## Run Backend With Fallback Disabled

Use environment variables to override the Development settings without editing `appsettings.Development.json`:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:HealthCoreAuth__AllowHeaderFallback = "false"
$env:HealthCoreAuth__AllowDefaultDevFallback = "false"
dotnet run --project .\backend\Zibzie.HealthCore.Api\Zibzie.HealthCore.Api.csproj
```

If no admin user exists yet, use the existing local bootstrap configuration for a one-time Development seed. Use a local-only password and do not commit secrets:

```powershell
$env:AdminAuth__BootstrapAdmin__Enabled = "true"
$env:AdminAuth__BootstrapAdmin__Username = "<local-admin-username>"
$env:AdminAuth__BootstrapAdmin__Password = "<local-admin-password>"
$env:AdminAuth__BootstrapAdmin__DisplayName = "Local Admin"
$env:AdminAuth__BootstrapAdmin__ProductRole = "HealthCoreAdmin"
```

After the admin exists, disable or clear the bootstrap variables before normal testing.

## Backend Smoke Script

Fallback-enabled Development mode remains the default:

```powershell
.\scripts\smoke-security-healthcore.ps1 -Mode Fallback -BaseUrl http://localhost:5230
```

Fallback-off JWT-required mode verifies that protected endpoints no longer pass without a token:

```powershell
.\scripts\smoke-security-healthcore.ps1 `
  -Mode Jwt `
  -BaseUrl http://localhost:5230 `
  -AdminUsername "<local-admin-username>" `
  -AdminPassword "<local-admin-password>"
```

Optional patient-scoped checks:

```powershell
.\scripts\smoke-security-healthcore.ps1 `
  -Mode Jwt `
  -BaseUrl http://localhost:5230 `
  -AdminUsername "<local-admin-username>" `
  -AdminPassword "<local-admin-password>" `
  -PatientId "<existing-patient-id>"
```

If no patient exists, use `-CreatePatientIfMissing` in local development only.

The JWT mode checks:

- `/health` is reachable
- unauthenticated `GET /api/health-core/patients` returns `401` or `403`
- InternalAdmin development headers are denied when fallback is off
- backend admin login succeeds
- `GET /api/health-core/auth/admin/me` works with the bearer token
- `GET /api/health-core/patients` works with the bearer token
- optional patient summary/documents reads work with the bearer token
- optional patient access grant list checks run when a patient is available:
  - unauthenticated grant list denied
  - InternalAdmin JWT grant list allowed
- optional patient AuditLog review checks run when a patient is available:
  - unauthenticated audit review denied
  - InternalAdmin JWT audit review allowed

The script does not print the admin password or JWT.

## Phase 99 Evidence Status

Phase 99 strengthens the fallback-off smoke script so it can produce local or
staging evidence that both default fallback and header fallback are disabled.

Current Phase 99B local status:

| Field | Result |
| --- | --- |
| Date | 2026-06-23 |
| Script syntax | PowerShell parser check passed for `scripts/smoke-security-healthcore.ps1` |
| Base URL | `http://localhost:5230` |
| Mode | `Jwt` |
| Health endpoint | Passed |
| Unauthenticated patient directory rejection | Passed |
| InternalAdmin development header rejection | Passed |
| Admin JWT login | Passed |
| Admin `/me` InternalAdmin product context | Passed |
| JWT patient directory authorization | Passed |
| Patient-scoped checks | Passed for one local test patient: summary, documents, access-grants, and audit-log review |
| Sensitive evidence handling | No admin password, JWT, secret value, patient id, or patient data recorded |
| Evidence status | Local fallback-off smoke evidence complete; real staging and production smoke evidence still required |

Repeat local, staging, or production evidence should run the script against a
backend started with:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:HealthCoreAuth__AllowHeaderFallback = "false"
$env:HealthCoreAuth__AllowDefaultDevFallback = "false"
```

and valid admin credentials supplied at runtime. Do not record the password or
bearer token in evidence.

## Frontend Session Checklist

Run the backend with fallback disabled as shown above, then run the frontend:

```powershell
cd frontend
npm.cmd run dev
```

Manual verification:

1. Open `/login`.
2. Log in with the local admin account.
3. Confirm the browser is redirected to `/patients`.
4. Confirm `/api/admin-auth/me` returns `InternalAdmin` context.
5. Confirm `/patients` loads through server-side authenticated fetching.
6. Open an existing patient and confirm `/patients/{id}` loads.
7. In browser network tools, confirm client-side health-record calls use same-origin `/api/health-core/...`.
8. Confirm the browser does not need a localStorage JWT for ordinary Health Core calls.
9. Run logout through the current route handler if no UI button exists yet:

```javascript
await fetch("/api/admin-auth/logout", { method: "POST" })
```

10. After logout, `GET /api/admin-auth/me` should return `401`.
11. After logout, protected `/api/health-core/...` calls should fail with `401` or `403` when backend fallback is off.

## What Must Pass Before Fallback Removal

- Backend JWT smoke passes with fallback disabled.
- Frontend login creates the httpOnly cookie session.
- `/patients` and `/patients/[id]` load with fallback disabled.
- Browser mutations and reads go through `/api/health-core/[...path]`.
- Logout clears the session cookie.
- No admin workflow still depends on localStorage tokens.
- Security smoke docs and scripts are updated for the fallback-off path.

## Production Rule

Production must keep fallback disabled:

- no header fallback
- no default `dev-admin` fallback
- signed JWT or trusted service identity required
- bootstrap admin disabled
- secure JWT authority/signing configuration supplied outside the repository

Development fallback can remain for local convenience until the admin login/session path is stable enough for everyday use.

Phase 87E4 adds Production startup validation for these safety requirements. See [Admin session security hardening](admin-session-security-hardening.md).

See [Final production readiness checklist](final-production-readiness-checklist.md) for the consolidated Phase 89 readiness checklist.

## Remaining Work

- Add a visible logout control in the admin UI.
- Add automated frontend cookie-session smoke tests.
- Remove or fully retire legacy localStorage token helpers after transition.
- Add staging configuration that runs fallback-off by default.
- Add authentication failure audit/monitoring outside controller paths.
- Implement final CSRF protection for cookie-authenticated mutations.
