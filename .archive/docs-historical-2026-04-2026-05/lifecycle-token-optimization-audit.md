# Lifecycle Token Optimization — Proposed Tasks

**Scope:** Core lifecycle skills — project-new, project-new-apply, project-spec-implement, stage-closeout-plan/apply, plan-author, stage-file-plan, opus-audit, ship-stage, domain-context-load.

**Status:** Proposals Registry complete. No BACKLOG issues filed. Unblocking path: Stage 6 → Stage 7 → Stage 7b.

---

## Proposals Registry

*Canonical, deduplicated list of all proposals. Sources: original audit phases 1–8, Appendix C/D.*

**Types:** TRACKED = exists in a master plan; SKILL-EDIT = inline SKILL.md or spec edit (no new TECH); NEW-TASK = one new TECH issue; NEW-STAGE = ≥2-task block.

**Status:** TRACKED | READY (no blockers) | BLOCKED (depends on unshipped work) | SKIP (superseded).

### Deduplication Resolutions

| Duplicate source | Resolution |
|---|---|
| Appendix C "Remove dead composite-first branches" | Same as Stage 6 T6.2 → TRACKED (P1-A). |
| §5.4 `stage_ship_context` concept | Absorbed into `ship_stage_bundle` — same design, different name. |
| §5.5 `stage_closeout_bundle` concept | Absorbed into `closeout_plan_bundle`. |
| Appendix C "Add server-side cache for lifecycle_stage_context" | Baked into §8.6 aggregator cache (30-min TTL, mtime-keyed). |
| §7.2 NEW-1 vs §8.5 NEW-1 (opus-audit CHAIN_CONTEXT) | Unified → SKILL-EDIT NEW-1. |
| Appendix C "Parallelize domain-context-load" vs §7.2 NEW-4 | Unified → SKILL-EDIT NEW-4, Status = SKIP. |
| Appendix C "next_stage_resolver" vs §7.2 NEW-5 | Unified → NEW-TASK NEW-5. |
| §8.5 NEW-11 vs Stage 7 T7.1 (`issue_context_bundle`) | NEW-11 = spec amendment to T7.1 before ship — SKILL-EDIT. |

### Dependency DAG

```
Stage 6 T6.2 (P1-A, ghost-branch removal)
  ↓
Stage 7 T7.3 (P0-A, lifecycle_stage_context)
  → NEW-6 (ship_stage_bundle)
  → NEW-7 (plan_author_bundle)
  → NEW-8 (stage_file_plan_bundle)
  → NEW-9 (opus_audit_bundle — 2 tasks)
  → NEW-10 (closeout_plan_bundle)

Stage 7 T7.1 (P0-B, issue_context_bundle)
  → NEW-11 (implement_bundle chain_context param — amend T7.1 spec)

Independent — READY now:
  NEW-1  (opus-audit CHAIN_CONTEXT guard)
  NEW-2  (Changelog split)
  NEW-3  (project-new backlog_list)
  NEW-5  (next_stage_resolver)
  NEW-12 (fold /author+/plan-review into /ship N=1)

  NEW-4 → SKIP
```

### Registry Table

