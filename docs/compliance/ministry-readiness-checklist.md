# Ministry / PGSB / SHAMS Readiness Checklist

This checklist describes Health Core readiness for future Ministry of Health,
PGSB/GSB, or SHAMS-style review.

It is not a certification statement. Health Core is not connected to Ministry
services, PGSB, GSB, SHAMS, or any national health information exchange.

Any real integration must be reviewed against the latest official technical,
legal, privacy, network, infrastructure, and operator requirements.

## Status Categories

| Category | Meaning |
| --- | --- |
| Implemented now | Implemented in the current Health Core codebase or documentation. |
| Required before production use | Needed before Health Core handles real production health data. |
| Required before Ministry / PGSB / SHAMS connection | Needed before any live government or exchange connectivity. |
| Requires legal/regulatory review | Requires privacy, legal, compliance, or formal external review. |
| Requires infrastructure/operator action | Must be handled by deployment, hosting, network, security operations, or organization policy outside app code. |

## 1. Identity and Access Control

| Area | Current readiness | Remaining requirements |
| --- | --- | --- |
| Admin identity | Implemented now: internal admin login, password hashing, JWT issuing, httpOnly frontend session cookie. | Required before production use: staff onboarding/offboarding, password reset, MFA decision for high-privilege users, persistent lockout/rate limiting. |
| Product/service identity | Implemented now: JWT bearer validation foundation and request context support for `service_account_id` / `client_id`. | Required before Ministry / PGSB / SHAMS connection: approved service-account lifecycle, service-token issuer or trusted identity provider, key rotation, disabled/revoked service behavior. |
| Development fallback | Implemented now: config-gated fallback; Production startup validation rejects fallback. Phase 99 smoke tooling verifies default fallback and header fallback are denied when disabled, and Phase 99B local fallback-off JWT smoke passed. | Required before production use: fallback-off smoke must pass in real staging and production deployments with evidence. |
| Patient/family identity | Not implemented. | Required before patient-facing or family-facing production workflows: formal identity proofing and authentication model. |

## 2. Role / Permission / Scope Model

| Area | Current readiness | Remaining requirements |
| --- | --- | --- |
| Permission catalog | Implemented now: `HealthPermissions` and documentation. | Required before external review: confirm all exchange-facing operations map to explicit permissions. |
| Product access profiles | Implemented now: product-context roles, scopes, and conservative profile defaults. | Required before Ministry / PGSB / SHAMS connection: approve service roles for each product and exchange flow. |
| Scope handling | Implemented now: access scopes, profile scope checks, InternalAdmin exception kept narrow. | Required before production use: grant-scoped patient directory filtering and stronger sensitivity filtering. |
| Broad access prevention | Implemented now: non-internal products require active patient grants. | Required before external exchange: prove no product/service has accidental all-patient access. |

## 3. PatientAccessGrant / Consent-Like Controlled Access

| Area | Current readiness | Remaining requirements |
| --- | --- | --- |
| Patient-scoped grants | Implemented now: `PatientAccessGrant` entity, authorization checks, admin list/create/revoke workflow. | Required before production use: operational policy for who may create/revoke grants. |
| Consent-like access | Partially implemented: grants can represent patient sharing, active care, invited cases, temporary access, or emergency access reasons. | Requires legal/regulatory review: determine whether grants satisfy consent requirements or whether a separate consent record/workflow is required. |
| Emergency access | Model supports `Emergency` reason/scope. | Required before production use if enabled: break-glass policy, justification capture, alerting, and after-the-fact review. |
| Service access | Implemented now: grants support service account ids. | Required before Ministry / PGSB / SHAMS connection: service-account registry and approved service-role mappings. |

## 4. Audit Trail and Non-Repudiation Readiness

| Area | Current readiness | Remaining requirements |
| --- | --- | --- |
| AuditLog foundation | Implemented now: `AuditLogEntry`, EF mapping, audit service, action/resource constants, and a strict read-only audit review endpoint/UI. | Required before production use: retention policy, formal review process, tamper-resistance plan, monitoring of audit write failures. |
| Endpoint audit coverage | Implemented now: protected health-record endpoints audit success and denied access. | Required before external review: evidence report showing endpoint coverage, sample audit rows, and denied-access cases. |
| Admin auth audit | Implemented now: login success/failure audited; throttled login attempts audited. | Required before production use: alerting on repeated failed login attempts. |
| Non-repudiation | Partially ready: audit captures user/service ids, product context, request metadata, action, resource, and success/failure. | Required before Ministry / PGSB / SHAMS connection: trusted time source, log integrity controls, retention requirements, and operator review procedure. |

