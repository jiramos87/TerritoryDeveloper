"use client";

/**
 * Structural-fidelity panel preview (TECH-2094 / Stage 10.1).
 *
 * Renders one DOM column per archetype-declared slot (Stage 8.1 panel shape)
 * with `panel_child` rows in `order_idx` order; recursive descent for nested
 * panel children. Depth cap = 6 (matches Stage 8.1 cycle-check budget +
 * TECH-2095 semantic resolution depth — prevents render-loop on a corrupt
 * fixture).
 *
 * Pure DOM + CSS — no Unity runtime imports, no canvas-3D deps.
 *
 * @see ia/projects/asset-pipeline/stage-10.1 — TECH-2094 §Plan Digest
 */

import { Fragment } from "react";

export type PanelPreviewChild = {
  slot: string;
  order_idx: number;
  display_name: string;
  /** Nested children when this child is itself a panel. Optional + bounded by depth cap. */
  nested?: PanelPreviewChild[];
};

export type PanelPreviewSlotDef = {
  slot: string;
  label?: string;
};

export type PanelPreviewProps = {
  display_name?: string;
  slots: PanelPreviewSlotDef[];
  panelChildren: PanelPreviewChild[];
  /** Internal recursion guard. Top-level callers leave this unset. */
  _depth?: number;
};

const DEPTH_CAP = 6;

export default function PanelPreview({
  display_name = "Panel",
  slots,
  panelChildren,
  _depth = 0,
}: PanelPreviewProps) {
  if (_depth >= DEPTH_CAP) {
    return (
      <div
        data-testid="panel-preview-recursion-cap"
        className="panel-preview-recursion-cap"
      >
        Recursion cap reached ({DEPTH_CAP}).
      </div>
    );
  }

  // Group children by slot, preserve order_idx.
  const childrenBySlot = new Map<string, PanelPreviewChild[]>();
  for (const c of panelChildren) {
    const arr = childrenBySlot.get(c.slot) ?? [];
    arr.push(c);
    childrenBySlot.set(c.slot, arr);
  }
  for (const arr of childrenBySlot.values()) {
    arr.sort((a, b) => a.order_idx - b.order_idx);
  }

  return (
    <div
      data-testid="panel-preview"
      data-depth={_depth}
      data-display-name={display_name}
      className="panel-preview"
    >
      <div className="panel-preview-slot-row">
        {slots.map((slotDef) => {
          const rows = childrenBySlot.get(slotDef.slot) ?? [];
          return (
            <div
              key={slotDef.slot}
              data-testid={`panel-preview-slot-${slotDef.slot}`}
              data-slot={slotDef.slot}
              className="panel-preview-slot-column"
            >
              <span className="panel-preview-slot-label">
                {slotDef.label ?? slotDef.slot}
              </span>
              {rows.map((row, idx) => (
                <Fragment key={`${slotDef.slot}-${row.order_idx}-${idx}`}>
                  {row.nested && row.nested.length > 0 ? (
                    <PanelPreview
                      display_name={row.display_name}
                      slots={slots}
                      panelChildren={row.nested}
                      _depth={_depth + 1}
                    />
                  ) : (
                    <span
                      data-testid={`panel-preview-child-${slotDef.slot}-${row.order_idx}`}
                      className="panel-preview-child"
                    >
                      {row.display_name}
                    </span>
                  )}
                </Fragment>
              ))}
            </div>
          );
        })}
      </div>
    </div>
  );
}
