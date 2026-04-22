/**
 * plan-tree.ts — Pure plan-tree builder (2-level Stage → Task).
 *
 * Exports `TreeNodeData` discriminated union and `buildPlanTree()`.
 * Zero runtime dependencies beyond plan-loader-types.
 *
 * Post lifecycle-refactor Stage 6: Step + Phase layers dropped.
 */

import type { PlanData, TaskStatus } from './plan-loader-types';
import type { Status } from '@/components/BadgeChip';

// Re-export Status so TreeNode.tsx imports from one place.
export type { Status };

// ---------------------------------------------------------------------------
// TreeNodeData discriminated union
// ---------------------------------------------------------------------------

interface NodeBase {
  id: string;
  label: string;
  status: Status;
  counts: { done: number; total: number };
}

export interface StageNode extends NodeBase {
  kind: 'stage';
  children: TaskNode[];
  objective?: string;
  pendingDecompose?: boolean;
}

export interface TaskNode extends NodeBase {
  kind: 'task';
  children: never[];
  /** Issue id (e.g. "TECH-87") or "_pending_" */
  issue: string;
}

export type TreeNodeData = StageNode | TaskNode;

// ---------------------------------------------------------------------------
// Internal helpers
// ---------------------------------------------------------------------------

function mapTaskStatus(t: TaskStatus): Status {
  switch (t) {
    case 'Done':
    case 'Done (archived)':
      return 'done';
    case 'In Progress':
    case 'In Review':
      return 'in-progress';
    case 'Draft':
    case '_pending_':
    default:
      return 'pending';
  }
}

/** Propagate status bottom-up from a non-empty children list. */
function propagateStatus(children: TreeNodeData[]): Status {
  if (children.length === 0) return 'pending';
  const statuses = children.map((c) => c.status);
  if (statuses.every((s) => s === 'done')) return 'done';
  if (statuses.some((s) => s === 'done' || s === 'in-progress')) return 'in-progress';
  return 'pending';
}

/** Sum counts bottom-up. */
function sumCounts(children: TreeNodeData[]): { done: number; total: number } {
  let done = 0;
  let total = 0;
  for (const c of children) {
    done += c.counts.done;
    total += c.counts.total;
  }
  return { done, total };
}

// ---------------------------------------------------------------------------
// Builder
// ---------------------------------------------------------------------------

/**
 * Build a renderable forest from a parsed plan.
 *
 * Post-refactor: returns `StageNode[]` with `TaskNode[]` children directly.
 * Phase grouping layer removed (lifecycle-refactor Stage 6).
 *
 * @param plan Parsed plan produced by the plan-loader.
 */
export function buildPlanTree(plan: PlanData): TreeNodeData[] {
  return plan.stages.map((stage) => {
    const taskNodes: TaskNode[] = stage.tasks.map((t) => {
      const s = mapTaskStatus(t.status);
      return {
        kind: 'task',
        id: t.id,
        label: t.intent,
        status: s,
        counts: { done: s === 'done' ? 1 : 0, total: 1 },
        children: [],
        issue: t.issue,
      };
    });

    const pendingDecomposeStage = stage.tasks.length === 0;
    const counts = sumCounts(taskNodes);
    const status = propagateStatus(taskNodes);
    return {
      kind: 'stage',
      id: stage.id,
      label: stage.title,
      status,
      counts,
      children: taskNodes,
      objective: stage.objective,
      pendingDecompose: pendingDecomposeStage,
    };
  });
}
