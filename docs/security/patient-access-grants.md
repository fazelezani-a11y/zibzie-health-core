# Patient Access Grants

`PatientAccessGrant` is the foundation for patient-scoped access decisions in Health Core.

It represents that a user or service account can access a specific patient record in a specific product context, under a product role, with a defined scope and authorization reason.

This is not endpoint enforcement yet. Controllers do not currently check grants, and no API behavior changes in this phase.

## What a Grant Represents

A grant records:

- the patient record being accessed
- the user or service account receiving access
- the product context, such as `DigiCare`, `HomeVisit`, or `SecondOpinion`
- the product role, such as `DigiCareCaseManager` or `SecondOpinionInvitedSpecialist`
- the access scope, such as `AssignedPatientsOnly`, `TemporaryAccess`, or `OwnRecordOnly`
- the authorization reason, such as `ActiveCare`, `PatientShared`, or `Emergency`
- the validity window
- who granted access and when
- whether access was revoked, by whom, and why

## What a Grant Does Not Store

Grants do not duplicate permission lists.

Permissions come from `ProductAccessProfiles`, which map product roles to stable Health Core permissions. The future authorization service should combine:

- identity
- product context
- product role/profile
- patient access grant
- requested permission
- access scope
- sensitivity level
- consent or authorization reason

## Supported Future Scenarios

The model supports:

- assigned care-team access
- temporary HomeVisit access
- invited Second Opinion case access
- patient-owned record access
- family-authorized record sharing
- shared provider access
- product service account access
- emergency access, when explicitly granted and audited

## Revocation and Expiry

`RevokedAt` being null means the grant has not been revoked. A grant can still be inactive if it is not yet valid or if `ValidUntil` has passed.

`ValidUntil` being null means no scheduled expiry. The grant can still be revoked.

## Audit Expectations

Grant creation, use, denial, expiry, and revocation should later be recorded in a security `AuditLog`.

Timeline is not AuditLog. Timeline is patient/clinical-facing history, while AuditLog is security, legal, and compliance evidence.

## Next Steps

- Add an authorization service that checks grants and product access profiles.
- Add a security audit log model and service.
- Apply authorization checks to high-risk endpoints first.
- Add admin tooling later for creating, reviewing, and revoking grants.
