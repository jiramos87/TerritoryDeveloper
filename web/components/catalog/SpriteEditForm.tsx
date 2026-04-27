"use client";

import { useMemo, useState } from "react";

const SLUG_RE = /^[a-z][a-z0-9_]{2,63}$/;

export type SpriteEditFormValue = {
  slug: string;
  display_name: string;
  tags: string[];
  sprite_detail: {
    pixels_per_unit: number;
    pivot_x: number;
    pivot_y: number;
    source_uri: string | null;
  };
};

export type SpriteEditFormProps = {
  /** When true, slug is frozen (post-publish per DEC-A24) and rendered read-only. */
  slugFrozen: boolean;
  initial: SpriteEditFormValue;
  onSubmit: (patch: {
    display_name: string;
    tags: string[];
    sprite_detail: SpriteEditFormValue["sprite_detail"];
  }) => void;
  /** Optional submit-side error (e.g. 409 from API) surfaced under footer. */
  submitError?: string | null;
};

/**
 * Edit form for sprite metadata + sprite_detail (TECH-1672).
 *
 * Slug is read-only when `slugFrozen` (post-publish per DEC-A24); editable
 * fields are display_name, tags, pixels_per_unit, pivot_x, pivot_y, source_uri.
 * Inline validation: slug regex + positive PPU + 0–1 pivot bounds.
 */
export default function SpriteEditForm({ slugFrozen, initial, onSubmit, submitError }: SpriteEditFormProps) {
  const [displayName, setDisplayName] = useState<string>(initial.display_name);
  const [tagsRaw, setTagsRaw] = useState<string>(initial.tags.join(", "));
  const [ppu, setPpu] = useState<number>(initial.sprite_detail.pixels_per_unit);
  const [pivotX, setPivotX] = useState<number>(initial.sprite_detail.pivot_x);
  const [pivotY, setPivotY] = useState<number>(initial.sprite_detail.pivot_y);
  const [sourceUri, setSourceUri] = useState<string>(initial.sprite_detail.source_uri ?? "");

  const errors = useMemo(() => {
    const errs: Record<string, string> = {};
    if (!slugFrozen && !SLUG_RE.test(initial.slug)) errs.slug = "Slug must match /^[a-z][a-z0-9_]{2,63}$/";
    if (displayName.trim() === "") errs.display_name = "Display name required";
    if (!(ppu > 0)) errs.pixels_per_unit = "PPU must be > 0";
    if (pivotX < 0 || pivotX > 1) errs.pivot_x = "Pivot X in [0, 1]";
    if (pivotY < 0 || pivotY > 1) errs.pivot_y = "Pivot Y in [0, 1]";
    return errs;
  }, [displayName, ppu, pivotX, pivotY, slugFrozen, initial.slug]);

  const hasErrors = Object.keys(errors).length > 0;

  function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    if (hasErrors) return;
    const tags = tagsRaw
      .split(",")
      .map((t) => t.trim())
      .filter((t) => t !== "");
    onSubmit({
      display_name: displayName.trim(),
      tags,
      sprite_detail: {
        pixels_per_unit: ppu,
        pivot_x: pivotX,
        pivot_y: pivotY,
        source_uri: sourceUri.trim() === "" ? null : sourceUri.trim(),
      },
    });
  }

  return (
    <form data-testid="sprite-edit-form" onSubmit={handleSubmit} className="flex flex-col gap-[var(--ds-spacing-sm)]">
      <label className="flex flex-col gap-[var(--ds-spacing-xs)]">
        <span className="text-[var(--ds-text-muted)]">Slug</span>
        <input
          type="text"
          data-testid="sprite-edit-slug"
          value={initial.slug}
          readOnly={slugFrozen}
          disabled={slugFrozen}
          aria-readonly={slugFrozen}
        />
        {errors.slug ? (
          <span data-testid="sprite-edit-error-slug" role="alert" className="text-[var(--ds-text-accent-critical)]">
            {errors.slug}
          </span>
        ) : null}
      </label>

      <label className="flex flex-col gap-[var(--ds-spacing-xs)]">
        <span className="text-[var(--ds-text-muted)]">Display name</span>
        <input
          type="text"
          data-testid="sprite-edit-display-name"
          value={displayName}
          onChange={(e) => setDisplayName(e.currentTarget.value)}
        />
        {errors.display_name ? (
          <span data-testid="sprite-edit-error-display-name" role="alert">
            {errors.display_name}
          </span>
        ) : null}
      </label>

      <label className="flex flex-col gap-[var(--ds-spacing-xs)]">
        <span className="text-[var(--ds-text-muted)]">Tags (comma-separated)</span>
        <input
          type="text"
          data-testid="sprite-edit-tags"
          value={tagsRaw}
          onChange={(e) => setTagsRaw(e.currentTarget.value)}
        />
      </label>

      <label className="flex flex-col gap-[var(--ds-spacing-xs)]">
        <span className="text-[var(--ds-text-muted)]">Pixels per unit</span>
        <input
          type="number"
          data-testid="sprite-edit-ppu"
          step="1"
          min="1"
          value={ppu}
          onChange={(e) => setPpu(Number(e.currentTarget.value))}
        />
        {errors.pixels_per_unit ? (
          <span data-testid="sprite-edit-error-ppu" role="alert">
            {errors.pixels_per_unit}
          </span>
        ) : null}
      </label>

      <label className="flex flex-col gap-[var(--ds-spacing-xs)]">
        <span className="text-[var(--ds-text-muted)]">Pivot X</span>
        <input
          type="number"
          data-testid="sprite-edit-pivot-x"
          step="0.01"
          min="0"
          max="1"
          value={pivotX}
          onChange={(e) => setPivotX(Number(e.currentTarget.value))}
        />
        {errors.pivot_x ? (
          <span data-testid="sprite-edit-error-pivot-x" role="alert">
            {errors.pivot_x}
          </span>
        ) : null}
      </label>

      <label className="flex flex-col gap-[var(--ds-spacing-xs)]">
        <span className="text-[var(--ds-text-muted)]">Pivot Y</span>
        <input
          type="number"
          data-testid="sprite-edit-pivot-y"
          step="0.01"
          min="0"
          max="1"
          value={pivotY}
          onChange={(e) => setPivotY(Number(e.currentTarget.value))}
        />
        {errors.pivot_y ? (
          <span data-testid="sprite-edit-error-pivot-y" role="alert">
            {errors.pivot_y}
          </span>
        ) : null}
      </label>

      <label className="flex flex-col gap-[var(--ds-spacing-xs)]">
        <span className="text-[var(--ds-text-muted)]">Source URI</span>
        <input
          type="text"
          data-testid="sprite-edit-source-uri"
          value={sourceUri}
          onChange={(e) => setSourceUri(e.currentTarget.value)}
          placeholder="gen://run-id/0 or Assets/Sprites/Generated/foo.png"
        />
      </label>

      <div className="flex gap-[var(--ds-spacing-sm)] items-center">
        <button type="submit" data-testid="sprite-edit-submit" disabled={hasErrors}>
          Save changes
        </button>
        {submitError ? (
          <span data-testid="sprite-edit-submit-error" role="alert" className="text-[var(--ds-text-accent-critical)]">
            {submitError}
          </span>
        ) : null}
      </div>
    </form>
  );
}
