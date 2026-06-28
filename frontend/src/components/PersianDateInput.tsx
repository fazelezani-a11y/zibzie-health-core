"use client";

import { useMemo, useState } from "react";
import DatePicker, { DateObject } from "react-multi-date-picker";
import TimePicker from "react-multi-date-picker/plugins/time_picker";
import gregorian from "react-date-object/calendars/gregorian";
import persian from "react-date-object/calendars/persian";
import gregorian_en from "react-date-object/locales/gregorian_en";
import persian_fa from "react-date-object/locales/persian_fa";

type PersianDateInputMode = "date" | "datetime";

type PersianDateInputProps = {
  label: string;
  value: string;
  onChange: (value: string) => void;
  mode?: PersianDateInputMode;
  required?: boolean;
  disabled?: boolean;
  className?: string;
  placeholder?: string;
};

const INPUT_CLASS =
  "h-10 w-full rounded-md border border-slate-300 bg-white px-3 text-slate-950 outline-none transition placeholder:text-slate-400 focus:border-teal-600 focus:ring-2 focus:ring-teal-100 disabled:cursor-not-allowed disabled:bg-slate-100";

function parseIsoDateValue(value: string, mode: PersianDateInputMode) {
  if (!value) {
    return null;
  }

  const match =
    mode === "datetime"
      ? value.match(
          /^(\d{4})-(\d{2})-(\d{2})(?:T|\s)(\d{2}):(\d{2})/,
        )
      : value.match(/^(\d{4})-(\d{2})-(\d{2})/);

  if (!match) {
    return null;
  }

  const [, year, month, day, hour = "12", minute = "0"] = match;

  return new DateObject({
    year: Number(year),
    month: Number(month),
    day: Number(day),
    hour: Number(hour),
    minute: Number(minute),
    calendar: gregorian,
    locale: gregorian_en,
  }).convert(persian, persian_fa);
}

function formatForBackend(
  date: DateObject,
  mode: PersianDateInputMode,
) {
  const gregorianDate = new DateObject(date).convert(
    gregorian,
    gregorian_en,
  );

  return gregorianDate.format(
    mode === "datetime" ? "YYYY-MM-DDTHH:mm" : "YYYY-MM-DD",
  );
}

function getValidatedText(value: string | string[]) {
  return Array.isArray(value) ? value.join(" ") : value;
}

export default function PersianDateInput({
  label,
  value,
  onChange,
  mode = "date",
  required = false,
  disabled = false,
  className = "",
  placeholder,
}: PersianDateInputProps) {
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const selectedDate = useMemo(
    () => parseIsoDateValue(value, mode),
    [mode, value],
  );
  const displayFormat = mode === "datetime" ? "YYYY/MM/DD HH:mm" : "YYYY/MM/DD";
  const resolvedPlaceholder =
    placeholder ?? (mode === "datetime" ? "۱۴۰۳/۰۱/۱۵ ۱۰:۳۰" : "۱۴۰۳/۰۱/۱۵");

  return (
    <label
      className={`flex flex-col gap-1.5 text-sm font-medium text-slate-700 ${className}`}
    >
      <span>{label}</span>
      <DatePicker
        calendar={persian}
        calendarPosition="bottom-right"
        className="teal"
        containerClassName="w-full"
        disabled={disabled}
        editable
        format={displayFormat}
        inputClass={INPUT_CLASS}
        inputMode="numeric"
        locale={persian_fa}
        mobileLabels={{
          CANCEL: "انصراف",
          OK: "تایید",
        }}
        onChange={(date, options) => {
          const typedValue = getValidatedText(options.validatedValue).trim();

          if (!date) {
            onChange("");
            setErrorMessage(
              typedValue ? "تاریخ واردشده معتبر نیست." : null,
            );
            return;
          }

          onChange(formatForBackend(date, mode));
          setErrorMessage(null);
        }}
        placeholder={resolvedPlaceholder}
        plugins={
          mode === "datetime"
            ? [<TimePicker hideSeconds key="time-picker" position="bottom" />]
            : undefined
        }
        required={required}
        value={selectedDate}
      />
      {errorMessage ? (
        <span className="text-xs font-medium text-rose-700">
          {errorMessage}
        </span>
      ) : null}
    </label>
  );
}
