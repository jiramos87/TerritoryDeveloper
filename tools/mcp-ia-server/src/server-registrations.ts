/**
 * Shared tool-registration helpers (TECH-524 / B1 server split).
 *
 * Two buckets:
 *   - `registerIaCoreTools` — IA-authoring surfaces (backlog, router, glossary,
 *     spec, rules, invariants, journal, reserve, materialize, plan-apply).
 *   - `registerBridgeTools` — Unity-bridge + compute surfaces (bridge command,
 *     bridge lease, callers/subscribers, findobjectoftype scan, city metrics,
 *     compute helpers).
 *
 * Legacy `index.ts` default entry composes BOTH for backward compat; split
 * entries (`index-ia.ts`, `index-bridge.ts`) each compose one bucket.
 */

import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import type { SpecRegistryEntry } from "./parser/types.js";

type Registry = SpecRegistryEntry[];

// IA-core tools
import { registerListSpecs } from "./tools/list-specs.js";
import { registerSpecOutline } from "./tools/spec-outline.js";
import { registerSpecSection } from "./tools/spec-section.js";
import { registerGlossaryLookup } from "./tools/glossary-lookup.js";
import { registerGlossaryDiscover } from "./tools/glossary-discover.js";
import { registerRouterForTask } from "./tools/router-for-task.js";
import { registerInvariantsSummary } from "./tools/invariants-summary.js";
import { registerListRules } from "./tools/list-rules.js";
import { registerRuleContent, registerRuleSection } from "./tools/rule-content.js";
import { registerBacklogIssue } from "./tools/backlog-issue.js";
import { registerBacklogList } from "./tools/backlog-list.js";
import { registerBacklogRecordValidate } from "./tools/backlog-record-validate.js";
import { registerParentPlanValidate } from "./tools/parent-plan-validate.js";
import { registerReserveBacklogIds } from "./tools/reserve-backlog-ids.js";
import { registerSpecSections } from "./tools/spec-sections.js";
import { registerStageCloseoutDigest } from "./tools/stage-closeout-digest.js";
import { registerProjectSpecJournalTools } from "./tools/project-spec-journal.js";
import { registerBacklogSearch } from "./tools/backlog-search.js";
import { registerInvariantPreflight } from "./tools/invariant-preflight.js";
import { registerCsharpClassSummary } from "./tools/csharp-class-summary.js";
import { registerMasterPlanLocate } from "./tools/master-plan-locate.js";
import { registerMasterPlanNextPending } from "./tools/master-plan-next-pending.js";
import { registerPlanApplyValidate } from "./tools/plan-apply-validate.js";
import { registerRuntimeState } from "./tools/runtime-state.js";

// Bridge + compute tools
import {
  registerDesirabilityTopCells,
  registerGeographyInitParamsValidate,
  registerGridDistance,
  registerGrowthRingClassify,
  registerIsometricWorldToGrid,
  registerPathfindingCostPreview,
} from "./tools/compute/index.js";
import { registerUnityBridgeCommand } from "./tools/unity-bridge-command.js";
import { registerUnityBridgeExportSugarTools } from "./tools/unity-bridge-export-sugar.js";
import { registerUnityBridgeLease } from "./tools/unity-bridge-lease.js";
import { registerFindObjectOfTypeScan } from "./tools/findobjectoftype-scan.js";
import { registerCityMetricsQuery } from "./tools/city-metrics-query.js";
import { registerUnityCallersOf } from "./tools/unity-callers-of.js";
import { registerUnitySubscribersOf } from "./tools/unity-subscribers-of.js";

/**
 * Register IA-authoring tool surfaces on the given MCP server.
 *
 * ≥23 tools: list-specs, spec-outline, spec-section, spec-sections, glossary
 * lookup/discover, router-for-task, invariants-summary, list-rules,
 * rule-content/section, backlog-issue/list/search/record-validate,
 * parent-plan-validate, reserve-backlog-ids, stage-closeout-digest,
 * project-spec-journal (2), invariant-preflight, csharp-class-summary,
 * master-plan-locate, master-plan-next-pending, plan-apply-validate,
 * runtime_state.
 */
export function registerIaCoreTools(server: McpServer, registry: Registry): void {
  registerListSpecs(server, registry);
  registerSpecOutline(server, registry);
  registerSpecSection(server, registry);
  registerGlossaryLookup(server, registry);
  registerGlossaryDiscover(server, registry);
  registerRouterForTask(server, registry);
  registerInvariantsSummary(server, registry);
  registerListRules(server, registry);
  registerRuleContent(server, registry);
  registerRuleSection(server, registry);
  registerBacklogIssue(server);
  registerBacklogRecordValidate(server);
  registerParentPlanValidate(server);
  registerReserveBacklogIds(server);
  registerSpecSections(server, registry);
  registerStageCloseoutDigest(server);
  registerProjectSpecJournalTools(server);
  registerBacklogList(server);
  registerBacklogSearch(server);
  registerInvariantPreflight(server, registry);
  registerCsharpClassSummary(server);
  registerMasterPlanLocate(server);
  registerMasterPlanNextPending(server);
  registerPlanApplyValidate(server);
  registerRuntimeState(server);
}

/**
 * Register Unity-bridge + compute tool surfaces on the given MCP server.
 *
 * Bridge + compute bucket: unity-bridge-command, unity_export_cell_chunk,
 * unity_export_sorting_debug, unity-bridge-lease,
 * unity-callers-of, unity-subscribers-of, findobjectoftype-scan,
 * city-metrics-query, plus 6 compute helpers (isometric-world-to-grid,
 * growth-ring-classify, grid-distance, pathfinding-cost-preview,
 * geography-init-params-validate, desirability-top-cells).
 */
export function registerBridgeTools(server: McpServer): void {
  registerIsometricWorldToGrid(server);
  registerGrowthRingClassify(server);
  registerGridDistance(server);
  registerPathfindingCostPreview(server);
  registerGeographyInitParamsValidate(server);
  registerDesirabilityTopCells(server);
  registerUnityBridgeCommand(server);
  registerUnityBridgeExportSugarTools(server);
  registerUnityBridgeLease(server);
  registerFindObjectOfTypeScan(server);
  registerCityMetricsQuery(server);
  registerUnityCallersOf(server);
  registerUnitySubscribersOf(server);
}
