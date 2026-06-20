# Patient Profile and Directory Authorization Strategy

Phase 84H defines the authorization strategy for `PatientsController` standalone patient profile and directory endpoints. This phase is documentation-only. It does not enforce authorization, change DTOs, or change frontend behavior.

## 1. Executive summary

Patient profile and directory endpoints are sensitive because they expose identity, contact, and patient-existence information. They are different from clinical-record modules: even a search result can reveal that a person exists in Health Core.

The current implemented gap is `PatientsController` standalone endpoint enforcement:

- patient list/search
- patient detail/profile
- patient create
- patient update
- patient delete/deactivate

`GET /api/health-core/patients/{patientId}/summary` is already protected by `ViewPatientSummary` and is not part of this enforcement gap.

Recommended direction:

- Add patient-directory specific permissions before enforcement.
- Protect list/search separately from detail/profile.
- Avoid returning strong identifiers and contact/location fields in future directory results unless the caller has stronger permissions.
- Require write/admin permissions for create, update, and deactivate.
- Audit all successful and denied access.
- Preserve current admin behavior during local development through the existing InternalAdmin dev fallback until real authentication and grant workflows exist.

## 2. Current PatientsController endpoint inventory

Base route: `/api/health-core/patients`

| Route | Method | Current action | Current response/request behavior | Current enforcement |
| --- | --- | --- | --- | --- |
| `/api/health-core/patients?search=&page=&pageSize=` | GET | List/search active patients | Searches first name, last name, national code, and mobile number. Returns id, full name, birth date, national code, mobile number, active status. | Protected in Phase 84H2 |
| `/api/health-core/patients/{id}` | GET | Get patient detail/profile | Returns first/last/full name, birth date, national code, gender, blood type, marital status, education, occupation, mobile, email, emergency contact, home/work address, active status, created date. | Protected in Phase 84H2 |
| `/api/health-core/patients/{patientId}/summary` | GET | Get patient summary | Returns profile/contact summary plus conditions, allergies, and current medications. | Protected in Phase 84E5 |
| `/api/health-core/patients` | POST | Create patient | Creates `PatientProfile` and `ContactInfo`. Mobile number is required and checked for duplicates. | Protected in Phase 84H3 |
| `/api/health-core/patients/{id}` | PUT | Update patient | Updates profile and contact fields, including mobile duplicate check. | Protected in Phase 84H3 |
| `/api/health-core/patients/{id}` | DELETE | Deactivate patient | Soft-deactivates patient by setting `IsActive = false`. | Protected in Phase 84H3 |

Frontend usage found:

- `/patients` calls `getPatients()` for the directory list.
- `/patients/new` calls `createPatient()`.
- `/patients/{id}` uses the already-protected patient summary endpoint as its initial detail load.

## 3. Data classification

| Data bucket | Current fields | Sensitivity risk | Roles/products that may need it | Recommended permission | Should appear in list/search? |
| --- | --- | --- | --- | --- | --- |
| Patient directory metadata | patient id, display/full name, active status | Medium. Reveals patient existence and current availability. | InternalAdmin, assigned DigiCare care team, scoped operational products. | `ViewPatientDirectory`; future `SearchPatients` if exact strong-identifier search is split. | Yes, but only in scoped results. |
| Identity profile | first name, last name, birth date, gender, blood type | Medium to high. Identity and demographic data can identify a person. | InternalAdmin, assigned care team, active clinical contexts, patient owner. | Existing `ViewPatientProfile`. | Name/status maybe yes; birth date/blood type only if needed. |
| Strong identifiers | national code, mobile number, email | High. Can uniquely identify or contact the person. | InternalAdmin, assigned case/care managers, authorized care providers, patient owner. | `ViewPatientContactInfo` plus future directory/search permission for searching. | No by default. Only with stronger permission or exact scoped context. |
| Contact/location | home address, work address, emergency contact name/phone | High. Location and emergency contact data create privacy and safety risk. | InternalAdmin, active care operations, HomeVisit visit-scoped staff, patient owner. | Existing `ViewPatientContactInfo`. | No. Should not be in broad directory results. |
| Administrative state | active/inactive, created/updated metadata | Medium. Reveals lifecycle and operational state. | InternalAdmin, HealthCoreAdmin, support/operations as needed. | `ViewPatientDirectory` for read, `DeactivatePatient` for state change. | Active status yes for authorized directory; timestamps usually detail/admin only. |

