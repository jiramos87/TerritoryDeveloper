### Stage 9 — Composite Bundles + Graph Freshness / Graph Freshness + Skill Recipe Sweep


**Status:** Done

**Objectives:** Wire real freshness metadata into `glossary_lookup` / `glossary_discover` responses; add `refresh_graph` non-blocking regen trigger. Sweep lifecycle skill bodies and agent docs to call composite bundle tools first, with bash fallback for MCP-unavailable path.

**Exit:**

- `glossary_lookup` response includes `meta.graph_generated_at` (ISO from `glossary-graph-index.json` mtime) + `meta.graph_stale` (true when > `GLOSSARY_GRAPH_STALE_DAYS` days, default 14).
- `refresh_graph: true` spawns regen child process; response returns without waiting.
- All 8+ lifecycle skill tool-recipe sections updated; subagent bodies + `docs/mcp-ia-server.md` catalog updated with all 3 composite tools.
- `npm run validate:all` passes.
- Phase 1 — Graph freshness metadata.
- Phase 2 — Skill recipe + docs sweep.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T9.1 | Graph freshness handler | **TECH-514** | Done (archived) | Extend `glossary-lookup.ts` + `glossary-discover.ts`: read `fs.stat("tools/mcp-ia-server/data/glossary-graph-index.json").mtime`; compute `graph_stale = mtime < Date.now() - (GLOSSARY_GRAPH_STALE_DAYS * 86400000)` (env default 14); attach to `EnvelopeMeta`. `refresh_graph?: boolean` input: if `true`, spawn `npm run build:glossary-graph` as detached child process (`child_process.spawn(..., { detached: true, stdio: "ignore" }).unref()`), return immediately. |
| T9.2 | Freshness tests | **TECH-515** | Done (archived) | Tests: mock `fs.stat` mtime = now - 15d → `graph_stale: true`; mtime = now - 1d → `graph_stale: false`; `GLOSSARY_GRAPH_STALE_DAYS=1` env override → stale threshold respected; `refresh_graph: true` → child process spawned without blocking (spy on `child_process.spawn`); `graph_generated_at` ISO format valid. |
| T9.3 | Skill recipe sweep | **TECH-516** | Done (archived) | Update `ia/skills/design-explore/SKILL.md`, `ia/skills/master-plan-new/SKILL.md`, `ia/skills/stage-file-plan/SKILL.md`, `ia/skills/stage-file-apply/SKILL.md`, `ia/skills/plan-author/SKILL.md`, `ia/skills/project-spec-implement/SKILL.md`, `ia/skills/stage-closeout-plan/SKILL.md`, `ia/skills/stage-closeout-apply/SKILL.md`, `ia/skills/release-rollout/SKILL.md` — replace 3–8 call opening sequence with `lifecycle_stage_context(master_plan_path, stage_id)` or `issue_context_bundle(issue_id)` as first call; add bash-fallback note for MCP-unavailable. |
| T9.4 | Agent + docs catalog update | **TECH-517** | Done (archived) | Update `.claude/agents/*.md` subagent bodies referencing old sequential recipe (same grep pattern as T9.3); update `docs/mcp-ia-server.md` MCP catalog: add `issue_context_bundle`, `lifecycle_stage_context`, `orchestrator_snapshot`, `rule_section` tool entries; mark `glossary_lookup` bulk-terms + freshness metadata; mark `spec_section` alias-drop migration. Gates Stage 17 T17.3 (TECH-497 README drift lint). |

#### §Stage File Plan

<!-- stage-file-plan output — do not hand-edit; apply via stage-file-apply -->

