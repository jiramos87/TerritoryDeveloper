/**
 * Semantic-token one-hop resolver (TECH-2094 / Stage 10.1).
 *
 * Walks the `semantic_target_entity_id` alias chain ONE hop and returns the
 * resolved primitive's `value_json`. Deeper chains are intentionally clipped:
 * server-rendering each hop scales O(depth) and DEC-A44 only requires N-hop
 * ripple at edit time, NOT preview time. Two-hop chains return the first hop's
 * value plus a `truncated: true` flag so callers can surface a hint.
 *
 * Pure helper — caller injects a fetcher (DB-backed in production, in-memory
 * stub in tests).
 *
 * @see ia/projects/asset-pipeline/stage-10.1 — TECH-2094 §Plan Digest
 */

export type ResolvableTokenRow = {
  entity_id: number;
  token_kind: "color" | "type-scale" | "motion" | "spacing" | "semantic";
  value_json: Record<string, unknown>;
  semantic_target_entity_id: number | null;
};

export type SemanticResolveResult = {
  resolved: Record<string, unknown> | null;
  truncated: boolean;
  hops: number;
};

export type SemanticTokenFetcher = (
  entityId: number,
) => Promise<ResolvableTokenRow | null>;

export async function resolveSemanticOneHop(
  source: ResolvableTokenRow,
  fetcher: SemanticTokenFetcher,
): Promise<SemanticResolveResult> {
  if (source.token_kind !== "semantic") {
    return { resolved: source.value_json, truncated: false, hops: 0 };
  }
  if (source.semantic_target_entity_id == null) {
    return { resolved: null, truncated: false, hops: 0 };
  }
  const target = await fetcher(source.semantic_target_entity_id);
  if (target == null) {
    return { resolved: null, truncated: false, hops: 1 };
  }
  if (target.token_kind === "semantic") {
    // Two-hop chain — preview falls back to the first hop value (alias-of-alias).
    return { resolved: target.value_json, truncated: true, hops: 1 };
  }
  return { resolved: target.value_json, truncated: false, hops: 1 };
}
