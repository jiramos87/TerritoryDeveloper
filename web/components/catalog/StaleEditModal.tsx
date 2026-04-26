"use client";

import { useMemo, useState } from "react";

import { deepDiff, type DiffEntry } from "@/lib/catalog/json-deep-diff";

/**
 * 3-way diff modal surfaced when an `If-Match` save returns 409 stale (DEC-A38).
 *
 * Columns (left → right):
 *   1. **loaded** — payload the author saw at edit start
 *   2. **current** — fresh server payload returned in the 409 envelope
 *   3. **pending** — author's local edits that triggered the save
 *
 * User actions:
 *   - **Reload**: discard local edits, refetch and re-render → `onReload()`
 *   - **Show diff**: reveal/collapse the 3-column diff inline
 *   - **Re-save**: per-field pick `current` vs `pending`; emit merged payload
 *     paired with the fresh `current_updated_at` fingerprint → `onResave(merged)`
 *
 * Kind-agnostic — the modal accepts JSON-shaped payloads + currentUpdatedAt.
 *
 * @see TECH-1616 — Optimistic concurrency middleware + 409 handler
 */

type JsonValue = unknown;

export type StaleEditModalProps = {
  loaded: JsonValue;
  current: JsonValue;
  pending: JsonValue;
  /** Server-fresh fingerprint to round-trip on the resave. */
  currentUpdatedAt: string;
  /** Discard local edits → caller refetches. */
  onReload: () => void;
  /** Optional toggle hook for analytics. */
  onShowDiff?: (visible: boolean) => void;
  /** Per-field merged payload + fresh fingerprint. */
  onResave: (merged: JsonValue, currentUpdatedAt: string) => void;
  /** Optional dismiss without action (e.g. close ×). */
  onDismiss?: () => void;
};

type Side = "current" | "pending";

const DIFF_PALETTE: Record<DiffEntry["status"], string> = {
  added: "var(--ds-bg-status-done)",
  removed: "var(--ds-bg-status-blocked)",
  changed: "var(--ds-bg-status-progress)",
};

