# Health Core API Contract Readiness

Phase 95 reviews the current Health Core API boundary for future product integration, especially a future Family Health Record / Family Health Badge product.

This document is not an implementation plan for the consumer app. It does not expose new endpoints, create new product roles, or change authorization behavior.

## Executive Summary

Health Core already has a protected, auditable API surface for internal admin and care-team workflows. The current endpoints are useful as an internal source-of-truth contract, but they are not yet consumer-safe public contracts.

Before any family or patient-facing product uses Health Core directly, the API needs:

- product-specific user identity for patients, guardians, and family members
- ownership and guardian relationship modeling
- PatientAccessGrant or consent workflows for family sharing
- consumer-safe DTOs with redaction/minimization
- clear API versioning
- stricter directory/search exposure rules
- OpenAPI documentation reviewed for public/client use

## Current Boundary

| Boundary | Current state | Contract implication |
| --- | --- | --- |
| Backend | Health Core API is the protected source of truth for patient record domains. | Internal services and admin/care-team clients can integrate through JWT/product context and grants. |
| Admin/care-team panel | Uses existing full-detail DTOs and admin/care-team assumptions. | Not appropriate as a template for consumer data exposure. |
| Consumer app | Not implemented. | Must use its own product identity, route design, DTOs, and UX. |
| Organization/school/clinic apps | Not implemented as Health Core clients yet. | Must be treated as separate product contexts with scoped grants and minimal DTOs. |
| AuditLog | Security/compliance evidence. | Must not be shown as clinical Timeline or exposed to consumer users. |

## Endpoint Inventory by Domain

| Domain | Endpoints | Current intended audience | Future possible consumers | Sensitivity notes | Contract readiness |
| --- | --- | --- | --- | --- | --- |
| Admin auth | `POST /api/health-core/auth/admin/login`, `GET /api/health-core/auth/admin/me` | Internal admin panel only | None directly | Admin credentials and JWT session context. | Internal only. Consumer auth must be separate. |
| Patient directory/profile | `GET /patients`, `GET /patients/{id}`, `POST /patients`, `PUT /patients/{id}`, `DELETE /patients/{id}` | Internal admin/care-team workflows | Future patient owner profile, guardian-managed child profile, clinic/queue identity flows | List/detail currently include identity/contact fields such as national code, mobile, email, address, emergency contact. | Needs consumer-safe directory/profile DTOs and ownership semantics. |
| Patient summary | `GET /patients/{patientId}/summary` | Admin/care-team overview | Future owner/family dashboard | Current summary includes identity/contact plus medical history. It is all-or-nothing. | Needs partial filtering/redaction before consumer use. |
| Medical history | Conditions, allergies, medications list/create/update/delete | Admin/care-team clinical workflow | Owner self-record, family authorized viewer, shared provider | Includes clinician notes, verification status, sensitivity level, source type. | Reads may be reusable after redaction; writes need source/verification model split. |
| Documents | Patient document list/create/detail/update/delete | Admin/care-team evidence management | Owner uploads, family/shared-provider view/upload, emergency card attachments | File URLs/references and verification/sensitivity fields are sensitive. | Needs file-access contract, signed downloads/uploads, redacted metadata DTOs. |
| Paraclinical results | Patient result list/create/detail/update/delete | Admin/care-team clinical workflow | Owner/family lab result view, second opinion sharing | Abnormal flags, interpretations, follow-up notes, lab values. | Needs patient-facing interpretation rules and abnormal/restricted filtering. |
| Care plan | Patient care-plan list/create/detail/update/delete | Care-team task planning | Owner/family view of instructions, provider-shared plan | Can reveal diagnoses, follow-up decisions, assigned staff, internal notes. | Needs consumer-facing instruction DTOs and internal-field redaction. |
| Reminders | Patient reminder list/create/detail/update/delete | Care-team/admin follow-up workflow | Owner/family reminders | Can reveal medication, diagnosis, lifestyle, follow-up details. | Needs audience-specific visibility and mutation rules. |
| Measurements | Patient measurement list/create/detail/update/delete | Care-team/admin monitoring | Owner/family tracking, device/service imports | Health trends such as glucose, BP, BMI, sleep/activity. | Reads/writes likely reusable with ownership and source validation; trend DTOs may be needed. |
| Timeline | Patient timeline list/create/update/delete | Clinical/operational record history | Owner/family readable history, limited event feed | Includes visibility and sensitivity but current UI can include internal events. | Consumer use needs visibility enforcement and public event DTOs. |
| PatientAccessGrant | Grant list/detail/create/revoke | Internal admin access management | Future consent/family sharing backend workflow | Controls who can see patient data. | Admin-only today; consumer consent UI/workflow is future work. |
| AuditLog | `GET /audit-log` | Internal admin/compliance review | None directly | Security evidence, request metadata, failure reasons. | Must remain internal/admin only. |

## Admin/Care-Team Only Endpoints

These endpoints should remain internal/admin or care-team only until a separate public contract exists:

- admin login and admin session endpoints
- patient directory/list/search
- patient create/update/deactivate
- PatientAccessGrant management
- AuditLog review
- broad patient detail/profile responses
- all endpoints returning clinician/internal notes without redaction
- all record delete/update endpoints unless product-specific ownership rules are defined

## Endpoints That Could Later Serve Consumer Products

Some current domains can become the foundation for consumer experiences, but only through safer contracts:

