/**
 * Recipe engine types — DEC-A19 Phase B.
 *
 * Mirror of tools/recipe-engine/schema/recipe.schema.json. Authoritative shape
 * is the schema; this file gives TS callers structural types so step modules
 * stay typesafe.
 */

export type StepKind = "mcp" | "bash" | "sql" | "seam" | "gate" | "flow";

export interface Recipe {
  recipe: string;
  recipe_version?: number;
  description?: string;
  inputs?: Record<string, unknown>;
  outputs?: Record<string, string>;
  steps: Step[];
}

export interface BaseStep {
  id: string;
  description?: string;
  bind?: string;
  when?: string;
  retry?: { max: number; backoff_ms?: number };
}

export interface McpStep extends BaseStep {
  mcp: string;
  args?: Record<string, unknown>;
}

export interface BashStep extends BaseStep {
  bash: string;
  args?: Record<string, unknown>;
}

export interface SqlStep extends BaseStep {
  sql: "query" | "exec";
  query: string;
  params?: unknown[];
}

export interface SeamStep extends BaseStep {
  seam: string;
  input: Record<string, unknown>;
}

export interface GateStep extends BaseStep {
  gate: string;
  args?: Record<string, unknown>;
}

export interface FlowSeqStep extends BaseStep {
  flow: "seq";
  steps: Step[];
}
export interface FlowParallelStep extends BaseStep {
  flow: "parallel";
  steps: Step[];
}
export interface FlowWhenStep extends BaseStep {
  flow: "when";
  cond: string;
  then?: Step[];
  else?: Step[];
}
export interface FlowUntilStep extends BaseStep {
  flow: "until";
  cond: string;
  steps: Step[];
  max_iters: number;
}
export interface FlowForeachStep extends BaseStep {
  flow: "foreach";
  items: string;
  as?: string;
  index_as?: string;
  steps: Step[];
}

export type FlowStep =
  | FlowSeqStep
  | FlowParallelStep
  | FlowWhenStep
  | FlowUntilStep
  | FlowForeachStep;
export type Step = McpStep | BashStep | SqlStep | SeamStep | GateStep | FlowStep;

export interface RunContext {
  run_id: string;
  recipe_slug: string;
  recipe_version: number;
  inputs: Record<string, unknown>;
  vars: Record<string, unknown>;
  cwd: string;
  dry_run: boolean;
  emit_trace: boolean;
  audit: AuditSink;
}

export interface AuditSink {
  begin(step: Step, parent_path: string): Promise<void>;
  end(step: Step, parent_path: string, result: StepResult): Promise<void>;
}

export interface TokenTotals {
  input_tokens: number;
  output_tokens: number;
  cache_read_tokens?: number;
  cache_creation_tokens?: number;
}

export interface SeamStepValue {
  seam: string;
  output: unknown;
  validated: boolean;
  token_totals?: TokenTotals;
  dispatch_mode?: "subagent" | "validate-only";
  dispatch_unavailable?: boolean;
}

export interface StepResult {
  ok: boolean;
  value?: unknown;
  skipped?: boolean;
  error?: { code: string; message: string; details?: unknown };
}

export function stepKind(s: Step): StepKind {
  if ("mcp" in s) return "mcp";
  if ("bash" in s) return "bash";
  if ("sql" in s) return "sql";
  if ("seam" in s) return "seam";
  if ("gate" in s) return "gate";
  if ("flow" in s) return "flow";
  throw new Error(`Unknown step kind: ${JSON.stringify(s)}`);
}
