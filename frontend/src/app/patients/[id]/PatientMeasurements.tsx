"use client";

import { useRouter } from "next/navigation";
import type { FormEvent, ReactNode } from "react";
import { useCallback, useEffect, useMemo, useState } from "react";
import PersianDateInput from "@/components/PersianDateInput";
import Badge from "@/components/ui/Badge";
import Notice from "@/components/ui/Notice";
import {
  createPatientMeasurement,
  getPatientMeasurements,
  type CreatePatientMeasurementPayload,
  type PatientMeasurement,
} from "@/lib/api";
import {
  formatBooleanPersian,
  formatDateTime,
  formatNumberPersian,
} from "@/lib/format";
import {
  defaultPriorityMeasurementTypes,
  labMeasurementTypes,
  lifestyleMeasurementTypes,
  measurementSourceTypeOptions as sourceTypeOptions,
  measurementTypeOptions,
  measurementVerificationStatusOptions as verificationStatusOptions,
  selectPlaceholder,
  sensitivityLevelOptions,
  vitalSignMeasurementTypes,
  getHealthOptionLabel,
  type HealthOption,
} from "@/lib/health-options";

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

const abnormalOptions: HealthOption[] = [
  { label: "ثبت نشده", value: "" },
  { label: "بله", value: "true" },
  { label: "خیر", value: "false" },
];

type MeasurementFilterId =
  | "all"
  | "priority"
  | "vitals"
  | "lab"
  | "lifestyle"
  | "abnormal";

type MeasurementGroup = {
  measurementType: string;
  label: string;
  unit: string;
  latest: PatientMeasurement;
  points: PatientMeasurement[];
};

const primaryMeasurementFilters: Array<{ id: MeasurementFilterId; label: string }> = [
  { id: "all", label: "همه" },
  { id: "priority", label: "اولویت‌دار" },
  { id: "abnormal", label: "غیرطبیعی" },
  { id: "vitals", label: "علائم حیاتی" },
  { id: "lab", label: "آزمایشگاهی" },
];

const advancedMeasurementFilters: Array<{ id: MeasurementFilterId; label: string }> = [
  { id: "lifestyle", label: "سبک زندگی" },
];

const measurementFilters = [
  ...primaryMeasurementFilters,
  ...advancedMeasurementFilters,
];

function formatNumber(value: number) {
  return formatNumberPersian(value, {
    maximumFractionDigits: 3,
  });
}

function formatBoolean(value: boolean | null) {
  return formatBooleanPersian(value);
}

function valueWithUnit(measurement: PatientMeasurement) {
  return `${formatNumber(measurement.value)} ${measurement.unit}`;
}

function getOptionLabel(options: HealthOption[], value: string | null | undefined) {
  return getHealthOptionLabel(options, value);
}

function getMeasurementTypeLabel(value: string, displayName?: string | null) {
  if (displayName?.trim()) {
    return displayName;
  }

  return getOptionLabel(measurementTypeOptions, value);
}

function formatTargetRange(measurement: PatientMeasurement) {
  if (measurement.targetMin === null && measurement.targetMax === null) {
    return null;
  }

  return `${measurement.targetMin !== null ? formatNumber(measurement.targetMin) : "ثبت نشده"} تا ${
    measurement.targetMax !== null ? formatNumber(measurement.targetMax) : "ثبت نشده"
  }`;
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

function getMeasurementGroups(
  measurements: PatientMeasurement[],
): MeasurementGroup[] {
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
        label: getMeasurementTypeLabel(measurementType, latest?.displayName),
        unit: latest?.unit ?? "",
        latest,
        points,
      };
    })
    .filter(
      (group): group is MeasurementGroup =>
        Boolean(group.latest) && group.points.length > 0,
    )
    .sort((first, second) => {
      const firstAbnormal = first.latest.isAbnormal ? 1 : 0;
      const secondAbnormal = second.latest.isAbnormal ? 1 : 0;

      return secondAbnormal - firstAbnormal || second.points.length - first.points.length;
    });
}

function measurementMatchesFilter(
  group: MeasurementGroup,
  filter: MeasurementFilterId,
  priorityMeasurementTypes: string[],
) {
  switch (filter) {
    case "all":
      return true;
    case "priority":
      return priorityMeasurementTypes.includes(group.measurementType);
    case "vitals":
      return vitalSignMeasurementTypes.includes(group.measurementType);
    case "lab":
      return labMeasurementTypes.includes(group.measurementType);
    case "lifestyle":
      return lifestyleMeasurementTypes.includes(group.measurementType);
    case "abnormal":
      return group.points.some((measurement) => measurement.isAbnormal === true);
  }
}

