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
import { registerPlanDigestVerifyPaths } from "./tools/plan-digest-verify-paths.js";
import { registerPlanDigestResolveAnchor } from "./tools/plan-digest-resolve-anchor.js";
import { registerPlanDigestRenderLiteral } from "./tools/plan-digest-render-literal.js";
import { registerPlanDigestScanForPicks } from "./tools/plan-digest-scan-for-picks.js";
import { registerPlanDigestLint } from "./tools/plan-digest-lint.js";
import { registerVerifyClassify } from "./tools/verify-classify.js";
import { registerIssueContextBundle } from "./tools/issue-context-bundle.js";
import { registerLifecycleStageContext } from "./tools/lifecycle-stage-context.js";
import { registerPlanDigestGateAuthorHelper } from "./tools/plan-digest-gate-author-helper.js";
import { registerCatalogList } from "./tools/catalog-list.js";
import { registerCatalogGet } from "./tools/catalog-get.js";
import { registerCatalogUpsert } from "./tools/catalog-upsert.js";
import {
  registerCatalogPoolGet,
  registerCatalogPoolList,
  registerCatalogPoolUpsert,
} from "./tools/catalog-pool-tools.js";
import { registerIaDbReadTools } from "./tools/ia-db-reads.js";
import { registerIaDbWriteTools } from "./tools/ia-db-writes.js";
import { registerMasterPlanRenderTools } from "./tools/master-plan-render-tools.js";
import { registerArchTools } from "./tools/arch.js";

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
 * ≥30 tools: list-specs, spec-outline, spec-section, spec-sections, glossary
 * lookup/discover, router-for-task, invariants-summary, list-rules,
 * rule-content/section, backlog-issue/list/search/record-validate,
 * reserve-backlog-ids, stage-closeout-digest,
 * project-spec-journal (2), invariant-preflight, csharp-class-summary,
 * master-plan-locate, master-plan-next-pending, plan-apply-validate,
 * runtime_state, plan-digest-verify-paths/resolve-anchor/render-literal/
 * scan-for-picks/lint/gate-author-helper/compile-stage-doc,
 * catalog_list/get/upsert, catalog_pool_list/get/upsert,
 * task_state, stage_state, master_plan_state, task_spec_body,
 * task_spec_section, task_spec_search, stage_bundle, task_bundle
 * (8 DB-backed reads — Step 3 of ia-dev-db-refactor),
 * task_insert, task_status_flip, task_spec_section_write,
 * task_commit_record, stage_verification_flip, stage_closeout_apply,
 * journal_append, fix_plan_write, fix_plan_consume
 * (9 DB-backed writes — Step 4 of ia-dev-db-refactor),
 * master_plan_render, stage_render, master_plan_preamble_write,
 * master_plan_description_write, master_plan_change_log_append
 * (4 DB-backed render + change-log surfaces — Step 9.6.8 of
 * ia-dev-db-refactor; replace `ia/projects/{slug}/{index.md, stage-*.md}`
 * filesystem reads/writes ahead of Step 9.6.11 folder bulk-delete).
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
  registerPlanDigestVerifyPaths(server);
  registerPlanDigestResolveAnchor(server);
  registerPlanDigestRenderLiteral(server);
  registerPlanDigestScanForPicks(server);
  registerPlanDigestLint(server);
  registerVerifyClassify(server);
  registerIssueContextBundle(server, registry);
  registerLifecycleStageContext(server, registry);
  registerPlanDigestGateAuthorHelper(server);
  registerCatalogList(server);
  registerCatalogGet(server);
  registerCatalogUpsert(server);
  registerCatalogPoolList(server);
  registerCatalogPoolGet(server);
  registerCatalogPoolUpsert(server);
  registerIaDbReadTools(server);
  registerIaDbWriteTools(server);
  registerMasterPlanRenderTools(server);
  registerArchTools(server);
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
