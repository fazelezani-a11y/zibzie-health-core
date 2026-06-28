# Compliance and Security Documentation Index

This index groups the current Health Core security and compliance documentation in a suggested reading order.

## Architecture Decisions

- [ADR 0002: Health Core Security and Compliance Foundation](../adr/0002-health-core-security-and-compliance.md)
- [ADR 0001: Health Core Automation Foundation](../adr/0001-health-core-automation-foundation.md)

## Security Foundations

- [Permission catalog](../security/permission-catalog.md)
- [Product access profiles](../security/product-access-profiles.md)
- [Patient access grants](../security/patient-access-grants.md)
- [PatientAccessGrant admin workflow](../security/patient-access-grant-admin-workflow.md)
- [Authorization service](../security/authorization-service.md)
- [Request context](../security/request-context.md)
- [Audit log](../security/audit-log.md)
- [Audit review tools](../security/audit-review-tools.md)
- [Admin auth backend foundation](../security/admin-auth-backend-foundation.md)
- Phase 90B added a minimal admin UI for creating PatientAccessGrant records from the patient security/access tab. This improves operational access management while preserving backend authorization, validation, and audit controls.

## Endpoint Authorization

- [Authorization endpoint matrix](../security/authorization-endpoint-matrix.md)
- [Security enforcement coverage audit](../security/security-enforcement-coverage-audit.md)
- [Documents authorization](../security/documents-authorization.md)
- [Paraclinical authorization](../security/paraclinical-authorization.md)
- [Medical history authorization](../security/medical-history-authorization.md)
- [Care plan authorization](../security/care-plan-authorization.md)
- [Reminders authorization](../security/reminders-authorization.md)
- [Measurements authorization](../security/measurements-authorization.md)
- [Patient summary authorization](../security/patient-summary-authorization.md)
- [Timeline authorization](../security/timeline-authorization.md)
- [Patient profile and directory authorization](../security/patient-profile-directory-authorization.md)
- [Security smoke test plan](../security/security-smoke-test-plan.md)

## Strategy Notes

- [Production auth and JWT strategy](../security/production-auth-jwt-strategy.md)
- [Service-to-service auth strategy](../security/service-to-service-auth-strategy.md)
- [Admin login and frontend JWT integration strategy](../security/admin-login-frontend-integration-strategy.md)
- [Frontend admin auth integration](../security/frontend-admin-auth-integration.md)
- [Server-side admin auth and session strategy](../security/server-side-admin-auth-session-strategy.md)
- [Next admin session route handlers](../security/next-admin-session-route-handlers.md)
- [Server-side authenticated API helper](../security/server-side-authenticated-api-helper.md)
- [Client API session proxy migration](../security/client-api-session-proxy-migration.md)
- [Fallback-off verification](../security/fallback-off-verification.md)
- [Admin session security hardening](../security/admin-session-security-hardening.md)
- [Production security hardening](../security/production-security-hardening.md)
- [Admin panel security UX polish](../security/admin-panel-security-ux-polish.md)
- [Admin panel UI/UX final review](../security/admin-panel-ui-ux-final-review.md)
- [Final production readiness checklist](../security/final-production-readiness-checklist.md)
- [Patient summary authorization strategy](../security/patient-summary-authorization-strategy.md)
- [Patient profile and directory authorization strategy](../security/patient-profile-directory-authorization-strategy.md)

## Compliance Pack

- [Health Core compliance pack](health-core-compliance-pack.md)
- [Security architecture summary](security-architecture-summary.md)
- [Security enforcement summary](security-enforcement-summary.md)
- [PGSB and e-health readiness notes](pgsb-readiness-notes.md)
- [Ministry / PGSB / SHAMS readiness checklist](ministry-readiness-checklist.md)
- [PGSB / GSB / SHAMS readiness notes](pgsb-shams-readiness-notes.md)
- [Privacy and data handling principles](privacy-data-handling-principles.md)
- [Legal / privacy / retention baseline](legal-privacy-retention-baseline.md)

## API Contract Readiness

- [Health Core API contract readiness](../api/health-core-api-contract-readiness.md)
- [Future consumer app contract gaps](../api/future-consumer-app-contract-gaps.md)

## Release Readiness

- [Health Core final closure review](../release/health-core-final-closure-review.md)
- [Health Core release candidate review](../release/health-core-release-candidate-review.md)
- [Production blockers prioritization](../release/production-blockers-prioritization.md)

## Operations and Data Safety

- [Backup / restore / data safety runbook](../operations/backup-restore.md)
- [Production backup / offsite / restore plan](../operations/production-backup-offsite-restore-plan.md)
- [Monitoring / incident response runbook](../operations/monitoring-incident-response-runbook.md)
- [Production environment and secrets](../operations/production-environment-and-secrets.md)
- [Environment example](../operations/env.example.md)

Phase 93 adds local PostgreSQL backup/restore scripts and a data-safety runbook.
Phase 93B records a local restore drill. Phase 101 adds a production
backup/offsite/restore plan. Production backup automation, encryption, offsite
storage, monitoring, retention approval, and restore evidence still require
operational setup.

Phase 102 adds a monitoring and incident response runbook. Centralized logs,
alert delivery, on-call ownership, SIEM/log retention, and production monitoring
evidence still require operational setup.

## Operational Next Steps

The strongest remaining readiness work is:

- treat the current Health Core baseline as internally closed with production blockers, as summarized in the final closure review
- address P0 production blockers in the order defined by the production blockers prioritization roadmap
- complete production environment/secrets readiness with real secret-store evidence and JWT key rotation procedure
- complete production JWT/service identity adoption and retire development fallback before production
- continue fallback-off rollout and admin auth/session hardening
- repeat and automate the Phase 99B fallback-off JWT smoke in real staging and production environments
- complete formal legal/privacy review using the Phase 100 legal/privacy/retention baseline
- define and pilot service-to-service auth for product backends without reusing admin tokens
- extend PatientAccessGrant workflows with consent/sharing and emergency-access policy
- define consumer-safe API contracts before any Family Health Record / Family Health Badge implementation
- add grant-scoped patient directory filtering
- implement production backup automation, encryption, monitoring, restore drills, retention policy, and incident response evidence
- add centralized monitoring/alerting around audit anomalies, failed login bursts, denied access spikes, backup failures, and uptime
- prepare Ministry / PGSB / GSB / SHAMS readiness evidence without claiming certification or live connectivity
- expand security smoke/E2E tests beyond the current local smoke foundation
