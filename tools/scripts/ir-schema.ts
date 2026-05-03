#!/usr/bin/env npx tsx
/**
 * IR JSON schema — TS types + slot accept-rule guard for Game UI Design System Stage 1+.
 *
 * Locked by `docs/game-ui-mvp-authoring-approach-exploration.md` §Phase 3 (IR JSON shape) +
 * §Phase 7 (slot accept-rule violation example). Reused by:
 *
 * - `tools/scripts/transcribe-cd-game-ui.ts` (Stage 1 — produces typed IR JSON).
 * - `Assets/Editor/Bridge/UiBakeHandler.cs` (Stage 2+ — consumes IR JSON; mirrors guard semantics).
 *
 * @packageDocumentation
 */

/** Token block — matches §Phase 3 locked grammar. Five subblocks. */
export interface IrTokens {
  palette: IrTokenPalette[];
  frame_style: IrTokenFrameStyle[];
  font_face: IrTokenFontFace[];
  motion_curve: IrTokenMotionCurve[];
  illumination: IrTokenIllumination[];
}

export interface IrTokenPalette {
  /** Slug must be unique within `palette[]`. */
  slug: string;
  /** Ramp = ordered hex stops (low → high). */
  ramp: string[];
}

export interface IrTokenFrameStyle {
  slug: string;
  /** `single` | `double` (CD partner extends as needed). */
  edge: string;
  innerShadowAlpha: number;
}

export interface IrTokenFontFace {
  slug: string;
  family: string;
  weight: number;
}

export interface IrTokenMotionCurve {
  slug: string;
  /** `spring` | `cubic-bezier` | other (CD partner extends as needed). */
  kind: string;
  /** Optional spring params (kind=spring). */
  stiffness?: number;
  damping?: number;
  /** Optional bezier params (kind=cubic-bezier). */
  c1?: number[];
  c2?: number[];
  /** Optional duration (any kind). */
  durationMs?: number;
}

export interface IrTokenIllumination {
  slug: string;
  color: string;
  haloRadiusPx: number;
}

/** Panel block — matches §Phase 3 locked grammar. Stage 12 Step 11 — adds `kind`. Stage 13.1 — adds optional `tabs?[]` + `rows?[]` for IR v2 (DEC-A21 Path C full-fidelity). */
export interface IrPanel {
  slug: string;
  /** ThemedPanel archetype identifier (Stage 3+ runtime composer). */
  archetype: string;
  /** Layout kind — drives runtime anchor + LayoutGroup choice in `ThemedPanel.OnEnable`. */
  kind?: PanelKind;
  slots: IrPanelSlot[];
  /**
   * IR v2 — optional tab taxonomy (Stage 13.1). Populated only when `panels.jsx` source
   * declares tabs (e.g. `role="tab"` children or `<Tab>` JSX nodes). Omitted on v1 panels.
   */
  tabs?: IrTab[];
  /**
   * IR v2 — optional row layout list (Stage 13.1). Populated only when `panels.jsx` source
   * declares row containers. Omitted on v1 panels.
   */
  rows?: IrRow[];
}

/**
 * IR v2 tab descriptor — one entry per tab in a tabbed panel (Stage 13.1).
 * Locked by D2 (tab taxonomy from `panels.jsx` `role="tab"` / class hint).
 */
export interface IrTab {
  /** Stable tab id (slug-cased). */
  id: string;
  /** Visible tab label. */
  label: string;
  /** Whether this tab is active by default. Optional — default false. */
  active?: boolean;
}

/**
 * IR v2 row descriptor — one entry per row in a row-layout panel (Stage 13.1).
 * Flat list (D3 — no nested groups in v2). `kind` discriminates render shape.
 */
export interface IrRow {
  /** Render shape — `stat` (label + numeric value), `detail` (key/value pair), `header` (section title). */
  kind: 'stat' | 'detail' | 'header';
  /** Optional row label. */
  label?: string;
  /** Optional row value (typically formatted string). */
  value?: string;
  /** Optional segmented-readout segment count (when kind=stat with a SegmentedReadout). */
  segments?: number;
  /** Optional font face slug (matches `tokens.font_face[].slug`). */
  fontSlug?: string;
}

