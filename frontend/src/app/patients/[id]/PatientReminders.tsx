"use client";

import { useRouter } from "next/navigation";
import type { FormEvent, ReactNode } from "react";
import { useCallback, useEffect, useState } from "react";
import {
  createPatientReminder,
  getPatientReminders,
  type CreatePatientReminderPayload,
  type PatientReminder,
} from "@/lib/api";

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

const reminderTypeOptions = [
  "Medication",
  "LabFollowUp",
  "ImagingFollowUp",
  "Appointment",
  "CarePlan",
  "Lifestyle",
  "General",
  "InternalAlert",
];

const statusOptions = ["Pending", "Done", "Snoozed", "Cancelled", "Missed"];
const priorityOptions = ["Low", "Normal", "High", "Urgent"];
const audienceOptions = ["Internal", "Patient", "CareTeam", "Both"];
const channelOptions = ["InApp", "SMS", "Call", "WhatsApp", "Email", "None"];
const sourceTypeOptions = ["Manual", "ClinicianEntered", "System"];
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

function isDone(reminder: PatientReminder) {
  return reminder.status === "Done";
}

function isOverduePending(reminder: PatientReminder) {
  if (reminder.status !== "Pending") {
    return false;
  }

  const dueAt = new Date(reminder.dueAt);

  return !Number.isNaN(dueAt.getTime()) && dueAt.getTime() < Date.now();
}

function isHighPriority(reminder: PatientReminder) {
  return reminder.priority === "High" || reminder.priority === "Urgent";
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

function Badge({
  children,
  tone = "slate",
}: {
  children: ReactNode;
  tone?: "slate" | "teal" | "rose" | "amber" | "emerald";
}) {
  const toneClass = {
    slate: "bg-slate-100 text-slate-700",
    teal: "bg-teal-50 text-teal-800",
    rose: "bg-rose-50 text-rose-800",
    amber: "bg-amber-50 text-amber-800",
    emerald: "bg-emerald-50 text-emerald-800",
  }[tone];

  return (
    <span className={`rounded-md px-2.5 py-1 text-xs font-semibold ${toneClass}`}>
      {children}
    </span>
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

function ReminderCard({ reminder }: { reminder: PatientReminder }) {
  const dueAt = formatDateTime(reminder.dueAt);
  const completedAt = formatDateTime(reminder.completedAt);
  const overdue = isOverduePending(reminder);
  const completed = isDone(reminder);
  const priorityTone =
    reminder.priority === "Urgent"
      ? "rose"
      : reminder.priority === "High"
        ? "amber"
        : "slate";

  return (
    <article className="rounded-lg border border-slate-200 bg-white p-4 shadow-sm">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
        <div>
          <h3 className="text-base font-bold text-slate-950">
            {reminder.title}
          </h3>
          {reminder.description ? (
            <p className="mt-2 text-sm leading-7 text-slate-600">
              {reminder.description}
            </p>
          ) : null}
        </div>
        <div className="flex shrink-0 flex-wrap gap-2">
          <Badge tone="teal">{reminder.reminderType}</Badge>
          <Badge tone={priorityTone}>{reminder.priority}</Badge>
          {overdue ? <Badge tone="rose">سررسید گذشته</Badge> : null}
          {isHighPriority(reminder) ? (
            <Badge tone={reminder.priority === "Urgent" ? "rose" : "amber"}>
              اولویت مهم
            </Badge>
          ) : null}
          {completed ? <Badge tone="emerald">انجام شده</Badge> : null}
        </div>
      </div>

      <dl className="mt-4 grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
        <MetaItem label="زمان سررسید" value={dueAt ?? reminder.dueAt} />
        {completedAt ? <MetaItem label="زمان انجام" value={completedAt} /> : null}
        <MetaItem label="وضعیت" value={reminder.status} />
        <MetaItem label="مخاطب" value={reminder.audience} />
        {reminder.channel ? (
          <MetaItem label="کانال" value={reminder.channel} />
        ) : null}
        <MetaItem label="منبع" value={reminder.sourceType} />
        <MetaItem label="سطح حساسیت" value={reminder.sensitivityLevel} />
        {reminder.relatedRecordType ? (
          <MetaItem
            label="نوع رکورد مرتبط"
            value={reminder.relatedRecordType}
          />
        ) : null}
        {reminder.relatedRecordId ? (
          <MetaItem
            label="شناسه رکورد مرتبط"
            value={<span dir="ltr">{reminder.relatedRecordId}</span>}
          />
        ) : null}
      </dl>
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
      className="rounded-lg border border-slate-200 bg-slate-50 p-4"
      onSubmit={handleSubmit}
    >
      <h3 className="text-base font-bold text-slate-950">ثبت یادآور جدید</h3>
      <div className="mt-3">
        <Notice error={errorMessage} success={successMessage} />
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
        {isSubmitting ? "در حال ثبت..." : "ثبت یادآور"}
      </button>
    </form>
  );
}

export default function PatientReminders({ patientId }: { patientId: string }) {
  const [reminders, setReminders] = useState<PatientReminder[]>([]);
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

  return (
    <section className="rounded-lg border border-slate-200 bg-white p-4 shadow-sm">
      <div className="flex flex-col gap-2 border-b border-slate-100 pb-4 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <h2 className="text-xl font-bold text-slate-950">یادآورها و هشدارها</h2>
          <p className="mt-2 text-sm leading-7 text-slate-600">
            پیگیری‌های دستی و هشدارهای داخلی که بعدا می‌توانند از داروها، پلن
            مراقبتی و قوانین خودکار هم تولید شوند.
          </p>
        </div>
        <button
          className="inline-flex h-10 items-center justify-center rounded-md border border-slate-300 px-4 text-sm font-semibold text-slate-700 transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:text-slate-400"
          disabled={isLoading}
          onClick={() => {
            void loadReminders();
          }}
          type="button"
        >
          به‌روزرسانی
        </button>
      </div>

      <div className="mt-5">
        <ReminderCreateForm onCreated={loadReminders} patientId={patientId} />
      </div>

      <div className="mt-5 space-y-3">
        {isLoading ? (
          <div className="rounded-md border border-slate-200 bg-slate-50 p-4 text-sm text-slate-600">
            در حال دریافت یادآورها...
          </div>
        ) : null}

        {!isLoading && errorMessage ? (
          <div className="rounded-md border border-rose-200 bg-rose-50 p-4 text-sm leading-7 text-rose-900">
            {errorMessage}
          </div>
        ) : null}

        {!isLoading && !errorMessage && reminders.length === 0 ? (
          <div className="rounded-md border border-dashed border-slate-300 bg-slate-50 p-4 text-sm leading-7 text-slate-600">
            هنوز یادآور یا هشدار فعالی برای این بیمار ثبت نشده است.
          </div>
        ) : null}

        {!isLoading && !errorMessage
          ? reminders.map((reminder) => (
              <ReminderCard key={reminder.id} reminder={reminder} />
            ))
          : null}
      </div>
    </section>
  );
}
