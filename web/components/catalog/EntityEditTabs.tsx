"use client";

import { useCallback, useId, useRef } from "react";
import type { KeyboardEvent, ReactNode } from "react";

/**
 * Shared catalog detail tabs (DEC-A34, TECH-1615).
 *
 * Five-tab strip in fixed order: Edit, Versions, References, Lints, Audit.
 * Controlled component — parent owns `activeTab` + `onTabChange` so per-kind
 * pages can round-trip the active tab through `?tab={key}` URL search params
 * for deep linking.
 *
 * Keyboard navigation follows the WAI-ARIA tabs pattern: ArrowLeft /
 * ArrowRight wrap focus across triggers, Home / End jump to first / last.
 *
 * @see ia/projects/asset-pipeline/stage-5.1.md — Authoring console scaffolding
 */

export type TabKey = "edit" | "versions" | "references" | "lints" | "audit";

export type EntityEditTabsProps = {
  tabs: Record<TabKey, ReactNode>;
  activeTab: TabKey;
  onTabChange: (key: TabKey) => void;
};

const TAB_ORDER: ReadonlyArray<TabKey> = [
  "edit",
  "versions",
  "references",
  "lints",
  "audit",
];

const TAB_LABELS: Record<TabKey, string> = {
  edit: "Edit",
  versions: "Versions",
  references: "References",
  lints: "Lints",
  audit: "Audit",
};

export default function EntityEditTabs({
  tabs,
  activeTab,
  onTabChange,
}: EntityEditTabsProps) {
  const baseId = useId();
  const triggerRefs = useRef<Record<TabKey, HTMLButtonElement | null>>({
    edit: null,
    versions: null,
    references: null,
    lints: null,
    audit: null,
  });

  const focusTab = useCallback((key: TabKey) => {
    triggerRefs.current[key]?.focus();
  }, []);

  const handleKeyDown = useCallback(
    (event: KeyboardEvent<HTMLButtonElement>, current: TabKey) => {
      const idx = TAB_ORDER.indexOf(current);
      if (idx === -1) return;
      let nextIdx: number | null = null;
      if (event.key === "ArrowRight") nextIdx = (idx + 1) % TAB_ORDER.length;
      else if (event.key === "ArrowLeft") nextIdx = (idx - 1 + TAB_ORDER.length) % TAB_ORDER.length;
      else if (event.key === "Home") nextIdx = 0;
      else if (event.key === "End") nextIdx = TAB_ORDER.length - 1;
      if (nextIdx === null) return;
      event.preventDefault();
      const nextKey = TAB_ORDER[nextIdx]!;
      onTabChange(nextKey);
      focusTab(nextKey);
    },
    [focusTab, onTabChange],
  );

  return (
    <div data-testid="entity-edit-tabs" style={{ display: "flex", flexDirection: "column", gap: "var(--ds-spacing-md)" }}>
      <div
        role="tablist"
        aria-label="Detail sections"
        style={{
          display: "flex",
          gap: "var(--ds-spacing-sm)",
          borderBottom: "1px solid var(--ds-border-subtle)",
        }}
      >
        {TAB_ORDER.map((key) => {
          const isActive = key === activeTab;
          return (
            <button
              key={key}
              type="button"
              role="tab"
              id={`${baseId}-tab-${key}`}
              aria-controls={`${baseId}-panel-${key}`}
              aria-selected={isActive}
              tabIndex={isActive ? 0 : -1}
              data-testid={`entity-edit-tab-${key}`}
              ref={(el) => {
                triggerRefs.current[key] = el;
              }}
              onClick={() => onTabChange(key)}
              onKeyDown={(event) => handleKeyDown(event, key)}
              style={{
                background: "transparent",
                border: 0,
                color: isActive ? "var(--ds-text-primary)" : "var(--ds-text-muted)",
                cursor: "pointer",
                padding: "var(--ds-spacing-xs) var(--ds-spacing-md)",
                borderBottom: isActive
                  ? "2px solid var(--ds-text-accent-info)"
                  : "2px solid transparent",
                fontWeight: isActive ? 600 : 400,
              }}
            >
              {TAB_LABELS[key]}
            </button>
          );
        })}
      </div>
      {TAB_ORDER.map((key) => {
        const isActive = key === activeTab;
        return (
          <div
            key={key}
            role="tabpanel"
            id={`${baseId}-panel-${key}`}
            aria-labelledby={`${baseId}-tab-${key}`}
            aria-hidden={!isActive}
            hidden={!isActive}
            data-testid={`entity-edit-panel-${key}`}
          >
            {tabs[key]}
          </div>
        );
      })}
    </div>
  );
}
