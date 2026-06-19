import {
  optionalBoolean,
  optionalDateTimeOffset,
  optionalNumber,
  optionalValue,
  requestJson,
} from "./client";

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
