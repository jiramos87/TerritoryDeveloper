"use client";

import { useCallback, useEffect, useMemo, useState } from "react";

import EntityEditTabs, { type TabKey } from "@/components/catalog/EntityEditTabs";
import { type EntityRefRow } from "@/components/catalog/EntityRefPicker";
import ColorTokenEditor from "@/components/catalog/tokens/ColorTokenEditor";
import MotionTokenEditor from "@/components/catalog/tokens/MotionTokenEditor";
import RippleBanner from "@/components/catalog/tokens/RippleBanner";
import SemanticTokenEditor from "@/components/catalog/tokens/SemanticTokenEditor";
import SpacingTokenEditor from "@/components/catalog/tokens/SpacingTokenEditor";
import TypeScaleTokenEditor from "@/components/catalog/tokens/TypeScaleTokenEditor";
import VersionsTab from "@/components/versions/VersionsTab";
import type {
  CatalogTokenColorValue,
  CatalogTokenDto,
  CatalogTokenKind,
  CatalogTokenMotionValue,
  CatalogTokenPatchBody,
  CatalogTokenSemanticValue,
  CatalogTokenSpacingValue,
  CatalogTokenTypeScaleValue,
  CatalogTokenValueJson,
} from "@/types/api/catalog-api";

type DetailApiPayload = {
  ok: "ok" | "error" | true;
  data?: CatalogTokenDto;
  error?: { code: string; message: string };
};

/**
 * Spine token detail (TECH-2093 / Stage 10.1). Five-tab strip + kind-discriminated
 * editor dispatch on Edit tab. RippleBanner pinned above per DEC-A44. Save PATCHes
 * `/api/catalog/tokens/[slug]` with optimistic concurrency via `updated_at`.
 */
