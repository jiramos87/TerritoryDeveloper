---
plan_slug: chain-token-cut
plan_title: Lifecycle chain token + speed reduction (15 light moves)
plan_shape: flat
parent_tag: null
stages:
  - id: 1
    name: trim-plan-size
    commit_prefix: chain-token-cut-stage-1
    tasks: [trim-plan-anchor-registry, trim-plan-drift-lint, trim-plan-digest-skeleton, trim-plan-mcp-cache, trim-plan-glossary-enrich]
  - id: 2
    name: mechanize-cycle
    commit_prefix: chain-token-cut-stage-2
    tasks: [mechanize-cycle-stage-cache, mechanize-cycle-marker-linter, mechanize-cycle-diff-classifier, mechanize-cycle-faceted-index, mechanize-cycle-topo-sort]
  - id: 3
    name: fix-exploration
    commit_prefix: chain-token-cut-stage-3
    tasks: [fix-exploration-status-cascade, fix-exploration-reverse-mining, fix-exploration-polling-templates, fix-exploration-review-skip, fix-exploration-arch-form]
core_prototype:
  verb: trim
  hardcoded_scope: chain design-explore→ship-plan→ship-cycle→ship-final on slug feature/asset-pipeline
  stubbed_systems: []
  throwaway: per-move feature flags (none — hard cut)
  forward_living: anchor registry, digest templates, polling templates, MCP cache, status trigger, drift lint
ship_plan_ready: true
---

# Lifecycle chain — light moves for token + speed reduction

**Date:** 2026-05-06
**Branch:** feature/asset-pipeline
**Status:** designed — ready for `/ship-plan`
**Companion docs:** `/tmp/roi-moves-2026-05-06.md`, `/tmp/requirements-layer-discussion-2026-05-06.md`

---

## Origin

Chain `design-explore → ship-plan → ship-cycle → ship-final` runs ~285k tokens + ~24–32 min per master plan today (rough model: N=10 tasks × 5 stages). 15 light moves (+ 5 medium/heavy) identified via ROI analysis that replace LLM heavy-lifting with scripts, SQL views/triggers, templates, caches.

Cumulative model: ~48% token cut, ~25–40% speed cut for the 15 light moves alone. Risk: 1–2 of 15 may regress quality.

## Goal

Implement the light moves (Phase 1 set) to lower token usage + speed up master-plan execution end-to-end. Single coordinated rollout, per-move pilot.

## Hard constraints (locked before exploration)

