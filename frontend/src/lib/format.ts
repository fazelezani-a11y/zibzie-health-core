const persianLocale = "fa-IR-u-ca-persian";
const missingValueLabel = "ثبت نشده";

function parseDateValue(value: string) {
  const normalizedValue = /^\d{4}-\d{2}-\d{2}$/.test(value)
    ? `${value}T00:00:00`
    : value;
  const date = new Date(normalizedValue);

  return Number.isNaN(date.getTime()) ? null : date;
}

export function formatDateTime(value: string | null | undefined) {
  if (!value) {
    return null;
  }

  const date = parseDateValue(value);

  if (!date) {
    return value;
  }

  return new Intl.DateTimeFormat(persianLocale, {
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
  }).format(date);
}

export function formatDate(value: string | null | undefined) {
  if (!value) {
    return null;
  }

  const date = parseDateValue(value);

  if (!date) {
    return value;
  }

  return new Intl.DateTimeFormat(persianLocale, {
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
  }).format(date);
}

export function formatBooleanPersian(value: boolean | null | undefined) {
  if (value === null || value === undefined) {
    return missingValueLabel;
  }

  return value ? "بله" : "خیر";
}

export function formatNumberPersian(
  value: number | null | undefined,
  options?: Intl.NumberFormatOptions,
) {
  if (value === null || value === undefined) {
    return missingValueLabel;
  }

  return new Intl.NumberFormat("fa-IR", options).format(value);
}

export function formatPersianDigits(value: string | number | null | undefined) {
  if (value === null || value === undefined || value === "") {
    return missingValueLabel;
  }

  return String(value).replace(/\d/g, (digit) =>
    new Intl.NumberFormat("fa-IR", { useGrouping: false }).format(Number(digit)),
  );
}

export function formatFileSizePersian(value: number | null | undefined) {
  if (value === null || value === undefined) {
    return null;
  }

  if (value >= 1024 * 1024) {
    return `${formatNumberPersian(value / (1024 * 1024), {
      maximumFractionDigits: 1,
    })} مگابایت`;
  }

  if (value >= 1024) {
    return `${formatNumberPersian(value / 1024, {
      maximumFractionDigits: 1,
    })} کیلوبایت`;
  }

  return `${formatNumberPersian(value)} بایت`;
}

export function formatNullable(value: string | number | null | undefined) {
  if (value === null || value === undefined || value === "") {
    return missingValueLabel;
  }

  return typeof value === "number" ? formatNumberPersian(value) : value;
}
