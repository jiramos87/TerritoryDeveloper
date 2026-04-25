/**
 * IA dev DB-primary refactor — Step 10 plan detail page.
 *
 * @see `docs/ia-dev-db-refactor-implementation.md` Step 10
 */
import Link from "next/link";
import { notFound } from "next/navigation";
import { getMasterPlan } from "@/lib/ia/queries";

export const dynamic = "force-dynamic";

type Params = Promise<{ slug: string }>;

export default async function PlanDetailPage({ params }: { params: Params }) {
  const { slug } = await params;
  const detail = await getMasterPlan(slug);
  if (!detail) notFound();

  const { plan, preamble, stages, change_log } = detail;
  const pct = plan.task_count > 0 ? Math.round((plan.task_done_count / plan.task_count) * 100) : 0;

  return (
    <main className="mx-auto max-w-5xl space-y-6 px-4 py-8 font-mono text-sm">
      <div className="flex flex-wrap gap-2 text-[11px] uppercase tracking-wide text-neutral-500">
        <Link href="/" className="hover:text-neutral-200">
          Territory
        </Link>
        <span className="opacity-50">{"//"}</span>
        <Link href="/ia" className="hover:text-neutral-200">
          IA
        </Link>
        <span className="opacity-50">{"//"}</span>
        <span className="text-blue-400">{plan.slug}</span>
      </div>

      <header>
        <h1 className="text-xl font-bold">{plan.title}</h1>
        <p className="mt-1 text-xs text-neutral-500">
          {plan.slug} · {plan.task_done_count}/{plan.task_count} tasks ({pct}%) · {plan.stage_count} stages
          {plan.source_spec_path && (
            <>
              {" · "}
              <code>{plan.source_spec_path}</code>
            </>
          )}
        </p>
      </header>

      {preamble && (
        <section className="rounded border border-neutral-800 p-3">
          <h2 className="mb-2 text-xs uppercase tracking-wide text-neutral-500">Preamble</h2>
          <pre className="whitespace-pre-wrap text-xs text-neutral-300">{preamble}</pre>
        </section>
      )}

      <section className="space-y-4">
        <h2 className="text-base font-bold">Stages</h2>
        {stages.length === 0 ? (
          <p className="text-neutral-500">No stages.</p>
        ) : (
          stages.map((s) => {
            const stagePct =
              s.task_count > 0 ? Math.round((s.task_done_count / s.task_count) * 100) : 0;
            return (
              <div key={s.stage_id} className="rounded border border-neutral-800 p-3">
                <div className="flex flex-wrap items-baseline gap-3">
                  <span className="font-bold text-blue-400">Stage {s.stage_id}</span>
                  {s.title && <span className="text-neutral-200">{s.title}</span>}
                  <span className="ml-auto text-xs text-neutral-400">
                    {s.task_done_count}/{s.task_count} ({stagePct}%) · {s.status}
                  </span>
                </div>
                {s.objective && (
                  <p className="mt-2 text-xs text-neutral-400">
                    <span className="text-neutral-500">Objective:</span> {s.objective}
                  </p>
                )}
                {s.tasks.length > 0 && (
                  <ul className="mt-2 space-y-1">
                    {s.tasks.map((t) => (
                      <li
                        key={t.task_id}
                        className="flex flex-wrap items-baseline gap-2 border-t border-neutral-800/60 pt-1 first:border-t-0 first:pt-0"
                      >
                        <Link
                          href={`/ia/tasks/${t.task_id}`}
                          className="font-bold text-blue-400 hover:underline"
                        >
                          {t.task_id}
                        </Link>
                        <span className="text-neutral-300">{t.title}</span>
                        <span className="ml-auto text-xs text-neutral-500">
                          {t.status}
                          {t.priority ? ` · ${t.priority}` : ""}
                        </span>
                      </li>
                    ))}
                  </ul>
                )}
              </div>
            );
          })
        )}
      </section>

      {change_log.length > 0 && (
        <section className="rounded border border-neutral-800 p-3">
          <h2 className="mb-2 text-xs uppercase tracking-wide text-neutral-500">
            Change log (latest 50)
          </h2>
          <ul className="space-y-1 text-xs">
            {change_log.map((e) => (
              <li key={e.entry_id} className="flex flex-wrap gap-2 border-t border-neutral-800/60 pt-1 first:border-t-0 first:pt-0">
                <span className="text-neutral-500">{new Date(e.ts).toISOString()}</span>
                <span className="text-amber-400">{e.kind}</span>
                {e.actor && <span className="text-neutral-500">{e.actor}</span>}
                {e.commit_sha && <code className="text-neutral-500">{e.commit_sha.slice(0, 7)}</code>}
                <span className="text-neutral-300">{e.body}</span>
              </li>
            ))}
          </ul>
        </section>
      )}
    </main>
  );
}
