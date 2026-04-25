/**
 * IA dev DB-primary refactor — Step 10 dashboard list page.
 *
 * @see `docs/ia-dev-db-refactor-implementation.md` Step 10
 */
import Link from "next/link";
import { listMasterPlans, searchTaskSpecs } from "@/lib/ia/queries";

export const dynamic = "force-dynamic";

type Search = Promise<{ q?: string }>;

export default async function IaDashboardPage({ searchParams }: { searchParams: Search }) {
  const { q } = await searchParams;
  const query = (q ?? "").trim();

  let plans: Awaited<ReturnType<typeof listMasterPlans>> = [];
  let plansErr: string | null = null;
  try {
    plans = await listMasterPlans();
  } catch (e) {
    plansErr = e instanceof Error ? e.message : "Failed to load plans";
  }

  let hits: Awaited<ReturnType<typeof searchTaskSpecs>> = [];
  let searchErr: string | null = null;
  if (query) {
    try {
      hits = await searchTaskSpecs(query, 25);
    } catch (e) {
      searchErr = e instanceof Error ? e.message : "Search failed";
    }
  }

  return (
    <main className="mx-auto max-w-5xl space-y-8 px-4 py-8 font-mono text-sm">
      <header>
        <div className="mb-2 flex flex-wrap gap-2 text-[11px] uppercase tracking-wide text-neutral-500">
          <Link href="/" className="hover:text-neutral-200">
            Territory
          </Link>
          <span className="opacity-50">{"//"}</span>
          <span className="text-blue-400">IA Refactor Dashboard</span>
        </div>
        <h1 className="text-xl font-bold">IA — Master Plans + Task Search</h1>
        <p className="mt-1 text-xs text-neutral-500">
          DB-primary read surface (Step 10). Source of truth: <code>ia_*</code> tables.
        </p>
      </header>

      <section>
        <form method="get" className="flex gap-2">
          <input
            name="q"
            defaultValue={query}
            placeholder="Search task specs (FTS over body)…"
            className="flex-1 rounded border border-neutral-700 bg-neutral-900 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none"
          />
          <button
            type="submit"
            className="rounded border border-neutral-700 bg-neutral-800 px-4 py-2 text-sm hover:border-blue-500"
          >
            Search
          </button>
        </form>
        {query && (
          <div className="mt-3">
            {searchErr ? (
              <p className="text-red-400">Search error: {searchErr}</p>
            ) : hits.length === 0 ? (
              <p className="text-neutral-500">No matches for &ldquo;{query}&rdquo;.</p>
            ) : (
              <ul className="space-y-2">
                {hits.map((h) => (
                  <li key={h.task_id} className="rounded border border-neutral-800 p-3">
                    <div className="flex flex-wrap items-baseline gap-2">
                      <Link
                        href={`/ia/tasks/${h.task_id}`}
                        className="font-bold text-blue-400 hover:underline"
                      >
                        {h.task_id}
                      </Link>
                      <span className="text-neutral-300">{h.title}</span>
                      <span className="ml-auto text-xs text-neutral-500">
                        {h.status} · rank {h.rank.toFixed(3)}
                      </span>
                    </div>
                    <p
                      className="mt-1 text-xs text-neutral-400"
                      dangerouslySetInnerHTML={{
                        __html: h.snippet
                          .replace(/&/g, "&amp;")
                          .replace(/</g, "&lt;")
                          .replace(/>/g, "&gt;")
                          .replace(/&lt;&lt;/g, "<mark>")
                          .replace(/&gt;&gt;/g, "</mark>"),
                      }}
                    />
                  </li>
                ))}
              </ul>
            )}
          </div>
        )}
      </section>

      <section>
        <h2 className="mb-3 text-base font-bold">Master plans</h2>
        {plansErr ? (
          <p className="text-red-400">Plans error: {plansErr}</p>
        ) : plans.length === 0 ? (
          <p className="text-neutral-500">No master plans in DB.</p>
        ) : (
          <ul className="space-y-1">
            {plans.map((p) => {
              const pct = p.task_count > 0 ? Math.round((p.task_done_count / p.task_count) * 100) : 0;
              return (
                <li
                  key={p.slug}
                  className="flex flex-wrap items-baseline gap-3 rounded border border-neutral-800 p-3"
                >
                  <Link
                    href={`/ia/${p.slug}`}
                    className="font-bold text-blue-400 hover:underline"
                  >
                    {p.title}
                  </Link>
                  <span className="text-xs text-neutral-500">{p.slug}</span>
                  <span className="ml-auto text-xs text-neutral-400">
                    {p.task_done_count}/{p.task_count} tasks ({pct}%) · {p.stage_count} stages
                  </span>
                </li>
              );
            })}
          </ul>
        )}
      </section>
    </main>
  );
}
