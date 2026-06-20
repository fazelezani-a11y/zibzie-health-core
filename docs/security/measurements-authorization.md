# Measurements Authorization

Phase 84E3 protects the Measurement endpoint group with authorization and audit logging.

Documents, Paraclinical Results, current Medical History endpoints, Care Plan endpoints, and Reminder endpoints were protected in earlier phases. This phase is limited to Patient Measurements.

## Protected Routes

| Route | Method | Permission | Audit success action | Audit resource |
|---|---:|---|---|---|
| `/api/health-core/patients/{patientId}/measurements` | GET | `ViewMeasurements` | `View` | `Measurement` |
| `/api/health-core/patients/{patientId}/measurements` | POST | `CreateMeasurement` | `Create` | `Measurement` |
| `/api/health-core/measurements/{measurementId}` | GET | `ViewMeasurements` | `View` | `Measurement` |
| `/api/health-core/measurements/{measurementId}` | PUT | `EditMeasurement` | `Update` | `Measurement` |
| `/api/health-core/measurements/{measurementId}` | DELETE | `EditMeasurement` | `Delete` | `Measurement` |

There are no separate trend/chart endpoints yet. The existing list endpoint supports filters that frontend graph and overview views can use.

## Audit Logging

Successful actions are audited with:

- `AuditActionTypes.View`
- `AuditActionTypes.Create`
- `AuditActionTypes.Update`
- `AuditActionTypes.Delete`

Denied attempts are audited with:

- `AuditActionTypes.AccessDenied`
- `AuditResourceTypes.Measurement`
- attempted permission
- patient id when available
- measurement id when available
- request context metadata
- authorization denial reason

## Sensitivity Handling

Measurement endpoints call the authorization service through section-aware methods:

- reads use `CanViewPatientSectionAsync`
- writes use `CanEditPatientSectionAsync`

Single-measurement routes use the measurement's current `SensitivityLevel`.

Create uses the requested `SensitivityLevel`.

Update uses the requested `SensitivityLevel` when supplied; otherwise it uses the existing measurement sensitivity.

List uses the optional `sensitivityLevel` query value when provided. A future phase may add per-record filtering or redaction for mixed-sensitivity measurement lists if needed.

Restricted sensitivity decisions are delegated to `HealthCoreAuthorizationService`.

## Privacy Sensitivity

Measurements are privacy-sensitive because they can reveal clinical monitoring, abnormal trends, lifestyle, sleep, activity, glucose, blood pressure, BMI, and disease-control status.

Phase 84E3 only adds endpoint authorization and audit logging. Existing graph and overview behavior is unchanged:

- no graph calculation behavior changed
- no frontend graph rendering changed
- no patient overview or patient summary endpoint behavior changed
- no measurement creation side effects changed

## Audit Volume

Measurement list and future trend/chart endpoints may be called frequently by dashboard and graph views.

For Phase 84E3, successful reads are audited as `View`, following the current protected endpoint pattern. If audit volume becomes high, a future phase may aggregate or summarize frequent graph/list reads while still always auditing denied attempts and restricted-data access.

## Development Fallback

There is still no real JWT/auth provider.

The Phase 84B1 request-context fallback remains active:

- `ProductCode = InternalAdmin`
- `ProductRole = HealthCoreAdmin`
- `ServiceAccountId = dev-admin`

This keeps the development admin panel usable. It is marked as fallback context and is not production-safe.

## Not Included Yet

- No authorization on patient summary/profile or timeline endpoints.
- No frontend changes.
- No real production authentication/JWT integration.
- No access grant creation workflow.
