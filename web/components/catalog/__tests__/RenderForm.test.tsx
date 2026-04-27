/**
 * RenderForm component tests (TECH-1674).
 *
 * Two test surfaces:
 *  1. SSR shape — `renderToStaticMarkup` covers initial render contract
 *     (variant_count input + embedded ArchetypeParamsForm + cancel button +
 *     phase=idle + no progress/error banner).
 *  2. API client integration — direct unit tests of `submitRenderRun` /
 *     `pollRenderJob` / `patchRenderRunDisposition` via mocked `globalThis.fetch`.
 *     We validate the full submit → long-poll round-trip plus error paths
 *     against the typed wrapper layer the component consumes.
 */
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { renderToStaticMarkup } from "react-dom/server";

import RenderForm from "@/components/catalog/RenderForm";
import {
  getRenderJob,
  patchRenderRunDisposition,
  pollRenderJob,
  submitRenderRun,
  type Envelope,
  type RenderJobView,
} from "@/lib/api/render-runs";
import type { JsonSchemaNode } from "@/lib/json-schema-form/types";

const SCHEMA: JsonSchemaNode = {
  type: "object",
  properties: {
    wind: { type: "number", minimum: 0, maximum: 1, default: 0.5 },
    age: { type: "integer", minimum: 0, maximum: 100, default: 12 },
  },
};

const ARCH_ID = "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa";
const VERSION_ID = "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb";
const RUN_ID = "cccccccc-cccc-4ccc-8ccc-cccccccccccc";

function jsonResponse<T>(body: T, init?: ResponseInit): Response {
  return new Response(JSON.stringify(body), {
    status: init?.status ?? 200,
    headers: { "Content-Type": "application/json", ...(init?.headers ?? {}) },
  });
}

describe("<RenderForm /> SSR shape", () => {
  it("renders variant_count input + embedded ArchetypeParamsForm + cancel + phase=idle", () => {
    const html = renderToStaticMarkup(
      <RenderForm
        archetypeId={ARCH_ID}
        archetypeVersionId={VERSION_ID}
        paramsSchema={SCHEMA}
        defaultParams={{ wind: 0.5, age: 12 }}
        onComplete={() => {}}
        onCancel={() => {}}
      />,
    );

    expect(html).toContain('data-testid="render-form"');
    expect(html).toContain('data-phase="idle"');
    expect(html).toContain('data-testid="render-form-variant-count-input"');
    expect(html).toContain('data-testid="render-form-cancel"');
    expect(html).toContain('data-testid="archetype-params-form"');
    // Idle render shows neither progress nor error banner.
    expect(html).not.toContain('data-testid="render-form-progress"');
    expect(html).not.toContain('data-testid="render-form-error"');
  });

  it("omits cancel button when no onCancel handler is supplied", () => {
    const html = renderToStaticMarkup(
      <RenderForm
        archetypeId={ARCH_ID}
        archetypeVersionId={VERSION_ID}
        paramsSchema={SCHEMA}
        onComplete={() => {}}
      />,
    );
    expect(html).not.toContain('data-testid="render-form-cancel"');
  });

  it("clamps defaultVariantCount to range [1, 16] via input min/max attributes", () => {
    const html = renderToStaticMarkup(
      <RenderForm
        archetypeId={ARCH_ID}
        archetypeVersionId={VERSION_ID}
        paramsSchema={SCHEMA}
        defaultVariantCount={3}
        onComplete={() => {}}
      />,
    );
    expect(html).toMatch(/min="1"/);
    expect(html).toMatch(/max="16"/);
    expect(html).toMatch(/value="3"/);
  });
});

