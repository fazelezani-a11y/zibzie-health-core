# Future Consumer App Contract Gaps

This document captures contract gaps for a future Family Health Record / Family Health Badge product.

It is not an implementation plan. No consumer app, public endpoint, product feature, payment, organization panel, or AI capability is introduced by this phase.

## Future Product Needs

A future family-facing product will likely need:

- account login for patients, guardians, and family members
- family member / dependent profile switching
- patient-owned health profile basics
- emergency card / health badge summary
- allergies, current medications, major conditions
- key measurements and trends
- documents and paraclinical results
- reminders and care instructions
- controlled sharing with family members and providers
- consent/grant revocation
- safe exports or share links if legally approved

## What Health Core Already Supports

| Need | Current Health Core support | Notes |
| --- | --- | --- |
| Central patient record | Patient profile and domain records exist. | Current DTOs are admin/care-team oriented. |
| Protected endpoints | Authorization and audit exist across current domains. | Product roles and PatientAccessGrant are already part of access decisions. |
| Product context | Product codes/roles exist for `PersonalHealthRecord` and other products. | Future family app may reuse this or define a new product code after review. |
| Family/shared role concept | `PersonalHealthRecordFamilyViewer` and `PersonalHealthRecordSharedProvider` exist. | Relationship and consent UX do not exist yet. |
| Patient-scoped grants | `PatientAccessGrant` supports user/service grantees and scopes. | Admin workflow exists; public consent workflow does not. |
| Measurements | Measurement records and create/read endpoints exist. | Device/source validation and consumer-safe trend contracts remain future work. |
| Documents/results | Document and paraclinical result records exist. | File access and patient-facing redaction need separate contracts. |
| Reminders/care plan | Reminder and care-plan records exist. | Consumer-visible audience and internal task redaction need refinement. |
| Timeline | Timeline records exist with visibility/sensitivity fields. | Consumer timeline must filter internal events. |

## Gaps Requiring New Contracts Later

| Gap | Why it blocks consumer exposure | Likely future contract |
| --- | --- | --- |
| Patient/family identity | Current admin auth is not patient/family auth. | Patient/family auth service and JWT claims. |
| Ownership model | Health Core cannot yet know who owns which patient record. | Owner/dependent/guardian relationship model. |
| Guardian authority | Child/dependent access needs formal verification. | Guardian relationship and verification status. |
| Consumer-safe profile DTO | Current profile/list DTOs expose identifiers/contact fields. | Minimal profile, contact, and emergency-contact DTO split. |
| Consumer-safe summary | Current summary is all-or-nothing and includes identity/contact/medical history. | Partial summary DTO with section-level authorization. |
| Consent/grant UX | PatientAccessGrant is admin-managed today. | Patient/family consent create/revoke workflow. |
| File access | Current document metadata can include raw file reference fields. | Signed upload/download or mediated file endpoint. |
| Clinician note redaction | Current domain DTOs may expose clinician/internal notes. | Patient-facing notes and internal-note suppression. |
| Timeline visibility | Current admin timeline can include internal events. | Consumer timeline filtered by visibility/sensitivity. |
| Audit boundary | AuditLog exists for operators, not patients. | Patient-facing activity history must be a separate product decision, not raw AuditLog. |
| API versioning | Current routes are unversioned. | `v1` or documented OpenAPI versioning before public clients. |

## Auth and Access Decisions Needed

The future family app must decide:

- whether it is `PersonalHealthRecord` or a new `FamilyHealthRecord` product code
- whether the product has owner, guardian, child, family viewer, and emergency viewer roles
- whether family viewers can see contact information
- whether guardians can edit child profiles
- who may upload documents or enter measurements
- who may mark reminders complete
- whether care-plan items are read-only for family members
- how consent is captured, expired, and revoked
- whether emergency card access is authenticated, tokenized, time-limited, or public-with-minimization

## Candidate Consumer-Safe Contracts

Future additive contracts could include:

- `GET /api/health-core/v1/me/records`
- `GET /api/health-core/v1/patients/{patientId}/consumer-profile`
- `GET /api/health-core/v1/patients/{patientId}/emergency-card`
- `GET /api/health-core/v1/patients/{patientId}/consumer-summary`
- `GET /api/health-core/v1/patients/{patientId}/consumer-medical-history`
- `GET /api/health-core/v1/patients/{patientId}/consumer-documents`
- `GET /api/health-core/v1/patients/{patientId}/consumer-results`
- `GET /api/health-core/v1/patients/{patientId}/consumer-measurements`
- `GET /api/health-core/v1/patients/{patientId}/consumer-reminders`
- `POST /api/health-core/v1/patients/{patientId}/family-grants`
- `POST /api/health-core/v1/family-grants/{grantId}/revoke`

These are examples only. They should not be implemented until product, legal, identity, and security decisions are complete.

## Data That Should Not Be Exposed by Default

- national code
- full address and work address
- emergency contact phone unless explicitly authorized
- raw file storage references
- clinician/internal notes
- internal Timeline events
- AuditLog events
- broad patient directory/search
- access grant administration details
- security metadata such as IP addresses, correlation ids, request paths, or failure reasons

## What Must Happen Before Public Use

Before any Health Core endpoint is exposed to a public consumer frontend:

1. real patient/family authentication must exist
2. development fallback must be disabled in staging/production
3. ownership and guardian model must be implemented
4. consumer-safe DTOs must be defined and tested
5. PatientAccessGrant/consent rules must be approved
6. sensitive/restricted data filtering must be verified
7. file access must use mediated or signed access
8. rate limiting, CSRF/session handling, and monitoring must be production-ready
9. legal/privacy review must approve data categories and retention
10. OpenAPI/Swagger contracts must distinguish internal and consumer endpoints

## Recommended Next Contract Phase

The next contract-readiness phase should be documentation-first:

- define family app personas and relationship states
- decide product code/role names
- draft consumer-safe DTOs
- define emergency card minimum dataset
- define consent/grant lifecycle
- define public OpenAPI grouping/versioning

Only after those decisions should implementation begin.
