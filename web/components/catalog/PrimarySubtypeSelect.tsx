"use client";

import type { EntityRefRow } from "@/components/catalog/EntityRefPicker";

/**
 * Primary-subtype single-select (TECH-1789).
 *
 * Restricted to current memberships (DEC-A11). Renders a `<select>` populated
 * from `memberships`, with a "(none)" option. Disabled when `memberships` is
 * empty — UI nudge that primary requires membership first.
 *
 * @see ia/projects/asset-pipeline/stage-7.1.md — TECH-1789 §Plan Digest
 */

export type PrimarySubtypeSelectProps = {
  memberships: EntityRefRow[];
  value: string | null;
  onChange: (entity_id: string | null) => void;
  disabled?: boolean;
};

export default function PrimarySubtypeSelect({
  memberships,
  value,
  onChange,
  disabled,
}: PrimarySubtypeSelectProps) {
  const empty = memberships.length === 0;
  return (
    <label data-testid="primary-subtype-select-wrap" className="flex flex-col gap-[var(--ds-spacing-xs)]">
      <span className="text-[var(--ds-text-muted)]">Primary subtype</span>
      <select
        data-testid="primary-subtype-select"
        value={value ?? ""}
        disabled={disabled || empty}
        onChange={(e) => {
          const next = e.currentTarget.value;
          onChange(next === "" ? null : next);
        }}
      >
        <option value="">(none)</option>
        {memberships.map((m) => (
          <option key={m.entity_id} value={m.entity_id} data-testid={`primary-subtype-option-${m.slug}`}>
            {m.display_name} ({m.slug})
          </option>
        ))}
      </select>
      {empty ? (
        <span data-testid="primary-subtype-empty-hint" className="text-[var(--ds-text-muted)]">
          Add a subtype membership first to select a primary.
        </span>
      ) : null}
    </label>
  );
}
