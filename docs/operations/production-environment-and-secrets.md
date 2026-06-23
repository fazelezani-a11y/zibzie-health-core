# Production Environment and Secrets Readiness

Phase 98 prepares Health Core environment and secret handling for safer staging and production operation.

This document is not a production deployment. It does not add real secrets, deploy Health Core, certify Health Core, connect to Ministry / PGSB / SHAMS services, or change the authorization model.

## Current Configuration Findings

| Area | Current state | Readiness note |
| --- | --- | --- |
| Base `appsettings.json` | Header fallback and default dev fallback are disabled. Bootstrap admin is disabled. JWT signing key is blank. | Safe default posture for auth fallback. Production must supply DB/JWT values through environment or secret store. |
| `appsettings.Development.json` | Development JWT key and fallback are enabled. Bootstrap admin remains disabled. | Development-only convenience. Must not be copied into staging/production. |
| `appsettings.Production.json` | Not present. | Acceptable if production uses environment variables/secret store. |
| Startup validation | Production rejects auth fallback, bootstrap admin, missing JWT authority/signing key, weak signing key, disabled issuer/audience/lifetime validation, disabled signing-key validation when using symmetric key, and non-HTTPS authority metadata. | Strong baseline. Deployment still needs secret management and smoke evidence. |
| Docker Compose | Local PostgreSQL uses `zibzie_dev_password`. | Development-only. Do not use local compose values in production. |
| `.gitignore` | `.env`, `.env.*`, and `backups/` are ignored; `.env.example` is allowed. | Good baseline for preventing accidental local secret/backup commits. |
| Next BFF backend URL | Uses `HEALTH_CORE_API_BASE_URL`, then `NEXT_PUBLIC_API_BASE_URL`, then local default. | Prefer server-only `HEALTH_CORE_API_BASE_URL` outside development. |

## Environment Model

### Development

Purpose:

- local development
- local smoke scripts
- local admin panel work

Allowed:

- header fallback enabled if needed
- default dev fallback enabled if needed
- local development JWT signing key
- local Docker PostgreSQL credentials

Not allowed:

- real patient data
- production secrets
- Ministry/PGSB/SHAMS credentials

### Staging / Production-Like

Purpose:

- validate real JWT/session/proxy flow
- run fallback-off smoke
- run production-like backup/restore drills
- validate deployment topology

Required:

- `HealthCoreAuth__AllowHeaderFallback=false`
- `HealthCoreAuth__AllowDefaultDevFallback=false`
- `AdminAuth__BootstrapAdmin__Enabled=false`
- real JWT authority or strong signing key from secret store
- non-development database and credentials
- no real production patient data unless legally approved
- deployment-specific HTTPS/proxy/cookie review

### Production

Purpose:

- real health data operation only after production blockers are closed

Required:

- fallback disabled
- bootstrap disabled
- production secrets from secret store/environment, never repository
- strict JWT validation
- production database with backups, encryption, access controls, and monitoring
- HTTPS/TLS, proxy headers, and cookie-domain configuration
- centralized logging/monitoring
- legal/privacy/retention approval
- incident response process

## Required Production Settings

Backend settings:

| Setting | Required | Secret | Notes |
| --- | --- | --- | --- |
| `ASPNETCORE_ENVIRONMENT=Production` | Yes | No | Enables Production startup validation. |
| `ConnectionStrings__DefaultConnection` | Yes | Yes | Must point to production database. Do not use local Docker password. |
| `Jwt__Issuer` | Yes unless authority supplies it | Usually no | Must match token issuer. |
| `Jwt__Audience` | Yes | Usually no | Must match Health Core backend audience. |
| `Jwt__Authority` | Recommended for IdP/JWKS model | Usually no | Must use HTTPS in Production. |
| `Jwt__RequireHttpsMetadata=true` | Yes when authority is used | No | Production validation enforces this. |
| `Jwt__Key` or `Jwt__SigningKey` | Required if no authority | Yes | Must be at least 32 bytes. Prefer secret store. |
| `Jwt__ValidateIssuer=true` | Yes | No | Production validation enforces this. |
| `Jwt__ValidateAudience=true` | Yes | No | Production validation enforces this. |
| `Jwt__ValidateLifetime=true` | Yes | No | Production validation enforces this. |
| `Jwt__ValidateIssuerSigningKey=true` | Yes for symmetric key | No | Production validation enforces this. |
| `Jwt__AccessTokenMinutes` | Yes | No | Keep short; current default is 60. |
| `HealthCoreAuth__AllowHeaderFallback=false` | Yes | No | Production validation enforces this. |
| `HealthCoreAuth__AllowDefaultDevFallback=false` | Yes | No | Production validation enforces this. |
| `AdminAuth__BootstrapAdmin__Enabled=false` | Yes | No | Production validation enforces this. |
| `AdminAuth__LoginThrottle__Enabled=true` | Recommended | No | Process-local only; production still needs edge/distributed control. |
| `AllowedHosts` | Recommended | No | Should be restricted for production deployment. |

Frontend/Next settings:

