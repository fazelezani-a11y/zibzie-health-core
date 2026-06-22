import Link from "next/link";
import AdminAccessState from "@/components/AdminAccessState";
import type { PatientSummary } from "@/lib/api";
import {
  getPatientSummaryServer,
  ServerApiError,
} from "@/lib/api/server-client";
import PatientRecordShell from "./PatientRecordShell";

export const dynamic = "force-dynamic";

function PatientSummaryError({
  status,
  message,
}: {
  status?: number;
  message: string;
}) {
  if (status !== 404) {
    return (
      <main className="mx-auto flex w-full max-w-3xl flex-1 flex-col gap-6 px-4 py-6 sm:px-6 lg:px-8">
        <Link
          className="text-sm font-semibold text-teal-700 transition hover:text-teal-900"
          href="/patients"
        >
          بازگشت به فهرست بیماران
        </Link>
        <AdminAccessState status={status} message={message} />
      </main>
    );
  }

  return (
    <main className="mx-auto flex w-full max-w-3xl flex-1 flex-col gap-6 px-4 py-6 sm:px-6 lg:px-8">
      <Link
        className="text-sm font-semibold text-teal-700 transition hover:text-teal-900"
        href="/patients"
      >
        بازگشت به فهرست بیماران
      </Link>
      <section className="rounded-md border border-rose-200 bg-rose-50 p-6 text-rose-950">
        <h1 className="text-xl font-bold">بیمار پیدا نشد</h1>
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
  let errorStatus: number | undefined;

  try {
    summary = await getPatientSummaryServer(id);
  } catch (error) {
    errorStatus = error instanceof ServerApiError ? error.status : undefined;
    errorMessage =
      error instanceof Error
        ? error.message
        : "ارتباط با سرویس پرونده سلامت برقرار نشد.";
  }

  if (!summary) {
    return (
      <PatientSummaryError
        message={errorMessage ?? "خلاصه پرونده بیمار در دسترس نیست."}
        status={errorStatus}
      />
    );
  }

  return <PatientRecordShell summary={summary} />;
}
