"use client";

import { useRouter } from "next/navigation";
import type { FormEvent } from "react";
import { useCallback, useEffect, useState } from "react";
import Badge from "@/components/ui/Badge";
import FormField from "@/components/ui/FormField";
import MetaItem from "@/components/ui/MetaItem";
import Notice from "@/components/ui/Notice";
import SectionHeader from "@/components/ui/SectionHeader";
import {
  createPatientDocument,
  getPatientDocuments,
  type CreatePatientDocumentPayload,
  type PatientDocument,
} from "@/lib/api";
import { formatDate } from "@/lib/format";

const timelineRefreshEventName = "zibzie:timeline-refresh";

const emptyForm: CreatePatientDocumentPayload = {
  documentType: "LabResult",
  title: "",
  description: "",
  documentDate: "",
  issuerName: "",
  fileName: "",
  fileUrl: "",
  fileReference: "",
  mimeType: "",
  fileSizeBytes: "",
  sourceType: "Manual",
  verificationStatus: "Unverified",
  sensitivityLevel: "Normal",
};

const documentTypeOptions = [
  "LabResult",
  "Imaging",
  "Prescription",
  "PhysicianReport",
  "Pathology",
  "DischargeSummary",
  "OperationReport",
  "Insurance",
  "Other",
];

const sourceTypeOptions = ["Manual", "ClinicianEntered", "System"];
const verificationStatusOptions = [
  "Unverified",
  "PatientReported",
  "ClinicianVerified",
];
const sensitivityLevelOptions = ["Normal", "Sensitive"];

function TextInput({
  label,
  value,
  onChange,
  required = false,
  type = "text",
  dir,
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
  required?: boolean;
  type?: string;
  dir?: "ltr" | "rtl" | "auto";
}) {
  return (
    <FormField label={label}>
      <input
        className="h-10 rounded-md border border-slate-300 bg-white px-3 text-slate-950 outline-none transition placeholder:text-slate-400 focus:border-teal-600 focus:ring-2 focus:ring-teal-100"
        dir={dir}
        min={type === "number" ? 0 : undefined}
        onChange={(event) => onChange(event.target.value)}
        required={required}
        type={type}
        value={value}
      />
    </FormField>
  );
}

function SelectInput({
  label,
  value,
  options,
  onChange,
}: {
  label: string;
  value: string;
  options: string[];
  onChange: (value: string) => void;
}) {
  return (
    <FormField label={label}>
      <select
        className="h-10 rounded-md border border-slate-300 bg-white px-3 text-slate-950 outline-none transition focus:border-teal-600 focus:ring-2 focus:ring-teal-100"
        onChange={(event) => onChange(event.target.value)}
        value={value}
      >
        {options.map((option) => (
          <option key={option} value={option}>
            {option}
          </option>
        ))}
      </select>
    </FormField>
  );
}

function FormNotice({
  error,
  success,
}: {
  error: string | null;
  success: string | null;
}) {
  if (!error && !success) {
    return null;
  }

  return (
    <div
      className={`rounded-md border p-3 text-sm leading-7 ${
        error
          ? "border-rose-200 bg-rose-50 text-rose-900"
          : "border-emerald-200 bg-emerald-50 text-emerald-900"
      }`}
    >
      {error ?? success}
    </div>
  );
}