| Setting | Required | Secret | Notes |
| --- | --- | --- | --- |
| `NODE_ENV=production` | Yes | No | Makes admin cookie `secure=true`. |
| `HEALTH_CORE_API_BASE_URL` | Yes | No, but deployment-sensitive | Preferred server-side backend URL for Next route handlers. |
| `NEXT_PUBLIC_API_BASE_URL` | Avoid unless needed | No, public | Public client variable. Prefer `HEALTH_CORE_API_BASE_URL` for production BFF calls. |

## Secret Inventory

Secrets that must never be committed:

- production database password / full production connection string
- JWT signing key or private key
- future service-account secrets
- admin bootstrap password, if used in non-production
- backup encryption key
- object/file storage credentials
- SMS/email provider credentials for future auth workflows
- monitoring/alerting API tokens
- Ministry / PGSB / SHAMS gateway credentials, certificates, keys, or integration secrets
- TLS private keys

## Storage Recommendations

Recommended:

- environment-specific secret store
- deployment platform secret variables
- restricted operator access
- audited secret reads/changes where platform supports it
- separate secrets per environment
- rotation runbook and owner

Forbidden:

- real secrets in `appsettings*.json`
- real secrets in `.env.example`
- real secrets in docs
- real secrets in screenshots/tickets/logs
- copying `appsettings.Development.json` into staging/production
- using local Docker database password in production

## JWT Key Rotation Readiness

Current state:

- Health Core can validate a single configured symmetric signing key, or validate through `Jwt__Authority`.
- If using a symmetric signing key directly, rotation likely requires controlled redeploy/restart.
- Existing tokens may become invalid immediately when the key changes unless an external issuer/key-ring supports overlap.

Near-term manual rotation process:

1. Keep access token lifetime short.
2. Schedule a maintenance window or low-traffic deployment window.
3. Generate a new high-entropy signing key outside the repository.
4. Store the new key in the production secret store.
5. Redeploy/restart API instances so all instances use the same new key.
6. Confirm admin login issues tokens signed with the new key.
7. Expect existing sessions to expire or require login again.
8. Run fallback-off smoke after rotation.
9. Record rotation date, operator, reason, and validation result.

Future stronger approach:

- use a central identity provider or zibzie auth service
- use JWKS with `kid`
- support key overlap during rotation
- separate audiences for admin and service tokens if needed
- document emergency key revocation

## Startup Validation Summary

In Production, current startup validation rejects:

- header fallback enabled
- default dev fallback enabled
- bootstrap admin enabled
- issuer/audience/lifetime validation disabled
- HTTPS metadata disabled for configured authority
- no authority and no signing key
- signing-key validation disabled when a signing key is configured
- signing key shorter than 32 bytes

Not currently validated in app code:

- whether production DB host is approved
- whether database password is rotated
- whether `AllowedHosts` is restricted
- whether TLS/proxy headers are correct
- whether a secret store is used
- whether backup encryption is configured

Those items depend on deployment infrastructure and must be covered by the deployment checklist and operator evidence.

## Staging / Fallback-Off Smoke Requirements

Before production:

1. Run backend with fallback disabled.
2. Confirm unauthenticated protected endpoint is denied.
3. Confirm admin login succeeds.
4. Confirm `/api/health-core/auth/admin/me` works with Bearer JWT.
5. Confirm frontend `/login` creates httpOnly cookie.
6. Confirm `/patients` server-side fetches work through cookie-backed JWT.
7. Confirm browser API calls go through `/api/health-core/...` proxy.
8. Confirm PatientAccessGrant endpoints require admin permissions.
9. Confirm AuditLog review is protected and audited.
10. Confirm logout clears cookie and protected calls fail afterward.

See [Fallback-off verification](../security/fallback-off-verification.md).

## Deployment Checklist

| Item | Required before production | Evidence |
| --- | --- | --- |
| Production environment variables configured | Yes | Deployment manifest / secret store screenshot with values redacted |
| Fallback disabled | Yes | Startup config and smoke result |
| Bootstrap disabled | Yes | Startup config |
| JWT authority or signing key configured safely | Yes | Secret store entry name, not secret value |
| JWT key rotation runbook written | Yes | Runbook and owner |
| Database connection from secret store | Yes | Secret reference, not connection string value |
| HTTPS/TLS configured | Yes | Certificate/proxy evidence |
| Cookie secure in production | Yes | `NODE_ENV=production`, browser verification |
| CORS/proxy/cookie-domain reviewed | Yes | Deployment review note |
| Backups encrypted/offsite | Yes before real data | Backup evidence |
| Monitoring/alerting configured | Yes before real data | Alert test |
| Incident response owner defined | Yes before real data | Runbook |

## Remaining Production Gaps

- production secret store selection and operator process
- JWT key rotation drill
- fallback-off staging smoke evidence
- production database hardening evidence
- production backup encryption/offsite/restore drill
- monitoring/alerting
- incident response runbook
- staff access lifecycle
- legal/privacy/retention approval
- future service-account lifecycle and service token issuer

## Phase 98 Status

Phase 98 documents the production environment and secrets readiness requirements.

It does not close the P0 blocker by itself. The blocker is closed only after a real staging/production deployment supplies secrets through an approved mechanism, passes fallback-off smoke, and records operator evidence.
