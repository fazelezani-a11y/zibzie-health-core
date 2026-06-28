"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import type { FormEvent } from "react";
import { useState } from "react";
import AdminSessionBar from "@/components/AdminSessionBar";
import PersianDateInput from "@/components/PersianDateInput";
import { createPatient, type CreatePatientInput } from "@/lib/api";
import {
  bloodTypeOptions,
  genderOptions,
  maritalStatusOptions,
  selectPlaceholder,
  type HealthOption,
} from "@/lib/health-options";

const emptyForm: CreatePatientInput = {
  firstName: "",
  lastName: "",
  birthDate: "",
  nationalCode: "",
  gender: "",
  bloodType: "",
  maritalStatus: "",
  educationLevel: "",
  occupation: "",
  mobileNumber: "",
  email: "",
  emergencyContactName: "",
  emergencyContactPhone: "",
  homeAddress: "",
  workAddress: "",
};

type TextFieldProps = {
  label: string;
  name: keyof CreatePatientInput;
  value: string;
  required?: boolean;
  type?: string;
  placeholder?: string;
  onChange: (name: keyof CreatePatientInput, value: string) => void;
};

function TextField({
  label,
  name,
  value,
  required = false,
  type = "text",
  placeholder,
  onChange,
}: TextFieldProps) {
  return (
    <label className="flex flex-col gap-2 text-sm font-medium text-slate-700">
      <span>
        {label}
        {required ? <span className="text-rose-600"> *</span> : null}
      </span>
      <input
        className="h-11 rounded-md border border-slate-300 bg-white px-3 text-slate-950 outline-none transition placeholder:text-slate-400 focus:border-teal-600 focus:ring-2 focus:ring-teal-100"
        name={name}
        onChange={(event) => onChange(name, event.target.value)}
        placeholder={placeholder}
        required={required}
        type={type}
        value={value}
      />
    </label>
  );
}

function TextAreaField({
  label,
  name,
  value,
  onChange,
}: {
  label: string;
  name: keyof CreatePatientInput;
  value: string;
  onChange: (name: keyof CreatePatientInput, value: string) => void;
}) {
  return (
    <label className="flex flex-col gap-2 text-sm font-medium text-slate-700 sm:col-span-2">
      <span>{label}</span>
      <textarea
        className="min-h-24 resize-y rounded-md border border-slate-300 bg-white px-3 py-3 text-slate-950 outline-none transition placeholder:text-slate-400 focus:border-teal-600 focus:ring-2 focus:ring-teal-100"
        name={name}
        onChange={(event) => onChange(name, event.target.value)}
        value={value}
      />
    </label>
  );
}

