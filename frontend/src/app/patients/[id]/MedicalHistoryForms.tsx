"use client";

import { useRouter } from "next/navigation";
import type { FormEvent, ReactNode } from "react";
import { useState } from "react";
import PersianDateInput from "@/components/PersianDateInput";
import {
  createAllergy,
  createCondition,
  createMedication,
  type AllergySummary,
  type ConditionSummary,
  type CreateAllergyInput,
  type CreateConditionInput,
  type CreateMedicationInput,
  type MedicationSummary,
  type PatientSummary,
} from "@/lib/api";
import { formatDate, formatNullable } from "@/lib/format";
import {
  allergySeverityOptions,
  allergyTypeOptions,
  conditionStatusOptions,
  medicationRouteOptions,
  selectPlaceholder,
  sensitivityLevelOptions,
  sourceTypeOptions,
  verificationStatusOptions,
  getHealthOptionLabel,
  type HealthOption,
} from "@/lib/health-options";
import PatientDocuments from "./PatientDocuments";
import PatientParaclinicalResults from "./PatientParaclinicalResults";

const emptyConditionForm: CreateConditionInput = {
  name: "",
  status: "",
  startedYear: "",
  treatmentSummary: "",
  clinicianNote: "",
};

const emptyAllergyForm: CreateAllergyInput = {
  allergen: "",
  allergyType: "",
  severity: "",
  reactionDescription: "",
  clinicianNote: "",
};

const emptyMedicationForm: CreateMedicationInput = {
  name: "",
  dose: "",
  frequency: "",
  route: "",
  reason: "",
  startDate: "",
  isCurrent: true,
  clinicianNote: "",
};

type MedicalHistoryTab =
  | "conditions"
  | "allergies"
  | "medications"
  | "other"
  | "evidence";

type AddPanel = "condition" | "allergy" | "medication" | null;

type TextFieldProps<T extends Record<string, string | boolean>> = {
  label: string;
  name: keyof T;
  value: string;
  required?: boolean;
  type?: string;
  placeholder?: string;
  onChange: (name: keyof T, value: string) => void;
};

const tabs: Array<{
  id: MedicalHistoryTab;
  label: string;
  count?: (summary: PatientSummary) => number;
}> = [
  {
    id: "conditions",
    label: "بیماری‌ها و مشکلات فعال",
    count: (summary) => summary.conditions.length,
  },
  {
    id: "allergies",
    label: "حساسیت‌ها",
    count: (summary) => summary.allergies.length,
  },
  {
    id: "medications",
    label: "داروها",
    count: (summary) => summary.currentMedications.length,
  },
  {
    id: "other",
    label: "سوابق دیگر",
  },
  {
    id: "evidence",
    label: "مدارک و نتایج",
  },
];

function getOptionLabel(options: HealthOption[], value: string | null | undefined) {
  return getHealthOptionLabel(options, value);
}

function formatMissing(value: string | number | null | undefined) {
  return formatNullable(value);
}

function TextField<T extends Record<string, string | boolean>>({
  label,
  name,
  value,
  required = false,
  type = "text",
  placeholder,
  onChange,
}: TextFieldProps<T>) {
  return (
    <label className="flex flex-col gap-1.5 text-sm font-medium text-slate-700">
      <span>
        {label}
        {required ? <span className="text-rose-600"> *</span> : null}
      </span>
      <input
        className="h-10 rounded-md border border-slate-300 bg-white px-3 text-slate-950 outline-none transition placeholder:text-slate-400 focus:border-teal-600 focus:ring-2 focus:ring-teal-100"
        name={String(name)}
        onChange={(event) => onChange(name, event.target.value)}
        placeholder={placeholder}
        required={required}
        type={type}
        value={value}
      />
    </label>
  );
}

function SelectField<T extends Record<string, string | boolean>>({
  label,
  name,
  value,
  options,
  onChange,
}: {
  label: string;
  name: keyof T;
  value: string;
  options: HealthOption[];
  onChange: (name: keyof T, value: string) => void;
}) {
  return (
    <label className="flex flex-col gap-1.5 text-sm font-medium text-slate-700">
      <span>{label}</span>
      <select
        className="h-10 rounded-md border border-slate-300 bg-white px-3 text-slate-950 outline-none transition focus:border-teal-600 focus:ring-2 focus:ring-teal-100"
        name={String(name)}
        onChange={(event) => onChange(name, event.target.value)}
        value={value}
      >
        <option value="">{selectPlaceholder}</option>
        {options.map((option) => (
          <option key={option.value} value={option.value}>
            {option.label}
          </option>
        ))}
      </select>
    </label>
  );
}

