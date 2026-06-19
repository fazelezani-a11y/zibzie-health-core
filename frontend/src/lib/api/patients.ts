import { optionalValue, requestJson } from "./client";
import type {
  AllergySummary,
  ConditionSummary,
  MedicationSummary,
} from "./medical-history";

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
