"use client";

import Link from "next/link";
import { useEffect, useMemo, useState } from "react";
import AdminSessionBar from "@/components/AdminSessionBar";
import PatientAccessGrantsPanel from "@/components/PatientAccessGrantsPanel";
import Badge from "@/components/ui/Badge";
import Notice from "@/components/ui/Notice";
import SectionHeader from "@/components/ui/SectionHeader";
import {
  getPatientCarePlan,
  getPatientMeasurements,
  getPatientParaclinicalResults,
  getPatientReminders,
  type CarePlanItem,
  type ParaclinicalResult,
  type PatientMeasurement,
  type PatientReminder,
  type PatientSummary,
} from "@/lib/api";
import { formatDate, formatDateTime, formatNullable } from "@/lib/format";
import { defaultPriorityMeasurementTypes } from "@/lib/health-options";
import MedicalHistoryForms from "./MedicalHistoryForms";
import PatientCarePlan from "./PatientCarePlan";
import PatientMeasurements from "./PatientMeasurements";
import PatientReminders from "./PatientReminders";
import PatientTimeline from "./PatientTimeline";

type SectionId =
  | "overview"
  | "care-plan"
  | "medical-history"
  | "measurements"
  | "reminders"
  | "timeline"
  | "access"
  | "personal";

type BadgeTone =
  | "default"
  | "success"
  | "warning"
  | "danger"
  | "info"
  | "muted";

type OverviewData = {
  carePlanItems: CarePlanItem[];
  reminders: PatientReminder[];
  measurements: PatientMeasurement[];
  paraclinicalResults: ParaclinicalResult[];
  isLoading: boolean;
  errorMessage: string | null;
};

type SummaryItem = {
  id: string;
  title: string;
  description?: string | null;
  meta?: string | null;
  targetSection?: SectionId;
  tone?: BadgeTone;
};

type SummaryItemWithDue = SummaryItem & {
  dueAt: string | null;
};

type MeasurementTrendGroup = {
  measurementType: string;
  displayName: string;
  unit: string;
  latest: PatientMeasurement;
  points: PatientMeasurement[];
};

const sections: Array<{
  id: SectionId;
  label: string;
  description: string;
}> = [
  {
    id: "overview",
    label: "نمای کلی",
    description: "جمع‌بندی سریع پرونده",
  },
  {
    id: "care-plan",
    label: "پلن مراقبتی",
    description: "اقدام‌ها و پیگیری‌ها",
  },
  {
    id: "medical-history",
    label: "سوابق پزشکی",
    description: "شرح حال، دارو، مدارک و نتایج",
  },
  {
    id: "measurements",
    label: "شاخص‌ها و نمودارها",
    description: "روند شاخص‌های سلامت",
  },
  {
    id: "reminders",
    label: "یادآورها و هشدارها",
    description: "پیگیری‌های زمان‌دار",
  },
  {
    id: "timeline",
    label: "خط زمانی پرونده",
    description: "نمای رویدادهای ثبت‌شده",
  },
  {
    id: "access",
    label: "امنیت و دسترسی",
    description: "دسترسی‌های فعال و لغوشده بیمار",
  },
  {
    id: "personal",
    label: "اطلاعات شخصی",
    description: "اطلاعات هویتی و تماس",
  },
];

const emptyOverviewData: OverviewData = {
  carePlanItems: [],
  reminders: [],
  measurements: [],
  paraclinicalResults: [],
  isLoading: true,
  errorMessage: null,
};

function settledValue<T>(result: PromiseSettledResult<T[]>): T[] {
  return result.status === "fulfilled" ? result.value : [];
}

function hasFailed(...results: PromiseSettledResult<unknown>[]) {
  return results.some((result) => result.status === "rejected");
}

function formatMissing(value: string | number | null | undefined) {
  return formatNullable(value);
}

function formatCount(value: number) {
  return new Intl.NumberFormat("fa-IR").format(value);
}

function formatNumber(value: number) {
  return new Intl.NumberFormat("fa-IR", {
    maximumFractionDigits: 2,
  }).format(value);
}

