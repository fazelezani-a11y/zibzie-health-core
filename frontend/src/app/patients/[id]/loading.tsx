export default function PatientSummaryLoading() {
  return (
    <main className="mx-auto flex w-full max-w-6xl flex-1 flex-col gap-6 px-4 py-6 sm:px-6 lg:px-8">
      <section className="space-y-3 border-b border-slate-200 pb-6">
        <p className="text-sm font-medium text-teal-700">
          در حال دریافت خلاصه پرونده...
        </p>
        <div className="h-8 w-56 animate-pulse rounded bg-slate-200" />
        <div className="h-4 w-80 max-w-full animate-pulse rounded bg-slate-200" />
      </section>

      <section className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
        {Array.from({ length: 4 }).map((_, index) => (
          <div
            className="rounded-lg border border-slate-200 bg-white p-4 shadow-sm"
            key={index}
          >
            <div className="h-4 w-24 animate-pulse rounded bg-slate-200" />
            <div className="mt-3 h-5 w-36 animate-pulse rounded bg-slate-100" />
          </div>
        ))}
      </section>

      <section className="grid gap-4 lg:grid-cols-3">
        {Array.from({ length: 3 }).map((_, index) => (
          <div
            className="rounded-lg border border-slate-200 bg-white p-4 shadow-sm"
            key={index}
          >
            <div className="h-5 w-32 animate-pulse rounded bg-slate-200" />
            <div className="mt-5 space-y-3">
              <div className="h-4 w-full animate-pulse rounded bg-slate-100" />
              <div className="h-4 w-4/5 animate-pulse rounded bg-slate-100" />
            </div>
          </div>
        ))}
      </section>
    </main>
  );
}
