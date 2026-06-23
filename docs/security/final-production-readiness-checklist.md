# Final Production Readiness Checklist

Phase 89 is a production-readiness and fallback-off validation pass. It does not remove Development fallback, implement central SSO, issue service tokens, redesign the frontend, or change health-record endpoint permissions.

## 1. Executive Summary

Health Core now has a meaningful security foundation:

- internal admin JWT login
- httpOnly cookie-backed Next admin session
- server-side and browser-side frontend API paths that can use the cookie-backed JWT
- endpoint authorization and audit logging across current health-record domains
- product/profile/scoped authorization model
- PatientAccessGrant admin workflow
- fallback-off smoke path
- Production startup validation for unsafe fallback/bootstrap/JWT config

This is readiness foundation, not final production approval. Real production still needs operational controls such as admin credential lifecycle, rate limiting/lockout, CSRF protection, secret management, monitoring, backup/restore verification, and legal/privacy review.

## 2. Current Completed Security Capabilities

Completed:

- `HealthPermissions` central permission catalog
- `ProductAccessProfiles`
- `PatientAccessGrant` entity and EF mapping
- `IHealthCoreAuthorizationService`
- `IHealthCoreRequestContextProvider`
- `AuditLogEntry` and `IAuditLogService`
- JWT bearer authentication wiring
- internal admin username/password login
- admin JWT issuing for `InternalAdmin`
- Next route handlers for admin login/me/logout
- httpOnly admin session cookie
- server-side authenticated API helper
- browser-side `/api/health-core/[...path]` proxy
- visible admin session/logout UX
- fallback-off verification docs and smoke mode
- service-to-service strategy and service-account grant tests
- internal-admin PatientAccessGrant list/create/revoke workflow
- Phase 92 production hardening for same-origin mutation checks, sensitive-response headers, process-local admin login throttling, and stricter Production JWT validation

Protected and audited endpoint groups:

- patient directory/profile/read
- patient create/update/soft-deactivate
- patient summary
- documents
- paraclinical results
- medical history: conditions, allergies, medications
- care plan
- reminders
- measurements
- timeline
- patient access grant management
- patient access grant list/revoke UI

## 3. Fallback-Off Readiness

Development fallback remains available for ordinary local work.

