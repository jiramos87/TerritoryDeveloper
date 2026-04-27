"use client";

import PoolConditionsEditor from "@/components/catalog/PoolConditionsEditor";
import type { CatalogPoolMemberSpineRow } from "@/types/api/catalog-api";

/**
 * Pool member table (TECH-1788). Editable weight + conditions per row;
 * remove control per row; bulk-add handled by parent via
 * `<AssetMultiSelectModal>`.
 *
 * Pure presentational — parent owns the member array and applies diffs on
 * submit.
 */

export type PoolMemberDraft = CatalogPoolMemberSpineRow;

export type PoolMemberTableProps = {
  members: PoolMemberDraft[];
  onChange: (next: PoolMemberDraft[]) => void;
  onAddRequested: () => void;
  disabled?: boolean;
};

export default function PoolMemberTable({ members, onChange, onAddRequested, disabled }: PoolMemberTableProps) {
  function update(idx: number, patch: Partial<PoolMemberDraft>) {
    const next = members.slice();
    const cur = next[idx]!;
    next[idx] = { ...cur, ...patch };
    onChange(next);
  }
  function remove(idx: number) {
    const next = members.slice();
    next.splice(idx, 1);
    onChange(next);
  }

  return (
    <div data-testid="pool-member-table" className="flex flex-col gap-[var(--ds-spacing-sm)]">
      <header className="flex items-center justify-between">
        <h2 className="text-[length:var(--ds-font-size-h3)] font-semibold">Members</h2>
        {!disabled ? (
          <button type="button" data-testid="pool-member-table-add" onClick={onAddRequested}>
            + Add members
          </button>
        ) : null}
      </header>

      {members.length === 0 ? (
        <p data-testid="pool-member-table-empty" className="text-[var(--ds-text-muted)]">
          No members yet.
        </p>
      ) : (
        <ul data-testid="pool-member-table-rows" className="flex flex-col gap-[var(--ds-spacing-sm)]">
          {members.map((m, idx) => (
            <li
              key={m.asset_entity_id}
              data-testid={`pool-member-row-${m.slug}`}
              className="flex flex-col gap-[var(--ds-spacing-xs)] rounded border border-[var(--ds-border-subtle)] bg-[var(--ds-bg-panel)] p-[var(--ds-spacing-sm)]"
            >
              <div className="flex items-center gap-[var(--ds-spacing-md)]">
                <span className="font-mono text-[var(--ds-text-primary)]">{m.slug}</span>
                <span className="text-[var(--ds-text-primary)]">{m.display_name}</span>
                <label className="flex items-center gap-[var(--ds-spacing-xs)]">
                  <span className="text-[var(--ds-text-muted)]">Weight</span>
                  <input
                    type="number"
                    data-testid={`pool-member-row-weight-${m.slug}`}
                    min="1"
                    step="1"
                    value={m.weight}
                    onChange={(e) => update(idx, { weight: Math.max(1, Math.trunc(Number(e.currentTarget.value) || 1)) })}
                    disabled={disabled}
                  />
                </label>
                {!disabled ? (
                  <button
                    type="button"
                    data-testid={`pool-member-row-remove-${m.slug}`}
                    onClick={() => remove(idx)}
                    className="text-[var(--ds-text-muted)]"
                  >
                    Remove
                  </button>
                ) : null}
              </div>
              <PoolConditionsEditor
                value={m.conditions_json ?? {}}
                onChange={(next) => update(idx, { conditions_json: next })}
                testId={`pool-member-row-conditions-${m.slug}`}
                disabled={disabled}
              />
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
