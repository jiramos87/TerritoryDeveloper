"use client";

import { useEffect, useState } from "react";

import EntityRefPicker, { type EntityRefRow } from "@/components/catalog/EntityRefPicker";
import { semanticCycleCheck } from "@/lib/tokens/semantic-cycle-check";
import type { CatalogTokenSemanticValue } from "@/types/api/catalog-api";

/**
 * Semantic token editor (TECH-2093 / Stage 10.1).
 *
 * Two surfaces:
 *  - `<EntityRefPicker accepts_kind={["token"]}>` for the alias target.
 *  - text `<input>` for `token_role`.
 *
 * Pre-flight cycle check via {@link semanticCycleCheck} per DEC-A44 — fetcher
 * hits `/api/catalog/tokens/[slug]` to follow the alias chain client-side and
 * surfaces a banner when a cycle would form. Banner is advisory; the server
 * gate in `patchTokenSpine` is authoritative.
 *
 * @see ia/projects/asset-pipeline/stage-10.1 — TECH-2093 §Plan Digest
 */

export type SemanticTokenEditorProps = {
  /** Self entity_id (numeric, parsed by caller from `dto.entity_id`). */
  selfEntityId: number;
  value: CatalogTokenSemanticValue;
  /** Resolved target row (from `dto.semantic_target_resolution`) or null. */
  targetRow: EntityRefRow | null;
  /** Bare target id when row not yet resolved. */
  targetId: number | null;
  /** Called when user picks/clears alias target. */
  onTargetChange: (entityId: number | null, row: EntityRefRow | null) => void;
  /** Called when token_role text changes. */
  onValueChange: (next: CatalogTokenSemanticValue) => void;
  /** Called when cycle status flips so parent can disable Save. */
  onCycleChange: (cycle: boolean) => void;
  disabled?: boolean;
};

type CycleState = { cycle: boolean; path: number[] } | null;

export default function SemanticTokenEditor({
  selfEntityId,
  value,
  targetRow,
  targetId,
  onTargetChange,
  onValueChange,
  onCycleChange,
  disabled,
}: SemanticTokenEditorProps) {
  const [cycleState, setCycleState] = useState<CycleState>(null);

  useEffect(() => {
    let cancelled = false;
    if (targetId == null) {
      // Defer the reset out of the synchronous render path so the effect body
      // does not call setState directly (lint rule react-hooks/set-state-in-effect).
      const handle = setTimeout(() => {
        if (cancelled) return;
        setCycleState(null);
        onCycleChange(false);
      }, 0);
      return () => {
        cancelled = true;
        clearTimeout(handle);
      };
    }
    void semanticCycleCheck(selfEntityId, targetId, async (id) => {
      const url = `/api/catalog/tokens/by-id/${id}`;
      try {
        const res = await fetch(url);
        if (!res.ok) return null;
        const payload = (await res.json()) as {
          ok?: unknown;
          data?: { token_detail?: { semantic_target_entity_id?: string | null } | null };
        };
        const tgt = payload?.data?.token_detail?.semantic_target_entity_id ?? null;
        return tgt == null ? null : Number.parseInt(tgt, 10);
      } catch {
        return null;
      }
    }).then((res) => {
      if (cancelled) return;
      setCycleState(res);
      onCycleChange(res.cycle);
    });
    return () => {
      cancelled = true;
    };
  }, [selfEntityId, targetId, onCycleChange]);

  return (
    <div data-testid="semantic-token-editor" className="flex flex-col gap-[var(--ds-spacing-sm)]">
      <EntityRefPicker
        testId="semantic-token-editor-target"
        label="Alias target (semantic → token)"
        accepts_kind={["token"]}
        value={targetRow}
        valueId={targetRow == null && targetId != null ? String(targetId) : null}
        disabled={disabled}
        onChange={(id, row) => onTargetChange(id == null ? null : Number.parseInt(id, 10), row)}
      />

      <label className="flex flex-col gap-[var(--ds-spacing-xs)]">
        <span className="text-[var(--ds-text-muted)]">Token role</span>
        <input
          type="text"
          data-testid="semantic-token-editor-role"
          value={value.token_role}
          disabled={disabled}
          onChange={(e) => onValueChange({ token_role: e.currentTarget.value })}
          className="rounded border border-[var(--ds-border-subtle)] bg-[var(--ds-bg-panel)] px-[var(--ds-spacing-sm)] py-[var(--ds-spacing-xs)] font-mono"
          placeholder="e.g. surface.elevated"
        />
      </label>

      {cycleState?.cycle ? (
        <p
          data-testid="semantic-token-editor-cycle-banner"
          role="alert"
          className="rounded border border-[var(--ds-text-accent-critical)] bg-[var(--ds-bg-panel)] p-[var(--ds-spacing-sm)] text-[var(--ds-text-accent-critical)]"
        >
          Alias would create a cycle: {cycleState.path.join(" → ")}. Save disabled.
        </p>
      ) : null}
    </div>
  );
}
