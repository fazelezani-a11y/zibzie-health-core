# ADR 0001: Health Core automation foundation

## Status

Proposed

## Context

Health Core currently supports manual and care-team-entered health record data:
patient profiles, medical history, timeline events, documents, paraclinical results
with lab items, care plan items, reminders, and measurements.

Future Health Core products may auto-generate or semi-automate reminders,
measurements, care plan items, alerts, and timeline events from existing record
data. Examples include care plan due dates creating reminders, lab items creating
measurements, abnormal measurements creating internal alerts, and chronic
conditions suggesting follow-up plans.

Automation must be explainable, auditable, and idempotent. The system needs to
answer why a record was generated, which trigger caused it, which rule version ran,
whether the same rule already produced the same output, and what happened during
execution.

Current MVP fields such as `SourceType`, `RelatedRecordType`, `RelatedRecordId`,
`VerificationStatus`, and `SensitivityLevel` are useful for distinguishing manual,
system, imported, and linked records. They are enough for the MVP, but not enough
for replayable and explainable automation.

Timeline events are clinical/user-facing history. They should not become the
technical source of truth for rule execution, provenance, or idempotency.

## Decision

- Do not implement a Rule Engine yet.
- Keep the current MVP fields and behavior for now.
- Centralize canonical source, status, type, visibility, priority, and sensitivity
  string values before introducing automation.
- Treat generated records as normal domain records with explicit provenance, not as
  a separate parallel record system.
- Introduce `RuleExecutionLog` before executing real rules.
- Add generated-record linkage so generated outputs can be connected to trigger
  records and rule executions.
- Use idempotency keys to prevent duplicate generated records when rules are
  retried, replayed, or run repeatedly.
- Keep timeline events as optional user-facing outputs of automation, not the
  source of technical truth.
- Make the first future deterministic rule narrow and low risk:
  `CarePlanItem.DueAt -> Reminder`.

## Proposed Future Model Additions

These are not implemented by this ADR.

- `RuleDefinition`: rule identity, name, status, version, description, and owner.
- `RuleTrigger`: trigger type and trigger record criteria.
- `RuleAction`: action type and action payload template.
- `RuleExecutionLog`: execution id, rule id/version, trigger record, started/ended
  timestamps, status, result, errors, and correlation data.
- `GeneratedRecordLink`: connects generated records to rule executions, trigger
  records, and optional parent records.
- `IdempotencyKey`: deterministic key for generated output uniqueness.
- Optional denormalized fields such as `GeneratedByRuleId` and
  `GeneratedByExecutionId` on generated record types if query performance or UI
  simplicity later justifies them.
- `MedicationSchedule`: structured medication timing before medication reminders
  are automated.
- Lab test mapping to measurement types before lab items generate measurements.

## Canonical Vocabulary Direction

String values should be centralized before automation so rules do not depend on
scattered literals or inconsistent terminology.

Current value drift to reconcile includes:

- `PatientSelfReport`
- `SelfReported`
- `PatientReported`
- `ClinicianEntered`
- `Manual`
- `System`

Canonical values should be defined in backend application/domain constants first,
then reused by controllers, services, tests, smoke scripts, and eventually the UI.

## Consequences

Positive outcomes:

- Better auditability for generated records.
- Safer automation with fewer accidental duplicates.
- Clearer provenance for care-team review.
- Easier future patient-facing explanations.
- A cleaner path from manual MVP workflows to deterministic automation.

Tradeoffs:

- More schema and service complexity in the automation phase.
- More upfront design before implementing a rule engine.
- More discipline required around constants, provenance, and generated-record
  ownership.

## Non-Goals

- No Rule Engine implementation now.
- No schema changes in this ADR.
- No migrations.
- No backend behavior changes.
- No frontend UI changes.
- No notification delivery system yet.
- No medication reminder automation until structured `MedicationSchedule` exists.
- No lab-to-measurement automation until lab mapping exists.

## Near-Term Follow-Up Tasks

- Centralize backend constants for source types, verification statuses, event
  types, visibility, priority, and sensitivity.
- Design provenance and generated-record linkage schema.
- Introduce application services for create flows and timeline side effects.
- Add generated/source badges later in the UI.
- Use the repeatable smoke test script in future PR verification.
- Eventually implement `CarePlanItem.DueAt -> Reminder` as the first narrow,
  deterministic rule.
