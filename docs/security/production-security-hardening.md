# Production Security Hardening

Phase 92 applies focused production-hardening changes around the existing Health Core admin authentication, httpOnly session cookie, same-origin proxy, startup validation, and operational readiness path.

This phase does not change Health Core endpoint permissions, authorization rules, patient/medical DTOs, database schema, ProductAccessProfiles, PatientAccessGrant semantics, or frontend product scope.

## Implemented Hardening

### Cookie-backed Mutation Guard

The Next route-handler layer now rejects cross-site state-changing requests for:

- `POST /api/admin-auth/login`
- `POST /api/admin-auth/logout`
- `POST`, `PUT`, `PATCH`, and `DELETE` through `/api/health-core/[...path]`

The guard uses the browser `Origin` header when present and falls back to `Sec-Fetch-Site` for clear cross-site browser requests. Same-origin requests continue to work, and `GET` behavior is unchanged.

This is a minimal CSRF risk reduction, not a complete CSRF framework. A future CSRF token should still be considered before public production, especially for multi-domain deployments or broader cookie-authenticated mutations.

### Sensitive Route Cache and Browser Headers

Next admin/session/proxy responses now consistently include:

- `Cache-Control: no-store`
- `Pragma: no-cache`
- `Expires: 0`
- `X-Content-Type-Options: nosniff`
- `Referrer-Policy: strict-origin-when-cross-origin`
- `X-Frame-Options: DENY`

The Next app also sets `X-Content-Type-Options`, `Referrer-Policy`, and `X-Frame-Options` for app routes through `next.config.ts`. The backend API adds basic security headers and `no-store` on `/api/health-core` responses. This reduces accidental caching of protected health/admin data and adds baseline browser hardening without introducing a risky Content Security Policy.

### Admin Login Throttling

The backend admin login flow now includes a small in-memory throttle for repeated failed login attempts.

Default settings:

- enabled: `true`
- failed attempts: `5`
- window: `15` minutes
- lockout: `5` minutes

Configuration section:

```json
"AdminAuth": {
  "LoginThrottle": {
    "Enabled": true,
    "MaxFailedAttempts": 5,
    "WindowMinutes": 15,
    "LockoutMinutes": 5
  }
}
```

Behavior:

- errors remain generic
- username existence is not revealed
- failures continue to be audited
- throttled attempts are audited as failed login attempts
- no passwords or tokens are logged

This is intentionally not a final distributed lockout system. Multi-instance deployments still need a shared rate-limit/lockout layer, WAF/reverse-proxy controls, or a central identity provider policy.

### Production JWT Validation Safety

Production startup validation now also requires:

- issuer validation enabled
- audience validation enabled
- lifetime validation enabled
- signing-key validation enabled when a local symmetric signing key is used
- HTTPS metadata required when `Jwt:Authority` is configured

Existing safeguards remain:

- header/default fallback must be disabled in Production
- bootstrap admin must be disabled in Production
- JWT authority or signing key must be configured
- symmetric signing key must be at least 32 bytes

## Verified Existing Controls

- The admin JWT cookie remains `httpOnly`.
- Cookie `secure` remains enabled outside Development.
- Cookie `SameSite` remains `lax`.
- Cookie path remains `/`.
- Cookie expiry remains aligned with token expiry when available.
- `POST /api/admin-auth/logout` clears the cookie.
- `401` from `/api/admin-auth/me` and the health-core proxy clears the cookie.
- The Next login route does not return the backend JWT to browser JavaScript.
- Development fallback remains available only through configuration and remains ignored in Production.

## Remaining Production Backlog

Still required before real production:

- production environment variables supplied from an approved secret store
- persistent/distributed login rate limiting and lockout
- optional MFA for high-privilege admin roles
- admin password reset and staff onboarding workflow
- token revocation or session-version store
- formal CSRF token strategy for cookie-authenticated mutations
- production secret management and signing-key rotation
- monitoring/alerting for authentication and audit anomalies
- production backup automation, encryption, monitoring, and restore validation evidence
- legal/privacy review
- deployment-level HTTPS, CORS, proxy, and cookie-domain hardening
- WAF/reverse-proxy rate limiting
- service-token issuer/lifecycle and central identity integration

Production environment and secret handling requirements are detailed in
[Production environment and secrets](../operations/production-environment-and-secrets.md).

## Verification

Phase 96 release-candidate verification passed:

- `dotnet build backend\Zibzie.HealthCore.sln -c Release`
- `dotnet test backend\Zibzie.HealthCore.sln -c Release --no-build`
- `npm.cmd run lint`
- `npm.cmd run build`

This supports the internal release-candidate verdict, but does not remove the production backlog above.

Recommended checks after this phase:

```powershell
cd frontend
npm run lint
npm run build
```

```powershell
dotnet build backend\Zibzie.HealthCore.sln -c Release
dotnet test backend\Zibzie.HealthCore.sln -c Release --no-build
```

Fallback-off smoke remains documented in [Fallback-off verification](fallback-off-verification.md) and [Security smoke test plan](security-smoke-test-plan.md).

Phase 99 updates `scripts/smoke-security-healthcore.ps1` so JWT mode fails if
unauthenticated default fallback or InternalAdmin development header fallback
still works while fallback is supposed to be disabled. The same mode verifies
admin JWT login, `/me`, protected patient directory access, and optional
patient-scoped summary/documents/access-grant/audit-log checks.

The Phase 99 script parser check passed on 2026-06-23. Phase 99B local
fallback-off JWT smoke passed against `http://localhost:5230`, including
unauthenticated patient directory rejection, InternalAdmin development header
rejection, admin login, `/me`, JWT patient directory authorization, and
patient-scoped summary/documents/access-grant/audit-log checks for one local
test patient. No password, JWT, secret value, patient id, or patient data was
recorded. Real staging and production smoke evidence remains required.

Backup/restore and data-safety expectations are documented in
[Backup / restore / data safety](../operations/backup-restore.md). Phase 93 adds
local PostgreSQL backup/restore scripts for development and restore drills, but
production backup scheduling, encryption, monitoring, retention approval, and
restore evidence remain operational requirements.

Environment and secret handling expectations are documented in
[Production environment and secrets](../operations/production-environment-and-secrets.md).

For Ministry / PGSB / SHAMS readiness framing, see
[Ministry / PGSB / SHAMS readiness checklist](../compliance/ministry-readiness-checklist.md)
and [PGSB / GSB / SHAMS readiness notes](../compliance/pgsb-shams-readiness-notes.md).
