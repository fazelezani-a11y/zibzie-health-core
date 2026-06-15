"use client";

import { useRouter } from "next/navigation";
import type { FormEvent, ReactNode } from "react";
import { useCallback, useEffect, useState } from "react";
import {
  createCarePlanItem,
  getPatientCarePlan,
  type CarePlanItem,
  type CreateCarePlanItemPayload,
} from "@/lib/api";

const timelineRefreshEventName = "zibzie:timeline-refresh";

const emptyForm: CreateCarePlanItemPayload = {
  category: "FollowUp",
  itemType: "SpecialistVisit",
  title: "",
  description: "",
  reason: "",
  requestedBy: "",
  assignedTo: "",
  plannedAt: "",
  dueAt: "",
  status: "Planned",
  priority: "Normal",
  resultSummary: "",
  nextAction: "",
  relatedRecordType: "",
  relatedRecordId: "",
  sourceType: "Manual",
  verificationStatus: "Unverified",
  sensitivityLevel: "Normal",
};

const categoryOptions = [
  "Diagnostic",
  "Treatment",
  "Care",
  "Lifestyle",
  "FollowUp",
  "Referral",
  "Other",
];

const itemTypeOptions = [
  "LabTest",
  "Imaging",
  "MedicationChange",
  "SpecialistVisit",
  "HomeCare",
  "Nutrition",
  "Exercise",
  "MentalHealth",
  "Education",
  "Other",
];

const statusOptions = [
  "Planned",
  "Scheduled",
  "InProgress",
  "Completed",
  "Cancelled",
  "Deferred",
];

const priorityOptions = ["Low", "Normal", "High", "Urgent"];
const sourceTypeOptions = ["Manual", "ClinicianEntered", "System"];
const verificationStatusOptions = [
  "Unverified",
  "PatientReported",
  "ClinicianVerified",
];
const sensitivityLevelOptions = ["Normal", "Sensitive"];

function formatDateTime(value: string | null) {
  if (!value) {
    return null;
  }

  const date = new Date(value);

  if (Number.isNaN(date.getTime())) {
    return value;
  }

  return new Intl.DateTimeFormat("fa-IR", {
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
  }).format(date);
}

function Field({
  label,
  children,
}: {
  label: string;
  children: ReactNode;
}) {
  return (
    <label className="flex flex-col gap-1.5 text-sm font-medium text-slate-700">
      <span>{label}</span>
      {children}
    </label>
  );
}

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
    <Field label={label}>
      <input
        className="h-10 rounded-md border border-slate-300 bg-white px-3 text-slate-950 outline-none transition placeholder:text-slate-400 focus:border-teal-600 focus:ring-2 focus:ring-teal-100"
        dir={dir}
        onChange={(event) => onChange(event.target.value)}
        required={required}
        type={type}
        value={value}
      />
    </Field>
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
    <Field label={label}>
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
    </Field>
  );
}

