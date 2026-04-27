"use client";

/**
 * Structural-fidelity button preview (TECH-2094 / Stage 10.1).
 *
 * Renders 6 nested sprite slot DOM nodes (idle/hover/pressed/disabled/icon/badge)
 * matching the Unity panel hierarchy. Only the `idle` slot is visible by default;
 * `hover|pressed|disabled` reachable via `:hover`, `:active`, `[aria-disabled=true]`
 * pseudo / ARIA selectors so no React state is needed (mirrors the discrete-state
 * Unity shader). Per DEC-A44, token values arrive via CSS custom properties so
 * editor mutations ripple within one render cycle.
 *
 * @see ia/projects/asset-pipeline/stage-10.1 — TECH-2094 §Plan Digest
 */

export type ButtonPreviewSpriteSlot = {
  slug: string | null;
  url: string | null;
};

export type ButtonPreviewProps = {
  label?: string;
  /** Six sprite slots; missing slots render as empty nodes (slot still present in DOM). */
  spriteSlots?: {
    idle?: ButtonPreviewSpriteSlot;
    hover?: ButtonPreviewSpriteSlot;
    pressed?: ButtonPreviewSpriteSlot;
    disabled?: ButtonPreviewSpriteSlot;
    icon?: ButtonPreviewSpriteSlot;
    badge?: ButtonPreviewSpriteSlot;
  };
  /** When true, renders disabled state (`[aria-disabled=true]`). */
  disabled?: boolean;
};

const SLOT_KEYS: ReadonlyArray<keyof Required<ButtonPreviewProps>["spriteSlots"]> = [
  "idle",
  "hover",
  "pressed",
  "disabled",
  "icon",
  "badge",
];

export default function ButtonPreview({
  label = "Button",
  spriteSlots,
  disabled,
}: ButtonPreviewProps) {
  return (
    <div
      data-testid="button-preview"
      className="button-preview"
      aria-disabled={disabled ? "true" : "false"}
      role="button"
      tabIndex={0}
    >
      {SLOT_KEYS.map((slot) => {
        const data = spriteSlots?.[slot];
        const slotClass = `button-preview-slot button-preview-slot--${slot}`;
        return (
          <span
            key={slot}
            data-testid={`button-preview-slot-${slot}`}
            data-slot={slot}
            data-slug={data?.slug ?? ""}
            className={slotClass}
            style={
              data?.url
                ? { backgroundImage: `url("${data.url}")`, backgroundSize: "cover" }
                : undefined
            }
          />
        );
      })}
      <span data-testid="button-preview-label" className="button-preview-label">
        {label}
      </span>
    </div>
  );
}