function formatMeasurementTargetRange(measurement: PatientMeasurement) {
  if (measurement.targetMin === null && measurement.targetMax === null) {
    return null;
  }

  return `${measurement.targetMin !== null ? formatNumber(measurement.targetMin) : "ثبت نشده"} تا ${
    measurement.targetMax !== null
      ? formatNumber(measurement.targetMax)
      : "ثبت نشده"
  }`;
}

function formatBirthDate(value: string | null | undefined) {
  const formatted = formatDate(value);
  const age = calculateAge(value);

  if (!formatted && age === null) {
    return "ثبت نشده";
  }

  return [formatted, age !== null ? `${age} سال` : null]
    .filter(Boolean)
    .join(" / ");
}

function calculateAge(value: string | null | undefined) {
  if (!value) {
    return null;
  }

  const birthDate = new Date(`${value}T00:00:00`);

  if (Number.isNaN(birthDate.getTime())) {
    return null;
  }

  const today = new Date();
  let age = today.getFullYear() - birthDate.getFullYear();
  const monthDiff = today.getMonth() - birthDate.getMonth();

  if (
    monthDiff < 0 ||
    (monthDiff === 0 && today.getDate() < birthDate.getDate())
  ) {
    age -= 1;
  }

  return age >= 0 ? new Intl.NumberFormat("fa-IR").format(age) : null;
}

function isTerminalCarePlanStatus(status: string) {
  return ["Completed", "Cancelled"].includes(status);
}

function isDoneReminderStatus(status: string) {
  return ["Done", "Completed", "Cancelled"].includes(status);
}

function isPastDue(value: string | null) {
  if (!value) {
    return false;
  }

  const date = new Date(value);
  return !Number.isNaN(date.getTime()) && date.getTime() < Date.now();
}

function bySoonestDue<T extends { dueAt: string | null }>(items: T[]) {
  return [...items].sort((first, second) => {
    const firstTime = first.dueAt ? new Date(first.dueAt).getTime() : Infinity;
    const secondTime = second.dueAt ? new Date(second.dueAt).getTime() : Infinity;

    return firstTime - secondTime;
  });
}

function byMostRecentClinicalDate(results: ParaclinicalResult[]) {
  return [...results].sort((first, second) => {
    const firstTime = new Date(
      first.resultDate ?? first.performedAt ?? first.createdAt,
    ).getTime();
    const secondTime = new Date(
      second.resultDate ?? second.performedAt ?? second.createdAt,
    ).getTime();

    return secondTime - firstTime;
  });
}

function sortMeasurementsByDate(measurements: PatientMeasurement[]) {
  return [...measurements].sort((first, second) => {
    const firstTime = new Date(first.measuredAt).getTime();
    const secondTime = new Date(second.measuredAt).getTime();

    return (Number.isNaN(firstTime) ? 0 : firstTime) -
      (Number.isNaN(secondTime) ? 0 : secondTime);
  });
}

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
      const points = sortMeasurementsByDate(group);
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

function InfoItem({
  label,
  value,
}: {
  label: string;
  value: string | null | undefined;
}) {
  return (
    <div className="rounded-md border border-slate-200 bg-white p-3">
      <dt className="text-xs font-medium text-slate-500">{label}</dt>
      <dd className="mt-1 text-sm font-bold text-slate-950">
        {value || "ثبت نشده"}
      </dd>
    </div>
  );
}

