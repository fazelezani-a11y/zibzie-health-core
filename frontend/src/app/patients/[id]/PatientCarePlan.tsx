"use client";

import { useRouter } from "next/navigation";
import type { FormEvent, ReactNode } from "react";
import { useCallback, useEffect, useMemo, useState } from "react";
import {
  createCarePlanItem,
  getPatientCarePlan,
  type CarePlanItem,
  type CreateCarePlanItemPayload,
} from "@/lib/api";
import {
  carePlanCategoryOptions as categoryOptions,
  carePlanItemTypeOptions as itemTypeOptions,
  carePlanStatusOptions as statusOptions,
  priorityOptions,
  selectPlaceholder,
  sensitivityLevelOptions,
  sourceTypeOptions,
  verificationStatusOptions,
  type HealthOption,
} from "@/lib/health-options";

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

type CarePlanFilterId =
  | "all"
  | "needs-action"
  | "in-progress"
  | "screening"
  | "treatment"
  | "care-rehab"
  | "lifestyle"
  | "referral-paraclinical"
  | "follow-up"
  | "near-future"
  | "overdue"
  | "completed";

type FilterDefinition = {
  id: CarePlanFilterId;
  label: string;
};

const primaryFilterDefinitions: FilterDefinition[] = [
  { id: "all", label: "همه" },
  { id: "needs-action", label: "نیازمند اقدام" },
  { id: "near-future", label: "آینده نزدیک" },
  { id: "in-progress", label: "در جریان" },
  { id: "completed", label: "تکمیل‌شده" },
];

const advancedFilterDefinitions: FilterDefinition[] = [
  { id: "screening", label: "غربالگری" },
  { id: "treatment", label: "درمانی" },
  { id: "care-rehab", label: "مراقبتی/توانبخشی" },
  { id: "lifestyle", label: "سبک زندگی" },
  { id: "referral-paraclinical", label: "ارجاع/پاراکلینیک" },
  { id: "follow-up", label: "نیازمند پیگیری" },
  { id: "overdue", label: "سررسید گذشته" },
];

