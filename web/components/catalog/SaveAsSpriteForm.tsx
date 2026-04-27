"use client";

import { useMemo, useState } from "react";

/**
 * SaveAsSpriteForm (TECH-1674) — slug picker + display_name + tags inputs;
 * caller wires to `POST /api/catalog/sprites` with `source_run_id` +
 * `source_variant_idx`.
 *
 * Inline validation matches the SpriteEditForm contract:
 *   - slug must match `/^[a-z][a-z0-9_]{2,63}$/`
 *   - display_name non-empty
 *   - tags optional comma-separated list
 */

const SLUG_RE = /^[a-z][a-z0-9_]{2,63}$/;

export type SaveAsSpriteFormSubmit = {
  slug: string;
  displayName: string;
  tags: string[];
};

export type SaveAsSpriteFormProps = {
  onSubmit: (value: SaveAsSpriteFormSubmit) => void;
  onCancel: () => void;
  defaultSlug?: string;
  defaultDisplayName?: string;
};

export default function SaveAsSpriteForm(props: SaveAsSpriteFormProps) {
  const { onSubmit, onCancel, defaultSlug, defaultDisplayName } = props;
  const [slug, setSlug] = useState<string>(defaultSlug ?? "");
  const [displayName, setDisplayName] = useState<string>(defaultDisplayName ?? "");
  const [tagsRaw, setTagsRaw] = useState<string>("");

  const errors = useMemo(() => {
    const out: Record<string, string> = {};
    if (!SLUG_RE.test(slug)) out.slug = "Slug must match ^[a-z][a-z0-9_]{2,63}$";
    if (displayName.trim().length === 0) out.displayName = "Display name required";
    return out;
  }, [slug, displayName]);

  const hasErrors = Object.keys(errors).length > 0;

  function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    if (hasErrors) return;
    const tags = tagsRaw
      .split(",")
      .map((t) => t.trim())
      .filter((t) => t.length > 0);
    onSubmit({ slug, displayName: displayName.trim(), tags });
  }

  return (
    <form
      data-testid="save-as-sprite-form"
      onSubmit={handleSubmit}
      style={{ display: "flex", flexDirection: "column", gap: "var(--ds-spacing-xs)", marginTop: "var(--ds-spacing-sm)" }}
    >
      <label style={{ display: "flex", flexDirection: "column", gap: 2 }}>
        <span style={{ fontSize: "var(--ds-font-size-body-sm)" }}>Slug</span>
        <input
          data-testid="save-as-sprite-slug"
          type="text"
          value={slug}
          onChange={(e) => setSlug(e.currentTarget.value)}
          aria-invalid={errors.slug ? "true" : "false"}
        />
        {errors.slug ? (
          <span data-testid="save-as-sprite-slug-error" role="alert" style={{ color: "var(--ds-text-accent-critical)", fontSize: "var(--ds-font-size-body-sm)" }}>
            {errors.slug}
          </span>
        ) : null}
      </label>

      <label style={{ display: "flex", flexDirection: "column", gap: 2 }}>
        <span style={{ fontSize: "var(--ds-font-size-body-sm)" }}>Display name</span>
        <input
          data-testid="save-as-sprite-display-name"
          type="text"
          value={displayName}
          onChange={(e) => setDisplayName(e.currentTarget.value)}
          aria-invalid={errors.displayName ? "true" : "false"}
        />
        {errors.displayName ? (
          <span data-testid="save-as-sprite-display-name-error" role="alert" style={{ color: "var(--ds-text-accent-critical)", fontSize: "var(--ds-font-size-body-sm)" }}>
            {errors.displayName}
          </span>
        ) : null}
      </label>

      <label style={{ display: "flex", flexDirection: "column", gap: 2 }}>
        <span style={{ fontSize: "var(--ds-font-size-body-sm)" }}>Tags (comma-separated)</span>
        <input
          data-testid="save-as-sprite-tags"
          type="text"
          value={tagsRaw}
          onChange={(e) => setTagsRaw(e.currentTarget.value)}
          placeholder="building, residential, low-density"
        />
      </label>

      <div style={{ display: "flex", gap: "var(--ds-spacing-xs)", marginTop: "var(--ds-spacing-xs)" }}>
        <button
          type="submit"
          data-testid="save-as-sprite-submit"
          disabled={hasErrors}
        >
          Save sprite
        </button>
        <button
          type="button"
          data-testid="save-as-sprite-cancel"
          onClick={onCancel}
        >
          Cancel
        </button>
      </div>
    </form>
  );
}