function SummaryCarouselCard({
  title,
  items,
  emptyText,
  onNavigate,
}: {
  title: string;
  items: SummaryItem[];
  emptyText: string;
  onNavigate?: (section: SectionId) => void;
}) {
  const [activeIndex, setActiveIndex] = useState(0);
  const safeActiveIndex = Math.min(activeIndex, Math.max(items.length - 1, 0));
  const activeItem = items[safeActiveIndex];
  const hasMultipleItems = items.length > 1;

  function showPrevious() {
    setActiveIndex((current) =>
      current === 0 ? Math.max(items.length - 1, 0) : current - 1,
    );
  }

  function showNext() {
    setActiveIndex((current) =>
      current >= items.length - 1 ? 0 : current + 1,
    );
  }

  return (
    <section className="rounded-md border border-slate-200 bg-white p-4">
      <div className="flex items-start justify-between gap-3">
        <div>
          <h3 className="text-base font-bold text-slate-950">{title}</h3>
          <p className="mt-1 text-xs font-medium text-slate-500">
            {formatCount(items.length)} مورد
          </p>
        </div>
        {hasMultipleItems ? (
          <Badge tone="muted">
            {formatCount(safeActiveIndex + 1)} / {formatCount(items.length)}
          </Badge>
        ) : null}
      </div>

      <div className="mt-4">
        {items.length === 0 || !activeItem ? (
          <Notice variant="empty">{emptyText}</Notice>
        ) : activeItem.targetSection ? (
          <button
            className="min-h-32 w-full rounded-md border border-slate-100 bg-slate-50 p-3 text-right transition hover:border-teal-200 hover:bg-teal-50/40"
            onClick={() => onNavigate?.(activeItem.targetSection!)}
            type="button"
          >
            <div className="flex flex-col gap-2 sm:flex-row sm:items-start sm:justify-between">
              <h4 className="text-sm font-bold text-slate-950">
                {activeItem.title}
              </h4>
              {activeItem.meta ? (
                <Badge tone={activeItem.tone}>{activeItem.meta}</Badge>
              ) : null}
            </div>
            {activeItem.description ? (
              <p className="mt-2 text-sm leading-7 text-slate-600">
                {activeItem.description}
              </p>
            ) : null}
            <span className="mt-3 inline-flex text-xs font-semibold text-teal-700">
              مشاهده بخش مرتبط
            </span>
          </button>
        ) : (
          <article className="min-h-32 rounded-md border border-slate-100 bg-slate-50 p-3">
            <div className="flex flex-col gap-2 sm:flex-row sm:items-start sm:justify-between">
              <h4 className="text-sm font-bold text-slate-950">
                {activeItem.title}
              </h4>
              {activeItem.meta ? (
                <Badge tone={activeItem.tone}>{activeItem.meta}</Badge>
              ) : null}
            </div>
            {activeItem.description ? (
              <p className="mt-2 text-sm leading-7 text-slate-600">
                {activeItem.description}
              </p>
            ) : null}
          </article>
        )}
      </div>

      {hasMultipleItems ? (
        <div className="mt-3 flex items-center justify-between gap-3">
          <div className="flex gap-1.5" aria-hidden="true">
            {items.map((item, index) => (
              <span
                className={`h-1.5 rounded-full transition-all ${
                  index === safeActiveIndex
                    ? "w-5 bg-teal-700"
                    : "w-1.5 bg-slate-300"
                }`}
                key={item.id}
              />
            ))}
          </div>
          <div className="flex gap-2">
            <button
              aria-label={`نمایش مورد قبلی ${title}`}
              className="inline-flex h-8 w-8 items-center justify-center rounded-md border border-slate-300 text-sm font-bold text-slate-700 transition hover:bg-slate-50"
              onClick={showPrevious}
              type="button"
            >
              →
            </button>
            <button
              aria-label={`نمایش مورد بعدی ${title}`}
              className="inline-flex h-8 w-8 items-center justify-center rounded-md border border-slate-300 text-sm font-bold text-slate-700 transition hover:bg-slate-50"
              onClick={showNext}
              type="button"
            >
              ←
            </button>
          </div>
        </div>
      ) : null}
    </section>
  );
}

function MiniTrendCard({
  group,
  onClick,
}: {
  group: MeasurementTrendGroup;
  onClick?: () => void;
}) {
  const width = 180;
  const height = 54;
  const values = group.points.map((point) => point.value);
  const minValue = Math.min(...values);
  const maxValue = Math.max(...values);
  const range = maxValue - minValue || 1;
  const targetRange = formatMeasurementTargetRange(group.latest);
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

  const content = (
    <>
      <div className="flex items-start justify-between gap-3">
        <div>
          <h4 className="text-sm font-bold text-slate-950">
            {group.displayName}
          </h4>
          <p className="mt-1 text-xs text-slate-500">
            {formatCount(group.points.length)} داده · آخرین ثبت:{" "}
            {formatDateTime(group.latest.measuredAt) ?? group.latest.measuredAt}
          </p>
        </div>
        <div className="flex flex-col items-end gap-1">
          <Badge tone={group.latest.isAbnormal ? "danger" : "info"}>
            {formatNumber(group.latest.value)} {group.unit}
          </Badge>
          {group.latest.isAbnormal ? <Badge tone="danger">غیرطبیعی</Badge> : null}
        </div>
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
      {targetRange ? (
        <p className="mt-2 text-xs leading-6 text-slate-500">
          هدف: {targetRange}
        </p>
      ) : null}
    </>
  );

  if (onClick) {
    return (
      <button
        className="w-full rounded-md border border-slate-200 bg-white p-3 text-right transition hover:border-teal-200 hover:bg-teal-50/40"
        onClick={onClick}
        type="button"
      >
        {content}
      </button>
    );
  }

  return (
    <article className="rounded-md border border-slate-200 bg-white p-3">
      {content}
    </article>
  );
}