- **NO metrics tools** — no telemetry collection, no dashboards, no observability layer. Only direct improvements.
- **NO requirements layer in this scope** — heavy moves (#4, #6, #9) paused; tracked separately.
- **NO `arch_decisions` redesign** — reuse existing infra.
- **Reuse existing infra as-is** for stages, tasks, glossary, references, MCP slices.
- **Prototype-first methodology** — every move ships a Stage 1.0 tracer + Stages 2+ §Visibility Delta lines.
- **TDD red/green** — every move ships red-stage proof before green.
- **Rollback-safe per move** — migration revert or feature flag.

## Out of scope (deferred)

- Requirements layer DB tables (heavy — paused).
- Polymorphic verify dispatch (heavy — depends on requirements layer).
- YAML schema codegen lockstep (heavy — paused).
- Waiver registry, decision-log table (medium — coupled to requirements layer).
- Metrics / instrumentation / dashboards.

## Candidate moves — 15 light

### Group A — ship-plan token diet (5)

| # | Move | Surface | Token saved/plan |
|---|---|---|---|
| 1 | Anchor registry table (`ia_spec_anchors`) + ship-plan Phase 5 SQL JOIN | ship-plan | ~10k |
| 2 | Drift lint pure validator (anchor + glossary + retired-surface) | ship-plan Phase 6 | ~5k |
| 3 | Digest skeleton codegen (Markdown templates per section) | ship-plan Phase 5 | ~30k |
| 8 | Shared MCP context cache (router + glossary + invariants per `plan_id`) | All skills | ~10k |
| 16 | Glossary back-link auto-enrich (post-ship-plan script) | ship-plan | ~3k |

### Group B — ship-cycle Pass A/B mechanization (5)

| # | Move | Surface | Token saved/plan |
|---|---|---|---|
| 13 | Stage-bundle digest cache (hash by `stage_id`) | ship-cycle resume | ~6k |
| 14 | Boundary marker linter (regex pre-flip gate) | ship-cycle Pass A | failure-mode only |
| 15 | Diff anomaly classifier (regex over `git diff`) | ship-cycle Pass B | ~25k |
| 17 | Faceted index materialized view for next-stage resolver | ship-cycle handoff | ~10k |
| 18 | Critical-path topological sort (finish existing partial) | `master_plan_next_pending` | ~5k |

### Group C — design-explore polling cut (4)

| # | Move | Surface | Token saved/plan |
|---|---|---|---|
| 11 | Reverse-mining red-stage proofs from C# tests + BDD names | design-explore Phase 4 | ~5k |
| 12 | Polling state machine (pre-canned templates by `core_prototype.verb`) | design-explore Phase 1/2 | ~10k |
| 19 | Subagent review skip-gate (when YAML schema clean) | design-explore Phase 8 | ~8k (50% trigger) |
| 20 | Architecture decision form-fill (4 turns → 1) | design-explore Phase 4 | ~5k |

### Group D — status auto-cascade (1)

| # | Move | Surface | Token saved/plan |
|---|---|---|---|
| 10 | Status cascade SQL trigger (child-all-done → parent auto-flip) | `stage_closeout_apply` | ~5k |

## Open decisions for design-explore polling

1. **Scope** — all 15 light moves, or top-5 subset (#3 + #1 + #12 + #15 + #8 = ~70% of 48% gain)?
2. **Rollout cadence** — one-by-one, batch by group (A→B→C→D), or all at once?
3. **Cache invalidation strategy** — TTL? content-hash gate? combined? for #8 + #13.
4. **Digest skeleton template registry** — `ia/templates/digest-sections/` as Markdown files? DB table? for #3.
5. **Polling template registry** — `ia/templates/polling/{verb}.json`? DB table? for #12 + #20.
6. **Pilot vs production** — feature-flag per move, or hard-cut after sample audit?
7. **Rollback budget** — 1 move? 2? 3?
8. **Quality-audit cadence** — replay last N master plans through new chain? sample N tasks per plan?
9. **Validator gate placement** — pre-commit hook? `validate:all`? both? for #2 + #14 + #15.
10. **Migration vs in-place** — new tables (#1) — separate migration per move, or umbrella migration?
11. **Anchor registry hash strategy** — SHA-256 of file? of section? rebuild on `generate:ia-indexes`?
12. **Skip-gate trigger condition for #19** — schema-pass + zero MCP warnings? or stricter?
13. **Boundary marker linter (#14) failure mode** — fail-fast vs auto-repair?
14. **Status cascade trigger (#10) reversibility** — soft-flip with audit row, or hard-flip?
15. **Topological sort (#18) — already exists partial?** — confirm + finish, or rewrite?

## Success criteria

- Per-plan token usage drops measurably (~30%+ minimum) on replay of last 3 plans.
- Per-plan wall-clock drops measurably (~20%+ minimum).
- Zero quality regression — sample-audit per move shows shipped tasks match pre-rollout standard.
- All moves rollback-safe (migration revert or feature flag).
- No new metrics/observability/dashboard surfaces introduced.

## Locked design decisions (2026-05-06 polling)

| # | Decision | Locked value |
|---|---|---|
| 1 | Scope | All 15 light moves, 3 stages × 5 tasks |
| 2 | Stage order | Stage 1 trim plan → Stage 2 mechanize cycle → Stage 3 fix exploration + cascade |
| 3 | Parallelism | Strict serial — one move per task, one task at a time |
| 4 | Cutover | Hard cut, no feature flag, atomic per-move commit, rollback = `git revert` |
| 5 | Quality audit | Skip formal sample audit; trust per-task TDD red/green |
| 6 | Cache invalidation | Source content-hash only (no TTL) for #8 + #13 |
| 7 | Template registry | Single shared file-based library `ia/templates/` (digest sections + polling verbs) |
| 8 | Status cascade trigger | Soft-flip with audit row in `ia_master_plan_change_log` |
| 9 | Boundary marker linter failure | Block emit + retry once (2 attempts max) |
| 10 | Subagent review skip-gate | Format pass + zero MCP warnings |
| 11 | Anchor registry hash | Hash whole file (rebuild on `generate:ia-indexes`) |
| 12 | Topological sort | Finish existing partial in `master_plan_next_pending` |
| 13 | Migrations | One migration per move (clean revert per move) |
| 14 | Drift lint gate | Inside `validate:all` (single suite, no separate hook) |
| 15 | Cross-task deps | Allowed within stage; declared via `task_dep_register` |
| 16 | Skill edits | Edit `ia/skills/{slug}/SKILL.md` in place; rebuild via `npm run skill:sync:all` |
| 17 | Red-stage proofs | Per-task folder under `ia/projects/{TASK_ID}/red-stage/` |
| 18 | Commits | Shared per-stage prefix; one commit per task via `/ship-cycle` |

## Iteration Roadmap

| Stage | Slice | Forward-living surfaces | Throwaway |
|---|---|---|---|
| 1.0 trim-plan-size | Cut ship-plan token cost ~58k → ~6k via anchor JOIN + digest templates + drift lint + MCP cache + glossary enrich | `ia_spec_anchors` table, `ia/templates/digest-sections/*.md`, `validate:drift-lint`, `mcp_cache_get/set`, glossary back-link script | none |
| 2.0 mechanize-cycle | Cut ship-cycle token cost ~80k → ~24k via stage cache + marker lint + diff classifier + faceted index + topo sort | `ia_stage_bundle_cache` table, `validate:boundary-markers`, `task_diff_anomaly_scan` regex pack, `ia_stage_facet_view` MV, `master_plan_next_pending` topo sort | none |
| 3.0 fix-exploration | Cut design-explore token cost ~25k → ~7k + status auto-cascade via SQL trigger + reverse-mining + polling templates + review skip + arch form | `trg_status_cascade`, `red_stage_proof_mine` script, `ia/templates/polling/*.json`, design-explore skip-gate, arch-decision form-fill template | none |

## Per-stage breakdown — 15 tasks

### Stage 1 — trim-plan-size (Group A)

#### Task 1.1 — anchor registry table + JOIN

- **Goal:** Replace ship-plan Phase 5 anchor scan (~10k tokens) with one SQL JOIN.
- **Red-stage proof:** Run ship-plan on a fixture plan with anchor `glossary:wet-run` referenced in 3 task digests. Assert `mcp__territory-ia__plan_digest_resolve_anchor` returns row from new `ia_spec_anchors` table, not file scan. Failing baseline: table empty → resolver throws `anchor_registry_empty`.
- **Green:** Migration `0077_ia_spec_anchors.sql` creates `(slug, section_id, sha256, body_text, last_indexed_at)`. `npm run generate:ia-indexes` populates rows. `plan_digest_resolve_anchor` MCP rewires to JOIN.
- **Rollback:** `git revert` migration; `plan_digest_resolve_anchor` falls back to file scan branch (kept).
- **Cross-task deps:** none (head of stage).

#### Task 1.2 — drift lint pure validator

- **Goal:** Move ship-plan Phase 6 drift check (~5k tokens, 2 round-trips) into pure Node validator inside `validate:all`.
- **Red-stage proof:** Author task digest with retired surface `Foo.OldBar()` + missing glossary term `bogus-term`. Run `npm run validate:drift-lint` → expect 2 errors. Without validator: ship-plan emits broken digest, caught only at runtime.
- **Green:** `tools/scripts/validate-drift-lint.mjs` — JOIN over `ia_spec_anchors` + glossary + retired surface table; errors on stdout. Wired into `validate:all`.
- **Rollback:** Remove script + npm script line.
- **Cross-task deps:** depends on Task 1.1 (anchor registry table).

#### Task 1.3 — digest skeleton template library

- **Goal:** Replace LLM-authored digest skeletons (~30k tokens) with file-based templates filled by template engine.
- **Red-stage proof:** Author 3 tasks (FEAT, BUG, TECH) via ship-plan; assert §Plan Digest matches template at `ia/templates/digest-sections/{kind}.md` byte-for-byte except slot fills. Without templates: 3× LLM emit, drift between identical-shape tasks.
- **Green:** `ia/templates/digest-sections/{goal,red-stage-proof,work-items}.md` Markdown skeletons with `{{slot}}` fills. ship-plan Phase 5 reads template, fills slots, writes digest. SKILL.md updated.
- **Rollback:** Revert SKILL.md edit; ship-plan falls back to LLM emit.
- **Cross-task deps:** none (parallel-safe with 1.1, 1.2).

#### Task 1.4 — shared MCP context cache

- **Goal:** Cache `router_for_task` + `glossary_lookup` + `invariants_summary` per `plan_id` (~10k tokens saved across all skills).
- **Red-stage proof:** Run design-explore + ship-plan + ship-cycle on same plan_id sequentially. Assert second + third calls to `router_for_task` return cached row. Without cache: 3× MCP roundtrip.
- **Green:** Migration `0078_mcp_context_cache.sql` adds `(plan_id, key, payload, content_hash, expires_at)`. New `mcp_cache_get` / `mcp_cache_set` MCP tools. Skills wrap fetches.
- **Rollback:** Revert migration; skills fall back to direct MCP fetch.
- **Cross-task deps:** none.

#### Task 1.5 — glossary back-link auto-enrich

- **Goal:** Post-ship-plan script scans digests for glossary terms → auto-links (~3k tokens saved per plan).
- **Red-stage proof:** Author plan digest mentioning `wet run` 5× without explicit glossary link. Run script. Assert `ia_glossary_backlinks` table gains 5 rows. Without script: zero back-links, manual enrichment burden.
- **Green:** `tools/scripts/glossary-backlink-enrich.mjs` runs after ship-plan; uses `glossary_discover` MCP. Migration `0079_glossary_backlinks.sql` creates table.
- **Rollback:** `git revert` migration + script.
- **Cross-task deps:** depends on Task 1.4 (uses MCP cache).

### Stage 2 — mechanize-cycle (Group B)

#### Task 2.1 — stage-bundle digest cache

- **Goal:** Cache `stage_bundle` payload by stage content-hash (~6k tokens saved on resume).
- **Red-stage proof:** Run ship-cycle Pass A on stage S1, kill before Pass B, resume. Assert second `stage_bundle` call returns cached row keyed by hash. Without cache: full re-fetch.
- **Green:** Migration `0080_ia_stage_bundle_cache.sql` adds `(stage_id, content_hash, payload, fetched_at)`. `stage_bundle` MCP wraps fetch.
- **Rollback:** Revert migration.
- **Cross-task deps:** none.

#### Task 2.2 — boundary marker linter

- **Goal:** Pre-flip regex gate over `<!-- TASK:{ISSUE_ID} START/END -->` markers; block emit + retry once.
- **Red-stage proof:** Inject ship-cycle Pass A emit with malformed marker `<!-- TASK:FEAT-99 START` (missing closing `-->`). Linter catches → emit retried once → if second attempt fails, block flip + emit error. Without linter: corrupt diff, manual cleanup.
- **Green:** `tools/scripts/validate-boundary-markers.mjs` regex check on Pass A output buffer. ship-cycle SKILL.md adds retry loop.
- **Rollback:** Revert SKILL.md + script removal.
- **Cross-task deps:** none.

#### Task 2.3 — diff anomaly classifier

- **Goal:** Replace LLM diff review (~25k tokens) with regex pack over `git diff HEAD`.
- **Red-stage proof:** Stage diff with mixed legitimate edit + suspicious `Debug.Log` insertion + accidental `*.meta` deletion. Classifier flags 2 anomalies. Without classifier: LLM read full diff.
- **Green:** `tools/scripts/diff-anomaly-classify.mjs` regex pack (debug logs, meta deletes, large hunks, retired symbols). Wired into ship-cycle Pass B before verify-loop. New MCP `task_diff_anomaly_scan`.
- **Rollback:** Revert SKILL.md + script + MCP tool.
- **Cross-task deps:** none.

#### Task 2.4 — faceted index materialized view

- **Goal:** Materialized view for next-stage resolver (`status × stage × dep`) — ~10k tokens saved.
- **Red-stage proof:** Plan with 5 stages × 5 tasks. Call `master_plan_next_pending`. Assert query plan uses MV `ia_stage_facet_view` not nested scan. Without MV: 25-row scan + N+1 dep checks.
- **Green:** Migration `0081_ia_stage_facet_view.sql` creates MV with refresh trigger on `task_status_flip`. Resolver MCP rewires.
- **Rollback:** Revert migration.
- **Cross-task deps:** none.

#### Task 2.5 — topological sort (finish partial)

- **Goal:** Finish existing partial topo sort in `master_plan_next_pending` (~5k tokens via single emit of all parallel-ready next commands).
- **Red-stage proof:** Plan with diamond dep graph (T1 → T2,T3 → T4). After T1 done, resolver returns `[T2, T3]` not `T2` alone. Without topo: returns first only.
- **Green:** Complete the partial topo sort impl in `tools/mcp-ia-server/src/index.ts` resolver. Use Kahn's algorithm over `ia_task_deps` table.
- **Rollback:** `git revert` to single-pick branch.
- **Cross-task deps:** depends on Task 2.4 (uses faceted view).

### Stage 3 — fix-exploration (Groups C + D)

#### Task 3.1 — status cascade SQL trigger (soft-flip with audit)

- **Goal:** Auto-flip `ia_stages.status` when all child tasks done; ~5k tokens saved per closeout.
- **Red-stage proof:** Stage with 3 tasks; flip last `verified → done` via MCP. Assert stage row auto-flips `pending → done` AND audit row appears in `ia_master_plan_change_log` with `kind='status_cascade'`. Without trigger: manual flip via `stage_closeout_apply`.
- **Green:** Migration `0082_status_cascade_trigger.sql` creates `trg_status_cascade` AFTER UPDATE on `ia_tasks`; soft-flips parent + writes audit row.
- **Rollback:** `DROP TRIGGER` in revert migration.
- **Cross-task deps:** none.

#### Task 3.2 — reverse-mining red-stage proofs

- **Goal:** Mine existing C# tests + BDD names for candidate red-stage proofs (~5k tokens saved per design-explore).
- **Red-stage proof:** Run `red_stage_proof_mine FEAT-123` on a backlog issue with related test `FeatTwentyThreeTests.cs`. Assert proof candidate emitted with test name + class + assertions. Without script: LLM authors proof from scratch.
- **Green:** `tools/scripts/red-stage-proof-mine.mjs` — scans `Assets/Scripts/Tests/**`, matches BDD name patterns, emits candidate. New MCP `red_stage_proof_mine`. design-explore SKILL.md Phase 4 reads candidate.
- **Rollback:** Revert SKILL.md + script + MCP tool.
- **Cross-task deps:** none.

#### Task 3.3 — polling state machine templates

- **Goal:** Pre-canned polling templates by `core_prototype.verb` (~10k tokens saved per design-explore).
- **Red-stage proof:** Run design-explore on seed with verb `trim`. Assert Phase 1 polls match template `ia/templates/polling/trim.json` byte-for-byte except slot fills. Without templates: LLM-authored polls each run.
- **Green:** `ia/templates/polling/{verb}.json` — JSON files with poll question + 5 options + recommended option per common verb (trim, add, replace, refactor, integrate). design-explore SKILL.md Phase 1+2 reads template.
- **Rollback:** Revert SKILL.md edit.
- **Cross-task deps:** none.

#### Task 3.4 — subagent review skip-gate

- **Goal:** Skip Phase 8 subagent review when YAML frontmatter format passes + zero MCP warnings (~8k tokens saved on ~50% of plans).
- **Red-stage proof:** Two design-explore runs — one with clean YAML + zero warnings → review skipped; one with malformed YAML → review fires. Without gate: review fires every time.
- **Green:** design-explore SKILL.md Phase 8 adds gate: `validate:design-explore-yaml` + warning count check. Skip when both pass.
- **Rollback:** Revert SKILL.md edit.
- **Cross-task deps:** none.

#### Task 3.5 — architecture decision form-fill

- **Goal:** Reduce DEC-A* polling from 4 turns to 1 via form template (~5k tokens saved).
- **Red-stage proof:** Run design-explore where Phase 4 needs an arch decision. Assert single AskUserQuestion poll uses form template `ia/templates/polling/arch-decision.json` with all 4 axes (problem / chosen / alternatives / consequences) as one combined poll. Without template: 4 sequential polls.
- **Green:** `ia/templates/polling/arch-decision.json` — single combined poll. design-explore SKILL.md Phase 4 reads template; downstream `arch_decision_write` MCP unchanged.
- **Rollback:** Revert SKILL.md + template removal.
- **Cross-task deps:** depends on Task 3.3 (polling template lib infra).

## Subsystem impact

| Surface | Change |
|---|---|
| `tools/mcp-ia-server/src/index.ts` | New tools: `mcp_cache_get/set`, `task_diff_anomaly_scan`, `red_stage_proof_mine`. Edits: `plan_digest_resolve_anchor`, `stage_bundle`, `master_plan_next_pending` resolvers. |
| `db/migrations/` | 6 new migrations (0077–0082): anchor registry, MCP cache, glossary backlinks, stage bundle cache, faceted index, status cascade trigger. |
| `ia/skills/{design-explore,ship-plan,ship-cycle}/SKILL.md` | Phase rewrites for templates + cache + skip-gates. Rebuild via `npm run skill:sync:all`. |
| `tools/scripts/` | New: `validate-drift-lint.mjs`, `validate-boundary-markers.mjs`, `diff-anomaly-classify.mjs`, `glossary-backlink-enrich.mjs`, `red-stage-proof-mine.mjs`. |
| `ia/templates/` | New: `digest-sections/*.md`, `polling/*.json`. |
| `package.json` | New scripts wired into `validate:all`. |

## Sample examples

### A. Digest skeleton template (Task 1.3)

`ia/templates/digest-sections/goal.md`:

```markdown
## Goal

{{intent_one_liner}}

**Surface:** {{primary_surface}}
**Glossary:** {{glossary_terms}}
```

### B. Polling template (Task 3.3)

`ia/templates/polling/trim.json`:

```json
{
  "verb": "trim",
  "phase_1_polls": [
    {
      "question": "What gets cut first?",
      "options": [
        {"label": "Highest-cost step", "recommended": true},
        {"label": "Easiest step"},
        {"label": "User pick"}
      ]
    }
  ]
}
```

### C. Status cascade trigger (Task 3.1)

`db/migrations/0082_status_cascade_trigger.sql`:

```sql
CREATE FUNCTION trg_status_cascade_fn() RETURNS TRIGGER AS $$
BEGIN
  IF (SELECT bool_and(status = 'done') FROM ia_tasks WHERE stage_id = NEW.stage_id) THEN
    UPDATE ia_stages SET status = 'done' WHERE id = NEW.stage_id AND status != 'done';
    INSERT INTO ia_master_plan_change_log (plan_id, kind, payload)
    VALUES ((SELECT plan_id FROM ia_stages WHERE id = NEW.stage_id), 'status_cascade',
            jsonb_build_object('stage_id', NEW.stage_id, 'trigger', 'soft_flip'));
  END IF;
  RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_status_cascade AFTER UPDATE OF status ON ia_tasks
FOR EACH ROW WHEN (NEW.status = 'done') EXECUTE FUNCTION trg_status_cascade_fn();
```

## Success criteria (locked)

- ship-plan per-plan token cost: ~58k → ~6k (target 90% cut on Group A surface).
- ship-cycle per-stage token cost: ~80k → ~24k (target 70% cut on Group B surface).
- design-explore per-plan token cost: ~25k → ~7k (target 70% cut on Group C surface).
- Wall-clock: 24–32 min → 16–24 min per plan (25–40% faster).
- Zero quality regression — per-task TDD red/green covers.
- All migrations revert cleanly.
- Zero new metrics / dashboard surfaces.

## Next

`/ship-plan chain-token-cut` — bulk-author 3 stages × 5 tasks via lean YAML frontmatter at top of this doc.
