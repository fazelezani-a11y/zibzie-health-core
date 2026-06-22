import { requestJson } from "./client";

export type PatientAccessGrant = {
  id: string;
  patientId: string;
  productCode: string;
  productRole: string;
  scope: string;
  reason: string;
  granteeUserId: string | null;
  serviceAccountId: string | null;
  isActive: boolean;
  validFrom: string;
  validUntil: string | null;
  grantedAt: string;
  grantedByUserId: string | null;
  grantedByServiceAccountId: string | null;
  revokedAt: string | null;
  revokedByUserId: string | null;
  revokedByServiceAccountId: string | null;
  revokeReason: string | null;
};

export type CreatePatientAccessGrantRequest = {
  granteeUserId?: string | null;
  serviceAccountId?: string | null;
  productCode: string;
  productRole: string;
  scope: string;
  reason: string;
  validFrom?: string | null;
  validUntil?: string | null;
  notes?: string | null;
};

export async function listPatientAccessGrants(
  patientId: string,
): Promise<PatientAccessGrant[]> {
  return requestJson<PatientAccessGrant[]>(
    `/api/health-core/patients/${patientId}/access-grants`,
    {
      cache: "no-store",
    },
  );
}

export async function createPatientAccessGrant(
  patientId: string,
  request: CreatePatientAccessGrantRequest,
): Promise<PatientAccessGrant> {
  return requestJson<PatientAccessGrant>(
    `/api/health-core/patients/${patientId}/access-grants`,
    {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify(request),
    },
  );
}

export async function revokePatientAccessGrant(
  grantId: string,
  reason?: string,
): Promise<PatientAccessGrant> {
  return requestJson<PatientAccessGrant>(
    `/api/health-core/access-grants/${grantId}/revoke`,
    {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify({
        reason: reason?.trim() || null,
      }),
    },
  );
}