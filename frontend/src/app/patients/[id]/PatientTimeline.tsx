"use client";

import type { FormEvent, ReactNode } from "react";
import { useCallback, useEffect, useMemo, useState } from "react";
import Badge from "@/components/ui/Badge";
import Notice from "@/components/ui/Notice";
import {
  createTimelineEvent,
  getPatientTimeline,
  type CreateTimelineEventPayload,
  type TimelineEvent,
} from "@/lib/api";

const emptyForm: CreateTimelineEventPayload = {
  eventType: "Note",
  title: "",
  description: "",
  occurredAt: "",
  sourceType: "Manual",
  visibility: "Internal",
  sensitivityLevel: "Normal",
};

type TimelineOption = {
  value: string;
  label: string;
};

type TimelineFilterId =
  | "all"
  | "careFollowup"
  | "recordsResults"
  | "carePlan"
  | "medicalHistory"
  | "evidence"
  | "measurements"
  | "reminders"
  | "personal"
  | "system";

const eventTypeOptions: TimelineOption[] = [
  { value: "Note", label: "یادداشت" },
  { value: "MedicalHistory", label: "سابقه پزشکی" },
  { value: "Document", label: "مدرک" },
  { value: "Visit", label: "ویزیت" },
  { value: "CarePlan", label: "پلن مراقبتی" },
  { value: "System", label: "سیستمی" },
];

const eventTypeLabelOptions: TimelineOption[] = [
  ...eventTypeOptions,
  { value: "ParaclinicalResult", label: "نتیجه پاراکلینیک" },
  { value: "Reminder", label: "یادآور" },
  { value: "Measurement", label: "شاخص سلامت" },
];

const sourceTypeOptions: TimelineOption[] = [
  { value: "Manual", label: "دستی" },
  { value: "System", label: "سیستمی" },
  { value: "ClinicianEntered", label: "ثبت تیم درمان" },
];

const visibilityOptions: TimelineOption[] = [
  { value: "Internal", label: "داخلی" },
  { value: "PatientVisible", label: "قابل مشاهده برای بیمار" },
];

const sensitivityOptions: TimelineOption[] = [
  { value: "Normal", label: "عادی" },
  { value: "Sensitive", label: "حساس" },
];

const relatedRecordTypeOptions: TimelineOption[] = [
  { value: "PatientProfile", label: "اطلاعات شخصی" },
  { value: "ContactInfo", label: "اطلاعات تماس" },
  { value: "Condition", label: "بیماری / مشکل فعال" },
  { value: "Allergy", label: "حساسیت" },
  { value: "Medication", label: "دارو" },
  { value: "PatientTimelineEvent", label: "خط زمانی" },
  { value: "PatientDocument", label: "مدرک پزشکی" },
  { value: "PatientParaclinicalResult", label: "نتیجه پاراکلینیک" },
  { value: "PatientLabResultItem", label: "آیتم آزمایش" },
  { value: "CarePlanItem", label: "پلن مراقبتی" },
  { value: "PatientReminder", label: "یادآور" },
  { value: "PatientMeasurement", label: "شاخص سلامت" },
];

const primaryTimelineFilters: Array<{ id: TimelineFilterId; label: string }> = [
  { id: "all", label: "همه" },
  { id: "careFollowup", label: "مراقبت و پیگیری" },
  { id: "recordsResults", label: "سوابق و نتایج" },
  { id: "measurements", label: "شاخص‌ها" },
  { id: "system", label: "سیستم" },
];

const advancedTimelineFilters: Array<{ id: TimelineFilterId; label: string }> = [
  { id: "carePlan", label: "پلن مراقبتی" },
  { id: "medicalHistory", label: "سوابق پزشکی" },
  { id: "evidence", label: "مدارک و نتایج" },
  { id: "reminders", label: "یادآورها" },
  { id: "personal", label: "اطلاعات شخصی" },
];

const timelineFilters = [...primaryTimelineFilters, ...advancedTimelineFilters];

const timelineRefreshEventName = "zibzie:timeline-refresh";

