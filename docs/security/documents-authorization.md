# Documents Authorization

Phase 84B2 is the first real endpoint enforcement slice in Health Core.

Only document endpoints are protected in this phase. Other endpoint groups remain unchanged.

## Protected Routes

| Route | Method | Permission | Audit success action | Audit resource |
|---|---:|---|---|---|
| `/api/health-core/patients/{patientId}/documents` | GET | `ViewDocuments` | `View` | `Document` |
| `/api/health-core/patients/{patientId}/documents` | POST | `UploadDocuments` | `Create` | `Document` |
| `/api/health-core/documents/{documentId}` | GET | `ViewDocuments` | `View` | `Document` |
| `/api/health-core/documents/{documentId}` | PUT | `EditDocuments` | `Update` | `Document` |
| `/api/health-core/documents/{documentId}` | DELETE | `DeleteDocuments` | `Delete` | `Document` |

Denied attempts are audited with:

- `AuditActionTypes.AccessDenied`
- `AuditResourceTypes.Document`
- attempted permission
- patient id when available
- document id when available
- request context metadata
- authorization denial reason

## Sensitivity Handling

Document endpoints call the authorization service through section-aware methods:

- reads use `CanViewPatientSectionAsync`
- writes use `CanEditPatientSectionAsync`

Single-document routes use the document's current `SensitivityLevel`.

Create uses the requested `SensitivityLevel`.

Update uses the requested `SensitivityLevel` when supplied; otherwise it uses the existing document sensitivity.

List uses the optional `sensitivityLevel` query value when provided. A future phase may add per-record redaction/filtering for mixed-sensitivity lists.

## Request Context

Audit records include:

- `UserId`
- `ServiceAccountId`
- `ProductCode`
- `ProductRole`
- `PatientId`
- document id when known
- permission
- matched access scope when authorization returns one
- correlation id
- IP address
- user agent
- request path
- HTTP method

## Development Fallback

There is still no real JWT/auth provider.

The Phase 84B1 request-context fallback remains active:

- `ProductCode = InternalAdmin`
- `ProductRole = HealthCoreAdmin`
- `ServiceAccountId = dev-admin`

This fallback lets the current development admin panel continue to work. It is marked as fallback context and is not production-safe.

## Not Included Yet

- No authorization on non-document endpoints.
- No frontend changes.
- No file download/share/export endpoints exist yet.
- No access grant creation workflow.
- No real production authentication/JWT integration.
