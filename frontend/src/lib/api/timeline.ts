import { optionalDateTimeOffset, optionalValue, requestJson } from "./client";

export type TimelineEvent = {
  id: string;
  patientProfileId: string;
  eventType: string;
  title: string;
  description: string | null;
  occurredAt: string;
  sourceType: string;
  relatedRecordType: string | null;
  relatedRecordId: string | null;
  visibility: string;
  sensitivityLevel: string;
  createdAt: string;
  updatedAt: string | null;
};

export type CreateTimelineEventPayload = {
  eventType: string;
  title: string;
  description: string;
  occurredAt: string;
  sourceType: string;
  visibility: string;
  sensitivityLevel: string;
};

export type GetPatientTimelineOptions = {
  eventType?: string;
  includeInternal?: boolean;
};

export async function getPatientTimeline(
  patientId: string,
  options?: GetPatientTimelineOptions,
): Promise<TimelineEvent[]> {
  const query = new URLSearchParams();

  if (options?.eventType?.trim()) {
    query.set("eventType", options.eventType.trim());
  }

  query.set("includeInternal", String(options?.includeInternal ?? true));

  const path = `/api/health-core/patients/${patientId}/timeline?${query.toString()}`;

  return requestJson<TimelineEvent[]>(path, {
    cache: "no-store",
  });
}

export async function createTimelineEvent(
  patientId: string,
  input: CreateTimelineEventPayload,
): Promise<TimelineEvent> {
  return requestJson<TimelineEvent>(
    `/api/health-core/patients/${patientId}/timeline`,
    {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify({
        eventType: input.eventType.trim(),
        title: input.title.trim(),
        description: optionalValue(input.description),
        occurredAt: optionalDateTimeOffset(input.occurredAt),
        sourceType: optionalValue(input.sourceType) ?? "Manual",
        visibility: optionalValue(input.visibility) ?? "Internal",
        sensitivityLevel: optionalValue(input.sensitivityLevel) ?? "Normal",
      }),
    },
  );
}
