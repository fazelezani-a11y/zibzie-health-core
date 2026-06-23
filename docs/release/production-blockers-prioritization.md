# Production Blockers Prioritization

Phase 97 prioritizes the remaining blockers after the Phase 96 release-candidate review.

This is a planning document only. It does not implement production readiness, Ministry/PGSB/SHAMS integration, consumer app features, AI, new endpoints, migrations, or auth redesign.

## 1. Executive Summary

Phase 96 verdict:

**Internal Release Candidate Ready with Production Blockers**

Phase 98 adds the production environment and secrets readiness runbook:
[Production environment and secrets](../operations/production-environment-and-secrets.md).

Health Core is internally release-candidate ready for the admin/care-team baseline, but it should not handle real production health data until the P0 blockers below are addressed. Ministry / PGSB / SHAMS readiness requires additional P2 work and external review. Consumer app design can begin in parallel only as contract/product design, not public exposure of current Health Core endpoints.

Recommended next blocker to tackle:

**Phase 98: Production Environment and Secrets Readiness**

Why this should be next:

- secrets, JWT key rotation, fallback-off configuration, and deployment environment evidence are prerequisites for almost every other production task
- it is required before staging/prod-like smoke is meaningful
- it does not require new product features
- it reduces risk before any real data or external product integration

## 2. Priority Categories

| Priority | Meaning |
| --- | --- |
| P0 | Must address before any real production health data use. |
| P1 | Must address before broader pilot, external users, or service/product integration. |
| P2 | Must address before Ministry / PGSB / SHAMS formal process or live connection. |
| P3 | Long-term hardening and operational maturity. |

## 3. P0 Blockers

| Blocker | Risk | Why it matters | Recommended next phase | Implementation type | Dependencies | Blocks consumer app planning | Blocks production | Blocks Ministry readiness |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Secret management and JWT key rotation | Token signing keys or DB credentials could leak, be reused, or become unrotatable. | Production auth cannot be trusted without controlled secrets and rotation. | Phase 98: Production environment and secrets readiness documented; evidence still required | Infra / config / operator / docs | Deployment target decision, secret store choice | No, design can proceed | Yes | Yes |
| Fallback-off staging/prod-like smoke evidence | Protected endpoints might still rely on dev/header fallback. | Production must prove JWT/session/proxy flow works with fallback disabled. | Phase 99: Fallback-off staging smoke and release gate | Config / scripts / operator / docs | Phase 98 secrets/config baseline | No | Yes | Yes |
| Legal/privacy/retention baseline | Health data handling may lack approved lawful basis, retention, deletion, and disclosure rules. | Compliance posture cannot be claimed without legal/privacy approval. | Phase 100: Legal/privacy/retention baseline package | Legal / compliance / docs | Product policy owner and jurisdiction review | Yes, for real product launch | Yes | Yes |
| Production backup/offsite/restore policy | Data loss or untested restore could make Health Core unsafe for real health records. | Local drill exists, but production needs automated encrypted backup and restore evidence. | Phase 101: Production backup and restore operations | Infra / operator / docs / scripts | Secret/encryption key strategy, storage location | No | Yes | Yes |
| Admin/staff lifecycle basics | Orphaned admin accounts, weak onboarding/offboarding, and no reset path create account risk. | Internal admin access is powerful and must be operationally controlled. | Phase 102: Admin staff lifecycle and MFA decision | Backend / frontend / legal / operator / docs | Identity policy decision, email/SMS/IdP direction | No | Yes | Yes |
| Production deployment hardening | TLS, CORS, proxy headers, cookie domain, DB security, and environment separation may be inconsistent. | Security controls depend on final deployment topology. | Phase 98 or Phase 103: Deployment hardening checklist | Infra / config / operator / docs | Hosting target | No | Yes | Yes |

## 4. P1 Blockers