## 5. Timeline vs AuditLog

| Area | Current readiness | Remaining requirements |
| --- | --- | --- |
| Separation | Implemented now: Timeline is clinical/operational history; AuditLog is security/compliance evidence. | Required before production use: ensure support/admin training does not use Timeline as audit evidence. |
| UI wording | Implemented now: admin timeline explains it is not AuditLog. | Required before external review: document data-flow and audit-flow diagrams. |

## 6. Data Confidentiality

| Area | Current readiness | Remaining requirements |
| --- | --- | --- |
| Endpoint authorization | Implemented now across current patient-record domains. | Required before production use: grant-scoped directory filtering, partial summary redaction, sensitivity/visibility improvements. |
| Token confidentiality | Implemented now: admin JWT stored in httpOnly cookie; browser client uses same-origin proxy. | Required before production use: verify no token exposure in logs, browser storage, monitoring tools, or error telemetry. |
| Data minimization | Partially ready: documented; some DTOs preserve existing shape for compatibility. | Required before production use: minimize list/search/profile output and review national code/contact/address exposure. |
| Encryption at rest | Not implemented in app code. | Requires infrastructure/operator action: database/storage encryption, backup encryption, key management. |

## 7. API Contract Readiness

| Area | Current readiness | Remaining requirements |
| --- | --- | --- |
| Internal/admin contracts | Partially ready: current Health Core endpoints are protected, audited, and documented for admin/care-team use. | Required before external review: signed-off OpenAPI grouping and contract examples. |
| Product integration contracts | Partially ready: product code, product role, permission, and PatientAccessGrant concepts exist. | Required before production use by product backends: service identity lifecycle, grant-scoped directory filtering, and product-specific DTO review. |
| Public/consumer contracts | Not ready for direct exposure: current DTOs are admin/care-team oriented and include sensitive identity/contact/clinical fields. | Required before public consumer use: patient/family auth, ownership/guardian model, consumer-safe DTOs, redaction/minimization, consent workflow, and legal/privacy review. |
| API versioning | Not implemented. | Required before stable public or partner contracts: versioning strategy and OpenAPI grouping by audience. |

## 8. Transport Security

| Area | Current readiness | Remaining requirements |
| --- | --- | --- |
| HTTPS expectations | Documented; cookies secure outside Development. | Requires infrastructure/operator action: TLS termination, certificate management, HSTS decision, proxy header configuration. |
| PGSB/GSB/SHAMS connectivity | Not implemented. | Required before Ministry / PGSB / SHAMS connection: official network path, VPN/IP allowlisting or approved gateway connectivity, routing/firewall review, connectivity test evidence. |
| CORS/cookie domain | Partially ready: same-origin Next proxy reduces browser exposure. | Required before production use: final deployment CORS and cookie-domain review. |

## 9. Production Fallback Safety

| Area | Current readiness | Remaining requirements |
| --- | --- | --- |
| Header/default fallback | Implemented now: fallback ignored in Production and disabled by base config. Phase 99B local smoke verified unauthenticated fallback and InternalAdmin development headers are rejected when fallback is disabled. | Required before production use: deployment config verification and real staging/production fallback-off smoke. |
| Bootstrap admin | Implemented now: Production startup validation rejects bootstrap. | Required before production use: staff provisioning path that does not rely on bootstrap credentials. |
| JWT config | Implemented now: Production startup validation requires issuer/audience/lifetime/signing-key safety. Phase 98 documents required production settings and key rotation expectations. | Requires infrastructure/operator action: secrets supplied by environment/secret store, not repository; rotation evidence required. |

## 10. Admin Authentication and Session Controls

| Area | Current readiness | Remaining requirements |
| --- | --- | --- |
| Admin login | Implemented now. | Required before production use: password reset, staff lifecycle, MFA decision. |
| Session cookie | Implemented now: httpOnly, SameSite=Lax, Secure outside Development, expiry aligned with token. | Required before production use: session lifetime policy and revocation/session-version decision. |
| Login abuse protection | Implemented now: process-local throttle. | Required before production use: distributed rate limiting/lockout or reverse-proxy/WAF rate limiting. |
| Logout | Implemented now: clears cookie; 401 clears cookie. | Required before production use: user-facing session expiry handling and monitoring. |