export default function TokenDetailClient({ slug }: { slug: string }) {
  const [token, setToken] = useState<CatalogTokenDto | null>(null);
  const [loading, setLoading] = useState<boolean>(true);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [saveError, setSaveError] = useState<string | null>(null);
  const [activeTab, setActiveTab] = useState<TabKey>("edit");

  // Edit-buffer state.
  const [displayName, setDisplayName] = useState<string>("");
  const [tokenKind, setTokenKind] = useState<CatalogTokenKind>("color");
  const [valueJson, setValueJson] = useState<CatalogTokenValueJson>(defaultValueFor("color"));
  const [semanticTargetId, setSemanticTargetId] = useState<number | null>(null);
  const [semanticTargetRow, setSemanticTargetRow] = useState<EntityRefRow | null>(null);
  const [cycle, setCycle] = useState<boolean>(false);

  const applyDto = useCallback((dto: CatalogTokenDto) => {
    setToken(dto);
    setDisplayName(dto.display_name);
    const detail = dto.token_detail;
    if (detail) {
      setTokenKind(detail.token_kind);
      setValueJson(detail.value_json as CatalogTokenValueJson);
      setSemanticTargetId(
        detail.semantic_target_entity_id == null
          ? null
          : Number.parseInt(detail.semantic_target_entity_id, 10),
      );
    } else {
      setTokenKind("color");
      setValueJson(defaultValueFor("color"));
      setSemanticTargetId(null);
    }
    if (dto.semantic_target_resolution) {
      setSemanticTargetRow({
        entity_id: dto.semantic_target_resolution.entity_id,
        slug: dto.semantic_target_resolution.slug,
        display_name: dto.semantic_target_resolution.display_name,
        kind: dto.semantic_target_resolution.kind,
        current_published_version_id: dto.semantic_target_resolution.current_published_version_id,
        retired_at: dto.semantic_target_resolution.retired_at,
      });
    } else {
      setSemanticTargetRow(null);
    }
    setCycle(false);
  }, []);

  useEffect(() => {
    let cancelled = false;
    fetch(`/api/catalog/tokens/${slug}`)
      .then((res) => res.json() as Promise<DetailApiPayload>)
      .then((payload) => {
        if (cancelled) return;
        if ((payload.ok !== "ok" && payload.ok !== true) || !payload.data) {
          setLoadError(payload.error?.message ?? "Token not found");
          setToken(null);
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
        setToken(null);
        setLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, [slug, applyDto]);

  function setKind(nextKind: CatalogTokenKind) {
    setTokenKind(nextKind);
    setValueJson(defaultValueFor(nextKind));
    if (nextKind !== "semantic") {
      setSemanticTargetId(null);
      setSemanticTargetRow(null);
    }
  }

  const dirty = useMemo(() => {
    if (!token) return false;
    if (displayName !== token.display_name) return true;
    const detail = token.token_detail;
    if (!detail) return true;
    if (tokenKind !== detail.token_kind) return true;
    if (JSON.stringify(valueJson) !== JSON.stringify(detail.value_json)) return true;
    const curTarget = detail.semantic_target_entity_id == null
      ? null
      : Number.parseInt(detail.semantic_target_entity_id, 10);
    if (semanticTargetId !== curTarget) return true;
    return false;
  }, [token, displayName, tokenKind, valueJson, semanticTargetId]);

  const saveDisabled = !dirty || cycle;

  function handleSave() {
    if (!token) return;
    setSaveError(null);
    const patch: CatalogTokenPatchBody = {
      updated_at: token.updated_at,
      display_name: displayName !== token.display_name ? displayName : undefined,
      token_detail: {
        token_kind: tokenKind,
        value_json: valueJson as Record<string, unknown>,
        semantic_target_entity_id:
          tokenKind === "semantic"
            ? semanticTargetId == null
              ? null
              : String(semanticTargetId)
            : null,
      },
    };
    fetch(`/api/catalog/tokens/${slug}`, {
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
      <p data-testid="token-detail-loading" className="text-[var(--ds-text-muted)]">
        Loading token…
      </p>
    );
  }
  if (loadError || !token) {
    return (
      <p data-testid="token-detail-error" role="alert" className="text-[var(--ds-text-accent-critical)]">
        {loadError ?? "Token not found"}
      </p>
    );
  }

  const selfEntityId = Number.parseInt(token.entity_id, 10);
  const editPanel = (
    <div data-testid="token-detail-edit-panel" className="flex flex-col gap-[var(--ds-spacing-md)]">
      <RippleBanner slug={slug} />

      {saveError ? (
        <p data-testid="token-detail-save-error" role="alert" className="text-[var(--ds-text-accent-critical)]">
          {saveError}
        </p>
      ) : null}

      <label className="flex flex-col gap-[var(--ds-spacing-xs)]">
        <span className="text-[var(--ds-text-muted)]">Display name</span>
        <input
          type="text"
          data-testid="token-detail-display-name"
          value={displayName}
          onChange={(e) => setDisplayName(e.currentTarget.value)}
          className="rounded border border-[var(--ds-border-subtle)] bg-[var(--ds-bg-panel)] px-[var(--ds-spacing-sm)] py-[var(--ds-spacing-xs)]"
        />
      </label>

      <label className="flex flex-col gap-[var(--ds-spacing-xs)]">
        <span className="text-[var(--ds-text-muted)]">Token kind</span>
        <select
          data-testid="token-detail-kind"
          value={tokenKind}
          onChange={(e) => setKind(e.currentTarget.value as CatalogTokenKind)}
          className="rounded border border-[var(--ds-border-subtle)] bg-[var(--ds-bg-panel)] px-[var(--ds-spacing-sm)] py-[var(--ds-spacing-xs)]"
        >
          <option value="color">color</option>
          <option value="type-scale">type-scale</option>
          <option value="motion">motion</option>
          <option value="spacing">spacing</option>
          <option value="semantic">semantic</option>
        </select>
      </label>

      {tokenKind === "color" ? (
        <ColorTokenEditor
          value={valueJson as CatalogTokenColorValue}
          onChange={(v) => setValueJson(v)}
        />
      ) : null}
      {tokenKind === "type-scale" ? (
        <TypeScaleTokenEditor
          value={valueJson as CatalogTokenTypeScaleValue}
          onChange={(v) => setValueJson(v)}
        />
      ) : null}
      {tokenKind === "motion" ? (
        <MotionTokenEditor
          value={valueJson as CatalogTokenMotionValue}
          onChange={(v) => setValueJson(v)}
        />
      ) : null}
      {tokenKind === "spacing" ? (
        <SpacingTokenEditor
          value={valueJson as CatalogTokenSpacingValue}
          onChange={(v) => setValueJson(v)}
        />
      ) : null}
      {tokenKind === "semantic" ? (
        <SemanticTokenEditor
          selfEntityId={selfEntityId}
          value={valueJson as CatalogTokenSemanticValue}
          targetRow={semanticTargetRow}
          targetId={semanticTargetId}
          onTargetChange={(id, row) => {
            setSemanticTargetId(id);
            setSemanticTargetRow(row);
          }}
          onValueChange={(v) => setValueJson(v)}
          onCycleChange={setCycle}
        />
      ) : null}
    </div>
  );

  return (
    <div data-testid="token-detail" className="flex flex-col gap-[var(--ds-spacing-md)]">
      <header className="flex items-center justify-between">
        <h1 className="text-[length:var(--ds-font-size-h2)] font-semibold">
          Token: <span className="font-mono">{token.slug}</span>
        </h1>
        <button
          type="button"
          data-testid="token-detail-save"
          disabled={saveDisabled}
          onClick={handleSave}
          className={
            !saveDisabled
              ? "rounded bg-[var(--ds-text-accent-info)] px-[var(--ds-spacing-md)] py-[var(--ds-spacing-xs)] text-[var(--ds-bg-canvas)]"
              : "rounded border border-[var(--ds-border-subtle)] px-[var(--ds-spacing-md)] py-[var(--ds-spacing-xs)] text-[var(--ds-text-muted)]"
          }
        >
          Save
        </button>
      </header>

      <EntityEditTabs
        activeTab={activeTab}
        onTabChange={setActiveTab}
        tabs={{
          edit: editPanel,
          versions: <VersionsTab entityId={token.entity_id} kind="token" />,
          references: <p data-testid="token-detail-placeholder-references" className="text-[var(--ds-text-muted)]">References — Stage 14.1 wires `catalog_ref_edge`.</p>,
          lints: <p data-testid="token-detail-placeholder-lints" className="text-[var(--ds-text-muted)]">Lints — Stage 12 ships this surface.</p>,
          audit: <p data-testid="token-detail-placeholder-audit" className="text-[var(--ds-text-muted)]">Audit — Stage 13 ships this surface.</p>,
        }}
      />
    </div>
  );
}

function defaultValueFor(kind: CatalogTokenKind): CatalogTokenValueJson {
  switch (kind) {
    case "color":
      return { hex: "#000000" } satisfies CatalogTokenColorValue;
    case "type-scale":
      return { font_family: "Inter", size_px: 14, line_height: 1.5 } satisfies CatalogTokenTypeScaleValue;
    case "motion":
      return { curve: "linear", duration_ms: 200 } satisfies CatalogTokenMotionValue;
    case "spacing":
      return { px: 8 } satisfies CatalogTokenSpacingValue;
    case "semantic":
      return { token_role: "" } satisfies CatalogTokenSemanticValue;
    default: {
      const exhaustive: never = kind;
      void exhaustive;
      return { hex: "#000000" } satisfies CatalogTokenColorValue;
    }
  }
}