const filterDefinitions = [
  ...primaryFilterDefinitions,
  ...advancedFilterDefinitions,
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

function formatCount(value: number) {
  return new Intl.NumberFormat("fa-IR").format(value);
}

function getOptionLabel(options: HealthOption[], value: string | null | undefined) {
  if (!value) {
    return "ثبت نشده";
  }

  return options.find((option) => option.value === value)?.label ?? value;
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

function TinyBadge({
  children,
  tone = "slate",
}: {
  children: ReactNode;
  tone?: "slate" | "teal" | "rose" | "amber" | "emerald" | "sky";
}) {
  const toneClass = {
    slate: "bg-slate-100 text-slate-700",
    teal: "bg-teal-50 text-teal-800",
    rose: "bg-rose-50 text-rose-800",
    amber: "bg-amber-50 text-amber-800",
    emerald: "bg-emerald-50 text-emerald-800",
    sky: "bg-sky-50 text-sky-800",
  }[tone];

  return (
    <span className={`rounded-md px-2.5 py-1 text-xs font-semibold ${toneClass}`}>
      {children}
    </span>
  );
}

function isCompleted(item: CarePlanItem) {
  return item.status === "Completed";
}

function isOverdue(item: CarePlanItem) {
  if (!item.dueAt || isCompleted(item) || item.status === "Cancelled") {
    return false;
  }

  const dueAt = new Date(item.dueAt);

  return !Number.isNaN(dueAt.getTime()) && dueAt.getTime() < Date.now();
}

function isNearFuture(item: CarePlanItem) {
  if (!item.dueAt || isCompleted(item) || item.status === "Cancelled") {
    return false;
  }

  const dueAt = new Date(item.dueAt);

  if (Number.isNaN(dueAt.getTime())) {
    return false;
  }

  const now = Date.now();
  const sevenDaysFromNow = now + 7 * 24 * 60 * 60 * 1000;

  return dueAt.getTime() >= now && dueAt.getTime() <= sevenDaysFromNow;
}

function isLifestyleItem(item: CarePlanItem) {
  return (
    item.category === "Lifestyle" ||
    ["Nutrition", "Exercise", "MentalHealth"].includes(item.itemType)
  );
}

function matchesFilter(item: CarePlanItem, filter: CarePlanFilterId) {
  switch (filter) {
    case "needs-action":
      return (
        !isCompleted(item) &&
        item.status !== "Cancelled" &&
        (isOverdue(item) || Boolean(item.dueAt) || Boolean(item.nextAction))
      );
    case "in-progress":
      return ["InProgress", "Scheduled", "Planned"].includes(item.status);
    case "screening":
      return item.category === "Diagnostic" || item.itemType === "LabTest";
    case "treatment":
      return item.category === "Treatment" || item.itemType === "MedicationChange";
    case "care-rehab":
      return item.category === "Care" || item.itemType === "HomeCare";
    case "lifestyle":
      return isLifestyleItem(item);
    case "referral-paraclinical":
      return (
        item.category === "Referral" ||
        ["Imaging", "SpecialistVisit"].includes(item.itemType)
      );
    case "follow-up":
      return item.category === "FollowUp" || Boolean(item.nextAction || item.dueAt);
    case "near-future":
      return isNearFuture(item);
    case "overdue":
      return isOverdue(item);
    case "completed":
      return isCompleted(item);
    case "all":
    default:
      return true;
  }
}

function sortCarePlanItems(items: CarePlanItem[]) {
  return [...items].sort((first, second) => {
    if (isCompleted(first) !== isCompleted(second)) {
      return isCompleted(first) ? 1 : -1;
    }

    if (isOverdue(first) !== isOverdue(second)) {
      return isOverdue(first) ? -1 : 1;
    }

    const firstDate = first.dueAt ?? first.plannedAt ?? first.createdAt;
    const secondDate = second.dueAt ?? second.plannedAt ?? second.createdAt;

    return new Date(firstDate).getTime() - new Date(secondDate).getTime();
  });
}

function CarePlanItemRow({
  item,
}: {
  item: CarePlanItem;
}) {
  const [isExpanded, setIsExpanded] = useState(false);
  const plannedAt = formatDateTime(item.plannedAt);
  const dueAt = formatDateTime(item.dueAt);
  const completedAt = formatDateTime(item.completedAt);
  const overdue = isOverdue(item);
  const completed = isCompleted(item);
  const inactive = item.status === "Cancelled" || item.status === "Deferred";
  const highPriority = item.priority === "High" || item.priority === "Urgent";
  const dateLabel = dueAt
    ? `موعد: ${dueAt}`
    : plannedAt
      ? `برنامه: ${plannedAt}`
      : "بدون موعد";

  return (
    <article
      className={`rounded-md border bg-white p-3 transition ${
        completed
          ? "border-slate-200 opacity-75"
          : overdue
            ? "border-rose-200 bg-rose-50/40"
            : "border-slate-200"
      }`}
    >
      <div
        className={`mb-3 h-1 rounded-full ${
          completed
            ? "bg-emerald-200"
            : inactive
              ? "bg-slate-200"
              : overdue
                ? "bg-rose-300"
                : isNearFuture(item)
                  ? "bg-amber-200"
                  : "bg-teal-200"
        }`}
      />
      <div className="flex flex-col gap-3 xl:flex-row xl:items-start xl:justify-between">
        <div className="min-w-0">
          <div className="flex flex-wrap items-center gap-2">
            <h3 className="text-base font-bold text-slate-950">{item.title}</h3>
            {overdue ? <TinyBadge tone="rose">سررسید گذشته</TinyBadge> : null}
            {highPriority ? (
              <TinyBadge tone={item.priority === "Urgent" ? "rose" : "amber"}>
                {getOptionLabel(priorityOptions, item.priority)}
              </TinyBadge>
            ) : null}
          </div>
          <p className="mt-2 text-sm leading-7 text-slate-600">
            {dateLabel} · وضعیت: {getOptionLabel(statusOptions, item.status)}
            {item.nextAction ? ` · اقدام بعدی: ${item.nextAction}` : ""}
          </p>
          {item.dueAt && !completed && !inactive ? (
            <p className="mt-1 text-xs leading-6 text-slate-500">
              یادآور خودکار می‌تواند از موعد این برنامه ساخته شود.
            </p>
          ) : null}
        </div>

        <div className="flex shrink-0 flex-wrap items-center gap-2 text-sm text-slate-600">
          <button
            className="inline-flex h-8 items-center justify-center rounded-md border border-slate-300 px-3 text-xs font-semibold text-slate-700 transition hover:bg-slate-50"
            onClick={() => setIsExpanded((current) => !current)}
            type="button"
          >
            {isExpanded ? "بستن جزئیات" : "جزئیات"}
          </button>
        </div>
      </div>

      {isExpanded ? (
        <div className="mt-4 rounded-md border border-slate-100 bg-slate-50 p-3">
          <dl className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
            <MetaItem
              label="دسته‌بندی"
              value={getOptionLabel(categoryOptions, item.category)}
            />
            <MetaItem
              label="نوع اقدام"
              value={getOptionLabel(itemTypeOptions, item.itemType)}
            />
            <MetaItem
              label="وضعیت"
              value={getOptionLabel(statusOptions, item.status)}
            />
            <MetaItem
              label="اولویت"
              value={getOptionLabel(priorityOptions, item.priority)}
            />
            <MetaItem label="منبع" value={getOptionLabel(sourceTypeOptions, item.sourceType)} />
            <MetaItem
              label="وضعیت تأیید"
              value={getOptionLabel(verificationStatusOptions, item.verificationStatus)}
            />
            <MetaItem
              label="سطح حساسیت"
              value={getOptionLabel(sensitivityLevelOptions, item.sensitivityLevel)}
            />
            {completedAt ? <MetaItem label="زمان تکمیل" value={completedAt} /> : null}
            {item.description ? (
              <MetaItem label="توضیحات" value={item.description} />
            ) : null}
            {item.reason ? <MetaItem label="علت / دلیل" value={item.reason} /> : null}
            {item.requestedBy ? (
              <MetaItem label="درخواست‌کننده" value={item.requestedBy} />
            ) : null}
            {item.assignedTo ? (
              <MetaItem label="مسئول انجام" value={item.assignedTo} />
            ) : null}
            {item.resultSummary ? (
              <MetaItem label="خلاصه نتیجه" value={item.resultSummary} />
            ) : null}
            {item.nextAction ? (
              <MetaItem label="اقدام بعدی" value={item.nextAction} />
            ) : null}
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
        </div>
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
      className="rounded-md border border-slate-200 bg-slate-50 p-4"
      onSubmit={handleSubmit}
    >
      <h3 className="text-base font-bold text-slate-950">
        افزودن آیتم پلن مراقبتی
      </h3>
      <div className="mt-3">
        <Notice error={errorMessage} success={successMessage} />
      </div>

      <div className="mt-4 grid gap-3 sm:grid-cols-2 xl:grid-cols-3">
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
  const [activeFilter, setActiveFilter] = useState<CarePlanFilterId>("all");
  const [showAdvancedFilters, setShowAdvancedFilters] = useState(false);
  const [isCreateOpen, setIsCreateOpen] = useState(false);
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

  const filterCounts = useMemo(() => {
    return filterDefinitions.reduce<Record<CarePlanFilterId, number>>(
      (counts, filter) => ({
        ...counts,
        [filter.id]:
          filter.id === "all"
            ? items.length
            : items.filter((item) => matchesFilter(item, filter.id)).length,
      }),
      {} as Record<CarePlanFilterId, number>,
    );
  }, [items]);

  const visibleItems = useMemo(
    () =>
      sortCarePlanItems(
        items.filter((item) => matchesFilter(item, activeFilter)),
      ),
    [activeFilter, items],
  );
  const activeFilterDefinition = filterDefinitions.find(
    (filter) => filter.id === activeFilter,
  );

  return (
    <section className="rounded-lg border border-slate-200 bg-white p-4 shadow-sm">
      <div className="flex flex-col gap-3 border-b border-slate-100 pb-4 lg:flex-row lg:items-start lg:justify-between">
        <div>
          <p className="max-w-3xl text-sm leading-7 text-slate-600">
            اقدامات تأییدشده، برنامه‌های درمانی، مراقبتی، سبک زندگی، ارجاع‌ها و
            پیگیری‌های پرونده بیمار.
          </p>
          <p className="mt-2 text-xs font-medium text-slate-500">
            {formatCount(items.length)} مورد
          </p>
        </div>
        <div className="flex flex-wrap gap-2">
          <button
            className="inline-flex h-10 items-center justify-center rounded-md bg-teal-700 px-4 text-sm font-semibold text-white transition hover:bg-teal-800"
            onClick={() => setIsCreateOpen((current) => !current)}
            type="button"
          >
            {isCreateOpen ? "بستن فرم" : "افزودن آیتم پلن"}
          </button>
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
      </div>

      {isCreateOpen ? (
        <div className="mt-4">
          <CarePlanCreateForm onCreated={loadCarePlan} patientId={patientId} />
        </div>
      ) : null}

      <div className="mt-4 flex flex-wrap items-center gap-2 text-xs">
        <button
          className="rounded-md border border-slate-200 bg-white px-3 py-2 font-semibold text-slate-700 transition hover:bg-slate-50"
          onClick={() => setShowAdvancedFilters((current) => !current)}
          type="button"
        >
          {showAdvancedFilters ? "بستن فیلتر پیشرفته" : "فیلتر پیشرفته"}
        </button>
        {activeFilter !== "all" ? (
          <>
            <span className="font-medium text-slate-500">
              فیلتر فعال است: {activeFilterDefinition?.label}
            </span>
            <button
              className="font-semibold text-teal-700 transition hover:text-teal-900"
              onClick={() => setActiveFilter("all")}
              type="button"
            >
              حذف فیلتر
            </button>
          </>
        ) : null}
      </div>

      {showAdvancedFilters ? (
        <div className="mt-2 flex flex-wrap gap-2 rounded-md border border-slate-200 bg-slate-50 p-2">
          {filterDefinitions.map((filter) => {
            const isActive = filter.id === activeFilter;

            return (
              <button
                className={`rounded-md border px-3 py-2 text-xs font-semibold transition ${
                  isActive
                    ? "border-teal-700 bg-teal-700 text-white"
                    : "border-slate-200 bg-white text-slate-700 hover:bg-slate-50"
                }`}
                key={filter.id}
                onClick={() => setActiveFilter(filter.id)}
                type="button"
              >
                {filter.label}
                <span
                  className={`mr-2 rounded-md px-1.5 py-0.5 ${
                    isActive ? "bg-teal-600 text-white" : "bg-slate-100 text-slate-500"
                  }`}
                >
                  {formatCount(filterCounts[filter.id] ?? 0)}
                </span>
              </button>
            );
          })}
        </div>
      ) : null}

      <div className="mt-4 space-y-3">
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
          <div className="rounded-md border border-dashed border-slate-300 bg-slate-50 p-5 text-sm leading-7 text-slate-600">
            هنوز آیتمی در پلن مراقبتی ثبت نشده است. اقدامات درمانی، مراقبتی،
            سبک زندگی، ارجاع‌ها و پیگیری‌های تأییدشده اینجا نمایش داده می‌شوند.
          </div>
        ) : null}

        {!isLoading &&
        !errorMessage &&
        items.length > 0 &&
        visibleItems.length === 0 ? (
          <div className="rounded-md border border-dashed border-slate-300 bg-slate-50 p-4 text-sm leading-7 text-slate-600">
            آیتمی برای این فیلتر پیدا نشد.
          </div>
        ) : null}

        {!isLoading && !errorMessage
          ? visibleItems.map((item) => (
              <CarePlanItemRow item={item} key={item.id} />
            ))
          : null}
      </div>
    </section>
  );
}
