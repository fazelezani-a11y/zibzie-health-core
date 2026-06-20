"use client";

import { useRouter } from "next/navigation";
import type { FormEvent } from "react";
import { useCallback, useEffect, useMemo, useState } from "react";
import Badge from "@/components/ui/Badge";
import FormField from "@/components/ui/FormField";
import MetaItem from "@/components/ui/MetaItem";
import Notice from "@/components/ui/Notice";
import {
  createPatientReminder,
  getPatientReminders,
  type CreatePatientReminderPayload,
  type PatientReminder,
} from "@/lib/api";
import { formatDateTime } from "@/lib/format";
import {
  audienceOptions,
  channelOptions,
  priorityOptions,
  reminderStatusOptions as statusOptions,
  reminderTypeOptions,
  selectPlaceholder,
  sensitivityLevelOptions,
  sourceTypeOptions,
  type HealthOption,
} from "@/lib/health-options";

const timelineRefreshEventName = "zibzie:timeline-refresh";

const emptyForm: CreatePatientReminderPayload = {
  reminderType: "CarePlan",
  title: "",
  description: "",
  dueAt: "",
  status: "Pending",
  priority: "Normal",
  audience: "Internal",
  channel: "InApp",
  relatedRecordType: "",
  relatedRecordId: "",
  sourceType: "Manual",
  sensitivityLevel: "Normal",
};

type ReminderFilterId =
  | "all"
  | "needs-action"
  | "urgent"
  | "overdue"
  | "today"
  | "upcoming"
  | "automatic"
  | "manual"
  | "carePlan"
  | "done"
  | "inactive";

const primaryReminderFilters: Array<{ id: ReminderFilterId; label: string }> = [
  { id: "all", label: "همه" },
  { id: "needs-action", label: "نیازمند اقدام" },
  { id: "today", label: "امروز" },
  { id: "upcoming", label: "آینده نزدیک" },
  { id: "done", label: "انجام‌شده" },
];

const advancedReminderFilters: Array<{ id: ReminderFilterId; label: string }> = [
  { id: "urgent", label: "فوری / اولویت بالا" },
  { id: "overdue", label: "سررسید گذشته" },
  { id: "automatic", label: "خودکار" },
  { id: "manual", label: "دستی" },
  { id: "carePlan", label: "ساخته‌شده از پلن مراقبتی" },
  { id: "inactive", label: "لغوشده / غیرفعال" },
];

const reminderFilters = [...primaryReminderFilters, ...advancedReminderFilters];

const relatedRecordTypeLabels: HealthOption[] = [
  { value: "CarePlanItem", label: "پلن مراقبتی" },
  { value: "PatientReminder", label: "یادآور" },
  { value: "PatientMeasurement", label: "شاخص سلامت" },
  { value: "PatientDocument", label: "مدرک پزشکی" },
  { value: "PatientParaclinicalResult", label: "نتیجه پاراکلینیک" },
  { value: "Condition", label: "بیماری / مشکل فعال" },
  { value: "Medication", label: "دارو" },
  { value: "Allergy", label: "حساسیت" },
];

function formatCount(value: number) {
  return new Intl.NumberFormat("fa-IR").format(value);
}

function getOptionLabel(options: HealthOption[], value: string | null | undefined) {
  if (!value?.trim()) {
    return "ثبت نشده";
  }

  return options.find((option) => option.value === value)?.label ?? value;
}

function parseDate(value: string | null | undefined) {
  if (!value) {
    return null;
  }

  const date = new Date(value);

  return Number.isNaN(date.getTime()) ? null : date;
}

function startOfToday() {
  const today = new Date();
  today.setHours(0, 0, 0, 0);
  return today;
}

function endOfToday() {
  const today = new Date();
  today.setHours(23, 59, 59, 999);
  return today;
}

function isDone(reminder: PatientReminder) {
  return ["Done", "Completed"].includes(reminder.status);
}

function isInactive(reminder: PatientReminder) {
  return ["Cancelled", "Snoozed", "Missed"].includes(reminder.status);
}