describe("render-runs API client (consumed by RenderForm)", () => {
  let fetchMock: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    fetchMock = vi.fn();
    globalThis.fetch = fetchMock as unknown as typeof globalThis.fetch;
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it("submitRenderRun POSTs JSON body + returns ok envelope", async () => {
    fetchMock.mockResolvedValueOnce(
      jsonResponse({ ok: true, data: { job_id: RUN_ID } } satisfies Envelope<{ job_id: string }>),
    );

    const env = await submitRenderRun({
      archetype_id: ARCH_ID,
      archetype_version_id: VERSION_ID,
      params_json: { wind: 0.5 },
      variant_count: 4,
    });

    expect(env.ok).toBe(true);
    if (env.ok) expect(env.data.job_id).toBe(RUN_ID);
    const call = fetchMock.mock.calls[0]!;
    expect(call[0]).toBe("/api/render/runs");
    expect(call[1]?.method).toBe("POST");
    const headers = call[1]?.headers as Record<string, string>;
    expect(headers["Content-Type"]).toBe("application/json");
    const parsedBody = JSON.parse(call[1]?.body as string) as Record<string, unknown>;
    expect(parsedBody.archetype_id).toBe(ARCH_ID);
    expect(parsedBody.variant_count).toBe(4);
  });

  it("submitRenderRun returns err envelope when server replies with 500", async () => {
    fetchMock.mockResolvedValueOnce(
      jsonResponse({ ok: false, error: { code: "internal", message: "boom" } } satisfies Envelope<{ job_id: string }>, { status: 500 }),
    );
    const env = await submitRenderRun({
      archetype_id: ARCH_ID,
      archetype_version_id: VERSION_ID,
      params_json: {},
    });
    expect(env.ok).toBe(false);
    if (!env.ok) {
      expect(env.error.code).toBe("internal");
      expect(env.error.message).toBe("boom");
    }
  });

  it("submitRenderRun forwards Idempotency-Key header when provided", async () => {
    fetchMock.mockResolvedValueOnce(jsonResponse({ ok: true, data: { job_id: RUN_ID } }));
    await submitRenderRun(
      { archetype_id: ARCH_ID, archetype_version_id: VERSION_ID, params_json: {} },
      { idempotencyKey: "key-001" },
    );
    const headers = fetchMock.mock.calls[0]![1]?.headers as Record<string, string>;
    expect(headers["Idempotency-Key"]).toBe("key-001");
  });

  it("getRenderJob calls GET on the run-id route", async () => {
    fetchMock.mockResolvedValueOnce(
      jsonResponse({
        ok: true,
        data: { job_id: RUN_ID, status: "queued", queue_position: 1 },
      } satisfies Envelope<RenderJobView>),
    );
    const env = await getRenderJob(RUN_ID);
    expect(env.ok).toBe(true);
    expect(fetchMock.mock.calls[0]![0]).toBe(`/api/render/runs/${RUN_ID}`);
    expect(fetchMock.mock.calls[0]![1]?.method).toBe("GET");
  });

  it("pollRenderJob exits on terminal `done` status with run_id linkage", async () => {
    fetchMock
      .mockResolvedValueOnce(jsonResponse({ ok: true, data: { job_id: RUN_ID, status: "queued", queue_position: 1 } }))
      .mockResolvedValueOnce(jsonResponse({ ok: true, data: { job_id: RUN_ID, status: "running" } }))
      .mockResolvedValueOnce(jsonResponse({ ok: true, data: { job_id: RUN_ID, status: "done", run_id: RUN_ID } }));

    const env = await pollRenderJob(RUN_ID, { intervalMs: 1, maxIterations: 10 });
    expect(env.ok).toBe(true);
    if (env.ok) {
      expect(env.data.status).toBe("done");
      expect(env.data.run_id).toBe(RUN_ID);
    }
    expect(fetchMock).toHaveBeenCalledTimes(3);
  });

  it("pollRenderJob exits on terminal `failed` status", async () => {
    fetchMock.mockResolvedValueOnce(
      jsonResponse({ ok: true, data: { job_id: RUN_ID, status: "failed", error: "sprite-gen unreachable" } }),
    );
    const env = await pollRenderJob(RUN_ID, { intervalMs: 1, maxIterations: 5 });
    expect(env.ok).toBe(true);
    if (env.ok) {
      expect(env.data.status).toBe("failed");
      expect(env.data.error).toBe("sprite-gen unreachable");
    }
  });

  it("patchRenderRunDisposition PATCHes variant_disposition_json body", async () => {
    fetchMock.mockResolvedValueOnce(
      jsonResponse({
        ok: true,
        data: { run_id: RUN_ID, variant_disposition_json: { v0: "discarded" } },
      }),
    );
    const env = await patchRenderRunDisposition(RUN_ID, { v0: "discarded" });
    expect(env.ok).toBe(true);
    const call = fetchMock.mock.calls[0]!;
    expect(call[0]).toBe(`/api/render/runs/${RUN_ID}`);
    expect(call[1]?.method).toBe("PATCH");
    const parsedBody = JSON.parse(call[1]?.body as string) as Record<string, unknown>;
    expect(parsedBody.variant_disposition_json).toEqual({ v0: "discarded" });
  });
});
