/**
 * validate-body.ts — request body shape checks for the render API.
 *
 * Plain hand-rolled validators (no zod dependency in `web/`). Mirrors the
 * pattern used by `lib/catalog/create-asset.ts`:
 *   - return `null` on success,
 *   - return `string` (single message) on failure for the simple form,
 *   - return `{ details: string[] }` for the structured-error form so the
 *     route can map to DEC-A48 `error.details` array.
 *
 * @see DEC-A48 mutate envelope — `error.code='validation'` carries `details`.
 */

const UUID_RE = /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/;

export type RenderRunRequestBody = {
  archetype_id: string;
  archetype_version_id: string;
  params_json: Record<string, unknown>;
};

export function validateRenderRunBody(body: unknown): { details: string[] } | null {
  const errs: string[] = [];
  if (body == null || typeof body !== "object" || Array.isArray(body)) {
    return { details: ["body must be a JSON object"] };
  }
  const b = body as Record<string, unknown>;

  if (typeof b.archetype_id !== "string" || !UUID_RE.test(b.archetype_id)) {
    errs.push("archetype_id must be a UUID string");
  }
  if (typeof b.archetype_version_id !== "string" || !UUID_RE.test(b.archetype_version_id)) {
    errs.push("archetype_version_id must be a UUID string");
  }
  if (b.params_json == null || typeof b.params_json !== "object" || Array.isArray(b.params_json)) {
    errs.push("params_json must be a JSON object");
  }

  return errs.length > 0 ? { details: errs } : null;
}

export type ReplayRequestBody = {
  params_json?: Record<string, unknown>;
};

/**
 * Replay body: empty allowed; only `params_json` may be present, and must
 * be a plain JSON object. Any other property is rejected (strict shape).
 */
export function validateReplayBody(body: unknown): { details: string[] } | null {
  // Empty body is allowed for replay — caller may rerun verbatim.
  if (body == null) return null;
  if (typeof body !== "object" || Array.isArray(body)) {
    return { details: ["body must be a JSON object or empty"] };
  }
  const b = body as Record<string, unknown>;
  const errs: string[] = [];
  if (b.params_json !== undefined) {
    if (b.params_json == null || typeof b.params_json !== "object" || Array.isArray(b.params_json)) {
      errs.push("params_json must be a JSON object when provided");
    }
  }
  for (const key of Object.keys(b)) {
    if (key !== "params_json") errs.push(`unexpected field: ${key}`);
  }
  return errs.length > 0 ? { details: errs } : null;
}

/**
 * Identical body: must be empty (or `{}`). Any field present is rejected.
 * Per DEC-A26 the identical re-render reuses source verbatim — clients
 * have nothing to override.
 */
export function validateIdenticalBody(body: unknown): { details: string[] } | null {
  if (body == null) return null;
  if (typeof body !== "object" || Array.isArray(body)) {
    return { details: ["body must be empty or {}"] };
  }
  const b = body as Record<string, unknown>;
  const keys = Object.keys(b);
  if (keys.length > 0) {
    return {
      details: keys.map((k) => `unexpected field: ${k}`),
    };
  }
  return null;
}
