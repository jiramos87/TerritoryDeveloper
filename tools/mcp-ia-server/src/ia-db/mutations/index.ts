/**
 * Barrel re-export for all ia-db mutation clusters.
 *
 * Every import of `./mutations` or `./mutations/index` gets the same surface
 * as the original monolithic mutations.ts. Per-cluster files live under
 * mutations/{cluster}.ts — edit there, not here.
 */

// Shared error class re-exported so existing callers using
// `IaDbValidationError` from mutations.ts continue to work.
export { IaDbValidationError } from "./shared.js";

// Task cluster
export type {
  TaskInsertInput,
  TaskInsertResult,
  TaskDepRegisterResult,
  TaskDepRegisterCycleResult,
  TaskRawMarkdownWriteResult,
  TaskStatusFlipBatchInput,
  TaskStatusFlipBatchResult,
  TaskBatchInsertItem,
  TaskBatchInsertInput,
  TaskBatchInsertResult,
} from "./task.js";
export {
  mutateTaskInsert,
  mutateTaskDepRegister,
  mutateTaskRawMarkdownWrite,
  mutateTaskStatusFlip,
  mutateTaskStatusFlipBatch,
  mutateTaskSpecSectionWrite,
  mutateTaskCommitRecord,
  mutateTaskBatchInsert,
} from "./task.js";

// Stage cluster
export type {
  StageInsertInput,
  StageInsertResult,
  StageUpdateInput,
  StageUpdateResult,
  StageDeleteInput,
  StageDeleteResult,
  StageBodyWriteResult,
  StageDecomposeApplyInput,
  StageDecomposeApplyResult,
} from "./stage.js";
export {
  mutateStageVerificationFlip,
  mutateStageCloseoutApply,
  mutateStageInsert,
  mutateStageUpdate,
  mutateStageDelete,
  mutateStageBodyWrite,
  mutateStageDecomposeApply,
} from "./stage.js";

// Master-plan cluster
export type {
  MasterPlanPreambleWriteResult,
  MasterPlanDescriptionWriteResult,
  MasterPlanInsertResult,
  MasterPlanCloseResult,
  MasterPlanVersionCreateInput,
  MasterPlanVersionCreateResult,
} from "./master-plan.js";
export {
  mutateMasterPlanPreambleWrite,
  mutateMasterPlanDescriptionWrite,
  mutateMasterPlanChangeLogAppend,
  mutateMasterPlanInsert,
  mutateMasterPlanClose,
  mutateMasterPlanVersionCreate,
} from "./master-plan.js";

// Journal cluster
export type { JournalAppendInput } from "./journal.js";
export {
  mutateJournalAppend,
  mutateFixPlanWrite,
  mutateFixPlanConsume,
} from "./journal.js";