```yaml
- operation: file_task
  task_key: T9.1
  reserved_id: TECH-514
  title: "Graph freshness handler"
  priority: medium
  issue_type: TECH
  notes: |
    Extend `tools/mcp-ia-server/src/tools/glossary-lookup.ts` + `glossary-discover.ts` — read `fs.stat("tools/mcp-ia-server/data/glossary-graph-index.json").mtime`; compute `graph_stale = mtime < Date.now() - (GLOSSARY_GRAPH_STALE_DAYS * 86400000)` (env default 14); attach to `EnvelopeMeta`. `refresh_graph?: boolean` input — `true` spawns `npm run build:glossary-graph` detached child via `child_process.spawn(..., { detached: true, stdio: "ignore" }).unref()`; response returns immediately without waiting.
  depends_on: []
  related:
    - TECH-515
    - TECH-516
    - TECH-517
  stub_body:
    summary: |
      Wire real freshness metadata into `glossary_lookup` + `glossary_discover` responses. `meta.graph_generated_at` + `meta.graph_stale` computed from graph-index mtime vs `GLOSSARY_GRAPH_STALE_DAYS` env (default 14). `refresh_graph: true` input spawns non-blocking regen child process.
    goals: |
      - `meta.graph_generated_at` ISO string from `glossary-graph-index.json` mtime on every `glossary_lookup` + `glossary_discover` response.
      - `meta.graph_stale` boolean — true when mtime older than `GLOSSARY_GRAPH_STALE_DAYS` days (default 14, env-overridable).
      - `refresh_graph?: boolean` input — `true` spawns detached `npm run build:glossary-graph` via `child_process.spawn(...).unref()`; tool returns without waiting.
      - `EnvelopeMeta` typings updated to carry the two new fields (Stage 3 foundation already exports `graph_generated_at?` / `graph_stale?`).
    systems_map: |
      - `tools/mcp-ia-server/src/tools/glossary-lookup.ts` (freshness + refresh_graph wiring)
      - `tools/mcp-ia-server/src/tools/glossary-discover.ts` (freshness wiring)
      - `tools/mcp-ia-server/data/glossary-graph-index.json` (mtime source)
      - `tools/mcp-ia-server/src/envelope.ts` (`EnvelopeMeta` — fields already reserved)
      - `GLOSSARY_GRAPH_STALE_DAYS` env var (default 14)
    impl_plan_sketch: |
      Phase 1 — Author `fs.stat` helper returning `{ mtime, stale }`; wire into both tool handlers inside `wrapTool` body; plumb `graph_generated_at` + `graph_stale` through `EnvelopeMeta`. Add `refresh_graph` Zod field (default false); when true, spawn detached regen child, return immediately. Confirm `tools/mcp-ia-server/package.json` has `build:glossary-graph` script (exists from prior stages).

- operation: file_task
  task_key: T9.2
  reserved_id: TECH-515
  title: "Freshness tests"
  priority: medium
  issue_type: TECH
  notes: |
    Unit tests in `tools/mcp-ia-server/tests/tools/glossary-lookup.test.ts` + `glossary-discover.test.ts`: mock `fs.stat` mtime = now - 15d → `graph_stale: true`; mtime = now - 1d → `graph_stale: false`; `GLOSSARY_GRAPH_STALE_DAYS=1` env override → stale threshold respected; `refresh_graph: true` spawns child without blocking (spy on `child_process.spawn`); `graph_generated_at` ISO format valid.
  depends_on:
    - TECH-514
  related:
    - TECH-516
    - TECH-517
  stub_body:
    summary: |
      Behavioral + env-override tests for T9.1 freshness handler. Covers stale/fresh thresholds, env override, detached-spawn non-blocking semantics, ISO format validity.
    goals: |
      - Mock mtime = now - 15d → `graph_stale: true`; mtime = now - 1d → `graph_stale: false`.
      - `GLOSSARY_GRAPH_STALE_DAYS=1` env override respected (mtime = now - 2d → stale).
      - `refresh_graph: true` → `child_process.spawn` spy called once with detached + unref; tool response returns before child exits.
      - `graph_generated_at` parses as valid ISO 8601.
    systems_map: |
      - `tools/mcp-ia-server/tests/tools/glossary-lookup.test.ts`
      - `tools/mcp-ia-server/tests/tools/glossary-discover.test.ts`
      - Vitest spies on `fs.stat` + `child_process.spawn`
      - `GLOSSARY_GRAPH_STALE_DAYS` env var scope
    impl_plan_sketch: |
      Phase 1 — Add test file or extend existing; stub `fs.promises.stat` with fixed `mtime` Date values; assert `meta.graph_stale` branches. Add env-override test block (set/restore env). Spy on `child_process.spawn` for refresh_graph path; assert no blocking await. Confirm `validate:all` green.

- operation: file_task
  task_key: T9.3
  reserved_id: TECH-516
  title: "Skill recipe sweep"
  priority: medium
  issue_type: TECH
  notes: |
    Update `ia/skills/design-explore/SKILL.md`, `ia/skills/master-plan-new/SKILL.md`, `ia/skills/stage-file-plan/SKILL.md`, `ia/skills/stage-file-apply/SKILL.md`, `ia/skills/plan-author/SKILL.md`, `ia/skills/project-spec-implement/SKILL.md`, `ia/skills/stage-closeout-plan/SKILL.md`, `ia/skills/stage-closeout-apply/SKILL.md`, `ia/skills/release-rollout/SKILL.md` — replace 3–8 call opening sequence with `lifecycle_stage_context(issue_id, stage)` or `issue_context_bundle(issue_id)` as first call; add bash-fallback note for MCP-unavailable path. Composite-first pattern.
  depends_on: []
  related:
    - TECH-514
    - TECH-515
    - TECH-517
  stub_body:
    summary: |
      Sweep lifecycle skill bodies to call composite bundle tools (`issue_context_bundle` / `lifecycle_stage_context`) as first MCP call instead of 3–8 bare-tool opening sequence. Adds bash-fallback note per skill for MCP-unavailable path.
    goals: |
      - Replace opening 3–8 call sequence with one composite call in every listed skill body.
      - Preserve existing tool ordering in a "fallback (MCP unavailable)" sub-section.
      - Reference canonical param names only — no legacy aliases (Stage 5 already dropped; this is enforcement).
      - Update any skill referencing retired sequential patterns (design-explore, master-plan-new, stage-file pair, plan-author, project-spec-implement, stage-closeout pair, release-rollout).
    systems_map: |
      - `ia/skills/design-explore/SKILL.md`
      - `ia/skills/master-plan-new/SKILL.md`
      - `ia/skills/stage-file-plan/SKILL.md` + `ia/skills/stage-file-apply/SKILL.md`
      - `ia/skills/plan-author/SKILL.md`
      - `ia/skills/project-spec-implement/SKILL.md`
      - `ia/skills/stage-closeout-plan/SKILL.md` + `ia/skills/stage-closeout-apply/SKILL.md`
      - `ia/skills/release-rollout/SKILL.md`
    impl_plan_sketch: |
      Phase 1 — Grep each skill body for bare-tool opening sequences; replace with `lifecycle_stage_context` / `issue_context_bundle` first-call block; move old sequence under `### Bash fallback (MCP unavailable)` heading. Preserve caveman preamble + phases frontmatter. Run `npm run validate:frontmatter` + `validate:all` green.

