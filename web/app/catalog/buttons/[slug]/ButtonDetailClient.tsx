"use client";

import { useEffect, useMemo, useState } from "react";

import EntityPreview from "@/components/preview/EntityPreview";
import EntityRefPicker, { type EntityRefRow } from "@/components/catalog/EntityRefPicker";
import VersionsTab from "@/components/versions/VersionsTab";
import { SIZE_VARIANTS, type SizeVariant } from "@/lib/catalog/button-enums";
import type {
  CatalogButtonDto,
  CatalogButtonPatchBody,
} from "@/types/api/catalog-api";

type DetailApiPayload = {
  ok: "ok" | "error" | true;
  data?: CatalogButtonDto;
  error?: { code: string; message: string };
};

const SPRITE_SLOTS: ReadonlyArray<{ col: SpriteSlotCol; label: string }> = [
  { col: "sprite_idle_entity_id", label: "Idle" },
  { col: "sprite_hover_entity_id", label: "Hover" },
  { col: "sprite_pressed_entity_id", label: "Pressed" },
  { col: "sprite_disabled_entity_id", label: "Disabled" },
  { col: "sprite_icon_entity_id", label: "Icon" },
  { col: "sprite_badge_entity_id", label: "Badge" },
];

const TOKEN_SLOTS: ReadonlyArray<{ col: TokenSlotCol; label: string }> = [
  { col: "token_palette_entity_id", label: "Palette" },
  { col: "token_frame_style_entity_id", label: "Frame style" },
  { col: "token_font_entity_id", label: "Font" },
  { col: "token_illumination_entity_id", label: "Illumination" },
];

type SpriteSlotCol =
  | "sprite_idle_entity_id"
  | "sprite_hover_entity_id"
  | "sprite_pressed_entity_id"
  | "sprite_disabled_entity_id"
  | "sprite_icon_entity_id"
  | "sprite_badge_entity_id";

type TokenSlotCol =
  | "token_palette_entity_id"
  | "token_frame_style_entity_id"
  | "token_font_entity_id"
  | "token_illumination_entity_id";

type SlotCol = SpriteSlotCol | TokenSlotCol;

/**
 * Spine button detail (TECH-1885 / Stage 8.1). Renders 6 sprite + 4 token
 * `<EntityRefPicker>` slots per DEC-A7, plus size_variant <select>, action_id
 * <input>, and enable_predicate_json <textarea>. Save PATCHes through
 * `/api/catalog/buttons/[slug]`.
 */
