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
