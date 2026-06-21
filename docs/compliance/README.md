# Compliance and Security Documentation Index

This index groups the current Health Core security and compliance documentation in a suggested reading order.

## Architecture Decisions

- [ADR 0002: Health Core Security and Compliance Foundation](../adr/0002-health-core-security-and-compliance.md)
- [ADR 0001: Health Core Automation Foundation](../adr/0001-health-core-automation-foundation.md)

## Security Foundations

- [Permission catalog](../security/permission-catalog.md)
- [Product access profiles](../security/product-access-profiles.md)
- [Patient access grants](../security/patient-access-grants.md)
- [Authorization service](../security/authorization-service.md)
- [Request context](../security/request-context.md)
- [Audit log](../security/audit-log.md)

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
- [Patient summary authorization strategy](../security/patient-summary-authorization-strategy.md)
- [Patient profile and directory authorization strategy](../security/patient-profile-directory-authorization-strategy.md)

## Compliance Pack

- [Health Core compliance pack](health-core-compliance-pack.md)
- [Security architecture summary](security-architecture-summary.md)
- [Security enforcement summary](security-enforcement-summary.md)
- [PGSB and e-health readiness notes](pgsb-readiness-notes.md)
- [Privacy and data handling principles](privacy-data-handling-principles.md)

## Operational Next Steps

The strongest remaining readiness work is:

- replace development fallback with production JWT or service-to-service authentication
- implement PatientAccessGrant creation/revocation workflows
- add grant-scoped patient directory filtering
- define retention, backup, restore, and incident response policies
- expand security smoke/E2E tests beyond the current local smoke foundation