function PatientDocumentCard({ document }: { document: PatientDocument }) {
  const documentDate = formatDate(document.documentDate);

  return (
    <article className="rounded-lg border border-slate-200 bg-white p-4 shadow-sm">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
        <div>
          <h3 className="text-base font-bold text-slate-950">
            {document.title}
          </h3>
          {document.description ? (
            <p className="mt-2 text-sm leading-7 text-slate-600">
              {document.description}
            </p>
          ) : null}
        </div>
        <Badge className="shrink-0" tone="info">
          {document.documentType}
        </Badge>
      </div>

      <dl className="mt-4 grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
        {documentDate ? (
          <MetaItem label="تاریخ مدرک" value={documentDate} />
        ) : null}
        {document.issuerName ? (
          <MetaItem
            label="صادرکننده / مرکز"
            value={document.issuerName}
          />
        ) : null}
        {document.fileName ? (
          <MetaItem label="نام فایل" value={document.fileName} />
        ) : null}
        {document.fileUrl ? (
          <MetaItem
            label="لینک فایل"
            value={
              <a
                className="break-all text-teal-700 transition hover:text-teal-900"
                href={document.fileUrl}
                rel="noreferrer"
                target="_blank"
              >
                {document.fileUrl}
              </a>
            }
          />
        ) : null}
        <MetaItem
          label="وضعیت تأیید"
          value={document.verificationStatus}
        />
        <MetaItem
          label="سطح حساسیت"
          value={document.sensitivityLevel}
        />
        <MetaItem label="منبع" value={document.sourceType} />
      </dl>
    </article>
  );
}

