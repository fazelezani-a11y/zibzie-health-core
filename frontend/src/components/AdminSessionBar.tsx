"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { ApiError, getCurrentAdmin, logoutAdmin, type AdminMeResponse } from "@/lib/api";
import { getHealthOptionLabel, productRoleOptions } from "@/lib/health-options";

type SessionState =
  | { status: "loading"; admin: null; message: null }
  | { status: "authenticated"; admin: AdminMeResponse; message: null }
  | { status: "unauthenticated"; admin: null; message: string }
  | { status: "error"; admin: null; message: string };

export default function AdminSessionBar() {
  const router = useRouter();
  const [session, setSession] = useState<SessionState>({
    status: "loading",
    admin: null,
    message: null,
  });
  const [isLoggingOut, setIsLoggingOut] = useState(false);

  useEffect(() => {
    let isMounted = true;

    getCurrentAdmin()
      .then((admin) => {
        if (isMounted) {
          setSession({ status: "authenticated", admin, message: null });
        }
      })
      .catch((error) => {
        if (!isMounted) {
          return;
        }

        if (error instanceof ApiError && error.status === 401) {
          setSession({
            status: "unauthenticated",
            admin: null,
            message: "نشست ادمین فعال نیست.",
          });
          return;
        }

        setSession({
          status: "error",
          admin: null,
          message: "وضعیت نشست قابل بررسی نیست.",
        });
      });

    return () => {
      isMounted = false;
    };
  }, []);

  async function handleLogout() {
    setIsLoggingOut(true);

    try {
      await logoutAdmin();
    } finally {
      router.replace("/login");
      router.refresh();
    }
  }

  return (
    <div className="rounded-md border border-slate-200 bg-white px-4 py-3 shadow-sm">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div className="min-w-0">
          <p className="text-xs font-semibold text-slate-500">نشست ادمین</p>
          {session.status === "authenticated" ? (
            <div className="mt-1 flex flex-wrap items-center gap-2">
              <span className="text-sm font-bold text-slate-950">
                {session.admin.displayName || "ادمین Health Core"}
              </span>
              <span className="rounded-md bg-teal-50 px-2.5 py-1 text-xs font-semibold text-teal-800">
                {getHealthOptionLabel(productRoleOptions, session.admin.productRole)}
              </span>
            </div>
          ) : (
            <p className="mt-1 text-sm text-slate-600">
              {session.status === "loading"
                ? "در حال بررسی نشست..."
                : session.message}
            </p>
          )}
        </div>

        <div className="flex flex-wrap gap-2">
          {session.status === "authenticated" ? (
            <button
              className="inline-flex h-10 items-center justify-center rounded-md border border-slate-300 px-4 text-sm font-semibold text-slate-700 transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-60"
              disabled={isLoggingOut}
              onClick={handleLogout}
              type="button"
            >
              {isLoggingOut ? "در حال خروج..." : "خروج"}
            </button>
          ) : (
            <Link
              className="inline-flex h-10 items-center justify-center rounded-md bg-teal-700 px-4 text-sm font-semibold text-white transition hover:bg-teal-800"
              href="/login"
            >
              ورود
            </Link>
          )}
        </div>
      </div>
    </div>
  );
}
