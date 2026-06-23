# Legal / Privacy / Retention Baseline

Phase 100 documents a conservative legal, privacy, and retention baseline for
Zibzie Health Core.

This document is not legal advice, legal approval, certification, Ministry
approval, PGSB/GSB/SHAMS approval, or a final production policy. It is a
readiness baseline and gap analysis for product, engineering, operations, and
legal/compliance review before Health Core is used with real production health
data.

## Purpose and Scope

Health Core stores identity, contact, clinical, operational, access-control, and
security-audit data. These records can affect care-team decisions, patient
privacy, operational access, and future regulatory review.

This baseline documents:

- data categories handled by Health Core
- sensitivity classification
- minimum necessary access expectations
- PatientAccessGrant and consent-like access model status
- retention and deletion/deactivation principles
- export, correction, amendment, and backup considerations
- staff/operator responsibilities
- remaining legal, privacy, retention, and Ministry-readiness gaps

It does not define final retention periods, patient consent language, privacy
notices, lawful basis, data-sharing agreements, or government integration
requirements. Those require formal legal/regulatory review.

## Data Categories

| Category | Examples | Sensitivity |
| --- | --- | --- |
| Identity/profile data | patient id, first name, last name, birth date, gender, blood type, marital status, education, occupation, profile image reference, active/inactive state | Sensitive personal data; may reveal patient identity and demographics. |
| Strong identifiers | national code, mobile number, email | Highly sensitive identity/contact data; can enable direct identification and account matching. |
| Contact and emergency data | home/work address, emergency contact name/phone | Highly sensitive contact/location data; should be limited to operational need. |
| Medical history | conditions, diagnoses, status, treatment summary, clinician notes, source/verification, sensitivity | Sensitive health data; may include stigmatizing or restricted conditions. |
| Allergies | allergen, allergy type, severity, reaction, clinician notes | Sensitive health and safety data; relevant to care but not broadly shareable. |
| Medications | medication name, dose, route, frequency, reason, dates, current status | Sensitive health data; may reveal diagnoses, pregnancy/fertility care, mental health treatment, chronic disease, or other private information. |
| Documents/files | document metadata, issuer, dates, file references, uploaded binaries if stored | Sensitive health evidence; file contents may be more sensitive than metadata. |
| Paraclinical results | result type, provider, abnormal flag, interpretation, follow-up, lab items, linked documents | Sensitive diagnostic data; abnormal results and interpretations can affect care and privacy. |
| Care plans | tasks, next actions, due dates, assignments, reason, status, priority | Operationally sensitive health data because it drives follow-up, tasks, reminders, and care-team decisions. |
| Reminders | reminder type, title, due date, status, audience, channel, related record | Sensitive operational data; can reveal medications, diagnoses, lifestyle instructions, or follow-up plans. |
| Measurements | measurement type, value, units, abnormal flag, targets, context, date | Sensitive monitoring data; can reveal disease control, lifestyle, activity, sleep, glucose, blood pressure, BMI, and other trends. |
| Timeline events | clinical/operational history, related records, visibility, sensitivity | Sensitive patient/care-team history; not a security audit substitute. |
| PatientAccessGrant records | grantee user/service account, product, role, scope, reason, validity, grant/revoke metadata | Security and access-control data; may reveal who is involved in care or sharing. |
| Audit logs | actor identity, service account, patient id, product context, action/resource, success/failure, request metadata | Security, compliance, and accountability evidence; should be restricted and retained according to policy. |
| Admin/security events | admin login, failed login, throttling, grant lifecycle, audit review | Security evidence; should support investigation and non-repudiation. |
| Backups | database dumps, restored copies, document/file backups if present | Sensitive health data and security evidence; must be protected like production data. |

## Sensitivity Classification

Planning classifications:

- `Restricted`: data that directly identifies a patient, reveals sensitive
  clinical information, exposes strong identifiers/contact/location, or provides
  access-control/security evidence.
- `Confidential`: ordinary clinical or operational patient-record data that
  should be shared only with authorized care/admin contexts.
- `Internal`: system configuration, workflow metadata, and operator notes that
  do not identify patients by themselves but may become sensitive in context.