function isOpenReminder(reminder: PatientReminder) {
  return !isDone(reminder) && !isInactive(reminder);
}

function isOverduePending(reminder: PatientReminder) {
  const dueAt = parseDate(reminder.dueAt);

  return Boolean(dueAt && isOpenReminder(reminder) && dueAt.getTime() < Date.now());
}

function isDueToday(reminder: PatientReminder) {
  const dueAt = parseDate(reminder.dueAt);

  return Boolean(
    dueAt &&
      isOpenReminder(reminder) &&
      dueAt.getTime() >= startOfToday().getTime() &&
      dueAt.getTime() <= endOfToday().getTime(),
  );
}

function isUpcomingSoon(reminder: PatientReminder) {
  const dueAt = parseDate(reminder.dueAt);
  const now = Date.now();
  const sevenDaysFromNow = now + 7 * 24 * 60 * 60 * 1000;

  return Boolean(
    dueAt &&
      isOpenReminder(reminder) &&
      dueAt.getTime() > now &&
      dueAt.getTime() <= sevenDaysFromNow,
  );
}

function isHighPriority(reminder: PatientReminder) {
  return reminder.priority === "High" || reminder.priority === "Urgent";
}

function isSystemGenerated(reminder: PatientReminder) {
  return reminder.sourceType === "System";
}

function isManualReminder(reminder: PatientReminder) {
  return reminder.sourceType !== "System";
}

function isGeneratedFromCarePlan(reminder: PatientReminder) {
  return reminder.relatedRecordType === "CarePlanItem";
}

function reminderMatchesFilter(
  reminder: PatientReminder,
  filter: ReminderFilterId,
) {
  switch (filter) {
    case "all":
      return true;
    case "needs-action":
      return isOpenReminder(reminder) && (isOverduePending(reminder) || isHighPriority(reminder));
    case "urgent":
      return isHighPriority(reminder);
    case "overdue":
      return isOverduePending(reminder);
    case "today":
      return isDueToday(reminder);
    case "upcoming":
      return isUpcomingSoon(reminder);
    case "automatic":
      return isSystemGenerated(reminder);
    case "manual":
      return isManualReminder(reminder);
    case "carePlan":
      return isGeneratedFromCarePlan(reminder);
    case "done":
      return isDone(reminder);
    case "inactive":
      return isInactive(reminder);
  }
}

