"use client";

import { useState } from "react";

import {
  type Envelope,
  pollRenderJob,
  type RenderJobView,
  submitRenderRun,
} from "@/lib/api/render-runs";
import type { JsonSchemaNode, UiHints } from "@/lib/json-schema-form/types";

import ArchetypeParamsForm from "./ArchetypeParamsForm";

/**
 * RenderForm (TECH-1674) — embeds `<ArchetypeParamsForm>` + integer
 * `variant_count` selector + Submit. Manages POST + long-poll progress.
 *
 * Submit flow:
 *   1. Validate archetype params via embedded form (button disabled while errors).
 *   2. POST `/api/render/runs` with `{ archetype_version_id, params_json, variant_count }`.
 *   3. Long-poll `GET /api/render/runs/[run_id]` until terminal status.
 *   4. On `done` with `run_id` populated, invoke `onComplete(run_id)` so caller
 *      can fetch `output_uris` + open `<VariantGrid>`.
 *   5. On `failed`, surface inline error banner; keep form open.
 *
 * Locked decision (DEC + §Plan Digest): long-poll only, no SSE.
 */

export type RenderFormProps = {
  archetypeId: string;
  archetypeVersionId: string;
  paramsSchema: JsonSchemaNode;
  uiHints?: UiHints;
  defaultParams?: unknown;
  defaultVariantCount?: number;
  onComplete: (runId: string) => void;
  onCancel?: () => void;
  /** Test seam — override default 1500 ms poll cadence. */
  pollIntervalMs?: number;
};

type Phase = "idle" | "submitting" | "polling" | "error";

const MIN_VARIANT_COUNT = 1;
const MAX_VARIANT_COUNT = 16;

export default function RenderForm(props: RenderFormProps) {
  const {
    archetypeId,
    archetypeVersionId,
    paramsSchema,
    uiHints,
    defaultParams,
    defaultVariantCount,
    onComplete,
    onCancel,
    pollIntervalMs,
  } = props;

  const [params, setParams] = useState<unknown>(defaultParams ?? {});
  const [variantCount, setVariantCount] = useState<number>(defaultVariantCount ?? 4);
  const [phase, setPhase] = useState<Phase>("idle");
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [progressMessage, setProgressMessage] = useState<string | null>(null);

  function handleSubmit(value: unknown) {
    setErrorMessage(null);
    setProgressMessage("Submitting render…");
    setPhase("submitting");

    const body = {
      archetype_id: archetypeId,
      archetype_version_id: archetypeVersionId,
      params_json: value as Record<string, unknown>,
      variant_count: variantCount,
    };

    submitRenderRun(body)
      .then((env) => {
        if (!env.ok) {
          const msg = env.error?.message ?? "Render submission failed";
          setErrorMessage(msg);
          setProgressMessage(null);
          setPhase("error");
          return;
        }
        const runId = env.data.job_id;
        setProgressMessage(`Render queued (run ${runId.slice(0, 8)}…); awaiting completion`);
        setPhase("polling");
        return pollRenderJob(runId, { intervalMs: pollIntervalMs }).then((pollEnv: Envelope<RenderJobView>) => {
          if (!pollEnv.ok) {
            setErrorMessage(pollEnv.error?.message ?? "Poll failed");
            setProgressMessage(null);
            setPhase("error");
            return;
          }
          const view = pollEnv.data;
          if (view.status === "failed") {
            setErrorMessage(view.error ?? "Render job reported failed status");
            setProgressMessage(null);
            setPhase("error");
            return;
          }
          if (view.status === "done") {
            const completedRunId = view.run_id ?? view.job_id;
            setProgressMessage(null);
            setPhase("idle");
            onComplete(completedRunId);
            return;
          }
          // Defensive — pollRenderJob already drives terminal exit.
          setErrorMessage(`Unexpected terminal status: ${view.status}`);
          setProgressMessage(null);
          setPhase("error");
        });
      })
      .catch((err: unknown) => {
        const msg = err instanceof Error ? err.message : "Network error";
        setErrorMessage(msg);
        setProgressMessage(null);
        setPhase("error");
      });
  }

  function handleVariantCountChange(e: React.ChangeEvent<HTMLInputElement>) {
    const raw = Number(e.currentTarget.value);
    if (Number.isFinite(raw)) {
      const clamped = Math.max(MIN_VARIANT_COUNT, Math.min(MAX_VARIANT_COUNT, Math.round(raw)));
      setVariantCount(clamped);
    }
  }

  const inFlight = phase === "submitting" || phase === "polling";

  return (
    <section
      data-testid="render-form"
      data-phase={phase}
      style={{ display: "flex", flexDirection: "column", gap: "var(--ds-spacing-sm)" }}
    >
      <header style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
        <h2 style={{ margin: 0, fontSize: "var(--ds-font-size-h3)" }}>New render</h2>
        {onCancel ? (
          <button
            type="button"
            data-testid="render-form-cancel"
            onClick={onCancel}
            disabled={inFlight}
            style={{ background: "transparent", border: 0, color: "var(--ds-text-muted)", cursor: "pointer" }}
          >
            Cancel
          </button>
        ) : null}
      </header>

      <div data-testid="render-form-variant-count" style={{ display: "flex", alignItems: "center", gap: "var(--ds-spacing-xs)" }}>
        <label htmlFor="render-variant-count" style={{ fontSize: "var(--ds-font-size-body-sm)" }}>
          Variant count
        </label>
        <input
          id="render-variant-count"
          data-testid="render-form-variant-count-input"
          type="number"
          min={MIN_VARIANT_COUNT}
          max={MAX_VARIANT_COUNT}
          step={1}
          value={variantCount}
          onChange={handleVariantCountChange}
          disabled={inFlight}
        />
      </div>

      <ArchetypeParamsForm
        schema={paramsSchema}
        hints={uiHints}
        value={params}
        onChange={setParams}
        onSubmit={handleSubmit}
      />

      {progressMessage ? (
        <p data-testid="render-form-progress" role="status" style={{ color: "var(--ds-text-accent-info)" }}>
          {progressMessage}
        </p>
      ) : null}

      {errorMessage ? (
        <p
          data-testid="render-form-error"
          role="alert"
          style={{ color: "var(--ds-text-accent-critical)" }}
        >
          {errorMessage}
        </p>
      ) : null}
    </section>
  );
}
