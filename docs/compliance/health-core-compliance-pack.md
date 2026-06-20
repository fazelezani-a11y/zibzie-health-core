# Health Core Compliance Pack

This document consolidates the current Health Core security and compliance posture for technical, product, legal/compliance, and partner-facing review. It is a readiness document, not a claim of legal certification or government integration approval.

## 1. What Health Core Is

Health Core is the shared health-record foundation for the Zibzie health product hub. It is intended to become a reusable source of truth for patient health records across products such as DigiCare, HomeVisit, Second Opinion, Personal Health Record, and Clinic Queue / Navigation.

Current implemented domains include:

- Patient profile and patient directory
- Contact information
- Medical history: conditions, allergies, medications
- Documents
- Paraclinical results and lab items
- Care plan
- Reminders and alerts
- Measurements and trendable indicators
- Timeline events

Health Core is not only a database. It is being shaped as a secure, auditable, product-aware health-record engine.

## 2. Why Health Data Requires Strong Access Control

Health data can expose identity, diagnoses, medications, test results, care actions, lifestyle patterns, addresses, emergency contacts, and sensitive trends. Improper access can harm patient privacy, clinical trust, legal readiness, and product credibility.

Access control must answer:

- Who is making the request?
- Which product context is the request coming from?
- What role does the caller have in that product?
- Which patient does the caller need access to?
- Which section or operation is being requested?
- Is the data normal, sensitive, or restricted?
- Is there a valid patient-specific grant?
- Was the decision audited?

## 3. Current Security Architecture

Implemented security foundations:

- Central permission catalog: `HealthPermissions`
- Product context and role profiles: `ProductAccessProfiles`
- Patient-scoped access grants: `PatientAccessGrant`
- Authorization decision service: `IHealthCoreAuthorizationService`
- Request context provider: `IHealthCoreRequestContextProvider`
- Security audit log: `AuditLogEntry` and `IAuditLogService`
- Endpoint-level enforcement across current patient-record domains

The model follows ADR 0002: Role + Permission + Product Context + Scope + PatientAccessGrant + Sensitivity + Audit.

Reference:

- [ADR 0002](../adr/0002-health-core-security-and-compliance.md)

## 4. Protected Data Domains

Current protected endpoint groups:

- Patient directory/profile/read endpoints
- Patient create/update/soft-deactivate endpoints
- Patient summary
- Documents
- Paraclinical results
- Medical history: conditions, allergies, medications
- Care plan
- Reminders
- Measurements
- Timeline

Each protected endpoint group performs an authorization decision and writes audit records for successful access and denied access.

Reference:

- [Security enforcement coverage audit](../security/security-enforcement-coverage-audit.md)
- [Authorization endpoint matrix](../security/authorization-endpoint-matrix.md)

## 5. Access-Control Model

Health Core authorization uses stable permission constants, product access profiles, and patient-scoped grants.

Core concepts:

- Identity: user id or service account id.
- Product context: product calling Health Core, such as `DigiCare` or `InternalAdmin`.
- Product role: stable role code, such as `DigiCareCaseManager`.
- Permission: action-level capability, such as `ViewDocuments`.
- Scope: allowed patient boundary, such as `AssignedPatientsOnly`.
- PatientAccessGrant: concrete access grant for a user/service and patient.
- Sensitivity: normal, sensitive, or future restricted handling.

Default behavior is deny unless profile, permission, scope, and grant rules allow access. A narrow InternalAdmin exception exists for local/admin operation and must remain production-controlled.

Reference:

- [Permission catalog](../security/permission-catalog.md)
- [Authorization service](../security/authorization-service.md)

## 6. Audit and Logging Model

AuditLog is separate from patient Timeline.

Timeline:

- Patient/care-team facing clinical and operational history.
- Useful for understanding record progression.

AuditLog:

- Security, legal, compliance, and system accountability evidence.
- Records who accessed or changed sensitive health data, in which product context, with which action, and whether the action succeeded or was denied.

Protected endpoints audit:

- successful reads
- successful creates/updates/deletes/soft-deletes
- denied access attempts
- request metadata such as product, role, service/user, path, method, correlation id, IP, and user agent when available

Reference:

- [Audit log](../security/audit-log.md)

## 7. Product Access Model

Health Core defines product access profiles so different products do not receive the same broad record access.

Examples:

- InternalAdmin has broad administrative access for local/admin operation.
- DigiCare care-team roles receive assigned-patient clinical operations access.
- HomeVisit roles are intended to use temporary visit-scoped access.
- Second Opinion roles are intended to use invited-case access.
- Personal Health Record roles are intended to use own-record or shared-record grants.
- Clinic Queue roles should remain minimal and avoid deep clinical access.

Reference:

- [Product access profiles](../security/product-access-profiles.md)

## 8. PatientAccessGrant Model

`PatientAccessGrant` represents who may access which patient in which product context and role, under which scope and authorization reason, and for what validity window.

It supports future access models such as:

- assigned care-team access
- temporary HomeVisit access
- Second Opinion invited-case access
- patient sharing
- family-authorized access
- emergency access with explicit audit

Grant creation and revocation workflows are not implemented yet. This is a key future compliance step.

Reference:

- [Patient access grants](../security/patient-access-grants.md)

## 9. Current Enforcement Coverage

All current patient-record controller groups now have endpoint-level authorization and audit logging:

- PatientsController: directory/profile reads, create, update, soft deactivate, summary
- PatientDocumentsController
- ParaclinicalResultsController
- ConditionsController
- AllergiesController
- MedicationsController
- CarePlanItemsController
- PatientRemindersController
- PatientMeasurementsController
- TimelineEventsController

Current tests cover allowed and denied cases for protected endpoint groups.

Reference:

- [Security enforcement summary](security-enforcement-summary.md)
- [Security enforcement coverage audit](../security/security-enforcement-coverage-audit.md)

## 10. Known Limitations and Next Steps

Known intentional future work:

- Replace dev/header fallback with production JWT or service-to-service authentication.
- Implement grant creation/revocation admin workflows.
- Implement Patient Summary partial filtering/redaction.
- Improve sensitivity and visibility filtering.
- Optimize audit volume for frequent dashboard reads.
- Add strict AuditLog admin/reporting endpoint only if operationally needed.
- Add future medical-history modules: surgery, hospitalization, vaccination, family/social history.
- Add security smoke tests and end-to-end authorization tests.

## 11. Future Production Auth/JWT Requirements

Before production use, Health Core needs a real identity model:

- signed JWT or service-to-service authentication
- stable user/service identifiers
- trusted product context claims
- product role claims or server-side role mapping
- correlation id propagation
- controlled service account credentials
- removal or production disabling of dev/header fallback

The current fallback exists to keep the local admin panel usable while production auth is not yet integrated. It is not production-safe.

## 12. Future Regulatory / PGSB Readiness Considerations

Health Core is not integrated with PGSB, any government gateway, or national health exchange.

Current work improves readiness by adding:

- centralized authorization foundations
- product-specific access profiles
- patient-scoped grants
- protected endpoints
- audit logging
- documented security architecture and endpoint matrix

Future readiness work should include:

- formal legal/privacy review
- deployment hardening
- infrastructure and network security review
- backup and retention policy
- incident response checklist
- data exchange contracts
- consent and grant workflows
- operational monitoring
- technical certification gap analysis if required by the relevant authority

This pack should be treated as a foundation for review, not as final compliance certification.
