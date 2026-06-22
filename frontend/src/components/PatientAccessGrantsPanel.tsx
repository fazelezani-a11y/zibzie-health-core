"use client";

import { useEffect, useMemo, useState } from "react";
import Notice from "@/components/ui/Notice";
import {
  ApiError,
  listPatientAccessGrants,
  revokePatientAccessGrant,
  type PatientAccessGrant,
} from "@/lib/api";
import { formatDateTime } from "@/lib/format";

function formatMissing(value: string | null | undefined) {
  return value && value.trim().length > 0 ? value : "ثبت نشده";
}

function formatGrantDate(value: string | null) {
  return formatDateTime(value) || "بدون محدودیت";
}

function grantStatus(grant: PatientAccessGrant) {
  if (grant.revokedAt) {
    return {
      label: "لغو شده",
      className: "bg-slate-100 text-slate-700",
    };
  }

  if (!grant.isActive) {
    return {
      label: "غیرفعال",
      className: "bg-amber-50 text-amber-800",
    };
  }

  return {
    label: "فعال",
    className: "bg-emerald-50 text-emerald-800",
  };
}

function GrantMeta({
  label,
  value,
}: {
  label: string;
  value: string | null | undefined;
}) {
  return (
    <div className="min-w-0 rounded-md bg-slate-50 px-3 py-2">
      <dt className="text-xs font-semibold text-slate-500">{label}</dt>
      <dd className="mt-1 break-words text-sm font-medium text-slate-900">
        {formatMissing(value)}
      </dd>
    </div>
  );
}