- operation: file_task
  task_key: T9.4
  reserved_id: TECH-517
  title: "Agent + docs catalog update"
  priority: medium
  issue_type: TECH
  notes: |
    Update `.claude/agents/*.md` subagent bodies referencing old sequential recipe (same grep pattern as T9.3); update `docs/mcp-ia-server.md` MCP catalog — add `issue_context_bundle`, `lifecycle_stage_context`, `orchestrator_snapshot`, `rule_section` tool entries; mark `glossary_lookup` bulk-terms + freshness metadata; mark `spec_section` alias-drop migration. **Gates Stage 17 T17.3 (TECH-497 README drift lint)** — lint must not land until catalog rewrite merges.
  depends_on: []
  related:
    - TECH-497
    - TECH-514
    - TECH-515
    - TECH-516
  stub_body:
    summary: |
      Sweep subagent bodies + rewrite `docs/mcp-ia-server.md` catalog to reflect Stages 1–8 surface changes. Adds composite-tool + `rule_section` entries; marks bulk-terms + freshness metadata on glossary; marks alias-drop migration on spec tools. Gates Stage 17 T17.3 README drift lint.
    goals: |
      - `.claude/agents/*.md` — grep + replace old sequential recipes w/ composite first-call (same surface set as T9.3).
      - `docs/mcp-ia-server.md` — add catalog entries for `issue_context_bundle`, `lifecycle_stage_context`, `orchestrator_snapshot`, `rule_section`.
      - Mark `glossary_lookup` with bulk-`terms` partial-result shape + freshness metadata fields.
      - Mark `spec_section` / `spec_sections` alias-drop migration note (canonical params only).
      - Confirm Stage 17 T17.3 unblocks post-merge.
    systems_map: |
      - `.claude/agents/*.md` (all subagent bodies w/ legacy recipes)
      - `docs/mcp-ia-server.md` (tool catalog)
      - Stage 17 T17.3 gate (TECH-497 README drift lint unblocks after this lands)
    impl_plan_sketch: |
      Phase 1 — Grep `.claude/agents/*.md` for legacy sequential recipes (same regex as T9.3); rewrite to composite-first. Rewrite `docs/mcp-ia-server.md` tool catalog — add 4 new tool entries + 2 migration notes + 1 bulk-shape annotation. Cross-check `registerTool(` count in `src/index.ts` matches README row count (advisory — T17.3 CI lint formalizes post-merge). Confirm `validate:all` green.