/**
 * Layout-kind taxonomy — Stage 12 Step 11.
 *
 * - `modal`   → centered 600×800 modal; VerticalLayoutGroup; SetAsLastSibling on enable.
 * - `screen`  → full-stretch (anchorMin (0,0) → anchorMax (1,1)); no LayoutGroup; children freely positioned.
 * - `hud`     → top-anchored full-width strip; HorizontalLayoutGroup.
 * - `toolbar` → left-anchored vertical strip; VerticalLayoutGroup (or designer override).
 *
 * Default when omitted: `modal` (preserves Stage 8 panel set behavior post-bake).
 */
export type PanelKind = 'modal' | 'screen' | 'hud' | 'toolbar';

export const PANEL_KINDS: readonly PanelKind[] = ['modal', 'screen', 'hud', 'toolbar'] as const;

export interface IrPanelSlot {
  /** Slot name unique within owning panel. */
  name: string;
  /** Allowed `interactives[].slug` values. */
  accepts: string[];
  /** Bound `interactives[].slug` values. Each must appear in `accepts`. */
  children: string[];
  /**
   * Optional per-child label content — parallel to `children[]`. When present, length
   * must equal `children.length`; empty string skips label injection for that index.
   * Stage 12 Step 12 — themed-button caption + themed-label text source.
   */
  labels?: string[];
  /**
   * Optional per-child icon sprite slug — parallel to `children[]`. When present, length
   * must equal `children.length`; empty/null skips icon injection for that index.
   * Stage 12 Step 16.D — illuminated-button icon sprite slug; bake handler resolves to
   * `Assets/Sprites/Buttons/{slug}-target.png` (with fallback to `Assets/Sprites/{slug}-target.png`).
   */
  iconSpriteSlugs?: string[];
}

/** Interactive block — matches §Phase 3 locked grammar. StudioControl ring. */
export interface IrInteractive {
  slug: string;
  kind: StudioControlKind;
  /** Per-archetype detail object. Shape varies by `kind`. */
  detail: IrInteractiveDetail;
}

/** StudioControl archetype enum — locked by §Phase 3 + Phase 6 Stage 4. Extended Stage 7 with `themed-overlay-toggle-row`. Extended Stage 8 with Themed* modal primitive kinds. Extended Stage 1.1 (ui-visual-fidelity-layer) with `section_header`, `divider`, `badge`, `panel`, `button`. Extended Stage 9 (game-ui-design-system) with `themed-tooltip`. */
export type StudioControlKind =
  | 'knob'
  | 'fader'
  | 'vu-meter'
  | 'oscilloscope'
  | 'illuminated-button'
  | 'segmented-readout'
  | 'detent-ring'
  | 'led'
  | 'themed-overlay-toggle-row'
  | 'themed-button'
  | 'themed-label'
  | 'themed-slider'
  | 'themed-toggle'
  | 'themed-tab-bar'
  | 'themed-list'
  | 'themed-tooltip'
  | 'section_header'
  | 'divider'
  | 'badge'
  | 'panel'
  | 'button';

export const STUDIO_CONTROL_KINDS: readonly StudioControlKind[] = [
  'knob',
  'fader',
  'vu-meter',
  'oscilloscope',
  'illuminated-button',
  'segmented-readout',
  'detent-ring',
  'led',
  'themed-overlay-toggle-row',
  'themed-button',
  'themed-label',
  'themed-slider',
  'themed-toggle',
  'themed-tab-bar',
  'themed-list',
  'themed-tooltip',
  'section_header',
  'divider',
  'badge',
  'panel',
  'button',
] as const;

/** Detail row shapes — open-ended union per archetype; transcribe + bridge handler refine. */
export type IrInteractiveDetail = Record<string, unknown>;

