export type HealthOption = {
  value: string;
  label: string;
};

export const selectPlaceholder = "انتخاب کنید";

export const sourceTypeOptions: HealthOption[] = [
  { value: "Manual", label: "دستی" },
  { value: "ClinicianEntered", label: "ثبت تیم درمان" },
  { value: "System", label: "سیستمی" },
];

export const measurementSourceTypeOptions: HealthOption[] = [
  { value: "Manual", label: "دستی" },
  { value: "LabResult", label: "نتیجه آزمایش" },
  { value: "Device", label: "دستگاه" },
  { value: "ParaclinicalResult", label: "نتیجه پاراکلینیک" },
  { value: "System", label: "سیستمی" },
];

export const verificationStatusOptions: HealthOption[] = [
  { value: "Unverified", label: "تأیید نشده" },
  { value: "PatientReported", label: "گزارش بیمار" },
  { value: "ClinicianVerified", label: "تأیید پزشک/تیم درمان" },
];

export const measurementVerificationStatusOptions: HealthOption[] = [
  ...verificationStatusOptions,
  { value: "DeviceImported", label: "واردشده از دستگاه" },
  { value: "SystemGenerated", label: "تولید سیستمی" },
];

export const sensitivityLevelOptions: HealthOption[] = [
  { value: "Normal", label: "عادی" },
  { value: "Sensitive", label: "حساس" },
];

export const priorityOptions: HealthOption[] = [
  { value: "Low", label: "کم" },
  { value: "Normal", label: "عادی" },
  { value: "High", label: "بالا" },
  { value: "Urgent", label: "فوری" },
];

export const conditionStatusOptions: HealthOption[] = [
  { value: "Active", label: "فعال" },
  { value: "Chronic", label: "مزمن" },
  { value: "Controlled", label: "کنترل‌شده" },
  { value: "Resolved", label: "برطرف‌شده" },
  { value: "Inactive", label: "غیرفعال" },
];

export const allergyTypeOptions: HealthOption[] = [
  { value: "Drug", label: "دارویی" },
  { value: "Food", label: "غذایی" },
  { value: "Environmental", label: "محیطی" },
  { value: "Latex", label: "لاتکس" },
  { value: "Other", label: "سایر" },
];

export const allergySeverityOptions: HealthOption[] = [
  { value: "Mild", label: "خفیف" },
  { value: "Moderate", label: "متوسط" },
  { value: "Severe", label: "شدید" },
  { value: "LifeThreatening", label: "تهدیدکننده حیات" },
];

export const medicationRouteOptions: HealthOption[] = [
  { value: "Oral", label: "خوراکی" },
  { value: "Injection", label: "تزریقی" },
  { value: "IV", label: "وریدی" },
  { value: "Inhaled", label: "استنشاقی" },
  { value: "Topical", label: "موضعی" },
  { value: "Sublingual", label: "زیرزبانی" },
  { value: "Rectal", label: "رکتال" },
  { value: "Other", label: "سایر" },
];

export const carePlanCategoryOptions: HealthOption[] = [
  { value: "Diagnostic", label: "غربالگری / تشخیصی" },
  { value: "Treatment", label: "درمانی" },
  { value: "Care", label: "مراقبتی / توانبخشی" },
  { value: "Lifestyle", label: "سبک زندگی" },
  { value: "FollowUp", label: "نیازمند پیگیری" },
  { value: "Referral", label: "ارجاع / پاراکلینیک" },
  { value: "Other", label: "سایر" },
];

export const carePlanItemTypeOptions: HealthOption[] = [
  { value: "LabTest", label: "آزمایش / غربالگری" },
  { value: "Imaging", label: "تصویربرداری / پاراکلینیک" },
  { value: "MedicationChange", label: "تغییر دارو" },
  { value: "SpecialistVisit", label: "ارجاع / ویزیت متخصص" },
  { value: "HomeCare", label: "مراقبت / توانبخشی در منزل" },
  { value: "Nutrition", label: "تغذیه" },
  { value: "Exercise", label: "ورزش و تناسب اندام" },
  { value: "MentalHealth", label: "سلامت روان" },
  { value: "Education", label: "آموزش" },
  { value: "Visit", label: "ویزیت" },
  { value: "Other", label: "سایر" },
];

export const carePlanStatusOptions: HealthOption[] = [
  { value: "Planned", label: "برنامه‌ریزی‌شده" },
  { value: "Scheduled", label: "زمان‌بندی‌شده" },
  { value: "InProgress", label: "در حال انجام" },
  { value: "Completed", label: "تکمیل‌شده" },
  { value: "Cancelled", label: "لغوشده" },
  { value: "Deferred", label: "به تعویق افتاده" },
];

