import Link from "next/link";

type AdminAccessStateProps = {
  status?: number;
  message?: string | null;
  backHref?: string;
  backLabel?: string;
};

function stateCopy(status?: number, message?: string | null) {
  if (status === 401) {
    return {
      tone: "amber",
      title: "ورود لازم است",
      message: "برای مشاهده این بخش ابتدا وارد پنل ادمین شوید.",
      actionHref: "/login",
      actionLabel: "ورود به پنل",
    };
  }

  if (status === 403) {
    return {
      tone: "rose",
      title: "دسترسی مجاز نیست",
      message: "حساب فعلی مجوز مشاهده این بخش را ندارد.",
      actionHref: "/patients",
      actionLabel: "بازگشت به فهرست بیماران",
    };
  }

  if (status === 502) {
    return {
      tone: "amber",
      title: "سرویس در دسترس نیست",
      message:
        "ارتباط با سرویس پرونده سلامت برقرار نشد. چند لحظه بعد دوباره تلاش کنید.",
      actionHref: null,
      actionLabel: null,
    };
  }

  return {
    tone: "rose",
    title: "خطا در دریافت اطلاعات",
    message: message || "درخواست با خطا روبه‌رو شد.",
    actionHref: null,
    actionLabel: null,
  };
}

export default function AdminAccessState({
  status,
  message,
  backHref,
  backLabel,
}: AdminAccessStateProps) {
  const copy = stateCopy(status, message);
  const toneClass =
    copy.tone === "amber"
      ? "border-amber-200 bg-amber-50 text-amber-950"
      : "border-rose-200 bg-rose-50 text-rose-950";
  const actionHref = backHref ?? copy.actionHref;
  const actionLabel = backLabel ?? copy.actionLabel;

  return (
    <section className={`rounded-md border p-5 ${toneClass}`}>
      <h2 className="text-base font-bold">{copy.title}</h2>
      <p className="mt-2 text-sm leading-7">{copy.message}</p>
      {actionHref && actionLabel ? (
        <Link
          className="mt-4 inline-flex h-10 items-center justify-center rounded-md bg-white px-4 text-sm font-semibold text-slate-800 shadow-sm ring-1 ring-inset ring-slate-200 transition hover:bg-slate-50"
          href={actionHref}
        >
          {actionLabel}
        </Link>
      ) : null}
    </section>
  );
}
