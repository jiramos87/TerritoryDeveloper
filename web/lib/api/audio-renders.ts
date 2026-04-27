/**
 * Typed client helpers for audio catalog + render-runs (TECH-1958).
 *
 * All functions return `Promise<{ ok: true; data } | { ok: false; error }>`
 * so callers can branch without exception handling. Network errors are
 * normalised into the same envelope shape.
 */

export type AudioListItemDto = {
  entity_id: string;
  slug: string;
  display_name: string;
  tags: string[];
  retired_at: string | null;
  current_published_version_id: string | null;
  active_version_id: string | null;
  active_version_status: "draft" | "published" | null;
  assets_path: string | null;
  source_uri: string | null;
  duration_ms: number | null;
  sample_rate: number | null;
  channels: number | null;
  loudness_lufs: number | null;
  peak_db: number | null;
  fingerprint: string | null;
  updated_at: string;
};

export type AudioDetailDto = AudioListItemDto & {
  active_version_params: Record<string, unknown> | null;
};

export type AudioListFilter = "active" | "retired" | "all";

export type AudioListResponse = {
  items: AudioListItemDto[];
  next_cursor: string | null;
};

export type ApiResult<T> =
  | { ok: true; data: T }
  | { ok: false; error: string; code?: string };

type Envelope<T> = {
  ok: "ok" | "error" | boolean;
  data?: T;
  error?: { code?: string; message?: string } | string;
};

async function unwrap<T>(res: Response): Promise<ApiResult<T>> {
  let body: Envelope<T>;
  try {
    body = (await res.json()) as Envelope<T>;
  } catch {
    return { ok: false, error: `Invalid JSON response (${res.status})` };
  }
  const okFlag = body.ok === true || body.ok === "ok";
  if (!okFlag || !body.data) {
    const errMsg =
      typeof body.error === "string"
        ? body.error
        : body.error?.message ?? `Request failed (${res.status})`;
    const code =
      typeof body.error === "object" && body.error
        ? body.error.code
        : undefined;
    return { ok: false, error: errMsg, code };
  }
  return { ok: true, data: body.data };
}

export async function fetchAudioList(
  filter: AudioListFilter = "active",
  limit = 50,
): Promise<ApiResult<AudioListResponse>> {
  const res = await fetch(`/api/catalog/audio?status=${filter}&limit=${limit}`);
  return unwrap<AudioListResponse>(res);
}

export async function fetchAudioBySlug(
  slug: string,
): Promise<ApiResult<AudioDetailDto>> {
  const res = await fetch(`/api/catalog/audio/${encodeURIComponent(slug)}`);
  return unwrap<AudioDetailDto>(res);
}

export type EnqueueRenderBody = {
  archetype_id: string;
  archetype_version_id: string;
  params_json: Record<string, unknown>;
};

export type EnqueueRenderResponse = { job_id: string };

export async function enqueueAudioRender(
  body: EnqueueRenderBody,
  idempotencyKey?: string,
): Promise<ApiResult<EnqueueRenderResponse>> {
  const headers: Record<string, string> = {
    "Content-Type": "application/json",
  };
  if (idempotencyKey) headers["Idempotency-Key"] = idempotencyKey;
  const res = await fetch("/api/render/runs", {
    method: "POST",
    headers,
    body: JSON.stringify(body),
  });
  return unwrap<EnqueueRenderResponse>(res);
}

export async function replayRenderRun(
  runId: string,
): Promise<ApiResult<EnqueueRenderResponse>> {
  const res = await fetch(`/api/render/runs/${encodeURIComponent(runId)}/replay`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
  });
  return unwrap<EnqueueRenderResponse>(res);
}

export async function reRenderIdentical(
  runId: string,
): Promise<ApiResult<EnqueueRenderResponse>> {
  const res = await fetch(
    `/api/render/runs/${encodeURIComponent(runId)}/identical`,
    {
      method: "POST",
      headers: { "Content-Type": "application/json" },
    },
  );
  return unwrap<EnqueueRenderResponse>(res);
}