/** Top-level IR JSON shape. Single output of `transcribe:cd-game-ui`. */
export interface Ir {
  tokens: IrTokens;
  panels: IrPanel[];
  interactives: IrInteractive[];
  /**
   * IR schema discriminator (Stage 13.1 — DEC-A21 Path C). When omitted, treat as v1
   * (legacy CD bundles + tests). v2 panels carry optional `tabs[]` + `rows[]`.
   */
  schemaVersion?: 1 | 2;
}

/**
 * Stage 13.1 — `IrRoot` alias for `Ir` (top-level IR JSON shape). Plan-digest naming alias
 * — keeps existing `Ir` references compiling while exposing `IrRoot` symbol per Path C spec.
 */
export type IrRoot = Ir;

/**
 * Stage 13.1 — Lift a v1 IR root into v2 by stamping `schemaVersion: 2`. Shallow clone;
 * panels untouched (per acceptance — no in-place mutation, no panel rewrite). Use when
 * legacy v1 fixtures must be carried into v2-aware downstream consumers.
 */
export function liftV1ToV2(root: IrRoot): IrRoot {
  return { ...root, schemaVersion: 2 };
}

// -- Slot accept-rule guard ---------------------------------------------------

export interface SlotAcceptOk {
  ok: true;
}

export interface SlotAcceptViolation {
  ok: false;
  error: 'slot_accept_violation';
  panel: string;
  slot: string;
  offending_children: string[];
  accepts: string[];
}

export type SlotAcceptResult = SlotAcceptOk | SlotAcceptViolation;

/**
 * Walk every panel slot; return first violation found OR `{ ok: true }`.
 *
 * Mirrors §Phase 7 edge-case rejection example shape so downstream parity is preserved
 * across transcribe (Node) and bridge handler (Unity C#).
 */
export function validateSlotAccept(ir: Ir): SlotAcceptResult {
  for (const panel of ir.panels) {
    for (const slot of panel.slots) {
      const accepts = new Set(slot.accepts);
      const offending = slot.children.filter((c) => !accepts.has(c));
      if (offending.length > 0) {
        return {
          ok: false,
          error: 'slot_accept_violation',
          panel: panel.slug,
          slot: slot.name,
          offending_children: offending,
          accepts: slot.accepts,
        };
      }
    }
  }
  return { ok: true };
}

// -- Top-level schema validation ---------------------------------------------

export interface SchemaError {
  ok: false;
  error: string;
  path: string;
  detail: string;
}

export interface SchemaOk {
  ok: true;
}

export type SchemaResult = SchemaOk | SchemaError;

function isObject(v: unknown): v is Record<string, unknown> {
  return typeof v === 'object' && v !== null && !Array.isArray(v);
}

function isStringArray(v: unknown): v is string[] {
  return Array.isArray(v) && v.every((x) => typeof x === 'string');
}

/**
 * Strict structural validation — rejects malformed top-level shape, missing required keys,
 * and bad types. Halts transcribe before slot accept-rule guard.
 */