export default function ButtonDetailClient({ slug }: { slug: string }) {
  const [button, setButton] = useState<CatalogButtonDto | null>(null);
  const [loading, setLoading] = useState<boolean>(true);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [saveError, setSaveError] = useState<string | null>(null);

  // Edit-buffer state.
  const [displayName, setDisplayName] = useState<string>("");
  const [slotIds, setSlotIds] = useState<Record<SlotCol, string | null>>(emptySlotIds());
  const [slotResolutions, setSlotResolutions] = useState<Record<SlotCol, EntityRefRow | null>>(
    emptySlotRows(),
  );
  const [sizeVariant, setSizeVariant] = useState<SizeVariant>("md");
  const [actionId, setActionId] = useState<string>("");
  const [predicateText, setPredicateText] = useState<string>("{}");
  const [predicateError, setPredicateError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    fetch(`/api/catalog/buttons/${slug}`)
      .then((res) => res.json() as Promise<DetailApiPayload>)
      .then((payload) => {
        if (cancelled) return;
        if ((payload.ok !== "ok" && payload.ok !== true) || !payload.data) {
          setLoadError(payload.error?.message ?? "Button not found");
          setButton(null);
          setLoading(false);
          return;
        }
        applyDto(payload.data);
        setLoadError(null);
        setLoading(false);
      })
      .catch((err: unknown) => {
        if (cancelled) return;
        setLoadError(err instanceof Error ? err.message : "Network error");
        setButton(null);
        setLoading(false);
      });
    return () => {
      cancelled = true;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [slug]);

  function applyDto(dto: CatalogButtonDto) {
    setButton(dto);
    setDisplayName(dto.display_name);
    const detail = dto.button_detail;
    const ids = emptySlotIds();
    if (detail) {
      ids.sprite_idle_entity_id = detail.sprite_idle_entity_id;
      ids.sprite_hover_entity_id = detail.sprite_hover_entity_id;
      ids.sprite_pressed_entity_id = detail.sprite_pressed_entity_id;
      ids.sprite_disabled_entity_id = detail.sprite_disabled_entity_id;
      ids.sprite_icon_entity_id = detail.sprite_icon_entity_id;
      ids.sprite_badge_entity_id = detail.sprite_badge_entity_id;
      ids.token_palette_entity_id = detail.token_palette_entity_id;
      ids.token_frame_style_entity_id = detail.token_frame_style_entity_id;
      ids.token_font_entity_id = detail.token_font_entity_id;
      ids.token_illumination_entity_id = detail.token_illumination_entity_id;
      setSizeVariant((SIZE_VARIANTS as readonly string[]).includes(detail.size_variant) ? (detail.size_variant as SizeVariant) : "md");
      setActionId(detail.action_id);
      setPredicateText(JSON.stringify(detail.enable_predicate_json ?? {}, null, 2));
    } else {
      setSizeVariant("md");
      setActionId("");
      setPredicateText("{}");
    }
    setSlotIds(ids);
    const rows = emptySlotRows();
    for (const col of slotCols()) {
      const v = dto.slot_resolutions[col];
      rows[col] = (v as EntityRefRow | null | undefined) ?? null;
    }
    setSlotResolutions(rows);
    setPredicateError(null);
  }

  function setSlot(col: SlotCol, id: string | null, row: EntityRefRow | null) {
    setSlotIds((prev) => ({ ...prev, [col]: id }));
    setSlotResolutions((prev) => ({ ...prev, [col]: row }));
  }

  const dirty = useMemo(() => {
    if (!button) return false;
    if (displayName !== button.display_name) return true;
    const detail = button.button_detail;
    if (!detail) return true;
    if (sizeVariant !== detail.size_variant) return true;
    if (actionId !== detail.action_id) return true;
    if (predicateText.trim() !== JSON.stringify(detail.enable_predicate_json ?? {}, null, 2).trim()) return true;
    for (const col of slotCols()) {
      const cur = slotIds[col];
      const prev = (detail as unknown as Record<string, unknown>)[col] as string | null;
      if ((cur ?? null) !== (prev ?? null)) return true;
    }
    return false;
  }, [button, displayName, sizeVariant, actionId, predicateText, slotIds]);

  function handleSave() {
    if (!button) return;
    setSaveError(null);
    setPredicateError(null);

    let predicateJson: Record<string, unknown> = {};
    try {
      const parsed = JSON.parse(predicateText) as unknown;
      if (typeof parsed !== "object" || parsed === null || Array.isArray(parsed)) {
        setPredicateError("enable_predicate_json must be a JSON object");
        return;
      }
      predicateJson = parsed as Record<string, unknown>;
    } catch (e: unknown) {
      setPredicateError(e instanceof Error ? e.message : "Invalid JSON");
      return;
    }

    const patch: CatalogButtonPatchBody = {
      updated_at: button.updated_at,
      display_name: displayName !== button.display_name ? displayName : undefined,
      button_detail: {
        size_variant: sizeVariant,
        action_id: actionId,
        enable_predicate_json: predicateJson,
        sprite_idle_entity_id: slotIds.sprite_idle_entity_id,
        sprite_hover_entity_id: slotIds.sprite_hover_entity_id,
        sprite_pressed_entity_id: slotIds.sprite_pressed_entity_id,
        sprite_disabled_entity_id: slotIds.sprite_disabled_entity_id,
        sprite_icon_entity_id: slotIds.sprite_icon_entity_id,
        sprite_badge_entity_id: slotIds.sprite_badge_entity_id,
        token_palette_entity_id: slotIds.token_palette_entity_id,
        token_frame_style_entity_id: slotIds.token_frame_style_entity_id,
        token_font_entity_id: slotIds.token_font_entity_id,
        token_illumination_entity_id: slotIds.token_illumination_entity_id,
      },
    };

    fetch(`/api/catalog/buttons/${slug}`, {
      method: "PATCH",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(patch),
    })
      .then((res) => res.json() as Promise<DetailApiPayload>)
      .then((payload) => {
        if ((payload.ok !== "ok" && payload.ok !== true) || !payload.data) {
          setSaveError(payload.error?.message ?? "Save failed");
          return;
        }
        applyDto(payload.data);
      })
      .catch((err: unknown) => {
        setSaveError(err instanceof Error ? err.message : "Network error");
      });
  }

  if (loading) {
    return (
      <p data-testid="button-detail-loading" className="text-[var(--ds-text-muted)]">
        Loading button…
      </p>
    );
  }
  if (loadError || !button) {
    return (
      <p data-testid="button-detail-error" role="alert" className="text-[var(--ds-text-accent-critical)]">
        {loadError ?? "Button not found"}
      </p>
    );
  }

  return (
    <div data-testid="button-detail" className="flex flex-col gap-[var(--ds-spacing-md)]">
      <header className="flex items-center justify-between">
        <h1 className="text-[length:var(--ds-font-size-h2)] font-semibold">
          Button: <span className="font-mono">{button.slug}</span>
        </h1>
        <button
          type="button"
          data-testid="button-detail-save"
          disabled={!dirty}
          onClick={handleSave}
          className={
            dirty
              ? "rounded bg-[var(--ds-text-accent-info)] px-[var(--ds-spacing-md)] py-[var(--ds-spacing-xs)] text-[var(--ds-bg-canvas)]"
              : "rounded border border-[var(--ds-border-subtle)] px-[var(--ds-spacing-md)] py-[var(--ds-spacing-xs)] text-[var(--ds-text-muted)]"
          }
        >
          Save
        </button>
      </header>

      {saveError ? (
        <p data-testid="button-detail-save-error" role="alert" className="text-[var(--ds-text-accent-critical)]">
          {saveError}
        </p>
      ) : null}

      <EntityPreview
        kind="button"
        label={displayName || button.slug}
        spriteSlots={buildSpriteSlots(slotResolutions)}
      />

      <label className="flex flex-col gap-[var(--ds-spacing-xs)]">
        <span className="text-[var(--ds-text-muted)]">Display name</span>
        <input
          type="text"
          data-testid="button-detail-display-name"
          value={displayName}
          onChange={(e) => setDisplayName(e.currentTarget.value)}
          className="rounded border border-[var(--ds-border-subtle)] bg-[var(--ds-bg-panel)] px-[var(--ds-spacing-sm)] py-[var(--ds-spacing-xs)]"
        />
      </label>

      <section data-testid="button-detail-sprite-slots" className="flex flex-col gap-[var(--ds-spacing-sm)]">
        <h2 className="text-[length:var(--ds-font-size-h3)] font-semibold">Sprite slots</h2>
        {SPRITE_SLOTS.map((slot) => (
          <EntityRefPicker
            key={slot.col}
            testId={`button-slot-${slot.col}`}
            label={slot.label}
            accepts_kind={["sprite"]}
            value={slotResolutions[slot.col]}
            valueId={slotIds[slot.col]}
            onChange={(id, row) => setSlot(slot.col, id, row)}
          />
        ))}
      </section>

      <section data-testid="button-detail-token-slots" className="flex flex-col gap-[var(--ds-spacing-sm)]">
        <h2 className="text-[length:var(--ds-font-size-h3)] font-semibold">Token slots</h2>
        {TOKEN_SLOTS.map((slot) => (
          <EntityRefPicker
            key={slot.col}
            testId={`button-slot-${slot.col}`}
            label={slot.label}
            accepts_kind={["token"]}
            value={slotResolutions[slot.col]}
            valueId={slotIds[slot.col]}
            onChange={(id, row) => setSlot(slot.col, id, row)}
          />
        ))}
      </section>

      <label className="flex flex-col gap-[var(--ds-spacing-xs)]">
        <span className="text-[var(--ds-text-muted)]">Size variant</span>
        <select
          data-testid="button-detail-size-variant"
          value={sizeVariant}
          onChange={(e) => setSizeVariant(e.currentTarget.value as SizeVariant)}
          className="rounded border border-[var(--ds-border-subtle)] bg-[var(--ds-bg-panel)] px-[var(--ds-spacing-sm)] py-[var(--ds-spacing-xs)]"
        >
          {SIZE_VARIANTS.map((sv) => (
            <option key={sv} value={sv}>
              {sv}
            </option>
          ))}
        </select>
      </label>

      <label className="flex flex-col gap-[var(--ds-spacing-xs)]">
        <span className="text-[var(--ds-text-muted)]">Action id</span>
        <input
          type="text"
          data-testid="button-detail-action-id"
          value={actionId}
          onChange={(e) => setActionId(e.currentTarget.value)}
          placeholder="UIManager entry-point slug"
          className="rounded border border-[var(--ds-border-subtle)] bg-[var(--ds-bg-panel)] px-[var(--ds-spacing-sm)] py-[var(--ds-spacing-xs)] font-mono"
        />
      </label>

      <label className="flex flex-col gap-[var(--ds-spacing-xs)]">
        <span className="text-[var(--ds-text-muted)]">Enable predicate (JSON)</span>
        <textarea
          data-testid="button-detail-enable-predicate"
          value={predicateText}
          onChange={(e) => setPredicateText(e.currentTarget.value)}
          rows={6}
          className="rounded border border-[var(--ds-border-subtle)] bg-[var(--ds-bg-panel)] px-[var(--ds-spacing-sm)] py-[var(--ds-spacing-xs)] font-mono"
        />
        {predicateError ? (
          <span data-testid="button-detail-predicate-error" role="alert" className="text-[var(--ds-text-accent-critical)]">
            {predicateError}
          </span>
        ) : null}
      </label>

      <VersionsTab entityId={button.entity_id} kind="button" />
    </div>
  );
}

function emptySlotIds(): Record<SlotCol, string | null> {
  return {
    sprite_idle_entity_id: null,
    sprite_hover_entity_id: null,
    sprite_pressed_entity_id: null,
    sprite_disabled_entity_id: null,
    sprite_icon_entity_id: null,
    sprite_badge_entity_id: null,
    token_palette_entity_id: null,
    token_frame_style_entity_id: null,
    token_font_entity_id: null,
    token_illumination_entity_id: null,
  };
}

function emptySlotRows(): Record<SlotCol, EntityRefRow | null> {
  return {
    sprite_idle_entity_id: null,
    sprite_hover_entity_id: null,
    sprite_pressed_entity_id: null,
    sprite_disabled_entity_id: null,
    sprite_icon_entity_id: null,
    sprite_badge_entity_id: null,
    token_palette_entity_id: null,
    token_frame_style_entity_id: null,
    token_font_entity_id: null,
    token_illumination_entity_id: null,
  };
}

/**
 * Adapter: derive ButtonPreview spriteSlots map from slot-resolution rows.
 * URL is synthesized as `/sprites/{slug}.png` — Stage 10.1 ships pure-DOM previews
 * (no pixel-diff golden); the URL surfaces in DOM as `data-sprite-url` for future
 * golden tests once a thumbnail endpoint exists.
 */
function buildSpriteSlots(
  rows: Record<SlotCol, EntityRefRow | null>,
): { idle?: { slug: string; url: string }; hover?: { slug: string; url: string }; pressed?: { slug: string; url: string }; disabled?: { slug: string; url: string }; icon?: { slug: string; url: string }; badge?: { slug: string; url: string } } {
  const out: Record<string, { slug: string; url: string }> = {};
  const map: Array<[SpriteSlotCol, "idle" | "hover" | "pressed" | "disabled" | "icon" | "badge"]> = [
    ["sprite_idle_entity_id", "idle"],
    ["sprite_hover_entity_id", "hover"],
    ["sprite_pressed_entity_id", "pressed"],
    ["sprite_disabled_entity_id", "disabled"],
    ["sprite_icon_entity_id", "icon"],
    ["sprite_badge_entity_id", "badge"],
  ];
  for (const [col, key] of map) {
    const row = rows[col];
    if (row) {
      out[key] = { slug: row.slug, url: `/sprites/${row.slug}.png` };
    }
  }
  return out;
}

function slotCols(): SlotCol[] {
  return [
    "sprite_idle_entity_id",
    "sprite_hover_entity_id",
    "sprite_pressed_entity_id",
    "sprite_disabled_entity_id",
    "sprite_icon_entity_id",
    "sprite_badge_entity_id",
    "token_palette_entity_id",
    "token_frame_style_entity_id",
    "token_font_entity_id",
    "token_illumination_entity_id",
  ];
}
