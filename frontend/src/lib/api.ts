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

export type LabResultItem = {
  id: string;
  patientParaclinicalResultId: string;
  testName: string;
  value: string | null;
  numericValue: number | null;
  unit: string | null;
  referenceRange: string | null;
  isAbnormal: boolean | null;
  interpretation: string | null;
  displayOrder: number;
  createdAt: string;
};

export type ParaclinicalResult = {
  id: string;
  patientProfileId: string;
  resultType: string;
  title: string;
  description: string | null;
  performedAt: string | null;
  resultDate: string | null;
  providerName: string | null;
  linkedDocumentId: string | null;
  summary: string | null;
  interpretation: string | null;
  isAbnormal: boolean | null;
  requiresFollowUp: boolean;
  followUpNote: string | null;
  sourceType: string;
  verificationStatus: string;
  sensitivityLevel: string;
  createdAt: string;
  updatedAt: string | null;
  labItems: LabResultItem[];
};

export type CreateLabResultItemPayload = {
  testName: string;
  value: string;
  numericValue: string;
  unit: string;
  referenceRange: string;
  isAbnormal: string;
  interpretation: string;
};

export type CreateParaclinicalResultPayload = {
  resultType: string;
  title: string;
  description: string;
  performedAt: string;
  resultDate: string;
  providerName: string;
  linkedDocumentId: string;
  summary: string;
  interpretation: string;
  isAbnormal: string;
  requiresFollowUp: boolean;
  followUpNote: string;
  sourceType: string;
  verificationStatus: string;
  sensitivityLevel: string;
  labItems: CreateLabResultItemPayload[];
};

export type GetPatientParaclinicalResultsOptions = {
  resultType?: string;
  verificationStatus?: string;
  sensitivityLevel?: string;
  requiresFollowUp?: boolean;
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

function optionalBoolean(value: string) {
  if (value === "true") {
    return true;
  }

  if (value === "false") {
    return false;
  }

  return null;
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

export async function getPatientParaclinicalResults(
  patientId: string,
  options?: GetPatientParaclinicalResultsOptions,
): Promise<ParaclinicalResult[]> {
  const query = new URLSearchParams();

  if (options?.resultType?.trim()) {
    query.set("resultType", options.resultType.trim());
  }

  if (options?.verificationStatus?.trim()) {
    query.set("verificationStatus", options.verificationStatus.trim());
  }

  if (options?.sensitivityLevel?.trim()) {
    query.set("sensitivityLevel", options.sensitivityLevel.trim());
  }

  if (options?.requiresFollowUp !== undefined) {
    query.set("requiresFollowUp", String(options.requiresFollowUp));
  }

  const queryString = query.toString();
  const path = `/api/health-core/patients/${patientId}/paraclinical-results${
    queryString ? `?${queryString}` : ""
  }`;

  return requestJson<ParaclinicalResult[]>(path, {
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

export async function createParaclinicalResult(
  patientId: string,
  input: CreateParaclinicalResultPayload,
): Promise<ParaclinicalResult> {
  const labItems = input.labItems.map((item, index) => ({
    testName: item.testName.trim(),
    value: optionalValue(item.value),
    numericValue: optionalNumber(item.numericValue),
    unit: optionalValue(item.unit),
    referenceRange: optionalValue(item.referenceRange),
    isAbnormal: optionalBoolean(item.isAbnormal),
    interpretation: optionalValue(item.interpretation),
    displayOrder: index + 1,
  }));

  return requestJson<ParaclinicalResult>(
    `/api/health-core/patients/${patientId}/paraclinical-results`,
    {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify({
        resultType: input.resultType.trim(),
        title: input.title.trim(),
        description: optionalValue(input.description),
        performedAt: optionalDateTimeOffset(input.performedAt),
        resultDate: optionalDateTimeOffset(input.resultDate),
        providerName: optionalValue(input.providerName),
        linkedDocumentId: optionalValue(input.linkedDocumentId),
        summary: optionalValue(input.summary),
        interpretation: optionalValue(input.interpretation),
        isAbnormal: optionalBoolean(input.isAbnormal),
        requiresFollowUp: input.requiresFollowUp,
        followUpNote: optionalValue(input.followUpNote),
        sourceType: optionalValue(input.sourceType) ?? "Manual",
        verificationStatus:
          optionalValue(input.verificationStatus) ?? "Unverified",
        sensitivityLevel: optionalValue(input.sensitivityLevel) ?? "Normal",
        labItems: labItems.length > 0 ? labItems : undefined,
      }),
    },
  );
}
