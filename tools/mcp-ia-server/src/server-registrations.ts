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
import { registerMasterPlanBundleApply } from "./tools/master-plan-bundle-apply.js";
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
import { registerTaskBundleBatch } from "./tools/task-bundle-batch.js";
import { registerPlanDigestGateAuthorHelper } from "./tools/plan-digest-gate-author-helper.js";
import { registerCatalogList } from "./tools/catalog-list.js";
import { registerCatalogGet } from "./tools/catalog-get.js";
import { registerCatalogUpsert } from "./tools/catalog-upsert.js";
import {
  registerCatalogPoolGet,
  registerCatalogPoolList,
  registerCatalogPoolUpsert,
} from "./tools/catalog-pool-tools.js";
import { registerCatalogReadTools, registerCatalogMutateTools } from "./tools/catalog-tools.js";
import { registerIaDbReadTools } from "./tools/ia-db-reads.js";
import { registerIaDbWriteTools } from "./tools/ia-db-writes.js";
import { registerMasterPlanRenderTools } from "./tools/master-plan-render-tools.js";
import { registerMasterPlanHealth } from "./tools/master-plan-health.js";
import { registerMasterPlanNextActionable } from "./tools/master-plan-next-actionable.js";
import { registerMasterPlanCrossImpactScan } from "./tools/master-plan-cross-impact-scan.js";
import { registerArchTools } from "./tools/arch.js";
import { registerArchSurfacesBackfill } from "./tools/arch-surfaces.js";
import { registerSeamsRun } from "./tools/seams-run.js";
import { registerIntentLint, registerTaskIntentGlossaryAlign } from "./tools/intent-lint.js";
import {
  registerTaskBatchInsert,
  registerStageDecomposeApply,
} from "./tools/task-batch-and-decompose.js";
import { registerTaskDiffAnomalyScan } from "./tools/task-diff-anomaly-scan.js";
import { registerGitDiffAnomalyScan } from "./tools/git-diff-anomaly-scan.js";
import { registerMasterPlanSections } from "./tools/master-plan-sections.js";
import { registerSectionClaimTools } from "./tools/section-claim.js";
import { registerStageClaimTools } from "./tools/stage-claim.js";
import { registerClaimHeartbeatTools } from "./tools/claim-heartbeat.js";
import { registerSectionCloseoutApply } from "./tools/section-closeout-apply.js";
import { registerMasterPlanLockArch } from "./tools/master-plan-lock-arch.js";
import { registerNextMigrationId } from "./tools/next-migration-id.js";
import { registerRedStageProofTools } from "./red-stage-proof/index.js";
import { registerRedStageProofMine } from "./tools/red-stage-proof-mine.js";
import { registerMcpCacheGet, registerMcpCacheSet } from "./tools/mcp-cache-tools.js";
import { registerDbReadBatch } from "./tools/db-read-batch.js";
import { registerThemePropose } from "./tools/theme-propose.js";
import { registerCronAuditLogEnqueue } from "./tools/cron-audit-log.js";
import { registerCronJournalAppendEnqueue } from "./tools/cron-journal-append.js";
import { registerCronTaskCommitRecordEnqueue } from "./tools/cron-task-commit-record.js";
import { registerCronStageVerificationFlipEnqueue } from "./tools/cron-stage-verification-flip.js";
import { registerCronArchChangelogAppendEnqueue } from "./tools/cron-arch-changelog-append.js";
import { registerCronMaterializeBacklogEnqueue } from "./tools/cron-materialize-backlog.js";
import { registerCronRegenIndexesEnqueue } from "./tools/cron-regen-indexes.js";
import { registerCronGlossaryBacklinksEnqueue } from "./tools/cron-glossary-backlinks.js";
import { registerCronAnchorReindexEnqueue } from "./tools/cron-anchor-reindex.js";
import { registerCronDriftLintEnqueue } from "./tools/cron-drift-lint.js";
import { registerCronCacheWarmEnqueue } from "./tools/cron-cache-warm.js";
import { registerCronCacheBustEnqueue } from "./tools/cron-cache-bust.js";
import { registerUiDefDriftScan } from "./tools/ui-def-drift-scan.js";
import { registerUiCalibrationCorpusQuery, registerUiCalibrationVerdictRecord } from "./tools/ui-calibration-corpus.js";
import { registerUiPanelGet, registerUiPanelList, registerUiPanelPublish } from "./tools/ui-panel.js";
import { registerUiTokenGet, registerUiTokenList, registerUiTokenPublish } from "./tools/ui-token.js";
import { registerUiComponentGet, registerUiComponentList, registerUiComponentPublish } from "./tools/ui-component.js";

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
 * ≥30 tools (incl. db-lifecycle-extensions Stage 2 additions:
 * master_plan_health, master_plan_next_actionable,
 * master_plan_cross_impact_scan):
 * list-specs, spec-outline, spec-section, spec-sections, glossary
 * lookup/discover, router-for-task, invariants-summary, list-rules,
 * rule-content/section, backlog-issue/list/search/record-validate,
 * reserve-backlog-ids, stage-closeout-digest,
 * project-spec-journal (2), invariant-preflight, csharp-class-summary,
 * master-plan-locate, master-plan-next-pending, plan-apply-validate,
 * runtime_state, plan-digest-verify-paths/resolve-anchor/render-literal/
 * scan-for-picks/lint/gate-author-helper/compile-stage-doc,
 * catalog_list/get/upsert, catalog_spawn_pool_list/get/upsert,
 * task_state, stage_state, master_plan_state, task_spec_body,
 * task_spec_section, task_spec_search, stage_bundle, task_bundle
 * (8 DB-backed reads — Step 3 of ia-dev-db-refactor),
 * task_insert, task_status_flip, task_spec_section_write,
 * stage_closeout_apply, fix_plan_write, fix_plan_consume
 * (DB-backed writes — Step 4 of ia-dev-db-refactor;
 * task_commit_record / stage_verification_flip / journal_append deleted
 * in async-cron-jobs Stage 6 — use cron_*_enqueue counterparts),
 * master_plan_render, stage_render, master_plan_preamble_write,
 * master_plan_description_write
 * (DB-backed render surfaces — Step 9.6.8 of ia-dev-db-refactor;
 * master_plan_change_log_append deleted in async-cron-jobs Stage 6
 * — use cron_audit_log_enqueue).
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
  registerMasterPlanBundleApply(server);
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
  registerTaskBundleBatch(server, registry);
  registerPlanDigestGateAuthorHelper(server);
  registerCatalogList(server);
  registerCatalogGet(server);
  registerCatalogUpsert(server);
  registerCatalogPoolList(server);
  registerCatalogPoolGet(server);
  registerCatalogPoolUpsert(server);
  registerCatalogReadTools(server);
  registerCatalogMutateTools(server);
  registerIaDbReadTools(server);
  registerIaDbWriteTools(server);
  registerMasterPlanRenderTools(server);
  registerMasterPlanHealth(server);
  registerMasterPlanNextActionable(server);
  registerMasterPlanCrossImpactScan(server);
  registerArchTools(server);
  registerArchSurfacesBackfill(server);
  registerSeamsRun(server);
  registerIntentLint(server);
  registerTaskIntentGlossaryAlign(server);
  registerTaskBatchInsert(server);
  registerStageDecomposeApply(server);
  registerTaskDiffAnomalyScan(server);
  registerGitDiffAnomalyScan(server);
  registerMasterPlanSections(server);
  registerSectionClaimTools(server);
  registerStageClaimTools(server);
  registerClaimHeartbeatTools(server);
  registerSectionCloseoutApply(server);
  registerMasterPlanLockArch(server);
  registerNextMigrationId(server);
  registerRedStageProofTools(server);
  registerRedStageProofMine(server);
  registerMcpCacheGet(server);
  registerMcpCacheSet(server);
  registerDbReadBatch(server);
  registerThemePropose(server);
  registerCronAuditLogEnqueue(server);
  registerCronJournalAppendEnqueue(server);
  registerCronTaskCommitRecordEnqueue(server);
  registerCronStageVerificationFlipEnqueue(server);
  registerCronArchChangelogAppendEnqueue(server);
  registerCronMaterializeBacklogEnqueue(server);
  registerCronRegenIndexesEnqueue(server);
  registerCronGlossaryBacklinksEnqueue(server);
  registerCronAnchorReindexEnqueue(server);
  registerCronDriftLintEnqueue(server);
  registerCronCacheWarmEnqueue(server);
  registerCronCacheBustEnqueue(server);
  registerUiDefDriftScan(server);
  registerUiCalibrationCorpusQuery(server);
  registerUiCalibrationVerdictRecord(server);
  registerUiPanelGet(server);
  registerUiPanelList(server);
  registerUiPanelPublish(server);
  registerUiTokenGet(server);
  registerUiTokenList(server);
  registerUiTokenPublish(server);
  registerUiComponentGet(server);
  registerUiComponentList(server);
  registerUiComponentPublish(server);
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
