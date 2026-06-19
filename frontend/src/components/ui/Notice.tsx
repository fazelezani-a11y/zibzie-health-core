import type { ReactNode } from "react";

type NoticeVariant = "loading" | "error" | "empty" | "success" | "info";

const variantClasses: Record<NoticeVariant, string> = {
  loading: "border-slate-200 bg-slate-50 text-slate-600",
  error: "border-rose-200 bg-rose-50 leading-7 text-rose-900",
  empty: "border-dashed border-slate-300 bg-slate-50 leading-7 text-slate-600",
  success: "border-emerald-200 bg-emerald-50 leading-7 text-emerald-900",
  info: "border-slate-200 bg-slate-50 leading-7 text-slate-600",
};

export default function Notice({
  variant,
  children,
}: {
  variant: NoticeVariant;
  children: ReactNode;
}) {
  return (
    <div className={`rounded-md border p-4 text-sm ${variantClasses[variant]}`}>
      {children}
    </div>
  );
}