function formatDateTime(value: string | null | undefined) {
  if (!value) {
    return "ثبت نشده";
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

function formatMissing(value: string | null | undefined) {
  return value?.trim() ? value : "ثبت نشده";
}

function getOptionLabel(options: TimelineOption[], value: string | null | undefined) {
  if (!value?.trim()) {
    return "ثبت نشده";
  }

  return options.find((option) => option.value === value)?.label ?? value;
}

function eventMatchesText(event: TimelineEvent, values: string[]) {
  const searchable = [
    event.eventType,
    event.relatedRecordType,
    event.title,
    event.description,
    event.sourceType,
  ]
    .filter(Boolean)
    .join(" ")
    .toLowerCase();

  return values.some((value) => searchable.includes(value.toLowerCase()));
}

function eventMatchesFilter(
  event: TimelineEvent,
  filter: TimelineFilterId,
): boolean {
  switch (filter) {
    case "all":
      return true;
    case "careFollowup":
      return eventMatchesFilter(event, "carePlan") || eventMatchesFilter(event, "reminders");
    case "recordsResults":
      return eventMatchesFilter(event, "medicalHistory") || eventMatchesFilter(event, "evidence");
    case "carePlan":
      return eventMatchesText(event, ["CarePlan", "CarePlanItem"]);
    case "medicalHistory":
      return eventMatchesText(event, [
        "MedicalHistory",
        "Condition",
        "Allergy",
        "Medication",
      ]);
    case "evidence":
      return eventMatchesText(event, [
        "Document",
        "ParaclinicalResult",
        "PatientDocument",
        "PatientParaclinicalResult",
        "PatientLabResultItem",
      ]);
    case "measurements":
      return eventMatchesText(event, ["Measurement", "PatientMeasurement"]);
    case "reminders":
      return eventMatchesText(event, ["Reminder", "PatientReminder"]);
    case "personal":
      return eventMatchesText(event, ["PatientProfile", "ContactInfo"]);
    case "system":
      return event.sourceType === "System" || event.eventType === "System";
  }
}

function getFilterCount(events: TimelineEvent[], filter: TimelineFilterId) {
  return events.filter((event) => eventMatchesFilter(event, filter)).length;
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

function SelectField({
  label,
  value,
  options,
  onChange,
}: {
  label: string;
  value: string;
  options: TimelineOption[];
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
          <option key={option.value} value={option.value}>
            {option.label}
          </option>
        ))}
      </select>
    </Field>
  );
}

function MetaItem({
  label,
  value,
}: {
  label: string;
  value: string | null | undefined;
}) {
  return (
    <div className="rounded-md bg-slate-50 px-3 py-2">
      <dt className="text-xs font-medium text-slate-500">{label}</dt>
      <dd className="mt-1 break-all text-sm font-semibold text-slate-800">
        {formatMissing(value)}
      </dd>
    </div>
  );
}

function TimelineEventCard({ event }: { event: TimelineEvent }) {
  const [isExpanded, setIsExpanded] = useState(false);
  const relatedRecordLabel = getOptionLabel(
    relatedRecordTypeOptions,
    event.relatedRecordType,
  );
  const hasRelatedRecord = Boolean(event.relatedRecordType || event.relatedRecordId);
  const isSystemGenerated = event.sourceType === "System";

  return (
    <article className="rounded-md border border-slate-200 bg-white p-3 shadow-sm">
      <div
        className={`mb-3 h-1 rounded-full ${
          isSystemGenerated ? "bg-slate-300" : "bg-teal-200"
        }`}
      />
      <div className="flex flex-col gap-3 lg:flex-row lg:items-start lg:justify-between">
        <div className="min-w-0">
          <div className="flex flex-wrap items-center gap-2">
            <h3 className="text-sm font-bold text-slate-950">{event.title}</h3>
            {isSystemGenerated ? <Badge tone="muted">خودکار</Badge> : null}
          </div>

          <p className="mt-2 text-xs leading-6 text-slate-500">
            {getOptionLabel(eventTypeLabelOptions, event.eventType)} ·{" "}
            {formatDateTime(event.occurredAt)}
          </p>
        </div>

        <div className="flex flex-wrap items-center gap-2">
          <button
            className="h-8 rounded-md border border-slate-200 px-3 text-xs font-semibold text-slate-700 transition hover:bg-slate-50"
            onClick={() => setIsExpanded((current) => !current)}
            type="button"
          >
            {isExpanded ? "بستن جزئیات" : "جزئیات"}
          </button>
        </div>
      </div>

      {event.description ? (
        <p className="mt-3 line-clamp-2 text-sm leading-7 text-slate-600">
          {event.description}
        </p>
      ) : null}

      {isExpanded ? (
        <div className="mt-4 border-t border-slate-100 pt-4">
          {event.description ? (
            <div className="mb-3 rounded-md border border-slate-100 bg-slate-50 px-3 py-2 text-sm leading-7 text-slate-700">
              {event.description}
            </div>
          ) : null}

          <dl className="grid gap-2 sm:grid-cols-2 xl:grid-cols-3">
            <MetaItem
              label="نوع رویداد"
              value={getOptionLabel(eventTypeLabelOptions, event.eventType)}
            />
            <MetaItem
              label="نوع رکورد مرتبط"
              value={hasRelatedRecord ? relatedRecordLabel : null}
            />
            <MetaItem label="شناسه رکورد مرتبط" value={event.relatedRecordId} />
            <MetaItem
              label="منبع"
              value={getOptionLabel(sourceTypeOptions, event.sourceType)}
            />
            <MetaItem
              label="سطح نمایش"
              value={getOptionLabel(visibilityOptions, event.visibility)}
            />
            <MetaItem
              label="سطح حساسیت"
              value={getOptionLabel(sensitivityOptions, event.sensitivityLevel)}
            />
            <MetaItem label="زمان وقوع" value={formatDateTime(event.occurredAt)} />
            <MetaItem label="زمان ثبت" value={formatDateTime(event.createdAt)} />
            <MetaItem
              label="آخرین به‌روزرسانی"
              value={event.updatedAt ? formatDateTime(event.updatedAt) : null}
            />
          </dl>
        </div>
      ) : null}
    </article>
  );
}

