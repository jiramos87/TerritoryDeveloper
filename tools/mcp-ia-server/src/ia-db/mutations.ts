/**
 * DB write mutations for the IA tables (Step 4 of ia-dev-db-refactor).
 *
 * This file is now a thin re-export barrel. All mutations live in
 * mutations/{cluster}.ts and are re-exported from mutations/index.ts.
 *
 * Existing callers (`import { ... } from './mutations'` or
 * `'../ia-db/mutations.js'`) continue to work without modification.
 *
 * Source of truth for decisions: docs/ia-dev-db-refactor-implementation.md
 * §Step 4.
 */

export * from "./mutations/index.js";

// Re-export read helper so tools can round-trip without two imports.
export { queryTaskBody, queryTaskState } from "./queries.js";
export type { TaskStateDB } from "./queries.js";
