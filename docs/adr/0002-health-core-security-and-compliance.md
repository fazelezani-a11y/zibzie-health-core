# ADR 0002: Health Core Security and Compliance Foundation

## Status

Proposed

## Context

Health Core stores sensitive health information and is intended to become the
shared source of truth for multiple Zibzie health products, including DigiCare,
HomeVisit, Second Opinion, Personal Health Record, and Clinic Queue /
Navigation.

Each product will have different user types, workflows, and access needs. A
care-team case manager, a home-visit doctor, an invited second-opinion
specialist, a patient, a family member, and a clinic receptionist should not all
receive the same record access. Health data access must therefore be controlled
centrally by Health Core, not reimplemented independently in each product.

The Ministry of Health guide for connecting non-governmental systems to national
health electronic information exchange services emphasizes secure exchange
infrastructure, technical compliance certification, Neuronta/MPLS or PGSB
connectivity paths, VPN/IP readiness in the PGSB path, non-disclosure and
internal network security commitments in the direct path, and secure, reliable,
high-availability infrastructure.

Health Core should therefore not be treated as only a health-record database. It
must become a secure, auditable, compliance-ready health-record engine.

Timeline and AuditLog are separate concepts. Timeline is clinical and
patient/care-team facing record history. AuditLog is security, legal, and system
audit evidence.

Current Health Core records already include useful fields such as source,
verification status, sensitivity level, related record type/id, and visibility in
some modules. These fields help with provenance and UI clarity, but they do not
yet form a complete access-control model.

## Decision

Health Core will use a centralized security and access-control foundation before
being connected broadly to other products.

The access model will combine:

- Role
- Permission
- Scope
- Sensitivity
- Product Context
- Consent / Grant
- Audit

### Identity

Identity represents the authenticated user or service account making a request.
Future identities may include:

- Internal users
- Patients
- Family members
- Providers
- External reviewers
- Product services

### Product Context

Every caller must operate within a product context. Initial product contexts are:

- `DigiCare`
- `HomeVisit`
- `SecondOpinion`
- `PersonalHealthRecord`
- `ClinicQueue`
- `InternalAdmin`

### Role

Roles describe the caller's general responsibility. Initial role examples are:

- `SuperAdmin`
- `HealthCoreAdmin`
- `CareTeamManager`
- `CaseManager`
- `Clinician`
- `Specialist`
- `NurseOrCareProvider`
- `Patient`
- `FamilyMember`
- `ExternalReviewer`
- `ProductService`
- `ReadOnlyAuditor`

Roles alone are not sufficient for authorization. They must map to permissions
and scopes in a product context.

### Permission

Permissions describe what a caller may do. Initial permission examples are:

- `ViewPatientProfile`
- `EditPatientProfile`
- `ViewMedicalHistory`
- `EditMedicalHistory`
- `ViewDocuments`
- `UploadDocuments`
- `VerifyDocuments`
- `ViewParaclinicalResults`
- `EditParaclinicalResults`
- `ViewCarePlan`
- `CreateCarePlanItem`
- `CompleteCarePlanItem`
- `ViewReminders`
- `CreateReminder`
- `ViewTimeline`
- `ViewAuditLog`
- `ManageAccess`
- `ManageConsent`
- `ExportRecord`
- `ShareRecord`
- `ViewRestrictedData`

### Scope

Scope limits where a permission applies. Initial scope examples are:

- `AllPatients`
- `AssignedPatientsOnly`
- `OwnRecordOnly`
- `FamilyAuthorizedRecords`
- `InvitedCasesOnly`
- `OrganizationPatients`
- `TemporaryAccess`
- `EmergencyAccess`
- `CreatedByMe`

### Sensitivity Level

Health Core already has a sensitivity concept. This concept must become
enforceable, not only displayed.

Suggested future sensitivity levels are:

- `Normal`
- `Sensitive`
- `Restricted`

Implementation should preserve existing raw values until a deliberate migration
or compatibility plan is defined.

### PatientAccessGrant

`PatientAccessGrant` is the central record that says which identity may access
which patient in which product context.

A grant should capture:

- User or service identity
- Patient
- Product context
- Authorization reason
- Scope
- Effective start time
- Effective end time, if temporary
- Granted by
- Revoked at / revoked by, if applicable

