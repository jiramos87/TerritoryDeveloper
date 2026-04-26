import type { ReactNode } from "react";

/**
 * Shared catalog list+detail layout (DEC-A34, TECH-1615).
 *
 * Kind-agnostic shell: left list panel (fixed-width 320px) + right detail
 * panel (flex). Per-kind pages compose this layout by passing pre-rendered
 * `listSlot` (e.g. virtualized roster of sprites/panels/pools) + `detailSlot`
 * (e.g. `<EntityEditTabs>` with edit form, audit log, etc.) without
 * re-implementing chrome.
 *
 * @see ia/projects/asset-pipeline/stage-5.1.md — Authoring console scaffolding
 */

export type EntityListDetailLayoutProps = {
  /** Pre-rendered list panel content (e.g. virtualized list of entity rows). */
  listSlot: ReactNode;
  /** Pre-rendered detail panel content (e.g. `<EntityEditTabs>` instance). */
  detailSlot: ReactNode;
  /**
   * Optional active selection id — passed through to inform parent-rendered
   * list slots, but layout itself remains list-rendering-agnostic.
   */
  selectedId?: string;
};

export default function EntityListDetailLayout({
  listSlot,
  detailSlot,
  selectedId,
}: EntityListDetailLayoutProps) {
  return (
    <div
      data-testid="entity-list-detail-layout"
      data-selected-id={selectedId ?? ""}
      style={{
        display: "flex",
        height: "100%",
        width: "100%",
        background: "var(--ds-bg-canvas)",
        color: "var(--ds-text-primary)",
      }}
    >
      <aside
        data-testid="entity-list-pane"
        aria-label="List"
        style={{
          width: "320px",
          flex: "0 0 320px",
          borderRight: "1px solid var(--ds-border-subtle)",
          overflowY: "auto",
          background: "var(--ds-bg-panel)",
        }}
      >
        {listSlot}
      </aside>
      <section
        data-testid="entity-detail-pane"
        aria-label="Detail"
        style={{
          flex: "1 1 auto",
          minWidth: 0,
          overflowY: "auto",
          padding: "var(--ds-spacing-lg)",
        }}
      >
        {detailSlot}
      </section>
    </div>
  );
}
