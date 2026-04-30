/**
 * Per-tool caller-agent allowlist for Territory IA MCP server.
 *
 * ALLOWLIST: Record<ToolName, readonly CallerAgent[]>
 *   - Covers all mutation + authorship tools gated by caller_agent.
 *   - Tool absent from map → bypass (read-only path; never throw).
 *   - Source: docs/mcp-lifecycle-tools-opus-4-7-audit-exploration.md §3.8
 *
 * checkCaller(tool, caller_agent): void
 *   - Throws plain POJO { code, message, hint } when caller missing or absent
 *     from the tool's allowlist. Matches wrapTool preservation branch in
 *     envelope.ts ("code" in err && "message" in err) — no Error subclass.
 */

import type { ErrorCode } from "../envelope.js";

// ---------------------------------------------------------------------------
// Allowlist map
// ---------------------------------------------------------------------------

/**
 * Per-tool caller allowlist.  Key = MCP tool name (external contract string).
 * Value = ordered list of authorized caller_agent values.
 *
 * Mutation tools (§3.6):
 *   orchestrator_task_update — flip task-table cells + Phase checkboxes in master plans.
 *   rollout_tracker_flip     — advance (a)–(g) lifecycle cell in rollout-tracker.md.
 *   backlog_record_upsert    — structured status / priority / depends_on / related updates.
 *
 * Authorship tools (§3.7):
 *   glossary_row_create   — append validated row to ia/specs/glossary.md.
 *   glossary_row_update   — patch existing glossary row (fuzzy-exact match).
 *   spec_section_append   — append new section under canonical spec.
 *   rule_create           — author ia/rules/*.md with required frontmatter.
 */
export const ALLOWLIST: Record<string, readonly string[]> = {
  // --- Mutation tools -------------------------------------------------------
  orchestrator_task_update: [
    "stage-file",
    "closeout",
    "ship-stage",
    "release-rollout-track",
  ],
  rollout_tracker_flip: [
    "release-rollout-track",
    "release-rollout",
  ],
  backlog_record_upsert: [
    "closeout",
    "stage-file",
    "project-new",
    "ship-stage",
  ],
  // --- Authorship tools -----------------------------------------------------
  glossary_row_create: [
    "spec-kickoff",
    "master-plan-new",
    "project-new",
    "closeout",
  ],
  glossary_row_update: [
    "spec-kickoff",
    "master-plan-new",
    "project-new",
    "closeout",
  ],
  spec_section_append: [
    "spec-kickoff",
    "closeout",
  ],
  rule_create: [
    "master-plan-new",
    "closeout",
  ],
  // --- Grid catalog mutations (Stage 1.4) ---------------------------------
  catalog_upsert: [
    "closeout",
    "stage-file",
    "project-new",
    "ship-stage",
  ],
  catalog_spawn_pool_upsert: [
    "closeout",
    "stage-file",
    "project-new",
    "ship-stage",
  ],
} as const;

// ---------------------------------------------------------------------------
// checkCaller gate
// ---------------------------------------------------------------------------

/**
 * Guard mutation + authorship tool handlers against unauthorized callers.
 *
 * @param tool         MCP tool name (e.g. "glossary_row_create").
 * @param caller_agent Value of the caller_agent input param (may be undefined).
 *
 * @throws `{ code: "unauthorized_caller", message, hint }` when:
 *   - caller_agent is undefined (missing from input), or
 *   - caller_agent is not listed in ALLOWLIST[tool].
 *
 * Tool absent from ALLOWLIST → returns void (read-only bypass).
 */
export function checkCaller(
  tool: string,
  caller_agent: string | undefined,
): void {
  const allowed = ALLOWLIST[tool];
  if (!allowed) return; // read-only bypass — tool not gated
  if (!caller_agent || !allowed.includes(caller_agent)) {
    throw {
      code: "unauthorized_caller" satisfies ErrorCode,
      message: `'${caller_agent ?? "<missing>"}' is not on the allowlist for '${tool}'.`,
      hint: `Allowed: ${allowed.join(", ")}.`,
    };
  }
}