Fallback-off verification is supported through environment overrides:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:HealthCoreAuth__AllowHeaderFallback = "false"
$env:HealthCoreAuth__AllowDefaultDevFallback = "false"
dotnet run --project .\backend\Zibzie.HealthCore.Api\Zibzie.HealthCore.Api.csproj
```

Expected backend behavior:

- unauthenticated protected endpoints return `401` or `403`
- admin login returns a JWT
- `/api/health-core/auth/admin/me` works with the JWT
- patient directory works with the JWT
- patient access grant endpoints require admin permissions

## 4. Admin Auth and Session Readiness

Current admin flow:

1. `/login` posts to Next `POST /api/admin-auth/login`.
2. Next calls backend admin login.
3. Backend issues an `InternalAdmin` JWT.
4. Next stores the token in `zibzie_admin_access_token`.
5. Cookie is `httpOnly`, `sameSite=lax`, `secure` outside Development, `path=/`, and expires with token expiry.
6. Session/proxy route responses are `Cache-Control: no-store`.

Remaining production work:

- admin password reset/provisioning lifecycle
- distributed rate limiting and lockout beyond the current process-local throttle
- optional MFA for high-privilege roles
- token revocation/session store decision

## 5. Service-to-Service Readiness

Current foundation:

- request context maps `service_account_id` / `client_id`
- authorization service accepts `ServiceAccountId`
- `PatientAccessGrant` supports `ServiceAccountId`
- tests prove service accounts require matching grants

Not implemented:

- service token issuer
- ServiceAccount table/lifecycle
- central SSO
- service role catalog
- product integration pilot

Required service token claims:

- `service_account_id` or `client_id`
- `product_code`
- `product_role` or future `service_role`
- `jti`, `iat`, `exp`, `iss`, `aud`

## 6. PatientAccessGrant Readiness

Current workflow:

- list patient grants
- view grant detail
- create user/service grants
- revoke grants
- audit lifecycle actions

Safety constraints:

- only internal admin profiles have grant-management permissions by default
- non-internal products cannot grant themselves access
- `InternalAdmin` grants cannot be created through the grant workflow
- duplicate overlapping active grants are blocked
- revoke of an already revoked grant returns `409 Conflict`

Not implemented:

- public patient consent UI
- family sharing UI
- emergency access UI/policy
- service account lifecycle table
- frontend grant creation form
- grant-scoped patient directory filtering

## 7. AuditLog Readiness

Current coverage:

- admin login success/failure audited
- protected endpoint success/denied access audited
- PatientAccessGrant list/create/revoke success/failure audited
- request metadata included where available
- Timeline remains separate from AuditLog

Remaining production work:

- audit failure monitoring
- audit retention policy
- strict admin-only AuditLog read/reporting endpoint only if operationally needed
- audit volume optimization for frequent dashboard reads

## 8. Frontend Session and Proxy Readiness

Current frontend path:

- `/login` creates the httpOnly cookie through Next route handlers
- session indicator calls `/api/admin-auth/me`
- logout button clears the cookie through `/api/admin-auth/logout`
- `/patients` uses server-side authenticated API helper
- `/patients/[id]` uses server-side authenticated API helper
- browser Health Core calls use `/api/health-core/[...path]`
- patient grant list/revoke UI uses the same proxy path
- localStorage helper remains only for legacy cleanup

Fallback-off frontend validation:

1. Run backend with fallback disabled.
2. Run frontend.
3. Login at `/login`.
4. Confirm httpOnly cookie exists.
5. Confirm `/patients` loads.
6. Confirm `/patients/{id}` loads when a patient exists.
7. Confirm browser API calls use `/api/health-core/...`.
8. Logout through `POST /api/admin-auth/logout`.
9. Confirm `/api/admin-auth/me` returns `401`.
10. Confirm protected calls fail with `401` or `403`.

## 9. Production Configuration Requirements

Backend:

- fallback disabled
- bootstrap admin disabled
- JWT authority or signing key configured
- symmetric signing key at least 32 bytes if used
- production secrets outside repository
- database connection configured through secrets/deployment config
- HTTPS enabled
- JWT issuer, audience, lifetime, and signing-key validation enabled
- HTTPS metadata required when an authority is configured
- logs do not contain tokens/passwords/secrets

Frontend:

- backend base URL configured:
  - `HEALTH_CORE_API_BASE_URL` for server-side calls
  - `NEXT_PUBLIC_API_BASE_URL` only where appropriate for local/development
- cookie domain/path reviewed for deployment
- `Secure` cookies outside Development
- CORS/cookie domain model reviewed

Operational:

- migrations applied through approved deployment process
- backups enabled and restore tested
- monitoring/alerting configured
- audit log retention defined

## 10. Required Smoke Tests Before Deployment

Backend fallback-off smoke:

```powershell
.\scripts\smoke-security-healthcore.ps1 `
  -Mode Jwt `
  -BaseUrl http://localhost:5230 `
  -AdminUsername "<local-admin-username>" `
  -AdminPassword "<local-admin-password>"
```

With a patient id:

```powershell
.\scripts\smoke-security-healthcore.ps1 `
  -Mode Jwt `
  -BaseUrl http://localhost:5230 `
  -AdminUsername "<local-admin-username>" `
  -AdminPassword "<local-admin-password>" `
  -PatientId "<existing-patient-id>"
```

Expected:

- unauthenticated patient directory denied
- admin login succeeds
- `/me` succeeds
- patient directory succeeds
- patient summary/documents succeed when patient exists
- unauthenticated patient access grant list denied
- admin JWT patient access grant list succeeds when patient exists

Frontend smoke:

- login creates cookie
- `/patients` loads
- `/patients/{id}` loads
- client calls use same-origin proxy
- logout clears cookie
- post-logout protected calls fail

Audit verification:

- query recent AuditLog entries by correlation id where local DB access is available
- verify denied and successful grant workflow events where exercised

## 11. Known Blockers Before Real Production

Real production blockers:

- admin credential lifecycle / staff onboarding / password reset
- distributed rate limiting and lockout for admin login beyond the current process-local throttle
- formal CSRF token policy if the same-origin mutation guard is not sufficient for deployment topology
- token revocation/session store decision
- secret management and signing key rotation
- production monitoring/alerting for auth failures and audit failures
- backup/restore verification
- production database security review
- legal/privacy review
- patient consent/sharing UI not implemented
- emergency access workflow/policy not implemented
- service token issuer/lifecycle not implemented
- service role/profile catalog not finalized
- grant-scoped patient directory filtering not implemented
- PGSB/government integration is not live and not certified

## 12. Recommended Next Phases

Recommended sequence:

1. Admin operational hardening:
   - distributed rate limiting
   - persistent lockout
   - password reset/provisioning
2. CSRF token decision for cookie-backed mutation proxy beyond the Phase 92 same-origin guard.
3. Secret/key rotation and deployment hardening.
4. Grant-scoped patient directory filtering.
5. Service account lifecycle and product service-role catalog.
6. Patient consent/sharing workflow.
7. Monitoring, audit retention, backup/restore, and incident response package.
8. Product integration pilot with fallback disabled.
