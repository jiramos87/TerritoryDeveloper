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

/** Panel block — matches §Phase 3 locked grammar. */
export interface IrPanel {
  slug: string;
  /** ThemedPanel archetype identifier (Stage 3+ runtime composer). */
  archetype: string;
  slots: IrPanelSlot[];
}

export interface IrPanelSlot {
  /** Slot name unique within owning panel. */
  name: string;
  /** Allowed `interactives[].slug` values. */
  accepts: string[];
  /** Bound `interactives[].slug` values. Each must appear in `accepts`. */
  children: string[];
}

/** Interactive block — matches §Phase 3 locked grammar. StudioControl ring. */
export interface IrInteractive {
  slug: string;
  kind: StudioControlKind;
  /** Per-archetype detail object. Shape varies by `kind`. */
  detail: IrInteractiveDetail;
}

/** StudioControl archetype enum — locked by §Phase 3 + Phase 6 Stage 4. */
export type StudioControlKind =
  | 'knob'
  | 'fader'
  | 'vu-meter'
  | 'oscilloscope'
  | 'illuminated-button'
  | 'segmented-readout'
  | 'detent-ring'
  | 'led';

export const STUDIO_CONTROL_KINDS: readonly StudioControlKind[] = [
  'knob',
  'fader',
  'vu-meter',
  'oscilloscope',
  'illuminated-button',
  'segmented-readout',
  'detent-ring',
  'led',
] as const;

/** Detail row shapes — open-ended union per archetype; transcribe + bridge handler refine. */
export type IrInteractiveDetail = Record<string, unknown>;

/** Top-level IR JSON shape. Single output of `transcribe:cd-game-ui`. */
export interface Ir {
  tokens: IrTokens;
  panels: IrPanel[];
  interactives: IrInteractive[];
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
  }
  return { ok: true };
}