| Blocker | Risk | Why it matters | Recommended next phase | Implementation type | Dependencies | Blocks consumer app planning | Blocks production | Blocks Ministry readiness |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Centralized monitoring and alerting | Failed login spikes, denied access anomalies, backup failures, and downtime may go unnoticed. | Production health systems need operational detection and response. | Phase 104: Monitoring and alerting foundation | Infra / operator / docs | Production logging target, alert channels | No | Yes for broader pilot | Yes |
| Incident response runbook | Security or data incidents may be handled inconsistently. | Audit, notification, evidence preservation, and escalation need a documented process. | Phase 105: Incident response runbook | Operator / legal / docs | Monitoring signals, staff roles | No | Yes for broader pilot | Yes |
| Persistent rate limiting / WAF / reverse proxy controls | Current login throttle is process-local and not enough for multi-instance production. | Public/staging exposure needs durable abuse protection. | Phase 106: Edge protection and distributed throttling | Infra / backend / config | Deployment topology, WAF/proxy choice | No | Yes for broader pilot | Yes |
| Service-account lifecycle | Product services might use unmanaged or over-broad identities. | Service-to-service access needs issuance, rotation, disable/revoke, owner tracking. | Phase 107: Service-account lifecycle foundation | Backend / infra / docs | Phase 98 secret strategy, first product integration target | Yes for product integration planning | Yes if services call Health Core | Yes |
| Grant-scoped patient directory filtering | Product roles could discover patients outside assigned grants if directory is exposed. | Directory/search can leak patient existence. | Phase 108: Grant-scoped directory filtering | Backend / tests / docs | Product/role decisions, PatientAccessGrant policy | Yes for consumer/public planning | Yes for external users | Yes |
| Audit retention and review procedure | Audit logs exist but may lack formal retention, reviewer process, and access policy. | Audit evidence must be preserved and reviewed responsibly. | Phase 109: Audit retention and review policy | Legal / operator / docs / infra | Retention policy, storage/SIEM choice | No | Yes for broader pilot | Yes |

## 5. P2 Blockers

| Blocker | Risk | Why it matters | Recommended next phase | Implementation type | Dependencies | Blocks consumer app planning | Blocks production | Blocks Ministry readiness |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Official current Ministry/PGSB/SHAMS requirements review | Project may optimize for outdated or incomplete assumptions. | Formal readiness must be based on current official requirements. | Phase 110: Official requirements review package | Legal / compliance / operator / docs | Access to current official docs and contacts | No | No, unless production is regulated by same gate | Yes |
| Network/operator evidence | Connectivity, firewall, allowlists, VPN/gateway, and deployment proof may be missing. | PGSB/GSB/SHAMS connection is partly an infrastructure process. | Phase 111: Infrastructure evidence package | Infra / operator / docs | Hosting and network design | No | No for internal production, yes for exchange | Yes |
| Data exchange contracts and integration testing | APIs may not match required exchange schemas/protocols. | Government/exchange connectivity requires agreed contracts and test evidence. | Phase 112: Exchange contract and integration test plan | Backend / docs / operator | Official requirements review, identity model | No | No for internal production | Yes |
| External technical certification if applicable | Missing required certification can block formal connection. | Some integrations may require official technical approval or certificate evidence. | Phase 113: Certification readiness package | Legal / operator / docs | Official requirements review | No | No for internal production | Yes |
| Ministry-specific service identity and key policy | Existing JWT strategy may not satisfy gateway-specific identity requirements. | Exchange services may require separate issuer/audience/cert/key handling. | Phase 114: Exchange identity/key model | Infra / backend / docs | Official requirements, central auth/service lifecycle | No | No for internal production | Yes |

## 6. P3 Maturity Items

