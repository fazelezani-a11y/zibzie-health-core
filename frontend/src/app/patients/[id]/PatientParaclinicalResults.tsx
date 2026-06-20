"use client";

import { useRouter } from "next/navigation";
import type { FormEvent, ReactNode } from "react";
import { useCallback, useEffect, useState } from "react";
import {
  createParaclinicalResult,
  getPatientParaclinicalResults,
  type CreateLabResultItemPayload,
  type CreateParaclinicalResultPayload,
  type LabResultItem,
  type ParaclinicalResult,
} from "@/lib/api";
import {
  paraclinicalResultTypeOptions as resultTypeOptions,
  selectPlaceholder,
  sensitivityLevelOptions,
  sourceTypeOptions,
  verificationStatusOptions,
  type HealthOption,
} from "@/lib/health-options";

const timelineRefreshEventName = "zibzie:timeline-refresh";

const emptyLabItem: CreateLabResultItemPayload = {
  testName: "",
  value: "",
  numericValue: "",
  unit: "",
  referenceRange: "",
  isAbnormal: "",
  interpretation: "",
};

const emptyForm: CreateParaclinicalResultPayload = {
  resultType: "Lab",
  title: "",
  description: "",
  performedAt: "",
  resultDate: "",
  providerName: "",
  linkedDocumentId: "",
  summary: "",
  interpretation: "",
  isAbnormal: "",
  requiresFollowUp: false,
  followUpNote: "",
  sourceType: "Manual",
  verificationStatus: "Unverified",
  sensitivityLevel: "Normal",
  labItems: [],
};

const abnormalOptions = [
  { label: "ثبت نشده", value: "" },
  { label: "بله", value: "true" },
  { label: "خیر", value: "false" },
];

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

function formatBoolean(value: boolean | null) {
  if (value === null) {
    return "ثبت نشده";
  }

  return value ? "بله" : "خیر";
}

function getOptionLabel(options: HealthOption[], value: string | null | undefined) {
  if (!value) {
    return null;
  }

  return options.find((option) => option.value === value)?.label ?? value;
}

function hasFilledLabItem(item: CreateLabResultItemPayload) {
  return Object.values(item).some((value) => value.trim().length > 0);
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
        step={type === "number" ? "any" : undefined}
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
  allowEmpty = false,
}: {
  label: string;
  value: string;
  options: HealthOption[];
  onChange: (value: string) => void;
  allowEmpty?: boolean;
}) {
  return (
    <Field label={label}>
      <select
        className="h-10 rounded-md border border-slate-300 bg-white px-3 text-slate-950 outline-none transition focus:border-teal-600 focus:ring-2 focus:ring-teal-100"
        onChange={(event) => onChange(event.target.value)}
        value={value}
      >
        {allowEmpty ? <option value="">{selectPlaceholder}</option> : null}
        {options.map((option) => (
          <option key={option.value} value={option.value}>
            {option.label}
          </option>
        ))}
      </select>
    </Field>
  );
}