## 11. CSRF and Cookie-Backed Route Hardening

| Area | Current readiness | Remaining requirements |
| --- | --- | --- |
| Same-origin mutation guard | Implemented now: Next route handlers reject cross-site mutations for admin login/logout and health-core proxy mutations. | Required before production use: verify deployment topology does not require additional trusted origins. |
| CSRF token | Not implemented. | Required before public exposure if same-origin guard is not sufficient: formal CSRF token or equivalent anti-forgery mechanism. |
| Security headers | Implemented now: no-store and basic browser security headers. | Required before production use: CSP/HSTS decision after frontend asset/deployment review. |

## 12. Data Backup and Restore Readiness

| Area | Current readiness | Remaining requirements |
| --- | --- | --- |
| Backup policy | Partially addressed: Phase 93 adds a backup/restore/data-safety runbook and local PostgreSQL backup script. Phase 93B ran a local Docker PostgreSQL backup drill and produced a non-empty ignored dump file. Phase 101 documents the production backup/offsite/restore plan. | Requires infrastructure/operator action before production: automated encrypted backups, schedule, offsite storage location, RPO/RTO targets, monitoring, and backup access policy. |
| Restore process | Partially addressed: Phase 93 adds a cautious local PostgreSQL restore script and restore validation checklist. Phase 93B restored the local backup into a temporary test database, verified table metadata, and dropped the temporary database. Phase 101 adds restore drill cadence and evidence templates. | Required before production use: repeated restore drill evidence in staging/production-like environment and documented operator procedure. |
| Audit backup | Partially addressed: AuditLog is included in PostgreSQL backup scope. | Requires legal/regulatory review: audit retention duration, tamper-resistance/integrity expectations, and access rules. |
| Uploaded documents | Not complete: current scripts cover PostgreSQL only. | Required before production use if binary document storage exists: object/file storage backup, encryption, retention, and restore order between metadata and files. |
| Restore evidence | Partially available: Phase 93B records local/dev drill evidence in the backup/restore runbook. | Required before production and Ministry readiness: periodic restore tests with operator, timestamp, backup file, duration, validation checks, issues found, and production-like environment evidence. |

## 13. Monitoring and Alerting

| Area | Current readiness | Remaining requirements |
| --- | --- | --- |
| Audit data | Implemented now with human review capability for protected audit events. | Required before production use: dashboards/alerts for denied access, failed login bursts, audit write failures, grant changes, unexpected fallback attempts. |
| Operational logs | Basic app logging exists. | Requires infrastructure/operator action: centralized logging, retention, redaction rules, alert routing. |
| Health checks | Basic `/health` exists. | Required before production use: readiness/liveness probes, database dependency checks if appropriate, synthetic auth smoke. |

## 14. Incident Response

| Area | Current readiness | Remaining requirements |
| --- | --- | --- |
| Incident response process | Not implemented in app code. | Requires infrastructure/operator action and legal/regulatory review: severity model, notification path, audit preservation, breach review, contact list. |
| Evidence preservation | Partially ready through AuditLog. | Required before production use: define who can access audit logs and how exports are controlled. |

## 15. Documentation Required for External Review

| Area | Current readiness | Remaining requirements |
| --- | --- | --- |
| Security architecture | Implemented now in docs. | Required before external review: update with final deployment topology and data-flow diagrams. |
| Endpoint coverage | Implemented now in endpoint matrix and coverage audit. | Required before external review: signed-off endpoint inventory and test evidence. |
| API contract readiness | Partially documented now. | Required before consumer or partner exposure: approved audience-specific contracts, DTO redaction rules, OpenAPI examples, and versioning decision. |
| Access model | Implemented now in permission/profile/grant docs. | Required before external review: role-to-persona mapping approved by product/legal owners. |
| Audit model | Implemented now with protected audit review tooling. | Required before external review: audit retention/access policy, sample report, reviewer procedure, and evidence that review access is restricted. |
| Legal/privacy/retention baseline | Documented in Phase 100. | Required before production or external review: formal legal/privacy approval, final retention periods, approved consent/sharing model, export/correction/deletion procedures, and operator responsibilities. |
| PGSB/SHAMS integration package | Not implemented. | Required before Ministry / PGSB / SHAMS connection: official forms, technical certificate evidence, network readiness proof, NDA/security commitments if applicable, integration test plan. |

