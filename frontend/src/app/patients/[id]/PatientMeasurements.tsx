"use client";

import { useRouter } from "next/navigation";
import type { FormEvent, ReactNode } from "react";
import { useCallback, useEffect, useMemo, useState } from "react";
import {
  createPatientMeasurement,
  getPatientMeasurements,
  type CreatePatientMeasurementPayload,
  type PatientMeasurement,
} from "@/lib/api";

const timelineRefreshEventName = "zibzie:timeline-refresh";

const emptyForm: CreatePatientMeasurementPayload = {
  measurementType: "Weight",
  displayName: "",
  value: "",
  unit: "",
  measuredAt: "",
  method: "",
  bodySite: "",
  context: "",
  referenceRange: "",
  isAbnormal: "",
  targetMin: "",
  targetMax: "",
  sourceType: "Manual",
  relatedRecordType: "",
  relatedRecordId: "",
  verificationStatus: "Unverified",
  sensitivityLevel: "Normal",
};

const measurementTypeOptions = [
  "Weight",
  "Height",
  "BMI",
  "BloodPressureSystolic",
  "BloodPressureDiastolic",
  "HeartRate",
  "Temperature",
  "SpO2",
  "FastingBloodGlucose",
  "RandomBloodGlucose",
  "HbA1c",
  "LDL",
  "HDL",
  "Triglycerides",
  "Creatinine",
  "eGFR",
  "Other",
];

const sourceTypeOptions = [
  "Manual",
  "LabResult",
  "Device",
  "ParaclinicalResult",
  "System",
];

const verificationStatusOptions = [
  "Unverified",
  "PatientReported",
  "ClinicianVerified",
  "DeviceImported",
  "SystemGenerated",
];

const sensitivityLevelOptions = ["Normal", "Sensitive"];

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

function formatNumber(value: number) {
  return new Intl.NumberFormat("fa-IR", {
    maximumFractionDigits: 3,
  }).format(value);
}

function formatBoolean(value: boolean | null) {
  if (value === null) {
    return "ثبت نشده";
  }

  return value ? "بله" : "خیر";
}

