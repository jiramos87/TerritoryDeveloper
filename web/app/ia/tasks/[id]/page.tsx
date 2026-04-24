/**
 * IA dev DB-primary refactor — Step 10 task detail page.
 *
 * @see `docs/ia-dev-db-refactor-implementation.md` Step 10
 */
import Link from "next/link";
import { notFound } from "next/navigation";
import { getTask, getTaskBody } from "@/lib/ia/queries";

export const dynamic = "force-dynamic";

type Params = Promise<{ id: string }>;

export default async function TaskDetailPage({ params }: { params: Params }) {
  const { id } = await params;
  const detail = await getTask(id);
  if (!detail) notFound();
  const { task, deps, commits, history_count } = detail;
  const bodyOut = await getTaskBody(id);

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
        {task.slug && (
          <>
            <span className="opacity-50">{"//"}</span>
            <Link href={`/ia/${task.slug}`} className="hover:text-neutral-200">
              {task.slug}
            </Link>
          </>
        )}
        <span className="opacity-50">{"//"}</span>
        <span className="text-blue-400">{task.task_id}</span>
      </div>

      <header>
        <h1 className="text-xl font-bold">
          {task.task_id} — {task.title}
        </h1>
        <p className="mt-1 text-xs text-neutral-500">
          {task.status}
          {task.stage_id && ` · stage ${task.stage_id}`}
          {task.priority && ` · ${task.priority}`}
          {task.type && ` · ${task.type}`}
          {" · history "}
          {history_count}
        </p>
      </header>

      {task.notes && (
        <section className="rounded border border-neutral-800 p-3 text-xs text-neutral-400">
          <span className="text-neutral-500">Notes:</span> {task.notes}
        </section>
      )}

      {deps.length > 0 && (
        <section className="rounded border border-neutral-800 p-3">
          <h2 className="mb-2 text-xs uppercase tracking-wide text-neutral-500">Deps</h2>
          <ul className="space-y-1 text-xs">
            {deps.map((d) => (
              <li key={`${d.kind}-${d.depends_on_id}`} className="flex gap-2">
                <span className="text-amber-400">{d.kind}</span>
                <Link
                  href={`/ia/tasks/${d.depends_on_id}`}
                  className="text-blue-400 hover:underline"
                >
                  {d.depends_on_id}
                </Link>
              </li>
            ))}
          </ul>
        </section>
      )}

      <section className="rounded border border-neutral-800 p-3">
        <h2 className="mb-2 text-xs uppercase tracking-wide text-neutral-500">Body</h2>
        {bodyOut?.body ? (
          <pre className="whitespace-pre-wrap text-xs text-neutral-300">{bodyOut.body}</pre>
        ) : (
          <p className="text-neutral-500">No body.</p>
        )}
      </section>

      {commits.length > 0 && (
        <section className="rounded border border-neutral-800 p-3">
          <h2 className="mb-2 text-xs uppercase tracking-wide text-neutral-500">
            Commits (latest 50)
          </h2>
          <ul className="space-y-1 text-xs">
            {commits.map((c) => (
              <li key={c.id} className="flex flex-wrap gap-2 border-t border-neutral-800/60 pt-1 first:border-t-0 first:pt-0">
                <span className="text-neutral-500">{new Date(c.recorded_at).toISOString()}</span>
                <span className="text-amber-400">{c.commit_kind}</span>
                <code className="text-neutral-500">{c.commit_sha.slice(0, 7)}</code>
                <span className="text-neutral-300">{c.message ?? "—"}</span>
              </li>
            ))}
          </ul>
        </section>
      )}
    </main>
  );
}