- patient profile read for owner/guardian with redacted identity/contact controls
- patient summary after section-level filtering
- medical history reads with source/verification explanation and clinician-note redaction
- document metadata and uploads with signed file access
- paraclinical result reads with patient-facing interpretation and restricted-result rules
- care-plan instructions with internal task fields removed
- reminders by audience
- measurements and trends
- Timeline events with public/consumer visibility only

## Sensitive Fields Needing Redaction or Separate DTOs

| Field group | Examples | Why it matters | Future contract recommendation |
| --- | --- | --- | --- |
| Strong identifiers | national code, mobile, email | Identity theft, patient existence leakage, contact privacy. | Split into `ViewPatientProfile` vs contact/identifier DTOs. |
| Location/contact | home/work address, emergency contact | Physical safety and privacy risk. | Expose only to owner/guardian/care roles with explicit permission. |
| Clinician/internal notes | clinician notes, follow-up notes, internal descriptions | May include clinician reasoning or sensitive operational context. | Separate patient-facing note/summary fields. |
| File storage references | file URL/reference, issuer, document metadata | Can leak storage paths or access patterns. | Use signed access/download contracts, not raw storage references. |
| Abnormal/restricted flags | abnormal lab/measurement flags, sensitivity level | Can reveal high-risk conditions. | Enforce sensitivity and patient-facing presentation rules. |
| Timeline internal events | internal visibility events | Operational/security confusion. | Filter by visibility and keep AuditLog separate. |
| Audit data | actor, IP, path, access decision | Security evidence, not clinical record content. | Internal/admin only. |

## Product Integration Scenarios

| Product | Current readiness | Needed before integration |
| --- | --- | --- |
| InternalAdmin | Implemented and using admin/care-team contracts. | Continue fallback-off rollout and production hardening. |
| DigiCare | Product roles and permissions exist. | Service identity, PatientAccessGrant lifecycle, and final care-team role approval. |
| HomeVisit | Product roles exist with temporary/access-scoped intent. | Visit-scoped grants, location/contact minimization, visit lifecycle. |
| SecondOpinion | Product roles exist with invited-case intent. | Case-scoped grants, upload/share rules, specialist access lifecycle. |
| PersonalHealthRecord | Owner/family/shared-provider roles exist conceptually. | Real patient/family auth, ownership/guardian model, consumer DTOs. |
| Future Family Health Record / Badge | Not implemented as a product code today. | Decide whether to extend `PersonalHealthRecord` or add a new `FamilyHealthRecord` product code; define owner/guardian/family roles and DTOs. |
| ClinicQueue | Minimal identity/contact role exists. | Queue-specific minimal identity DTOs; no broad clinical data. |

## ProductAccessProfile and PatientAccessGrant Implications

Current profiles already model `PersonalHealthRecordOwner`, `PersonalHealthRecordFamilyViewer`, and `PersonalHealthRecordSharedProvider`, but that does not complete consumer readiness.

Future consumer/family access needs:

- authenticated patient/family user identity
- relationship model for guardian, child, dependent, family viewer, and invited caregiver
- consent/grant creation and revocation workflow
- proof of ownership/guardian authority
- emergency-card sharing policy
- separate permissions for owner write vs family read-only access
- explicit rules for who can upload documents, add measurements, complete reminders, or edit profile/contact data

## Ownership and Guardian Model Gaps

Health Core currently has `PatientAccessGrant`, but it does not yet model:

- patient account ownership
- family relationship graph
- child/dependent guardianship
- guardian verification
- invitation acceptance
- consent capture and withdrawal UX
- emergency card/share link lifecycle
- delegated caregiver permissions

These gaps block safe public consumer exposure.

## Contract Stability Notes

- Current endpoints are appropriate for internal admin/care-team use and internal product integration behind authorization.
- Current DTOs should be treated as internal contracts, not public mobile/web app contracts.
- Avoid changing existing admin DTO shapes casually because the admin panel depends on them.
- Future consumer contracts should be additive and versioned.
- Do not reuse AuditLog or admin access endpoints in consumer clients.

## API Versioning Recommendation

Before public or external product integration, add a versioning strategy such as:

- `/api/health-core/v1/...` for stable API contracts, or
- explicit OpenAPI grouping/versioning if route changes are deferred.

Recommended split:

- internal admin/care-team contracts
- product/service contracts
- consumer/patient/family contracts

Public consumer contracts should be allowed to evolve independently from admin DTOs.

## OpenAPI / Swagger Readiness Notes

Swagger is currently wired for the API, but the contract is not yet curated for external consumers.

Before external use:

- group admin/internal endpoints separately from future consumer endpoints
- document authorization requirements per endpoint
- document sensitivity and redaction behavior
- add response examples for safe DTOs
- hide or clearly mark admin-only endpoints
- avoid exposing development fallback assumptions
- define stable error contracts for 401/403/404/validation failures

## Blocking Gaps Before Public Consumer Exposure

- real patient/family authentication
- ownership and guardian model
- consumer-safe DTOs and redaction
- grant/consent workflows
- file access/download/upload contract
- grant-scoped directory/profile filtering
- patient summary partial filtering
- sensitivity/visibility enforcement improvements
- API versioning
- legal/privacy review
- production monitoring, backup, and incident response evidence

## Current Recommendation

Keep Health Core as the protected source-of-truth API. Do not expose the current admin/care-team DTOs directly to a future consumer frontend.

The next contract-readiness phase should define consumer-safe read models for:

1. profile basics
2. emergency card
3. family member list/relationship model
4. health summary
5. allergies/medications/conditions
6. documents/lab results
7. measurements/trends
8. reminders/tasks
