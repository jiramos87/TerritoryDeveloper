"use client";

import Link from "next/link";
import { useEffect, useMemo, useState } from "react";

import MigrationHintEditor from "@/components/archetype/MigrationHintEditor";
import PinCountPreview from "@/components/archetype/PinCountPreview";
import SchemaEditor from "@/components/archetype/SchemaEditor";
import { diffSchemas } from "@/lib/archetype/diff-schemas";
import type { MigrationHint } from "@/lib/archetype/migration-hint-validator";
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

type CloneResp = {
  ok: "ok" | "error" | true;
  data?: { new_version_id: string };
  error?: { code: string; message: string };
};

type PatchResp = {
  ok: "ok" | "error" | true;
  data?: { version: CatalogArchetypeVersion };
  error?: { code: string; message: string };
};

type PublishResp = {
  ok: "ok" | "error" | true;
  data?: { version: CatalogArchetypeVersion };
  error?: { code: string; message: string; details?: Array<{ path: string; message: string }> };
};

/**
 * Bump-flow client for archetype version (TECH-2461).
 * - Loads archetype detail.
 * - If draft missing, clones latest published into a new draft (POST /versions).
 * - Hydrates SchemaEditor + MigrationHintEditor.
 * - Saves draft via PATCH; publishes via POST /publish.
 */
