/**
 * Plan-loader type definitions.
 *
 * Post lifecycle-refactor Stage 6: hierarchy collapsed to Stage → Task (2-level).
 * Legacy Step + Phase layers removed per `ia/rules/project-hierarchy.md`.
 *
 * Zero runtime code: export type / export interface only.
 */

export type TaskStatus =
  | '_pending_'
  | 'Draft'
  | 'In Review'
  | 'In Progress'
  | 'Done (archived)'
  | 'Done'; // short form found in some rows

export type HierarchyStatus =
  | 'Draft'
  | 'In Review'
  | 'In Progress' // may have trailing " — {active child}" detail
  | 'Final';

export interface TaskRow {
  id: string;       // e.g. "T1.1"
  name?: string;    // optional name column (some plans omit it)
  issue: string;    // e.g. "TECH-87" or "_pending_"
  status: TaskStatus;
  intent: string;
}

export interface Stage {
  id: string;           // e.g. "1" or "1.1"
  title: string;
  status: HierarchyStatus;
  statusDetail: string; // text after " — " in status line, if any
  objective?: string;   // **Objectives:** paragraph text
  tasks: TaskRow[];
}

export interface PlanData {
  title: string;               // first # heading
  filename: string;            // basename of the source file
  overallStatus: string;       // raw status line from opening blockquote
  overallStatusDetail: string; // text after " — " in overall status, if any
  siblingWarnings: string[];   // blockquote lines mentioning sibling orchestrators
  stages: Stage[];
  allTasks: TaskRow[];         // flat list across all stages (convenience)
}

/** Per-stage task-count breakdown for chart rendering. */
export interface StageChartBar {
  label: string;      // stage title
  pending: number;
  inProgress: number;
  done: number;
}

/** Per-stage done / total counts. */
export interface StageTaskCounts {
  done: number;
  total: number;
}

/** Pre-computed dashboard metrics for one PlanData. Derived by computePlanMetrics(). */
export interface PlanMetrics {
  completedCount: number;
  totalCount: number;
  /** Formatted "X / Y done" label for StatBar. */
  statBarLabel: string;
  /** Per-stage chart breakdown (parallel to plan.stages). */
  chartData: StageChartBar[];
  /** Per-stage done/total counts, keyed by stage.id. */
  stageCounts: Record<string, StageTaskCounts>;
}