function Sparkline({ group }: { group: MeasurementGroup }) {
  const width = 180;
  const height = 54;
  const values = group.points.map((point) => point.value);
  const minValue = Math.min(...values);
  const maxValue = Math.max(...values);
  const range = maxValue - minValue || 1;
  const chartPoints = group.points.map((point, index) => {
    const x =
      group.points.length === 1
        ? width / 2
        : (index / (group.points.length - 1)) * width;
    const y = 6 + ((maxValue - point.value) / range) * (height - 12);

    return {
      id: point.id,
      isAbnormal: point.isAbnormal,
      x,
      y,
    };
  });
  const linePoints = chartPoints.map((point) => `${point.x},${point.y}`).join(" ");

  return (
    <svg
      aria-label={`روند ${group.label}`}
      className="h-14 w-full rounded-md bg-slate-50"
      preserveAspectRatio="none"
      role="img"
      viewBox={`0 0 ${width} ${height}`}
    >
      {group.points.length >= 2 ? (
        <polyline
          fill="none"
          points={linePoints}
          stroke={group.latest.isAbnormal ? "#e11d48" : "#0f766e"}
          strokeLinecap="round"
          strokeLinejoin="round"
          strokeWidth="3"
        />
      ) : null}
      {chartPoints.map((point) => (
        <circle
          cx={point.x}
          cy={point.y}
          fill={point.isAbnormal ? "#e11d48" : "#0f766e"}
          key={point.id}
          r="3.5"
        />
      ))}
    </svg>
  );
}

function MeasurementGroupCard({
  group,
  isPriority,
  onSelect,
  onTogglePriority,
}: {
  group: MeasurementGroup;
  isPriority: boolean;
  onSelect: (measurementType: string) => void;
  onTogglePriority: (measurementType: string) => void;
}) {
  const latestMeasuredAt = formatDateTime(group.latest.measuredAt);
  const targetRange = formatTargetRange(group.latest);
  const hasTrend = group.points.length >= 2;

  return (
    <article className="rounded-md border border-slate-200 bg-white p-3">
      <div className="flex items-start justify-between gap-3">
        <div>
          <div className="flex flex-wrap items-center gap-2">
            <h3 className="text-sm font-bold text-slate-950">{group.label}</h3>
            {isPriority ? <Badge tone="info">اولویت</Badge> : null}
            {group.latest.isAbnormal ? <Badge tone="danger">غیرطبیعی</Badge> : null}
          </div>
          <p className="mt-1 text-xs text-slate-500">
            {formatNumber(group.points.length)} داده · آخرین ثبت:{" "}
            {latestMeasuredAt ?? group.latest.measuredAt}
          </p>
        </div>
        <div className="text-left">
          <p className="text-lg font-bold text-teal-800">
            {formatNumber(group.latest.value)}
          </p>
          <p className="text-xs font-medium text-slate-500">{group.unit}</p>
        </div>
      </div>

      <div className="mt-3">
        <Sparkline group={group} />
      </div>

      <div className="mt-3 flex flex-wrap items-center justify-between gap-2">
        <div className="text-xs leading-6 text-slate-500">
          {hasTrend ? "روند قابل مشاهده است." : "برای روند، داده بیشتری لازم است."}
          {targetRange ? <span> هدف: {targetRange}</span> : null}
        </div>
        <div className="flex flex-wrap gap-2">
          <button
            className="h-8 rounded-md border border-slate-200 px-3 text-xs font-semibold text-slate-700 transition hover:bg-slate-50"
            onClick={() => onSelect(group.measurementType)}
            type="button"
          >
            نمایش روند
          </button>
          <button
            className={`h-8 rounded-md border px-3 text-xs font-semibold transition ${
              isPriority
                ? "border-teal-200 bg-teal-50 text-teal-800 hover:bg-teal-100"
                : "border-slate-200 text-slate-700 hover:bg-slate-50"
            }`}
            onClick={() => onTogglePriority(group.measurementType)}
            type="button"
          >
            {isPriority ? "حذف از نمای کلی" : "نمایش در نمای کلی"}
          </button>
        </div>
      </div>
    </article>
  );
}