```

#### §Plan Fix

<!-- plan-review output — do not hand-edit; apply via plan-fix-apply -->

```yaml
- operation: replace_line
  target_path: ia/projects/mcp-lifecycle-tools-opus-4-7-audit-master-plan.md
  target_anchor: "| T9.3 | Skill recipe sweep | **TECH-516** | Draft | Update `ia/skills/design-explore/SKILL.md`, `ia/skills/master-plan-new/SKILL.md`, `ia/skills/stage-file/SKILL.md`, `ia/skills/project-spec-kickoff/SKILL.md`, `ia/skills/project-spec-implement/SKILL.md`, `ia/skills/project-spec-close/SKILL.md`, `ia/skills/release-rollout/SKILL.md`, `ia/skills/closeout/SKILL.md` — replace 3–8 call opening sequence with `lifecycle_stage_context(issue_id, stage)` or `issue_context_bundle(issue_id)` as first call; add bash-fallback note for MCP-unavailable. |"
  payload: |
    | T9.3 | Skill recipe sweep | **TECH-516** | Draft | Update `ia/skills/design-explore/SKILL.md`, `ia/skills/master-plan-new/SKILL.md`, `ia/skills/stage-file-plan/SKILL.md`, `ia/skills/stage-file-apply/SKILL.md`, `ia/skills/plan-author/SKILL.md`, `ia/skills/project-spec-implement/SKILL.md`, `ia/skills/stage-closeout-plan/SKILL.md`, `ia/skills/stage-closeout-apply/SKILL.md`, `ia/skills/release-rollout/SKILL.md` — replace 3–8 call opening sequence with `lifecycle_stage_context(master_plan_path, stage_id)` or `issue_context_bundle(issue_id)` as first call; add bash-fallback note for MCP-unavailable. |
  rationale: |
    Retired-surface drift in Stage 9 T9.3 Intent cell. Cell listed 4 retired skill paths (`ia/skills/stage-file/SKILL.md`, `ia/skills/project-spec-kickoff/SKILL.md`, `ia/skills/project-spec-close/SKILL.md`, `ia/skills/closeout/SKILL.md`) — all folded or split under M6 collapse (CLAUDE.md §3 + ia/rules/agent-lifecycle.md). Replace with canonical 9-skill live set from TECH-516 §Acceptance (`stage-file-plan`, `stage-file-apply`, `plan-author`, `project-spec-implement`, `stage-closeout-plan`, `stage-closeout-apply` etc.). Also fix arg signature `lifecycle_stage_context(issue_id, stage)` → canonical `(master_plan_path, stage_id)` per TECH-516 skill-map table + TECH-517 catalog entry.

- operation: replace_line
  target_path: ia/projects/mcp-lifecycle-tools-opus-4-7-audit-master-plan.md
  target_anchor: "| T9.4 | Agent + docs catalog update | **TECH-517** | Draft | Update `.claude/agents/*.md` subagent bodies referencing old sequential recipe (same grep pattern as T2.4.2); update `docs/mcp-ia-server.md` MCP catalog: add `issue_context_bundle`, `lifecycle_stage_context`, `orchestrator_snapshot`, `rule_section` tool entries; mark `glossary_lookup` bulk-terms + freshness metadata; mark `spec_section` alias-drop migration. |"
  payload: |
    | T9.4 | Agent + docs catalog update | **TECH-517** | Draft | Update `.claude/agents/*.md` subagent bodies referencing old sequential recipe (same grep pattern as T9.3); update `docs/mcp-ia-server.md` MCP catalog: add `issue_context_bundle`, `lifecycle_stage_context`, `orchestrator_snapshot`, `rule_section` tool entries; mark `glossary_lookup` bulk-terms + freshness metadata; mark `spec_section` alias-drop migration. Gates Stage 17 T17.3 (TECH-497 README drift lint). |
  rationale: |
    Stale task-ref `T2.4.2` in Stage 9 T9.4 Intent cell points at pre-M6 step/stage decomposition numbering (no longer exists in current flat T9.x scheme). Sibling Stage 9 task T9.3 owns the same grep pattern. Replace T2.4.2 → T9.3. Also surface the T17.3 gate annotation (already in stub_body notes) into the Intent cell for lifecycle visibility.

