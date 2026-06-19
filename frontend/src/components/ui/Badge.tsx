import type { ReactNode } from "react";

type BadgeTone = "default" | "success" | "warning" | "danger" | "info" | "muted";

const toneClasses: Record<BadgeTone, string> = {
  default: "bg-slate-100 text-slate-700",
  success: "bg-emerald-50 text-emerald-800",
  warning: "bg-amber-50 text-amber-800",
  danger: "bg-rose-50 text-rose-800",
  info: "bg-teal-50 text-teal-800",
  muted: "bg-slate-100 text-slate-700",
};

export default function Badge({
  children,
  tone = "default",
  className = "",
}: {
  children: ReactNode;
  tone?: BadgeTone;
  className?: string;
}) {
  return (
    <span
      className={`rounded-md px-2.5 py-1 text-xs font-semibold ${toneClasses[tone]} ${className}`}
    >
      {children}
    </span>
  );
}