function valueWithUnit(measurement: PatientMeasurement) {
  return `${formatNumber(measurement.value)} ${measurement.unit}`;
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

function sortByMeasuredAtAscending(measurements: PatientMeasurement[]) {
  return [...measurements].sort((first, second) => {
    const firstTime = new Date(first.measuredAt).getTime();
    const secondTime = new Date(second.measuredAt).getTime();

    return (Number.isNaN(firstTime) ? 0 : firstTime) -
      (Number.isNaN(secondTime) ? 0 : secondTime);
  });
}

type MeasurementTrendGroup = {
  measurementType: string;
  displayName: string;
  unit: string;
  latest: PatientMeasurement;
  points: PatientMeasurement[];
};

function getMeasurementTrendGroups(
  measurements: PatientMeasurement[],
): MeasurementTrendGroup[] {
  const grouped = measurements.reduce<Record<string, PatientMeasurement[]>>(
    (groups, measurement) => ({
      ...groups,
      [measurement.measurementType]: [
        ...(groups[measurement.measurementType] ?? []),
        measurement,
      ],
    }),
    {},
  );

  return Object.entries(grouped)
    .map(([measurementType, group]) => {
      const points = sortByMeasuredAtAscending(group);
      const latest = points[points.length - 1];

      return {
        measurementType,
        displayName: latest?.displayName ?? measurementType,
        unit: latest?.unit ?? "",
        latest,
        points,
      };
    })
    .filter(
      (group): group is MeasurementTrendGroup =>
        Boolean(group.latest) && group.points.length >= 2,
    )
    .sort((first, second) => {
      const firstAbnormal = first.latest.isAbnormal ? 1 : 0;
      const secondAbnormal = second.latest.isAbnormal ? 1 : 0;

      return secondAbnormal - firstAbnormal || second.points.length - first.points.length;
    });
}

function MiniTrendCard({ group }: { group: MeasurementTrendGroup }) {
  const width = 180;
  const height = 54;
  const values = group.points.map((point) => point.value);
  const minValue = Math.min(...values);
  const maxValue = Math.max(...values);
  const range = maxValue - minValue || 1;
  const linePoints = group.points
    .map((point, index) => {
      const x =
        group.points.length === 1
          ? 0
          : (index / (group.points.length - 1)) * width;
      const y = 6 + ((maxValue - point.value) / range) * (height - 12);

      return `${x},${y}`;
    })
    .join(" ");

  return (
    <article className="rounded-md border border-slate-200 bg-white p-3">
      <div className="flex items-start justify-between gap-3">
        <div>
          <h3 className="text-sm font-bold text-slate-950">
            {group.displayName}
          </h3>
          <p className="mt-1 text-xs text-slate-500">
            {formatNumber(group.points.length)} داده
          </p>
        </div>
        <Badge tone={group.latest.isAbnormal ? "rose" : "teal"}>
          {formatNumber(group.latest.value)} {group.unit}
        </Badge>
      </div>
      <svg
        aria-label={`روند ${group.displayName}`}
        className="mt-3 h-14 w-full rounded-md bg-slate-50"
        preserveAspectRatio="none"
        role="img"
        viewBox={`0 0 ${width} ${height}`}
      >
        <polyline
          fill="none"
          points={linePoints}
          stroke={group.latest.isAbnormal ? "#e11d48" : "#0f766e"}
          strokeLinecap="round"
          strokeLinejoin="round"
          strokeWidth="3"
        />
        {group.points.map((point, index) => {
          const x =
            group.points.length === 1
              ? 0
              : (index / (group.points.length - 1)) * width;
          const y = 6 + ((maxValue - point.value) / range) * (height - 12);

          return (
            <circle
              cx={x}
              cy={y}
              fill={point.isAbnormal ? "#e11d48" : "#0f766e"}
              key={point.id}
              r="3.5"
            />
          );
        })}
      </svg>
    </article>
  );
}

function MeasurementQuickTrends({
  groups,
}: {
  groups: MeasurementTrendGroup[];
}) {
  return (
    <section className="rounded-md border border-slate-200 bg-slate-50 p-4">
      <div className="flex flex-col gap-2 sm:flex-row sm:items-start sm:justify-between">
        <div>
          <h3 className="text-base font-bold text-slate-950">
            روندهای سریع شاخص‌ها
          </h3>
          <p className="mt-1 text-sm leading-7 text-slate-600">
            خلاصه کوچک از شاخص‌هایی که حداقل دو داده ثبت‌شده دارند.
          </p>
        </div>
        {groups.length > 0 ? (
          <Badge tone="teal">{formatNumber(groups.length)} نمودار</Badge>
        ) : null}
      </div>
      <div className="mt-4 grid gap-3 sm:grid-cols-2 xl:grid-cols-3">
        {groups.length > 0 ? (
          groups.map((group) => (
            <MiniTrendCard group={group} key={group.measurementType} />
          ))
        ) : (
          <div className="rounded-md border border-dashed border-slate-300 bg-white p-4 text-sm leading-7 text-slate-600 sm:col-span-2 xl:col-span-3">
            برای نمایش نمودار سریع، حداقل دو اندازه‌گیری از یک نوع لازم است.
          </div>
        )}
      </div>
    </section>
  );
}

function MeasurementTrend({
  measurementType,
  measurements,
}: {
  measurementType: string | null;
  measurements: PatientMeasurement[];
}) {
  if (!measurementType) {
    return (
      <div className="rounded-md border border-dashed border-slate-300 bg-slate-50 p-4 text-sm leading-7 text-slate-600">
        برای نمایش روند، ابتدا یک نوع شاخص انتخاب کنید.
      </div>
    );
  }

  const points = sortByMeasuredAtAscending(measurements);

  if (points.length < 2) {
    return (
      <div className="rounded-md border border-dashed border-slate-300 bg-slate-50 p-4 text-sm leading-7 text-slate-600">
        برای نمایش روند {measurementType} حداقل دو اندازه‌گیری لازم است.
      </div>
    );
  }

  const width = 720;
  const height = 220;
  const paddingX = 54;
  const paddingTop = 28;
  const paddingBottom = 42;
  const plotWidth = width - paddingX * 2;
  const plotHeight = height - paddingTop - paddingBottom;
  const values = points.map((point) => point.value);
  const minValue = Math.min(...values);
  const maxValue = Math.max(...values);
  const range = maxValue - minValue || 1;
  const latest = points[points.length - 1];

  const chartPoints = points.map((point, index) => {
    const x =
      paddingX +
      (points.length === 1 ? 0 : (index / (points.length - 1)) * plotWidth);
    const y =
      paddingTop + ((maxValue - point.value) / range) * plotHeight;

    return {
      measurement: point,
      x,
      y,
    };
  });

  const linePoints = chartPoints
    .map((point) => `${point.x},${point.y}`)
    .join(" ");

  return (
    <div className="rounded-lg border border-slate-200 bg-slate-50 p-4">
      <div className="flex flex-col gap-2 sm:flex-row sm:items-start sm:justify-between">
        <div>
          <h3 className="text-base font-bold text-slate-950">
            روند {measurementType}
          </h3>
          <p className="mt-1 text-sm leading-7 text-slate-600">
            آخرین مقدار: {valueWithUnit(latest)} در{" "}
            {formatDateTime(latest.measuredAt) ?? latest.measuredAt}
          </p>
        </div>
        <Badge tone={latest.isAbnormal ? "rose" : "teal"}>
          {latest.displayName}: {valueWithUnit(latest)}
        </Badge>
      </div>

      <div className="mt-4 overflow-x-auto">
        <svg
          aria-label={`روند ${measurementType}`}
          className="h-56 min-w-[640px] rounded-md bg-white"
          role="img"
          viewBox={`0 0 ${width} ${height}`}
        >
          <line
            stroke="#cbd5e1"
            strokeWidth="1"
            x1={paddingX}
            x2={paddingX}
            y1={paddingTop}
            y2={height - paddingBottom}
          />
          <line
            stroke="#cbd5e1"
            strokeWidth="1"
            x1={paddingX}
            x2={width - paddingX}
            y1={height - paddingBottom}
            y2={height - paddingBottom}
          />
          <text
            fill="#64748b"
            fontSize="12"
            textAnchor="end"
            x={paddingX - 10}
            y={paddingTop + 4}
          >
            {formatNumber(maxValue)}
          </text>
          <text
            fill="#64748b"
            fontSize="12"
            textAnchor="end"
            x={paddingX - 10}
            y={height - paddingBottom + 4}
          >
            {formatNumber(minValue)}
          </text>
          <polyline
            fill="none"
            points={linePoints}
            stroke="#0f766e"
            strokeLinecap="round"
            strokeLinejoin="round"
            strokeWidth="3"
          />
          {chartPoints.map((point) => (
            <g key={point.measurement.id}>
              <circle
                cx={point.x}
                cy={point.y}
                fill={point.measurement.isAbnormal ? "#e11d48" : "#0f766e"}
                r="5"
              />
              <circle
                cx={point.x}
                cy={point.y}
                fill="transparent"
                r="12"
              >
                <title>
                  {`${point.measurement.displayName}: ${valueWithUnit(
                    point.measurement,
                  )} - ${
                    formatDateTime(point.measurement.measuredAt) ??
                    point.measurement.measuredAt
                  }`}
                </title>
              </circle>
            </g>
          ))}
          <text
            fill="#64748b"
            fontSize="12"
            textAnchor="start"
            x={paddingX}
            y={height - 14}
          >
            {formatDateTime(points[0].measuredAt) ?? points[0].measuredAt}
          </text>
          <text
            fill="#64748b"
            fontSize="12"
            textAnchor="end"
            x={width - paddingX}
            y={height - 14}
          >
            {formatDateTime(latest.measuredAt) ?? latest.measuredAt}
          </text>
        </svg>
      </div>
    </div>
  );
}

function MeasurementCard({
  measurement,
}: {
  measurement: PatientMeasurement;
}) {
  const measuredAt = formatDateTime(measurement.measuredAt);
  const targetRange =
    measurement.targetMin !== null || measurement.targetMax !== null
      ? `${measurement.targetMin !== null ? formatNumber(measurement.targetMin) : "ثبت نشده"} تا ${
          measurement.targetMax !== null
            ? formatNumber(measurement.targetMax)
            : "ثبت نشده"
        }`
      : null;

  return (
    <article className="rounded-lg border border-slate-200 bg-white p-4 shadow-sm">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
        <div>
          <h3 className="text-base font-bold text-slate-950">
            {measurement.displayName}
          </h3>
          <p className="mt-2 text-lg font-bold text-teal-800">
            {valueWithUnit(measurement)}
          </p>
        </div>
        <div className="flex shrink-0 flex-wrap gap-2">
          <Badge tone="teal">{measurement.measurementType}</Badge>
          {measurement.isAbnormal !== null ? (
            <Badge tone={measurement.isAbnormal ? "rose" : "emerald"}>
              {measurement.isAbnormal ? "غیرطبیعی" : "طبیعی"}
            </Badge>
          ) : null}
        </div>
      </div>

      <dl className="mt-4 grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
        <MetaItem label="زمان اندازه‌گیری" value={measuredAt ?? measurement.measuredAt} />
        {measurement.method ? (
          <MetaItem label="روش اندازه‌گیری" value={measurement.method} />
        ) : null}
        {measurement.bodySite ? (
          <MetaItem label="محل اندازه‌گیری" value={measurement.bodySite} />
        ) : null}
        {measurement.context ? (
          <MetaItem label="زمینه / توضیح" value={measurement.context} />
        ) : null}
        {measurement.referenceRange ? (
          <MetaItem label="محدوده مرجع" value={measurement.referenceRange} />
        ) : null}
        {measurement.isAbnormal !== null ? (
          <MetaItem
            label="غیرطبیعی"
            value={formatBoolean(measurement.isAbnormal)}
          />
        ) : null}
        {targetRange ? <MetaItem label="هدف" value={targetRange} /> : null}
        <MetaItem label="منبع" value={measurement.sourceType} />
        <MetaItem label="وضعیت تأیید" value={measurement.verificationStatus} />
        <MetaItem label="سطح حساسیت" value={measurement.sensitivityLevel} />
        {measurement.relatedRecordType ? (
          <MetaItem
            label="نوع رکورد مرتبط"
            value={measurement.relatedRecordType}
          />
        ) : null}
        {measurement.relatedRecordId ? (
          <MetaItem
            label="شناسه رکورد مرتبط"
            value={<span dir="ltr">{measurement.relatedRecordId}</span>}
          />
        ) : null}
      </dl>
    </article>
  );
}

function MeasurementCreateForm({
  patientId,
  onCreated,
}: {
  patientId: string;
  onCreated: (measurementType: string) => Promise<void>;
}) {
  const router = useRouter();
  const [form, setForm] =
    useState<CreatePatientMeasurementPayload>(emptyForm);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  function updateForm<K extends keyof CreatePatientMeasurementPayload>(
    key: K,
    value: CreatePatientMeasurementPayload[K],
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

    const numericValue = Number(form.value);

    if (
      !form.measurementType.trim() ||
      !form.value.trim() ||
      !Number.isFinite(numericValue) ||
      !form.unit.trim()
    ) {
      setErrorMessage("نوع شاخص، مقدار عددی و واحد الزامی هستند.");
      return;
    }

    setIsSubmitting(true);

    try {
      await createPatientMeasurement(patientId, form);
      const createdMeasurementType = form.measurementType.trim();
      setForm(emptyForm);
      setSuccessMessage("شاخص سلامت با موفقیت ثبت شد.");
      await onCreated(createdMeasurementType);
      window.dispatchEvent(new Event(timelineRefreshEventName));
      router.refresh();
    } catch (error) {
      setErrorMessage(
        error instanceof Error
          ? `خطا در ثبت شاخص سلامت: ${error.message}`
          : "خطا در ثبت شاخص سلامت رخ داد.",
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
        ثبت شاخص سلامت جدید
      </h3>
      <div className="mt-3">
        <Notice error={errorMessage} success={successMessage} />
      </div>

      <div className="mt-4 grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
        <SelectInput
          label="نوع شاخص"
          onChange={(value) => updateForm("measurementType", value)}
          options={measurementTypeOptions}
          value={form.measurementType}
        />
        <TextInput
          label="عنوان نمایشی"
          onChange={(value) => updateForm("displayName", value)}
          value={form.displayName}
        />
        <TextInput
          dir="ltr"
          label="مقدار"
          onChange={(value) => updateForm("value", value)}
          required
          type="number"
          value={form.value}
        />
        <TextInput
          dir="ltr"
          label="واحد"
          onChange={(value) => updateForm("unit", value)}
          required
          value={form.unit}
        />
        <TextInput
          label="زمان اندازه‌گیری"
          onChange={(value) => updateForm("measuredAt", value)}
          type="datetime-local"
          value={form.measuredAt}
        />
        <TextInput
          label="روش اندازه‌گیری"
          onChange={(value) => updateForm("method", value)}
          value={form.method}
        />
        <TextInput
          label="محل اندازه‌گیری"
          onChange={(value) => updateForm("bodySite", value)}
          value={form.bodySite}
        />
        <TextInput
          label="محدوده مرجع"
          onChange={(value) => updateForm("referenceRange", value)}
          value={form.referenceRange}
        />
        <BooleanSelect
          label="غیرطبیعی"
          onChange={(value) => updateForm("isAbnormal", value)}
          value={form.isAbnormal}
        />
        <TextInput
          dir="ltr"
          label="هدف حداقل"
          onChange={(value) => updateForm("targetMin", value)}
          type="number"
          value={form.targetMin}
        />
        <TextInput
          dir="ltr"
          label="هدف حداکثر"
          onChange={(value) => updateForm("targetMax", value)}
          type="number"
          value={form.targetMax}
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
      </div>

      <Field label="زمینه / توضیح">
        <textarea
          className="mt-1 min-h-20 resize-y rounded-md border border-slate-300 bg-white px-3 py-2 text-slate-950 outline-none transition placeholder:text-slate-400 focus:border-teal-600 focus:ring-2 focus:ring-teal-100"
          onChange={(event) => updateForm("context", event.target.value)}
          value={form.context}
        />
      </Field>

      <button
        className="mt-4 inline-flex h-10 w-full items-center justify-center rounded-md bg-teal-700 px-4 text-sm font-semibold text-white transition hover:bg-teal-800 disabled:cursor-not-allowed disabled:bg-slate-400 sm:w-auto"
        disabled={isSubmitting}
        type="submit"
      >
        {isSubmitting ? "در حال ثبت..." : "ثبت شاخص سلامت"}
      </button>
    </form>
  );
}

export default function PatientMeasurements({
  patientId,
}: {
  patientId: string;
}) {
  const [measurements, setMeasurements] = useState<PatientMeasurement[]>([]);
  const [measurementTypeFilter, setMeasurementTypeFilter] = useState("");
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const loadMeasurements = useCallback(async () => {
    setIsLoading(true);
    setErrorMessage(null);

    try {
      const patientMeasurements = await getPatientMeasurements(patientId);
      setMeasurements(patientMeasurements);
    } catch (error) {
      setErrorMessage(
        error instanceof Error
          ? error.message
          : "دریافت شاخص‌های سلامت بیمار با خطا روبه‌رو شد.",
      );
    } finally {
      setIsLoading(false);
    }
  }, [patientId]);

  useEffect(() => {
    const timer = window.setTimeout(() => {
      void loadMeasurements();
    }, 0);

    return () => window.clearTimeout(timer);
  }, [loadMeasurements]);

  const availableMeasurementTypes = useMemo(() => {
    const knownTypes = measurements.map(
      (measurement) => measurement.measurementType,
    );

    return Array.from(new Set([...measurementTypeOptions, ...knownTypes]));
  }, [measurements]);

  const visibleMeasurements = useMemo(() => {
    if (!measurementTypeFilter) {
      return measurements;
    }

    return measurements.filter(
      (measurement) => measurement.measurementType === measurementTypeFilter,
    );
  }, [measurementTypeFilter, measurements]);

  const trendMeasurementType = useMemo(() => {
    if (measurementTypeFilter) {
      return measurementTypeFilter;
    }

    const countsByType = measurements.reduce<Record<string, number>>(
      (counts, measurement) => ({
        ...counts,
        [measurement.measurementType]:
          (counts[measurement.measurementType] ?? 0) + 1,
      }),
      {},
    );

    return (
      Object.entries(countsByType).find(([, count]) => count >= 2)?.[0] ??
      measurements[0]?.measurementType ??
      null
    );
  }, [measurementTypeFilter, measurements]);

  const trendMeasurements = useMemo(() => {
    if (!trendMeasurementType) {
      return [];
    }

    return measurements.filter(
      (measurement) => measurement.measurementType === trendMeasurementType,
    );
  }, [measurements, trendMeasurementType]);

  const quickTrendGroups = useMemo(
    () => getMeasurementTrendGroups(measurements).slice(0, 3),
    [measurements],
  );

  async function handleCreated(createdMeasurementType: string) {
    setMeasurementTypeFilter(createdMeasurementType);
    await loadMeasurements();
  }

  return (
    <section className="rounded-lg border border-slate-200 bg-white p-4 shadow-sm">
      <div className="flex flex-col gap-2 border-b border-slate-100 pb-4 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <h2 className="text-xl font-bold text-slate-950">
            شاخص‌های سلامت و نمودارها
          </h2>
          <p className="mt-2 text-sm leading-7 text-slate-600">
            ثبت دستی داده‌های قابل ترند برای پیگیری طولی، تصمیم‌های مراقبتی و
            اتصال‌های آینده به آزمایش، دستگاه و اتوماسیون.
          </p>
        </div>
        <button
          className="inline-flex h-10 items-center justify-center rounded-md border border-slate-300 px-4 text-sm font-semibold text-slate-700 transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:text-slate-400"
          disabled={isLoading}
          onClick={() => {
            void loadMeasurements();
          }}
          type="button"
        >
          به‌روزرسانی
        </button>
      </div>

      <div className="mt-5">
        <MeasurementQuickTrends groups={quickTrendGroups} />
      </div>

      <div className="mt-5">
        <MeasurementCreateForm
          onCreated={handleCreated}
          patientId={patientId}
        />
      </div>

      <div className="mt-5 grid gap-3 lg:grid-cols-[minmax(0,18rem)_1fr]">
        <Field label="فیلتر نوع شاخص">
          <select
            className="h-10 rounded-md border border-slate-300 bg-white px-3 text-slate-950 outline-none transition focus:border-teal-600 focus:ring-2 focus:ring-teal-100"
            onChange={(event) => setMeasurementTypeFilter(event.target.value)}
            value={measurementTypeFilter}
          >
            <option value="">همه شاخص‌ها</option>
            {availableMeasurementTypes.map((option) => (
              <option key={option} value={option}>
                {option}
              </option>
            ))}
          </select>
        </Field>
        <MeasurementTrend
          measurementType={trendMeasurementType}
          measurements={trendMeasurements}
        />
      </div>

      <div className="mt-5 space-y-3">
        {isLoading ? (
          <div className="rounded-md border border-slate-200 bg-slate-50 p-4 text-sm text-slate-600">
            در حال دریافت شاخص‌های سلامت...
          </div>
        ) : null}

        {!isLoading && errorMessage ? (
          <div className="rounded-md border border-rose-200 bg-rose-50 p-4 text-sm leading-7 text-rose-900">
            {errorMessage}
          </div>
        ) : null}

        {!isLoading && !errorMessage && measurements.length === 0 ? (
          <div className="rounded-md border border-dashed border-slate-300 bg-slate-50 p-4 text-sm leading-7 text-slate-600">
            هنوز شاخص سلامت قابل نمایش برای این بیمار ثبت نشده است.
          </div>
        ) : null}

        {!isLoading &&
        !errorMessage &&
        measurements.length > 0 &&
        visibleMeasurements.length === 0 ? (
          <div className="rounded-md border border-dashed border-slate-300 bg-slate-50 p-4 text-sm leading-7 text-slate-600">
            شاخصی با این نوع برای بیمار پیدا نشد.
          </div>
        ) : null}

        {!isLoading && !errorMessage
          ? visibleMeasurements.map((measurement) => (
              <MeasurementCard
                key={measurement.id}
                measurement={measurement}
              />
            ))
          : null}
      </div>
    </section>
  );
}
