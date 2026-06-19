import { optionalNumber, optionalValue, requestJson } from "./client";

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
