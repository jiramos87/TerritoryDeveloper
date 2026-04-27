"use client";

/**
 * SaveBindPlaceholder (TECH-1674) — disabled-state stub for the Stage 7.x
 * asset-slot binding flow. Surfaces a "coming-in-Stage-7.x" banner with a
 * dismiss button so the host can re-enable the rest of the variant grid.
 *
 * Replace with a true slot picker once Stage 7.x ships asset slots.
 */

export type SaveBindPlaceholderProps = {
  variantIdx: number;
  onDismiss: () => void;
};

export default function SaveBindPlaceholder(props: SaveBindPlaceholderProps) {
  const { variantIdx, onDismiss } = props;
  return (
    <div
      data-testid="save-bind-placeholder"
      role="status"
      style={{
        marginTop: "var(--ds-spacing-sm)",
        padding: "var(--ds-spacing-sm)",
        border: "1px dashed var(--ds-border-subtle)",
        background: "var(--ds-bg-canvas)",
      }}
    >
      <p style={{ margin: 0, fontSize: "var(--ds-font-size-body-sm)", color: "var(--ds-text-muted)" }}>
        Save + Bind for variant v{variantIdx} is coming in Stage 7.x once asset
        slot binding ships. The action stays disabled here until then.
      </p>
      <button
        type="button"
        data-testid="save-bind-placeholder-dismiss"
        onClick={onDismiss}
        style={{ marginTop: "var(--ds-spacing-xs)" }}
      >
        Dismiss
      </button>
    </div>
  );
}
