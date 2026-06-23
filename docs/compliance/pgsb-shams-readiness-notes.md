# PGSB / GSB / SHAMS Readiness Notes

This document summarizes Health Core architecture alignment for future
PGSB/GSB or SHAMS-style health information exchange readiness.

It does not claim that Health Core is certified, approved, connected, or legally
compliant for Ministry of Health exchange use.

The exact requirements for any real connection must be validated against the
latest official guidance, contracts, technical certification process, and
operator obligations.

## 1. Executive Summary

Health Core now has a security and auditability foundation that can support
future readiness work:

- centralized role/permission/product/scope model
- patient-scoped `PatientAccessGrant`
- endpoint-level authorization for current patient-record domains
- security `AuditLog` separate from clinical Timeline
- admin JWT authentication and httpOnly session path
- production fallback safety controls
- service-to-service auth strategy
- grant-management workflow
- production hardening for same-origin mutations, headers, and login throttling

This is a readiness foundation only. It is not an exchange integration.

## 2. Current State

Implemented now:

- Health Core API and admin panel for internal patient-record administration.
- JWT bearer authentication foundation.
- Internal admin login and session handling.
- Product access profiles for multiple Zibzie product contexts.
- PatientAccessGrant model and admin workflow.
- AuditLog foundation with endpoint success/denied access events.
- Fallback-off smoke path and production startup validation.
- Security/compliance documentation pack.

Not implemented:

- live PGSB/GSB/SHAMS connectivity
- Ministry or government gateway integration
- official technical compliance certificate
- exchange-specific schema contracts
- national identity/consent integration
- service token issuer/lifecycle
- production network/VPN/IP allowlisting setup
- formal retention, backup, incident, and legal evidence package

## 3. Readiness Principles

Future integration work should follow these principles:

- No product or service receives broad patient access by default.
- Every non-internal product/service call must carry trusted product context.
- Patient-level access should be bounded by active grants or approved consent.
- All high-risk reads/writes/denials must be auditable.
- Clinical Timeline must remain separate from security AuditLog.
- Production must not rely on fallback headers or default dev identity.
- Exchange integration should be implemented through approved service identities,
  not human admin tokens.
- Official connectivity and certification requirements must be treated as
  external gates, not assumptions inside app code.

## 4. Alignment With Common Exchange-Readiness Areas

| Area | Current Health Core alignment | Remaining gap |
| --- | --- | --- |
| Identity and trust | JWT bearer auth, admin JWT, service-account claim mapping. | Approved identity provider/service-token issuer and service-account lifecycle. |
| Product context | `ProductCodes`, `ProductRoles`, `ProductAccessProfiles`. | Exchange-specific product/service role approval. |
| Patient-scoped access | `PatientAccessGrant` with validity, reason, revoke state. | Formal consent/legal mapping and patient-facing consent flows if required. |
| Auditability | AuditLog captures user/service, product role, patient/resource, action, success/failure, request metadata. | Audit retention, integrity, reporting, and operator review process. |
| Access denial | Protected endpoints audit denied access. | Monitoring/alerting for repeated denied access or suspicious patterns. |
| Data confidentiality | Endpoint authorization, httpOnly admin cookie, same-origin proxy, no-store headers. | Encryption-at-rest, backup encryption, final deployment hardening. |
| Transport security | HTTPS expectations, secure cookies outside Development. | Official network path, VPN/IP/PGSB or other gateway connectivity evidence. |
| Environment separation | Development fallback is config-gated and rejected in Production. | Staging/production operational policy and smoke evidence. |
| Integration contracts | Not implemented. | Official schema/API contracts, test environment, certification evidence. |

## 5. PGSB / GSB Connectivity Readiness

Health Core is not currently connected to PGSB or GSB.

Before any connection path is attempted, the team should identify:

- the official integration owner and approval path
- required legal/entity registration steps
- required technical compliance certificate or review process
- allowed network path, such as approved gateway, VPN, private connectivity, or
  IP allowlisting
- required TLS/certificate model
- endpoint authentication and token trust model
- allowed data domains and exchange schemas
- logging/audit retention expectations
- monitoring and incident reporting obligations
- test/sandbox environment and certification test cases

The app code should not hardcode Ministry/PGSB assumptions until these
requirements are confirmed.

## 6. SHAMS-Style Readiness

Health Core is not integrated with SHAMS or any SHAMS-style national health data
exchange service.

Before SHAMS-style connectivity, the team should define:

- which patient-record domains will be exchanged
- whether Health Core is a source, consumer, or both
- identity mapping for organizations, providers, patients, and services
- consent/authorization basis for data exchange
- audit/reporting requirements
- data normalization and coding requirements
- validation rules for transmitted records
- failure/retry/dead-letter handling
- reconciliation process for mismatched patient or record identifiers

Current Health Core fields such as source, sensitivity, verification status,
timeline events, documents, paraclinical results, medical history, and grants may
support future mapping work, but they are not a finalized SHAMS contract.

## 7. Evidence Package Needed Before External Review

Prepare an evidence package containing:

- architecture diagram
- data-flow diagram
- endpoint inventory and authorization matrix
- permission catalog
- product access profiles
- PatientAccessGrant lifecycle policy
- audit model and sample audit records
- production configuration checklist
- fallback-off smoke results
- security test results
- backup/restore policy and restore evidence
- incident response plan
- retention/deletion policy
- privacy/legal review memo
- service account lifecycle and key rotation policy
- deployment topology and network security evidence

The existing docs provide the starting material, but several operational
documents must still be created by infrastructure, security, legal, and product
owners.

## 8. Legal and Regulatory Review Questions

Open questions for legal/regulatory review:

- What exact legal basis permits each data exchange?
- What consent model is required for patient/family/provider sharing?
- Which records may be exchanged and which must be excluded or redacted?
- What retention period applies to clinical records and AuditLog entries?
- What patient correction, deletion, or deactivation rights apply?
- What notification/reporting obligations exist for incidents?
- What organization-level commitments or NDAs are required?
- What official technical certification checklist applies?

## 9. Operator Responsibilities

Operator responsibilities outside application code include:

- production network security
- firewall/VPN/IP allowlisting or gateway setup
- TLS certificate lifecycle
- secret storage and rotation
- database hardening and encryption
- backup and restore operation
- centralized logging and monitoring
- alerting and incident response
- staff access lifecycle
- legal/compliance evidence archive
- external review/certification coordination

## 10. Recommended Next Steps

Recommended sequence:

1. Freeze the current readiness evidence package for internal review.
2. Run fallback-off JWT/session/proxy smoke in a staging-like environment.
3. Define staging and production deployment topology.
4. Draft backup/restore, incident response, retention, and audit review policies.
5. Define service-account lifecycle and service-token issuer approach.
6. Approve product service roles and grant workflows.
7. Perform legal/privacy review of consent-like grant behavior.
8. Identify the exact official Ministry / PGSB / GSB / SHAMS integration
   checklist for the first intended exchange path.
9. Build a separate integration design once official requirements are confirmed.

## 11. Language to Use Externally

Safe wording:

"Health Core has implemented a security, authorization, patient-scoped access,
and auditability foundation that supports future Ministry / PGSB / SHAMS
readiness work."

Avoid:

- certified
- approved
- connected
- Ministry-compliant
- PGSB-ready as a final state
- SHAMS-integrated
- national exchange connected

Use "readiness foundation" until formal review and integration are complete.
