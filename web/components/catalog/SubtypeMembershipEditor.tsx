"use client";

import { useState } from "react";

import EntityRefPicker, { type EntityRefRow } from "@/components/catalog/EntityRefPicker";

/**
 * Subtype membership multi-select editor (TECH-1789).
 *
 * Renders existing pool memberships as chips with remove buttons + an
 * "Add subtype" picker that filters `kind=pool`. Mutations are diff-style:
 * parent owns `value` (current memberships) + receives `onChange(next)`.
 *
 * @see ia/projects/asset-pipeline/stage-7.1.md — TECH-1789 §Plan Digest
 */

export type SubtypeMembershipEditorProps = {
  /** Current memberships (resolved entity rows). */
  value: EntityRefRow[];
  /** Called with the full next list when add/remove fires. */
  onChange: (next: EntityRefRow[]) => void;
  /** Optional id of the pool currently bound as primary subtype — disables remove. */
  primaryPoolId?: string | null;
  disabled?: boolean;
};

export default function SubtypeMembershipEditor({
  value,
  onChange,
  primaryPoolId,
  disabled,
}: SubtypeMembershipEditorProps) {
  const [pickerOpen, setPickerOpen] = useState<boolean>(false);

  function add(row: EntityRefRow | null) {
    if (!row) return;
    if (value.some((r) => r.entity_id === row.entity_id)) return;
    onChange([...value, row]);
    setPickerOpen(false);
  }

  function remove(entity_id: string) {
    if (entity_id === primaryPoolId) return;
    onChange(value.filter((r) => r.entity_id !== entity_id));
  }

  return (
    <div data-testid="subtype-membership-editor" className="flex flex-col gap-[var(--ds-spacing-xs)]">
      <span data-testid="subtype-membership-editor-label" className="text-[var(--ds-text-muted)]">
        Subtype memberships
      </span>
      <ul data-testid="subtype-membership-list" className="flex flex-wrap gap-[var(--ds-spacing-xs)]">
        {value.length === 0 ? (
          <li data-testid="subtype-membership-empty" className="text-[var(--ds-text-muted)]">
            No memberships
          </li>
        ) : (
          value.map((row) => {
            const isPrimary = primaryPoolId != null && row.entity_id === primaryPoolId;
            return (
              <li
                key={row.entity_id}
                data-testid={`subtype-membership-chip-${row.slug}`}
                className="flex items-center gap-[var(--ds-spacing-xs)] rounded border border-[var(--ds-border-subtle)] bg-[var(--ds-bg-panel)] px-[var(--ds-spacing-sm)] py-[var(--ds-spacing-xs)]"
              >
                <span className="text-[var(--ds-text-primary)]">{row.display_name}</span>
                <span className="font-mono text-[var(--ds-text-muted)]">{row.slug}</span>
                {isPrimary ? (
                  <span data-testid={`subtype-membership-chip-primary-${row.slug}`} className="text-[var(--ds-text-accent-info)]">
                    primary
                  </span>
                ) : null}
                {!disabled && !isPrimary ? (
                  <button
                    type="button"
                    data-testid={`subtype-membership-remove-${row.slug}`}
                    onClick={() => remove(row.entity_id)}
                    className="text-[var(--ds-text-muted)]"
                  >
                    Remove
                  </button>
                ) : null}
              </li>
            );
          })
        )}
      </ul>
      {!disabled ? (
        pickerOpen ? (
          <EntityRefPicker
            accepts_kind={["pool"]}
            value={null}
            onChange={(_id, row) => add(row)}
            label="Add subtype"
            testId="subtype-membership-picker"
          />
        ) : (
          <button
            type="button"
            data-testid="subtype-membership-add"
            onClick={() => setPickerOpen(true)}
            className="self-start text-[var(--ds-text-accent-info)]"
          >
            + Add subtype
          </button>
        )
      ) : null}
    </div>
  );
}
