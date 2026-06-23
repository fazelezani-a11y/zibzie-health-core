"use client";

import { useEffect, useMemo, useState } from "react";
import Notice from "@/components/ui/Notice";
import {
  ApiError,
  createPatientAccessGrant,
  listPatientAccessGrants,
  revokePatientAccessGrant,
  type CreatePatientAccessGrantRequest,
  type PatientAccessGrant,
} from "@/lib/api";
import { formatDateTime } from "@/lib/format";

type RecipientType = "service" | "user";

type CreateGrantFormState = {
  recipientType: RecipientType;
  serviceAccountId: string;
  granteeUserId: string;
  productCode: string;
  productRole: string;
  scope: string;
  reason: string;
  validUntil: string;
  notes: string;
};

type RoleProfileDefaults = {
  scope: string;
  reason: string;
};

const PRODUCT_OPTIONS = [
  { value: "DigiCare", label: "دیجی‌مراقب" },
  { value: "HomeVisit", label: "ویزیت در منزل" },
  { value: "SecondOpinion", label: "نظر دوم" },
  { value: "PersonalHealthRecord", label: "پرونده سلامت شخصی" },
  { value: "ClinicQueue", label: "صف و نوبت مطب" },
];

const PRODUCT_ROLE_OPTIONS: Record<string, { value: string; label: string }[]> = {
  DigiCare: [
    { value: "DigiCareCaseManager", label: "مدیر پرونده دیجی‌مراقب" },
    { value: "DigiCareCareTeamManager", label: "مدیر تیم مراقبت" },
    { value: "DigiCareClinician", label: "درمانگر/پزشک دیجی‌مراقب" },
    { value: "DigiCarePersonalDoctor", label: "پزشک شخصی" },
    { value: "DigiCarePersonalCounselor", label: "مشاور شخصی" },
    { value: "DigiCareNutritionSpecialist", label: "متخصص تغذیه" },
    { value: "DigiCareExerciseSpecialist", label: "متخصص ورزش" },
    { value: "DigiCareOperations", label: "عملیات دیجی‌مراقب" },
    { value: "DigiCareTransportCoordinator", label: "هماهنگ‌کننده حمل‌ونقل" },
  ],
  HomeVisit: [
    { value: "HomeVisitDoctor", label: "پزشک ویزیت در منزل" },
    { value: "HomeVisitDispatcher", label: "دیسپچر ویزیت در منزل" },
    { value: "HomeVisitPatient", label: "بیمار ویزیت در منزل" },
  ],
  SecondOpinion: [
    { value: "SecondOpinionCaseManager", label: "مدیر پرونده نظر دوم" },
    { value: "SecondOpinionLeadSpecialist", label: "متخصص لیدر" },
    { value: "SecondOpinionInvitedSpecialist", label: "متخصص دعوت‌شده" },
    { value: "SecondOpinionPatient", label: "بیمار نظر دوم" },
  ],
  PersonalHealthRecord: [
    { value: "PersonalHealthRecordOwner", label: "مالک پرونده شخصی" },
    { value: "PersonalHealthRecordFamilyViewer", label: "عضو خانواده مجاز" },
    { value: "PersonalHealthRecordSharedProvider", label: "ارائه‌دهنده مشترک" },
  ],
  ClinicQueue: [
    { value: "ClinicQueueReceptionist", label: "منشی/پذیرش مطب" },
    { value: "ClinicQueueClinicAdmin", label: "مدیر کلینیک" },
    { value: "ClinicQueuePatient", label: "بیمار صف مطب" },
  ],
};

const SCOPE_OPTIONS = [
  { value: "AssignedPatientsOnly", label: "فقط بیماران تخصیص‌یافته" },
  { value: "OwnRecordOnly", label: "فقط پرونده خود فرد" },
  { value: "FamilyAuthorizedRecords", label: "پرونده‌های مجاز خانواده" },
  { value: "InvitedCasesOnly", label: "فقط پرونده‌های دعوت‌شده" },
  { value: "OrganizationPatients", label: "بیماران سازمان/مرکز" },
  { value: "TemporaryAccess", label: "دسترسی موقت" },
  { value: "EmergencyAccess", label: "دسترسی اضطراری" },
  { value: "CreatedByMe", label: "موارد ایجادشده توسط خود فرد" },
];