### Consent / Authorization Reason

Access should include a reason that can be audited and explained. Initial reason
examples are:

- `ActiveCare`
- `SecondOpinion`
- `HomeVisit`
- `PatientShared`
- `Emergency`
- `InternalAdmin`
- `CareTeamOperation`

### AuditLog

`AuditLog` is a security, legal, and system audit trail. It must remain separate
from patient-facing Timeline.

AuditLog should record:

- User or service id
- Patient id, if applicable
- Product context
- Action type, such as view, create, update, delete, export, share, login, or
  access denied
- Section or resource type
- Resource id, if available
- Reason / context
- Timestamp
- IP and client context, if available
- Success or failure

Timeline may still show user-facing clinical activity, but it is not the source
of truth for access auditing.

### Product Access Profiles

Each product must define how its local roles map to Health Core permissions and
scopes.

#### DigiCare

- `CaseManager` can manage assigned patient records and care operations.
- `Clinician` can view and edit clinical sections for assigned patients.
- `Patient` can view own approved data where the product supports patient access.

#### HomeVisit

- `Doctor` can view visit-specific patient summary, allergies, active
  medications, critical conditions, and create a visit report.
- `Doctor` should not automatically browse the full longitudinal record unless a
  broader grant exists.

#### SecondOpinion

- `LeadSpecialist` can view prepared case data and invite or request other
  specialist opinions.
- `InvitedSpecialist` can view only invited case data and add an opinion.
- `CaseManager` can prepare summaries and manage specialist access.

#### PersonalHealthRecord

- `Patient` can view own record.
- `Patient` can upload documents.
- `Patient` may share limited access with family members or providers.

#### ClinicQueue

- `Receptionist` should not access the deep medical record.
- Access should be limited to minimal identity and appointment context when
  needed.

## Consequences

Positive outcomes:

- Central security model reusable across products.
- Easier compliance readiness for Ministry/PGSB-style review.
- Stronger auditability for sensitive health-record access.
- Least-privilege access by default.
- Clearer product integration boundaries.
- Better future support for patient-facing explanations and consent.

Tradeoffs:

- More backend complexity.
- Every endpoint eventually needs authorization checks.
- Test coverage must grow.
- Product teams must define access profiles before integration.
- Operational support must handle grants, revocations, emergency access, and
  audit review.

## Non-Goals for Immediate Implementation

- Do not implement full PGSB integration yet.
- Do not implement full national health exchange yet.
- Do not build a complex authorization rule engine yet.
- Do not migrate all existing endpoints at once.
- Do not expose sensitive raw audit logs to ordinary users.
- Do not change database schema as part of this ADR.
- Do not change backend or frontend behavior as part of this ADR.

## Implementation Roadmap

### Phase 79: Permission Catalog

- Define central permission constants.
- Group permissions by Health Core section and operation.

### Phase 80: Product Context and Product Access Profiles

- Define product codes.
- Define a mapping model from product-local roles to Health Core permissions and
  scopes.

### Phase 81: PatientAccessGrant Model

- Add user-patient-product scoped grants.
- Support temporary, revoked, emergency, and patient-shared grants.

### Phase 82: Authorization Service

- Add `CanViewPatientSection`.
- Add `CanEditPatientSection`.
- Add `CanViewSensitivityLevel`.
- Add `CanAccessPatientInProductContext`.

### Phase 83: AuditLog Model and Service

- Add security AuditLog separate from Timeline.
- Log reads, writes, denied access, export/share actions, and authentication
  events where applicable.

### Phase 84: Apply Authorization to High-Risk Endpoints First

- Documents
- Paraclinical results
- Medical history
- Care plan
- Patient summary

### Phase 85: Compliance Documentation Pack

- Data-flow diagram.
- Access matrix.
- Audit policy.
- Encryption policy.
- Backup/restore policy.
- Incident response checklist.
- PGSB readiness checklist.

## Open Questions

- What identity provider will be used first?
- Will patients authenticate directly through Health Core or through product
  frontends?
- How will consent be collected and stored?
- What exact Ministry technical certification checklist will be required later?
- Which product integrates first with Health Core?