function OverviewSection({
  onNavigateToSection,
  summary,
  priorityMeasurementTypes,
}: {
  onNavigateToSection: (section: SectionId) => void;
  summary: PatientSummary;
  priorityMeasurementTypes: string[];
}) {
  const [overviewData, setOverviewData] =
    useState<OverviewData>(emptyOverviewData);

  useEffect(() => {
    let isCurrent = true;

    async function loadOverviewData() {
      setOverviewData((current) => ({
        ...current,
        isLoading: true,
        errorMessage: null,
      }));

      const [carePlanResult, remindersResult, measurementsResult, resultsResult] =
        await Promise.allSettled([
          getPatientCarePlan(summary.id),
          getPatientReminders(summary.id),
          getPatientMeasurements(summary.id),
          getPatientParaclinicalResults(summary.id),
        ]);

      if (!isCurrent) {
        return;
      }

      setOverviewData({
        carePlanItems: settledValue(carePlanResult),
        reminders: settledValue(remindersResult),
        measurements: settledValue(measurementsResult),
        paraclinicalResults: settledValue(resultsResult),
        isLoading: false,
        errorMessage: hasFailed(
          carePlanResult,
          remindersResult,
          measurementsResult,
          resultsResult,
        )
          ? "بخشی از داده‌های نمای کلی در حال حاضر در دسترس نیست."
          : null,
      });
    }

    void loadOverviewData();

    return () => {
      isCurrent = false;
    };
  }, [summary.id]);

  const activeConditions = useMemo(
    () =>
      summary.conditions.filter((condition) => condition.status !== "Resolved"),
    [summary.conditions],
  );

  const importantAllergies = useMemo(
    () => summary.allergies.filter((allergy) => allergy.severity !== "Mild"),
    [summary.allergies],
  );

  const currentMedications = summary.currentMedications;

  const openCarePlanItems = overviewData.carePlanItems.filter(
    (item) => !isTerminalCarePlanStatus(item.status),
  );
  const openReminders = overviewData.reminders.filter(
    (reminder) => !isDoneReminderStatus(reminder.status),
  );
  const upcomingItems: SummaryItem[] = [
    ...bySoonestDue(openCarePlanItems.filter((item) => item.dueAt)).map(
      (item): SummaryItemWithDue => ({
        id: `care-plan-${item.id}`,
        title: item.title,
        description: `سررسید: ${formatDateTime(item.dueAt) ?? "ثبت نشده"} · وضعیت: ${formatMissing(
          item.status,
        )}`,
        meta: "پلن مراقبتی",
        targetSection: "care-plan",
        tone: isPastDue(item.dueAt) ? "danger" : "info",
        dueAt: item.dueAt,
      }),
    ),
    ...bySoonestDue(openReminders).map(
      (reminder): SummaryItemWithDue => ({
        id: `reminder-${reminder.id}`,
        title: reminder.title,
        description: `سررسید: ${formatDateTime(reminder.dueAt) ?? "ثبت نشده"} · وضعیت: ${formatMissing(
          reminder.status,
        )}`,
        meta: "یادآور",
        targetSection: "reminders",
        tone: isPastDue(reminder.dueAt) ? "danger" : "warning",
        dueAt: reminder.dueAt,
      }),
    ),
  ]
    .sort((first, second) => {
      const firstTime = first.dueAt ? new Date(first.dueAt).getTime() : Infinity;
      const secondTime = second.dueAt
        ? new Date(second.dueAt).getTime()
        : Infinity;

      return firstTime - secondTime;
    })
    .map(({ id, title, description, meta, targetSection, tone }) => ({
      id,
      title,
      description,
      meta,
      targetSection,
      tone,
    }));
  const attentionResults = byMostRecentClinicalDate(
    overviewData.paraclinicalResults.filter(
      (result) =>
        result.requiresFollowUp ||
        result.isAbnormal ||
        result.labItems.some((item) => item.isAbnormal),
    ),
  );
  const allMeasurementTrendGroups = getMeasurementTrendGroups(
    overviewData.measurements,
  );
  const measurementTrendGroups =
    priorityMeasurementTypes.length === 0
      ? []
      : allMeasurementTrendGroups
          .filter((group) =>
            priorityMeasurementTypes.includes(group.measurementType),
          )
          .slice(0, 4);

  return (
    <div className="space-y-5">
      <section className="rounded-md border border-slate-200 bg-white p-4">
        <div className="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
          <div>
            <div className="flex flex-wrap items-center gap-2">
              <h2 className="text-xl font-bold text-slate-950">
                {summary.fullName || "پرونده بیمار"}
              </h2>
              <Badge tone="success">پرونده فعال</Badge>
            </div>
            <p className="mt-2 text-sm leading-7 text-slate-600">
              {formatBirthDate(summary.birthDate)} · جنسیت:{" "}
              {formatMissing(summary.gender)} · گروه خونی:{" "}
              {formatMissing(summary.bloodType)}
            </p>
          </div>
          <div className="grid gap-2 text-sm text-slate-600 sm:grid-cols-2 lg:min-w-80">
            <span>موبایل: {formatMissing(summary.mobileNumber)}</span>
            <span>کد ملی: {formatMissing(summary.nationalCode)}</span>
          </div>
        </div>
      </section>

      {overviewData.isLoading ? (
        <Notice variant="loading">در حال دریافت نمای کلی پرونده...</Notice>
      ) : null}

      {!overviewData.isLoading && overviewData.errorMessage ? (
        <Notice variant="info">{overviewData.errorMessage}</Notice>
      ) : null}

      <section className="grid gap-4 xl:grid-cols-3">
        <SummaryCarouselCard
          emptyText="بیماری فعالی در خلاصه پرونده ثبت نشده است."
          items={activeConditions.map((condition) => ({
            id: condition.id,
            title: condition.name,
            description: `وضعیت: ${formatMissing(condition.status)}`,
            meta:
              condition.startedYear !== null && condition.startedYear !== undefined
                ? `شروع: ${condition.startedYear}`
                : null,
            targetSection: "medical-history",
          }))}
          onNavigate={onNavigateToSection}
          title="بیماری‌های فعال"
        />
        <SummaryCarouselCard
          emptyText="آلرژی مهمی برای این بیمار ثبت نشده است."
          items={importantAllergies.map((allergy) => ({
            id: allergy.id,
            title: allergy.allergen,
            description: `نوع: ${formatMissing(allergy.allergyType)}`,
            meta: allergy.severity,
            targetSection: "medical-history",
            tone: allergy.severity === "Severe" ? "danger" : "warning",
          }))}
          onNavigate={onNavigateToSection}
          title="آلرژی‌های مهم"
        />
        <SummaryCarouselCard
          emptyText="داروی فعلی در خلاصه پرونده ثبت نشده است."
          items={currentMedications.map((medication) => ({
            id: medication.id,
            title: medication.name,
            description: `دوز: ${formatMissing(medication.dose)} · تکرار: ${formatMissing(
              medication.frequency,
            )}`,
            meta: medication.route,
            targetSection: "medical-history",
          }))}
          onNavigate={onNavigateToSection}
          title="داروهای فعلی"
        />
      </section>

      <section className="grid gap-4 xl:grid-cols-3">
        <SummaryCarouselCard
          emptyText="اقدام یا یادآور باز زمان‌دار پیدا نشد."
          items={upcomingItems}
          onNavigate={onNavigateToSection}
          title="اقدام‌ها و یادآورهای بعدی"
        />
        <SummaryCarouselCard
          emptyText="نتیجه غیرطبیعی یا نیازمند پیگیری در داده‌های فعلی پیدا نشد."
          items={attentionResults.map((result) => ({
            id: result.id,
            title: result.title,
            description: `تاریخ: ${
              formatDateTime(result.resultDate ?? result.performedAt) ??
              "ثبت نشده"
            }`,
            meta: result.isAbnormal ? "غیرطبیعی" : "نیازمند پیگیری",
            targetSection: "medical-history",
            tone: result.isAbnormal ? "danger" : "warning",
          }))}
          onNavigate={onNavigateToSection}
          title="نتایج نیازمند توجه"
        />
        <section className="rounded-md border border-slate-200 bg-white p-4">
          <div className="flex items-start justify-between gap-3">
            <div>
              <h3 className="text-base font-bold text-slate-950">
                شاخص‌ها و نمودارهای سریع
              </h3>
              <p className="mt-1 text-xs font-medium text-slate-500">
                {formatCount(measurementTrendGroups.length)} روند اولویت‌دار
              </p>
            </div>
            {measurementTrendGroups.length > 0 ? (
              <Badge tone="info">نمای کلی</Badge>
            ) : null}
          </div>
          <div className="mt-4 grid gap-3 sm:grid-cols-2 xl:grid-cols-1">
            {measurementTrendGroups.length > 0 ? (
              measurementTrendGroups.map((group) => (
                <MiniTrendCard
                  group={group}
                  key={group.measurementType}
                  onClick={() => onNavigateToSection("measurements")}
                />
              ))
            ) : (
              <Notice variant="empty">
                برای شاخص‌های اولویت‌دار هنوز روند قابل نمایش وجود ندارد.
              </Notice>
            )}
          </div>
        </section>
      </section>
    </div>
  );
}