export default function PatientAccessGrantsPanel({
  patientId,
}: {
  patientId: string;
}) {
  const [grants, setGrants] = useState<PatientAccessGrant[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [revokeGrantId, setRevokeGrantId] = useState<string | null>(null);
  const [revokeReason, setRevokeReason] = useState("");
  const [isRevoking, setIsRevoking] = useState(false);

  const sortedGrants = useMemo(
    () =>
      [...grants].sort((first, second) => {
        const firstTime = new Date(first.grantedAt).getTime();
        const secondTime = new Date(second.grantedAt).getTime();

        return (
          (Number.isNaN(secondTime) ? 0 : secondTime) -
          (Number.isNaN(firstTime) ? 0 : firstTime)
        );
      }),
    [grants],
  );

  useEffect(() => {
    let isMounted = true;

    listPatientAccessGrants(patientId)
      .then((items) => {
        if (isMounted) {
          setGrants(items);
        }
      })
      .catch((error) => {
        if (!isMounted) {
          return;
        }

        if (error instanceof ApiError && error.status === 401) {
          setErrorMessage("برای مشاهده دسترسی‌های بیمار ابتدا وارد شوید.");
        } else if (error instanceof ApiError && error.status === 403) {
          setErrorMessage("حساب فعلی مجوز مشاهده دسترسی‌های بیمار را ندارد.");
        } else {
          setErrorMessage("دریافت دسترسی‌های بیمار با خطا روبه‌رو شد.");
        }
      })
      .finally(() => {
        if (isMounted) {
          setIsLoading(false);
        }
      });

    return () => {
      isMounted = false;
    };
  }, [patientId]);

  async function handleRevoke(grantId: string) {
    setIsRevoking(true);
    setErrorMessage(null);

    try {
      const updated = await revokePatientAccessGrant(grantId, revokeReason);

      setGrants((current) =>
        current.map((grant) => (grant.id === updated.id ? updated : grant)),
      );
      setRevokeGrantId(null);
      setRevokeReason("");
    } catch (error) {
      if (error instanceof ApiError && error.status === 403) {
        setErrorMessage("حساب فعلی مجوز لغو این دسترسی را ندارد.");
      } else {
        setErrorMessage("لغو دسترسی با خطا روبه‌رو شد.");
      }
    } finally {
      setIsRevoking(false);
    }
  }

  if (isLoading) {
    return <Notice variant="loading">در حال دریافت دسترسی‌های بیمار...</Notice>;
  }

  if (errorMessage) {
    return <Notice variant="error">{errorMessage}</Notice>;
  }

  if (sortedGrants.length === 0) {
    return <Notice variant="empty">برای این بیمار دسترسی فعالی ثبت نشده است.</Notice>;
  }

  return (
    <div className="space-y-3">
      {sortedGrants.map((grant) => {
        const status = grantStatus(grant);
        const grantee =
          grant.serviceAccountId ??
          (grant.granteeUserId ? `کاربر ${grant.granteeUserId}` : null);
        const isRevokeOpen = revokeGrantId === grant.id;

        return (
          <article
            className="rounded-md border border-slate-200 bg-white p-4"
            key={grant.id}
          >
            <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
              <div className="min-w-0">
                <div className="flex flex-wrap items-center gap-2">
                  <h3 className="break-words text-base font-bold text-slate-950">
                    {grant.productCode} / {grant.productRole}
                  </h3>
                  <span
                    className={`rounded-md px-2.5 py-1 text-xs font-semibold ${status.className}`}
                  >
                    {status.label}
                  </span>
                </div>
                <p className="mt-2 break-words text-sm text-slate-600">
                  {formatMissing(grantee)}
                </p>
              </div>

              {grant.isActive && !grant.revokedAt ? (
                <button
                  className="inline-flex h-10 items-center justify-center rounded-md border border-rose-200 px-3 text-sm font-semibold text-rose-700 transition hover:bg-rose-50"
                  onClick={() => setRevokeGrantId(grant.id)}
                  type="button"
                >
                  لغو دسترسی
                </button>
              ) : null}
            </div>

            <dl className="mt-4 grid gap-2 sm:grid-cols-2 xl:grid-cols-4">
              <GrantMeta label="دامنه" value={grant.scope} />
              <GrantMeta label="دلیل" value={grant.reason} />
              <GrantMeta label="شروع اعتبار" value={formatDateTime(grant.validFrom)} />
              <GrantMeta label="پایان اعتبار" value={formatGrantDate(grant.validUntil)} />
              <GrantMeta label="زمان اعطا" value={formatDateTime(grant.grantedAt)} />
              <GrantMeta
                label="اعطا کننده"
                value={
                  grant.grantedByServiceAccountId ??
                  grant.grantedByUserId ??
                  null
                }
              />
              <GrantMeta label="زمان لغو" value={formatDateTime(grant.revokedAt)} />
              <GrantMeta label="دلیل لغو" value={grant.revokeReason} />
            </dl>

            {isRevokeOpen ? (
              <div className="mt-4 rounded-md border border-rose-100 bg-rose-50 p-3">
                <label className="flex flex-col gap-2 text-sm font-medium text-rose-950">
                  <span>دلیل لغو</span>
                  <input
                    className="h-10 rounded-md border border-rose-200 bg-white px-3 text-slate-950 outline-none transition focus:border-rose-500 focus:ring-2 focus:ring-rose-100"
                    onChange={(event) => setRevokeReason(event.target.value)}
                    value={revokeReason}
                  />
                </label>
                <div className="mt-3 flex flex-wrap gap-2">
                  <button
                    className="inline-flex h-10 items-center justify-center rounded-md bg-rose-700 px-4 text-sm font-semibold text-white transition hover:bg-rose-800 disabled:cursor-not-allowed disabled:bg-slate-400"
                    disabled={isRevoking}
                    onClick={() => handleRevoke(grant.id)}
                    type="button"
                  >
                    {isRevoking ? "در حال لغو..." : "تایید لغو"}
                  </button>
                  <button
                    className="inline-flex h-10 items-center justify-center rounded-md border border-slate-300 px-4 text-sm font-semibold text-slate-700 transition hover:bg-white"
                    disabled={isRevoking}
                    onClick={() => {
                      setRevokeGrantId(null);
                      setRevokeReason("");
                    }}
                    type="button"
                  >
                    انصراف
                  </button>
                </div>
              </div>
            ) : null}
          </article>
        );
      })}
    </div>
  );
}
