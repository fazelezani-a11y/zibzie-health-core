# Health Core Release Candidate Review

Date: 2026-06-23

Verdict: **Internal Release Candidate Ready with Production Blockers**

This review closes the current Health Core baseline as an internal release candidate for the admin/care-team foundation. It does not certify production readiness, Ministry approval, PGSB/GSB/SHAMS connectivity, or public consumer-app readiness.

## Phase Summary

Recent readiness phases completed:

- Phase 90B: PatientAccessGrant create UI
- Phase 91: Admin panel UI/UX final review
- Phase 92: Production security hardening
- Phase 92B: Ministry / PGSB / SHAMS readiness hardening
- Phase 93: Backup / restore / data safety
- Phase 93B: Backup / restore live drill
- Phase 94: Monitoring / Audit review tools
- Phase 95: API contract readiness for future consumer app

## Current System Capabilities

Health Core currently provides:

- patient directory/profile admin workflows
- patient summary
- medical history: conditions, allergies, medications
- documents and paraclinical results
- care plan
- reminders
- measurements and graphs
- clinical Timeline
- Security & Access admin tab
- PatientAccessGrant list/create/revoke workflow
- AuditLog foundation and patient-scoped audit review
- admin login and JWT issuing
- Next.js httpOnly admin session cookie
- same-origin health-core API proxy
- server-side authenticated API helper
- request context provider
- HealthCoreAuthorizationService
- ProductAccessProfiles and permission catalog
- backup/restore runbook and local PostgreSQL drill evidence
- Ministry/PGSB/SHAMS readiness checklist
- API contract readiness notes for future product integration

## Verification Results

| Check | Command | Result |
| --- | --- | --- |
| Backend Release build | `dotnet build backend\Zibzie.HealthCore.sln -c Release` | Passed, 0 warnings, 0 errors |
| Backend Release tests | `dotnet test backend\Zibzie.HealthCore.sln -c Release --no-build` | Passed: 116 tests |
| Frontend lint | `npm.cmd run lint` from `frontend` | Passed |
| Frontend production build | `npm.cmd run build` from `frontend` | Passed |

## Security Readiness Status

Ready for internal RC:

- protected endpoint groups use authorization service checks
- request context maps user/service/product/role metadata
- admin JWT/session/proxy path exists
- Production startup validation blocks unsafe fallback/bootstrap/JWT config
- same-origin mutation guard reduces CSRF risk
- no-store and baseline browser security headers exist
- admin login throttling exists as a local/process-level guard

Known non-blocking backlog for internal RC:

- broader security smoke/E2E automation
- deeper DTO minimization
- sensitivity/visibility filtering refinements
- audit volume optimization for frequent reads

Blocking before production use:

- persistent/distributed login rate limiting or reverse-proxy/WAF controls
- MFA decision for high-privilege roles
- admin password reset and staff lifecycle management
- token revocation/session-version strategy if required
- production secret management and key rotation
- final HTTPS/CORS/proxy/cookie-domain deployment review
- fallback-off smoke in real staging and production environments

## Audit Readiness Status

Ready for internal RC:

- AuditLogEntry and AuditLogService exist
- protected endpoints audit success and denied attempts
- admin login success/failure and throttling events are audited
- PatientAccessGrant lifecycle is audited
- audit review endpoint/UI exists and is protected by `ViewAuditLog`
- Timeline and AuditLog are documented and separated

Blocking before production use:

- audit retention/access policy
- audit integrity/tamper-resistance plan
- centralized log retention and SIEM/export decision
- monitoring of audit write failures
- alerting on failed login bursts, denied-access spikes, and grant changes

## Backup / Restore Readiness Status

Ready for internal RC:

- backup/restore runbook exists
- local PostgreSQL backup script exists
- cautious local restore script exists
- `backups/` is ignored
- Phase 93B recorded a local backup and restore drill to a temporary database
- drill did not inspect or print sensitive row data

Blocking before production use:

- automated scheduled backups
- encrypted offsite backups
- production-like restore drill evidence
- monitoring/alerting on backup failures
- approved retention/deletion policy
- uploaded document/file storage backup plan
- key/secret backup and recovery procedure

