"use client";

import { useMemo } from "react";

import type { SchemaDiff } from "@/lib/archetype/diff-schemas";
import type { MigrationHint } from "@/lib/archetype/migration-hint-validator";

/**
 * Three-section migration-hint editor (TECH-2461).
 * Sections: Added (default value collector), Removed (drop OR rename target),
 * Renamed (heuristic suggestions overridable).
 *
 * Controlled component — caller owns `hint` state. Value shape matches
 * `migration_hint_json` jsonb on `entity_version`.
 */
export type MigrationHintEditorProps = {
  diff: SchemaDiff;
  hint: MigrationHint;
  onChange: (next: MigrationHint) => void;
};

type RemovedRule = "drop" | "rename" | "none";

function ruleForRemoved(hint: MigrationHint, slug: string): {
  kind: RemovedRule;
  to?: string;
} {
  const r = (hint.rename ?? []).find((x) => x.from === slug);
  if (r) return { kind: "rename", to: r.to };
  if ((hint.drop ?? []).some((x) => x.slug === slug)) return { kind: "drop" };
  return { kind: "none" };
}

function defaultFor(hint: MigrationHint, slug: string): unknown {
  return (hint.default ?? []).find((d) => d.slug === slug)?.value;
}

function setDefault(hint: MigrationHint, slug: string, value: unknown): MigrationHint {
  const others = (hint.default ?? []).filter((d) => d.slug !== slug);
  const next = value === undefined ? others : [...others, { slug, value }];
  return { ...hint, default: next };
}

function setRemovedRule(
  hint: MigrationHint,
  slug: string,
  rule: RemovedRule,
  to?: string,
): MigrationHint {
  const renameOthers = (hint.rename ?? []).filter((x) => x.from !== slug);
  const dropOthers = (hint.drop ?? []).filter((x) => x.slug !== slug);
  if (rule === "drop") {
    return { ...hint, rename: renameOthers, drop: [...dropOthers, { slug }] };
  }
  if (rule === "rename" && to) {
    return {
      ...hint,
      rename: [...renameOthers, { from: slug, to }],
      drop: dropOthers,
    };
  }
  return { ...hint, rename: renameOthers, drop: dropOthers };
}

