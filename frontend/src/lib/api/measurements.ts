import {
  optionalBoolean,
  optionalDateTimeOffset,
  optionalNumber,
  optionalValue,
  requestJson,
} from "./client";

export type PatientMeasurement = {
  id: string;
  patientProfileId: string;
  measurementType: string;
  displayName: string;
  value: number;
  unit: string;
  measuredAt: string;
  method: string | null;
  bodySite: string | null;
  context: string | null;
  referenceRange: string | null;
  isAbnormal: boolean | null;
  targetMin: number | null;
  targetMax: number | null;
  sourceType: string;
  relatedRecordType: string | null;
  relatedRecordId: string | null;
  verificationStatus: string;
  sensitivityLevel: string;
  createdAt: string;
  updatedAt: string | null;
};

export type CreatePatientMeasurementPayload = {
  measurementType: string;
  displayName: string;
  value: string;
  unit: string;
  measuredAt: string;
  method: string;
  bodySite: string;
  context: string;
  referenceRange: string;
  isAbnormal: string;
  targetMin: string;
  targetMax: string;
  sourceType: string;
  relatedRecordType: string;
  relatedRecordId: string;
  verificationStatus: string;
  sensitivityLevel: string;
};

export type GetPatientMeasurementsOptions = {
  measurementType?: string;
  sourceType?: string;
  verificationStatus?: string;
  sensitivityLevel?: string;
  measuredFrom?: string;
  measuredTo?: string;
  relatedRecordType?: string;
  relatedRecordId?: string;
};

export async function getPatientMeasurements(
  patientId: string,
  options?: GetPatientMeasurementsOptions,
): Promise<PatientMeasurement[]> {
  const query = new URLSearchParams();

  if (options?.measurementType?.trim()) {
    query.set("measurementType", options.measurementType.trim());
  }

  if (options?.sourceType?.trim()) {
    query.set("sourceType", options.sourceType.trim());
  }

  if (options?.verificationStatus?.trim()) {
    query.set("verificationStatus", options.verificationStatus.trim());
  }

  if (options?.sensitivityLevel?.trim()) {
    query.set("sensitivityLevel", options.sensitivityLevel.trim());
  }

  const measuredFrom = options?.measuredFrom
    ? optionalDateTimeOffset(options.measuredFrom)
    : null;
  const measuredTo = options?.measuredTo
    ? optionalDateTimeOffset(options.measuredTo)
    : null;

  if (measuredFrom) {
    query.set("measuredFrom", measuredFrom);
  }

  if (measuredTo) {
    query.set("measuredTo", measuredTo);
  }

  if (options?.relatedRecordType?.trim()) {
    query.set("relatedRecordType", options.relatedRecordType.trim());
  }

  if (options?.relatedRecordId?.trim()) {
    query.set("relatedRecordId", options.relatedRecordId.trim());
  }

  const queryString = query.toString();
  const path = `/api/health-core/patients/${patientId}/measurements${
    queryString ? `?${queryString}` : ""
  }`;

  return requestJson<PatientMeasurement[]>(path, {
    cache: "no-store",
  });
}

export async function createPatientMeasurement(
  patientId: string,
  input: CreatePatientMeasurementPayload,
): Promise<PatientMeasurement> {
  return requestJson<PatientMeasurement>(
    `/api/health-core/patients/${patientId}/measurements`,
    {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify({
        measurementType: input.measurementType.trim(),
        displayName: optionalValue(input.displayName),
        value: optionalNumber(input.value),
        unit: input.unit.trim(),
        measuredAt: optionalDateTimeOffset(input.measuredAt),
        method: optionalValue(input.method),
        bodySite: optionalValue(input.bodySite),
        context: optionalValue(input.context),
        referenceRange: optionalValue(input.referenceRange),
        isAbnormal: optionalBoolean(input.isAbnormal),
        targetMin: optionalNumber(input.targetMin),
        targetMax: optionalNumber(input.targetMax),
        sourceType: optionalValue(input.sourceType) ?? "Manual",
        relatedRecordType: optionalValue(input.relatedRecordType),
        relatedRecordId: optionalValue(input.relatedRecordId),
        verificationStatus:
          optionalValue(input.verificationStatus) ?? "Unverified",
        sensitivityLevel: optionalValue(input.sensitivityLevel) ?? "Normal",
      }),
    },
  );
}
