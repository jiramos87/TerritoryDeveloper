/**
 * plan-tree.ts — Pure plan-tree builder.
 *
 * Exports `TreeNodeData` discriminated union and `buildPlanTree()`.
 * Zero runtime dependencies beyond plan-loader-types.
 */

import type { PlanData, PlanMetrics, TaskRow, TaskStatus } from './plan-loader-types';
import type { Status } from '@/components/BadgeChip';

// Re-export Status so Stage 7.2 TreeNode.tsx imports from one place.
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

export interface StepNode extends NodeBase {
  kind: 'step';
  children: StageNode[];
  objective?: string;
  pendingDecompose?: boolean;
}

export interface StageNode extends NodeBase {
  kind: 'stage';
  children: PhaseNode[];
  objective?: string;
  pendingDecompose?: boolean;
}

export interface PhaseNode extends NodeBase {
  kind: 'phase';
  children: TaskNode[];
}

export interface TaskNode extends NodeBase {
  kind: 'task';
  children: never[];
  /** Issue id (e.g. "TECH-87") or "_pending_" */
  issue: string;
}

export type TreeNodeData = StepNode | StageNode | PhaseNode | TaskNode;

// ---------------------------------------------------------------------------
// Internal helpers
// ---------------------------------------------------------------------------

/**
 * Map a raw TaskStatus to the BadgeChip Status union.
 *
 * NB2: `'blocked'` is unreachable at MVP — no TaskStatus maps to it
 * (cross-ref TECH-341 JSDoc NB). Reserved in the union for future use.
 */
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
 * NB1 — Phase nodes are synthesized by `groupBy(task.phase)` within each
 * stage. This is intentionally distinct from `Stage.phases` (the checklist
 * entries in `PhaseEntry[]`), which can drift from the actual task list.
 * Task-derived grouping is the ground truth for tree rendering.
 *
 * @param plan    Parsed plan produced by the plan-loader.
 * @param metrics Pre-computed metrics from `computePlanMetrics()` (unused
 *                directly; kept in signature for future per-step count hints
 *                and API symmetry with sibling shapers).
 */
export function buildPlanTree(plan: PlanData, _metrics: PlanMetrics): TreeNodeData[] {
  return plan.steps.map((step) => {
    const stageNodes: StageNode[] = step.stages.map((stage) => {
      // Group tasks by phase string, lexicographic ascending order.
      const phaseMap = new Map<string, TaskRow[]>();
      for (const task of stage.tasks) {
        const bucket = phaseMap.get(task.phase);
        if (bucket) {
          bucket.push(task);
        } else {
          phaseMap.set(task.phase, [task]);
        }
      }

      // Sort phase keys lexicographically.
      const sortedPhaseKeys = [...phaseMap.keys()].sort();

      const phaseNodes: PhaseNode[] = sortedPhaseKeys.map((phaseKey) => {
        const tasks = phaseMap.get(phaseKey)!;
        const taskNodes: TaskNode[] = tasks.map((t) => {
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

        const counts = sumCounts(taskNodes);
        const status = propagateStatus(taskNodes);
        return {
          kind: 'phase',
          id: `${stage.id}-p${phaseKey}`,
          label: `Phase ${phaseKey}`,
          status,
          counts,
          children: taskNodes,
        };
      });

      const pendingDecomposeStage = stage.tasks.length === 0;
      const counts = sumCounts(phaseNodes);
      const status = propagateStatus(phaseNodes);
      return {
        kind: 'stage',
        id: stage.id,
        label: stage.title,
        status,
        counts,
        children: phaseNodes,
        objective: stage.objective,
        pendingDecompose: pendingDecomposeStage,
      };
    });

    const pendingDecomposeStep = step.stages.length === 0;
    const counts = sumCounts(stageNodes);
    const status = propagateStatus(stageNodes);
    return {
      kind: 'step',
      id: step.id,
      label: step.title,
      status,
      counts,
      children: stageNodes,
      objective: step.objective,
      pendingDecompose: pendingDecomposeStep,
    };
  });
}