## Ministry / PGSB / SHAMS Readiness Status

Ready for internal RC:

- readiness checklist exists
- security architecture and compliance pack exist
- production fallback safety is documented and partially enforced
- access model, audit model, backup/restore, and API contract gaps are documented

Blocking before Ministry / PGSB / SHAMS readiness:

- official current technical and legal requirements review
- no real PGSB/GSB/SHAMS integration exists
- no certification or approval claim exists
- production identity and service-account lifecycle must be finalized
- legal/privacy/retention policies must be approved
- infrastructure/network/security evidence must be prepared
- backup/restore and monitoring must be operational with evidence
- integration-specific schemas, contracts, routing, firewall, and connectivity tests must be defined
- any required technical certification process must be completed

## API Contract Readiness Status

Ready for internal RC:

- current endpoint inventory is documented
- admin/care-team contracts are protected and audited
- future consumer-app contract gaps are documented
- ProductAccessProfiles and PatientAccessGrant provide a foundation for product-scoped integration

Blocking before public consumer app exposure:

- real patient/family authentication
- ownership and guardian/dependent model
- consumer-safe DTOs and redaction/minimization
- patient/family consent and grant workflows
- file access/download/upload contract
- grant-scoped directory/profile filtering
- patient summary partial filtering
- sensitivity/visibility enforcement improvements
- API versioning and OpenAPI grouping
- legal/privacy review

## Acceptable Known Non-Blocking Backlog

These do not block the internal RC verdict, but should stay visible:

- partial patient summary filtering/redaction
- grant-scoped patient directory filtering
- future medical-history modules such as surgery, hospitalization, vaccination, family history, social history
- richer audit reporting/export with redaction and integrity controls
- audit volume controls
- expanded frontend UX for admin/operator review workflows
- broader automated security smoke/E2E tests

## Production Blockers

Health Core should not be used with real production health data until these are handled:

- production identity/service account lifecycle
- fallback-off real staging and production smoke
- secret management and JWT key rotation
- staff onboarding/offboarding and admin credential lifecycle
- persistent rate limiting/lockout or WAF/reverse-proxy equivalent
- monitoring and alerting
- incident response runbook
- production DB hardening
- encrypted offsite backup and restore evidence
- document/file storage backup plan
- legal/privacy/retention approval
- deployment hardening and operational evidence

## Ministry / PGSB / SHAMS Blockers

Health Core is not Ministry-certified, PGSB-connected, GSB-connected, SHAMS-connected, or approved for government exchange use.

Before any Ministry / PGSB / SHAMS readiness claim:

- review current official requirements
- finalize production identity and service authentication
- prepare legal/privacy documentation
- prepare network and infrastructure evidence
- define data exchange contracts
- complete integration-specific testing
- complete any required external technical certification
- maintain audit, backup, monitoring, and incident response evidence

## Recommended Next Steps

Recommended next work after closing the Health Core internal baseline:

1. Freeze the Health Core admin/care-team baseline as an internal RC.
2. Repeat fallback-off smoke in real staging and production environments.
3. Produce deployment-specific security and backup evidence.
4. Decide production identity and service-token lifecycle.
5. Draft consumer-safe DTOs and ownership/guardian model before starting the Family Health Record / Badge app.
6. Define formal legal/privacy/retention policies.
7. Prepare Ministry/PGSB/SHAMS evidence package only after official requirements are reviewed.

Phase 97 prioritizes these blockers in [Production blockers prioritization](production-blockers-prioritization.md).
Phase 98 documents environment and secret handling in [Production environment and secrets](../operations/production-environment-and-secrets.md).
Phase 99 strengthens and syntax-checks fallback-off smoke evidence tooling. Phase 99B records successful local fallback-off JWT smoke against `http://localhost:5230` without recording secrets, tokens, or patient data; real staging and production execution evidence is still required before production use.

## Final Verdict

**Internal Release Candidate Ready with Production Blockers**

The core/admin/security foundation is strong enough to treat Health Core as an internal release candidate baseline. It is not yet ready for regulated production use or Ministry / PGSB / SHAMS connection because operational security, monitoring, backup, legal/privacy, identity lifecycle, and external review evidence remain blocking work.
