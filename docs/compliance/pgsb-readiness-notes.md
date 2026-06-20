# PGSB and E-Health Readiness Notes

This note uses cautious language. Health Core is not yet integrated with PGSB, any government gateway, or any national health information exchange. This document describes readiness foundations, not certification or legal compliance.

## Current Readiness Foundation

Recent Health Core work improves future readiness by implementing:

- centralized permission catalog
- product-specific access profiles
- patient-scoped access grant model
- authorization service
- request context provider
- security AuditLog separate from Timeline
- endpoint-level authorization for current patient-record domains
- audit logging for successful and denied access
- documentation of security decisions and endpoint coverage

These foundations make future review and integration planning easier because access decisions and audit evidence are explicit rather than ad hoc.

## What Is Not Implemented Yet

Health Core does not currently provide:

- PGSB integration
- national health exchange integration
- government gateway connectivity
- formal compliance certificate
- production identity provider integration
- consent/grant management UI or APIs
- legal/privacy-reviewed retention policy
- deployment hardening evidence
- formal incident response process

## Future Work Before Real Integration

Before any real external health exchange integration, Health Core should complete:

- production JWT or service-to-service authentication
- formal legal and privacy review
- secure deployment hardening
- infrastructure and network security requirements review
- operational logging and monitoring
- backup, restore, retention, and disaster recovery policies
- data exchange contracts and schemas
- patient consent and access grant workflows
- encryption policy review
- incident response checklist
- compliance evidence package
- technical certification gap analysis if required by the relevant authority

## PGSB Readiness Framing

The current state should be described as:

"Health Core has implemented a security and auditability foundation that can support future PGSB/e-health readiness work."

It should not be described as:

- PGSB certified
- government integrated
- legally compliant
- nationally connected
- technically certified

## Review Questions for Future Integration

- Which product will integrate first?
- What identity provider and trust model will be used?
- Which patient consent model is required?
- Which data domains will be exchanged?
- What network and gateway requirements apply?
- What audit retention period is required?
- What incident response and reporting commitments apply?
- What formal technical certification checklist applies?
