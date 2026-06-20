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

## Next Phases

Later phases should map products and roles to these permissions, then introduce a
central authorization service and AuditLog. Endpoint enforcement should be applied
incrementally, starting with high-risk health record sections such as documents,
paraclinical results, medical history, care plan, and patient summary.