Most Health Core records should be treated as `Confidential` or `Restricted`.
List/search/profile endpoints are especially sensitive because they can reveal
patient existence even before clinical details are viewed.

## Minimum Necessary Access

Health Core should continue using least-privilege controls:

- ProductAccessProfiles define role-level permission defaults.
- HealthPermissions define stable action/domain policy keys.
- AccessScopes and AuthorizationReasons describe why and how access is allowed.
- PatientAccessGrant bounds non-internal product/service access to specific
  patients.
- AuditLog records allowed and denied access.

Operational expectation:

- Staff should access only records needed for their assigned work.
- Product/service callers should not receive broad all-patient access by
  default.
- Directory/search access should remain stricter than patient-scoped record
  access because it can reveal patient existence.
- Contact/location data should require explicit operational need.
- AuditLog review should be limited to internal admin/compliance roles.

## PatientAccessGrant / Consent-Like Model Status

`PatientAccessGrant` is currently an access-control primitive. It can represent
approved care, product-specific access, service access, temporary access,
patient-shared access, or emergency access depending on `AuthorizationReason`,
`AccessScope`, and validity dates.

Current status:

- Internal admins can list/create/revoke grants.
- Non-internal products still need both ProductAccessProfile permissions and an
  active matching grant.
- Grant lifecycle actions are audited.
- Grant creation does not issue credentials.
- InternalAdmin access is not granted through PatientAccessGrant.

Important gap:

PatientAccessGrant is not yet a formal patient consent workflow. Before
patient/family/public product use, legal/product review must decide whether:

- grants are enough for a specific workflow
- a separate consent record is required
- patients/family members need their own review/revoke UI
- emergency or break-glass access requires special notification and review
- grant reasons map to approved policy language

## Data Retention Principles

No final retention periods are defined in this phase.

Planning principles:

- Retain clinical records long enough to support patient care, continuity,
  investigation, and legally required recordkeeping.
- Do not hard-delete health or audit records without formal legal review and an
  approved operational procedure.
- Prefer soft deactivation/revocation for ordinary lifecycle changes.
- Separate access restriction from deletion: a record can be hidden from normal
  workflows while still retained for legal, safety, or audit reasons.
- Retention periods may differ for active clinical data, inactive records,
  uploaded files, audit logs, backups, and admin/security events.
- Expired backups should be securely deleted according to an approved backup
  retention policy.
- Development and staging should avoid real patient data unless explicitly
  approved.

## Suggested Retention Categories

| Category | Suggested baseline posture | Open decision |
| --- | --- | --- |
| Active clinical record data | Retain while patient record is active and clinically relevant. | Final legal retention period and archival rules. |
| Inactive/deactivated patient records | Soft deactivate and restrict routine access; retain until policy permits archival or deletion. | Whether inactive records remain searchable and who may reactivate/view them. |
| Medical history, measurements, reminders, care plans, timeline | Retain as part of the longitudinal record unless legally approved deletion/amendment applies. | Final clinical retention duration and patient access rights. |
| Uploaded documents/files | Retain metadata and binaries together; treat missing binary restore as incomplete. | Storage-specific retention, encryption, and deletion process. |
| PatientAccessGrant records | Retain grant/revoke history as access-control evidence. | Retention period and export/review policy. |
| Audit logs | Retain as security/compliance evidence; do not expose as Timeline. | Retention duration, tamper-resistance, integrity controls, and export policy. |
| Admin/security events | Retain for investigation and accountability. | Retention duration and monitoring/alerting process. |
| Backups | Retain according to approved RPO/RTO and retention schedule; encrypt before production. | Offsite location, retention tiers, secure deletion, and restore drill cadence. |

## Deletion vs Deactivation

Health Core currently uses soft-deactivation patterns in several areas:

- patient deactivation rather than hard delete
- soft-delete markers on clinical record entities
- grant revocation rather than grant deletion
- audit logs retained as evidence

Baseline position:

- Soft deactivation is the default for patient and medical records.
- Hard deletion should not be introduced for health or audit data without legal
  review.
