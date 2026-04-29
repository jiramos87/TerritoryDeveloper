/**
 * Tarjan strongly-connected-component cycle detection (TECH-2976).
 *
 * Pure-TS, O(V+E), no external dep. Used by `task_dep_register` to reject
 * cycle-inducing edges before COMMIT. Implementation is iterative to avoid
 * stack overflow on production-sized graphs (mirrors §Implementer Latitude).
 *
 * Edge convention: `edges[u]` lists nodes that `u` depends on. A cycle
 * through nodes A → B → A appears as an SCC of size ≥ 2. Self-loops
 * (`edges[A].includes(A)`) appear as singleton SCCs of size 1 — callers
 * decide whether to reject them.
 */

export interface SccResult {
  /** SCCs in reverse topological order (sinks first). */
  sccs: string[][];
  /** Convenience: SCC of size > 1 (true cycles, ignoring self-loops). */
  multiNodeSccs: string[][];
  /** Nodes that have a self-loop (`u ∈ edges[u]`). */
  selfLoops: string[];
}

/**
 * Run Tarjan SCC over a directed graph expressed as an adjacency map.
 *
 * Iterative implementation — recursion stack replaced with explicit work
 * queue + frame structs to keep call depth bounded irrespective of graph
 * size.
 */
export function tarjanScc(
  nodes: ReadonlyArray<string>,
  edges: ReadonlyMap<string, ReadonlyArray<string>>,
): SccResult {
  const indexOf = new Map<string, number>();
  const lowLink = new Map<string, number>();
  const onStack = new Set<string>();
  const stack: string[] = [];
  const sccs: string[][] = [];
  const selfLoops: string[] = [];
  let nextIndex = 0;

  // Detect self-loops up-front; they count as singleton SCCs but callers
  // need them flagged separately so they can short-circuit error response.
  for (const u of nodes) {
    const out = edges.get(u) ?? [];
    if (out.includes(u)) selfLoops.push(u);
  }

  // Iterative Tarjan: each frame holds (node, iterator pointer over its
  // out-edges). On entering a frame we set index/lowLink/push onto stack;
  // on exhausting edges we either pop SCC (root) or update parent lowLink.
  type Frame = { node: string; outIdx: number; outs: ReadonlyArray<string> };

  const dfs = (start: string): void => {
    if (indexOf.has(start)) return;
    const frames: Frame[] = [];
    const enter = (u: string): void => {
      indexOf.set(u, nextIndex);
      lowLink.set(u, nextIndex);
      nextIndex++;
      stack.push(u);
      onStack.add(u);
      frames.push({ node: u, outIdx: 0, outs: edges.get(u) ?? [] });
    };
    enter(start);

    while (frames.length > 0) {
      const frame = frames[frames.length - 1]!;
      if (frame.outIdx < frame.outs.length) {
        const w = frame.outs[frame.outIdx]!;
        frame.outIdx++;
        if (!indexOf.has(w)) {
          enter(w);
        } else if (onStack.has(w)) {
          lowLink.set(
            frame.node,
            Math.min(lowLink.get(frame.node)!, indexOf.get(w)!),
          );
        }
        continue;
      }
      // All outgoing edges processed.
      const u = frame.node;
      frames.pop();
      if (lowLink.get(u) === indexOf.get(u)) {
        const component: string[] = [];
        while (true) {
          const v = stack.pop()!;
          onStack.delete(v);
          component.push(v);
          if (v === u) break;
        }
        sccs.push(component);
      }
      if (frames.length > 0) {
        const parent = frames[frames.length - 1]!.node;
        lowLink.set(
          parent,
          Math.min(lowLink.get(parent)!, lowLink.get(u)!),
        );
      }
    }
  };

  for (const u of nodes) dfs(u);

  const multiNodeSccs = sccs.filter((c) => c.length > 1);
  return { sccs, multiNodeSccs, selfLoops };
}