| Blocker | Risk | Why it matters | Recommended next phase | Implementation type | Dependencies | Blocks consumer app planning | Blocks production | Blocks Ministry readiness |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| SIEM/log retention maturity | Security evidence may be hard to search, correlate, or preserve. | Mature operations need central retention and correlation. | Phase 115: SIEM and retention maturity | Infra / operator / docs | Monitoring foundation | No | No for initial controlled production if covered manually | Helpful, may be required |
| Audit integrity/tamper-resistance | Audit data could be altered by privileged operators or DB compromise. | Non-repudiation requires stronger integrity controls. | Phase 116: Audit integrity controls | Infra / backend / docs | Audit retention policy, storage design | No | No for internal pilot, yes for higher assurance | Likely yes |
| Advanced anomaly detection | Subtle misuse may not trigger simple alerts. | Useful for maturity but not first production gate. | Phase 117: Audit anomaly detection | Infra / backend / analytics / docs | Centralized logs/SIEM | No | No | Helpful |
| Consumer-safe contract implementation | Family app cannot safely use current admin DTOs. | Required before real consumer app, but not required to close Health Core internal baseline. | Future consumer foundation phase | Backend / frontend / legal / product | Ownership/guardian decisions, patient auth | Yes for implementation, not design | No for admin/care-team production | Indirect |
| OpenAPI versioning and contract examples | Future consumers/partners need stable documented contracts. | Contract clarity prevents accidental public exposure of admin DTOs. | API versioning/readiness phase | Backend/docs | Consumer/product contract decisions | Yes before implementation | No for current admin baseline | Helpful |

## 7. Recommended Next 3-5 Phases

1. **Phase 98: Production Environment and Secrets Readiness**
   - choose secret store / environment injection pattern
   - document JWT key rotation
   - validate production fallback/bootstrap/JWT config expectations
   - produce deployment checklist for env vars, TLS, CORS, proxy, DB security
   - status: documented in Phase 98; operational evidence still required

2. **Phase 99: Fallback-Off Staging Smoke and Release Gate**
   - run backend/frontend path with fallback disabled
   - verify admin login, httpOnly cookie, server-side fetches, proxy calls, grant endpoints, audit review
   - capture smoke evidence and make it a release gate

3. **Phase 100: Legal / Privacy / Retention Baseline Package**
   - define data categories, retention/deletion/correction policy, admin access policy, audit access policy
   - decide what requires legal/regulatory review before production

4. **Phase 101: Production Backup and Restore Operations**
   - define encrypted offsite backup target
   - schedule backups
   - monitor failures
   - perform staging/production-like restore drill
   - include uploaded document storage plan

5. **Phase 104: Monitoring, Alerting, and Incident Response**
   - centralize logs
   - alert on failed login bursts, denied access spikes, audit write failures, backup failures
   - create incident response runbook and contact/escalation path

## 8. What Can Stay Backlog While Starting Consumer App Design

Consumer app design can begin only as product/contract design, not implementation against live Health Core endpoints.

Can remain backlog during early consumer discovery/design:

- Ministry/PGSB/SHAMS formal process
- SIEM maturity
- advanced anomaly detection
- audit tamper-resistance implementation
- full service-token issuer if consumer app does not yet call Health Core
- consumer-safe DTO implementation, as long as design is clearly marked non-production

Cannot remain backlog before consumer app implementation/public exposure:

- patient/family authentication model
- ownership and guardian/dependent model
- consumer-safe DTOs and redaction
- consent/grant lifecycle
- legal/privacy review of public-facing data categories
- API versioning/public contract boundary
- fallback disabled in non-development environments

## 9. Explicit Recommendation

Tackle **secrets/key management and production environment readiness** next.

Reason:

- it is a P0 blocker
- it unlocks meaningful fallback-off staging smoke
- it is required for production identity, backup encryption, service-account lifecycle, and Ministry readiness
- it can be handled without product feature work

After that, run fallback-off staging smoke and then address legal/privacy/retention plus production backup/monitoring.

## 10. Non-Claims

This prioritization does not claim:

- production readiness
- Ministry certification or approval
- PGSB/GSB/SHAMS connection
- legal/privacy approval
- consumer app readiness
- service-to-service production readiness

It is a practical execution roadmap for closing the remaining blockers after the Health Core internal release-candidate baseline.
