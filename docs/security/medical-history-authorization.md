# Medical History Authorization

Phase 84D protects the current Medical History endpoints with authorization and audit logging.

Documents were protected in Phase 84B2. Paraclinical Results were protected in Phase 84C. This phase is limited to Conditions, Allergies, and Medications.

## Protected Routes

| Route | Method | Permission | Audit success action | Audit resource |
|---|---:|---|---|---|
| `/api/health-core/patients/{patientId}/conditions` | GET | `ViewMedicalHistory` | `View` | `Condition` |
| `/api/health-core/patients/{patientId}/conditions` | POST | `EditMedicalHistory` | `Create` | `Condition` |
| `/api/health-core/conditions/{conditionId}` | PUT | `EditMedicalHistory` | `Update` | `Condition` |
| `/api/health-core/conditions/{conditionId}` | DELETE | `EditMedicalHistory` | `Delete` | `Condition` |
| `/api/health-core/patients/{patientId}/allergies` | GET | `ViewMedicalHistory` | `View` | `Allergy` |
| `/api/health-core/patients/{patientId}/allergies` | POST | `EditMedicalHistory` | `Create` | `Allergy` |
| `/api/health-core/allergies/{allergyId}` | PUT | `EditMedicalHistory` | `Update` | `Allergy` |
| `/api/health-core/allergies/{allergyId}` | DELETE | `EditMedicalHistory` | `Delete` | `Allergy` |
| `/api/health-core/patients/{patientId}/medications` | GET | `ViewMedicalHistory` | `View` | `Medication` |
| `/api/health-core/patients/{patientId}/medications` | POST | `EditMedicalHistory` | `Create` | `Medication` |
| `/api/health-core/medications/{medicationId}` | PUT | `EditMedicalHistory` | `Update` | `Medication` |
| `/api/health-core/medications/{medicationId}` | DELETE | `EditMedicalHistory` | `Delete` | `Medication` |

There are no separate detail or verify endpoints for these resources yet.

## Audit Logging

Successful actions are audited with:

- `AuditActionTypes.View`
- `AuditActionTypes.Create`
- `AuditActionTypes.Update`
- `AuditActionTypes.Delete`

Denied attempts are audited with:

- `AuditActionTypes.AccessDenied`
- resource type based on the protected record: `Condition`, `Allergy`, or `Medication`
- attempted permission
- patient id when available
- record id when available
- request context metadata
- authorization denial reason

## Sensitivity Handling

Medical History endpoints call the authorization service through section-aware methods:

- reads use `CanViewPatientSectionAsync`
- writes use `CanEditPatientSectionAsync`

Create uses the requested `SensitivityLevel`.

Update uses the requested `SensitivityLevel` when supplied; otherwise it uses the existing record sensitivity.

Delete uses the existing record sensitivity.

List endpoints use baseline `ViewMedicalHistory` because they return mixed medical-history records and do not perform per-record redaction yet. A future phase may add filtering or redaction for mixed-sensitivity lists if the product needs it.

Restricted sensitivity decisions are delegated to `HealthCoreAuthorizationService`.

## Development Fallback

There is still no real JWT/auth provider.

The Phase 84B1 request-context fallback remains active:

- `ProductCode = InternalAdmin`
- `ProductRole = HealthCoreAdmin`
- `ServiceAccountId = dev-admin`

This keeps the development admin panel usable. It is marked as fallback context and is not production-safe.

## Future Medical History Modules

Future medical-history modules such as surgery, hospitalization, vaccination, family history, and social history should follow the same pattern:

- use `ViewMedicalHistory` for reads
- use `EditMedicalHistory` for writes unless a more specific permission is introduced
- use `VerifyMedicalHistory` for explicit verification workflows
- audit successful and denied access
- resolve patient id before authorization on record-scoped routes

## Not Included Yet

- No authorization on care plan, reminders, measurements, patient summary/profile, or timeline endpoints.
- No frontend changes.
- No real production authentication/JWT integration.
- No access grant creation workflow.
