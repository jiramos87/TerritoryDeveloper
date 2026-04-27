/**
 * Semantic-token cycle detection (TECH-2093 / Stage 10.1).
 *
 * Walks the `semantic_target_entity_id` chain via dep-injected fetcher; aborts
 * when a previously visited node reappears. Used both client-side (pre-flight
 * UX feedback in <SemanticTokenEditor>) and server-side (PATCH gate in the
 * tokens by-slug route) per DEC-A44 "no cycles".
 *
 * @see ia/projects/asset-pipeline/stage-10.1 — TECH-2093 §Plan Digest
 */

export type CycleCheckResult = { cycle: boolean; path: number[] };

export type SemanticTargetFetcher = (id: number) => Promise<number | null>;

/**
 * DFS the alias chain starting at sourceId → targetId. The hypothetical edge
 * (source → target) is checked against the existing chain rooted at target;
 * if walking from target ever revisits sourceId (or any earlier node) the
 * caller would create a cycle.
 *
 * Path returned: ordered chain incl. cycle revisit (e.g. [src, b, c, src])
 * when cycle=true; full traversed chain (e.g. [src, target, ...]) when false.
 */
export async function semanticCycleCheck(
  sourceId: number,
  targetId: number,
  fetcher: SemanticTargetFetcher,
): Promise<CycleCheckResult> {
  if (sourceId === targetId) {
    return { cycle: true, path: [sourceId, sourceId] };
  }
  const visited = new Set<number>();
  const path: number[] = [sourceId, targetId];
  visited.add(sourceId);
  visited.add(targetId);

  let cursor: number | null = targetId;
  while (cursor != null) {
    const next = await fetcher(cursor);
    if (next == null) return { cycle: false, path };
    if (visited.has(next)) {
      path.push(next);
      return { cycle: true, path };
    }
    visited.add(next);
    path.push(next);
    cursor = next;
  }
  return { cycle: false, path };
}