export default function MigrationHintEditor({
  diff,
  hint,
  onChange,
}: MigrationHintEditorProps) {
  const renamedFromSet = useMemo(
    () => new Set((hint.rename ?? []).map((r) => r.from)),
    [hint.rename],
  );

  return (
    <div
      data-testid="migration-hint-editor"
      className="flex flex-col gap-[var(--ds-spacing-md)]"
    >
      <section
        data-testid="migration-hint-added"
        className="flex flex-col gap-[var(--ds-spacing-xs)]"
      >
        <h3 className="text-[length:var(--ds-font-size-h3)] font-semibold">
          Added fields
        </h3>
        {diff.added.length === 0 ? (
          <p className="text-[var(--ds-text-muted)]">None added.</p>
        ) : (
          diff.added.map((a) => (
            <label
              key={a.slug}
              data-testid={`migration-hint-added-${a.slug}`}
              className="flex items-center gap-[var(--ds-spacing-xs)]"
            >
              <code>{a.slug}</code>
              <span className="text-[var(--ds-text-muted)]">({a.type})</span>
              <span>default:</span>
              <input
                data-testid={`migration-hint-added-${a.slug}-default`}
                type={a.type === "integer" || a.type === "number" ? "number" : "text"}
                value={(defaultFor(hint, a.slug) as string | number | undefined) ?? ""}
                onChange={(e) => {
                  const raw = e.target.value;
                  if (raw === "") {
                    onChange(setDefault(hint, a.slug, undefined));
                    return;
                  }
                  const v =
                    a.type === "integer"
                      ? Number.parseInt(raw, 10)
                      : a.type === "number"
                        ? Number(raw)
                        : raw;
                  onChange(setDefault(hint, a.slug, v));
                }}
                className="rounded border border-[var(--ds-border-subtle)] bg-[var(--ds-bg-canvas)] px-[var(--ds-spacing-xs)] py-[var(--ds-spacing-2xs)]"
              />
            </label>
          ))
        )}
      </section>

      <section
        data-testid="migration-hint-removed"
        className="flex flex-col gap-[var(--ds-spacing-xs)]"
      >
        <h3 className="text-[length:var(--ds-font-size-h3)] font-semibold">
          Removed fields
        </h3>
        {diff.removed.length === 0 ? (
          <p className="text-[var(--ds-text-muted)]">None removed.</p>
        ) : (
          diff.removed.map((r) => {
            const rule = ruleForRemoved(hint, r.slug);
            const sameTypeAdded = diff.added.filter((a) => a.type === r.type);
            return (
              <div
                key={r.slug}
                data-testid={`migration-hint-removed-${r.slug}`}
                className="flex items-center gap-[var(--ds-spacing-xs)]"
              >
                <code>{r.slug}</code>
                <span className="text-[var(--ds-text-muted)]">({r.type})</span>
                <select
                  data-testid={`migration-hint-removed-${r.slug}-rule`}
                  value={rule.kind}
                  onChange={(e) => {
                    const k = e.target.value as RemovedRule;
                    if (k === "rename") {
                      onChange(
                        setRemovedRule(hint, r.slug, "rename", sameTypeAdded[0]?.slug),
                      );
                      return;
                    }
                    onChange(setRemovedRule(hint, r.slug, k));
                  }}
                  className="rounded border border-[var(--ds-border-subtle)] bg-[var(--ds-bg-canvas)] px-[var(--ds-spacing-xs)] py-[var(--ds-spacing-2xs)]"
                >
                  <option value="none">— pick rule —</option>
                  <option value="drop">Drop</option>
                  {sameTypeAdded.length > 0 ? (
                    <option value="rename">Rename to...</option>
                  ) : null}
                </select>
                {rule.kind === "rename" ? (
                  <select
                    data-testid={`migration-hint-removed-${r.slug}-target`}
                    value={rule.to ?? ""}
                    onChange={(e) =>
                      onChange(setRemovedRule(hint, r.slug, "rename", e.target.value))
                    }
                    className="rounded border border-[var(--ds-border-subtle)] bg-[var(--ds-bg-canvas)] px-[var(--ds-spacing-xs)] py-[var(--ds-spacing-2xs)]"
                  >
                    {sameTypeAdded.map((a) => (
                      <option key={a.slug} value={a.slug}>
                        {a.slug}
                      </option>
                    ))}
                  </select>
                ) : null}
              </div>
            );
          })
        )}
      </section>

      <section
        data-testid="migration-hint-suggestions"
        className="flex flex-col gap-[var(--ds-spacing-xs)]"
      >
        <h3 className="text-[length:var(--ds-font-size-h3)] font-semibold">
          Rename suggestions
        </h3>
        {diff.renamed_candidates.length === 0 ? (
          <p className="text-[var(--ds-text-muted)]">No suggestions.</p>
        ) : (
          diff.renamed_candidates.map((c) => {
            const accepted = renamedFromSet.has(c.from);
            return (
              <div
                key={c.from}
                data-testid={`migration-hint-suggestion-${c.from}`}
                className="flex items-center gap-[var(--ds-spacing-xs)]"
              >
                <code>{c.from}</code>
                <span>→</span>
                <code>{c.to}</code>
                <span className="text-[var(--ds-text-muted)]">
                  (distance {c.distance})
                </span>
                <button
                  type="button"
                  data-testid={`migration-hint-suggestion-${c.from}-accept`}
                  disabled={accepted}
                  onClick={() => onChange(setRemovedRule(hint, c.from, "rename", c.to))}
                  className="rounded border border-[var(--ds-border-subtle)] px-[var(--ds-spacing-xs)] py-[var(--ds-spacing-2xs)] disabled:opacity-50"
                >
                  {accepted ? "Accepted" : "Accept"}
                </button>
              </div>
            );
          })
        )}
      </section>
    </div>
  );
}
