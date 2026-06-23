import { requestJson } from "./client";

export type AuditLogEntry = {
  id: string;
  userId: string | null;
  serviceAccountId: string | null;
  patientId: string | null;
  productCode: string | null;
  productRole: string | null;
  actionType: string;
  resourceType: string;
  resourceId: string | null;
  permission: string | null;
  accessScope: string | null;
  authorizationReason: string | null;
  succeeded: boolean;
  failureReason: string | null;
  ipAddress: string | null;
  userAgent: string | null;
  correlationId: string | null;
  requestPath: string | null;
  httpMethod: string | null;
  createdAt: string;
};

export type AuditLogQueryResponse = {
  items: AuditLogEntry[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
};

export type AuditLogQuery = {
  patientId?: string;
  actorUserId?: string;
  actorServiceAccountId?: string;
  actionType?: string;
  resourceType?: string;
  from?: string;
  to?: string;
  succeeded?: boolean | null;
  page?: number;
  pageSize?: number;
};

export async function listAuditLog(
  query: AuditLogQuery = {},
): Promise<AuditLogQueryResponse> {
  const searchParams = new URLSearchParams();

  if (query.patientId) {
    searchParams.set("patientId", query.patientId);
  }

  if (query.actorUserId) {
    searchParams.set("actorUserId", query.actorUserId);
  }

  if (query.actorServiceAccountId) {
    searchParams.set("actorServiceAccountId", query.actorServiceAccountId);
  }

  if (query.actionType) {
    searchParams.set("actionType", query.actionType);
  }

  if (query.resourceType) {
    searchParams.set("resourceType", query.resourceType);
  }

  if (query.from) {
    searchParams.set("from", query.from);
  }

  if (query.to) {
    searchParams.set("to", query.to);
  }

  if (query.succeeded !== undefined && query.succeeded !== null) {
    searchParams.set("succeeded", String(query.succeeded));
  }

  if (query.page) {
    searchParams.set("page", String(query.page));
  }

  if (query.pageSize) {
    searchParams.set("pageSize", String(query.pageSize));
  }

  const queryString = searchParams.toString();

  return requestJson<AuditLogQueryResponse>(
    `/api/health-core/audit-log${queryString ? `?${queryString}` : ""}`,
    {
      cache: "no-store",
    },
  );
}