function TimelineCreateForm({
  patientId,
  onCreated,
}: {
  patientId: string;
  onCreated: () => Promise<void>;
}) {
  const [form, setForm] = useState<CreateTimelineEventPayload>(emptyForm);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  function updateForm<K extends keyof CreateTimelineEventPayload>(
    key: K,
    value: CreateTimelineEventPayload[K],
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

    if (!form.eventType.trim() || !form.title.trim()) {
      setErrorMessage("نوع رویداد و عنوان الزامی هستند.");
      return;
    }

    setIsSubmitting(true);

    try {
      await createTimelineEvent(patientId, form);
      setForm(emptyForm);
      setSuccessMessage("رویداد با موفقیت ثبت شد.");
      await onCreated();
    } catch (error) {
      setErrorMessage(
        error instanceof Error
          ? `خطا در ثبت رویداد: ${error.message}`
          : "خطا در ثبت رویداد رخ داد.",
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
      <h3 className="text-base font-bold text-slate-950">ثبت رویداد جدید</h3>

      {errorMessage || successMessage ? (
        <div
          className={`mt-3 rounded-md border p-3 text-sm leading-7 ${
            errorMessage
              ? "border-rose-200 bg-rose-50 text-rose-900"
              : "border-emerald-200 bg-emerald-50 text-emerald-900"
          }`}
        >
          {errorMessage ?? successMessage}
        </div>
      ) : null}

      <div className="mt-4 grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
        <SelectField
          label="نوع رویداد"
          onChange={(value) => updateForm("eventType", value)}
          options={eventTypeOptions}
          value={form.eventType}
        />
        <Field label="عنوان">
          <input
            className="h-10 rounded-md border border-slate-300 bg-white px-3 text-slate-950 outline-none transition placeholder:text-slate-400 focus:border-teal-600 focus:ring-2 focus:ring-teal-100"
            onChange={(event) => updateForm("title", event.target.value)}
            required
            value={form.title}
          />
        </Field>
        <Field label="زمان وقوع">
          <input
            className="h-10 rounded-md border border-slate-300 bg-white px-3 text-slate-950 outline-none transition focus:border-teal-600 focus:ring-2 focus:ring-teal-100"
            onChange={(event) => updateForm("occurredAt", event.target.value)}
            type="datetime-local"
            value={form.occurredAt}
          />
        </Field>
        <SelectField
          label="منبع"
          onChange={(value) => updateForm("sourceType", value)}
          options={sourceTypeOptions}
          value={form.sourceType}
        />
        <SelectField
          label="سطح نمایش"
          onChange={(value) => updateForm("visibility", value)}
          options={visibilityOptions}
          value={form.visibility}
        />
        <SelectField
          label="حساسیت"
          onChange={(value) => updateForm("sensitivityLevel", value)}
          options={sensitivityOptions}
          value={form.sensitivityLevel}
        />
      </div>

      <Field label="توضیحات">
        <textarea
          className="mt-1 min-h-20 resize-y rounded-md border border-slate-300 bg-white px-3 py-2 text-slate-950 outline-none transition placeholder:text-slate-400 focus:border-teal-600 focus:ring-2 focus:ring-teal-100"
          onChange={(event) => updateForm("description", event.target.value)}
          value={form.description}
        />
      </Field>

      <button
        className="mt-4 inline-flex h-10 w-full items-center justify-center rounded-md bg-teal-700 px-4 text-sm font-semibold text-white transition hover:bg-teal-800 disabled:cursor-not-allowed disabled:bg-slate-400 sm:w-auto"
        disabled={isSubmitting}
        type="submit"
      >
        {isSubmitting ? "در حال ثبت..." : "ثبت رویداد"}
      </button>
    </form>
  );
}

export default function PatientTimeline({
  patientId,
  showCreateForm = true,
}: {
  patientId: string;
  showCreateForm?: boolean;
}) {
  const [events, setEvents] = useState<TimelineEvent[]>([]);
  const [activeFilter, setActiveFilter] = useState<TimelineFilterId>("all");
  const [showAdvancedFilters, setShowAdvancedFilters] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const loadTimeline = useCallback(async () => {
    setErrorMessage(null);

    try {
      const timelineEvents = await getPatientTimeline(patientId, {
        includeInternal: true,
      });
      setEvents(timelineEvents);
    } catch (error) {
      setErrorMessage(
        error instanceof Error
          ? error.message
          : "دریافت خط زمانی بیمار با خطا روبه‌رو شد.",
      );
    } finally {
      setIsLoading(false);
    }
  }, [patientId]);

  useEffect(() => {
    const timer = window.setTimeout(() => {
      void loadTimeline();
    }, 0);

    return () => window.clearTimeout(timer);
  }, [loadTimeline]);

  useEffect(() => {
    function handleTimelineRefresh() {
      setIsLoading(true);
      void loadTimeline();
    }

    window.addEventListener(timelineRefreshEventName, handleTimelineRefresh);

    return () => {
      window.removeEventListener(
        timelineRefreshEventName,
        handleTimelineRefresh,
      );
    };
  }, [loadTimeline]);

  const filteredEvents = useMemo(
    () => events.filter((event) => eventMatchesFilter(event, activeFilter)),
    [activeFilter, events],
  );
  const activeFilterDefinition = timelineFilters.find(
    (filter) => filter.id === activeFilter,
  );

  return (
    <section className="rounded-md border border-slate-200 bg-white p-4 shadow-sm">
      <div className="flex flex-col gap-3 border-b border-slate-100 pb-4 lg:flex-row lg:items-end lg:justify-between">
        <div>
          <p className="text-sm leading-7 text-slate-600">
            گزارش فشرده رویدادها، تغییرات و فعالیت‌های مهم پرونده سلامت.
          </p>
        </div>
        <button
          className="inline-flex h-9 items-center justify-center rounded-md border border-slate-300 px-4 text-sm font-semibold text-slate-700 transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:text-slate-400"
          disabled={isLoading}
          onClick={() => {
            setIsLoading(true);
            void loadTimeline();
          }}
          type="button"
        >
          به‌روزرسانی
        </button>
      </div>

      <div className="mt-4">
        <Notice variant="info">
          خط زمانی، تاریخچه بالینی و عملیاتی پرونده بیمار است و جایگزین AuditLog
          امنیتی نیست.
        </Notice>
      </div>

      {showCreateForm ? (
        <div className="mt-5">
          <TimelineCreateForm patientId={patientId} onCreated={loadTimeline} />
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
          {timelineFilters.map((filter) => {
            const isActive = filter.id === activeFilter;
            const count = getFilterCount(events, filter.id);

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
                  {count}
                </span>
              </button>
            );
          })}
        </div>
      ) : null}

      <div className="mt-4 space-y-3">
        {isLoading ? <Notice variant="loading">در حال دریافت رویدادها...</Notice> : null}

        {!isLoading && errorMessage ? (
          <Notice variant="error">{errorMessage}</Notice>
        ) : null}

        {!isLoading && !errorMessage && events.length === 0 ? (
          <Notice variant="empty">
            هنوز رویدادی در خط زمانی ثبت نشده است. تغییرات و اقدام‌های مهم پرونده
            در این بخش نمایش داده می‌شوند.
          </Notice>
        ) : null}

        {!isLoading &&
        !errorMessage &&
        events.length > 0 &&
        filteredEvents.length === 0 ? (
          <Notice variant="empty">
            در این فیلتر رویدادی پیدا نشد. برای دیدن همه فعالیت‌ها فیلتر «همه» را
            انتخاب کنید.
          </Notice>
        ) : null}

        {!isLoading && !errorMessage
          ? filteredEvents.map((event) => (
              <TimelineEventCard event={event} key={event.id} />
            ))
          : null}
      </div>
    </section>
  );
}