| ID | Title | Type | Placement | Depends on | Effort | Token savings/run | Status |
|---|---|---|---|---|---|---|---|
| P0-A | Build `lifecycle_stage_context` composite MCP tool | TRACKED | mcp-lifecycle-tools Stage 7 T7.3 | Stage 6 T6.1 | — | ~3–5k / Stage-open skill | TRACKED |
| P0-B | Build `issue_context_bundle` composite MCP tool | TRACKED | mcp-lifecycle-tools Stage 7 T7.1 | Stage 6 | — | ~3–5k / Task implement | TRACKED |
| P1-A | Remove ghost composite-first branches (5 skills) | TRACKED | mcp-lifecycle-tools Stage 6 T6.2 | Stage 7 ships | — | cleanup only | TRACKED |
| NEW-1 | Thread CHAIN_CONTEXT ship-stage → opus-audit (skip domain-context-load at Stage-end) | SKILL-EDIT | `ia/skills/opus-audit/SKILL.md` Phase 1 guard | None | Low | ~4k / 4-Task chain | READY |
| NEW-2 | Split Changelog → sibling `CHANGELOG.md` per skill | NEW-STAGE | mcp-lifecycle-tools Stage 7b (2 tasks: bulk move + skill-train tooling update) | None | Medium | 500–2500+ / invocation | READY |
| NEW-3 | project-new: dep verify → `backlog_list` batch | SKILL-EDIT | `ia/skills/project-new/SKILL.md` Phase 2 | None | Very Low | ~1.5k / multi-dep Task | READY |
| NEW-4 | Parallelize domain-context-load (3 concurrent batches) | SKILL-EDIT | `ia/skills/domain-context-load/SKILL.md` | None | Medium | ~1.5k latency | SKIP |
| NEW-5 | Build `next_stage_resolver` MCP tool (4-case dedup from ship-stage + closeout-apply) | NEW-TASK | mcp-lifecycle-tools Stage 7b OR standalone | None | Low | 0 (correctness) | READY |
| NEW-6 | Build `ship_stage_bundle` MCP tool (pending tasks + stage meta + CHAIN_CONTEXT projection) | NEW-TASK | mcp-lifecycle-tools Stage 7b | P0-A | Low | ~5k / chain open | BLOCKED |
| NEW-7 | Build `plan_author_bundle` MCP tool (stage header + task_spec_stubs + glossary + domain context) | NEW-TASK | mcp-lifecycle-tools Stage 7b | P0-A | Low-Med | ~4k / plan-author run | BLOCKED |
| NEW-8 | Build `stage_file_plan_bundle` MCP tool (pending tasks + dep union + router + invariants) | NEW-TASK | mcp-lifecycle-tools Stage 7b | P0-A | Low | ~3k / stage-file-plan run | BLOCKED |
| NEW-9 | Build `opus_audit_bundle` MCP tool + update opus-audit SKILL.md | NEW-STAGE | mcp-lifecycle-tools Stage 7b (2 tasks: tool impl + SKILL.md update) | P0-A | Medium | ~8k / Stage audit | BLOCKED |
| NEW-10 | Build `closeout_plan_bundle` MCP tool (expanded task_closeout_data + glossary_candidates + rules_in_scope) | NEW-TASK | mcp-lifecycle-tools Stage 7b | P0-A | Medium | ~5–10k / closeout run | BLOCKED |
| NEW-11 | Amend `implement_bundle` (T7.1 spec) — add optional `chain_context` passthrough | SKILL-EDIT | Amend mcp-lifecycle-tools Stage 7 T7.1 spec before ship | P0-B | Low | ~3k × N Tasks | BLOCKED |
| NEW-12 | Fold `/author` + `/plan-review` into `/ship` N=1 chain | NEW-TASK | Standalone OR lifecycle-refactor Stage 11 | None | Low | ~10k / N=1 ship | READY |

**Stage 7b sizing gate:** NEW-2 (2) + NEW-5 (1) + NEW-6 (1) + NEW-7 (1) + NEW-8 (1) + NEW-9 (2) + NEW-10 (1) = 9 tasks. Exceeds soft limit (≤8). At filing time split → Stage 7b.A (NEW-2/5/6/7/8, 6 tasks) + Stage 7b.B (NEW-9/10, 3 tasks). Recheck at `/stage-decompose`.

**SKIP rationale — NEW-4:** Once Stage 7 ships and sweep completes, all chain skills call a per-skill bundle (server parallelizes internally). NEW-4 only helps standalone `domain-context-load` invocations — low-volume, not worth a TECH issue.

---

## Background

### Ghost Composite Tools

Two composite MCP tools are referenced by skills under a "Composite-first call (MCP available)" branch but **do not exist** in `tools/mcp-ia-server/src/tools/`:

| Ghost tool | Referenced in |
|---|---|
| `lifecycle_stage_context` | plan-author, stage-file-plan, stage-closeout-plan, stage-closeout-apply |
| `issue_context_bundle` | project-spec-implement |