export default function StaleEditModal({
  loaded,
  current,
  pending,
  currentUpdatedAt,
  onReload,
  onShowDiff,
  onResave,
  onDismiss,
}: StaleEditModalProps) {
  const [showDiff, setShowDiff] = useState(false);
  const [picks, setPicks] = useState<Record<string, Side>>({});

  const diffs = useMemo(() => buildPathTable(loaded, current, pending), [loaded, current, pending]);

  const toggleDiff = () => {
    const next = !showDiff;
    setShowDiff(next);
    onShowDiff?.(next);
  };

  const setPick = (path: string, side: Side) => {
    setPicks((prev) => ({ ...prev, [path]: side }));
  };

  const handleResave = () => {
    const merged = applyPicks(pending, current, picks);
    onResave(merged, currentUpdatedAt);
  };

  return (
    <div
      role="dialog"
      aria-modal="true"
      aria-label="Stale edit"
      data-testid="stale-edit-modal"
      style={{
        position: "fixed",
        inset: 0,
        background: "var(--ds-overlay-panel)",
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        zIndex: 50,
      }}
    >
      <div
        style={{
          background: "var(--ds-bg-panel)",
          color: "var(--ds-text-primary)",
          padding: "var(--ds-spacing-lg)",
          maxWidth: "80vw",
          maxHeight: "85vh",
          overflow: "auto",
          border: "1px solid var(--ds-border-strong)",
        }}
      >
        <header style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
          <h2 style={{ margin: 0, fontSize: "var(--ds-font-size-h3)" }}>Save conflict</h2>
          {onDismiss ? (
            <button
              type="button"
              aria-label="Close"
              onClick={onDismiss}
              style={{ background: "transparent", border: 0, color: "var(--ds-text-muted)", cursor: "pointer" }}
            >
              ×
            </button>
          ) : null}
        </header>

        <p style={{ marginTop: "var(--ds-spacing-sm)" }}>
          Someone else updated this record while you were editing. Resolve the conflict before saving.
        </p>

        <div style={{ display: "flex", gap: "var(--ds-spacing-sm)", margin: "var(--ds-spacing-md) 0" }}>
          <button
            type="button"
            data-testid="stale-modal-reload"
            onClick={onReload}
            style={ctaStyle("var(--ds-text-accent-critical)")}
          >
            Reload (lose my changes)
          </button>
          <button
            type="button"
            data-testid="stale-modal-toggle-diff"
            onClick={toggleDiff}
            style={ctaStyle("var(--ds-text-accent-info)")}
          >
            {showDiff ? "Hide diff" : "Show diff"}
          </button>
          <button
            type="button"
            data-testid="stale-modal-resave"
            onClick={handleResave}
            style={ctaStyle("var(--ds-text-accent-warn)")}
          >
            Re-save with refreshed fingerprint
          </button>
        </div>

        {showDiff ? (
          <table
            data-testid="stale-modal-diff-table"
            style={{ width: "100%", borderCollapse: "collapse", fontSize: "var(--ds-font-size-body-sm)" }}
          >
            <thead>
              <tr>
                <th style={cellHeaderStyle()}>Path</th>
                <th style={cellHeaderStyle()} data-column="loaded">loaded</th>
                <th style={cellHeaderStyle()} data-column="current">current</th>
                <th style={cellHeaderStyle()} data-column="pending">pending</th>
                <th style={cellHeaderStyle()}>Pick</th>
              </tr>
            </thead>
            <tbody>
              {diffs.map((row) => {
                const picked = picks[row.path];
                return (
                  <tr key={row.path}>
                    <td style={cellStyle()}>{row.path}</td>
                    <td style={cellStyle()}>{stringify(row.loadedValue)}</td>
                    <td
                      style={{
                        ...cellStyle(),
                        background: row.currentChanged ? DIFF_PALETTE.changed : undefined,
                      }}
                    >
                      {stringify(row.currentValue)}
                    </td>
                    <td
                      style={{
                        ...cellStyle(),
                        background: row.pendingChanged ? DIFF_PALETTE.changed : undefined,
                      }}
                    >
                      {stringify(row.pendingValue)}
                    </td>
                    <td style={cellStyle()}>
                      <label style={{ marginRight: "var(--ds-spacing-xs)" }}>
                        <input
                          type="radio"
                          name={`pick-${row.path}`}
                          checked={picked === "current"}
                          onChange={() => setPick(row.path, "current")}
                          aria-label={`Pick current for ${row.path}`}
                        />
                        current
                      </label>
                      <label>
                        <input
                          type="radio"
                          name={`pick-${row.path}`}
                          checked={picked === "pending"}
                          onChange={() => setPick(row.path, "pending")}
                          aria-label={`Pick pending for ${row.path}`}
                        />
                        pending
                      </label>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        ) : null}
      </div>
    </div>
  );
}

function ctaStyle(borderColor: string): React.CSSProperties {
  return {
    background: "var(--ds-bg-canvas)",
    border: `1px solid ${borderColor}`,
    color: "var(--ds-text-primary)",
    padding: "var(--ds-spacing-xs) var(--ds-spacing-md)",
    cursor: "pointer",
  };
}

function cellHeaderStyle(): React.CSSProperties {
  return {
    textAlign: "left",
    borderBottom: "1px solid var(--ds-border-subtle)",
    padding: "var(--ds-spacing-xs)",
  };
}

function cellStyle(): React.CSSProperties {
  return {
    padding: "var(--ds-spacing-xs)",
    borderBottom: "1px solid var(--ds-border-subtle)",
    verticalAlign: "top",
  };
}

function stringify(value: unknown): string {
  if (value === undefined) return "—";
  if (typeof value === "string") return value;
  try {
    return JSON.stringify(value);
  } catch {
    return String(value);
  }
}

/* ------------------------------------------------------------------ */
/* Pure helpers — exported for unit tests                              */
/* ------------------------------------------------------------------ */

export type DiffRow = {
  path: string;
  loadedValue: unknown;
  currentValue: unknown;
  pendingValue: unknown;
  currentChanged: boolean;
  pendingChanged: boolean;
};

export function buildPathTable(loaded: unknown, current: unknown, pending: unknown): DiffRow[] {
  const left = deepDiff(loaded, current);
  const right = deepDiff(loaded, pending);
  const paths = new Set<string>();
  for (const e of left) paths.add(e.path);
  for (const e of right) paths.add(e.path);

  const leftByPath = new Map(left.map((e) => [e.path, e]));
  const rightByPath = new Map(right.map((e) => [e.path, e]));

  return [...paths].map((path) => {
    const lEntry = leftByPath.get(path);
    const rEntry = rightByPath.get(path);
    return {
      path,
      loadedValue: lEntry?.base ?? rEntry?.base ?? readPath(loaded, path),
      currentValue: lEntry?.other ?? readPath(current, path),
      pendingValue: rEntry?.other ?? readPath(pending, path),
      currentChanged: !!lEntry,
      pendingChanged: !!rEntry,
    };
  });
}

/** Apply per-path picks against the pending payload, layering in `current` selections. */
export function applyPicks(
  pending: unknown,
  current: unknown,
  picks: Record<string, Side>,
): unknown {
  let merged = clone(pending);
  for (const [path, side] of Object.entries(picks)) {
    if (side === "current") merged = writePath(merged, path, readPath(current, path));
    // side === "pending" → no change (pending is already the base).
  }
  return merged;
}

/* ------------------------------------------------------------------ */
/* Path utilities                                                      */
/* ------------------------------------------------------------------ */

type PathSegment = { kind: "key"; value: string } | { kind: "index"; value: number };

function parsePath(path: string): PathSegment[] {
  if (path === "$" || path === "") return [];
  const segments: PathSegment[] = [];
  // Tokenize `a.b[2].c` → key("a"), key("b"), index(2), key("c")
  const re = /([^.[\]]+)|\[(\d+)\]/g;
  let match: RegExpExecArray | null;
  while ((match = re.exec(path)) !== null) {
    if (match[1] !== undefined) segments.push({ kind: "key", value: match[1] });
    else if (match[2] !== undefined) segments.push({ kind: "index", value: Number(match[2]) });
  }
  return segments;
}

function readPath(root: unknown, path: string): unknown {
  const segments = parsePath(path);
  let cursor: unknown = root;
  for (const seg of segments) {
    if (cursor == null) return undefined;
    if (seg.kind === "key" && typeof cursor === "object" && !Array.isArray(cursor)) {
      cursor = (cursor as Record<string, unknown>)[seg.value];
    } else if (seg.kind === "index" && Array.isArray(cursor)) {
      cursor = cursor[seg.value];
    } else {
      return undefined;
    }
  }
  return cursor;
}

function writePath(root: unknown, path: string, value: unknown): unknown {
  const segments = parsePath(path);
  if (segments.length === 0) return value;
  const next = clone(root);
  let cursor: unknown = next;
  for (let i = 0; i < segments.length - 1; i++) {
    const seg = segments[i]!;
    if (seg.kind === "key") {
      const obj = cursor as Record<string, unknown>;
      if (typeof obj[seg.value] !== "object" || obj[seg.value] === null) {
        obj[seg.value] = segments[i + 1]!.kind === "index" ? [] : {};
      }
      cursor = obj[seg.value];
    } else {
      const arr = cursor as unknown[];
      if (typeof arr[seg.value] !== "object" || arr[seg.value] === null) {
        arr[seg.value] = segments[i + 1]!.kind === "index" ? [] : {};
      }
      cursor = arr[seg.value];
    }
  }
  const last = segments[segments.length - 1]!;
  if (last.kind === "key") (cursor as Record<string, unknown>)[last.value] = value;
  else (cursor as unknown[])[last.value] = value;
  return next;
}

function clone<T>(value: T): T {
  return value === undefined ? value : (JSON.parse(JSON.stringify(value)) as T);
}
