const API_BASE_URL =
  process.env.NEXT_PUBLIC_API_BASE_URL?.replace(/\/$/, "") ??
  "http://localhost:5230";

export class ApiError extends Error {
  constructor(
    message: string,
    public readonly status?: number,
  ) {
    super(message);
    this.name = "ApiError";
  }
}

export type PatientListItem = {
  id: string;
  fullName: string;
  birthDate: string | null;
  nationalCode: string | null;
  mobileNumber: string;
  isActive: boolean;
};

export type PatientDetails = {
  id: string;
  firstName: string;
  lastName: string;
  fullName: string;
  birthDate: string | null;
  nationalCode: string | null;
  gender: string | null;
  bloodType: string | null;
  maritalStatus: string | null;
  educationLevel: string | null;
  occupation: string | null;
  mobileNumber: string;
  email: string | null;
  emergencyContactName: string | null;
  emergencyContactPhone: string | null;
  homeAddress: string | null;
  workAddress: string | null;
  isActive: boolean;
  createdAt: string;
};

export type CreatePatientInput = {
  firstName: string;
  lastName: string;
  birthDate: string;
  nationalCode: string;
  gender: string;
  bloodType: string;
  maritalStatus: string;
  educationLevel: string;
  occupation: string;
  mobileNumber: string;
  email: string;
  emergencyContactName: string;
  emergencyContactPhone: string;
  homeAddress: string;
  workAddress: string;
};

export type ConditionSummary = {
  id: string;
  patientProfileId: string;
  name: string;
  status: string | null;
  startedYear: number | null;
  treatmentSummary: string | null;
  clinicianNote: string | null;
  sourceType: string;
  verificationStatus: string;
  sensitivityLevel: string;
  createdAt: string;
  updatedAt: string | null;
};

export type AllergySummary = {
  id: string;
  patientProfileId: string;
  allergen: string;
  allergyType: string | null;
  severity: string | null;
  reactionDescription: string | null;
  clinicianNote: string | null;
  sourceType: string;
  verificationStatus: string;
  sensitivityLevel: string;
  createdAt: string;
  updatedAt: string | null;
};

export type MedicationSummary = {
  id: string;
  patientProfileId: string;
  name: string;
  dose: string | null;
  frequency: string | null;
  route: string | null;
  reason: string | null;
  startDate: string | null;
  endDate: string | null;
  isCurrent: boolean;
  clinicianNote: string | null;
  sourceType: string;
  verificationStatus: string;
  sensitivityLevel: string;
  createdAt: string;
  updatedAt: string | null;
};

export type CreateConditionInput = {
  name: string;
  status: string;
  startedYear: string;
  treatmentSummary: string;
  clinicianNote: string;
};

export type CreateAllergyInput = {
  allergen: string;
  allergyType: string;
  severity: string;
  reactionDescription: string;
  clinicianNote: string;
};

export type CreateMedicationInput = {
  name: string;
  dose: string;
  frequency: string;
  route: string;
  reason: string;
  startDate: string;
  isCurrent: boolean;
  clinicianNote: string;
};

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

export type PatientDocument = {
  id: string;
  patientProfileId: string;
  documentType: string;
  title: string;
  description: string | null;
  documentDate: string | null;
  issuerName: string | null;
  fileName: string | null;
  fileUrl: string | null;
  fileReference: string | null;
  mimeType: string | null;
  fileSizeBytes: number | null;
  sourceType: string;
  verificationStatus: string;
  sensitivityLevel: string;
  createdAt: string;
  updatedAt: string | null;
};

export type CreatePatientDocumentPayload = {
  documentType: string;
  title: string;
  description: string;
  documentDate: string;
  issuerName: string;
  fileName: string;
  fileUrl: string;
  fileReference: string;
  mimeType: string;
  fileSizeBytes: string;
  sourceType: string;
  verificationStatus: string;
  sensitivityLevel: string;
};

export type GetPatientDocumentsOptions = {
  documentType?: string;
  verificationStatus?: string;
  sensitivityLevel?: string;
};

export type PatientSummary = {
  id: string;
  firstName: string;
  lastName: string;
  fullName: string;
  birthDate: string | null;
  nationalCode: string | null;
  gender: string | null;
  bloodType: string | null;
  mobileNumber: string;
  email: string | null;
  emergencyContactName: string | null;
  emergencyContactPhone: string | null;
  homeAddress: string | null;
  workAddress: string | null;
  conditions: ConditionSummary[];
  allergies: AllergySummary[];
  currentMedications: MedicationSummary[];
};

function apiUrl(path: string) {
  return `${API_BASE_URL}${path.startsWith("/") ? path : `/${path}`}`;
}

function optionalValue(value: string) {
  const trimmed = value.trim();
  return trimmed.length > 0 ? trimmed : null;
}

function optionalNumber(value: string) {
  const trimmed = value.trim();
  if (trimmed.length === 0) {
    return null;
  }

  const parsed = Number(trimmed);
  return Number.isFinite(parsed) ? parsed : null;
}

function optionalDateTimeOffset(value: string) {
  const trimmed = value.trim();
  if (trimmed.length === 0) {
    return null;
  }

  const date = new Date(trimmed);
  if (Number.isNaN(date.getTime())) {
    return trimmed;
  }

  return date.toISOString();
}

