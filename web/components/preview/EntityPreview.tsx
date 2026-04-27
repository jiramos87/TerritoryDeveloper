"use client";

/**
 * Entity preview dispatcher (TECH-2094 / Stage 10.1).
 *
 * Mounted above `<EntityEditTabs>` on `assets/[slug]`, `buttons/[slug]`,
 * `panels/[slug]` detail pages. Dispatches on `kind` to the structural
 * preview component, injects token CSS custom properties at the preview root
 * so editor token-edits ripple via `var(--token-*)` per DEC-A44.
 *
 * No Unity runtime imports anywhere under `web/components/preview/**`.
 *
 * @see ia/projects/asset-pipeline/stage-10.1 — TECH-2094 §Plan Digest
 */

import "./preview.css";

import ButtonPreview, {
  type ButtonPreviewProps,
} from "./ButtonPreview";
import PanelPreview, { type PanelPreviewProps } from "./PanelPreview";

export type EntityPreviewKind = "button" | "panel";

export type EntityPreviewTokenMap = {
  /** Mapping from CSS custom-property name (without `--` prefix) to value. */
  [cssVar: string]: string;
};

export type EntityPreviewProps =
  | ({
      kind: "button";
      tokens?: EntityPreviewTokenMap;
    } & ButtonPreviewProps)
  | ({
      kind: "panel";
      tokens?: EntityPreviewTokenMap;
    } & PanelPreviewProps);

export default function EntityPreview(props: EntityPreviewProps) {
  const { kind, tokens } = props;
  const styleVars = tokensToStyleVars(tokens);
  return (
    <section
      data-testid="entity-preview"
      data-kind={kind}
      className="entity-preview-root"
      style={styleVars}
    >
      <header data-testid="entity-preview-header" className="entity-preview-header">
        Preview
      </header>
      {kind === "button" ? (
        <ButtonPreview
          label={(props as { label?: string }).label}
          spriteSlots={(props as ButtonPreviewProps).spriteSlots}
          disabled={(props as ButtonPreviewProps).disabled}
        />
      ) : (
        <PanelPreview
          display_name={(props as PanelPreviewProps).display_name}
          slots={(props as PanelPreviewProps).slots}
          panelChildren={(props as PanelPreviewProps).panelChildren}
        />
      )}
    </section>
  );
}

function tokensToStyleVars(
  tokens: EntityPreviewTokenMap | undefined,
): React.CSSProperties | undefined {
  if (!tokens) return undefined;
  const out: Record<string, string> = {};
  for (const [k, v] of Object.entries(tokens)) {
    const name = k.startsWith("--") ? k : `--${k}`;
    out[name] = v;
  }
  return out as React.CSSProperties;
}