function PatientDocumentCreateForm({
  patientId,
  onCreated,
}: {
  patientId: string;
  onCreated: () => Promise<void>;
}) {
  const router = useRouter();
  const [form, setForm] = useState<CreatePatientDocumentPayload>(emptyForm);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  function updateForm<K extends keyof CreatePatientDocumentPayload>(
    key: K,
    value: CreatePatientDocumentPayload[K],
  ) {
    setForm((current) => ({
      ...current,
      [key]: value,
    }));
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setErrorMessage(null);
    setSuccessMessage(null);

    if (!form.documentType.trim() || !form.title.trim()) {
      setErrorMessage("نوع مدرک و عنوان مدرک الزامی هستند.");
      return;
    }

    setIsSubmitting(true);

    try {
      await createPatientDocument(patientId, form);
      setForm(emptyForm);
      setSuccessMessage("مدرک پزشکی با موفقیت ثبت شد.");
      await onCreated();
      window.dispatchEvent(new Event(timelineRefreshEventName));
      router.refresh();
    } catch (error) {
      setErrorMessage(
        error instanceof Error
          ? `خطا در ثبت مدرک پزشکی: ${error.message}`
          : "خطا در ثبت مدرک پزشکی رخ داد.",
      );
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <form
      className="rounded-lg border border-slate-200 bg-slate-50 p-4"
      onSubmit={handleSubmit}
    >
      <h3 className="text-base font-bold text-slate-950">
        ثبت مدرک پزشکی جدید
      </h3>
      <div className="mt-3">
        <FormNotice error={errorMessage} success={successMessage} />
      </div>

      <div className="mt-4 grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
        <SelectInput
          label="نوع مدرک"
          onChange={(value) => updateForm("documentType", value)}
          options={documentTypeOptions}
          value={form.documentType}
        />
        <TextInput
          label="عنوان مدرک"
          onChange={(value) => updateForm("title", value)}
          required
          value={form.title}
        />
        <TextInput
          label="تاریخ مدرک"
          onChange={(value) => updateForm("documentDate", value)}
          type="date"
          value={form.documentDate}
        />
        <TextInput
          label="صادرکننده / مرکز"
          onChange={(value) => updateForm("issuerName", value)}
          value={form.issuerName}
        />
        <TextInput
          label="نام فایل"
          onChange={(value) => updateForm("fileName", value)}
          value={form.fileName}
        />
        <TextInput
          dir="ltr"
          label="لینک فایل"
          onChange={(value) => updateForm("fileUrl", value)}
          type="url"
          value={form.fileUrl}
        />
        <TextInput
          dir="ltr"
          label="شناسه/رفرنس فایل"
          onChange={(value) => updateForm("fileReference", value)}
          value={form.fileReference}
        />
        <TextInput
          dir="ltr"
          label="نوع فایل"
          onChange={(value) => updateForm("mimeType", value)}
          value={form.mimeType}
        />
        <TextInput
          dir="ltr"
          label="حجم فایل"
          onChange={(value) => updateForm("fileSizeBytes", value)}
          type="number"
          value={form.fileSizeBytes}
        />
        <SelectInput
          label="منبع"
          onChange={(value) => updateForm("sourceType", value)}
          options={sourceTypeOptions}
          value={form.sourceType}
        />
        <SelectInput
          label="وضعیت تأیید"
          onChange={(value) => updateForm("verificationStatus", value)}
          options={verificationStatusOptions}
          value={form.verificationStatus}
        />
        <SelectInput
          label="سطح حساسیت"
          onChange={(value) => updateForm("sensitivityLevel", value)}
          options={sensitivityLevelOptions}
          value={form.sensitivityLevel}
        />
      </div>

      <FormField label="توضیحات">
        <textarea
          className="mt-1 min-h-20 resize-y rounded-md border border-slate-300 bg-white px-3 py-2 text-slate-950 outline-none transition placeholder:text-slate-400 focus:border-teal-600 focus:ring-2 focus:ring-teal-100"
          onChange={(event) => updateForm("description", event.target.value)}
          value={form.description}
        />
      </FormField>

      <button
        className="mt-4 inline-flex h-10 w-full items-center justify-center rounded-md bg-teal-700 px-4 text-sm font-semibold text-white transition hover:bg-teal-800 disabled:cursor-not-allowed disabled:bg-slate-400 sm:w-auto"
        disabled={isSubmitting}
        type="submit"
      >
        {isSubmitting ? "در حال ثبت..." : "ثبت مدرک پزشکی"}
      </button>
    </form>
  );
}

export default function PatientDocuments({ patientId }: { patientId: string }) {
  const [documents, setDocuments] = useState<PatientDocument[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const loadDocuments = useCallback(async () => {
    setIsLoading(true);
    setErrorMessage(null);

    try {
      const patientDocuments = await getPatientDocuments(patientId);
      setDocuments(patientDocuments);
    } catch (error) {
      setErrorMessage(
        error instanceof Error
          ? error.message
          : "دریافت مدارک پزشکی بیمار با خطا روبه‌رو شد.",
      );
    } finally {
      setIsLoading(false);
    }
  }, [patientId]);

  useEffect(() => {
    const timer = window.setTimeout(() => {
      void loadDocuments();
    }, 0);

    return () => window.clearTimeout(timer);
  }, [loadDocuments]);

  return (
    <section className="rounded-lg border border-slate-200 bg-white p-4 shadow-sm">
      <SectionHeader
        action={
          <button
            className="inline-flex h-10 items-center justify-center rounded-md border border-slate-300 px-4 text-sm font-semibold text-slate-700 transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:text-slate-400"
            disabled={isLoading}
            onClick={() => {
              void loadDocuments();
            }}
            type="button"
          >
            به‌روزرسانی
          </button>
        }
        description="متادیتا و ارجاع فایل‌های مرتبط با پرونده سلامت بیمار."
        title="مدارک پزشکی"
      />

      <div className="mt-5">
        <PatientDocumentCreateForm
          onCreated={loadDocuments}
          patientId={patientId}
        />
      </div>

      <div className="mt-5 space-y-3">
        {isLoading ? (
          <Notice variant="loading">در حال دریافت مدارک پزشکی...</Notice>
        ) : null}

        {!isLoading && errorMessage ? (
          <Notice variant="error">{errorMessage}</Notice>
        ) : null}

        {!isLoading && !errorMessage && documents.length === 0 ? (
          <Notice variant="empty">
            هنوز مدرک پزشکی برای این بیمار ثبت نشده است.
          </Notice>
        ) : null}

        {!isLoading && !errorMessage
          ? documents.map((document) => (
              <PatientDocumentCard document={document} key={document.id} />
            ))
          : null}
      </div>
    </section>
  );
}