Every invocation silently falls through to the 5-call `domain-context-load` bash fallback: `glossary_discover → glossary_lookup → router_for_task → spec_sections → invariants_summary`. Stage 7 is the unblocking gate — once it lands, all 5+ swept skills activate their primary context path with zero further skill edits.

### 4-Task Ship-Stage: Current vs. After Bundles

| Category | Current calls | After bundles |
|---|---|---|
| domain-context-load × 3 (ship-stage open + opus-audit + closeout-plan) | 15 | 0 — replaced by ship_stage_bundle + opus_audit_bundle + closeout_plan_bundle |
| issue_context_bundle ghost→fallback × 4 Tasks | 20 | 4 — implement_bundle with chain_context passthrough |
| opus_audit_bundle N file reads × 4 Tasks | 4 (Read, not MCP) | 0 — folded into opus_audit_bundle task_audit_secs |
| verify/bridge preflight | 3 | 3 (unchanged) |
| backlog_issue per dep (plan-author, closeout) | 2–6 | 0–1 (backlog_list batch) |
| rule_content per cited rule (closeout) | 2–4 | 0 — folded into closeout_plan_bundle rules_in_scope |
| **Total** | **~46–52** | **~8–10** |

---

## Implementation Reference

### Shared Internal Aggregator

All per-skill bundle tools (NEW-6–10) are thin field-projection wrappers on a single aggregator. File: `tools/mcp-ia-server/src/tools/internal/stage-aggregator.ts`.

```typescript
export interface StageAggregatorInput {
  master_plan_path: string;
  stage_id: string;
  filter_status: "all" | "pending" | "non-done";
  fields: AggregatorField[];
  chain_context?: ChainContext;  // skip re-fetch when ship-stage passes context
}

export type AggregatorField =
  | "stage_header"        // objectives + exit_criteria + raw task rows
  | "task_rows"           // filtered task list (title, intent, priority, depends_on, issue_id)
  | "task_spec_stubs"     // full spec bodies per task (§1/§2/§4/§5/§7/§8)
  | "task_audit_secs"     // §Impl + §Findings + §Verification per task
  | "task_closeout_secs"  // §Audit + §Lessons + §Issues + §Acceptance per task
  | "glossary_anchors"    // existing canonical terms from glossary.md
  | "glossary_candidates" // new/candidate terms mined from task spec content
  | "router_domains"      // domain routing table from router_for_task
  | "spec_sections"       // spec section excerpts for routed domains
  | "invariants"          // invariant guardrails (filtered by domain keywords)
  | "pair_contract_slice" // plan-apply-pair-contract.md excerpt
  | "rules_in_scope"      // rule bodies cited in §Audit paragraphs
  | "dep_ids_union"       // union of all Depends-on ids across pending tasks
  | "files_touched"       // file paths extracted from §7 deliverable bullets
  | "template_sections"   // section headings from project-spec-template.md
  | "retired_surfaces"    // basenames from ia/skills/_retired/, agents/_retired/, commands/_retired/
  | "tooling_only_flag"   // true when stage touches only tooling paths
  | "chain_context";      // {glossary_anchors, router_domains, spec_sections, invariants}
```

### Sub-fetch Parallelization

```
Batch 1 — parallel:
  A: readFile(master_plan_path)     → stageBlock, taskRows, toolingOnlyFlag
  B: invariants_summary(keywords)   → invariants
  C: glossary_discover()            → termList

Batch 2 — depends on A (parallel where possible):
  D: Promise.all(taskRows.map(readTaskSpec))  → spec bodies (N reads)      [after A]
  E: glossary_lookup(termList)               → glossary_anchors             [after C]
  F: router_for_task(keywords).then(fetch)   → router_domains + spec_sections [after A]

Batch 3 — depends on D (parallel):
  G: computeGlossaryCandidates(D, C)  → glossary_candidates
  H: computeDepUnion(taskRows)        → dep_ids_union
  I: extractFilesTouched(D)           → files_touched
  J: loadRulesInScope(D)              → rules_in_scope  [only when task_closeout_secs requested]

Sync (fast, no I/O):
  K: readDirSync(retired paths × 3)  → retired_surfaces
  L: readTemplateHeadings()          → template_sections
```

