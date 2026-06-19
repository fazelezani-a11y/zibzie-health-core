import {
  optionalDateTimeOffset,
  optionalValue,
  requestJson,
} from "./client";

export type CarePlanItem = {
  id: string;
  patientProfileId: string;
  category: string;
  itemType: string;
  title: string;
  description: string | null;
  reason: string | null;
  requestedBy: string | null;
  assignedTo: string | null;
  plannedAt: string | null;
  dueAt: string | null;
  completedAt: string | null;
  status: string;
  priority: string;
  resultSummary: string | null;
  nextAction: string | null;
  relatedRecordType: string | null;
  relatedRecordId: string | null;
  sourceType: string;
  verificationStatus: string;
  sensitivityLevel: string;
  createdAt: string;
  updatedAt: string | null;
};

export type CreateCarePlanItemPayload = {
  category: string;
  itemType: string;
  title: string;
  description: string;
  reason: string;
  requestedBy: string;
  assignedTo: string;
  plannedAt: string;
  dueAt: string;
  status: string;
  priority: string;
  resultSummary: string;
  nextAction: string;
  relatedRecordType: string;
  relatedRecordId: string;
  sourceType: string;
  verificationStatus: string;
  sensitivityLevel: string;
};

export type GetPatientCarePlanOptions = {
  category?: string;
  itemType?: string;
  status?: string;
  priority?: string;
  dueBefore?: string;
  dueAfter?: string;
};

export async function getPatientCarePlan(
  patientId: string,
  options?: GetPatientCarePlanOptions,
): Promise<CarePlanItem[]> {
  const query = new URLSearchParams();

  if (options?.category?.trim()) {
    query.set("category", options.category.trim());
  }

  if (options?.itemType?.trim()) {
    query.set("itemType", options.itemType.trim());
  }

  if (options?.status?.trim()) {
    query.set("status", options.status.trim());
  }

  if (options?.priority?.trim()) {
    query.set("priority", options.priority.trim());
  }

  const dueBefore = options?.dueBefore
    ? optionalDateTimeOffset(options.dueBefore)
    : null;
  const dueAfter = options?.dueAfter
    ? optionalDateTimeOffset(options.dueAfter)
    : null;

  if (dueBefore) {
    query.set("dueBefore", dueBefore);
  }

  if (dueAfter) {
    query.set("dueAfter", dueAfter);
  }

  const queryString = query.toString();
  const path = `/api/health-core/patients/${patientId}/care-plan${
    queryString ? `?${queryString}` : ""
  }`;

  return requestJson<CarePlanItem[]>(path, {
    cache: "no-store",
  });
}

export async function createCarePlanItem(
  patientId: string,
  input: CreateCarePlanItemPayload,
): Promise<CarePlanItem> {
  return requestJson<CarePlanItem>(
    `/api/health-core/patients/${patientId}/care-plan`,
    {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify({
        category: input.category.trim(),
        itemType: input.itemType.trim(),
        title: input.title.trim(),
        description: optionalValue(input.description),
        reason: optionalValue(input.reason),
        requestedBy: optionalValue(input.requestedBy),
        assignedTo: optionalValue(input.assignedTo),
        plannedAt: optionalDateTimeOffset(input.plannedAt),
        dueAt: optionalDateTimeOffset(input.dueAt),
        status: optionalValue(input.status) ?? "Planned",
        priority: optionalValue(input.priority) ?? "Normal",
        resultSummary: optionalValue(input.resultSummary),
        nextAction: optionalValue(input.nextAction),
        relatedRecordType: optionalValue(input.relatedRecordType),
        relatedRecordId: optionalValue(input.relatedRecordId),
        sourceType: optionalValue(input.sourceType) ?? "Manual",
        verificationStatus:
          optionalValue(input.verificationStatus) ?? "Unverified",
        sensitivityLevel: optionalValue(input.sensitivityLevel) ?? "Normal",
      }),
    },
  );
}
