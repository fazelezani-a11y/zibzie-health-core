import Link from "next/link";
import { getPatients, type PatientListItem } from "@/lib/api";

export const dynamic = "force-dynamic";

function formatBirthDate(value: string | null) {
  if (!value) {
    return "ثبت نشده";
  }

  const date = new Date(`${value}T00:00:00`);

  if (Number.isNaN(date.getTime())) {
    return value;
  }

  return new Intl.DateTimeFormat("fa-IR").format(date);
}

function DetailRow({
  label,
  value,
}: {
  label: string;
  value: string | null | undefined;
}) {
  return (
    <div className="flex items-center justify-between gap-4 text-sm">
      <span className="text-slate-500">{label}</span>
      <span className="font-medium text-slate-800">{value || "ثبت نشده"}</span>
    </div>
  );
}

function PatientCard({ patient }: { patient: PatientListItem }) {
  return (
    <article className="rounded-lg border border-slate-200 bg-white p-4 shadow-sm transition hover:border-teal-300 hover:shadow-md">
      <div className="flex items-start justify-between gap-3">
        <div>
          <h2 className="text-base font-bold text-slate-950">
            {patient.fullName || "بدون نام"}
          </h2>
          <p className="mt-1 text-xs text-slate-500">پرونده سلامت بیمار</p>
        </div>
        <span className="rounded-md bg-emerald-50 px-2.5 py-1 text-xs font-medium text-emerald-700">
          فعال
        </span>
      </div>

      <div className="mt-5 space-y-3 border-t border-slate-100 pt-4">
        <DetailRow label="موبایل" value={patient.mobileNumber} />
        <DetailRow label="کد ملی" value={patient.nationalCode} />
        <DetailRow label="تاریخ تولد" value={formatBirthDate(patient.birthDate)} />
      </div>
    </article>
  );
}

export default async function PatientsPage() {
  let patients: PatientListItem[] = [];
  let errorMessage: string | null = null;

  try {
    patients = await getPatients();
  } catch (error) {
    errorMessage =
      error instanceof Error
        ? error.message
        : "ارتباط با سرویس پرونده سلامت برقرار نشد.";
  }

  return (
    <main className="mx-auto flex w-full max-w-6xl flex-1 flex-col gap-6 px-4 py-6 sm:px-6 lg:px-8">
      <section className="flex flex-col gap-4 border-b border-slate-200 pb-6 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <p className="text-sm font-medium text-teal-700">Zibzie Health Core</p>
          <h1 className="mt-2 text-2xl font-bold text-slate-950 sm:text-3xl">
            فهرست بیماران
          </h1>
          <p className="mt-2 max-w-2xl text-sm leading-7 text-slate-600">
            نمای اولیه پرونده‌های سلامت برای مشاهده سریع اطلاعات پایه بیماران.
          </p>
        </div>

        <Link
          className="inline-flex h-11 items-center justify-center rounded-md bg-teal-700 px-4 text-sm font-semibold text-white shadow-sm transition hover:bg-teal-800 focus:outline-none focus:ring-2 focus:ring-teal-600 focus:ring-offset-2"
          href="/patients/new"
        >
          ثبت بیمار جدید
        </Link>
      </section>

      {errorMessage ? (
        <section className="rounded-lg border border-rose-200 bg-rose-50 p-5 text-rose-900">
          <h2 className="text-base font-bold">خطا در دریافت اطلاعات</h2>
          <p className="mt-2 text-sm leading-7">{errorMessage}</p>
        </section>
      ) : null}

      {!errorMessage && patients.length === 0 ? (
        <section className="rounded-lg border border-dashed border-slate-300 bg-white p-8 text-center">
          <h2 className="text-lg font-bold text-slate-950">
            هنوز بیماری ثبت نشده است
          </h2>
          <p className="mt-2 text-sm leading-7 text-slate-600">
            پس از ثبت اولین بیمار، اطلاعات پایه پرونده در همین صفحه نمایش داده می‌شود.
          </p>
        </section>
      ) : null}

      {!errorMessage && patients.length > 0 ? (
        <section className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
          {patients.map((patient) => (
            <PatientCard key={patient.id} patient={patient} />
          ))}
        </section>
      ) : null}
    </main>
  );
}