const REASON_OPTIONS = [
  { value: "ActiveCare", label: "مراقبت فعال" },
  { value: "SecondOpinion", label: "نظر دوم" },
  { value: "HomeVisit", label: "ویزیت در منزل" },
  { value: "PatientShared", label: "اشتراک‌گذاری توسط بیمار" },
  { value: "Emergency", label: "شرایط اضطراری" },
  { value: "CareTeamOperation", label: "عملیات تیم مراقبت" },
  { value: "SystemAutomation", label: "اتوماسیون سیستمی" },
];

const ROLE_PROFILE_DEFAULTS: Record<string, RoleProfileDefaults> = {
  DigiCareCaseManager: {
    scope: "AssignedPatientsOnly",
    reason: "CareTeamOperation",
  },
  DigiCareCareTeamManager: {
    scope: "AssignedPatientsOnly",
    reason: "CareTeamOperation",
  },
  DigiCareClinician: {
    scope: "AssignedPatientsOnly",
    reason: "ActiveCare",
  },
  DigiCarePersonalDoctor: {
    scope: "AssignedPatientsOnly",
    reason: "ActiveCare",
  },
  DigiCarePersonalCounselor: {
    scope: "AssignedPatientsOnly",
    reason: "ActiveCare",
  },
  DigiCareNutritionSpecialist: {
    scope: "AssignedPatientsOnly",
    reason: "ActiveCare",
  },
  DigiCareExerciseSpecialist: {
    scope: "AssignedPatientsOnly",
    reason: "ActiveCare",
  },
  DigiCareOperations: {
    scope: "AssignedPatientsOnly",
    reason: "CareTeamOperation",
  },
  DigiCareTransportCoordinator: {
    scope: "AssignedPatientsOnly",
    reason: "CareTeamOperation",
  },
  HomeVisitDoctor: {
    scope: "TemporaryAccess",
    reason: "HomeVisit",
  },
  HomeVisitDispatcher: {
    scope: "AssignedPatientsOnly",
    reason: "HomeVisit",
  },
  HomeVisitPatient: {
    scope: "OwnRecordOnly",
    reason: "HomeVisit",
  },
  SecondOpinionCaseManager: {
    scope: "InvitedCasesOnly",
    reason: "SecondOpinion",
  },
  SecondOpinionLeadSpecialist: {
    scope: "InvitedCasesOnly",
    reason: "SecondOpinion",
  },
  SecondOpinionInvitedSpecialist: {
    scope: "InvitedCasesOnly",
    reason: "SecondOpinion",
  },
  SecondOpinionPatient: {
    scope: "OwnRecordOnly",
    reason: "SecondOpinion",
  },
  PersonalHealthRecordOwner: {
    scope: "OwnRecordOnly",
    reason: "PatientShared",
  },
  PersonalHealthRecordFamilyViewer: {
    scope: "FamilyAuthorizedRecords",
    reason: "PatientShared",
  },
  PersonalHealthRecordSharedProvider: {
    scope: "TemporaryAccess",
    reason: "PatientShared",
  },
  ClinicQueueReceptionist: {
    scope: "OrganizationPatients",
    reason: "CareTeamOperation",
  },
  ClinicQueueClinicAdmin: {
    scope: "OrganizationPatients",
    reason: "CareTeamOperation",
  },
  ClinicQueuePatient: {
    scope: "OwnRecordOnly",
    reason: "PatientShared",
  },
};

const INITIAL_CREATE_FORM: CreateGrantFormState = {
  recipientType: "service",
  serviceAccountId: "",
  granteeUserId: "",
  productCode: "DigiCare",
  productRole: "DigiCareCaseManager",
  scope: "AssignedPatientsOnly",
  reason: "CareTeamOperation",
  validUntil: "",
  notes: "",
};

function formatMissing(value: string | null | undefined) {
  return value && value.trim().length > 0 ? value : "ثبت نشده";
}

function formatGrantDate(value: string | null) {
  return formatDateTime(value) || "بدون محدودیت";
}

function toNullableTrimmed(value: string) {
  const trimmed = value.trim();
  return trimmed.length > 0 ? trimmed : null;
}