async function requestJson<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(apiUrl(path), {
    ...init,
    headers: {
      Accept: "application/json",
      ...init?.headers,
    },
  });

  if (!response.ok) {
    let message = `درخواست ناموفق بود. کد خطا: ${response.status}`;

    try {
      const body = (await response.json()) as { message?: string };
      if (body.message) {
        message = body.message;
      }
    } catch {
      // Keep the default message when the API does not return JSON.
    }

    throw new ApiError(message, response.status);
  }

  return response.json();
}

export async function getPatients(): Promise<PatientListItem[]> {
  return requestJson<PatientListItem[]>("/api/health-core/patients", {
    cache: "no-store",
  });
}

export async function createPatient(
  input: CreatePatientInput,
): Promise<PatientDetails> {
  return requestJson<PatientDetails>("/api/health-core/patients", {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify({
      firstName: input.firstName.trim(),
      lastName: input.lastName.trim(),
      birthDate: optionalValue(input.birthDate),
      nationalCode: optionalValue(input.nationalCode),
      gender: optionalValue(input.gender),
      bloodType: optionalValue(input.bloodType),
      maritalStatus: optionalValue(input.maritalStatus),
      educationLevel: optionalValue(input.educationLevel),
      occupation: optionalValue(input.occupation),
      mobileNumber: input.mobileNumber.trim(),
      email: optionalValue(input.email),
      emergencyContactName: optionalValue(input.emergencyContactName),
      emergencyContactPhone: optionalValue(input.emergencyContactPhone),
      homeAddress: optionalValue(input.homeAddress),
      workAddress: optionalValue(input.workAddress),
    }),
  });
}

export async function getPatientSummary(id: string): Promise<PatientSummary> {
  return requestJson<PatientSummary>(`/api/health-core/patients/${id}/summary`, {
    cache: "no-store",
  });
}

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

export async function getPatientDocuments(
  patientId: string,
  options?: GetPatientDocumentsOptions,
): Promise<PatientDocument[]> {
  const query = new URLSearchParams();

  if (options?.documentType?.trim()) {
    query.set("documentType", options.documentType.trim());
  }

  if (options?.verificationStatus?.trim()) {
    query.set("verificationStatus", options.verificationStatus.trim());
  }

  if (options?.sensitivityLevel?.trim()) {
    query.set("sensitivityLevel", options.sensitivityLevel.trim());
  }

  const queryString = query.toString();
  const path = `/api/health-core/patients/${patientId}/documents${
    queryString ? `?${queryString}` : ""
  }`;

  return requestJson<PatientDocument[]>(path, {
    cache: "no-store",
  });
}

export async function createCondition(
  patientId: string,
  input: CreateConditionInput,
): Promise<ConditionSummary> {
  return requestJson<ConditionSummary>(
    `/api/health-core/patients/${patientId}/conditions`,
    {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify({
        name: input.name.trim(),
        status: optionalValue(input.status),
        startedYear: optionalNumber(input.startedYear),
        treatmentSummary: optionalValue(input.treatmentSummary),
        clinicianNote: optionalValue(input.clinicianNote),
        sourceType: "ClinicianEntered",
        verificationStatus: "ClinicianVerified",
        sensitivityLevel: "Normal",
      }),
    },
  );
}

export async function createAllergy(
  patientId: string,
  input: CreateAllergyInput,
): Promise<AllergySummary> {
  return requestJson<AllergySummary>(
    `/api/health-core/patients/${patientId}/allergies`,
    {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify({
        allergen: input.allergen.trim(),
        allergyType: optionalValue(input.allergyType),
        severity: optionalValue(input.severity),
        reactionDescription: optionalValue(input.reactionDescription),
        clinicianNote: optionalValue(input.clinicianNote),
        sourceType: "ClinicianEntered",
        verificationStatus: "ClinicianVerified",
        sensitivityLevel: "Normal",
      }),
    },
  );
}

export async function createMedication(
  patientId: string,
  input: CreateMedicationInput,
): Promise<MedicationSummary> {
  return requestJson<MedicationSummary>(
    `/api/health-core/patients/${patientId}/medications`,
    {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify({
        name: input.name.trim(),
        dose: optionalValue(input.dose),
        frequency: optionalValue(input.frequency),
        route: optionalValue(input.route),
        reason: optionalValue(input.reason),
        startDate: optionalValue(input.startDate),
        endDate: null,
        isCurrent: input.isCurrent,
        clinicianNote: optionalValue(input.clinicianNote),
        sourceType: "ClinicianEntered",
        verificationStatus: "ClinicianVerified",
        sensitivityLevel: "Normal",
      }),
    },
  );
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

export async function createPatientDocument(
  patientId: string,
  input: CreatePatientDocumentPayload,
): Promise<PatientDocument> {
  return requestJson<PatientDocument>(
    `/api/health-core/patients/${patientId}/documents`,
    {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify({
        documentType: input.documentType.trim(),
        title: input.title.trim(),
        description: optionalValue(input.description),
        documentDate: optionalDateTimeOffset(input.documentDate),
        issuerName: optionalValue(input.issuerName),
        fileName: optionalValue(input.fileName),
        fileUrl: optionalValue(input.fileUrl),
        fileReference: optionalValue(input.fileReference),
        mimeType: optionalValue(input.mimeType),
        fileSizeBytes: optionalNumber(input.fileSizeBytes),
        sourceType: optionalValue(input.sourceType) ?? "Manual",
        verificationStatus:
          optionalValue(input.verificationStatus) ?? "Unverified",
        sensitivityLevel: optionalValue(input.sensitivityLevel) ?? "Normal",
      }),
    },
  );
}