export const reminderTypeOptions: HealthOption[] = [
  { value: "Medication", label: "دارو" },
  { value: "LabFollowUp", label: "پیگیری آزمایش" },
  { value: "ImagingFollowUp", label: "پیگیری تصویربرداری" },
  { value: "Appointment", label: "قرار/ویزیت" },
  { value: "CarePlan", label: "پلن مراقبتی" },
  { value: "Lifestyle", label: "سبک زندگی" },
  { value: "General", label: "عمومی" },
  { value: "InternalAlert", label: "هشدار داخلی" },
];

export const reminderStatusOptions: HealthOption[] = [
  { value: "Pending", label: "در انتظار" },
  { value: "Done", label: "انجام‌شده" },
  { value: "Snoozed", label: "تعویق‌شده" },
  { value: "Cancelled", label: "لغوشده" },
  { value: "Missed", label: "از دست‌رفته" },
];

export const audienceOptions: HealthOption[] = [
  { value: "Internal", label: "داخلی" },
  { value: "Patient", label: "بیمار" },
  { value: "CareTeam", label: "تیم مراقبت" },
  { value: "Both", label: "هر دو" },
];

export const channelOptions: HealthOption[] = [
  { value: "InApp", label: "داخل سامانه" },
  { value: "SMS", label: "پیامک" },
  { value: "Call", label: "تماس" },
  { value: "WhatsApp", label: "واتساپ" },
  { value: "Email", label: "ایمیل" },
  { value: "None", label: "بدون کانال" },
];

export const documentTypeOptions: HealthOption[] = [
  { value: "LabResult", label: "نتیجه آزمایش" },
  { value: "Imaging", label: "تصویربرداری" },
  { value: "Prescription", label: "نسخه" },
  { value: "PhysicianReport", label: "گزارش پزشک" },
  { value: "Pathology", label: "پاتولوژی" },
  { value: "DischargeSummary", label: "خلاصه ترخیص" },
  { value: "OperationReport", label: "گزارش عمل" },
  { value: "Insurance", label: "بیمه" },
  { value: "Other", label: "سایر" },
];

export const paraclinicalResultTypeOptions: HealthOption[] = [
  { value: "Lab", label: "آزمایش" },
  { value: "Imaging", label: "تصویربرداری" },
  { value: "Pathology", label: "پاتولوژی" },
  { value: "ECG", label: "نوار قلب" },
  { value: "Endoscopy", label: "اندوسکوپی" },
  { value: "Other", label: "سایر" },
];

export const measurementTypeOptions: HealthOption[] = [
  { value: "Weight", label: "وزن" },
  { value: "Height", label: "قد" },
  { value: "BMI", label: "شاخص توده بدنی" },
  { value: "BloodPressureSystolic", label: "فشار خون سیستولیک" },
  { value: "BloodPressureDiastolic", label: "فشار خون دیاستولیک" },
  { value: "HeartRate", label: "ضربان قلب" },
  { value: "Temperature", label: "دما" },
  { value: "SpO2", label: "اشباع اکسیژن" },
  { value: "FastingBloodGlucose", label: "قند خون ناشتا" },
  { value: "RandomBloodGlucose", label: "قند خون تصادفی" },
  { value: "HbA1c", label: "HbA1c" },
  { value: "LDL", label: "LDL" },
  { value: "HDL", label: "HDL" },
  { value: "Triglycerides", label: "تری‌گلیسیرید" },
  { value: "Creatinine", label: "کراتینین" },
  { value: "eGFR", label: "eGFR" },
  { value: "Other", label: "سایر" },
];

export const vitalSignMeasurementTypes = [
  "BloodPressureSystolic",
  "BloodPressureDiastolic",
  "HeartRate",
  "Temperature",
  "SpO2",
];

export const labMeasurementTypes = [
  "FastingBloodGlucose",
  "RandomBloodGlucose",
  "HbA1c",
  "LDL",
  "HDL",
  "Triglycerides",
  "Creatinine",
  "eGFR",
];

export const lifestyleMeasurementTypes = ["Weight", "Height", "BMI"];

export const defaultPriorityMeasurementTypes = [
  "BloodPressureSystolic",
  "BloodPressureDiastolic",
  "Weight",
  "BMI",
  "FastingBloodGlucose",
  "RandomBloodGlucose",
  "HbA1c",
  "LDL",
];

export const abnormalStatusOptions: HealthOption[] = [
  { value: "true", label: "بله" },
  { value: "false", label: "خیر" },
];
