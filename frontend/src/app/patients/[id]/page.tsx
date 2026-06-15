import Link from "next/link";
import {
  ApiError,
  getPatientSummary,
  type AllergySummary,
  type ConditionSummary,
  type MedicationSummary,
  type PatientSummary,
} from "@/lib/api";
import {
  AllergyCreateForm,
  ConditionCreateForm,
  MedicationCreateForm,
} from "./MedicalHistoryForms";
import PatientCarePlan from "./PatientCarePlan";
import PatientDocuments from "./PatientDocuments";
import PatientParaclinicalResults from "./PatientParaclinicalResults";
import PatientTimeline from "./PatientTimeline";

export const dynamic = "force-dynamic";

function formatDate(value: string | null) {
  if (!value) {
    return "ثبت نشده";
  }

  const date = new Date(`${value}T00:00:00`);

  if (Number.isNaN(date.getTime())) {
    return value;
  }

  return new Intl.DateTimeFormat("fa-IR").format(date);
}

function InfoItem({
  label,
  value,
}: {
  label: string;
  value: string | null | undefined;
}) {
  return (
    <div className="rounded-lg border border-slate-200 bg-white p-4 shadow-sm">
      <dt className="text-xs font-medium text-slate-500">{label}</dt>
      <dd className="mt-2 text-sm font-bold text-slate-950">
        {value || "ثبت نشده"}
      </dd>
    </div>
  );
}

function EmptySection({ text }: { text: string }) {
  return (
    <div className="rounded-md border border-dashed border-slate-300 bg-slate-50 p-4 text-sm leading-7 text-slate-600">
      {text}
    </div>
  );
}

function ConditionsSection({
  items,
  patientId,
}: {
  items: ConditionSummary[];
  patientId: string;
}) {
  return (
    <section className="rounded-lg border border-slate-200 bg-white p-4 shadow-sm">
      <h2 className="text-lg font-bold text-slate-950">بیماری‌ها</h2>
      <div className="mt-4 space-y-3">
        {items.length === 0 ? (
          <EmptySection text="بیماری فعالی برای این بیمار ثبت نشده است." />
        ) : (
          items.map((condition) => (
            <article
              className="rounded-md border border-slate-100 bg-slate-50 p-3"
              key={condition.id}
            >
              <h3 className="font-semibold text-slate-950">{condition.name}</h3>
              <p className="mt-2 text-sm leading-7 text-slate-600">
                وضعیت: {condition.status || "ثبت نشده"}
              </p>
              <p className="text-sm leading-7 text-slate-600">
                سال شروع: {condition.startedYear ?? "ثبت نشده"}
              </p>
            </article>
          ))
        )}
      </div>
      <ConditionCreateForm patientId={patientId} />
    </section>
  );
}

function AllergiesSection({
  items,
  patientId,
}: {
  items: AllergySummary[];
  patientId: string;
}) {
  return (
    <section className="rounded-lg border border-slate-200 bg-white p-4 shadow-sm">
      <h2 className="text-lg font-bold text-slate-950">آلرژی‌ها</h2>
      <div className="mt-4 space-y-3">
        {items.length === 0 ? (
          <EmptySection text="آلرژی ثبت‌شده‌ای برای این بیمار وجود ندارد." />
        ) : (
          items.map((allergy) => (
            <article
              className="rounded-md border border-slate-100 bg-slate-50 p-3"
              key={allergy.id}
            >
              <h3 className="font-semibold text-slate-950">{allergy.allergen}</h3>
              <p className="mt-2 text-sm leading-7 text-slate-600">
                نوع: {allergy.allergyType || "ثبت نشده"}
              </p>
              <p className="text-sm leading-7 text-slate-600">
                شدت: {allergy.severity || "ثبت نشده"}
              </p>
            </article>
          ))
        )}
      </div>
      <AllergyCreateForm patientId={patientId} />
    </section>
  );
}

