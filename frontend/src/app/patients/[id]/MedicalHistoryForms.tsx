"use client";

import { useRouter } from "next/navigation";
import type { FormEvent } from "react";
import { useState } from "react";
import {
  createAllergy,
  createCondition,
  createMedication,
  type CreateAllergyInput,
  type CreateConditionInput,
  type CreateMedicationInput,
} from "@/lib/api";

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

type TextFieldProps<T extends Record<string, string | boolean>> = {
  label: string;
  name: keyof T;
  value: string;
  required?: boolean;
  type?: string;
  placeholder?: string;
  onChange: (name: keyof T, value: string) => void;
};

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
      className="mt-5 space-y-3 rounded-md border border-slate-200 bg-slate-50 p-3"
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
        <TextField<CreateConditionInput>
          label="وضعیت"
          name="status"
          onChange={updateField}
          placeholder="مثلا فعال"
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
      className="mt-5 space-y-3 rounded-md border border-slate-200 bg-slate-50 p-3"
      onSubmit={handleSubmit}
    >
      <h3 className="font-bold text-slate-950">افزودن آلرژی</h3>
      <FormNotice error={errorMessage} success={successMessage} />
      <TextField<CreateAllergyInput>
        label="آلرژن"
        name="allergen"
        onChange={updateField}
        required
        value={form.allergen}
      />
      <div className="grid gap-3 sm:grid-cols-2">
        <TextField<CreateAllergyInput>
          label="نوع آلرژی"
          name="allergyType"
          onChange={updateField}
          placeholder="مثلا دارویی"
          value={form.allergyType}
        />
        <TextField<CreateAllergyInput>
          label="شدت"
          name="severity"
          onChange={updateField}
          placeholder="مثلا متوسط"
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
        {isSubmitting ? "در حال ثبت..." : "ثبت آلرژی"}
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
      className="mt-5 space-y-3 rounded-md border border-slate-200 bg-slate-50 p-3"
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
        <TextField<CreateMedicationInput>
          label="روش مصرف"
          name="route"
          onChange={updateField}
          placeholder="مثلا خوراکی"
          value={form.route}
        />
        <TextField<CreateMedicationInput>
          label="تاریخ شروع"
          name="startDate"
          onChange={updateField}
          type="date"
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
