// Tool role baselines — single source for skill-tools renderer + validator.
// Imported by both skill-tools/render-agent.ts and (later) the existing
// validate-agent-tools-uniformity.ts when it is ported to consume this map.

const COMMON = ["Read", "Edit", "Write", "Bash", "Grep", "Glob"] as const;

const PAIR_HEAD_MCP = [
  "mcp__territory-ia__router_for_task",
  "mcp__territory-ia__glossary_discover",
  "mcp__territory-ia__glossary_lookup",
  "mcp__territory-ia__invariants_summary",
  "mcp__territory-ia__spec_section",
  "mcp__territory-ia__spec_sections",
  "mcp__territory-ia__backlog_issue",
  "mcp__territory-ia__master_plan_locate",
  "mcp__territory-ia__list_rules",
  "mcp__territory-ia__rule_content",
];

const PAIR_TAIL_MCP = [
  "mcp__territory-ia__backlog_issue",
  "mcp__territory-ia__master_plan_locate",
];

const PLANNER_MCP = [
  "mcp__territory-ia__router_for_task",
  "mcp__territory-ia__glossary_discover",
  "mcp__territory-ia__glossary_lookup",
  "mcp__territory-ia__invariants_summary",
  "mcp__territory-ia__spec_section",
  "mcp__territory-ia__spec_sections",
  "mcp__territory-ia__backlog_issue",
  "mcp__territory-ia__list_rules",
  "mcp__territory-ia__rule_content",
];

const VERIFY_MCP = [
  "mcp__territory-ia__invariant_preflight",
  "mcp__territory-ia__rule_content",
  "mcp__territory-ia__verify_classify",
  "mcp__territory-ia__unity_compile",
  "mcp__territory-ia__unity_bridge_command",
  "mcp__territory-ia__unity_bridge_get",
];

export const TOOL_ROLE_BASELINES: Record<string, readonly string[]> = {
  // Single-task standalone ship pipeline (e.g. /ship)
  "standalone-pipeline": [...COMMON],

  // Stage-attached chained ship pipeline (e.g. /ship-stage)
  "stage-pipeline": [...COMMON],

  // Opus pair-head — reads, plans, emits tuple list
  "pair-head": [...COMMON, ...PAIR_HEAD_MCP],

  // Sonnet pair-tail — applies tuple list, runs validators, mutates yaml/spec
  "pair-tail": [...COMMON, ...PAIR_TAIL_MCP],

  // Generic planner / context-loader subagent
  planner: [...COMMON, ...PLANNER_MCP],

  // spec-implementer style — wide MCP including unity bridge
  implementer: [...COMMON, ...VERIFY_MCP],

  // verify-loop / verifier style — read + verify MCPs
  validator: ["Read", "Bash", "Grep", "Glob", ...VERIFY_MCP],

  // Generic lifecycle helper (small surface)
  "lifecycle-helper": ["Read", "Edit", "Glob"],

  // Custom — agent must list every tool in tools_extra
  custom: [],
};

export function resolveTools(tools_role: string, tools_extra: readonly string[]): string[] {
  const baseline = TOOL_ROLE_BASELINES[tools_role] ?? [];
  const merged = [...baseline, ...tools_extra];
  // Dedupe preserving insertion order
  const seen = new Set<string>();
  const result: string[] = [];
  for (const tool of merged) {
    if (seen.has(tool)) continue;
    seen.add(tool);
    result.push(tool);
  }
  return result;
}
