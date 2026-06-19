import {
  optionalDateTimeOffset,
  optionalValue,
  requestJson,
} from "./client";

export type PatientReminder = {
  id: string;
  patientProfileId: string;
  reminderType: string;
  title: string;
  description: string | null;
  dueAt: string;
  completedAt: string | null;
  status: string;
  priority: string;
  audience: string;
  channel: string | null;
  relatedRecordType: string | null;
  relatedRecordId: string | null;
  sourceType: string;
  sensitivityLevel: string;
  createdAt: string;
  updatedAt: string | null;
};

export type CreatePatientReminderPayload = {
  reminderType: string;
  title: string;
  description: string;
  dueAt: string;
  status: string;
  priority: string;
  audience: string;
  channel: string;
  relatedRecordType: string;
  relatedRecordId: string;
  sourceType: string;
  sensitivityLevel: string;
};

export type GetPatientRemindersOptions = {
  reminderType?: string;
  status?: string;
  priority?: string;
  audience?: string;
  dueBefore?: string;
  dueAfter?: string;
  includeDone?: boolean;
};

export async function getPatientReminders(
  patientId: string,
  options?: GetPatientRemindersOptions,
): Promise<PatientReminder[]> {
  const query = new URLSearchParams();

  if (options?.reminderType?.trim()) {
    query.set("reminderType", options.reminderType.trim());
  }

  if (options?.status?.trim()) {
    query.set("status", options.status.trim());
  }

  if (options?.priority?.trim()) {
    query.set("priority", options.priority.trim());
  }

  if (options?.audience?.trim()) {
    query.set("audience", options.audience.trim());
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

  if (options?.includeDone !== undefined) {
    query.set("includeDone", String(options.includeDone));
  }

  const queryString = query.toString();
  const path = `/api/health-core/patients/${patientId}/reminders${
    queryString ? `?${queryString}` : ""
  }`;

  return requestJson<PatientReminder[]>(path, {
    cache: "no-store",
  });
}

export async function createPatientReminder(
  patientId: string,
  input: CreatePatientReminderPayload,
): Promise<PatientReminder> {
  return requestJson<PatientReminder>(
    `/api/health-core/patients/${patientId}/reminders`,
    {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify({
        reminderType: input.reminderType.trim(),
        title: input.title.trim(),
        description: optionalValue(input.description),
        dueAt: optionalDateTimeOffset(input.dueAt),
        status: optionalValue(input.status) ?? "Pending",
        priority: optionalValue(input.priority) ?? "Normal",
        audience: optionalValue(input.audience) ?? "Internal",
        channel: optionalValue(input.channel),
        relatedRecordType: optionalValue(input.relatedRecordType),
        relatedRecordId: optionalValue(input.relatedRecordId),
        sourceType: optionalValue(input.sourceType) ?? "Manual",
        sensitivityLevel: optionalValue(input.sensitivityLevel) ?? "Normal",
      }),
    },
  );
}