function MeasurementTrend({
  group,
}: {
  group: MeasurementGroup | null;
}) {
  if (!group) {
    return (
      <Notice variant="empty">
        برای نمایش روند، ابتدا یک نوع شاخص انتخاب کنید.
      </Notice>
    );
  }

  const points = group.points;

  if (points.length < 2) {
    return (
      <Notice variant="empty">
        برای نمایش روند {group.label} حداقل دو اندازه‌گیری لازم است.
      </Notice>
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
    const y = paddingTop + ((maxValue - point.value) / range) * plotHeight;

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
    <div className="rounded-md border border-slate-200 bg-slate-50 p-4">
      <div className="flex flex-col gap-2 sm:flex-row sm:items-start sm:justify-between">
        <div>
          <h3 className="text-base font-bold text-slate-950">
            روند {group.label}
          </h3>
          <p className="mt-1 text-sm leading-7 text-slate-600">
            آخرین مقدار: {valueWithUnit(latest)} در{" "}
            {formatDateTime(latest.measuredAt) ?? latest.measuredAt}
          </p>
        </div>
        <Badge tone={latest.isAbnormal ? "danger" : "info"}>
          {latest.displayName}: {valueWithUnit(latest)}
        </Badge>
      </div>

      <div className="mt-4 overflow-x-auto">
        <svg
          aria-label={`روند ${group.label}`}
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
            stroke={latest.isAbnormal ? "#e11d48" : "#0f766e"}
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
  const [isExpanded, setIsExpanded] = useState(false);
  const measuredAt = formatDateTime(measurement.measuredAt);
  const targetRange = formatTargetRange(measurement);

  return (
    <article className="rounded-md border border-slate-200 bg-white p-4 shadow-sm">
      <div
        className={`mb-3 h-1 rounded-full ${
          measurement.isAbnormal ? "bg-rose-300" : "bg-teal-200"
        }`}
      />
      <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
        <div>
          <h3 className="text-base font-bold text-slate-950">
            {measurement.displayName}
          </h3>
          <p className="mt-2 text-sm leading-7 text-slate-600">
            {getMeasurementTypeLabel(measurement.measurementType)} ·{" "}
            {measuredAt ?? measurement.measuredAt}
            {targetRange ? ` · هدف: ${targetRange}` : ""}
          </p>
        </div>
        <div className="flex shrink-0 flex-wrap gap-2">
          <Badge tone={measurement.isAbnormal ? "danger" : "info"}>
            {valueWithUnit(measurement)}
          </Badge>
          {measurement.isAbnormal ? (
            <Badge tone="danger">غیرطبیعی</Badge>
          ) : null}
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
        <dl className="mt-4 grid gap-3 rounded-md border border-slate-100 bg-slate-50 p-3 sm:grid-cols-2 lg:grid-cols-4">
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
          <MetaItem
            label="منبع"
            value={getOptionLabel(sourceTypeOptions, measurement.sourceType)}
          />
          <MetaItem
            label="وضعیت تأیید"
            value={getOptionLabel(
              verificationStatusOptions,
              measurement.verificationStatus,
            )}
          />
          <MetaItem
            label="سطح حساسیت"
            value={getOptionLabel(
              sensitivityLevelOptions,
              measurement.sensitivityLevel,
            )}
          />
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
      ) : null}
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
  const [form, setForm] = useState<CreatePatientMeasurementPayload>(emptyForm);
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
      className="rounded-md border border-slate-200 bg-slate-50 p-4"
      onSubmit={handleSubmit}
    >
      <h3 className="text-base font-bold text-slate-950">
        ثبت شاخص سلامت جدید
      </h3>
      <div className="mt-3">
        <FormNotice error={errorMessage} success={successMessage} />
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
        <PersianDateInput
          label="زمان اندازه‌گیری"
          mode="datetime"
          onChange={(value) => updateForm("measuredAt", value)}
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
  priorityMeasurementTypes,
  onPriorityMeasurementTypesChange,
}: {
  patientId: string;
  priorityMeasurementTypes?: string[];
  onPriorityMeasurementTypesChange?: (measurementTypes: string[]) => void;
}) {
  const [measurements, setMeasurements] = useState<PatientMeasurement[]>([]);
  const [selectedFilter, setSelectedFilter] = useState<MeasurementFilterId>("all");
  const [selectedMeasurementType, setSelectedMeasurementType] = useState("");
  const [showAdvancedFilters, setShowAdvancedFilters] = useState(false);
  const [isCreateOpen, setIsCreateOpen] = useState(false);
  const [localPriorityMeasurementTypes, setLocalPriorityMeasurementTypes] =
    useState<string[]>(defaultPriorityMeasurementTypes);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const activePriorityMeasurementTypes =
    priorityMeasurementTypes ?? localPriorityMeasurementTypes;

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
    const configuredValues = new Set(
      measurementTypeOptions.map((option) => option.value),
    );
    const customTypes = measurements
      .map((measurement) => measurement.measurementType)
      .filter((measurementType) => !configuredValues.has(measurementType));

    return [
      ...measurementTypeOptions,
      ...Array.from(new Set(customTypes)).map((measurementType) => ({
        value: measurementType,
        label: measurementType,
      })),
    ];
  }, [measurements]);

  const measurementGroups = useMemo(
    () => getMeasurementGroups(measurements),
    [measurements],
  );

  const selectedGroup = useMemo(() => {
    const selectedType =
      selectedMeasurementType || measurementGroups[0]?.measurementType || "";

    return (
      measurementGroups.find((group) => group.measurementType === selectedType) ??
      null
    );
  }, [measurementGroups, selectedMeasurementType]);

  const filteredGroups = useMemo(
    () =>
      measurementGroups
        .filter((group) =>
          measurementMatchesFilter(
            group,
            selectedFilter,
            activePriorityMeasurementTypes,
          ),
        )
        .sort((first, second) => {
          const firstPriority = activePriorityMeasurementTypes.includes(
            first.measurementType,
          )
            ? 1
            : 0;
          const secondPriority = activePriorityMeasurementTypes.includes(
            second.measurementType,
          )
            ? 1
            : 0;

          return secondPriority - firstPriority;
        }),
    [activePriorityMeasurementTypes, measurementGroups, selectedFilter],
  );
  const activeFilterDefinition = measurementFilters.find(
    (filter) => filter.id === selectedFilter,
  );

  const visibleMeasurements = useMemo(() => {
    if (!selectedMeasurementType) {
      return measurements;
    }

    return measurements.filter(
      (measurement) => measurement.measurementType === selectedMeasurementType,
    );
  }, [measurements, selectedMeasurementType]);

  function setPriorityTypes(nextTypes: string[]) {
    if (onPriorityMeasurementTypesChange) {
      onPriorityMeasurementTypesChange(nextTypes);
      return;
    }

    setLocalPriorityMeasurementTypes(nextTypes);
  }

  function togglePriorityMeasurementType(measurementType: string) {
    const nextTypes = activePriorityMeasurementTypes.includes(measurementType)
      ? activePriorityMeasurementTypes.filter((type) => type !== measurementType)
      : [...activePriorityMeasurementTypes, measurementType];

    setPriorityTypes(nextTypes);
  }

  async function handleCreated(createdMeasurementType: string) {
    setSelectedMeasurementType(createdMeasurementType);
    await loadMeasurements();
  }

  return (
    <section className="rounded-md border border-slate-200 bg-white p-4 shadow-sm">
      <div className="flex flex-col gap-3 border-b border-slate-100 pb-4 lg:flex-row lg:items-end lg:justify-between">
        <div>
          <p className="text-sm leading-7 text-slate-600">
            مرکز پیگیری شاخص‌های سلامت، روندهای قابل مشاهده و مقدارهای قابل اتصال
            به آزمایش، دستگاه و اتوماسیون در نسخه‌های بعدی.
          </p>
        </div>
        <div className="flex flex-wrap gap-2">
          <button
            className="inline-flex h-9 items-center justify-center rounded-md border border-slate-300 px-4 text-sm font-semibold text-slate-700 transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:text-slate-400"
            disabled={isLoading}
            onClick={() => {
              void loadMeasurements();
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
            {isCreateOpen ? "بستن فرم" : "ثبت مقدار جدید"}
          </button>
        </div>
      </div>

      <div className="mt-4 rounded-md border border-slate-200 bg-slate-50 p-3 text-sm leading-7 text-slate-600">
        نتایج آزمایش قابل روندگیری در نسخه‌های بعدی به شاخص‌ها متصل می‌شوند.
      </div>

      <div className="mt-4 flex flex-wrap items-center gap-2 text-xs">
        <button
          className="rounded-md border border-slate-200 bg-white px-3 py-2 font-semibold text-slate-700 transition hover:bg-slate-50"
          onClick={() => setShowAdvancedFilters((current) => !current)}
          type="button"
        >
          {showAdvancedFilters ? "بستن فیلتر پیشرفته" : "فیلتر پیشرفته"}
        </button>
        {selectedFilter !== "all" ? (
          <>
            <span className="font-medium text-slate-500">
              فیلتر فعال است: {activeFilterDefinition?.label}
            </span>
            <button
              className="font-semibold text-teal-700 transition hover:text-teal-900"
              onClick={() => setSelectedFilter("all")}
              type="button"
            >
              حذف فیلتر
            </button>
          </>
        ) : null}
      </div>

      {showAdvancedFilters ? (
        <div className="mt-2 flex flex-wrap gap-2 rounded-md border border-slate-200 bg-slate-50 p-2">
          {measurementFilters.map((filter) => {
            const isActive = filter.id === selectedFilter;
            const count = measurementGroups.filter((group) =>
              measurementMatchesFilter(
                group,
                filter.id,
                activePriorityMeasurementTypes,
              ),
            ).length;

            return (
              <button
                className={`rounded-md border px-3 py-2 text-xs font-semibold transition ${
                  isActive
                    ? "border-teal-700 bg-teal-700 text-white"
                    : "border-slate-200 bg-white text-slate-700 hover:bg-slate-50"
                }`}
                key={filter.id}
                onClick={() => setSelectedFilter(filter.id)}
                type="button"
              >
                {filter.label}
                <span
                  className={`mr-2 rounded-md px-1.5 py-0.5 ${
                    isActive ? "bg-teal-600 text-white" : "bg-slate-100 text-slate-500"
                  }`}
                >
                  {formatNumber(count)}
                </span>
              </button>
            );
          })}
        </div>
      ) : null}

      <div className="mt-4 grid gap-3 lg:grid-cols-2 xl:grid-cols-3">
        {filteredGroups.length > 0 ? (
          filteredGroups.map((group) => (
            <MeasurementGroupCard
              group={group}
              isPriority={activePriorityMeasurementTypes.includes(
                group.measurementType,
              )}
              key={group.measurementType}
              onSelect={setSelectedMeasurementType}
              onTogglePriority={togglePriorityMeasurementType}
            />
          ))
        ) : (
          <div className="lg:col-span-2 xl:col-span-3">
            <Notice variant="empty">
              در این فیلتر شاخصی برای نمایش وجود ندارد.
            </Notice>
          </div>
        )}
      </div>

      <div className="mt-5">
        <MeasurementTrend group={selectedGroup} />
      </div>

      {isCreateOpen ? (
        <div className="mt-5">
          <MeasurementCreateForm
            onCreated={handleCreated}
            patientId={patientId}
          />
        </div>
      ) : null}

      <div className="mt-5 grid gap-3 lg:grid-cols-[minmax(0,18rem)_1fr]">
        <Field label="فیلتر فهرست ثبت‌ها">
          <select
            className="h-10 rounded-md border border-slate-300 bg-white px-3 text-slate-950 outline-none transition focus:border-teal-600 focus:ring-2 focus:ring-teal-100"
            onChange={(event) => setSelectedMeasurementType(event.target.value)}
            value={selectedMeasurementType}
          >
            <option value="">همه شاخص‌ها</option>
            {availableMeasurementTypes.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>
        </Field>
        <div className="rounded-md border border-slate-200 bg-slate-50 p-3 text-sm leading-7 text-slate-600">
          برای محدود کردن فهرست، یک نوع شاخص را انتخاب کنید.
        </div>
      </div>

      <div className="mt-4 space-y-3">
        {isLoading ? (
          <Notice variant="loading">
            در حال دریافت شاخص‌های سلامت...
          </Notice>
        ) : null}

        {!isLoading && errorMessage ? (
          <Notice variant="error">{errorMessage}</Notice>
        ) : null}

        {!isLoading && !errorMessage && measurements.length === 0 ? (
          <Notice variant="empty">
            هنوز شاخص سلامت قابل نمایش برای این بیمار ثبت نشده است.
          </Notice>
        ) : null}

        {!isLoading &&
        !errorMessage &&
        measurements.length > 0 &&
        visibleMeasurements.length === 0 ? (
          <Notice variant="empty">
            شاخصی با این نوع برای بیمار پیدا نشد.
          </Notice>
        ) : null}

        {!isLoading && !errorMessage
          ? visibleMeasurements.map((measurement) => (
              <MeasurementCard key={measurement.id} measurement={measurement} />
            ))
          : null}
      </div>
    </section>
  );
}