## 16. Environment Separation

| Area | Current readiness | Remaining requirements |
| --- | --- | --- |
| Development | Implemented now with dev fallback and local JWT key. | No real patient data should be used. |
| Staging | Not fully defined. | Required before production use: fallback off by default, production-like JWT, seeded test data only, smoke automation. |
| Production | Production validation exists. | Required before production use: secrets, TLS, database, backup, monitoring, incident response, legal approvals. |

## 17. Secret Management and Key Rotation

| Area | Current readiness | Remaining requirements |
| --- | --- | --- |
| Repository secrets | Base config avoids production JWT secret; `.env`, `.env.*`, and `backups/` are ignored. Phase 98 adds a placeholder-only environment example. | Required before production use: secret store, rotation plan, emergency rotation procedure, and evidence that real secrets are not committed. |
| JWT keys | Validation exists. | Required before Ministry / PGSB / SHAMS connection: issuer/key management approved for integration path. |
| Service secrets | Not implemented. | Required before service-to-service production: service-account credential lifecycle and rotation. |

## 18. Legal / Privacy / Retention

| Area | Current readiness | Remaining requirements |
| --- | --- | --- |
| Privacy principles | Documented; Phase 100 adds a conservative legal/privacy/retention baseline and data classification. | Requires legal/regulatory review: privacy notice, data subject rights, patient consent model, lawful basis, disclosure rules. |
| Retention/deletion | Soft deactivation preferred; Phase 100 documents deactivation vs deletion considerations and suggested retention categories. | Requires legal/regulatory review: final retention periods, correction policy, deletion policy, backup retention, audit retention. |
| Data exchange | Not implemented. | Requires legal/regulatory review: data sharing agreements, patient consent/authorization, external exchange contracts. |

## 19. Operator Responsibilities Outside Code

Operators must own:

- production hosting and hardening
- TLS/certificate management
- firewall/VPN/PGSB/GSB connectivity
- secret storage and key rotation
- database security and backups
- centralized logging and monitoring
- incident response
- staff access lifecycle
- legal/privacy review evidence
- external certification or Ministry checklist submission

## 20. Current Readiness Summary

Phase 96 release-candidate verdict:

**Internal Release Candidate Ready with Production Blockers**

See [Health Core release candidate review](../release/health-core-release-candidate-review.md).

Phase 97 prioritizes production and Ministry-readiness blockers in
[Production blockers prioritization](../release/production-blockers-prioritization.md).

Phase 98 documents production environment and secret handling in
[Production environment and secrets](../operations/production-environment-and-secrets.md).

Phase 99 strengthens and syntax-checks fallback-off smoke evidence tooling.
Phase 99B records successful local fallback-off JWT smoke without recording
passwords, JWTs, secret values, patient ids, or patient data. Real staging and
production execution evidence remains required before production use.

Phase 100 documents a legal/privacy/retention baseline in
[Legal / privacy / retention baseline](legal-privacy-retention-baseline.md).
Formal legal/privacy approval remains a production and Ministry-readiness
blocker.

Phase 101 documents the production backup/offsite/restore plan in
[Production backup / offsite / restore plan](../operations/production-backup-offsite-restore-plan.md).
Encrypted offsite backups, monitoring, final retention policy, and
production-like restore evidence remain blockers.

Health Core has a strong internal readiness foundation:

- centralized access model
- patient-scoped grants
- endpoint enforcement and audit
- admin JWT/session path
- production fallback safety
- same-origin mutation guard and security headers
- internal API contract inventory and consumer-contract gap analysis
- compliance documentation

Health Core is not ready for real Ministry / PGSB / SHAMS connection until:

- official current requirements are reviewed
- production identity and service-account lifecycle are finalized
- legal/privacy/retention policies are approved
- infrastructure/network/security evidence is prepared
- backup/restore and monitoring are operational
- consumer-safe contracts and ownership/guardian models are designed before public exposure
- integration-specific schemas, contracts, and testing are defined
- any required technical certification process is completed
