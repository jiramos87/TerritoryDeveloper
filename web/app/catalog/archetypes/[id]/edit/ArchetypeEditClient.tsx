"use client";

import Link from "next/link";
import { useEffect, useState } from "react";

import SchemaEditor from "@/components/archetype/SchemaEditor";
import type { JsonSchemaNode } from "@/lib/json-schema-form/types";
import type {
  CatalogArchetype,
  CatalogArchetypeVersion,
  CatalogArchetypeVersionWithPinCount,
  CatalogArchetypeVersionPatchBody,
} from "@/types/api/catalog-api";

type DetailPayload = {
  ok: "ok" | "error" | true;
  data?: {
    archetype: CatalogArchetype;
    versions: CatalogArchetypeVersionWithPinCount[];
  };
  error?: { code: string; message: string };
};

type PatchPayload = {
  ok: "ok" | "error" | true;
  data?: { version: CatalogArchetypeVersion };
  error?: { code: string; message: string };
};

/**
 * Archetype draft schema editor (TECH-2460 / Stage 11.1).
 *
 * Loads `/api/catalog/archetypes/by-id/[id]`, picks the draft version
 * (status="draft"), hydrates `<SchemaEditor />` with `params_json`, PATCHes
 * `/api/catalog/archetypes/[slug]/versions/[versionId]` on save with the
 * optimistic-lock fingerprint per DEC-A38.
 */
export default function ArchetypeEditClient({ entityId }: { entityId: string }) {
  const [archetype, setArchetype] = useState<CatalogArchetype | null>(null);
  const [draft, setDraft] = useState<CatalogArchetypeVersion | null>(null);
  const [schema, setSchema] = useState<JsonSchemaNode>({ type: "object", properties: {} });
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState<boolean>(false);
  const [saveMsg, setSaveMsg] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    fetch(`/api/catalog/archetypes/by-id/${encodeURIComponent(entityId)}`)
      .then((res) => res.json() as Promise<DetailPayload>)
      .then((payload) => {
        if (cancelled) return;
        if ((payload.ok !== "ok" && payload.ok !== true) || !payload.data) {
          setError(payload.error?.message ?? "Failed to load archetype");
          setLoading(false);
          return;
        }
        const arch = payload.data.archetype;
        const draftRow =
          payload.data.versions.find((v) => v.status === "draft") ?? null;
        setArchetype(arch);
        setDraft(draftRow);
        if (draftRow) setSchema(draftRow.params_json as JsonSchemaNode);
        setError(null);
        setLoading(false);
      })
      .catch((err: unknown) => {
        if (cancelled) return;
        setError(err instanceof Error ? err.message : "Network error");
        setLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, [entityId]);

  const handleSave = async () => {
    if (!archetype || !draft) return;
    setSaving(true);
    setSaveMsg(null);
    const body: CatalogArchetypeVersionPatchBody = {
      updated_at: draft.updated_at,
      params_json: schema as unknown as Record<string, unknown>,
    };
    try {
      const res = await fetch(
        `/api/catalog/archetypes/${encodeURIComponent(archetype.slug)}/versions/${encodeURIComponent(draft.version_id)}`,
        {
          method: "PATCH",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify(body),
        },
      );
      const payload = (await res.json()) as PatchPayload;
      if (!res.ok || (payload.ok !== "ok" && payload.ok !== true) || !payload.data) {
        setSaveMsg(payload.error?.message ?? "Save failed");
        setSaving(false);
        return;
      }
      setDraft(payload.data.version);
      setSaveMsg("Saved.");
      setSaving(false);
    } catch (err: unknown) {
      setSaveMsg(err instanceof Error ? err.message : "Network error");
      setSaving(false);
    }
  };

  if (loading) {
    return (
      <p data-testid="archetype-edit-loading" className="text-[var(--ds-text-muted)]">
        Loading archetype...
      </p>
    );
  }
  if (error) {
    return (
      <p
        data-testid="archetype-edit-error"
        role="alert"
        className="text-[var(--ds-text-accent-critical)]"
      >
        {error}
      </p>
    );
  }
  if (!archetype) {
    return (
      <p data-testid="archetype-edit-missing" className="text-[var(--ds-text-muted)]">
        Archetype not found.
      </p>
    );
  }
  if (!draft) {
    return (
      <div className="flex flex-col gap-[var(--ds-spacing-sm)]">
        <p data-testid="archetype-edit-no-draft" className="text-[var(--ds-text-muted)]">
          No draft version to edit. Bump a new version to start editing.
        </p>
        <Link
          href={`/catalog/archetypes/${entityId}/version/new`}
          data-testid="archetype-edit-bump-cta"
          className="self-start rounded bg-[var(--ds-text-accent-info)] px-[var(--ds-spacing-md)] py-[var(--ds-spacing-xs)] text-[var(--ds-bg-canvas)]"
        >
          Bump version
        </Link>
      </div>
    );
  }

  return (
    <div data-testid="archetype-edit" className="flex flex-col gap-[var(--ds-spacing-md)]">
      <header className="flex items-center justify-between">
        <div className="flex flex-col">
          <h1 className="text-[length:var(--ds-font-size-h2)] font-semibold">
            Edit draft v{draft.version_number}
          </h1>
          <span className="text-[var(--ds-text-muted)]">
            {archetype.display_name} ({archetype.slug})
          </span>
        </div>
        <div className="flex items-center gap-[var(--ds-spacing-xs)]">
          <Link
            href={`/catalog/archetypes/${entityId}`}
            data-testid="archetype-edit-back"
            className="rounded border border-[var(--ds-border-subtle)] px-[var(--ds-spacing-sm)] py-[var(--ds-spacing-xs)]"
          >
            Cancel
          </Link>
          <button
            type="button"
            data-testid="archetype-edit-save"
            disabled={saving}
            onClick={() => void handleSave()}
            className="rounded bg-[var(--ds-text-accent-info)] px-[var(--ds-spacing-md)] py-[var(--ds-spacing-xs)] text-[var(--ds-bg-canvas)] disabled:opacity-50"
          >
            {saving ? "Saving..." : "Save draft"}
          </button>
        </div>
      </header>
      {saveMsg ? (
        <p
          data-testid="archetype-edit-save-msg"
          role="status"
          className="text-[var(--ds-text-muted)]"
        >
          {saveMsg}
        </p>
      ) : null}
      <SchemaEditor value={schema} onChange={setSchema} />
    </div>
  );
}