function MedicalHistorySection({ summary }: { summary: PatientSummary }) {
  return <MedicalHistoryForms summary={summary} />;
}

function PersonalInfoSection({ summary }: { summary: PatientSummary }) {
  const emergencyContact = [
    summary.emergencyContactName,
    summary.emergencyContactPhone,
  ]
    .filter(Boolean)
    .join(" - ");

  return (
    <section className="rounded-md border border-slate-200 bg-white p-4">
      <p className="text-sm leading-7 text-slate-600">
        اطلاعات پایه و تماس که در خلاصه پرونده در دسترس است.
      </p>
      <dl className="mt-4 grid gap-3 sm:grid-cols-2 xl:grid-cols-3">
        <InfoItem label="نام کامل" value={summary.fullName} />
        <InfoItem label="تاریخ تولد / سن" value={formatBirthDate(summary.birthDate)} />
        <InfoItem label="جنسیت" value={summary.gender} />
        <InfoItem label="گروه خونی" value={summary.bloodType} />
        <InfoItem label="موبایل" value={summary.mobileNumber} />
        <InfoItem label="کد ملی" value={summary.nationalCode} />
        <InfoItem label="ایمیل" value={summary.email} />
        <InfoItem label="آدرس منزل" value={summary.homeAddress} />
        <InfoItem label="آدرس محل کار" value={summary.workAddress} />
        <InfoItem label="تماس اضطراری" value={emergencyContact} />
      </dl>
    </section>
  );
}