function BooleanSelect({
  label,
  value,
  onChange,
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
}) {
  return (
    <Field label={label}>
      <select
        className="h-10 rounded-md border border-slate-300 bg-white px-3 text-slate-950 outline-none transition focus:border-teal-600 focus:ring-2 focus:ring-teal-100"
        onChange={(event) => onChange(event.target.value)}
        value={value}
      >
        {abnormalOptions.map((option) => (
          <option key={option.value} value={option.value}>
            {option.label}
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

function LabItemCard({ item }: { item: LabResultItem }) {
  return (
    <article className="rounded-md border border-slate-200 bg-slate-50 p-3">
      <h4 className="font-semibold text-slate-950">{item.testName}</h4>
      <dl className="mt-3 grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
        {item.value ? <MetaItem label="مقدار" value={item.value} /> : null}
        {item.numericValue !== null ? (
          <MetaItem label="مقدار عددی" value={item.numericValue} />
        ) : null}
        {item.unit ? <MetaItem label="واحد" value={item.unit} /> : null}
        {item.referenceRange ? (
          <MetaItem label="محدوده مرجع" value={item.referenceRange} />
        ) : null}
        {item.isAbnormal !== null ? (
          <MetaItem label="غیرطبیعی" value={formatBoolean(item.isAbnormal)} />
        ) : null}
      </dl>
      {item.interpretation ? (
        <p className="mt-3 text-sm leading-7 text-slate-600">
          تفسیر: {item.interpretation}
        </p>
      ) : null}
    </article>
  );
}

function ParaclinicalResultCard({ result }: { result: ParaclinicalResult }) {
  const [isExpanded, setIsExpanded] = useState(false);
  const [showLabItems, setShowLabItems] = useState(false);
  const performedAt = formatDateTime(result.performedAt);
  const resultDate = formatDateTime(result.resultDate);
  const createdAt = formatDateTime(result.createdAt);
  const updatedAt = formatDateTime(result.updatedAt);
  const resultTypeLabel = getOptionLabel(resultTypeOptions, result.resultType);
  const sourceTypeLabel = getOptionLabel(sourceTypeOptions, result.sourceType);
  const verificationStatusLabel = getOptionLabel(
    verificationStatusOptions,
    result.verificationStatus,
  );
  const sensitivityLevelLabel = getOptionLabel(
    sensitivityLevelOptions,
    result.sensitivityLevel,
  );
  const dateParts = [
    resultDate ? `نتیجه: ${resultDate}` : null,
    performedAt ? `انجام: ${performedAt}` : null,
    result.providerName ? `مرکز: ${result.providerName}` : null,
  ].filter(Boolean);

  return (
    <article className="rounded-md border border-slate-200 bg-white p-4">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
        <div>
          <div className="flex flex-wrap items-center gap-2">
            <h3 className="text-base font-bold text-slate-950">
              {result.title}
            </h3>
            {resultTypeLabel ? (
              <span className="rounded-md bg-teal-50 px-2.5 py-1 text-xs font-semibold text-teal-800">
                {resultTypeLabel}
              </span>
            ) : null}
            {result.isAbnormal ? (
              <span className="rounded-md bg-rose-50 px-2.5 py-1 text-xs font-semibold text-rose-700">
                غیرطبیعی
              </span>
            ) : null}
            {result.requiresFollowUp ? (
              <span className="rounded-md bg-amber-50 px-2.5 py-1 text-xs font-semibold text-amber-700">
                نیازمند پیگیری
              </span>
            ) : null}
          </div>
          {dateParts.length > 0 ? (
            <p className="mt-2 text-sm leading-7 text-slate-600">
              {dateParts.join(" · ")}
            </p>
          ) : null}
          {result.summary ? (
            <p className="mt-2 text-sm leading-7 text-slate-600">
              {result.summary}
            </p>
          ) : null}
        </div>
        <div className="flex flex-wrap items-center gap-2">
          {result.labItems.length > 0 ? (
            <button
              className="inline-flex h-9 items-center justify-center rounded-md border border-slate-200 px-3 text-xs font-semibold text-slate-700 transition hover:bg-slate-50"
              onClick={() => setShowLabItems((current) => !current)}
              type="button"
            >
              {showLabItems
                ? "بستن آیتم‌های آزمایش"
                : `مشاهده آیتم‌های آزمایش (${new Intl.NumberFormat("fa-IR").format(
                    result.labItems.length,
                  )})`}
            </button>
          ) : null}
          <button
            className="inline-flex h-9 items-center justify-center rounded-md border border-slate-200 px-3 text-xs font-semibold text-slate-700 transition hover:bg-slate-50"
            onClick={() => setIsExpanded((current) => !current)}
            type="button"
          >
            {isExpanded ? "بستن جزئیات" : "جزئیات"}
          </button>
        </div>
      </div>

      {isExpanded ? (
        <dl className="mt-4 grid gap-3 rounded-md border border-slate-100 bg-slate-50 p-3 sm:grid-cols-2 lg:grid-cols-3">
          {result.description ? (
            <MetaItem label="توضیحات" value={result.description} />
          ) : null}
          {result.summary ? (
            <MetaItem label="خلاصه نتیجه" value={result.summary} />
          ) : null}
          {result.interpretation ? (
            <MetaItem label="تفسیر" value={result.interpretation} />
          ) : null}
          {result.followUpNote ? (
            <MetaItem label="توضیح پیگیری" value={result.followUpNote} />
          ) : null}
          <MetaItem label="نوع نتیجه" value={resultTypeLabel} />
          {resultTypeLabel !== result.resultType ? (
            <MetaItem label="مقدار خام نوع نتیجه" value={result.resultType} />
          ) : null}
          {performedAt ? <MetaItem label="تاریخ انجام" value={performedAt} /> : null}
          {resultDate ? <MetaItem label="تاریخ نتیجه" value={resultDate} /> : null}
          {result.providerName ? (
            <MetaItem label="مرکز / ارائه‌دهنده" value={result.providerName} />
          ) : null}
          {result.isAbnormal !== null ? (
            <MetaItem label="غیرطبیعی" value={formatBoolean(result.isAbnormal)} />
          ) : null}
          <MetaItem
            label="نیازمند پیگیری"
            value={formatBoolean(result.requiresFollowUp)}
          />
          {result.linkedDocumentId ? (
            <MetaItem
              label="شناسه مدرک مرتبط"
              value={<span dir="ltr">{result.linkedDocumentId}</span>}
            />
          ) : null}
          <MetaItem label="منبع" value={sourceTypeLabel} />
          <MetaItem label="وضعیت تأیید" value={verificationStatusLabel} />
          <MetaItem label="سطح حساسیت" value={sensitivityLevelLabel} />
          {createdAt ? <MetaItem label="زمان ایجاد" value={createdAt} /> : null}
          {updatedAt ? (
            <MetaItem label="آخرین به‌روزرسانی" value={updatedAt} />
          ) : null}
        </dl>
      ) : null}

      {showLabItems && result.labItems.length > 0 ? (
        <div className="mt-4 border-t border-slate-100 pt-4">
          <h4 className="text-sm font-bold text-slate-950">آیتم‌های آزمایش</h4>
          <div className="mt-3 space-y-3">
            {result.labItems.map((item) => (
              <LabItemCard item={item} key={item.id} />
            ))}
          </div>
        </div>
      ) : null}
    </article>
  );
}

function LabItemRow({
  item,
  index,
  onChange,
  onRemove,
}: {
  item: CreateLabResultItemPayload;
  index: number;
  onChange: (
    index: number,
    key: keyof CreateLabResultItemPayload,
    value: string,
  ) => void;
  onRemove: (index: number) => void;
}) {
  return (
    <div className="rounded-md border border-slate-200 bg-white p-3">
      <div className="flex items-center justify-between gap-3">
        <h4 className="text-sm font-bold text-slate-950">
          آیتم آزمایش {index + 1}
        </h4>
        <button
          className="inline-flex h-9 items-center justify-center rounded-md border border-rose-200 px-3 text-xs font-semibold text-rose-700 transition hover:bg-rose-50"
          onClick={() => onRemove(index)}
          type="button"
        >
          حذف
        </button>
      </div>

      <div className="mt-3 grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
        <TextInput
          label="نام تست"
          onChange={(value) => onChange(index, "testName", value)}
          value={item.testName}
        />
        <TextInput
          dir="ltr"
          label="مقدار"
          onChange={(value) => onChange(index, "value", value)}
          value={item.value}
        />
        <TextInput
          dir="ltr"
          label="مقدار عددی"
          onChange={(value) => onChange(index, "numericValue", value)}
          type="number"
          value={item.numericValue}
        />
        <TextInput
          dir="ltr"
          label="واحد"
          onChange={(value) => onChange(index, "unit", value)}
          value={item.unit}
        />
        <TextInput
          dir="ltr"
          label="محدوده مرجع"
          onChange={(value) => onChange(index, "referenceRange", value)}
          value={item.referenceRange}
        />
        <BooleanSelect
          label="غیرطبیعی"
          onChange={(value) => onChange(index, "isAbnormal", value)}
          value={item.isAbnormal}
        />
      </div>

      <Field label="تفسیر">
        <textarea
          className="mt-1 min-h-16 resize-y rounded-md border border-slate-300 bg-white px-3 py-2 text-slate-950 outline-none transition placeholder:text-slate-400 focus:border-teal-600 focus:ring-2 focus:ring-teal-100"
          onChange={(event) =>
            onChange(index, "interpretation", event.target.value)
          }
          value={item.interpretation}
        />
      </Field>
    </div>
  );
}

function ParaclinicalResultCreateForm({
  patientId,
  onCreated,
}: {
  patientId: string;
  onCreated: () => Promise<void>;
}) {
  const router = useRouter();
  const [form, setForm] = useState<CreateParaclinicalResultPayload>(emptyForm);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  function updateForm<K extends keyof CreateParaclinicalResultPayload>(
    key: K,
    value: CreateParaclinicalResultPayload[K],
  ) {
    setForm((current) => ({
      ...current,
      [key]: value,
    }));
  }

  function addLabItem() {
    setForm((current) => ({
      ...current,
      labItems: [...current.labItems, { ...emptyLabItem }],
    }));
  }

  function removeLabItem(index: number) {
    setForm((current) => ({
      ...current,
      labItems: current.labItems.filter((_, itemIndex) => itemIndex !== index),
    }));
  }

  function updateLabItem(
    index: number,
    key: keyof CreateLabResultItemPayload,
    value: string,
  ) {
    setForm((current) => ({
      ...current,
      labItems: current.labItems.map((item, itemIndex) =>
        itemIndex === index
          ? {
              ...item,
              [key]: value,
            }
          : item,
      ),
    }));
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setErrorMessage(null);
    setSuccessMessage(null);

    if (!form.resultType.trim() || !form.title.trim()) {
      setErrorMessage("نوع نتیجه و عنوان نتیجه الزامی هستند.");
      return;
    }

    const filledLabItems = form.labItems.filter(hasFilledLabItem);

    if (filledLabItems.some((item) => !item.testName.trim())) {
      setErrorMessage("برای هر آیتم آزمایش تکمیل‌شده، نام تست الزامی است.");
      return;
    }

    setIsSubmitting(true);

    try {
      await createParaclinicalResult(patientId, {
        ...form,
        labItems: filledLabItems,
      });
      setForm(emptyForm);
      setSuccessMessage("نتیجه پاراکلینیک با موفقیت ثبت شد.");
      await onCreated();
      window.dispatchEvent(new Event(timelineRefreshEventName));
      router.refresh();
    } catch (error) {
      setErrorMessage(
        error instanceof Error
          ? `خطا در ثبت نتیجه پاراکلینیک: ${error.message}`
          : "خطا در ثبت نتیجه پاراکلینیک رخ داد.",
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
        ثبت نتیجه پاراکلینیک جدید
      </h3>
      <div className="mt-3">
        <Notice error={errorMessage} success={successMessage} />
      </div>

      <div className="mt-4 grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
        <SelectInput
          label="نوع نتیجه"
          onChange={(value) => updateForm("resultType", value)}
          options={resultTypeOptions}
          value={form.resultType}
        />
        <TextInput
          label="عنوان نتیجه"
          onChange={(value) => updateForm("title", value)}
          required
          value={form.title}
        />
        <TextInput
          label="تاریخ انجام"
          onChange={(value) => updateForm("performedAt", value)}
          type="date"
          value={form.performedAt}
        />
        <TextInput
          label="تاریخ نتیجه"
          onChange={(value) => updateForm("resultDate", value)}
          type="date"
          value={form.resultDate}
        />
        <TextInput
          label="مرکز / ارائه‌دهنده"
          onChange={(value) => updateForm("providerName", value)}
          value={form.providerName}
        />
        <TextInput
          dir="ltr"
          label="شناسه مدرک مرتبط"
          onChange={(value) => updateForm("linkedDocumentId", value)}
          value={form.linkedDocumentId}
        />
        <BooleanSelect
          label="غیرطبیعی"
          onChange={(value) => updateForm("isAbnormal", value)}
          value={form.isAbnormal}
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
        <Field label="خلاصه نتیجه">
          <textarea
            className="min-h-20 resize-y rounded-md border border-slate-300 bg-white px-3 py-2 text-slate-950 outline-none transition placeholder:text-slate-400 focus:border-teal-600 focus:ring-2 focus:ring-teal-100"
            onChange={(event) => updateForm("summary", event.target.value)}
            value={form.summary}
          />
        </Field>
        <Field label="تفسیر">
          <textarea
            className="min-h-20 resize-y rounded-md border border-slate-300 bg-white px-3 py-2 text-slate-950 outline-none transition placeholder:text-slate-400 focus:border-teal-600 focus:ring-2 focus:ring-teal-100"
            onChange={(event) =>
              updateForm("interpretation", event.target.value)
            }
            value={form.interpretation}
          />
        </Field>
        <Field label="توضیح پیگیری">
          <textarea
            className="min-h-20 resize-y rounded-md border border-slate-300 bg-white px-3 py-2 text-slate-950 outline-none transition placeholder:text-slate-400 focus:border-teal-600 focus:ring-2 focus:ring-teal-100"
            onChange={(event) => updateForm("followUpNote", event.target.value)}
            value={form.followUpNote}
          />
        </Field>
      </div>

      <label className="mt-4 flex items-center gap-2 text-sm font-medium text-slate-700">
        <input
          checked={form.requiresFollowUp}
          className="h-4 w-4 rounded border-slate-300 text-teal-700 focus:ring-teal-600"
          onChange={(event) =>
            updateForm("requiresFollowUp", event.target.checked)
          }
          type="checkbox"
        />
        نیازمند پیگیری
      </label>

      <div className="mt-5 border-t border-slate-200 pt-4">
        <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
          <div>
            <h4 className="font-bold text-slate-950">آیتم‌های آزمایش</h4>
            <p className="mt-1 text-sm leading-7 text-slate-600">
              برای نتایج آزمایشگاهی می‌توانید ردیف‌های ساختاریافته اضافه کنید.
            </p>
          </div>
          <button
            className="inline-flex h-10 items-center justify-center rounded-md border border-slate-300 px-4 text-sm font-semibold text-slate-700 transition hover:bg-white"
            onClick={addLabItem}
            type="button"
          >
            افزودن آیتم آزمایش
          </button>
        </div>

        {form.labItems.length > 0 ? (
          <div className="mt-4 space-y-3">
            {form.labItems.map((item, index) => (
              <LabItemRow
                index={index}
                item={item}
                key={index}
                onChange={updateLabItem}
                onRemove={removeLabItem}
              />
            ))}
          </div>
        ) : null}
      </div>

      <button
        className="mt-4 inline-flex h-10 w-full items-center justify-center rounded-md bg-teal-700 px-4 text-sm font-semibold text-white transition hover:bg-teal-800 disabled:cursor-not-allowed disabled:bg-slate-400 sm:w-auto"
        disabled={isSubmitting}
        type="submit"
      >
        {isSubmitting ? "در حال ثبت..." : "ثبت نتیجه پاراکلینیک"}
      </button>
    </form>
  );
}

export default function PatientParaclinicalResults({
  patientId,
}: {
  patientId: string;
}) {
  const [results, setResults] = useState<ParaclinicalResult[]>([]);
  const [isCreateOpen, setIsCreateOpen] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const loadResults = useCallback(async () => {
    setIsLoading(true);
    setErrorMessage(null);

    try {
      const patientResults = await getPatientParaclinicalResults(patientId);
      setResults(patientResults);
    } catch (error) {
      setErrorMessage(
        error instanceof Error
          ? error.message
          : "دریافت نتایج پاراکلینیک بیمار با خطا روبه‌رو شد.",
      );
    } finally {
      setIsLoading(false);
    }
  }, [patientId]);

  useEffect(() => {
    const timer = window.setTimeout(() => {
      void loadResults();
    }, 0);

    return () => window.clearTimeout(timer);
  }, [loadResults]);

  return (
    <section className="rounded-lg border border-slate-200 bg-white p-4 shadow-sm">
      <div className="flex flex-col gap-2 border-b border-slate-100 pb-4 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <h2 className="text-xl font-bold text-slate-950">
            نتایج پاراکلینیک
          </h2>
          <p className="mt-2 text-sm leading-7 text-slate-600">
            آزمایش‌ها، تصویربرداری‌ها، پاتولوژی و سایر شواهد پاراکلینیک بیمار.
          </p>
        </div>
        <div className="flex flex-wrap gap-2">
          <button
            className="inline-flex h-10 items-center justify-center rounded-md border border-slate-300 px-4 text-sm font-semibold text-slate-700 transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:text-slate-400"
            disabled={isLoading}
            onClick={() => {
              void loadResults();
            }}
            type="button"
          >
            به‌روزرسانی
          </button>
          <button
            className="inline-flex h-10 items-center justify-center rounded-md bg-teal-700 px-4 text-sm font-semibold text-white transition hover:bg-teal-800"
            onClick={() => setIsCreateOpen((current) => !current)}
            type="button"
          >
            {isCreateOpen ? "بستن فرم" : "ثبت نتیجه پاراکلینیک"}
          </button>
        </div>
      </div>

      <div className="mt-5 space-y-3">
        {isLoading ? (
          <div className="rounded-md border border-slate-200 bg-slate-50 p-4 text-sm text-slate-600">
            در حال دریافت نتایج پاراکلینیک...
          </div>
        ) : null}

        {!isLoading && errorMessage ? (
          <div className="rounded-md border border-rose-200 bg-rose-50 p-4 text-sm leading-7 text-rose-900">
            {errorMessage}
          </div>
        ) : null}

        {!isLoading && !errorMessage && results.length === 0 ? (
          <div className="rounded-md border border-dashed border-slate-300 bg-slate-50 p-4 text-sm leading-7 text-slate-600">
            هنوز نتیجه‌ای ثبت نشده است.
          </div>
        ) : null}

        {!isLoading && !errorMessage
          ? results.map((result) => (
              <ParaclinicalResultCard key={result.id} result={result} />
            ))
          : null}
      </div>

      {isCreateOpen ? (
        <div className="mt-5">
          <ParaclinicalResultCreateForm
            onCreated={loadResults}
            patientId={patientId}
          />
        </div>
      ) : null}
    </section>
  );
}
