/**
 * Plan-loader type definitions.
 *
 * Mirrors `tools/progress-tracker/parse.mjs` JSDoc schema verbatim.
 * parse.mjs is authoritative — edit field names / union members there first,
 * then sync this file. Drift between here and parse.mjs lines 9–60 is a defect.
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
  id: string;      // e.g. "T1.1.1"
  phase: string;   // e.g. "1"
  issue: string;   // e.g. "TECH-87" or "_pending_"
  status: TaskStatus;
  intent: string;
}

export interface PhaseEntry {
  checked: boolean; // true = [x], false = [ ]
  label: string;    // phase label text
}

export interface Stage {
  id: string;           // e.g. "1.1"
  title: string;
  status: HierarchyStatus;
  statusDetail: string; // text after " — " in status line, if any
  phases: PhaseEntry[];
  tasks: TaskRow[];
}

export interface Step {
  id: string;           // e.g. "1"
  title: string;
  status: HierarchyStatus;
  statusDetail: string;
  stages: Stage[];
}

export interface PlanData {
  title: string;               // first # heading
  overallStatus: string;       // raw status line from opening blockquote
  overallStatusDetail: string; // text after " — " in overall status, if any
  siblingWarnings: string[];   // blockquote lines mentioning sibling orchestrators
  steps: Step[];
  allTasks: TaskRow[];         // flat list across all steps/stages (convenience)
}
