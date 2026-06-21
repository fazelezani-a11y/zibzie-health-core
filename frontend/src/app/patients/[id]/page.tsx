import Link from "next/link";
import type { PatientSummary } from "@/lib/api";
import {
  getPatientSummaryServer,
  ServerApiError,
} from "@/lib/api/server-client";
import PatientRecordShell from "./PatientRecordShell";

export const dynamic = "force-dynamic";

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
    summary = await getPatientSummaryServer(id);
  } catch (error) {
    isNotFound = error instanceof ServerApiError && error.status === 404;
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

  return <PatientRecordShell summary={summary} />;
}