function SelectField({
  label,
  name,
  value,
  options,
  onChange,
}: {
  label: string;
  name: keyof CreatePatientInput;
  value: string;
  options: HealthOption[];
  onChange: (name: keyof CreatePatientInput, value: string) => void;
}) {
  return (
    <label className="flex flex-col gap-2 text-sm font-medium text-slate-700">
      <span>{label}</span>
      <select
        className="h-11 rounded-md border border-slate-300 bg-white px-3 text-slate-950 outline-none transition focus:border-teal-600 focus:ring-2 focus:ring-teal-100"
        name={name}
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

export default function NewPatientPage() {
  const router = useRouter();
  const [form, setForm] = useState<CreatePatientInput>(emptyForm);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  function updateField(name: keyof CreatePatientInput, value: string) {
    setForm((current) => ({
      ...current,
      [name]: value,
    }));
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setErrorMessage(null);

    if (!form.firstName.trim() || !form.lastName.trim() || !form.mobileNumber.trim()) {
      setErrorMessage("نام، نام خانوادگی و شماره موبایل الزامی هستند.");
      return;
    }

    setIsSubmitting(true);

    try {
      const patient = await createPatient(form);
      router.push(`/patients/${patient.id}`);
    } catch (error) {
      setErrorMessage(
        error instanceof Error
          ? error.message
          : "ثبت بیمار با خطا روبه‌رو شد.",
      );
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <main className="mx-auto flex w-full max-w-5xl flex-1 flex-col gap-6 px-4 py-6 sm:px-6 lg:px-8">
      <AdminSessionBar />

      <section className="flex flex-col gap-4 border-b border-slate-200 pb-6 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <Link
            className="text-sm font-semibold text-teal-700 transition hover:text-teal-900"
            href="/patients"
          >
            بازگشت به فهرست بیماران
          </Link>
          <h1 className="mt-3 text-2xl font-bold text-slate-950 sm:text-3xl">
            ثبت بیمار جدید
          </h1>
          <p className="mt-2 max-w-2xl text-sm leading-7 text-slate-600">
            اطلاعات پایه و تماس بیمار را برای ایجاد پرونده سلامت وارد کنید.
          </p>
        </div>
      </section>

      <form
        className="rounded-lg border border-slate-200 bg-white p-4 shadow-sm sm:p-6"
        onSubmit={handleSubmit}
      >
        {errorMessage ? (
          <div className="mb-5 rounded-md border border-rose-200 bg-rose-50 p-4 text-sm leading-7 text-rose-900">
            {errorMessage}
          </div>
        ) : null}

        <div className="grid gap-4 sm:grid-cols-2">
          <TextField
            label="نام"
            name="firstName"
            onChange={updateField}
            required
            value={form.firstName}
          />
          <TextField
            label="نام خانوادگی"
            name="lastName"
            onChange={updateField}
            required
            value={form.lastName}
          />
          <PersianDateInput
            label="تاریخ تولد"
            onChange={(value) => updateField("birthDate", value)}
            value={form.birthDate}
          />
          <TextField
            label="کد ملی"
            name="nationalCode"
            onChange={updateField}
            value={form.nationalCode}
          />
          <SelectField
            label="جنسیت"
            name="gender"
            onChange={updateField}
            options={genderOptions}
            value={form.gender}
          />
          <SelectField
            label="گروه خونی"
            name="bloodType"
            onChange={updateField}
            options={bloodTypeOptions}
            value={form.bloodType}
          />
          <SelectField
            label="وضعیت تاهل"
            name="maritalStatus"
            onChange={updateField}
            options={maritalStatusOptions}
            value={form.maritalStatus}
          />
          <TextField
            label="تحصیلات"
            name="educationLevel"
            onChange={updateField}
            value={form.educationLevel}
          />
          <TextField
            label="شغل"
            name="occupation"
            onChange={updateField}
            value={form.occupation}
          />
          <TextField
            label="موبایل"
            name="mobileNumber"
            onChange={updateField}
            required
            type="tel"
            value={form.mobileNumber}
          />
          <TextField
            label="ایمیل"
            name="email"
            onChange={updateField}
            type="email"
            value={form.email}
          />
          <TextField
            label="نام تماس اضطراری"
            name="emergencyContactName"
            onChange={updateField}
            value={form.emergencyContactName}
          />
          <TextField
            label="تلفن تماس اضطراری"
            name="emergencyContactPhone"
            onChange={updateField}
            type="tel"
            value={form.emergencyContactPhone}
          />
          <TextAreaField
            label="آدرس منزل"
            name="homeAddress"
            onChange={updateField}
            value={form.homeAddress}
          />
          <TextAreaField
            label="آدرس محل کار"
            name="workAddress"
            onChange={updateField}
            value={form.workAddress}
          />
        </div>

        <div className="mt-6 flex flex-col-reverse gap-3 sm:flex-row sm:justify-end">
          <Link
            className="inline-flex h-11 items-center justify-center rounded-md border border-slate-300 px-4 text-sm font-semibold text-slate-700 transition hover:bg-slate-50"
            href="/patients"
          >
            انصراف
          </Link>
          <button
            className="inline-flex h-11 items-center justify-center rounded-md bg-teal-700 px-5 text-sm font-semibold text-white shadow-sm transition hover:bg-teal-800 disabled:cursor-not-allowed disabled:bg-slate-400"
            disabled={isSubmitting}
            type="submit"
          >
            {isSubmitting ? "در حال ثبت..." : "ثبت بیمار"}
          </button>
        </div>
      </form>
    </main>
  );
}