function Notice({
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

function MetaItem({
  label,
  value,
}: {
  label: string;
  value: ReactNode;
}) {
  return (
    <div>
      <dt className="text-xs font-medium text-slate-500">{label}</dt>
      <dd className="mt-1 break-words text-sm font-semibold text-slate-800">
        {value}
      </dd>
    </div>
  );
}

function CarePlanItemCard({ item }: { item: CarePlanItem }) {
  const plannedAt = formatDateTime(item.plannedAt);
  const dueAt = formatDateTime(item.dueAt);
  const completedAt = formatDateTime(item.completedAt);

  return (
    <article className="rounded-lg border border-slate-200 bg-white p-4 shadow-sm">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
        <div>
          <h3 className="text-base font-bold text-slate-950">{item.title}</h3>
          {item.description ? (
            <p className="mt-2 text-sm leading-7 text-slate-600">
              {item.description}
            </p>
          ) : null}
        </div>
        <div className="flex shrink-0 flex-wrap gap-2">
          <span className="rounded-md bg-teal-50 px-2.5 py-1 text-xs font-semibold text-teal-800">
            {item.category}
          </span>
          <span className="rounded-md bg-slate-100 px-2.5 py-1 text-xs font-semibold text-slate-700">
            {item.priority}
          </span>
        </div>
      </div>

      <dl className="mt-4 grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
        <MetaItem label="نوع اقدام" value={item.itemType} />
        <MetaItem label="وضعیت" value={item.status} />
        {plannedAt ? (
          <MetaItem label="زمان برنامه‌ریزی‌شده" value={plannedAt} />
        ) : null}
        {dueAt ? <MetaItem label="موعد انجام" value={dueAt} /> : null}
        {completedAt ? (
          <MetaItem label="زمان تکمیل" value={completedAt} />
        ) : null}
        {item.requestedBy ? (
          <MetaItem label="درخواست‌کننده" value={item.requestedBy} />
        ) : null}
        {item.assignedTo ? (
          <MetaItem label="مسئول انجام" value={item.assignedTo} />
        ) : null}
        <MetaItem label="وضعیت تأیید" value={item.verificationStatus} />
        <MetaItem label="سطح حساسیت" value={item.sensitivityLevel} />
        <MetaItem label="منبع" value={item.sourceType} />
        {item.relatedRecordType ? (
          <MetaItem label="نوع رکورد مرتبط" value={item.relatedRecordType} />
        ) : null}
        {item.relatedRecordId ? (
          <MetaItem
            label="شناسه رکورد مرتبط"
            value={<span dir="ltr">{item.relatedRecordId}</span>}
          />
        ) : null}
      </dl>

      {item.reason ? (
        <p className="mt-4 text-sm leading-7 text-slate-600">
          علت / دلیل: {item.reason}
        </p>
      ) : null}
      {item.resultSummary ? (
        <p className="mt-2 text-sm leading-7 text-slate-600">
          خلاصه نتیجه: {item.resultSummary}
        </p>
      ) : null}
      {item.nextAction ? (
        <p className="mt-2 text-sm leading-7 text-slate-600">
          اقدام بعدی: {item.nextAction}
        </p>
      ) : null}
    </article>
  );
}

function CarePlanCreateForm({
  patientId,
  onCreated,
}: {
  patientId: string;
  onCreated: () => Promise<void>;
}) {
  const router = useRouter();
  const [form, setForm] = useState<CreateCarePlanItemPayload>(emptyForm);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  function updateForm<K extends keyof CreateCarePlanItemPayload>(
    key: K,
    value: CreateCarePlanItemPayload[K],
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

    if (!form.category.trim() || !form.itemType.trim() || !form.title.trim()) {
      setErrorMessage("دسته‌بندی، نوع اقدام و عنوان اقدام الزامی هستند.");
      return;
    }

    setIsSubmitting(true);

    try {
      await createCarePlanItem(patientId, form);
      setForm(emptyForm);
      setSuccessMessage("آیتم پلن مراقبتی با موفقیت ثبت شد.");
      await onCreated();
      window.dispatchEvent(new Event(timelineRefreshEventName));
      router.refresh();
    } catch (error) {
      setErrorMessage(
        error instanceof Error
          ? `خطا در ثبت آیتم پلن مراقبتی: ${error.message}`
          : "خطا در ثبت آیتم پلن مراقبتی رخ داد.",
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
        ثبت آیتم پلن مراقبتی جدید
      </h3>
      <div className="mt-3">
        <Notice error={errorMessage} success={successMessage} />
      </div>

      <div className="mt-4 grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
        <SelectInput
          label="دسته‌بندی"
          onChange={(value) => updateForm("category", value)}
          options={categoryOptions}
          value={form.category}
        />
        <SelectInput
          label="نوع اقدام"
          onChange={(value) => updateForm("itemType", value)}
          options={itemTypeOptions}
          value={form.itemType}
        />
        <TextInput
          label="عنوان اقدام"
          onChange={(value) => updateForm("title", value)}
          required
          value={form.title}
        />
        <TextInput
          label="درخواست‌کننده"
          onChange={(value) => updateForm("requestedBy", value)}
          value={form.requestedBy}
        />
        <TextInput
          label="مسئول انجام"
          onChange={(value) => updateForm("assignedTo", value)}
          value={form.assignedTo}
        />
        <TextInput
          label="زمان برنامه‌ریزی‌شده"
          onChange={(value) => updateForm("plannedAt", value)}
          type="datetime-local"
          value={form.plannedAt}
        />
        <TextInput
          label="موعد انجام"
          onChange={(value) => updateForm("dueAt", value)}
          type="datetime-local"
          value={form.dueAt}
        />
        <SelectInput
          label="وضعیت"
          onChange={(value) => updateForm("status", value)}
          options={statusOptions}
          value={form.status}
        />
        <SelectInput
          label="اولویت"
          onChange={(value) => updateForm("priority", value)}
          options={priorityOptions}
          value={form.priority}
        />
        <TextInput
          label="نوع رکورد مرتبط"
          onChange={(value) => updateForm("relatedRecordType", value)}
          value={form.relatedRecordType}
        />
        <TextInput
          dir="ltr"
          label="شناسه رکورد مرتبط"
          onChange={(value) => updateForm("relatedRecordId", value)}
          value={form.relatedRecordId}
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

      <div className="mt-4 grid gap-3 lg:grid-cols-2">
        <Field label="توضیحات">
          <textarea
            className="min-h-20 resize-y rounded-md border border-slate-300 bg-white px-3 py-2 text-slate-950 outline-none transition placeholder:text-slate-400 focus:border-teal-600 focus:ring-2 focus:ring-teal-100"
            onChange={(event) => updateForm("description", event.target.value)}
            value={form.description}
          />
        </Field>
        <Field label="علت / دلیل">
          <textarea
            className="min-h-20 resize-y rounded-md border border-slate-300 bg-white px-3 py-2 text-slate-950 outline-none transition placeholder:text-slate-400 focus:border-teal-600 focus:ring-2 focus:ring-teal-100"
            onChange={(event) => updateForm("reason", event.target.value)}
            value={form.reason}
          />
        </Field>
        <Field label="خلاصه نتیجه">
          <textarea
            className="min-h-20 resize-y rounded-md border border-slate-300 bg-white px-3 py-2 text-slate-950 outline-none transition placeholder:text-slate-400 focus:border-teal-600 focus:ring-2 focus:ring-teal-100"
            onChange={(event) =>
              updateForm("resultSummary", event.target.value)
            }
            value={form.resultSummary}
          />
        </Field>
        <Field label="اقدام بعدی">
          <textarea
            className="min-h-20 resize-y rounded-md border border-slate-300 bg-white px-3 py-2 text-slate-950 outline-none transition placeholder:text-slate-400 focus:border-teal-600 focus:ring-2 focus:ring-teal-100"
            onChange={(event) => updateForm("nextAction", event.target.value)}
            value={form.nextAction}
          />
        </Field>
      </div>

      <button
        className="mt-4 inline-flex h-10 w-full items-center justify-center rounded-md bg-teal-700 px-4 text-sm font-semibold text-white transition hover:bg-teal-800 disabled:cursor-not-allowed disabled:bg-slate-400 sm:w-auto"
        disabled={isSubmitting}
        type="submit"
      >
        {isSubmitting ? "در حال ثبت..." : "ثبت آیتم پلن مراقبتی"}
      </button>
    </form>
  );
}

export default function PatientCarePlan({ patientId }: { patientId: string }) {
  const [items, setItems] = useState<CarePlanItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const loadCarePlan = useCallback(async () => {
    setIsLoading(true);
    setErrorMessage(null);

    try {
      const carePlanItems = await getPatientCarePlan(patientId);
      setItems(carePlanItems);
    } catch (error) {
      setErrorMessage(
        error instanceof Error
          ? error.message
          : "دریافت پلن مراقبتی بیمار با خطا روبه‌رو شد.",
      );
    } finally {
      setIsLoading(false);
    }
  }, [patientId]);

  useEffect(() => {
    const timer = window.setTimeout(() => {
      void loadCarePlan();
    }, 0);

    return () => window.clearTimeout(timer);
  }, [loadCarePlan]);

  return (
    <section className="rounded-lg border border-slate-200 bg-white p-4 shadow-sm">
      <div className="flex flex-col gap-2 border-b border-slate-100 pb-4 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <h2 className="text-xl font-bold text-slate-950">پلن مراقبتی</h2>
          <p className="mt-2 text-sm leading-7 text-slate-600">
            اقدامات برنامه‌ریزی‌شده، مسئولیت‌ها، موعدها و خروجی‌های مراقبتی
            بیمار.
          </p>
        </div>
        <button
          className="inline-flex h-10 items-center justify-center rounded-md border border-slate-300 px-4 text-sm font-semibold text-slate-700 transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:text-slate-400"
          disabled={isLoading}
          onClick={() => {
            void loadCarePlan();
          }}
          type="button"
        >
          به‌روزرسانی
        </button>
      </div>

      <div className="mt-5">
        <CarePlanCreateForm onCreated={loadCarePlan} patientId={patientId} />
      </div>

      <div className="mt-5 space-y-3">
        {isLoading ? (
          <div className="rounded-md border border-slate-200 bg-slate-50 p-4 text-sm text-slate-600">
            در حال دریافت پلن مراقبتی...
          </div>
        ) : null}

        {!isLoading && errorMessage ? (
          <div className="rounded-md border border-rose-200 bg-rose-50 p-4 text-sm leading-7 text-rose-900">
            {errorMessage}
          </div>
        ) : null}

        {!isLoading && !errorMessage && items.length === 0 ? (
          <div className="rounded-md border border-dashed border-slate-300 bg-slate-50 p-4 text-sm leading-7 text-slate-600">
            هنوز آیتمی در پلن مراقبتی این بیمار ثبت نشده است.
          </div>
        ) : null}

        {!isLoading && !errorMessage
          ? items.map((item) => <CarePlanItemCard item={item} key={item.id} />)
          : null}
      </div>
    </section>
  );
}
