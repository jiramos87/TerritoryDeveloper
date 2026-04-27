"use client";

import { useMemo } from "react";

import type { CatalogTokenColorValue } from "@/types/api/catalog-api";

/**
 * Color token editor (TECH-2093 / Stage 10.1).
 *
 * Presentational — parent owns `value` + `onChange`. Renders a HEX input
 * (#RRGGBB) and three HSL number inputs (h 0-360, s 0-100, l 0-100). When the
 * caller mounts in HEX mode the user can flip to HSL via the mode toggle; flip
 * preserves color via {@link hexToHsl} / {@link hslToHex}.
 *
 * @see ia/projects/asset-pipeline/stage-10.1 — TECH-2093 §Plan Digest
 */

export type ColorTokenEditorProps = {
  value: CatalogTokenColorValue;
  onChange: (next: CatalogTokenColorValue) => void;
  disabled?: boolean;
};

const HEX_RE = /^#?[0-9a-fA-F]{6}$/;

export default function ColorTokenEditor({ value, onChange, disabled }: ColorTokenEditorProps) {
  const isHex = "hex" in value;
  const hex = useMemo(() => (isHex ? normalizeHex(value.hex) : hslToHex(value)), [isHex, value]);
  const hsl = useMemo(() => (isHex ? hexToHsl(value.hex) : { h: value.h, s: value.s, l: value.l }), [isHex, value]);

  function setHex(raw: string) {
    onChange({ hex: raw });
  }

  function setHsl(patch: Partial<{ h: number; s: number; l: number }>) {
    const next = { h: hsl.h, s: hsl.s, l: hsl.l, ...patch };
    onChange({ h: clampNum(next.h, 0, 360), s: clampNum(next.s, 0, 100), l: clampNum(next.l, 0, 100) });
  }

  function flipMode() {
    if (isHex) onChange({ h: hsl.h, s: hsl.s, l: hsl.l });
    else onChange({ hex });
  }

  return (
    <div data-testid="color-token-editor" className="flex flex-col gap-[var(--ds-spacing-sm)]">
      <div className="flex items-center gap-[var(--ds-spacing-md)]">
        <span data-testid="color-token-editor-mode" className="text-[var(--ds-text-muted)]">
          Mode: <strong>{isHex ? "HEX" : "HSL"}</strong>
        </span>
        <button
          type="button"
          data-testid="color-token-editor-flip"
          onClick={flipMode}
          disabled={disabled}
          className="text-[var(--ds-text-accent-info)]"
        >
          Switch to {isHex ? "HSL" : "HEX"}
        </button>
        <span
          data-testid="color-token-editor-swatch"
          aria-hidden
          style={{
            display: "inline-block",
            width: 24,
            height: 24,
            borderRadius: 4,
            border: "1px solid var(--ds-border-subtle)",
            background: HEX_RE.test(hex) ? normalizeHex(hex) : "transparent",
          }}
        />
      </div>

      {isHex ? (
        <label className="flex flex-col gap-[var(--ds-spacing-xs)]">
          <span className="text-[var(--ds-text-muted)]">HEX (#RRGGBB)</span>
          <input
            type="text"
            data-testid="color-token-editor-hex"
            value={value.hex}
            disabled={disabled}
            onChange={(e) => setHex(e.currentTarget.value)}
            className="rounded border border-[var(--ds-border-subtle)] bg-[var(--ds-bg-panel)] px-[var(--ds-spacing-sm)] py-[var(--ds-spacing-xs)] font-mono"
          />
        </label>
      ) : (
        <div className="grid grid-cols-3 gap-[var(--ds-spacing-sm)]">
          <label className="flex flex-col gap-[var(--ds-spacing-xs)]">
            <span className="text-[var(--ds-text-muted)]">H (0-360)</span>
            <input
              type="number"
              data-testid="color-token-editor-h"
              value={hsl.h}
              min={0}
              max={360}
              disabled={disabled}
              onChange={(e) => setHsl({ h: Number.parseFloat(e.currentTarget.value) })}
              className="rounded border border-[var(--ds-border-subtle)] bg-[var(--ds-bg-panel)] px-[var(--ds-spacing-sm)] py-[var(--ds-spacing-xs)]"
            />
          </label>
          <label className="flex flex-col gap-[var(--ds-spacing-xs)]">
            <span className="text-[var(--ds-text-muted)]">S (0-100)</span>
            <input
              type="number"
              data-testid="color-token-editor-s"
              value={hsl.s}
              min={0}
              max={100}
              disabled={disabled}
              onChange={(e) => setHsl({ s: Number.parseFloat(e.currentTarget.value) })}
              className="rounded border border-[var(--ds-border-subtle)] bg-[var(--ds-bg-panel)] px-[var(--ds-spacing-sm)] py-[var(--ds-spacing-xs)]"
            />
          </label>
          <label className="flex flex-col gap-[var(--ds-spacing-xs)]">
            <span className="text-[var(--ds-text-muted)]">L (0-100)</span>
            <input
              type="number"
              data-testid="color-token-editor-l"
              value={hsl.l}
              min={0}
              max={100}
              disabled={disabled}
              onChange={(e) => setHsl({ l: Number.parseFloat(e.currentTarget.value) })}
              className="rounded border border-[var(--ds-border-subtle)] bg-[var(--ds-bg-panel)] px-[var(--ds-spacing-sm)] py-[var(--ds-spacing-xs)]"
            />
          </label>
        </div>
      )}
    </div>
  );
}

function clampNum(v: number, lo: number, hi: number): number {
  if (Number.isNaN(v)) return lo;
  return Math.min(hi, Math.max(lo, v));
}

function normalizeHex(raw: string): string {
  if (!HEX_RE.test(raw)) return raw;
  return raw.startsWith("#") ? raw.toUpperCase() : `#${raw.toUpperCase()}`;
}

/** Pure helper exported for unit tests. Returns `{h,s,l}` rounded to integers. */
export function hexToHsl(rawHex: string): { h: number; s: number; l: number } {
  const m = HEX_RE.exec(rawHex);
  if (!m) return { h: 0, s: 0, l: 0 };
  const hex = rawHex.startsWith("#") ? rawHex.slice(1) : rawHex;
  const r = Number.parseInt(hex.slice(0, 2), 16) / 255;
  const g = Number.parseInt(hex.slice(2, 4), 16) / 255;
  const b = Number.parseInt(hex.slice(4, 6), 16) / 255;
  const max = Math.max(r, g, b);
  const min = Math.min(r, g, b);
  const l = (max + min) / 2;
  let h = 0;
  let s = 0;
  if (max !== min) {
    const d = max - min;
    s = l > 0.5 ? d / (2 - max - min) : d / (max + min);
    switch (max) {
      case r:
        h = ((g - b) / d + (g < b ? 6 : 0)) * 60;
        break;
      case g:
        h = ((b - r) / d + 2) * 60;
        break;
      default:
        h = ((r - g) / d + 4) * 60;
        break;
    }
  }
  return { h: Math.round(h), s: Math.round(s * 100), l: Math.round(l * 100) };
}

/** Pure helper exported for unit tests. */
export function hslToHex(input: { h: number; s: number; l: number }): string {
  const h = ((input.h % 360) + 360) % 360 / 360;
  const s = clampNum(input.s, 0, 100) / 100;
  const l = clampNum(input.l, 0, 100) / 100;
  let r: number;
  let g: number;
  let b: number;
  if (s === 0) {
    r = g = b = l;
  } else {
    const q = l < 0.5 ? l * (1 + s) : l + s - l * s;
    const p = 2 * l - q;
    r = hue2rgb(p, q, h + 1 / 3);
    g = hue2rgb(p, q, h);
    b = hue2rgb(p, q, h - 1 / 3);
  }
  const toHex = (v: number) => Math.round(v * 255).toString(16).padStart(2, "0");
  return `#${toHex(r)}${toHex(g)}${toHex(b)}`.toUpperCase();
}

function hue2rgb(p: number, q: number, t: number): number {
  let tt = t;
  if (tt < 0) tt += 1;
  if (tt > 1) tt -= 1;
  if (tt < 1 / 6) return p + (q - p) * 6 * tt;
  if (tt < 1 / 2) return q;
  if (tt < 2 / 3) return p + (q - p) * (2 / 3 - tt) * 6;
  return p;
}
