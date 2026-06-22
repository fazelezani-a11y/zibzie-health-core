# Health Core Permission Catalog

Health Core permissions are centralized as stable backend constants under
`Zibzie.HealthCore.Domain.Security`.

Phase 79 added the catalog only. Later phases now use these constants for
endpoint enforcement across the current patient-record API surface. The catalog
itself remains a stable set of policy identifiers; it does not create database
tables or migrations.

## Purpose

The permission catalog is the future foundation for:

- Product access profiles
- Role-to-permission mappings
- Patient access grants
- Authorization service checks
- AuditLog context
- Future admin access management UI

## Stability

Permission strings are internal policy identifiers and should remain stable
English keys such as `ViewDocuments` or `CreateCarePlanItem`.

Persian labels can be added later for UI display, but UI labels should not replace
the raw permission keys used by backend policy code.

## Current Classes

- `HealthPermissions`: central permission constants grouped by Health Core area.
- `ProductCodes`: product context identifiers such as `DigiCare` and
  `SecondOpinion`.
- `AccessScopes`: scope identifiers such as `AssignedPatientsOnly` and
  `OwnRecordOnly`.
- `AuthorizationReasons`: access reason identifiers such as `ActiveCare`,
  `PatientShared`, and `Emergency`.

## Service-to-Service Use

Phase 87F does not add separate service-only permissions.

Service tokens should use the same stable `HealthPermissions` values through
`ProductAccessProfiles`. A service account must still provide product context
and a product role/profile, and non-internal service access should be bounded by
`PatientAccessGrant`.

Future service role constants may be added after product/service boundaries are
approved, but they should still map to the central permission catalog rather
than inventing a second permission system.

## Patient Summary

Phase 84E5 added `ViewPatientSummary` for the patient summary endpoint.

`ViewPatientSummary` is a baseline summary permission. The current protected
summary remains all-or-nothing and keeps the existing response shape for allowed
callers. Future partial summary filtering should still check section-level
permissions such as `ViewPatientProfile`, `ViewPatientContactInfo`, and
`ViewMedicalHistory`.

## Patient Profile and Directory

Phase 84H2 added `ViewPatientDirectory` for patient list/search reads.
Phase 84H3 added patient write/lifecycle permissions:

- `CreatePatient`
- `DeactivatePatient`

Existing patient-profile permissions remain:

- `ViewPatientProfile`
- `EditPatientProfile`
- `ViewPatientContactInfo`
- `EditPatientContactInfo`

`ViewPatientDirectory` protects patient existence and directory-style lookup.
`ViewPatientProfile` protects detail/profile reads. `CreatePatient` protects
patient creation. `EditPatientProfile` protects patient updates for now.
`DeactivatePatient` protects the current soft-deactivate endpoint. The current
detail endpoint still returns contact fields all-or-nothing; contact-level
redaction remains a future phase.

## Timeline

Phase 84F added Timeline write permissions:

- `CreateTimelineEvent`
- `EditTimelineEvent`
- `DeleteTimelineEvent`

`ViewTimeline` already existed. Timeline permissions protect patient/care-team
visible timeline history. They do not grant access to `AuditLog`, which remains
covered by separate audit/compliance permissions.

## Next Phases

Later phases should continue applying endpoint enforcement incrementally. Future
patient work should focus on DTO minimization, contact-level redaction, and
grant-scoped directory filtering.

Service-to-service planning is documented in
[Service-to-service auth strategy](service-to-service-auth-strategy.md).