Wall-time: Batch 1 ~500ms + Batch 2 ~600ms + Batch 3 ~200ms = **~1.3s** (vs 5-call sequential: ~2.5s).

Field projection: aggregator runs only batches whose requested `fields[]` require. Example: `opus_audit_bundle` requests `["task_audit_secs", "glossary_anchors", "invariants", "glossary_candidates"]` → only Batches 1A/1B/1C + 2D/2E + 3G. Skips router_for_task and spec_sections.

### Cache Strategy

```typescript
const CACHE_TTL_MS = 30 * 60 * 1000; // 30 min
// Key includes master plan mtime to invalidate on edits
const cacheKey = (input: StageAggregatorInput): string => {
  const mtime = fs.statSync(input.master_plan_path).mtimeMs;
  return `${input.master_plan_path}::${input.stage_id}::${mtime}`;
};
// Cache stores full output (superset). Per-skill projection applied after cache hit.
// cache_bust: true input param forces refresh (e.g. after closeout deletes a spec).
```

### chain_context Passthrough

When `chain_context` is provided (ship-stage passes it after chain open), the aggregator short-circuits:
- Skips Batch 1B (`invariants_summary`) → uses `chain_context.invariants`
- Skips Batch 2E+2F (`glossary_lookup` + `router_for_task`) → uses `chain_context.*`
- Result: `implement_bundle` × 4 Tasks with chain_context → only 4 task-spec reads (vs 20 MCP calls via current 4 × 5-call fallback).

---

### Per-Skill Output Schemas

Corrected schemas for each bundle tool (include fields found missing in original §8.2 analysis).

**`ship_stage_bundle`**
```json
{
  "pending_tasks": [{ "issue_id", "title", "status", "phase" }],
  "all_task_ids": ["TECH-501", "TECH-502"],
  "stage_objectives": "...",
  "exit_criteria": "...",
  "tooling_only": false,
  "compile_gate_required": true,
  "chain_context": {
    "glossary_anchors": [...],
    "router_domains": [...],
    "spec_sections": [...],
    "invariants": [...]
  }
}
```

**`plan_author_bundle`**
```json
{
  "stage_header": { "objectives": "...", "exit_criteria": "..." },
  "tasks": [{ "issue_id", "title", "intent", "depends_on", "priority" }],
  "task_spec_stubs": [{ "issue_id", "summary", "goals", "current_state", "proposed_design", "impl_plan", "acceptance" }],
  "glossary_anchors": [...],
  "router_domains": [...],
  "spec_sections": [...],
  "invariants": [...],
  "pair_contract_slice": "...",
  "template_sections": ["§1 Summary", "§2.1 Goals", "..."],
  "retired_surfaces": ["spec-kickoff", "project-stage-close", "..."]
}
```

**`stage_file_plan_bundle`**
```json
{
  "pending_tasks": [{ "task_key", "title", "intent", "priority", "depends_on" }],
  "dep_ids_union": ["TECH-450", "TECH-460"],
  "stage_objectives": "...",
  "router_domains": [...],
  "spec_sections": [...],
  "invariants": [...]
}
```

**`opus_audit_bundle`**
```json
{
  "task_reads": [{ "issue_id", "impl_plan", "findings", "verification" }],
  "glossary_anchors": [...],
  "glossary_terms_introduced": ["term-a", "term-b"],
  "invariants": [...]
}
```
Accepts optional `chain_context: { glossary_anchors, invariants }` — skips those sub-fetches when passed from ship-stage.

**`closeout_plan_bundle`**
```json
{
  "task_closeout_data": [
    {
      "issue_id": "...",
      "audit_paragraph": "...",
      "verification_summary": "...",
      "lessons_learned": "...",
      "issues_found": "...",
      "acceptance_state": ["- [x] ...", "..."],
      "files_touched": ["path/to/file.ts"]
    }
  ],
  "glossary_candidates": [{ "term", "current_row", "proposed_row", "cited_in": ["TECH-501"] }],
  "glossary_anchors": [...],
  "invariants": [...],
  "rules_in_scope": [{ "name", "path", "excerpt" }]
}
```