## 4. Patient existence leakage risk

Patient list/search is the highest-risk read endpoint in `PatientsController` because it can reveal whether a person is in Health Core. Current search supports national code and mobile number, and current list results return national code and mobile number.

Recommended behavior:

- Require a directory/search permission before list/search.
- Return only patients allowed by the caller's product context, role, scope, and active `PatientAccessGrant`.
- Do not reveal whether a patient exists to callers without directory/search permission.
- For unauthorized callers, prefer `403 Forbidden` when the caller is not allowed to use the directory at all.
- For scoped callers with directory permission, return only scoped results. Empty results should mean "nothing visible in this scope", not global non-existence.
- Do not return national code, mobile, email, or address in broad directory results unless the role also has contact/strong-identifier permission.
- Avoid storing sensitive raw search text in audit metadata if it may contain national code, phone, or email. Store query presence, normalized type, page, and page size instead.

## 5. Recommended permission model

Existing relevant permissions:

- `ViewPatientProfile`
- `EditPatientProfile`
- `ViewPatientContactInfo`
- `EditPatientContactInfo`
- `ViewPatientSummary`

Permissions checked during Phase 84H:

- `ViewPatients`
- `SearchPatients`
- `CreatePatient`
- `DeactivatePatient`
- `DeletePatient`

Phase 84H2 added:

- `ViewPatientDirectory`: allows using patient list/search, subject to product scope and grants.

Recommended additions in a later coding phase:

- `SearchPatients`: optional narrower permission if exact national-code/mobile/email search should be separated from ordinary assigned-directory listing.
- `CreatePatient`: allows creating a patient profile and initial contact info.
- `DeactivatePatient`: allows soft-deactivating a patient.

Recommended use of existing permissions:

- `ViewPatientProfile`: basic detail/profile fields.
- `ViewPatientContactInfo`: mobile, email, emergency contact, home address, and work address.
- `EditPatientProfile`: update profile/demographic fields.
- `EditPatientContactInfo`: update contact/location/emergency-contact fields.

Avoid hard-delete semantics for medical records. If a hard delete endpoint is ever introduced, it should require a separate highly restricted permission and legal/compliance review. The current `DELETE` endpoint behaves as soft deactivation and should be modeled as `DeactivatePatient`, not ordinary deletion.

## 6. Recommended product access profile model

InternalAdmin:

- `SuperAdmin` and `HealthCoreAdmin` can have broad directory, profile, contact, create, update, and deactivate permissions.
- `ReadOnlyAuditor` should not receive ordinary directory/profile browsing by default unless an audit investigation workflow explicitly requires it.
- `SupportOperator` should have limited profile/contact access only if needed for support workflows.

DigiCare:

- Case managers and care-team managers may need directory access for assigned patients and possibly broader operational queues depending on the final DigiCare model.
- Clinicians and personal doctors should see assigned patients only.
- Lifestyle specialists should see assigned patient identity and relevant contact only when needed for active care.
- Operations and transport roles should be conservative: minimal identity/contact for logistics, no broad clinical directory.

HomeVisit:

- No broad patient directory by default.
- Doctors should receive temporary visit-scoped access to patient identity, critical contact/location data, and safety-relevant summary data.
- Dispatchers may need contact/location for assigned visits, but not full longitudinal profile browsing.

SecondOpinion:

- Case managers can see case-scoped identity and contact needed to prepare the case.
- Lead and invited specialists should generally receive case-scoped identity. If de-identification is supported later, specialist views may use limited identity.
- No broad patient directory for specialists.

PersonalHealthRecord:

