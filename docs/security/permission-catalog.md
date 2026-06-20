# Health Core Permission Catalog

Health Core permissions are centralized as stable backend constants under
`Zibzie.HealthCore.Domain.Security`.

This phase adds the catalog only. It does not enforce permissions on endpoints,
change controller behavior, add database tables, or introduce migrations.

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

## Patient Summary

Phase 84E5 added `ViewPatientSummary` for the patient summary endpoint.

`ViewPatientSummary` is a baseline summary permission. The current protected
summary remains all-or-nothing and keeps the existing response shape for allowed
callers. Future partial summary filtering should still check section-level
permissions such as `ViewPatientProfile`, `ViewPatientContactInfo`, and
`ViewMedicalHistory`.

## Patient Profile and Directory

Phase 84H2 added `ViewPatientDirectory` for patient list/search reads.

Existing patient-profile permissions remain:

- `ViewPatientProfile`
- `EditPatientProfile`
- `ViewPatientContactInfo`
- `EditPatientContactInfo`

`ViewPatientDirectory` protects patient existence and directory-style lookup.
`ViewPatientProfile` protects detail/profile reads. The current detail endpoint
still returns contact fields all-or-nothing; contact-level redaction remains a
future phase.

## Timeline

Phase 84F added Timeline write permissions:

- `CreateTimelineEvent`
- `EditTimelineEvent`
- `DeleteTimelineEvent`

`ViewTimeline` already existed. Timeline permissions protect patient/care-team
visible timeline history. They do not grant access to `AuditLog`, which remains
covered by separate audit/compliance permissions.

## Next Phases

Later phases should continue applying endpoint enforcement incrementally. Patient
create/update/deactivate permissions remain future work after the Phase 84H2
read-only patient directory/profile enforcement.
