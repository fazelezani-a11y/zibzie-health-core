"use client";

import { useEffect, useMemo, useState } from "react";
import Badge from "@/components/ui/Badge";
import Notice from "@/components/ui/Notice";
import {
  ApiError,
  listAuditLog,
  type AuditLogEntry,
  type AuditLogQueryResponse,
} from "@/lib/api";
import { formatDateTime } from "@/lib/format";

type OutcomeFilter = "all" | "succeeded" | "failed";

const PAGE_SIZE = 10;

function formatMissing(value: string | null | undefined) {
  return value && value.trim().length > 0 ? value : "ثبت نشده";
}

function formatActor(entry: AuditLogEntry) {
  if (entry.userId) {
    return `کاربر: ${entry.userId}`;
  }

  if (entry.serviceAccountId) {
    return `سرویس: ${entry.serviceAccountId}`;
  }

  return "عامل ثبت نشده";
}

function outcomeLabel(entry: AuditLogEntry) {
  return entry.succeeded ? "موفق" : "ناموفق/رد شده";
}

function outcomeTone(entry: AuditLogEntry): "success" | "danger" {
  return entry.succeeded ? "success" : "danger";
}

function buildOutcomeFilter(value: OutcomeFilter) {
  if (value === "succeeded") {
    return true;
  }

  if (value === "failed") {
    return false;
  }

  return null;
}

function AuditLogRow({ entry }: { entry: AuditLogEntry }) {
  return (
    <article className="rounded-md border border-slate-200 bg-white p-4">
      <div className="flex flex-col gap-3 md:flex-row md:items-start md:justify-between">
        <div>
          <div className="flex flex-wrap items-center gap-2">
            <Badge tone={outcomeTone(entry)}>{outcomeLabel(entry)}</Badge>
            <span className="text-sm font-bold text-slate-950">
              {entry.actionType} / {entry.resourceType}
            </span>
          </div>
          <p className="mt-2 text-xs leading-6 text-slate-500">
            {formatDateTime(entry.createdAt) || "زمان ثبت نشده"} ·{" "}
            {formatActor(entry)}
          </p>
        </div>
        <div className="text-left text-xs font-medium text-slate-500">
          {formatMissing(entry.httpMethod)} {formatMissing(entry.requestPath)}
        </div>
      </div>

      <dl className="mt-4 grid gap-3 text-xs md:grid-cols-3">
        <div className="rounded-md bg-slate-50 p-3">
          <dt className="font-medium text-slate-500">محصول / نقش</dt>
          <dd className="mt-1 font-bold text-slate-800">
            {formatMissing(entry.productCode)} / {formatMissing(entry.productRole)}
          </dd>
        </div>
        <div className="rounded-md bg-slate-50 p-3">
          <dt className="font-medium text-slate-500">مجوز</dt>
          <dd className="mt-1 font-bold text-slate-800">
            {formatMissing(entry.permission)}
          </dd>
        </div>
        <div className="rounded-md bg-slate-50 p-3">
          <dt className="font-medium text-slate-500">دامنه / شناسه منبع</dt>
          <dd className="mt-1 break-all font-bold text-slate-800">
            {formatMissing(entry.accessScope)} / {formatMissing(entry.resourceId)}
          </dd>
        </div>
      </dl>

      {!entry.succeeded && entry.failureReason ? (
        <p className="mt-3 rounded-md bg-rose-50 p-3 text-xs leading-6 text-rose-900">
          دلیل رد شدن: {entry.failureReason}
        </p>
      ) : null}

      {entry.correlationId ? (
        <p className="mt-3 break-all text-xs text-slate-500">
          Correlation ID: {entry.correlationId}
        </p>
      ) : null}
    </article>
  );
}