- Legal deletion requests, if applicable, need a formal workflow that considers:
  - clinical safety
  - audit and non-repudiation obligations
  - backups and restored copies
  - linked documents/files
  - downstream product copies or exports
  - Ministry/exchange obligations if later integrated

## Export and Access Request Considerations

Health Core does not yet implement a patient-facing export/access request
workflow.

Before production or consumer-facing use, define:

- who may request patient record access/export
- identity proofing requirements
- whether family/guardian requests are allowed
- which data is exportable
- which data must be redacted or withheld
- how AuditLog is handled in access requests
- export format and delivery controls
- operator approval and audit logging

Exports must not include secrets, tokens, internal-only metadata, or unrestricted
AuditLog details unless explicitly approved.

## Correction and Amendment Considerations

Health data may require correction or amendment without erasing history.

Recommended baseline:

- Prefer amendment/versioning or corrected records over silent overwrite for
  clinically meaningful changes.
- Preserve enough history to explain who changed what and why.
- Audit correction/amendment actions.
- Distinguish patient-reported data from clinician-verified data.
- Use source and verification fields where available.

Future work should define correction/amendment workflows for patient profile,
medical history, documents, results, measurements, and Timeline events.

## Staff and Operator Confidentiality Responsibilities

App controls are not enough by themselves. Operators and staff must have policy
and training.

Before production use:

- define staff roles and permitted access
- approve onboarding/offboarding procedures
- require confidentiality expectations for admins, care-team users, and
  operators
- document when support staff may view patient records
- define audit review responsibilities
- prohibit copying patient data into tickets, screenshots, chat, email, or
  personal storage unless policy explicitly permits it
- require secure handling of local exports, backups, and logs

## Backup Privacy Implications

Backups contain sensitive health data, access grants, admin users, and audit
logs. They can also restore deleted/deactivated data.

Production backup policy must define:

- encryption at rest and in transit
- approved storage locations
- operator access restrictions
- retention and secure deletion
- restore approval process
- restore evidence without exposing row data
- handling of uploaded document binaries
- key management and emergency key recovery

Local backup scripts are development tools. They do not satisfy production
backup privacy requirements by themselves.

## AuditLog Retention and Integrity Gap

AuditLog is security and compliance evidence, not clinical Timeline.

Current readiness:

- protected endpoints audit allowed and denied access
- admin auth and grant lifecycle events are audited
- internal admin audit review exists

Remaining gaps:

- final AuditLog retention period
- tamper-resistance or integrity controls
- reviewer access policy
- export/redaction procedure
- alerting on failed logins, denied-access spikes, grant changes, and audit
  write failures
- legal position on patient access to audit history

## Ministry / PGSB / SHAMS Readiness Implications

This baseline improves readiness but does not create any official connection or
approval.

Before any Ministry, PGSB, GSB, SHAMS, or similar exchange process:

- review current official requirements
- approve privacy, retention, consent, and disclosure policies
- define service identity and data-sharing contracts
- prepare audit, backup, monitoring, and incident response evidence
- define integration-specific schemas and data minimization rules
- complete any required external technical or legal review

## Remaining Legal / Privacy / Retention Gaps

Production blockers:

- formal legal/privacy approval
- approved privacy notice and user/staff policy
- lawful basis / authorization model for each workflow
- staff onboarding/offboarding and confidentiality process
- final retention periods by data category
- correction/amendment procedure
- deletion/deactivation procedure
- backup retention and secure deletion policy
- AuditLog retention, reviewer, export, and integrity policy
- patient/family access, export, and consent workflow
- guardian/dependent/child access rules before consumer app work
- data-sharing agreements for products, providers, or exchange partners
- incident response and breach review procedure
- production monitoring/alerting evidence
- official Ministry / PGSB / SHAMS requirements review

## Non-Claims

This document does not claim:

- legal compliance is complete
- Health Core is production-ready
- Health Core is Ministry-certified or approved
- Health Core is connected to PGSB, GSB, SHAMS, or any government exchange
- PatientAccessGrant is legally sufficient consent for every workflow
- hard deletion is implemented or approved
- consumer/family health app contracts are ready for public exposure