export function validateIrShape(raw: unknown): SchemaResult {
  if (!isObject(raw)) {
    return { ok: false, error: 'ir_shape_invalid', path: '$', detail: 'top-level must be object' };
  }
  if (!isObject(raw.tokens)) {
    return { ok: false, error: 'ir_shape_invalid', path: '$.tokens', detail: 'tokens must be object' };
  }
  const tokens = raw.tokens as Record<string, unknown>;
  for (const k of ['palette', 'frame_style', 'font_face', 'motion_curve', 'illumination']) {
    if (!Array.isArray(tokens[k])) {
      return {
        ok: false,
        error: 'ir_shape_invalid',
        path: `$.tokens.${k}`,
        detail: `tokens.${k} must be array`,
      };
    }
  }
  if (!Array.isArray(raw.panels)) {
    return { ok: false, error: 'ir_shape_invalid', path: '$.panels', detail: 'panels must be array' };
  }
  if (!Array.isArray(raw.interactives)) {
    return {
      ok: false,
      error: 'ir_shape_invalid',
      path: '$.interactives',
      detail: 'interactives must be array',
    };
  }
  // Per-panel + per-slot shape.
  const panelKinds = new Set<string>(PANEL_KINDS);
  for (let i = 0; i < raw.panels.length; i++) {
    const p = raw.panels[i];
    if (!isObject(p) || typeof p.slug !== 'string' || typeof p.archetype !== 'string') {
      return {
        ok: false,
        error: 'ir_shape_invalid',
        path: `$.panels[${i}]`,
        detail: 'panel missing slug/archetype',
      };
    }
    if (p.kind !== undefined && (typeof p.kind !== 'string' || !panelKinds.has(p.kind))) {
      return {
        ok: false,
        error: 'ir_shape_invalid',
        path: `$.panels[${i}].kind`,
        detail: `panel.kind '${String(p.kind)}' not in PanelKind enum (${[...panelKinds].join(', ')})`,
      };
    }
    if (!Array.isArray(p.slots)) {
      return {
        ok: false,
        error: 'ir_shape_invalid',
        path: `$.panels[${i}].slots`,
        detail: 'panel.slots must be array',
      };
    }
    for (let j = 0; j < p.slots.length; j++) {
      const s = p.slots[j];
      if (
        !isObject(s) ||
        typeof s.name !== 'string' ||
        !isStringArray(s.accepts) ||
        !isStringArray(s.children)
      ) {
        return {
          ok: false,
          error: 'ir_shape_invalid',
          path: `$.panels[${i}].slots[${j}]`,
          detail: 'slot needs name/accepts[]/children[]',
        };
      }
      if (s.labels !== undefined) {
        if (!isStringArray(s.labels) || (s.labels as string[]).length !== (s.children as string[]).length) {
          return {
            ok: false,
            error: 'ir_shape_invalid',
            path: `$.panels[${i}].slots[${j}].labels`,
            detail: 'labels must be string[] with same length as children[]',
          };
        }
      }
    }
  }
  // Per-interactive shape + archetype enum.
  const kinds = new Set<string>(STUDIO_CONTROL_KINDS);
  for (let i = 0; i < raw.interactives.length; i++) {
    const it = raw.interactives[i];
    if (
      !isObject(it) ||
      typeof it.slug !== 'string' ||
      typeof it.kind !== 'string' ||
      !isObject(it.detail)
    ) {
      return {
        ok: false,
        error: 'ir_shape_invalid',
        path: `$.interactives[${i}]`,
        detail: 'interactive needs slug/kind/detail',
      };
    }
    if (!kinds.has(it.kind)) {
      return {
        ok: false,
        error: 'ir_shape_invalid',
        path: `$.interactives[${i}].kind`,
        detail: `kind '${it.kind}' not in StudioControl archetype enum (${[...kinds].join(', ')})`,
      };
    }
    // Per-kind detail validation.
    if (it.kind === 'panel') {
      const d = it.detail;
      if (typeof d.paddingX !== 'number' || typeof d.paddingY !== 'number' || typeof d.gap !== 'number') {
        return {
          ok: false,
          error: 'ir_shape_invalid',
          path: `$.interactives[${i}].detail`,
          detail: 'panel detail requires paddingX:number, paddingY:number, gap:number',
        };
      }
      if (d.dividerThickness !== undefined && typeof d.dividerThickness !== 'number') {
        return {
          ok: false,
          error: 'ir_shape_invalid',
          path: `$.interactives[${i}].detail.dividerThickness`,
          detail: 'panel detail.dividerThickness must be number when present',
        };
      }
    }
    if (it.kind === 'button') {
      const d = it.detail;
      if (typeof d.paddingX !== 'number' || typeof d.paddingY !== 'number') {
        return {
          ok: false,
          error: 'ir_shape_invalid',
          path: `$.interactives[${i}].detail`,
          detail: 'button detail requires paddingX:number, paddingY:number',
        };
      }
    }
  }
  return { ok: true };
}
