"use client";

import type { FormEvent } from "react";
import { useCallback, useEffect, useState } from "react";
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

const eventTypeOptions = [
  "Note",
  "MedicalHistory",
  "Document",
  "Visit",
  "CarePlan",
  "System",
];

const sourceTypeOptions = ["Manual", "System", "ClinicianEntered"];
const visibilityOptions = ["Internal", "PatientVisible"];
const sensitivityOptions = ["Normal", "Sensitive"];
const timelineRefreshEventName = "zibzie:timeline-refresh";

function formatDateTime(value: string) {
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
  children: React.ReactNode;
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

function TimelineEventCard({ event }: { event: TimelineEvent }) {
  return (
    <article className="rounded-lg border border-slate-200 bg-white p-4 shadow-sm">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
        <div>
          <h3 className="text-base font-bold text-slate-950">{event.title}</h3>
          {event.description ? (
            <p className="mt-2 text-sm leading-7 text-slate-600">
              {event.description}
            </p>
          ) : null}
        </div>
        <time className="shrink-0 rounded-md bg-slate-100 px-2.5 py-1 text-xs font-medium text-slate-600">
          {formatDateTime(event.occurredAt)}
        </time>
      </div>

      <div className="mt-4 flex flex-wrap gap-2 text-xs">
        <span className="rounded-md bg-teal-50 px-2.5 py-1 font-medium text-teal-800">
          {event.eventType}
        </span>
        <span className="rounded-md bg-slate-100 px-2.5 py-1 font-medium text-slate-700">
          منبع: {event.sourceType}
        </span>
        <span className="rounded-md bg-slate-100 px-2.5 py-1 font-medium text-slate-700">
          نمایش: {event.visibility}
        </span>
        <span className="rounded-md bg-slate-100 px-2.5 py-1 font-medium text-slate-700">
          حساسیت: {event.sensitivityLevel}
        </span>
      </div>
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
      className="rounded-lg border border-slate-200 bg-slate-50 p-4"
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

export default function PatientTimeline({ patientId }: { patientId: string }) {
  const [events, setEvents] = useState<TimelineEvent[]>([]);
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

  return (
    <section className="rounded-lg border border-slate-200 bg-white p-4 shadow-sm">
      <div className="flex flex-col gap-2 border-b border-slate-100 pb-4 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <h2 className="text-xl font-bold text-slate-950">خط زمانی بیمار</h2>
          <p className="mt-2 text-sm leading-7 text-slate-600">
            نمای رویدادهای مهم پرونده سلامت برای تیم مراقبت.
          </p>
        </div>
        <button
          className="inline-flex h-10 items-center justify-center rounded-md border border-slate-300 px-4 text-sm font-semibold text-slate-700 transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:text-slate-400"
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

      <div className="mt-5">
        <TimelineCreateForm patientId={patientId} onCreated={loadTimeline} />
      </div>

      <div className="mt-5 space-y-3">
        {isLoading ? (
          <div className="rounded-md border border-slate-200 bg-slate-50 p-4 text-sm text-slate-600">
            در حال دریافت رویدادها...
          </div>
        ) : null}

        {!isLoading && errorMessage ? (
          <div className="rounded-md border border-rose-200 bg-rose-50 p-4 text-sm leading-7 text-rose-900">
            {errorMessage}
          </div>
        ) : null}

        {!isLoading && !errorMessage && events.length === 0 ? (
          <div className="rounded-md border border-dashed border-slate-300 bg-slate-50 p-4 text-sm leading-7 text-slate-600">
            هنوز رویدادی برای این بیمار ثبت نشده است.
          </div>
        ) : null}

        {!isLoading && !errorMessage
          ? events.map((event) => (
              <TimelineEventCard event={event} key={event.id} />
            ))
          : null}
      </div>
    </section>
  );
}
