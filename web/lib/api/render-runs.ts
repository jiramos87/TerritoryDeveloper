/**
 * Typed client wrapper for `/api/render/runs/*` (TECH-1674).
 *
 * Pure-fetch helpers — no React, no DB. Tests mock `globalThis.fetch` directly.
 */

export type RenderRunStatus = "queued" | "running" | "done" | "failed";

export type DispositionLabel = "kept" | "discarded" | "saved";

export type RenderJobView = {
  job_id: string;
  status: RenderRunStatus;
  queue_position?: number;
  started_at?: string;
  finished_at?: string;
  error?: string;
  run_id?: string;
};

export type RenderRunSubmitBody = {
  archetype_id: string;
  archetype_version_id: string;
  params_json: Record<string, unknown>;
  variant_count?: number;
};

export type EnvelopeOk<T> = { ok: true; data: T };
export type EnvelopeErr = { ok: false; error: { code: string; message: string; details?: unknown } };
export type Envelope<T> = EnvelopeOk<T> | EnvelopeErr;

async function readEnvelope<T>(res: Response): Promise<Envelope<T>> {
  const json = (await res.json()) as Envelope<T>;
  return json;
}

export async function submitRenderRun(
  body: RenderRunSubmitBody,
  init?: { idempotencyKey?: string; signal?: AbortSignal },
): Promise<Envelope<{ job_id: string }>> {
  const headers: Record<string, string> = { "Content-Type": "application/json" };
  if (init?.idempotencyKey) headers["Idempotency-Key"] = init.idempotencyKey;
  const res = await fetch("/api/render/runs", {
    method: "POST",
    headers,
    body: JSON.stringify(body),
    signal: init?.signal,
  });
  return readEnvelope<{ job_id: string }>(res);
}

export async function getRenderJob(
  runId: string,
  init?: { signal?: AbortSignal },
): Promise<Envelope<RenderJobView>> {
  const res = await fetch(`/api/render/runs/${runId}`, { method: "GET", signal: init?.signal });
  return readEnvelope<RenderJobView>(res);
}

export async function patchRenderRunDisposition(
  runId: string,
  disposition: Record<string, DispositionLabel>,
  init?: { signal?: AbortSignal },
): Promise<Envelope<{ run_id: string; variant_disposition_json: Record<string, DispositionLabel> }>> {
  const res = await fetch(`/api/render/runs/${runId}`, {
    method: "PATCH",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ variant_disposition_json: disposition }),
    signal: init?.signal,
  });
  return readEnvelope<{ run_id: string; variant_disposition_json: Record<string, DispositionLabel> }>(res);
}

/**
 * Poll a render job until terminal status (`done` | `failed`) or the
 * abort signal fires. Returns the terminal envelope (or throws on signal abort).
 *
 * `intervalMs` (default 1500) controls the cadence; tests pass a tiny value
 * to keep poll runs fast.
 */
export async function pollRenderJob(
  runId: string,
  options?: { intervalMs?: number; signal?: AbortSignal; maxIterations?: number },
): Promise<Envelope<RenderJobView>> {
  const intervalMs = options?.intervalMs ?? 1500;
  const maxIterations = options?.maxIterations ?? 240; // 6 min @ 1.5 s
  for (let i = 0; i < maxIterations; i++) {
    if (options?.signal?.aborted) throw new Error("aborted");
    const env = await getRenderJob(runId, { signal: options?.signal });
    if (!env.ok) return env;
    if (env.data.status === "done" || env.data.status === "failed") return env;
    await new Promise((r) => setTimeout(r, intervalMs));
  }
  return { ok: false, error: { code: "poll_timeout", message: "Render job poll timeout" } };
}