function TextAreaField<T extends Record<string, string | boolean>>({
  label,
  name,
  value,
  onChange,
}: {
  label: string;
  name: keyof T;
  value: string;
  onChange: (name: keyof T, value: string) => void;
}) {
  return (
    <label className="flex flex-col gap-1.5 text-sm font-medium text-slate-700">
      <span>{label}</span>
      <textarea
        className="min-h-20 resize-y rounded-md border border-slate-300 bg-white px-3 py-2 text-slate-950 outline-none transition placeholder:text-slate-400 focus:border-teal-600 focus:ring-2 focus:ring-teal-100"
        name={String(name)}
        onChange={(event) => onChange(name, event.target.value)}
        value={value}
      />
    </label>
  );
}

function FormNotice({
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

function EmptyState({ children }: { children: ReactNode }) {
  return (
    <div className="rounded-md border border-dashed border-slate-300 bg-slate-50 p-4 text-sm leading-7 text-slate-600">
      {children}
    </div>
  );
}

export function ConditionCreateForm({ patientId }: { patientId: string }) {
  const router = useRouter();
  const [form, setForm] = useState<CreateConditionInput>(emptyConditionForm);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  function updateField(name: keyof CreateConditionInput, value: string) {
    setForm((current) => ({
      ...current,
      [name]: value,
    }));
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setErrorMessage(null);
    setSuccessMessage(null);

    if (!form.name.trim()) {
      setErrorMessage("نام بیماری الزامی است.");
      return;
    }

    setIsSubmitting(true);

    try {
      await createCondition(patientId, form);
      setForm(emptyConditionForm);
      setSuccessMessage("بیماری با موفقیت ثبت شد.");
      router.refresh();
    } catch (error) {
      setErrorMessage(
        error instanceof Error
          ? `خطا در ثبت بیماری: ${error.message}`
          : "خطا در ثبت بیماری رخ داد.",
      );
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <form
      className="space-y-3 rounded-md border border-slate-200 bg-slate-50 p-3"
      onSubmit={handleSubmit}
    >
      <h3 className="font-bold text-slate-950">افزودن بیماری</h3>
      <FormNotice error={errorMessage} success={successMessage} />
      <TextField<CreateConditionInput>
        label="نام بیماری"
        name="name"
        onChange={updateField}
        required
        value={form.name}
      />
      <div className="grid gap-3 sm:grid-cols-2">
        <SelectField<CreateConditionInput>
          label="وضعیت"
          name="status"
          onChange={updateField}
          options={conditionStatusOptions}
          value={form.status}
        />
        <TextField<CreateConditionInput>
          label="سال شروع"
          name="startedYear"
          onChange={updateField}
          type="number"
          value={form.startedYear}
        />
      </div>
      <TextAreaField<CreateConditionInput>
        label="خلاصه درمان"
        name="treatmentSummary"
        onChange={updateField}
        value={form.treatmentSummary}
      />
      <TextAreaField<CreateConditionInput>
        label="یادداشت پزشک"
        name="clinicianNote"
        onChange={updateField}
        value={form.clinicianNote}
      />
      <button
        className="inline-flex h-10 w-full items-center justify-center rounded-md bg-teal-700 px-4 text-sm font-semibold text-white transition hover:bg-teal-800 disabled:cursor-not-allowed disabled:bg-slate-400"
        disabled={isSubmitting}
        type="submit"
      >
        {isSubmitting ? "در حال ثبت..." : "ثبت بیماری"}
      </button>
    </form>
  );
}

export function AllergyCreateForm({ patientId }: { patientId: string }) {
  const router = useRouter();
  const [form, setForm] = useState<CreateAllergyInput>(emptyAllergyForm);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  function updateField(name: keyof CreateAllergyInput, value: string) {
    setForm((current) => ({
      ...current,
      [name]: value,
    }));
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setErrorMessage(null);
    setSuccessMessage(null);

    if (!form.allergen.trim()) {
      setErrorMessage("نام آلرژن الزامی است.");
      return;
    }

    setIsSubmitting(true);

    try {
      await createAllergy(patientId, form);
      setForm(emptyAllergyForm);
      setSuccessMessage("آلرژی با موفقیت ثبت شد.");
      router.refresh();
    } catch (error) {
      setErrorMessage(
        error instanceof Error
          ? `خطا در ثبت آلرژی: ${error.message}`
          : "خطا در ثبت آلرژی رخ داد.",
      );
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <form
      className="space-y-3 rounded-md border border-slate-200 bg-slate-50 p-3"
      onSubmit={handleSubmit}
    >
      <h3 className="font-bold text-slate-950">افزودن حساسیت</h3>
      <FormNotice error={errorMessage} success={successMessage} />
      <TextField<CreateAllergyInput>
        label="آلرژن"
        name="allergen"
        onChange={updateField}
        required
        value={form.allergen}
      />
      <div className="grid gap-3 sm:grid-cols-2">
        <SelectField<CreateAllergyInput>
          label="نوع آلرژی"
          name="allergyType"
          onChange={updateField}
          options={allergyTypeOptions}
          value={form.allergyType}
        />
        <SelectField<CreateAllergyInput>
          label="شدت"
          name="severity"
          onChange={updateField}
          options={allergySeverityOptions}
          value={form.severity}
        />
      </div>
      <TextAreaField<CreateAllergyInput>
        label="شرح واکنش"
        name="reactionDescription"
        onChange={updateField}
        value={form.reactionDescription}
      />
      <TextAreaField<CreateAllergyInput>
        label="یادداشت پزشک"
        name="clinicianNote"
        onChange={updateField}
        value={form.clinicianNote}
      />
      <button
        className="inline-flex h-10 w-full items-center justify-center rounded-md bg-teal-700 px-4 text-sm font-semibold text-white transition hover:bg-teal-800 disabled:cursor-not-allowed disabled:bg-slate-400"
        disabled={isSubmitting}
        type="submit"
      >
        {isSubmitting ? "در حال ثبت..." : "ثبت حساسیت"}
      </button>
    </form>
  );
}

export function MedicationCreateForm({ patientId }: { patientId: string }) {
  const router = useRouter();
  const [form, setForm] = useState<CreateMedicationInput>(emptyMedicationForm);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  function updateField(name: keyof CreateMedicationInput, value: string) {
    setForm((current) => ({
      ...current,
      [name]: value,
    }));
  }

  function updateIsCurrent(value: boolean) {
    setForm((current) => ({
      ...current,
      isCurrent: value,
    }));
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setErrorMessage(null);
    setSuccessMessage(null);

    if (!form.name.trim()) {
      setErrorMessage("نام دارو الزامی است.");
      return;
    }

    setIsSubmitting(true);

    try {
      await createMedication(patientId, form);
      setForm(emptyMedicationForm);
      setSuccessMessage("دارو با موفقیت ثبت شد.");
      router.refresh();
    } catch (error) {
      setErrorMessage(
        error instanceof Error
          ? `خطا در ثبت دارو: ${error.message}`
          : "خطا در ثبت دارو رخ داد.",
      );
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <form
      className="space-y-3 rounded-md border border-slate-200 bg-slate-50 p-3"
      onSubmit={handleSubmit}
    >
      <h3 className="font-bold text-slate-950">افزودن دارو</h3>
      <FormNotice error={errorMessage} success={successMessage} />
      <TextField<CreateMedicationInput>
        label="نام دارو"
        name="name"
        onChange={updateField}
        required
        value={form.name}
      />
      <div className="grid gap-3 sm:grid-cols-2">
        <TextField<CreateMedicationInput>
          label="دوز"
          name="dose"
          onChange={updateField}
          placeholder="مثلا ۵ میلی‌گرم"
          value={form.dose}
        />
        <TextField<CreateMedicationInput>
          label="تکرار مصرف"
          name="frequency"
          onChange={updateField}
          placeholder="مثلا روزی یک بار"
          value={form.frequency}
        />
      </div>
      <div className="grid gap-3 sm:grid-cols-2">
        <SelectField<CreateMedicationInput>
          label="روش مصرف"
          name="route"
          onChange={updateField}
          options={medicationRouteOptions}
          value={form.route}
        />
        <PersianDateInput
          label="تاریخ شروع"
          onChange={(value) => updateField("startDate", value)}
          value={form.startDate}
        />
      </div>
      <TextField<CreateMedicationInput>
        label="علت مصرف"
        name="reason"
        onChange={updateField}
        value={form.reason}
      />
      <label className="flex items-center gap-2 text-sm font-medium text-slate-700">
        <input
          checked={form.isCurrent}
          className="h-4 w-4 rounded border-slate-300 text-teal-700 focus:ring-teal-600"
          onChange={(event) => updateIsCurrent(event.target.checked)}
          type="checkbox"
        />
        داروی فعلی است
      </label>
      <TextAreaField<CreateMedicationInput>
        label="یادداشت پزشک"
        name="clinicianNote"
        onChange={updateField}
        value={form.clinicianNote}
      />
      <button
        className="inline-flex h-10 w-full items-center justify-center rounded-md bg-teal-700 px-4 text-sm font-semibold text-white transition hover:bg-teal-800 disabled:cursor-not-allowed disabled:bg-slate-400"
        disabled={isSubmitting}
        type="submit"
      >
        {isSubmitting ? "در حال ثبت..." : "ثبت دارو"}
      </button>
    </form>
  );
}

function AddButton({
  isOpen,
  onClick,
  children,
}: {
  isOpen: boolean;
  onClick: () => void;
  children: ReactNode;
}) {
  return (
    <button
      className="inline-flex h-9 items-center justify-center rounded-md bg-teal-700 px-3 text-sm font-semibold text-white transition hover:bg-teal-800"
      onClick={onClick}
      type="button"
    >
      {isOpen ? "بستن فرم" : children}
    </button>
  );
}

function ConditionCard({ condition }: { condition: ConditionSummary }) {
  const [isExpanded, setIsExpanded] = useState(false);
  const hasDetails = Boolean(
    condition.treatmentSummary ||
      condition.clinicianNote ||
      condition.sourceType ||
      condition.verificationStatus ||
      condition.sensitivityLevel,
  );

  return (
    <article className="rounded-md border border-slate-200 bg-white p-3">
      <div className="flex flex-col gap-2 lg:flex-row lg:items-start lg:justify-between">
        <div>
          <div className="flex flex-wrap items-center gap-2">
            <h3 className="text-base font-bold text-slate-950">
              {condition.name}
            </h3>
            {condition.status === "Active" || condition.status === "Chronic" ? (
              <Badge tone="teal">فعال</Badge>
            ) : null}
          </div>
          <p className="mt-2 text-sm leading-7 text-slate-600">
            وضعیت: {getOptionLabel(conditionStatusOptions, condition.status)}
            {condition.startedYear ? ` · شروع: ${condition.startedYear}` : ""}
            {condition.clinicianNote ? " · یادداشت پزشک دارد" : ""}
          </p>
        </div>
        {hasDetails ? (
          <button
            className="inline-flex h-8 items-center justify-center rounded-md border border-slate-300 px-3 text-xs font-semibold text-slate-700 transition hover:bg-slate-50"
            onClick={() => setIsExpanded((current) => !current)}
            type="button"
          >
            {isExpanded ? "بستن جزئیات" : "جزئیات"}
          </button>
        ) : null}
      </div>

      {isExpanded ? (
        <div className="mt-3 rounded-md border border-slate-100 bg-slate-50 p-3">
          <dl className="grid gap-3 md:grid-cols-2">
            <MetaItem label="منبع" value={getOptionLabel(sourceTypeOptions, condition.sourceType)} />
            <MetaItem
              label="وضعیت تأیید"
              value={getOptionLabel(verificationStatusOptions, condition.verificationStatus)}
            />
            <MetaItem
              label="سطح حساسیت"
              value={getOptionLabel(sensitivityLevelOptions, condition.sensitivityLevel)}
            />
            {condition.treatmentSummary ? (
              <MetaItem label="خلاصه درمان" value={condition.treatmentSummary} />
            ) : null}
            {condition.clinicianNote ? (
              <MetaItem label="یادداشت پزشک" value={condition.clinicianNote} />
            ) : null}
          </dl>
        </div>
      ) : null}
    </article>
  );
}

function AllergyCard({ allergy }: { allergy: AllergySummary }) {
  const [isExpanded, setIsExpanded] = useState(false);
  const hasDetails = Boolean(
    allergy.reactionDescription ||
      allergy.clinicianNote ||
      allergy.sourceType ||
      allergy.verificationStatus ||
      allergy.sensitivityLevel,
  );
  const moderateOrSevere =
    allergy.severity === "Moderate" ||
    allergy.severity === "Severe" ||
    allergy.severity === "LifeThreatening";
  const severe =
    allergy.severity === "Severe" || allergy.severity === "LifeThreatening";

  return (
    <article className="rounded-md border border-slate-200 bg-white p-3">
      <div className="flex flex-col gap-2 lg:flex-row lg:items-start lg:justify-between">
        <div>
          <div className="flex flex-wrap items-center gap-2">
            <h3 className="text-base font-bold text-slate-950">
              {allergy.allergen}
            </h3>
            {moderateOrSevere ? (
              <Badge tone={severe ? "rose" : "amber"}>
                {getOptionLabel(allergySeverityOptions, allergy.severity)}
              </Badge>
            ) : null}
          </div>
          <p className="mt-2 text-sm leading-7 text-slate-600">
            {getOptionLabel(allergyTypeOptions, allergy.allergyType)}
            {allergy.reactionDescription
              ? ` · واکنش: ${allergy.reactionDescription}`
              : ""}
          </p>
        </div>
        {hasDetails ? (
          <button
            className="inline-flex h-8 items-center justify-center rounded-md border border-slate-300 px-3 text-xs font-semibold text-slate-700 transition hover:bg-slate-50"
            onClick={() => setIsExpanded((current) => !current)}
            type="button"
          >
            {isExpanded ? "بستن جزئیات" : "جزئیات"}
          </button>
        ) : null}
      </div>

      {isExpanded ? (
        <div className="mt-3 rounded-md border border-slate-100 bg-slate-50 p-3">
          <dl className="grid gap-3 md:grid-cols-2">
            <MetaItem label="منبع" value={getOptionLabel(sourceTypeOptions, allergy.sourceType)} />
            <MetaItem
              label="وضعیت تأیید"
              value={getOptionLabel(verificationStatusOptions, allergy.verificationStatus)}
            />
            <MetaItem
              label="سطح حساسیت"
              value={getOptionLabel(sensitivityLevelOptions, allergy.sensitivityLevel)}
            />
            <MetaItem
              label="شدت"
              value={getOptionLabel(allergySeverityOptions, allergy.severity)}
            />
            {allergy.reactionDescription ? (
              <MetaItem label="شرح واکنش" value={allergy.reactionDescription} />
            ) : null}
            {allergy.clinicianNote ? (
              <MetaItem label="یادداشت پزشک" value={allergy.clinicianNote} />
            ) : null}
          </dl>
        </div>
      ) : null}
    </article>
  );
}

function MedicationCard({ medication }: { medication: MedicationSummary }) {
  const [isExpanded, setIsExpanded] = useState(false);
  const startDate = formatDate(medication.startDate);
  const endDate = formatDate(medication.endDate);
  const hasDetails = Boolean(
    medication.reason ||
      medication.clinicianNote ||
      medication.startDate ||
      medication.endDate ||
      medication.sourceType ||
      medication.verificationStatus ||
      medication.sensitivityLevel,
  );

  return (
    <article className="rounded-md border border-slate-200 bg-white p-3">
      <div className="flex flex-col gap-2 lg:flex-row lg:items-start lg:justify-between">
        <div>
          <div className="flex flex-wrap items-center gap-2">
            <h3 className="text-base font-bold text-slate-950">
              {medication.name}
            </h3>
            <Badge tone={medication.isCurrent ? "emerald" : "slate"}>
              {medication.isCurrent ? "داروی فعلی" : "غیرفعال"}
            </Badge>
          </div>
          <p className="mt-2 text-sm leading-7 text-slate-600">
            دوز: {formatMissing(medication.dose)} · تکرار:{" "}
            {formatMissing(medication.frequency)}
            {medication.route
              ? ` · روش مصرف: ${getOptionLabel(medicationRouteOptions, medication.route)}`
              : ""}
          </p>
        </div>
        {hasDetails ? (
          <button
            className="inline-flex h-8 items-center justify-center rounded-md border border-slate-300 px-3 text-xs font-semibold text-slate-700 transition hover:bg-slate-50"
            onClick={() => setIsExpanded((current) => !current)}
            type="button"
          >
            {isExpanded ? "بستن جزئیات" : "جزئیات"}
          </button>
        ) : null}
      </div>

      {isExpanded ? (
        <div className="mt-3 rounded-md border border-slate-100 bg-slate-50 p-3">
          <dl className="grid gap-3 md:grid-cols-2">
            <MetaItem label="منبع" value={getOptionLabel(sourceTypeOptions, medication.sourceType)} />
            <MetaItem
              label="وضعیت تأیید"
              value={getOptionLabel(verificationStatusOptions, medication.verificationStatus)}
            />
            <MetaItem
              label="سطح حساسیت"
              value={getOptionLabel(sensitivityLevelOptions, medication.sensitivityLevel)}
            />
            <MetaItem
              label="روش مصرف"
              value={getOptionLabel(medicationRouteOptions, medication.route)}
            />
            {medication.reason ? (
              <MetaItem label="علت مصرف" value={medication.reason} />
            ) : null}
            {startDate ? <MetaItem label="تاریخ شروع" value={startDate} /> : null}
            {endDate ? <MetaItem label="تاریخ پایان" value={endDate} /> : null}
            {medication.clinicianNote ? (
              <MetaItem label="یادداشت پزشک" value={medication.clinicianNote} />
            ) : null}
          </dl>
        </div>
      ) : null}
    </article>
  );
}

function ConditionsTab({
  items,
  patientId,
  openPanel,
  setOpenPanel,
}: {
  items: ConditionSummary[];
  patientId: string;
  openPanel: AddPanel;
  setOpenPanel: (panel: AddPanel) => void;
}) {
  const isOpen = openPanel === "condition";

  return (
    <div className="space-y-3">
      <div className="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <p className="text-sm text-slate-600">
            {items.length} مورد ثبت‌شده
          </p>
        </div>
        <AddButton
          isOpen={isOpen}
          onClick={() => setOpenPanel(isOpen ? null : "condition")}
        >
          افزودن بیماری
        </AddButton>
      </div>
      {isOpen ? <ConditionCreateForm patientId={patientId} /> : null}
      {items.length === 0 ? (
        <EmptyState>هنوز موردی ثبت نشده است.</EmptyState>
      ) : (
        <div className="space-y-3">
          {items.map((condition) => (
            <ConditionCard condition={condition} key={condition.id} />
          ))}
        </div>
      )}
    </div>
  );
}

function AllergiesTab({
  items,
  patientId,
  openPanel,
  setOpenPanel,
}: {
  items: AllergySummary[];
  patientId: string;
  openPanel: AddPanel;
  setOpenPanel: (panel: AddPanel) => void;
}) {
  const isOpen = openPanel === "allergy";

  return (
    <div className="space-y-3">
      <div className="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <p className="text-sm text-slate-600">
            {items.length} مورد ثبت‌شده
          </p>
        </div>
        <AddButton
          isOpen={isOpen}
          onClick={() => setOpenPanel(isOpen ? null : "allergy")}
        >
          افزودن حساسیت
        </AddButton>
      </div>
      {isOpen ? <AllergyCreateForm patientId={patientId} /> : null}
      {items.length === 0 ? (
        <EmptyState>هنوز موردی ثبت نشده است.</EmptyState>
      ) : (
        <div className="space-y-3">
          {items.map((allergy) => (
            <AllergyCard allergy={allergy} key={allergy.id} />
          ))}
        </div>
      )}
    </div>
  );
}

function MedicationsTab({
  items,
  patientId,
  openPanel,
  setOpenPanel,
}: {
  items: MedicationSummary[];
  patientId: string;
  openPanel: AddPanel;
  setOpenPanel: (panel: AddPanel) => void;
}) {
  const isOpen = openPanel === "medication";

  return (
    <div className="space-y-3">
      <div className="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <p className="text-sm text-slate-600">
            {items.length} داروی فعلی
          </p>
        </div>
        <AddButton
          isOpen={isOpen}
          onClick={() => setOpenPanel(isOpen ? null : "medication")}
        >
          افزودن دارو
        </AddButton>
      </div>
      {isOpen ? <MedicationCreateForm patientId={patientId} /> : null}
      {items.length === 0 ? (
        <EmptyState>هنوز موردی ثبت نشده است.</EmptyState>
      ) : (
        <div className="space-y-3">
          {items.map((medication) => (
            <MedicationCard medication={medication} key={medication.id} />
          ))}
        </div>
      )}
    </div>
  );
}

function OtherHistoryTab() {
  return (
    <div className="rounded-md border border-dashed border-slate-300 bg-slate-50 p-5 text-sm leading-7 text-slate-600">
      سوابق جراحی، بستری، واکسیناسیون، سابقه خانوادگی و اجتماعی در فازهای بعدی
      به همین بخش اضافه می‌شوند.
    </div>
  );
}

function EvidenceTab({ patientId }: { patientId: string }) {
  return (
    <div className="space-y-4">
      <div className="rounded-md border border-slate-200 bg-slate-50 p-4">
        <p className="text-sm leading-7 text-slate-600">
          مدارک پزشکی و نتایج پاراکلینیک شواهد پرونده هستند. مدل‌ها و APIها جدا
          باقی مانده‌اند.
        </p>
      </div>
      <section className="space-y-3">
        <PatientDocuments patientId={patientId} />
      </section>
      <section className="space-y-3">
        <PatientParaclinicalResults patientId={patientId} />
      </section>
    </div>
  );
}

export default function MedicalHistoryForms({
  summary,
}: {
  summary: PatientSummary;
}) {
  const [activeTab, setActiveTab] = useState<MedicalHistoryTab>("conditions");
  const [openPanel, setOpenPanel] = useState<AddPanel>(null);

  return (
    <section className="rounded-lg border border-slate-200 bg-white p-4 shadow-sm">
      <div className="flex flex-col gap-2 border-b border-slate-100 pb-4 lg:flex-row lg:items-start lg:justify-between">
        <div>
          <p className="max-w-3xl text-sm leading-7 text-slate-600">
            بیماری‌ها، حساسیت‌ها، داروها و شواهد پزشکی بیمار در یک فضای
            فشرده‌تر و قابل مرور.
          </p>
        </div>
      </div>

      <div className="mt-4 flex gap-2 overflow-x-auto pb-1">
        {tabs.map((tab) => {
          const isActive = tab.id === activeTab;
          const count = tab.count?.(summary);

          return (
            <button
              className={`min-w-max rounded-md border px-3 py-2 text-sm font-semibold transition ${
                isActive
                  ? "border-teal-700 bg-teal-700 text-white"
                  : "border-slate-200 bg-white text-slate-700 hover:bg-slate-50"
              }`}
              key={tab.id}
              onClick={() => {
                setActiveTab(tab.id);
                setOpenPanel(null);
              }}
              type="button"
            >
              {tab.label}
              {count !== undefined ? (
                <span
                  className={`mr-2 rounded-md px-1.5 py-0.5 text-xs ${
                    isActive ? "bg-teal-600 text-white" : "bg-slate-100 text-slate-500"
                  }`}
                >
                  {count}
                </span>
              ) : null}
            </button>
          );
        })}
      </div>

      <div className="mt-4 rounded-md border border-slate-100 bg-white p-3">
        {activeTab === "conditions" ? (
          <ConditionsTab
            items={summary.conditions}
            openPanel={openPanel}
            patientId={summary.id}
            setOpenPanel={setOpenPanel}
          />
        ) : null}
        {activeTab === "allergies" ? (
          <AllergiesTab
            items={summary.allergies}
            openPanel={openPanel}
            patientId={summary.id}
            setOpenPanel={setOpenPanel}
          />
        ) : null}
        {activeTab === "medications" ? (
          <MedicationsTab
            items={summary.currentMedications}
            openPanel={openPanel}
            patientId={summary.id}
            setOpenPanel={setOpenPanel}
          />
        ) : null}
        {activeTab === "other" ? <OtherHistoryTab /> : null}
        {activeTab === "evidence" ? <EvidenceTab patientId={summary.id} /> : null}
      </div>
    </section>
  );
}