- Patient owner can view and maintain own identity/contact in a future patient-facing contract.
- Family/shared viewers require a separate grant and should not receive broad directory access.
- Shared providers should see only explicitly shared patients.

ClinicQueue:

- Receptionists should not get a clinical or global patient directory from Health Core.
- If queue integration needs identity, use a queue-scoped minimal identity/appointment context.
- Clinic admins should remain conservative unless an organization-scoped directory model is explicitly approved.

## 7. Recommended audit model

Successful list/search:

- `ActionType`: `View`
- `ResourceType`: `PatientProfile` for now, or future `PatientDirectory` if a resource constant is added.
- `PatientId`: empty for broad list/search.
- `MetadataJson`: page, page size, whether search was used, and safe search classification if available. Avoid storing raw national code, mobile, email, or full search text.

Successful detail/profile view:

- `ActionType`: `View`
- `ResourceType`: `PatientProfile`
- `PatientId`: route patient id
- Include permission and request context metadata.

Create:

- `ActionType`: `Create`
- `ResourceType`: `PatientProfile`
- `PatientId`: created patient id
- Include product context and creator identity/service account.

Update:

- `ActionType`: `Update`
- `ResourceType`: `PatientProfile`
- `PatientId`: route patient id
- Consider metadata that lists changed field groups, not raw sensitive before/after values.

Deactivate:

- `ActionType`: `Delete` if following current audit action mapping, or `Update` if the team wants to emphasize soft-delete semantics.
- `ResourceType`: `PatientProfile`
- `PatientId`: route patient id
- Include explicit note/reason in a future request contract if added.

Denied attempts:

- `ActionType`: `AccessDenied`
- Always audit.
- Include attempted permission, product context, role, user/service identity, request metadata, and authorization denial reason.

## 8. Recommended rollout phases

84H1: Patient profile/directory strategy documentation

- This phase.
- No code, API, frontend, schema, or migration changes.

84H2: Add patient profile/directory permissions and protect read endpoints

- Add `ViewPatientDirectory`.
- Protect `GET /api/health-core/patients`.
- Protect `GET /api/health-core/patients/{id}`.
- Preserve current DTOs initially if needed to avoid frontend breakage.
- Ensure InternalAdmin dev fallback still works.
- Audit successful and denied reads.
- Add allowed/denied tests.

84H3: Protect patient create/update/deactivate

- Add `CreatePatient` and `DeactivatePatient`.
- Protect `POST /api/health-core/patients`.
- Protect `PUT /api/health-core/patients/{id}` with profile/contact edit permissions.
- Protect `DELETE /api/health-core/patients/{id}` as soft deactivation.
- Audit all writes and denied attempts.
- Add tests.

84H4: Optional DTO minimization for list/search

- Introduce a directory DTO distinct from full identity/contact profile.
- Remove national code and mobile number from broad list results unless caller has stronger permission.
- Keep full detail/contact data behind detail permission checks.

84H5: Grant-scoped patient directory filtering

- Apply product/role/scope filtering using active `PatientAccessGrant`.
- External products should only see patients covered by active grants or explicit product-scoped assignment.
- InternalAdmin exception should remain narrow.

## 9. Open questions / deferred decisions

- Should ordinary patient directory listing use one permission (`ViewPatientDirectory`) or split listing from exact strong-identifier search (`SearchPatients`)?
- Should `PatientListItemDto` remain as-is for internal admin and a new minimal external directory DTO be added later?
- How will a newly created patient receive initial `PatientAccessGrant` rows?
- Should patient create be allowed only for InternalAdmin/HealthCoreAdmin at first, or also for DigiCare case managers?
- Should future patient lifecycle workflows add more granular permissions beyond the current `DeactivatePatient` soft-deactivation permission?
- How should patient owner/family/shared-provider profile editing be modeled when patient-facing products integrate?
- Should AuditLog use a future `PatientDirectory` resource type for list/search, or continue using `PatientProfile`?
- What identity provider and production claims will replace the current development fallback?