---

### TypeScript Stubs

All tools import `runAggregator` from the shared internal aggregator.

**`ship_stage_bundle`**
```typescript
// tools/mcp-ia-server/src/tools/ship-stage-bundle.ts
import { z } from "zod";
import type { ToolDef } from "../types";
import { runAggregator } from "./internal/stage-aggregator";

export const shipStageBundleTool: ToolDef = {
  name: "ship_stage_bundle",
  description: `Load ship-stage chain-open context in one call. Replaces Step 0 master-plan parse +
    Step 1 domain-context-load. Returns pending task list, stage meta, and pre-built CHAIN_CONTEXT
    (glossary/router/invariants) for passing to inner dispatches.
    Use at ship-stage Step 0; pass returned chain_context to every per-task implement_bundle call.`,
  inputSchema: z.object({
    master_plan_path: z.string().describe("Repo-relative path to *-master-plan.md"),
    stage_id: z.string().describe("Stage identifier, e.g. '7.2' or 'Stage 7.2'"),
  }),
  async handler({ master_plan_path, stage_id }) {
    const agg = await runAggregator({
      master_plan_path, stage_id,
      filter_status: "non-done",
      fields: ["stage_header", "task_rows", "tooling_only_flag", "chain_context"],
    });
    return {
      pending_tasks: agg.task_rows,
      all_task_ids: agg.task_rows?.map(t => t.issue_id) ?? [],
      stage_objectives: agg.stage_header?.objectives ?? "",
      exit_criteria: agg.stage_header?.exit_criteria ?? "",
      tooling_only: agg.tooling_only_flag ?? false,
      compile_gate_required: !(agg.tooling_only_flag ?? false),
      chain_context: agg.chain_context,
    };
  },
};
```

**`plan_author_bundle`**
```typescript
// tools/mcp-ia-server/src/tools/plan-author-bundle.ts
export const planAuthorBundleTool: ToolDef = {
  name: "plan_author_bundle",
  description: `Load plan-author Stage context in one call. Replaces Phase 1 lifecycle_stage_context
    ghost (domain-context-load fallback) + rule_content calls + retired-surface dir reads.
    Use at plan-author Phase 1.`,
  inputSchema: z.object({ master_plan_path: z.string(), stage_id: z.string() }),
  async handler({ master_plan_path, stage_id }) {
    const agg = await runAggregator({
      master_plan_path, stage_id, filter_status: "all",
      fields: [
        "stage_header", "task_rows", "task_spec_stubs",
        "glossary_anchors", "router_domains", "spec_sections", "invariants",
        "pair_contract_slice", "template_sections", "retired_surfaces",
      ],
    });
    return {
      stage_header: agg.stage_header,
      tasks: agg.task_rows,
      task_spec_stubs: agg.task_spec_stubs,
      glossary_anchors: agg.glossary_anchors,
      router_domains: agg.router_domains,
      spec_sections: agg.spec_sections,
      invariants: agg.invariants,
      pair_contract_slice: agg.pair_contract_slice,
      template_sections: agg.template_sections,
      retired_surfaces: agg.retired_surfaces,
    };
  },
};
```

**`stage_file_plan_bundle`**
```typescript
// tools/mcp-ia-server/src/tools/stage-file-plan-bundle.ts
export const stageFilePlanBundleTool: ToolDef = {
  name: "stage_file_plan_bundle",
  description: `Load stage-file-plan Stage context in one call. Replaces Phase 0 lifecycle_stage_context
    ghost (domain-context-load fallback). dep_ids_union pre-computed server-side — still call
    backlog_list once for dep verification. Use at stage-file-plan Phase 0.`,
  inputSchema: z.object({ master_plan_path: z.string(), stage_id: z.string() }),
  async handler({ master_plan_path, stage_id }) {
    const agg = await runAggregator({
      master_plan_path, stage_id, filter_status: "pending",
      fields: ["stage_header", "task_rows", "dep_ids_union", "router_domains", "spec_sections", "invariants"],
    });
    return {
      pending_tasks: agg.task_rows,
      dep_ids_union: agg.dep_ids_union,
      stage_objectives: agg.stage_header?.objectives ?? "",
      exit_criteria: agg.stage_header?.exit_criteria ?? "",
      router_domains: agg.router_domains,
      spec_sections: agg.spec_sections,
      invariants: agg.invariants,
    };
  },
};
```