export default function PatientAuditLogPanel({
  patientId,
}: {
  patientId: string;
}) {
  const [data, setData] = useState<AuditLogQueryResponse | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [page, setPage] = useState(1);
  const [outcome, setOutcome] = useState<OutcomeFilter>("all");

  const succeeded = useMemo(() => buildOutcomeFilter(outcome), [outcome]);

  useEffect(() => {
    let isMounted = true;

    async function loadAuditLog() {
      setIsLoading(true);
      setErrorMessage(null);

      try {
        const response = await listAuditLog({
          patientId,
          page,
          pageSize: PAGE_SIZE,
          succeeded,
        });

        if (isMounted) {
          setData(response);
        }
      } catch (error) {
        if (isMounted) {
          setErrorMessage(
            error instanceof ApiError
              ? error.message
              : "دریافت گزارش امنیتی با خطا روبه‌رو شد.",
          );
        }
      } finally {
        if (isMounted) {
          setIsLoading(false);
        }
      }
    }

    loadAuditLog();

    return () => {
      isMounted = false;
    };
  }, [patientId, page, succeeded]);

  const items = data?.items ?? [];
  const canGoPrevious = page > 1;
  const canGoNext = data ? page < data.totalPages : false;

  function handleOutcomeChange(value: OutcomeFilter) {
    setOutcome(value);
    setPage(1);
  }

  return (
    <section className="rounded-md border border-slate-200 bg-white p-5">
      <div className="flex flex-col gap-4 md:flex-row md:items-start md:justify-between">
        <div>
          <p className="text-xs font-semibold uppercase tracking-wide text-teal-700">
            AuditLog
          </p>
          <h2 className="mt-1 text-lg font-bold text-slate-950">
            گزارش امنیتی بیمار
          </h2>
          <p className="mt-2 max-w-2xl text-sm leading-7 text-slate-600">
            این بخش برای بازبینی رخدادهای امنیتی و دسترسی است و با خط زمانی
            بالینی پرونده تفاوت دارد.
          </p>
        </div>
        <label className="text-sm font-medium text-slate-700">
          وضعیت
          <select
            className="mt-2 block min-w-40 rounded-md border border-slate-300 bg-white px-3 py-2 text-sm text-slate-900"
            value={outcome}
            onChange={(event) =>
              handleOutcomeChange(event.target.value as OutcomeFilter)
            }
          >
            <option value="all">همه رخدادها</option>
            <option value="succeeded">موفق</option>
            <option value="failed">ناموفق/رد شده</option>
          </select>
        </label>
      </div>

      <div className="mt-5 space-y-3">
        {isLoading ? (
          <Notice variant="loading">در حال دریافت گزارش امنیتی...</Notice>
        ) : errorMessage ? (
          <Notice variant="error">{errorMessage}</Notice>
        ) : items.length === 0 ? (
          <Notice variant="empty">رخداد امنیتی برای این فیلتر ثبت نشده است.</Notice>
        ) : (
          items.map((entry) => <AuditLogRow entry={entry} key={entry.id} />)
        )}
      </div>

      {data && data.totalCount > 0 ? (
        <div className="mt-4 flex flex-col gap-3 border-t border-slate-100 pt-4 text-sm text-slate-600 md:flex-row md:items-center md:justify-between">
          <span>
            صفحه {data.page} از {Math.max(data.totalPages, 1)} · {data.totalCount} رخداد
          </span>
          <div className="flex gap-2">
            <button
              className="rounded-md border border-slate-300 px-3 py-2 text-sm font-semibold text-slate-700 disabled:cursor-not-allowed disabled:opacity-50"
              disabled={!canGoPrevious || isLoading}
              onClick={() => setPage((current) => Math.max(current - 1, 1))}
              type="button"
            >
              قبلی
            </button>
            <button
              className="rounded-md border border-slate-300 px-3 py-2 text-sm font-semibold text-slate-700 disabled:cursor-not-allowed disabled:opacity-50"
              disabled={!canGoNext || isLoading}
              onClick={() => setPage((current) => current + 1)}
              type="button"
            >
              بعدی
            </button>
          </div>
        </div>
      ) : null}
    </section>
  );
}
