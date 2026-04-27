"use client";

import Link from "next/link";
import { useEffect, useState } from "react";

import { deepDiff, type DiffResult } from "@/lib/catalog/json-deep-diff";

/**
 * Entity-version upgrade preview client (TECH-2462).
 * Loads runner output via GET /preview, renders side-by-side diff via
 * `deepDiff`, then POSTs to /upgrade on confirm.
 */
type PreviewPayload = {
  ok: "ok" | "error" | true;
  data?: {
    before: Record<string, unknown>;
    after: Record<string, unknown>;
    warnings: Array<{ path: string; message: string }>;
  };
  error?: { code: string; message: string };
};

type UpgradeResp = {
  ok: "ok" | "error" | true;
  data?: { new_version_id: string; warnings: Array<{ path: string; message: string }> };
  error?: { code: string; message: string };
};

export type UpgradePreviewClientProps = {
  entityId: string;
  sourceVersionId: string | null;
  targetArchetypeVersionId: string | null;
};

export default function UpgradePreviewClient({
  entityId,
  sourceVersionId,
  targetArchetypeVersionId,
}: UpgradePreviewClientProps) {
  const missingParams = !sourceVersionId || !targetArchetypeVersionId;
  const [before, setBefore] = useState<Record<string, unknown> | null>(null);
  const [after, setAfter] = useState<Record<string, unknown> | null>(null);
  const [warnings, setWarnings] = useState<Array<{ path: string; message: string }>>([]);
  const [loading, setLoading] = useState<boolean>(!missingParams);
  const [error, setError] = useState<string | null>(
    missingParams ? "Missing source_version_id or target_archetype_version_id query params" : null,
  );
  const [busy, setBusy] = useState<boolean>(false);
  const [statusMsg, setStatusMsg] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    if (!sourceVersionId || !targetArchetypeVersionId) {
      return () => {
        cancelled = true;
      };
    }
    const url = `/api/catalog/entity-versions/upgrade/preview?source_version_id=${encodeURIComponent(sourceVersionId)}&target_archetype_version_id=${encodeURIComponent(targetArchetypeVersionId)}`;
    fetch(url)
      .then((res) => res.json() as Promise<PreviewPayload>)
      .then((payload) => {
        if (cancelled) return;
        if ((payload.ok !== "ok" && payload.ok !== true) || !payload.data) {
          setError(payload.error?.message ?? "Failed to load preview");
          setLoading(false);
          return;
        }
        setBefore(payload.data.before);
        setAfter(payload.data.after);
        setWarnings(payload.data.warnings);
        setLoading(false);
      })
      .catch((e: unknown) => {
        if (cancelled) return;
        setError(e instanceof Error ? e.message : "Network error");
        setLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, [sourceVersionId, targetArchetypeVersionId]);

  const handleConfirm = async () => {
    if (!sourceVersionId || !targetArchetypeVersionId) return;
    setBusy(true);
    setStatusMsg(null);
    try {
      const res = await fetch(`/api/catalog/entity-versions/upgrade`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          entity_version_id: sourceVersionId,
          target_archetype_version_id: targetArchetypeVersionId,
        }),
      });
      const payload = (await res.json()) as UpgradeResp;
      if (!res.ok || (payload.ok !== "ok" && payload.ok !== true) || !payload.data) {
        setStatusMsg(payload.error?.message ?? "Upgrade failed");
        setBusy(false);
        return;
      }
      setStatusMsg(`Upgraded to entity_version ${payload.data.new_version_id} (draft).`);
      setBusy(false);
    } catch (e: unknown) {
      setStatusMsg(e instanceof Error ? e.message : "Network error");
      setBusy(false);
    }
  };

  if (loading) {
    return (
      <p data-testid="upgrade-preview-loading" className="text-[var(--ds-text-muted)]">
        Loading upgrade preview...
      </p>
    );
  }
  if (error) {
    return (
      <p
        data-testid="upgrade-preview-error"
        role="alert"
        className="text-[var(--ds-text-accent-critical)]"
      >
        {error}
      </p>
    );
  }
  if (before == null || after == null) {
    return (
      <p data-testid="upgrade-preview-empty" className="text-[var(--ds-text-muted)]">
        No preview data.
      </p>
    );
  }

  const diff: DiffResult = deepDiff(before, after);

  return (
    <div data-testid="upgrade-preview" className="flex flex-col gap-[var(--ds-spacing-md)]">
      <header className="flex items-center justify-between">
        <h1 className="text-[length:var(--ds-font-size-h2)] font-semibold">Upgrade preview</h1>
        <div className="flex items-center gap-[var(--ds-spacing-xs)]">
          <Link
            href={`/catalog/entities/${encodeURIComponent(entityId)}`}
            data-testid="upgrade-preview-cancel"
            className="rounded border border-[var(--ds-border-subtle)] px-[var(--ds-spacing-sm)] py-[var(--ds-spacing-xs)]"
          >
            Cancel
          </Link>
          <button
            type="button"
            data-testid="upgrade-preview-confirm"
            disabled={busy}
            onClick={() => void handleConfirm()}
            className="rounded bg-[var(--ds-text-accent-info)] px-[var(--ds-spacing-md)] py-[var(--ds-spacing-xs)] text-[var(--ds-bg-canvas)] disabled:opacity-50"
          >
            {busy ? "Upgrading..." : "Confirm upgrade"}
          </button>
        </div>
      </header>

      {statusMsg ? (
        <p data-testid="upgrade-preview-status" role="status" className="text-[var(--ds-text-muted)]">
          {statusMsg}
        </p>
      ) : null}

      {warnings.length > 0 ? (
        <ul
          data-testid="upgrade-preview-warnings"
          role="status"
          className="rounded border border-[var(--ds-text-accent-warning)] p-[var(--ds-spacing-xs)] text-[var(--ds-text-accent-warning)]"
        >
          {warnings.map((w, i) => (
            <li key={i}>
              <code>{w.path}</code>: {w.message}
            </li>
          ))}
        </ul>
      ) : null}

      <section className="grid grid-cols-2 gap-[var(--ds-spacing-md)]">
        <div data-testid="upgrade-preview-before">
          <h2 className="text-[length:var(--ds-font-size-h3)] font-semibold">Before</h2>
          <pre className="overflow-auto rounded border border-[var(--ds-border-subtle)] p-[var(--ds-spacing-xs)] text-[length:var(--ds-font-size-mono)]">
            {JSON.stringify(before, null, 2)}
          </pre>
        </div>
        <div data-testid="upgrade-preview-after">
          <h2 className="text-[length:var(--ds-font-size-h3)] font-semibold">After</h2>
          <pre className="overflow-auto rounded border border-[var(--ds-border-subtle)] p-[var(--ds-spacing-xs)] text-[length:var(--ds-font-size-mono)]">
            {JSON.stringify(after, null, 2)}
          </pre>
        </div>
      </section>

      <section data-testid="upgrade-preview-diff">
        <h2 className="text-[length:var(--ds-font-size-h3)] font-semibold">Changes</h2>
        {diff.length === 0 ? (
          <p className="text-[var(--ds-text-muted)]">No changes detected.</p>
        ) : (
          <ul>
            {diff.map((entry, i) => (
              <li key={i}>
                <code>{entry.path}</code>: <span className="text-[var(--ds-text-muted)]">{entry.status}</span>
              </li>
            ))}
          </ul>
        )}
      </section>
    </div>
  );
}