**`opus_audit_bundle`**
```typescript
// tools/mcp-ia-server/src/tools/opus-audit-bundle.ts
export const opusAuditBundleTool: ToolDef = {
  name: "opus_audit_bundle",
  description: `Load opus-audit Stage context in one call. Replaces Phase 1 domain-context-load
    (5 MCP calls) + Phase 2 per-Task file reads (N Read calls). Skips router_for_task + spec_sections
    (not used in audit synthesis). Use at opus-audit Phase 1; replaces both Phase 1 and Phase 2.`,
  inputSchema: z.object({
    master_plan_path: z.string(),
    stage_id: z.string(),
    chain_context: z.optional(z.object({
      glossary_anchors: z.array(z.any()),
      invariants: z.array(z.any()),
    })).describe("Pass ship-stage CHAIN_CONTEXT to skip re-fetch of glossary + invariants"),
  }),
  async handler({ master_plan_path, stage_id, chain_context }) {
    const agg = await runAggregator({
      master_plan_path, stage_id, filter_status: "non-done",
      fields: [
        "task_audit_secs",
        ...(chain_context ? [] : ["glossary_anchors", "invariants"] as const),
        "glossary_candidates",
      ],
      chain_context,
    });
    return {
      task_reads: agg.task_audit_secs,
      glossary_anchors: chain_context?.glossary_anchors ?? agg.glossary_anchors,
      glossary_terms_introduced: agg.glossary_candidates?.map(c => c.term) ?? [],
      invariants: chain_context?.invariants ?? agg.invariants,
    };
  },
};
```

**`closeout_plan_bundle`**
```typescript
// tools/mcp-ia-server/src/tools/closeout-plan-bundle.ts
export const closeoutPlanBundleTool: ToolDef = {
  name: "closeout_plan_bundle",
  description: `Load stage-closeout-plan Stage context in one call. Replaces Phase 1 lifecycle_stage_context
    ghost (5–10 calls) + per-rule rule_content calls. Pre-condition: every Task Status=Done and §Audit
    paragraph written by opus-audit. Use at stage-closeout-plan Phase 1.`,
  inputSchema: z.object({ master_plan_path: z.string(), stage_id: z.string() }),
  async handler({ master_plan_path, stage_id }) {
    const agg = await runAggregator({
      master_plan_path, stage_id, filter_status: "all",
      fields: [
        "task_closeout_secs", "files_touched",
        "glossary_candidates", "glossary_anchors",
        "invariants", "rules_in_scope",
      ],
    });
    return {
      task_closeout_data: agg.task_closeout_secs?.map((t, i) => ({
        ...t,
        files_touched: agg.files_touched?.[i] ?? [],
      })),
      glossary_candidates: agg.glossary_candidates,
      glossary_anchors: agg.glossary_anchors,
      invariants: agg.invariants,
      rules_in_scope: agg.rules_in_scope,
    };
  },
};
```

**`implement_bundle` — amendment to Stage 7 T7.1 spec**

Not a new tool. Amend `issue_context_bundle` spec before it ships:

```typescript
// Add to inputSchema of issue_context_bundle (T7.1):
chain_context: z.optional(z.object({
  stage_id: z.string(),
  invariants: z.array(z.any()),
  router_domains: z.array(z.any()),
})).describe(
  "When passed from ship-stage, skip invariants_summary + router_for_task re-fetch. " +
  "Use chain_context.invariants and chain_context.router_domains directly."
),
// Handler: if chain_context provided → skip those sub-fetches; still fetch issue-specific spec_sections.
```