function SectionContent({
  activeSection,
  onNavigateToSection,
  onPriorityMeasurementTypesChange,
  priorityMeasurementTypes,
  summary,
}: {
  activeSection: SectionId;
  onNavigateToSection: (section: SectionId) => void;
  onPriorityMeasurementTypesChange: (measurementTypes: string[]) => void;
  priorityMeasurementTypes: string[];
  summary: PatientSummary;
}) {
  switch (activeSection) {
    case "overview":
      return (
        <OverviewSection
          onNavigateToSection={onNavigateToSection}
          priorityMeasurementTypes={priorityMeasurementTypes}
          summary={summary}
        />
      );
    case "care-plan":
      return <PatientCarePlan patientId={summary.id} />;
    case "medical-history":
      return <MedicalHistorySection summary={summary} />;
    case "measurements":
      return (
        <PatientMeasurements
          onPriorityMeasurementTypesChange={onPriorityMeasurementTypesChange}
          patientId={summary.id}
          priorityMeasurementTypes={priorityMeasurementTypes}
        />
      );
    case "reminders":
      return <PatientReminders patientId={summary.id} />;
    case "timeline":
      return <PatientTimeline patientId={summary.id} showCreateForm={false} />;
    case "access":
      return <PatientAccessGrantsPanel patientId={summary.id} />;
    case "personal":
      return <PersonalInfoSection summary={summary} />;
    default:
      return (
        <OverviewSection
          onNavigateToSection={onNavigateToSection}
          priorityMeasurementTypes={priorityMeasurementTypes}
          summary={summary}
        />
      );
  }
}