export default function VersionBumpClient({ entityId }: { entityId: string }) {
  const [archetype, setArchetype] = useState<CatalogArchetype | null>(null);
  const [draft, setDraft] = useState<CatalogArchetypeVersion | null>(null);
  const [parent, setParent] = useState<CatalogArchetypeVersion | null>(null);
  const [schema, setSchema] = useState<JsonSchemaNode>({ type: "object", properties: {} });
  const [hint, setHint] = useState<MigrationHint>({});
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState<boolean>(false);
  const [statusMsg, setStatusMsg] = useState<string | null>(null);
  const [hintErrors, setHintErrors] = useState<Array<{ path: string; message: string }>>([]);

  useEffect(() => {
    let cancelled = false;
    const run = async () => {
      try {
        const res = await fetch(`/api/catalog/archetypes/by-id/${encodeURIComponent(entityId)}`);
        const payload = (await res.json()) as DetailPayload;
        if (cancelled) return;
        if ((payload.ok !== "ok" && payload.ok !== true) || !payload.data) {
          setError(payload.error?.message ?? "Failed to load archetype");
          setLoading(false);
          return;
        }
        const arch = payload.data.archetype;
        const versions = payload.data.versions;
        const existingDraft = versions.find((v) => v.status === "draft") ?? null;
        const latestPublished = versions
          .filter((v) => v.status === "published")
          .sort((a, b) => b.version_number - a.version_number)[0];

        setArchetype(arch);

        if (existingDraft) {
          setDraft(existingDraft);
          setSchema(existingDraft.params_json as JsonSchemaNode);
          setHint((existingDraft.migration_hint_json ?? {}) as MigrationHint);
          if (existingDraft.parent_version_id) {
            const parentRow =
              versions.find((v) => v.version_id === existingDraft.parent_version_id) ?? null;
            setParent(parentRow);
          } else {
            setParent(latestPublished ?? null);
          }
          setLoading(false);
          return;
        }

        if (!latestPublished) {
          setError("No published version to bump from. Publish the initial draft first.");
          setLoading(false);
          return;
        }

        // Clone published -> draft.
        const cloneRes = await fetch(
          `/api/catalog/archetypes/${encodeURIComponent(arch.slug)}/versions`,
          {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ source_version_id: latestPublished.version_id }),
          },
        );
        const cloned = (await cloneRes.json()) as CloneResp;
        if (cancelled) return;
        if (!cloneRes.ok || (cloned.ok !== "ok" && cloned.ok !== true) || !cloned.data) {
          setError(cloned.error?.message ?? "Clone failed");
          setLoading(false);
          return;
        }
        // Re-fetch detail to pick up the new draft.
        const detail2 = await fetch(
          `/api/catalog/archetypes/by-id/${encodeURIComponent(entityId)}`,
        );
        const payload2 = (await detail2.json()) as DetailPayload;
        if (cancelled) return;
        if ((payload2.ok !== "ok" && payload2.ok !== true) || !payload2.data) {
          setError(payload2.error?.message ?? "Failed to reload after clone");
          setLoading(false);
          return;
        }
        const arch2 = payload2.data.archetype;
        const versions2 = payload2.data.versions;
        const newDraft = versions2.find((v) => v.version_id === cloned.data!.new_version_id) ?? null;
        setArchetype(arch2);
        if (newDraft) {
          setDraft(newDraft);
          setSchema(newDraft.params_json as JsonSchemaNode);
          setHint((newDraft.migration_hint_json ?? {}) as MigrationHint);
          setParent(latestPublished);
        }
        setLoading(false);
      } catch (e: unknown) {
        if (cancelled) return;
        setError(e instanceof Error ? e.message : "Network error");
        setLoading(false);
      }
    };
    void run();
    return () => {
      cancelled = true;
    };
  }, [entityId]);

  const diff = useMemo(() => {
    if (!parent) return { added: [], removed: [], renamed_candidates: [] };
    return diffSchemas(parent.params_json as JsonSchemaNode, schema);
  }, [parent, schema]);

  const handleSave = async () => {
    if (!archetype || !draft) return;
    setBusy(true);
    setStatusMsg(null);
    setHintErrors([]);
    const body: CatalogArchetypeVersionPatchBody = {
      updated_at: draft.updated_at,
      params_json: schema as unknown as Record<string, unknown>,
      migration_hint_json: hint as Record<string, unknown> as never,
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
      const payload = (await res.json()) as PatchResp;
      if (!res.ok || (payload.ok !== "ok" && payload.ok !== true) || !payload.data) {
        setStatusMsg(payload.error?.message ?? "Save failed");
        setBusy(false);
        return;
      }
      setDraft(payload.data.version);
      setStatusMsg("Draft saved.");
      setBusy(false);
    } catch (e: unknown) {
      setStatusMsg(e instanceof Error ? e.message : "Network error");
      setBusy(false);
    }
  };

  const handlePublish = async () => {
    if (!archetype || !draft) return;
    setBusy(true);
    setStatusMsg(null);
    setHintErrors([]);
    try {
      const res = await fetch(
        `/api/catalog/archetypes/${encodeURIComponent(archetype.slug)}/versions/${encodeURIComponent(draft.version_id)}/publish`,
        { method: "POST" },
      );
      const payload = (await res.json()) as PublishResp;
      if (res.status === 409 && payload.error?.details) {
        setHintErrors(payload.error.details);
        setStatusMsg(payload.error.message);
        setBusy(false);
        return;
      }
      if (!res.ok || (payload.ok !== "ok" && payload.ok !== true) || !payload.data) {
        setStatusMsg(payload.error?.message ?? "Publish failed");
        setBusy(false);
        return;
      }
      setStatusMsg(`Published v${payload.data.version.version_number}.`);
      setDraft(payload.data.version);
      setBusy(false);
    } catch (e: unknown) {
      setStatusMsg(e instanceof Error ? e.message : "Network error");
      setBusy(false);
    }
  };

  if (loading) {
    return (
      <p data-testid="version-bump-loading" className="text-[var(--ds-text-muted)]">
        Loading bump flow...
      </p>
    );
  }
  if (error) {
    return (
      <p
        data-testid="version-bump-error"
        role="alert"
        className="text-[var(--ds-text-accent-critical)]"
      >
        {error}
      </p>
    );
  }
  if (!archetype || !draft) {
    return (
      <p data-testid="version-bump-missing" className="text-[var(--ds-text-muted)]">
        No draft to bump.
      </p>
    );
  }

  return (
    <div data-testid="version-bump" className="flex flex-col gap-[var(--ds-spacing-md)]">
      <header className="flex items-center justify-between">
        <div className="flex flex-col">
          <h1 className="text-[length:var(--ds-font-size-h2)] font-semibold">
            Bump v{draft.version_number}
          </h1>
          <span className="text-[var(--ds-text-muted)]">
            {archetype.display_name} ({archetype.slug})
          </span>
        </div>
        <div className="flex items-center gap-[var(--ds-spacing-xs)]">
          <Link
            href={`/catalog/archetypes/${entityId}`}
            data-testid="version-bump-cancel"
            className="rounded border border-[var(--ds-border-subtle)] px-[var(--ds-spacing-sm)] py-[var(--ds-spacing-xs)]"
          >
            Cancel
          </Link>
          <button
            type="button"
            data-testid="version-bump-save"
            disabled={busy}
            onClick={() => void handleSave()}
            className="rounded border border-[var(--ds-border-subtle)] px-[var(--ds-spacing-md)] py-[var(--ds-spacing-xs)] disabled:opacity-50"
          >
            {busy ? "Saving..." : "Save draft"}
          </button>
          <button
            type="button"
            data-testid="version-bump-publish"
            disabled={busy || draft.status === "published"}
            onClick={() => void handlePublish()}
            className="rounded bg-[var(--ds-text-accent-info)] px-[var(--ds-spacing-md)] py-[var(--ds-spacing-xs)] text-[var(--ds-bg-canvas)] disabled:opacity-50"
          >
            Publish
          </button>
        </div>
      </header>

      {statusMsg ? (
        <p
          data-testid="version-bump-status"
          role="status"
          className="text-[var(--ds-text-muted)]"
        >
          {statusMsg}
        </p>
      ) : null}

      {hintErrors.length > 0 ? (
        <ul
          data-testid="version-bump-hint-errors"
          role="alert"
          className="rounded border border-[var(--ds-text-accent-critical)] p-[var(--ds-spacing-xs)] text-[var(--ds-text-accent-critical)]"
        >
          {hintErrors.map((err, i) => (
            <li key={i}>
              <code>{err.path}</code>: {err.message}
            </li>
          ))}
        </ul>
      ) : null}

      {parent ? (
        <PinCountPreview slug={archetype.slug} versionId={parent.version_id} />
      ) : null}

      <SchemaEditor value={schema} onChange={setSchema} />

      <MigrationHintEditor diff={diff} hint={hint} onChange={setHint} />
    </div>
  );
}
