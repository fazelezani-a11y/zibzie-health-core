import {
  optionalDateTimeOffset,
  optionalNumber,
  optionalValue,
  requestJson,
} from "./client";

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