function toIsoDateTimeOrNull(value: string) {
  if (!value) {
    return null;
  }

  const date = new Date(value);

  if (Number.isNaN(date.getTime())) {
    return null;
  }

  return date.toISOString();
}

function isGuidLike(value: string) {
  return /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i.test(
    value.trim(),
  );
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
  const [loadErrorMessage, setLoadErrorMessage] = useState<string | null>(null);
  const [actionErrorMessage, setActionErrorMessage] = useState<string | null>(
    null,
  );
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [revokeGrantId, setRevokeGrantId] = useState<string | null>(null);
  const [revokeReason, setRevokeReason] = useState("");
  const [isRevoking, setIsRevoking] = useState(false);
  const [isCreateOpen, setIsCreateOpen] = useState(false);
  const [createForm, setCreateForm] =
    useState<CreateGrantFormState>(INITIAL_CREATE_FORM);
  const [isCreating, setIsCreating] = useState(false);

  const availableProductRoles =
    PRODUCT_ROLE_OPTIONS[createForm.productCode] ?? [];
  const selectedRoleDefaults = ROLE_PROFILE_DEFAULTS[createForm.productRole];
  const availableScopeOptions = selectedRoleDefaults
    ? SCOPE_OPTIONS.filter((option) => option.value === selectedRoleDefaults.scope)
    : SCOPE_OPTIONS;

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
          setLoadErrorMessage("برای مشاهده دسترسی‌های بیمار ابتدا وارد شوید.");
        } else if (error instanceof ApiError && error.status === 403) {
          setLoadErrorMessage("حساب فعلی مجوز مشاهده دسترسی‌های بیمار را ندارد.");
        } else {
          setLoadErrorMessage("دریافت دسترسی‌های بیمار با خطا روبه‌رو شد.");
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

  function updateCreateForm<Key extends keyof CreateGrantFormState>(
    key: Key,
    value: CreateGrantFormState[Key],
  ) {
    setCreateForm((current) => {
      if (key === "productCode") {
        const nextRoles = PRODUCT_ROLE_OPTIONS[String(value)] ?? [];
        const nextRole = nextRoles[0]?.value ?? "";
        const nextDefaults = ROLE_PROFILE_DEFAULTS[nextRole];

        return {
          ...current,
          productCode: String(value),
          productRole: nextRole,
          scope: nextDefaults?.scope ?? current.scope,
          reason: nextDefaults?.reason ?? current.reason,
        };
      }

      if (key === "productRole") {
        const nextDefaults = ROLE_PROFILE_DEFAULTS[String(value)];

        return {
          ...current,
          productRole: String(value),
          scope: nextDefaults?.scope ?? current.scope,
          reason: nextDefaults?.reason ?? current.reason,
        };
      }

      return {
        ...current,
        [key]: value,
      };
    });
  }

  function validateCreateForm(nowMs: number) {
    if (!createForm.productCode) {
      return "لطفاً محصول مقصد را انتخاب کنید.";
    }

    if (!createForm.productRole) {
      return "لطفاً نقش دسترسی را انتخاب کنید.";
    }

    if (!availableProductRoles.some((role) => role.value === createForm.productRole)) {
      return "نقش انتخاب‌شده با محصول مقصد سازگار نیست.";
    }

    if (!createForm.scope) {
      return "لطفاً محدوده دسترسی را انتخاب کنید.";
    }

    if (
      selectedRoleDefaults &&
      createForm.scope !== selectedRoleDefaults.scope
    ) {
      return "محدوده دسترسی باید با نقش انتخاب‌شده هماهنگ باشد.";
    }

    if (!createForm.reason) {
      return "لطفاً دلیل ایجاد دسترسی را انتخاب کنید.";
    }

    if (
      createForm.recipientType === "service" &&
      !toNullableTrimmed(createForm.serviceAccountId)
    ) {
      return "شناسه سرویس الزامی است.";
    }

    if (
      createForm.recipientType === "user" &&
      !toNullableTrimmed(createForm.granteeUserId)
    ) {
      return "شناسه کاربر الزامی است.";
    }

    if (
      createForm.recipientType === "user" &&
      !isGuidLike(createForm.granteeUserId)
    ) {
      return "شناسه کاربر باید یک GUID معتبر باشد.";
    }

    if (createForm.validUntil) {
      const validUntil = new Date(createForm.validUntil);

      if (Number.isNaN(validUntil.getTime())) {
        return "تاریخ پایان اعتبار معتبر نیست.";
      }

      if (validUntil.getTime() <= nowMs) {
        return "تاریخ پایان اعتبار باید در آینده باشد.";
      }
    }

    return null;
  }

  async function handleCreateGrant() {
    const validationMessage = validateCreateForm(new Date().getTime());

    setActionErrorMessage(null);
    setSuccessMessage(null);

    if (validationMessage) {
      setActionErrorMessage(validationMessage);
      return;
    }

    const request: CreatePatientAccessGrantRequest = {
      productCode: createForm.productCode,
      productRole: createForm.productRole,
      scope: createForm.scope,
      reason: createForm.reason,
      granteeUserId:
        createForm.recipientType === "user"
          ? toNullableTrimmed(createForm.granteeUserId)
          : null,
      serviceAccountId:
        createForm.recipientType === "service"
          ? toNullableTrimmed(createForm.serviceAccountId)
          : null,
      validUntil: toIsoDateTimeOrNull(createForm.validUntil),
      notes: toNullableTrimmed(createForm.notes),
    };

    setIsCreating(true);

    try {
      const createdGrant = await createPatientAccessGrant(patientId, request);
      setGrants((current) => [createdGrant, ...current]);
      setCreateForm(INITIAL_CREATE_FORM);
      setIsCreateOpen(false);
      setSuccessMessage("دسترسی جدید با موفقیت ایجاد شد.");
    } catch (error) {
      if (error instanceof ApiError && error.status === 400) {
        setActionErrorMessage(
          "اطلاعات واردشده معتبر نیست. لطفاً فیلدها را بررسی کنید.",
        );
      } else if (error instanceof ApiError && error.status === 401) {
        setActionErrorMessage("برای ایجاد دسترسی ابتدا وارد شوید.");
      } else if (error instanceof ApiError && error.status === 403) {
        setActionErrorMessage("حساب فعلی مجوز ایجاد دسترسی برای این پرونده را ندارد.");
      } else if (error instanceof ApiError && error.status === 404) {
        setActionErrorMessage("بیمار مورد نظر پیدا نشد یا فعال نیست.");
      } else if (error instanceof ApiError && error.status === 409) {
        setActionErrorMessage(
          "برای این گیرنده و محدوده، یک دسترسی فعال مشابه وجود دارد.",
        );
      } else if (error instanceof ApiError && error.status === 502) {
        setActionErrorMessage("سرویس پرونده سلامت در دسترس نیست.");
      } else {
        setActionErrorMessage("ایجاد دسترسی با خطا روبه‌رو شد.");
      }
    } finally {
      setIsCreating(false);
    }
  }

  async function handleRevoke(grantId: string) {
    setIsRevoking(true);
    setActionErrorMessage(null);
    setSuccessMessage(null);

    try {
      const updated = await revokePatientAccessGrant(grantId, revokeReason);

      setGrants((current) =>
        current.map((grant) => (grant.id === updated.id ? updated : grant)),
      );
      setRevokeGrantId(null);
      setRevokeReason("");
      setSuccessMessage("دسترسی با موفقیت لغو شد.");
    } catch (error) {
      if (error instanceof ApiError && error.status === 401) {
        setActionErrorMessage("برای لغو دسترسی ابتدا وارد شوید.");
      } else if (error instanceof ApiError && error.status === 403) {
        setActionErrorMessage("حساب فعلی مجوز لغو این دسترسی را ندارد.");
      } else if (error instanceof ApiError && error.status === 404) {
        setActionErrorMessage("این دسترسی پیدا نشد.");
      } else if (error instanceof ApiError && error.status === 409) {
        setActionErrorMessage("این دسترسی قبلاً لغو شده یا دیگر فعال نیست.");
      } else if (error instanceof ApiError && error.status === 502) {
        setActionErrorMessage("سرویس پرونده سلامت در دسترس نیست.");
      } else {
        setActionErrorMessage("لغو دسترسی با خطا روبه‌رو شد.");
      }
    } finally {
      setIsRevoking(false);
    }
  }

  if (isLoading) {
    return <Notice variant="loading">در حال دریافت دسترسی‌های بیمار...</Notice>;
  }

  if (loadErrorMessage) {
    return <Notice variant="error">{loadErrorMessage}</Notice>;
  }

  return (
    <div className="space-y-4">
      <section className="rounded-md border border-slate-200 bg-white p-4">
        <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
          <div>
            <h3 className="text-base font-bold text-slate-950">
              مدیریت دسترسی‌های پرونده
            </h3>
            <p className="mt-1 text-sm leading-6 text-slate-600">
              در این بخش می‌توانید دسترسی یک سرویس یا کاربر مشخص به همین پرونده
              را ایجاد یا لغو کنید. اعتبار نهایی هر دسترسی همچنان توسط قوانین
              امنیتی بک‌اند کنترل می‌شود.
            </p>
          </div>

          <button
            className="inline-flex h-10 items-center justify-center rounded-md bg-slate-900 px-4 text-sm font-semibold text-white transition hover:bg-slate-800"
            onClick={() => {
              setIsCreateOpen((current) => !current);
              setActionErrorMessage(null);
              setSuccessMessage(null);
            }}
            type="button"
          >
            {isCreateOpen ? "بستن فرم" : "افزودن دسترسی"}
          </button>
        </div>

        {actionErrorMessage ? (
          <div className="mt-4 rounded-md border border-rose-100 bg-rose-50 px-3 py-2 text-sm font-medium text-rose-800">
            {actionErrorMessage}
          </div>
        ) : null}

        {successMessage ? (
          <div className="mt-4 rounded-md border border-emerald-100 bg-emerald-50 px-3 py-2 text-sm font-medium text-emerald-800">
            {successMessage}
          </div>
        ) : null}

        {isCreateOpen ? (
          <div className="mt-4 grid gap-3 rounded-md border border-slate-100 bg-slate-50 p-3">
            <div className="grid gap-3 md:grid-cols-2">
              <label className="flex flex-col gap-2 text-sm font-medium text-slate-700">
                <span>نوع گیرنده دسترسی</span>
                <select
                  className="h-10 rounded-md border border-slate-200 bg-white px-3 text-slate-950 outline-none transition focus:border-slate-500 focus:ring-2 focus:ring-slate-100"
                  onChange={(event) =>
                    updateCreateForm(
                      "recipientType",
                      event.target.value as RecipientType,
                    )
                  }
                  value={createForm.recipientType}
                >
                  <option value="service">سرویس / محصول</option>
                  <option value="user">کاربر مشخص</option>
                </select>
              </label>

              {createForm.recipientType === "service" ? (
                <label className="flex flex-col gap-2 text-sm font-medium text-slate-700">
                  <span>شناسه سرویس</span>
                  <input
                    className="h-10 rounded-md border border-slate-200 bg-white px-3 text-left text-slate-950 outline-none transition focus:border-slate-500 focus:ring-2 focus:ring-slate-100"
                    dir="ltr"
                    onChange={(event) =>
                      updateCreateForm("serviceAccountId", event.target.value)
                    }
                    placeholder="example-service"
                    value={createForm.serviceAccountId}
                  />
                </label>
              ) : (
                <label className="flex flex-col gap-2 text-sm font-medium text-slate-700">
                  <span>شناسه کاربر</span>
                  <input
                    className="h-10 rounded-md border border-slate-200 bg-white px-3 text-left text-slate-950 outline-none transition focus:border-slate-500 focus:ring-2 focus:ring-slate-100"
                    dir="ltr"
                    onChange={(event) =>
                      updateCreateForm("granteeUserId", event.target.value)
                    }
                    placeholder="user-id"
                    value={createForm.granteeUserId}
                  />
                </label>
              )}
            </div>

            <div className="grid gap-3 md:grid-cols-2">
              <label className="flex flex-col gap-2 text-sm font-medium text-slate-700">
                <span>محصول مقصد</span>
                <select
                  className="h-10 rounded-md border border-slate-200 bg-white px-3 text-slate-950 outline-none transition focus:border-slate-500 focus:ring-2 focus:ring-slate-100"
                  onChange={(event) =>
                    updateCreateForm("productCode", event.target.value)
                  }
                  value={createForm.productCode}
                >
                  {PRODUCT_OPTIONS.map((option) => (
                    <option key={option.value} value={option.value}>
                      {option.label}
                    </option>
                  ))}
                </select>
              </label>

              <label className="flex flex-col gap-2 text-sm font-medium text-slate-700">
                <span>نقش در محصول</span>
                <select
                  className="h-10 rounded-md border border-slate-200 bg-white px-3 text-slate-950 outline-none transition focus:border-slate-500 focus:ring-2 focus:ring-slate-100"
                  onChange={(event) =>
                    updateCreateForm("productRole", event.target.value)
                  }
                  value={createForm.productRole}
                >
                  {availableProductRoles.map((option) => (
                    <option key={option.value} value={option.value}>
                      {option.label}
                    </option>
                  ))}
                </select>
              </label>
            </div>

            <div className="grid gap-3 md:grid-cols-2">
              <label className="flex flex-col gap-2 text-sm font-medium text-slate-700">
                <span>محدوده دسترسی</span>
                <select
                  className="h-10 rounded-md border border-slate-200 bg-white px-3 text-slate-950 outline-none transition focus:border-slate-500 focus:ring-2 focus:ring-slate-100"
                  onChange={(event) =>
                    updateCreateForm("scope", event.target.value)
                  }
                  value={createForm.scope}
                >
                  {availableScopeOptions.map((option) => (
                    <option key={option.value} value={option.value}>
                      {option.label}
                    </option>
                  ))}
                </select>
                <span className="text-xs leading-6 text-slate-500">
                  محدوده بر اساس نقش انتخاب‌شده تنظیم می‌شود.
                </span>
              </label>

              <label className="flex flex-col gap-2 text-sm font-medium text-slate-700">
                <span>دلیل ایجاد دسترسی</span>
                <select
                  className="h-10 rounded-md border border-slate-200 bg-white px-3 text-slate-950 outline-none transition focus:border-slate-500 focus:ring-2 focus:ring-slate-100"
                  onChange={(event) =>
                    updateCreateForm("reason", event.target.value)
                  }
                  value={createForm.reason}
                >
                  {REASON_OPTIONS.map((option) => (
                    <option key={option.value} value={option.value}>
                      {option.label}
                    </option>
                  ))}
                </select>
              </label>
            </div>

            <div className="grid gap-3 md:grid-cols-2">
              <label className="flex flex-col gap-2 text-sm font-medium text-slate-700">
                <span>پایان اعتبار</span>
                <input
                  className="h-10 rounded-md border border-slate-200 bg-white px-3 text-slate-950 outline-none transition focus:border-slate-500 focus:ring-2 focus:ring-slate-100"
                  onChange={(event) =>
                    updateCreateForm("validUntil", event.target.value)
                  }
                  type="datetime-local"
                  value={createForm.validUntil}
                />
              </label>

              <label className="flex flex-col gap-2 text-sm font-medium text-slate-700">
                <span>یادداشت</span>
                <input
                  className="h-10 rounded-md border border-slate-200 bg-white px-3 text-slate-950 outline-none transition focus:border-slate-500 focus:ring-2 focus:ring-slate-100"
                  onChange={(event) =>
                    updateCreateForm("notes", event.target.value)
                  }
                  value={createForm.notes}
                />
              </label>
            </div>

            <div className="flex flex-wrap gap-2">
              <button
                className="inline-flex h-10 items-center justify-center rounded-md bg-slate-900 px-4 text-sm font-semibold text-white transition hover:bg-slate-800 disabled:cursor-not-allowed disabled:bg-slate-400"
                disabled={isCreating}
                onClick={handleCreateGrant}
                type="button"
              >
                {isCreating ? "در حال ایجاد..." : "ایجاد دسترسی"}
              </button>
              <button
                className="inline-flex h-10 items-center justify-center rounded-md border border-slate-300 px-4 text-sm font-semibold text-slate-700 transition hover:bg-white disabled:cursor-not-allowed disabled:opacity-60"
                disabled={isCreating}
                onClick={() => {
                  setCreateForm(INITIAL_CREATE_FORM);
                  setIsCreateOpen(false);
                  setActionErrorMessage(null);
                }}
                type="button"
              >
                انصراف
              </button>
            </div>
          </div>
        ) : null}
      </section>

      {sortedGrants.length === 0 ? (
        <Notice variant="empty">برای این بیمار دسترسی فعالی ثبت نشده است.</Notice>
      ) : null}

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