function sortReminders(reminders: PatientReminder[]) {
  return [...reminders].sort((first, second) => {
    const firstDone = isDone(first) || isInactive(first) ? 1 : 0;
    const secondDone = isDone(second) || isInactive(second) ? 1 : 0;

    if (firstDone !== secondDone) {
      return firstDone - secondDone;
    }

    const firstOverdue = isOverduePending(first) ? 1 : 0;
    const secondOverdue = isOverduePending(second) ? 1 : 0;

    if (firstOverdue !== secondOverdue) {
      return secondOverdue - firstOverdue;
    }

    const priorityWeight: Record<string, number> = {
      Urgent: 4,
      High: 3,
      Normal: 2,
      Low: 1,
    };

    const priorityDiff =
      (priorityWeight[second.priority] ?? 0) -
      (priorityWeight[first.priority] ?? 0);

    if (priorityDiff !== 0) {
      return priorityDiff;
    }

    const firstDueAt = parseDate(first.dueAt)?.getTime() ?? Infinity;
    const secondDueAt = parseDate(second.dueAt)?.getTime() ?? Infinity;

    return firstDueAt - secondDueAt;
  });
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
    <FormField label={label}>
      <input
        className="h-10 rounded-md border border-slate-300 bg-white px-3 text-slate-950 outline-none transition placeholder:text-slate-400 focus:border-teal-600 focus:ring-2 focus:ring-teal-100"
        dir={dir}
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
  allowEmpty = false,
}: {
  label: string;
  value: string;
  options: HealthOption[];
  onChange: (value: string) => void;
  allowEmpty?: boolean;
}) {
  return (
    <FormField label={label}>
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
  if (error) {
    return <Notice variant="error">{error}</Notice>;
  }

  if (success) {
    return <Notice variant="success">{success}</Notice>;
  }

  return null;
}

function ReminderCard({ reminder }: { reminder: PatientReminder }) {
  const [isExpanded, setIsExpanded] = useState(false);
  const dueAt = formatDateTime(reminder.dueAt);
  const completedAt = formatDateTime(reminder.completedAt);
  const createdAt = formatDateTime(reminder.createdAt);
  const updatedAt = formatDateTime(reminder.updatedAt);
  const overdue = isOverduePending(reminder);
  const done = isDone(reminder);
  const inactive = isInactive(reminder);
  const systemGenerated = isSystemGenerated(reminder);
  const generatedFromCarePlan = isGeneratedFromCarePlan(reminder);
  const quiet = done || inactive;
  const highPriority = isHighPriority(reminder);
  const stateLabel = done
    ? "انجام‌شده"
    : inactive
      ? getOptionLabel(statusOptions, reminder.status)
      : overdue
        ? "سررسید گذشته"
        : isDueToday(reminder)
          ? "امروز"
          : isUpcomingSoon(reminder)
            ? "آینده نزدیک"
            : getOptionLabel(statusOptions, reminder.status);
  const stateTone = done
    ? "success"
    : inactive
      ? "muted"
      : overdue
        ? "danger"
        : isDueToday(reminder) || highPriority
          ? "warning"
          : "info";

  return (
    <article
      className={`rounded-md border p-3 shadow-sm ${
        quiet
          ? "border-slate-200 bg-slate-50 opacity-80"
          : overdue
            ? "border-rose-200 bg-rose-50/50"
            : isHighPriority(reminder)
              ? "border-amber-200 bg-amber-50/40"
              : "border-slate-200 bg-white"
      }`}
    >
      <div
        className={`mb-3 h-1 rounded-full ${
          quiet
            ? "bg-slate-200"
            : overdue
              ? "bg-rose-300"
              : isDueToday(reminder) || highPriority
                ? "bg-amber-200"
                : "bg-teal-200"
        }`}
      />
      <div className="flex flex-col gap-3 lg:flex-row lg:items-start lg:justify-between">
        <div className="min-w-0">
          <div className="flex flex-wrap items-center gap-2">
            <h3 className="text-sm font-bold text-slate-950">
              {reminder.title}
            </h3>
            {systemGenerated ? <Badge tone="info">خودکار</Badge> : null}
            {!systemGenerated && generatedFromCarePlan ? (
              <Badge tone="muted">ساخته‌شده از برنامه مراقبتی</Badge>
            ) : null}
            {highPriority ? (
              <Badge tone={reminder.priority === "Urgent" ? "danger" : "warning"}>
                {getOptionLabel(priorityOptions, reminder.priority)}
              </Badge>
            ) : null}
          </div>
          <p className="mt-2 text-xs leading-6 text-slate-500">
            {getOptionLabel(reminderTypeOptions, reminder.reminderType)} · سررسید:{" "}
            {dueAt ?? reminder.dueAt} · وضعیت: {stateLabel}
          </p>
        </div>

        <div className="flex flex-wrap items-center gap-2">
          <Badge tone={stateTone}>{stateLabel}</Badge>
          <button
            className="h-8 rounded-md border border-slate-200 bg-white px-3 text-xs font-semibold text-slate-700 transition hover:bg-slate-50"
            onClick={() => setIsExpanded((current) => !current)}
            type="button"
          >
            {isExpanded ? "بستن جزئیات" : "جزئیات"}
          </button>
        </div>
      </div>

      {isExpanded ? (
        <div className="mt-4 border-t border-slate-100 pt-4">
          {reminder.description ? (
            <div className="mb-3 rounded-md border border-slate-100 bg-white px-3 py-2 text-sm leading-7 text-slate-700">
              {reminder.description}
            </div>
          ) : null}

          <dl className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
            <MetaItem
              label="نوع یادآور"
              value={getOptionLabel(reminderTypeOptions, reminder.reminderType)}
            />
            <MetaItem
              label="وضعیت"
              value={getOptionLabel(statusOptions, reminder.status)}
            />
            <MetaItem
              label="اولویت"
              value={getOptionLabel(priorityOptions, reminder.priority)}
            />
            <MetaItem label="مخاطب" value={getOptionLabel(audienceOptions, reminder.audience)} />
            <MetaItem label="کانال" value={getOptionLabel(channelOptions, reminder.channel)} />
            <MetaItem
              label="نوع رکورد مرتبط"
              value={
                reminder.relatedRecordType
                  ? getOptionLabel(relatedRecordTypeLabels, reminder.relatedRecordType)
                  : null
              }
            />
            <MetaItem
              label="شناسه رکورد مرتبط"
              value={
                reminder.relatedRecordId ? (
                  <span dir="ltr">{reminder.relatedRecordId}</span>
                ) : null
              }
            />
            <MetaItem label="زمان انجام" value={completedAt} />
            <MetaItem
              label="سطح حساسیت"
              value={getOptionLabel(
                sensitivityLevelOptions,
                reminder.sensitivityLevel,
              )}
            />
            <MetaItem
              label="منبع خام"
              value={getOptionLabel(sourceTypeOptions, reminder.sourceType)}
            />
            <MetaItem label="زمان ثبت" value={createdAt} />
            <MetaItem label="آخرین به‌روزرسانی" value={updatedAt} />
          </dl>
        </div>
      ) : null}
    </article>
  );
}

function ReminderCreateForm({
  patientId,
  onCreated,
}: {
  patientId: string;
  onCreated: () => Promise<void>;
}) {
  const router = useRouter();
  const [form, setForm] = useState<CreatePatientReminderPayload>(emptyForm);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  function updateForm<K extends keyof CreatePatientReminderPayload>(
    key: K,
    value: CreatePatientReminderPayload[K],
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

    if (!form.reminderType.trim() || !form.title.trim() || !form.dueAt.trim()) {
      setErrorMessage("نوع یادآور، عنوان یادآور و زمان سررسید الزامی هستند.");
      return;
    }

    setIsSubmitting(true);

    try {
      await createPatientReminder(patientId, form);
      setForm(emptyForm);
      setSuccessMessage("یادآور با موفقیت ثبت شد.");
      await onCreated();
      window.dispatchEvent(new Event(timelineRefreshEventName));
      router.refresh();
    } catch (error) {
      setErrorMessage(
        error instanceof Error
          ? `خطا در ثبت یادآور: ${error.message}`
          : "خطا در ثبت یادآور رخ داد.",
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
        افزودن یادآور دستی
      </h3>
      <div className="mt-3">
        <FormNotice error={errorMessage} success={successMessage} />
      </div>

      <div className="mt-4 grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
        <SelectInput
          label="نوع یادآور"
          onChange={(value) => updateForm("reminderType", value)}
          options={reminderTypeOptions}
          value={form.reminderType}
        />
        <TextInput
          label="عنوان یادآور"
          onChange={(value) => updateForm("title", value)}
          required
          value={form.title}
        />
        <TextInput
          label="زمان سررسید"
          onChange={(value) => updateForm("dueAt", value)}
          required
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
        <SelectInput
          label="مخاطب"
          onChange={(value) => updateForm("audience", value)}
          options={audienceOptions}
          value={form.audience}
        />
        <SelectInput
          label="کانال"
          onChange={(value) => updateForm("channel", value)}
          options={channelOptions}
          value={form.channel}
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
        {isSubmitting ? "در حال ثبت..." : "ثبت یادآور"}
      </button>
    </form>
  );
}

export default function PatientReminders({ patientId }: { patientId: string }) {
  const [reminders, setReminders] = useState<PatientReminder[]>([]);
  const [activeFilter, setActiveFilter] = useState<ReminderFilterId>("all");
  const [showAdvancedFilters, setShowAdvancedFilters] = useState(false);
  const [isCreateOpen, setIsCreateOpen] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const loadReminders = useCallback(async () => {
    setIsLoading(true);
    setErrorMessage(null);

    try {
      const patientReminders = await getPatientReminders(patientId, {
        includeDone: true,
      });
      setReminders(patientReminders);
    } catch (error) {
      setErrorMessage(
        error instanceof Error
          ? error.message
          : "دریافت یادآورها و هشدارهای بیمار با خطا روبه‌رو شد.",
      );
    } finally {
      setIsLoading(false);
    }
  }, [patientId]);

  useEffect(() => {
    const timer = window.setTimeout(() => {
      void loadReminders();
    }, 0);

    return () => window.clearTimeout(timer);
  }, [loadReminders]);

  const filteredReminders = useMemo(
    () =>
      sortReminders(
        reminders.filter((reminder) =>
          reminderMatchesFilter(reminder, activeFilter),
        ),
      ),
    [activeFilter, reminders],
  );
  const activeFilterDefinition = reminderFilters.find(
    (filter) => filter.id === activeFilter,
  );

  return (
    <section className="rounded-md border border-slate-200 bg-white p-4 shadow-sm">
      <div className="flex flex-col gap-3 border-b border-slate-100 pb-4 lg:flex-row lg:items-start lg:justify-between">
        <p className="max-w-3xl text-sm leading-7 text-slate-600">
          پیگیری‌های دستی، هشدارهای داخلی و یادآورهای ساخته‌شده از موعدهای پلن
          مراقبتی در این برد عملیاتی کنار هم نمایش داده می‌شوند.
        </p>
        <div className="flex flex-wrap gap-2">
          <button
            className="inline-flex h-9 items-center justify-center rounded-md border border-slate-300 px-4 text-sm font-semibold text-slate-700 transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:text-slate-400"
            disabled={isLoading}
            onClick={() => {
              void loadReminders();
            }}
            type="button"
          >
            به‌روزرسانی
          </button>
          <button
            className="inline-flex h-9 items-center justify-center rounded-md bg-teal-700 px-4 text-sm font-semibold text-white transition hover:bg-teal-800"
            onClick={() => setIsCreateOpen((current) => !current)}
            type="button"
          >
            {isCreateOpen ? "بستن فرم" : "افزودن یادآور دستی"}
          </button>
        </div>
      </div>

      <div className="mt-4 rounded-md border border-slate-200 bg-slate-50 p-3 text-sm leading-7 text-slate-600">
        برخی یادآورها به‌صورت سیستمی از سررسید آیتم‌های پلن مراقبتی ساخته
        می‌شوند. در مراحل بعد، هنگام ثبت دارو، نتیجه غیرطبیعی یا شاخص نیازمند
        پیگیری، امکان ساخت یادآور مستقیم از همان بخش اضافه می‌شود.
      </div>

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
          {reminderFilters.map((filter) => {
            const isActive = filter.id === activeFilter;
            const count = reminders.filter((reminder) =>
              reminderMatchesFilter(reminder, filter.id),
            ).length;

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
                  {formatCount(count)}
                </span>
              </button>
            );
          })}
        </div>
      ) : null}

      {isCreateOpen ? (
        <div className="mt-5">
          <ReminderCreateForm onCreated={loadReminders} patientId={patientId} />
        </div>
      ) : null}

      <div className="mt-5 space-y-3">
        {isLoading ? (
          <Notice variant="loading">در حال دریافت یادآورها...</Notice>
        ) : null}

        {!isLoading && errorMessage ? (
          <Notice variant="error">{errorMessage}</Notice>
        ) : null}

        {!isLoading && !errorMessage && reminders.length === 0 ? (
          <Notice variant="empty">
            هنوز یادآوری یا هشداری ثبت نشده است.
          </Notice>
        ) : null}

        {!isLoading &&
        !errorMessage &&
        reminders.length > 0 &&
        filteredReminders.length === 0 ? (
          <Notice variant="empty">
            در این فیلتر یادآور یا هشداری پیدا نشد.
          </Notice>
        ) : null}

        {!isLoading && !errorMessage
          ? filteredReminders.map((reminder) => (
              <ReminderCard key={reminder.id} reminder={reminder} />
            ))
          : null}
      </div>
    </section>
  );
}