function MedicationsSection({
  items,
  patientId,
}: {
  items: MedicationSummary[];
  patientId: string;
}) {
  return (
    <section className="rounded-lg border border-slate-200 bg-white p-4 shadow-sm">
      <h2 className="text-lg font-bold text-slate-950">داروهای فعلی</h2>
      <div className="mt-4 space-y-3">
        {items.length === 0 ? (
          <EmptySection text="داروی فعلی برای این بیمار ثبت نشده است." />
        ) : (
          items.map((medication) => (
            <article
              className="rounded-md border border-slate-100 bg-slate-50 p-3"
              key={medication.id}
            >
              <h3 className="font-semibold text-slate-950">{medication.name}</h3>
              <p className="mt-2 text-sm leading-7 text-slate-600">
                دوز: {medication.dose || "ثبت نشده"}
              </p>
              <p className="text-sm leading-7 text-slate-600">
                تکرار مصرف: {medication.frequency || "ثبت نشده"}
              </p>
            </article>
          ))
        )}
      </div>
      <MedicationCreateForm patientId={patientId} />
    </section>
  );
}

function PatientSummaryView({ summary }: { summary: PatientSummary }) {
  const emergencyContact = [
    summary.emergencyContactName,
    summary.emergencyContactPhone,
  ]
    .filter(Boolean)
    .join(" - ");

  return (
    <main className="mx-auto flex w-full max-w-6xl flex-1 flex-col gap-6 px-4 py-6 sm:px-6 lg:px-8">
      <section className="flex flex-col gap-4 border-b border-slate-200 pb-6 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <Link
            className="text-sm font-semibold text-teal-700 transition hover:text-teal-900"
            href="/patients"
          >
            بازگشت به فهرست بیماران
          </Link>
          <h1 className="mt-3 text-2xl font-bold text-slate-950 sm:text-3xl">
            {summary.fullName || "پرونده بیمار"}
          </h1>
          <p className="mt-2 text-sm leading-7 text-slate-600">
            خلاصه اطلاعات پایه، تماس و سوابق مهم سلامت بیمار.
          </p>
        </div>
      </section>

      <dl className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
        <InfoItem label="موبایل" value={summary.mobileNumber} />
        <InfoItem label="کد ملی" value={summary.nationalCode} />
        <InfoItem label="تاریخ تولد" value={formatDate(summary.birthDate)} />
        <InfoItem label="جنسیت" value={summary.gender} />
        <InfoItem label="گروه خونی" value={summary.bloodType} />
        <InfoItem label="آدرس منزل" value={summary.homeAddress} />
        <InfoItem label="تماس اضطراری" value={emergencyContact} />
        <InfoItem label="ایمیل" value={summary.email} />
      </dl>

      <PatientTimeline patientId={summary.id} />

      <PatientDocuments patientId={summary.id} />

      <PatientParaclinicalResults patientId={summary.id} />

      <PatientCarePlan patientId={summary.id} />

      <section className="grid gap-4 lg:grid-cols-3">
        <ConditionsSection items={summary.conditions} patientId={summary.id} />
        <AllergiesSection items={summary.allergies} patientId={summary.id} />
        <MedicationsSection
          items={summary.currentMedications}
          patientId={summary.id}
        />
      </section>
    </main>
  );
}

function PatientSummaryError({
  isNotFound,
  message,
}: {
  isNotFound: boolean;
  message: string;
}) {
  return (
    <main className="mx-auto flex w-full max-w-3xl flex-1 flex-col gap-6 px-4 py-6 sm:px-6 lg:px-8">
      <Link
        className="text-sm font-semibold text-teal-700 transition hover:text-teal-900"
        href="/patients"
      >
        بازگشت به فهرست بیماران
      </Link>
      <section className="rounded-lg border border-rose-200 bg-rose-50 p-6 text-rose-950">
        <h1 className="text-xl font-bold">
          {isNotFound ? "بیمار پیدا نشد" : "خطا در دریافت خلاصه پرونده"}
        </h1>
        <p className="mt-3 text-sm leading-7">{message}</p>
      </section>
    </main>
  );
}

export default async function PatientPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;
  let summary: PatientSummary | null = null;
  let errorMessage: string | null = null;
  let isNotFound = false;

  try {
    summary = await getPatientSummary(id);
  } catch (error) {
    isNotFound = error instanceof ApiError && error.status === 404;
    errorMessage =
      error instanceof Error
        ? error.message
        : "ارتباط با سرویس پرونده سلامت برقرار نشد.";
  }

  if (!summary) {
    return (
      <PatientSummaryError
        isNotFound={isNotFound}
        message={errorMessage ?? "خلاصه پرونده بیمار در دسترس نیست."}
      />
    );
  }

  return <PatientSummaryView summary={summary} />;
}