export default function PatientRecordShell({
  summary,
}: {
  summary: PatientSummary;
}) {
  const [activeSection, setActiveSection] = useState<SectionId>("overview");
  const [priorityMeasurementTypes, setPriorityMeasurementTypes] = useState<
    string[]
  >(defaultPriorityMeasurementTypes);
  const activeItem =
    sections.find((section) => section.id === activeSection) ?? sections[0];

  return (
    <main className="mx-auto flex w-full max-w-7xl flex-1 flex-col gap-5 px-4 py-5 sm:px-6 lg:px-8">
      <AdminSessionBar />

      <section className="rounded-md border border-slate-200 bg-white p-4">
        <div className="flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">
          <div>
            <Link
              className="text-sm font-semibold text-teal-700 transition hover:text-teal-900"
              href="/patients"
            >
              بازگشت به فهرست بیماران
            </Link>
            <div className="mt-3 flex flex-wrap items-center gap-2">
              <h1 className="text-2xl font-bold text-slate-950 sm:text-3xl">
                {summary.fullName || "پرونده بیمار"}
              </h1>
              <Badge tone="success">پرونده فعال</Badge>
            </div>
            <div className="mt-3 flex flex-wrap gap-2 text-xs text-slate-600">
              <span className="rounded-md bg-slate-100 px-2.5 py-1">
                {formatBirthDate(summary.birthDate)}
              </span>
              <span className="rounded-md bg-slate-100 px-2.5 py-1">
                جنسیت: {formatMissing(summary.gender)}
              </span>
              <span className="rounded-md bg-slate-100 px-2.5 py-1">
                گروه خونی: {formatMissing(summary.bloodType)}
              </span>
              <span className="rounded-md bg-slate-100 px-2.5 py-1">
                موبایل: {formatMissing(summary.mobileNumber)}
              </span>
            </div>
          </div>
          <div className="text-sm leading-7 text-slate-600 lg:max-w-sm">
            نمای پرونده برای تیم مراقبت؛ ماژول‌ها از مسیرهای داخلی همین صفحه
            قابل دسترسی هستند.
          </div>
        </div>
      </section>

      <div className="grid gap-5 lg:grid-cols-[240px_minmax(0,1fr)]">
        <aside className="lg:sticky lg:top-5 lg:self-start">
          <nav
            aria-label="ناوبری داخلی پرونده بیمار"
            className="flex gap-2 overflow-x-auto rounded-md border border-slate-200 bg-white p-2 lg:flex-col lg:overflow-visible"
          >
            {sections.map((section) => {
              const isActive = section.id === activeSection;

              return (
                <button
                  aria-current={isActive ? "page" : undefined}
                  className={`min-w-max rounded-md px-3 py-2 text-right text-sm transition lg:min-w-0 ${
                    isActive
                      ? "bg-teal-700 font-bold text-white"
                      : "text-slate-700 hover:bg-slate-50"
                  }`}
                  key={section.id}
                  onClick={() => setActiveSection(section.id)}
                  type="button"
                >
                  <span className="block">{section.label}</span>
                  <span
                    className={`mt-1 block text-xs ${
                      isActive ? "text-teal-50" : "text-slate-500"
                    }`}
                  >
                    {section.description}
                  </span>
                </button>
              );
            })}
          </nav>
        </aside>

        <section className="min-w-0 space-y-4">
          <SectionHeader
            description={activeItem.description}
            title={activeItem.label}
          />
          <SectionContent
            activeSection={activeSection}
            onNavigateToSection={setActiveSection}
            onPriorityMeasurementTypesChange={setPriorityMeasurementTypes}
            priorityMeasurementTypes={priorityMeasurementTypes}
            summary={summary}
          />
        </section>
      </div>
    </main>
  );
}