- operation: replace_block
  target_path: ia/backlog-archive/TECH-517.yaml
  target_anchor: "- Risk: subagent body grep pattern drift — earlier stage <!-- WARN: stale task-ref T2.4.2 — verify against ia/projects/mcp-lifecycle-tools-opus-4-7-audit-master-plan.md (pre-Step/Stage collapse legacy format) --> used `grep -rn \"router_for_task\\|spec_section\\|glossary_lookup\\|invariants_summary\" .claude/agents/`. Mitigation: re-run same grep pre-sweep; archive match list in `ia/projects/TECH-517-subagent-grep-snapshot.txt` (optional) for audit."
  payload: |
    - Risk: subagent body grep pattern drift — sibling Stage 9 task T9.3 (TECH-516) runs the same grep (`grep -rn "router_for_task\|spec_section\|glossary_lookup\|invariants_summary" .claude/agents/`) against skill bodies. Mitigation: re-run identical grep pre-sweep against live agent surface; archive match list in `ia/projects/TECH-517-subagent-grep-snapshot.txt` (optional) for audit.
  rationale: |
    Plan-author flagged stale T2.4.2 ref in TECH-517 §Audit Notes via HTML WARN comment. T2.4.2 = pre-collapse decomposition numbering; current master-plan uses flat T9.x. Rewrite sentence to cite sibling T9.3 (TECH-516), drop WARN comment.
```

#### §Stage Audit

> Opus `opus-audit` writes one `§Audit` paragraph per Task row here (Stage-scoped bulk, non-pair) once every Task reaches Done post-verify. Feeds `§Stage Closeout Plan` migration tuples downstream. Contract: `ia/rules/plan-apply-pair-contract.md` Stage-scoped non-pair row.

_retroactive-skip — Stage archived prior to 2026-04-24 lifecycle refactor that introduced the canonical `§Stage Audit` subsection (see `ia/projects/MASTER-PLAN-STRUCTURE.md` §3.4 + Changelog entry 2026-04-24). Task-level §Audit prose captured in per-Task specs during Stage-scoped closeout before spec deletion; no retroactive re-run needed._

#### §Stage Closeout Plan

> stage-closeout-plan — 4 Tasks (0 shared migration ops + 16 per-Task ops + 1 stage-level status flip = 17 tuples total). Spawn `stage-closeout-apply ia/projects/mcp-lifecycle-tools-opus-4-7-audit-master-plan.md 9`.

```yaml
# Shared migration ops — none (no new glossary rows, no shared rule edits, no shared doc edits across Tasks).

# Per-Task ops — TECH-514 (T9.1 Graph freshness handler)
- operation: archive_record
  target_path: ia/backlog/TECH-514.yaml
  target_anchor: "id: \"TECH-514\""
  payload:
    status: closed
    completed: "2026-04-19"
    dest: ia/backlog-archive/TECH-514.yaml

- operation: delete_file
  target_path: ia/backlog-archive/TECH-514.yaml
  target_anchor: "file:TECH-514.md"
  payload: null

- operation: replace_section
  target_path: ia/projects/mcp-lifecycle-tools-opus-4-7-audit-master-plan.md
  target_anchor: "task_key:T9.1"
  payload: |
    | T9.1 | Graph freshness handler | **TECH-514** | Done (archived) | Extend `glossary-lookup.ts` + `glossary-discover.ts`: read `fs.stat("tools/mcp-ia-server/data/glossary-graph-index.json").mtime`; compute `graph_stale = mtime < Date.now() - (GLOSSARY_GRAPH_STALE_DAYS * 86400000)` (env default 14); attach to `EnvelopeMeta`. `refresh_graph?: boolean` input: if `true`, spawn `npm run build:glossary-graph` as detached child process (`child_process.spawn(..., { detached: true, stdio: "ignore" }).unref()`), return immediately. |

- operation: digest_emit
  target_path: ia/backlog-archive/TECH-514.yaml
  target_anchor: "TECH-514"
  payload:
    tool: stage_closeout_digest
    mode: per_task

# Per-Task ops — TECH-515 (T9.2 Freshness tests)
- operation: archive_record
  target_path: ia/backlog/TECH-515.yaml
  target_anchor: "id: \"TECH-515\""
  payload:
    status: closed
    completed: "2026-04-19"
    dest: ia/backlog-archive/TECH-515.yaml

