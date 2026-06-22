"use client";

import { useRouter } from "next/navigation";
import type { FormEvent } from "react";
import { useEffect, useState } from "react";
import { ApiError, getCurrentAdmin, loginAdmin } from "@/lib/api";
import { clearAdminAccessToken } from "@/lib/auth/admin-auth";

export default function AdminLoginPage() {
  const router = useRouter();
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [isCheckingSession, setIsCheckingSession] = useState(true);
  const [isSubmitting, setIsSubmitting] = useState(false);

  useEffect(() => {
    let isMounted = true;

    getCurrentAdmin()
      .then(() => {
        if (isMounted) {
          router.replace("/patients");
        }
      })
      .catch((error) => {
        if (error instanceof ApiError && error.status === 401) {
          clearAdminAccessToken();
        }
      })
      .finally(() => {
        if (isMounted) {
          setIsCheckingSession(false);
        }
      });

    return () => {
      isMounted = false;
    };
  }, [router]);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setErrorMessage(null);

    if (!username.trim() || !password) {
      setErrorMessage("نام کاربری و رمز عبور را وارد کنید.");
      return;
    }

    setIsSubmitting(true);

    try {
      await loginAdmin({ username, password });

      router.replace("/patients");
      router.refresh();
    } catch (error) {
      clearAdminAccessToken();

      if (error instanceof ApiError && error.status === 502) {
        setErrorMessage(
          "سرویس احراز هویت در دسترس نیست. چند لحظه بعد دوباره تلاش کنید.",
        );
      } else {
        setErrorMessage("نام کاربری یا رمز عبور درست نیست.");
      }
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <main className="flex min-h-screen items-center justify-center bg-slate-50 px-4 py-8">
      <section className="w-full max-w-sm rounded-md border border-slate-200 bg-white p-6 shadow-sm">
        <div className="space-y-2">
          <p className="text-sm font-semibold text-teal-700">Zibzie Health Core</p>
          <h1 className="text-2xl font-bold text-slate-950">ورود پنل ادمین</h1>
          <p className="text-sm leading-7 text-slate-600">
            برای مدیریت پرونده‌های سلامت با حساب داخلی ادمین وارد شوید.
          </p>
        </div>

        {isCheckingSession ? (
          <div className="mt-5 rounded-md border border-slate-200 bg-slate-50 px-3 py-2 text-sm text-slate-600">
            در حال بررسی نشست فعلی...
          </div>
        ) : null}

        <form className="mt-6 space-y-4" onSubmit={handleSubmit}>
          {errorMessage ? (
            <div className="rounded-md border border-rose-200 bg-rose-50 px-3 py-2 text-sm leading-7 text-rose-900">
              {errorMessage}
            </div>
          ) : null}

          <label className="flex flex-col gap-2 text-sm font-medium text-slate-700">
            <span>نام کاربری</span>
            <input
              autoComplete="username"
              className="h-11 rounded-md border border-slate-300 bg-white px-3 text-left text-slate-950 outline-none transition placeholder:text-slate-400 focus:border-teal-600 focus:ring-2 focus:ring-teal-100"
              dir="ltr"
              onChange={(event) => setUsername(event.target.value)}
              value={username}
            />
          </label>

          <label className="flex flex-col gap-2 text-sm font-medium text-slate-700">
            <span>رمز عبور</span>
            <input
              autoComplete="current-password"
              className="h-11 rounded-md border border-slate-300 bg-white px-3 text-left text-slate-950 outline-none transition placeholder:text-slate-400 focus:border-teal-600 focus:ring-2 focus:ring-teal-100"
              dir="ltr"
              onChange={(event) => setPassword(event.target.value)}
              type="password"
              value={password}
            />
          </label>

          <button
            className="inline-flex h-11 w-full items-center justify-center rounded-md bg-teal-700 px-4 text-sm font-semibold text-white shadow-sm transition hover:bg-teal-800 disabled:cursor-not-allowed disabled:bg-slate-400"
            disabled={isSubmitting || isCheckingSession}
            type="submit"
          >
            {isSubmitting ? "در حال ورود..." : "ورود"}
          </button>
        </form>
      </section>
    </main>
  );
}
