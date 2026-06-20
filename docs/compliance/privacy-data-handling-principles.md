# Privacy and Data Handling Principles

Health Core stores sensitive health and identity information. These principles should guide future backend, frontend, infrastructure, and product decisions.

## Least Privilege

Callers should receive only the permissions required for their product role and workflow. Broad internal/admin access should remain limited and auditable.

## Need-to-Know Access

Access should be based on an active care, operational, patient-shared, case-invited, emergency, or administrative reason. Product convenience alone is not enough.

## Patient-Scoped Access

External product users should access only patients covered by active grants or approved product scopes. Patient directory/list results should not become a global lookup surface.

## Product-Scoped Access

Each product should have its own access profile. DigiCare, HomeVisit, Second Opinion, Personal Health Record, and Clinic Queue should not share the same broad record access.

## Auditability

Reads, writes, denied access, export/share actions, and grant changes should be auditable. AuditLog must remain separate from patient-facing Timeline.

## No Hard Delete Without Formal Policy

Medical records should generally prefer soft deletion, deactivation, or explicit correction flows. Hard delete should not be introduced without legal, retention, backup, and operational policy.

## Timeline and AuditLog Separation

Timeline is clinical/operational patient history. AuditLog is security/compliance evidence. They serve different audiences and must not be merged.

## Sensitive and Restricted Data

Sensitivity values should become enforceable everywhere sensitive or restricted data can be read or changed. Mixed-sensitivity list endpoints should eventually support filtering or redaction.

## Directory Minimization

Patient list/search endpoints should minimize exposed data. Broad directory responses should avoid strong identifiers and contact/location fields unless the caller has a clear permission, scope, and reason.

## Consent and Grants

Future patient sharing, family access, provider access, Second Opinion invitations, HomeVisit temporary access, and emergency access should be modeled through explicit grants or consent/authorization workflows.

## Retention and Backup

Health Core still needs formal retention, backup, restore, and incident response policies. Audit logs and health records may require different retention and access rules.

## Explainability

The system should be able to explain why access was allowed or denied, which product context was involved, and which grant or internal admin exception applied.