- operation: delete_file
  target_path: ia/backlog-archive/TECH-515.yaml
  target_anchor: "file:TECH-515.md"
  payload: null

- operation: replace_section
  target_path: ia/projects/mcp-lifecycle-tools-opus-4-7-audit-master-plan.md
  target_anchor: "task_key:T9.2"
  payload: |
    | T9.2 | Freshness tests | **TECH-515** | Done (archived) | Tests: mock `fs.stat` mtime = now - 15d → `graph_stale: true`; mtime = now - 1d → `graph_stale: false`; `GLOSSARY_GRAPH_STALE_DAYS=1` env override → stale threshold respected; `refresh_graph: true` → child process spawned without blocking (spy on `child_process.spawn`); `graph_generated_at` ISO format valid. |

- operation: digest_emit
  target_path: ia/backlog-archive/TECH-515.yaml
  target_anchor: "TECH-515"
  payload:
    tool: stage_closeout_digest
    mode: per_task

# Per-Task ops — TECH-516 (T9.3 Skill recipe sweep)
- operation: archive_record
  target_path: ia/backlog/TECH-516.yaml
  target_anchor: "id: \"TECH-516\""
  payload:
    status: closed
    completed: "2026-04-19"
    dest: ia/backlog-archive/TECH-516.yaml

- operation: delete_file
  target_path: ia/backlog-archive/TECH-516.yaml
  target_anchor: "file:TECH-516.md"
  payload: null

- operation: replace_section
  target_path: ia/projects/mcp-lifecycle-tools-opus-4-7-audit-master-plan.md
  target_anchor: "task_key:T9.3"
  payload: |
    | T9.3 | Skill recipe sweep | **TECH-516** | Done (archived) | Update `ia/skills/design-explore/SKILL.md`, `ia/skills/master-plan-new/SKILL.md`, `ia/skills/stage-file-plan/SKILL.md`, `ia/skills/stage-file-apply/SKILL.md`, `ia/skills/plan-author/SKILL.md`, `ia/skills/project-spec-implement/SKILL.md`, `ia/skills/stage-closeout-plan/SKILL.md`, `ia/skills/stage-closeout-apply/SKILL.md`, `ia/skills/release-rollout/SKILL.md` — replace 3–8 call opening sequence with `lifecycle_stage_context(master_plan_path, stage_id)` or `issue_context_bundle(issue_id)` as first call; add bash-fallback note for MCP-unavailable. |

- operation: digest_emit
  target_path: ia/backlog-archive/TECH-516.yaml
  target_anchor: "TECH-516"
  payload:
    tool: stage_closeout_digest
    mode: per_task

# Per-Task ops — TECH-517 (T9.4 Agent + docs catalog update)
- operation: archive_record
  target_path: ia/backlog/TECH-517.yaml
  target_anchor: "id: \"TECH-517\""
  payload:
    status: closed
    completed: "2026-04-19"
    dest: ia/backlog-archive/TECH-517.yaml

- operation: delete_file
  target_path: ia/backlog-archive/TECH-517.yaml
  target_anchor: "file:TECH-517.md"
  payload: null

- operation: replace_section
  target_path: ia/projects/mcp-lifecycle-tools-opus-4-7-audit-master-plan.md
  target_anchor: "task_key:T9.4"
  payload: |
    | T9.4 | Agent + docs catalog update | **TECH-517** | Done (archived) | Update `.claude/agents/*.md` subagent bodies referencing old sequential recipe (same grep pattern as T9.3); update `docs/mcp-ia-server.md` MCP catalog: add `issue_context_bundle`, `lifecycle_stage_context`, `orchestrator_snapshot`, `rule_section` tool entries; mark `glossary_lookup` bulk-terms + freshness metadata; mark `spec_section` alias-drop migration. Gates Stage 17 T17.3 (TECH-497 README drift lint). |

- operation: digest_emit
  target_path: ia/backlog-archive/TECH-517.yaml
  target_anchor: "TECH-517"
  payload:
    tool: stage_closeout_digest
    mode: per_task

# Stage-level status flip (once all 4 tasks archived)
- operation: replace_section
  target_path: ia/projects/mcp-lifecycle-tools-opus-4-7-audit-master-plan.md
  target_anchor: "stage_status:9"
  payload: |
    **Status:** Done
```

---
