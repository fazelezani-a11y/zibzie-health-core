# Health Core Product Access Profiles

Product access profiles define the intended least-privilege defaults for Zibzie products that call Health Core.

This catalog is code-only for now. It does not enforce endpoint authorization, change API behavior, or create database records. Future phases will use these stable role/profile identifiers with `PatientAccessGrant`, an authorization service, and security `AuditLog` records.

## How to Use These Profiles Later

- Each product maps local users and service accounts to stable `ProductRoles`.
- Each role profile declares a product code, access scope, authorization reason, and permission set.
- Actual patient access will still require a patient-level grant or equivalent authorization decision.
- Audit logging should record the product context, role/profile, permission decision, patient/resource, action, and success or failure.
- UI labels can be Persian later, but raw role, product, permission, scope, and reason keys should remain stable English identifiers.

## Included Product Contexts

- `InternalAdmin`
- `DigiCare`
- `HomeVisit`
- `SecondOpinion`
- `PersonalHealthRecord`
- `ClinicQueue`

## Conservative Defaults

Some roles are intentionally narrow until product workflows are fully specified:

- HomeVisit doctors get temporary clinical access, but not broad document/result/timeline browsing by default.
- HomeVisit dispatchers and DigiCare transport coordinators get identity/contact/logistics access only.
- ClinicQueue roles do not get deep medical history, documents, results, care plan, reminders, or measurements by default.
- Second Opinion access is scoped to invited cases.
- Personal Health Record family and shared-provider roles rely on explicit sharing grants before real access is allowed.

Phase 84E5 added `ViewPatientSummary` only to roles that already have the
current summary's required shape: patient profile/contact access plus medical
history read access. Narrow logistics, receptionist, auditor, and shared-viewer
roles were not broadened for the current all-or-nothing summary.

## Next Phases

- Persist patient-product-user access grants.
- Implement an authorization service using product context, role profile, permissions, scope, sensitivity, and grants.
- Add security audit logging separate from patient-facing timeline events.
- Apply authorization checks to high-risk endpoints first.
