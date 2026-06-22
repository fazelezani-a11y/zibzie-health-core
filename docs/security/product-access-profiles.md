# Health Core Product Access Profiles

Product access profiles define the intended least-privilege defaults for Zibzie products that call Health Core.

This catalog defines stable role/profile identifiers used by the current authorization service. Endpoint enforcement now uses these profiles together with `PatientAccessGrant` and AuditLog.

## How to Use These Profiles Later

- Each product maps local users and service accounts to stable `ProductRoles`.
- Each role profile declares a product code, access scope, authorization reason, and permission set.
- Actual patient access will still require a patient-level grant or equivalent authorization decision.
- Audit logging should record the product context, role/profile, permission decision, patient/resource, action, and success or failure.
- UI labels can be Persian later, but raw role, product, permission, scope, and reason keys should remain stable English identifiers.

## Service-to-Service Use

Phase 87F keeps service-to-service auth conservative:

- product services should use signed JWTs with `service_account_id` or `client_id`
- service tokens should carry product context and a product role/profile
- non-internal service calls still require patient-scoped grants
- service-specific role constants are deferred until product service boundaries are approved
- human `InternalAdmin` roles must not be reused for product services

See [Service-to-service auth strategy](service-to-service-auth-strategy.md).

Phase 88 adds patient access grant management permissions only to broad
InternalAdmin profiles through `HealthPermissions.All` / the existing
HealthCoreAdmin broad profile. Non-internal product roles are not broadened with
`ViewPatientAccessGrants`, `CreatePatientAccessGrant`, or
`RevokePatientAccessGrant`.

See [PatientAccessGrant admin workflow](patient-access-grant-admin-workflow.md).

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

Phase 84F added Timeline write permissions conservatively. Internal admin roles
can manage Timeline through the existing all-permissions profiles. DigiCare
clinical and care-team-manager roles received limited Timeline management where
they already have broad care-record operational access. Narrow logistics,
receptionist, patient, family, shared-provider, and read-only auditor roles were
not broadened for Timeline writes.

Phase 84H2 added `ViewPatientDirectory` conservatively. Internal admin roles
receive it through the existing all-permissions profiles so the local admin panel
continues to work with the development fallback. DigiCare case-manager and
care-team-manager roles also receive directory access because they are operational
care coordination roles. Narrow logistics, receptionist, patient, family,
HomeVisit, SecondOpinion specialist, and shared-provider roles were not broadened
for global patient directory access.

Phase 84H3 added patient write/lifecycle permissions conservatively. Internal
admin roles receive `CreatePatient` and `DeactivatePatient` through existing
all-permissions profiles. DigiCare case-manager and care-team-manager roles
receive `CreatePatient` because they are operational care coordination roles.
`DeactivatePatient` was not granted to DigiCare or other external product roles
by default; soft deactivation remains an internal admin capability until the
patient lifecycle workflow is specified.

## Next Phases

- Define conservative service role/profile catalog after product service boundaries are approved.
- Add grant creation/revocation workflows.
- Add grant-scoped patient directory filtering.
- Add service-token smoke tests after a safe test issuer exists.
