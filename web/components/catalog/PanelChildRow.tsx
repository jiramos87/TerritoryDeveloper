"use client";

import { useState } from "react";

import PanelChildParamsEditor from "@/components/catalog/PanelChildParamsEditor";
import type { CatalogPanelChildKind } from "@/types/api/catalog-api";

/**
 * Single panel-child row (TECH-1886). Shows resolved child slug/display_name +
 * up/down reorder arrows + delete + expandable `params_json` editor.
 */

export type PanelChildRowState = {
  child_entity_id: string | null;
  child_kind: CatalogPanelChildKind;
  order_idx: number;
  params_json: Record<string, unknown>;
  resolved: { slug: string; display_name: string } | null;
};

export type PanelChildRowProps = {
  slotName: string;
  index: number;
  child: PanelChildRowState;
  isFirst: boolean;
  isLast: boolean;
  onMoveUp: () => void;
  onMoveDown: () => void;
  onDelete: () => void;
  onParamsChange: (next: Record<string, unknown>) => void;
};

export default function PanelChildRow({
  slotName,
  index,
  child,
  isFirst,
  isLast,
  onMoveUp,
  onMoveDown,
  onDelete,
  onParamsChange,
}: PanelChildRowProps) {
  const [expanded, setExpanded] = useState<boolean>(false);
  const childIdLabel = child.resolved?.slug ?? child.child_entity_id ?? `(${child.child_kind})`;
  const displayName = child.resolved?.display_name ?? "";
  const testIdBase = `panel-child-row-${slotName}-${index}`;

  return (
    <div
      data-testid={testIdBase}
      className="flex flex-col gap-[var(--ds-spacing-xs)] rounded border border-[var(--ds-border-subtle)] bg-[var(--ds-bg-panel)] p-[var(--ds-spacing-xs)]"
    >
      <div className="flex items-center gap-[var(--ds-spacing-xs)]">
        <span
          data-testid={`${testIdBase}-kind`}
          className="rounded px-[var(--ds-spacing-xs)] text-[var(--ds-text-muted)]"
        >
          {child.child_kind}
        </span>
        <span
          data-testid={`${testIdBase}-slug`}
          className="font-mono text-[var(--ds-text-primary)]"
        >
          {childIdLabel}
        </span>
        {displayName ? (
          <span
            data-testid={`${testIdBase}-name`}
            className="text-[var(--ds-text-muted)]"
          >
            {displayName}
          </span>
        ) : null}
        <span className="ml-auto flex items-center gap-[var(--ds-spacing-xs)]">
          <button
            type="button"
            data-testid={`${testIdBase}-up`}
            disabled={isFirst}
            onClick={onMoveUp}
            aria-label="Move up"
            className={
              isFirst
                ? "rounded border border-[var(--ds-border-subtle)] px-[var(--ds-spacing-xs)] py-[var(--ds-spacing-xs)] text-[var(--ds-text-muted)] opacity-40"
                : "rounded border border-[var(--ds-border-subtle)] px-[var(--ds-spacing-xs)] py-[var(--ds-spacing-xs)] text-[var(--ds-text-primary)]"
            }
          >
            ↑
          </button>
          <button
            type="button"
            data-testid={`${testIdBase}-down`}
            disabled={isLast}
            onClick={onMoveDown}
            aria-label="Move down"
            className={
              isLast
                ? "rounded border border-[var(--ds-border-subtle)] px-[var(--ds-spacing-xs)] py-[var(--ds-spacing-xs)] text-[var(--ds-text-muted)] opacity-40"
                : "rounded border border-[var(--ds-border-subtle)] px-[var(--ds-spacing-xs)] py-[var(--ds-spacing-xs)] text-[var(--ds-text-primary)]"
            }
          >
            ↓
          </button>
          <button
            type="button"
            data-testid={`${testIdBase}-toggle-params`}
            onClick={() => setExpanded((prev) => !prev)}
            aria-expanded={expanded}
            className="rounded border border-[var(--ds-border-subtle)] px-[var(--ds-spacing-xs)] py-[var(--ds-spacing-xs)] text-[var(--ds-text-muted)]"
          >
            params
          </button>
          <button
            type="button"
            data-testid={`${testIdBase}-delete`}
            onClick={onDelete}
            aria-label="Delete child"
            className="rounded border border-[var(--ds-border-subtle)] px-[var(--ds-spacing-xs)] py-[var(--ds-spacing-xs)] text-[var(--ds-text-accent-critical)]"
          >
            ×
          </button>
        </span>
      </div>
      {expanded ? (
        <PanelChildParamsEditor
          testId={`${testIdBase}-params`}
          value={child.params_json}
          onChange={onParamsChange}
        />
      ) : null}
    </div>
  );
}
