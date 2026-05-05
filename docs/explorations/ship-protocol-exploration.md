currently, the ship protocol — primarily the "ship-stage protocol" (the single-task `ship` skill is still live but used only for one-offs; this proposal retires neither but reframes their roles) — is a pipeline composed by chaining: 

- `design-explore`
- `master-plan-new`
- `master-plan-extend`
- `stage-file`
- `stage-authoring`
- `ship-stage`

With the current architecture, the use of TDD red/geen and the prototype-first methodology, the proposal is to simplify the ship protocol to a single stage, and to use the prototype-first methodology to drive the ship protocol. An example of the new ship protocol is:

- `design-explore`
- `ship-plan`
- `ship-stage`

The `ship-plan` skill would be a new skill that would be responsible for:

- authoring the plan digest (for all stages and tasks)
- implementing the plan (for all stages and tasks)
- verifying the plan
- closing the plan

Given the fact that this set represents a very complex pipeline which can take a very long time to complete, I think that this becomes a good oportunity to produce smaller master-plans, limited to no more than 5 stages, and each stage to be no more than 5 tasks. Also, task specific digest will be shorter, limited to less than 40 lines. Today the average is 65.7 lines ⏺ §Plan Digest line stats (DB)                                                                                                                                                                 
                                                                                                                                                                                               
  432 tasks carry §Plan Digest section.                                                                                                                                                        
                                                                                                                                                                                             
  ┌───────────┬─────────┐                                                                                                                                                                      
  │  Metric   │  Lines  │                                                                                                                                                                    
  ├───────────┼─────────┤
  │ Avg       │ 65.7    │
  ├───────────┼─────────┤
  │ Median    │ 59      │                                                                                                                                                                      
  ├───────────┼─────────┤
  │ Min / Max │ 2 / 417 │                                                                                                                                                                      
  └───────────┴─────────┘                                                                                                                                                                      
  
  Distribution:                                                                                                                                                                                
  - stub (≤5 lines): 2 tasks                                                                                                                                                                 
  - mid (31–80): 394 tasks · avg 58
  - large (>80): 36 tasks · avg 155
                                                                                                                                                                                               
  Typical authored digest = ~60 lines. Outliers >80 pull mean up.
                                                                                                                                                                                               
✻ Baked for 49s                                                            


------


We could reduce the average to 40 lines, and the median to 30 lines, helping reduce the time to stage-authoring each task.
In the same way, we could reduce verify-loop given the proposals made before:

  Don't narrow validate:all itself. Narrow at the call site instead. Three options ranked:                                                                                                                                       
                                                                                                                                                                                               
  1. Tiered band (best). Add validate:fast (touched-paths only — e.g. C# only → skip test:ia + validate:fixtures + MCP/skill drift; IA only → skip compute-lib:build). ship-stage Pass B picks 
  band from git diff --name-only. verify:local keeps full chain.                                                                                                                               
  2. Path-aware skip flags. Patch verify-loop Step 2 to scope by changed surfaces (already partial via validate:web:conditional pattern).                                                      
  3. Parallelize. Many scripts independent — wrap in npm-run-all -p. Same coverage, ~3× faster.  


------

Also, prototype-first methodology ensures incremental delivery, limiting the size and the blast radius. Each master-plan would deliver a fast prototype, having a version, which could be extended in a new version by extending the master-plan.

Also, some skills should be merged or simplified:
- `stage-file` and `stage-authoring` could be merged into a single skill, or at least the part that is common to both could be merged.
- `ship-stage` could be simplified by merging the pass A and pass B into a single pass.

Or maybe stage-file and stage-authoring could be first merged into one block, merging the functionality into the `master-plan-new` and `master-plan-extend` skills, and then retiring the `stage-file` and `stage-authoring` skills. And then merge those two skills as different paths inside `design-explore`.

This way, we could get finally:

- `design-explore`: 
  1) idea + grill relentlessly until common understanding is reached between the human and the agent, and the prototype to implement is defined.
  2) creation of set of user requirements to be tested against the prototype (in doc)
  3) agent defines technical requirements to meet the user requirements (in doc)
  4) defines pseudo-code tests to be used in the TDD red/green methodology (one per stage at least, one per task opt-in)

- `ship-plan`:
  4) creation of list of stages and tasks by technical requirements (in doc)
  5) master-plan-new or master-plan-extend (in database) persists stages and tasks, creates task rows in bulk, and stages rows in bulk, and master plan row. Creates any other required rows in bulk. Important to use bulk operations to avoid race conditions and to improve performance, and also define which are the tables the need to be updated in bulk.
  6) authoring of plan digest for each task (in doc), and then write to database in bulk.
- `ship-stage` (iterative):
  7) implements stages and tasks in one pass, then verifies the stage in one pass, and then closes the stage in one pass. Human does not review the plan or the code, only tests the prototype and adds comments to the agent implementing the stage, in simple product language.
- `prototype-closeout` (at the end of the iterative process)
  8) closes the version and the master plan, which could later be extended by a new master-plan-extend to create a new version.

This approach would allow for a more incremental delivery, and would allow for a more focused delivery of the prototype, and would allow for a more focused delivery of the master plan. In other way, iterative processes will have to be optimized and reduced. These processes are mainly authoring digests, implementation and access to mcp and database, for example bulk operations, and also reducing the verify-loop time by narrowing the scope of the verify-loop and leave validate:all in the final Stage of the prototype, passing responsibility to the TDD red/green methodology and the narrowed verify-loops until the final stage of the prototype. I think that is is a balanced proposal according to our current times. Also, all `ship-stage` (iterative) processes should be designed to run with sonnet 4.6 with low effort, or composer-2 in cursor, to achieve maximum speed and overall quality of implementation. The `prototype-closeout` (at the end of the iterative process) could be an Opus 4.7 with xhigh effort to achieve maximon quality control and verification of the prototype.

------

## Design Expansion

**Approach selected.** Hybrid B + per-stage atomic batch + middle skill retirement + new versioning row + validate:fast band + Sonnet/Opus split via subagent frontmatter. Resolved via 7-round design-explore grill (`AskUserQuestion` polls).

### Pipeline

```
design-explore → ship-plan → ship-cycle (iterative) → ship-final
                              ↑__________________|
                              (stages 1..N, sequential)
```

| Skill | Model | Role | Output |
|---|---|---|---|
| `design-explore` | Opus xhigh | Idea + relentless grill + user reqs + tech reqs + pseudo-code red-stage tests | Doc-only handoff under `docs/explorations/{slug}.md` with `## Design Expansion` block + lean YAML handoff frontmatter (no DB writes) |
| `ship-plan` | Opus xhigh | Bulk-author master plan + stages + tasks + 3-section digests; atomic Postgres tx | `ia_master_plans` row + `ia_stages` rows + `ia_tasks` rows + `ia_task_specs` digest bodies (all in one `master_plan_bundle_apply` MCP call) |
| `ship-cycle` | Sonnet 4.6 low-effort | Per-stage iterative: batch-implement all tasks → `validate:fast` → commit-stage → flip statuses | One git commit per stage; per-stage `master_plan_change_log_append` audit row |
| `ship-final` | Opus xhigh | Closeout: assert all sections+stages done → run full `validate:all` over cumulative diff → tag `{slug}-v{N}` + flip `ia_master_plans.closed_at` + journal entry | Git tag + closed master-plan row + journal entry |

### design-explore Phase 1 (grilling) — exit gate

- Iterative `AskUserQuestion` polling. 1-4 questions per round.
- Phase exits ONLY when user types `phase-1-done` or selects "close phase 1" option.
- **Hard rule:** zero unresolved decisions allowed at exit. No deferred decisions reach ship-plan.
- Phase 2-4 follow: user requirements (markdown), technical requirements (markdown), pseudo-code red-stage tests (one mandatory per stage, opt-in per task) — all written into the exploration doc body.

### Handoff YAML schema (lean)

design-explore emits frontmatter at top of `docs/explorations/{slug}.md`:

```yaml
---
slug: {slug}
parent_plan_id: {prior-version-id-or-null}
target_version: {N}
stages:
  - id: 1.0
    title: "Tracer slice"
    exit: "..."
    red_stage_proof: |
      pseudo-code test...
    tasks:
      - id: 1.0.1
        title: "..."
        prefix: TECH
        depends_on: []
        digest_outline: "..."
        touched_paths: ["Assets/Scripts/X.cs", "ia/specs/Y.md"]
        kind: code | doc-only | mcp-only
  - id: 2
    title: "Feature stage 1"
    exit: "..."
    red_stage_proof: |
      pseudo-code test...
    tasks:
      - id: 2.1
        ...
---
```

`ship-plan` reads frontmatter; calls `router_for_task` + `glossary_lookup` + `invariants_summary` ONCE per plan (cached); inlines anchor expansions; writes 3-section digests; bundles into one `master_plan_bundle_apply` Postgres tx.

### Plan Digest (3 sections, ~30 lines)

```markdown
## §Goal
{1-3 lines, intent}

## §Red-Stage Proof
{anchor + pseudo-code or path::method ref to failing test}

## §Work Items
- {bullet 1}
- {bullet 2}
- ...
```

Drop: §Acceptance (subsumed by Red-Stage Proof), §Test Blueprint, §Implementer Latitude, §Pending Decisions, §Invariants & Gate (moved to stage exit criteria).

### Cardinality (soft target — NEW plans only)

- ≤5 feature stages + 1 mandatory Stage 1.0 tracer (effective max 6)
- ≤5 tasks per stage
- Existing plans grandfathered (legacy ship-stage protocol)

### ship-cycle stage-atomic batch

Per stage:
1. Pre-flight: read `task_bundle` cache (loaded once at ship-plan time); skip Editor save/quit if no `Assets/**/*.cs` in stage's `touched_paths`.
2. Single Sonnet 4.6 inference implements ALL tasks in stage with structured boundaries.
3. `validate:fast` on cumulative stage diff (touched-paths band, parallel where safe).
4. If pass: single commit `feat({slug}-stage-X.Y)` + batch `task_status_flip(done)` for all stage tasks + `master_plan_change_log_append`.
5. If fail: `ia_stages.status='partial'` (new enum value). Resume re-enters at first non-done task. Fix-forward only — no rollback.

Trivial-task short-circuit: tasks with `kind=doc-only` or `kind=mcp-only` skip Unity compile-check + bridge smoke.

### Versioning

New column `parent_plan_id INTEGER NULL` on `ia_master_plans` + `version INTEGER NOT NULL DEFAULT 1`.
- v1 = first plan run (parent_plan_id=NULL).
- v2 = master-plan-new with `parent_plan_id={v1.id}, version=2`.
- ship-final tags `{slug}-v{N}` + sets `closed_at`.
- Cross-version queries walk parent chain.

### Skill retirement (Middle bundle)

| Retire | Why |
|---|---|
| `stage-decompose` | Lean YAML always fully decomposed at design-explore Phase 4 |
| `master-plan-extend` | Replaced by versioning (new master-plan row per version) |
| Old `ship-stage` | Renamed to `ship-cycle` with new semantics |
| `stage-file` | Folded into `ship-plan` bulk authoring |
| `stage-authoring` | Folded into `ship-plan` bulk authoring |
| `code-review` | TDD red/green + verify-fast + closeout validate:all replace it |
| `plan-review` | Drift scan moved into `ship-plan` synchronous lint |

| Keep | Why |
|---|---|
| `ship` (single-task) | Trivial 1-line bugfix path; "do the easiest" cost answer |
| `project-new` | One-off BACKLOG issues |
| `section-claim` / `section-closeout` | Parallel work within plan |
| `arch-drift-scan` | On-demand architecture audit |
| `verify-loop` | Sonnet, validate:fast, called by ship-cycle |
| `release-rollout` | Umbrella plans (kept under Middle bundle) |

### Existing plan migration

Two-codepath. Legacy plans run on old skills until natural close. New plans use new pipeline. No forced migration.

### Speed wins (locked)

**ship-plan:**
- C: pre-fetch glossary/router/invariants once per plan (~5× fewer MCP reads)
- D: inline anchor + glossary expansion at digest write time (~30% faster implement)
- F: pre-declared `touched_paths` per task in handoff YAML (skip diff scan)
- G: pre-loaded `task_bundle` cache at ship-plan time (cached per-plan)
- B: single `master_plan_bundle_apply` atomic tx (30× fewer DB roundtrips)

**ship-cycle:**
- H: Sonnet 4.6 low-effort vs Opus (~2-3× faster inference)
- I: `validate:fast` touched-paths band (~3-5× faster validation)
- J: parallelize validate scripts via `npm-run-all -p` (~3× faster Group A)
- K: defer `test:ia` to ship-final only (saves ~30s per stage)
- L: cache bridge preflight + Editor across stages in one invocation
- M: skip Editor save/quit when no C# touched (touched-paths heuristic)
- N: batch `task_status_flip` calls (~Nx100ms saved per stage)
- Q: trivial-task short-circuit (doc-only/mcp-only skip Unity + smoke)

**Architectural:**
- R: stage-atomic batch implement (one Sonnet call per stage; verify once; commit once)
- T: drop §Acceptance from digest (3 sections, ~30 lines)

### Drift detection

Moves INTO `ship-plan` as synchronous lint at digest write time:
- Anchor resolution (every `@anchor` ref must resolve)
- Glossary alignment (every glossary slug must exist in `ia/specs/glossary.md`)
- Retired-surface scan (no refs to retired classes/files)
- Failure → re-author that task's digest before commit to DB.

### Section-closeout × stage-atomic interaction

Resolved by stage-atomic commit (one commit per stage). Section diffs stay clean. `section-closeout` asserts all member stages = `status=done` (status='partial' fails the assertion) before releasing claim. `ship-final` asserts all sections closed before tagging version.

------

## Subsystem Impact

### Database (`db/migrations/`)

| Migration | Purpose |
|---|---|
| `00XX_master_plan_versioning.sql` | Add `parent_plan_id INTEGER NULL REFERENCES ia_master_plans(id)`, `version INTEGER NOT NULL DEFAULT 1`, `closed_at TIMESTAMPTZ NULL` to `ia_master_plans`; index on `parent_plan_id` |
| `00XX_stage_partial_status.sql` | Extend `ia_stages.status` enum with `'partial'`; update closeout queries to treat partial as non-terminal |
| `00XX_master_plan_bundle_apply.sql` | Postgres function `master_plan_bundle_apply(jsonb)` — atomic insert of plan + stages + tasks + digests |

### MCP server (`tools/mcp-ia-server/`)

| Tool | Status | Purpose |
|---|---|---|
| `master_plan_bundle_apply` | NEW | Atomic plan materialization — input matches handoff YAML schema |
| `task_bundle_batch` | NEW | Pre-load all task contexts for one plan in one call (used by ship-cycle cache) |
| `master_plan_version_create` | NEW | Create v(N+1) row pointing at v(N); used by next master-plan-new |
| `stage_status_partial_flip` | NEW | Flip stage to `partial` on task failure; reset to `in_progress` on resume |
| `task_status_flip_batch` | NEW | Batch flip all stage tasks in one MCP call |
| `master_plan_locate` | EXTEND | Return latest version row for slug; expose `parent_plan_id` chain |

Existing tools reused: `glossary_lookup`, `router_for_task`, `invariants_summary`, `spec_section`, `master_plan_change_log_append`, `journal_append`.

### Skills (`ia/skills/`)

| Skill | Action |
|---|---|
| `design-explore` | EXTEND — add Phase 1 grill exit gate (`phase-1-done` token), Phase 4 pseudo-code emit, lean YAML frontmatter writer |
| `ship-plan` | NEW — replaces `stage-file` + `stage-authoring`; reads handoff YAML, calls `master_plan_bundle_apply`, writes 3-section digests with inline anchor expansion + drift lint |
| `ship-cycle` | NEW (rename of `ship-stage` with new semantics) — Sonnet frontmatter, stage-atomic batch implement, `validate:fast` band, single commit per stage |
| `ship-final` | NEW — runs `validate:all`, tags `{slug}-v{N}`, flips `closed_at`, asserts section closure |
| `stage-decompose`, `master-plan-extend`, old `ship-stage`, `stage-file`, `stage-authoring`, `code-review`, `plan-review` | RETIRE — move to `_retired/` |
| `verify-loop` | EXTEND — accept `band={fast|all}` arg; default `fast` when invoked from ship-cycle |
| `ship` | KEEP unchanged — single-task one-off path |
| `project-new` | KEEP unchanged |
| `section-claim`, `section-closeout` | EXTEND — handle stage `partial` status; ship-final integration |
| `arch-drift-scan` | KEEP unchanged |
| `release-rollout` | KEEP — adapts to ≤5 cap per child plan |

### Validators (`tools/scripts/`)

| Script | Status | Purpose |
|---|---|---|
| `validate-fast.mjs` | NEW | Touched-paths band runner — reads `git diff --name-only`, maps to script set, runs union(baseline, scoped) |
| `validate-fast-coverage.mjs` | NEW | Meta-gate — every script in `validate:all` must have ≥1 trigger glob in path-map |
| `validate-handoff-schema.mjs` | NEW | Validates lean YAML frontmatter against schema |
| `validate-all` (existing) | EXTEND | Add parallel groups via `npm-run-all -p` for read-only Group A |
| `validate-plan-red-stage` (existing) | KEEP | Already gates per-stage Red-Stage Proof |

### Agents + commands (`.claude/`)

Generated from SKILL.md frontmatter via `npm run skill:sync:all`:
- `.claude/agents/ship-plan.md` (model: opus)
- `.claude/agents/ship-cycle.md` (model: sonnet)
- `.claude/agents/ship-final.md` (model: opus)
- `.claude/commands/ship-plan.md`, `.claude/commands/ship-cycle.md`, `.claude/commands/ship-final.md`
- Move retired: `.claude/agents/_retired/ship-stage.md`, `stage-file.md`, `stage-authoring.md`, `stage-decompose.md`, `master-plan-extend.md`, `code-review.md`, `plan-review.md` (+ matching `commands/_retired/`)

### Unity (`Assets/`)

No Unity code changes required by this proposal. ship-cycle's `kind=code` tasks still run `unity:compile-check` per touched-paths heuristic.

### Web (`web/`)

No web/ changes required.

### Documentation

| Doc | Action |
|---|---|
| `docs/MASTER-PLAN-STRUCTURE.md` | UPDATE — 3-section digest, ≤5/≤5 cap, version model |
| `docs/agent-lifecycle.md` | UPDATE — pipeline diagram, seam → surface matrix |
| `docs/agent-led-verification-policy.md` | UPDATE — `validate:fast` band, deferred `test:ia` |
| `ia/rules/tdd-red-green-methodology.md` | KEEP — already stage-level scoped |
| `ia/rules/prototype-first-methodology.md` | KEEP — Stage 1.0 tracer compatible |
| `CLAUDE.md` | UPDATE — task routing table |

------

## Implementation Points

Ordered for mechanical seeding into master plan via `/master-plan-new`:

### Stage 1.0 — Tracer slice

Single end-to-end pass on the smallest possible new-protocol plan. Validates the full pipeline before fanning out features.

- Task 1.0.1 (TECH): write 1 migration `master_plan_versioning.sql` (parent_plan_id + version + closed_at columns + index)
- Task 1.0.2 (TECH): write `master_plan_bundle_apply` Postgres function + MCP tool stub returning fixed-shape result
- Task 1.0.3 (TECH): hand-author the smallest 1-stage-1-task plan, run `master_plan_bundle_apply`, assert row counts
- Task 1.0.4 (TECH): tracer red-stage proof `tools/scripts/__tests__/master-plan-bundle-apply.test.mjs::TracerInsertsAtomically`

### Stage 2 — ship-plan skill (bulk authoring)

- Task 2.1 (TECH): `ship-plan` SKILL.md + agent-body.md with handoff YAML reader + 3-section digest writer + drift lint
- Task 2.2 (TECH): `validate-handoff-schema.mjs` validator
- Task 2.3 (TECH): `task_bundle_batch` MCP tool
- Task 2.4 (TECH): inline anchor expansion at digest write (calls `spec_section` per ref, embeds body)
- Task 2.5 (TECH): wire `npm run skill:sync:all` to generate `.claude/agents/ship-plan.md` + `.claude/commands/ship-plan.md`

### Stage 3 — ship-cycle skill (iterative)

- Task 3.1 (TECH): `ship-cycle` SKILL.md (rename of ship-stage) + Sonnet 4.6 frontmatter + stage-atomic batch implement prompt
- Task 3.2 (TECH): extend `ia_stages.status` enum with `'partial'` (migration + queries)
- Task 3.3 (TECH): `validate-fast.mjs` runner + path-map JSON + `validate-fast-coverage.mjs` meta-gate
- Task 3.4 (TECH): parallelize `validate:all` Group A via `npm-run-all -p`
- Task 3.5 (TECH): `task_status_flip_batch` MCP tool

### Stage 4 — ship-final skill (closeout)

- Task 4.1 (TECH): `ship-final` SKILL.md with section-closure assertion + `validate:all` + git tag + journal entry
- Task 4.2 (TECH): `master_plan_version_create` MCP tool
- Task 4.3 (TECH): closeout journal schema row

### Stage 5 — design-explore extensions + retirement migration

- Task 5.1 (TECH): design-explore Phase 1 `phase-1-done` exit token + relentless grilling polling loop
- Task 5.2 (TECH): design-explore Phase 4 pseudo-code red-stage proof writer + lean YAML frontmatter emitter
- Task 5.3 (TECH): retire skills via `_retired/` move + sync agents/commands
- Task 5.4 (TECH): doc updates (MASTER-PLAN-STRUCTURE.md, agent-lifecycle.md, agent-led-verification-policy.md, CLAUDE.md)
- Task 5.5 (TECH): self-host validation — author this proposal's master plan via the new pipeline (dogfood)

------

## Architecture Sketch

```
┌────────────────────────────────────────────────────────────────────┐
│ design-explore (Opus)                                              │
│   Phase 1: relentless grill (AskUserQuestion polling)              │
│            exit gate = phase-1-done token                          │
│   Phase 2: user requirements (markdown body)                       │
│   Phase 3: technical requirements (markdown body)                  │
│   Phase 4: pseudo-code red-stage tests (one per stage min)         │
│            emit lean YAML frontmatter                              │
└────────────────────────────────────────────────────────────────────┘
                              │
                              ▼ docs/explorations/{slug}.md (frontmatter)
┌────────────────────────────────────────────────────────────────────┐
│ ship-plan (Opus)                                                   │
│   - read handoff YAML                                              │
│   - pre-fetch glossary + router + invariants ONCE                  │
│   - inline anchor expansion at digest write                        │
│   - drift lint (anchor + glossary + retired-surface)               │
│   - master_plan_bundle_apply (atomic Postgres tx)                  │
│       │   master_plans row                                         │
│       │   stages rows (stage-atomic exit + red_stage_proof)        │
│       │   tasks rows                                               │
│       │   task_specs digest bodies (3-section, ~30 lines)          │
└────────────────────────────────────────────────────────────────────┘
                              │
                              ▼ DB-backed plan ready
┌────────────────────────────────────────────────────────────────────┐
│ ship-cycle (Sonnet 4.6 low-effort)  [iterates per stage]           │
│   ┌──────────────────────────────────────────────────┐             │
│   │ stage K:                                         │             │
│   │   pre-flight (cached bundle, kind heuristic)     │             │
│   │   single inference: implement ALL stage tasks    │             │
│   │   validate:fast (touched-paths band, parallel)   │             │
│   │   ┌──────────┐                                   │             │
│   │   │ pass?    │──── no ──► stage.status='partial' │             │
│   │   └────┬─────┘            (fix-forward, resume)  │             │
│   │        │ yes                                     │             │
│   │        ▼                                         │             │
│   │   commit feat({slug}-stage-K)                    │             │
│   │   batch task_status_flip(done)                   │             │
│   │   master_plan_change_log_append                  │             │
│   └──────────────────────────────────────────────────┘             │
│   next stage…                                                      │
└────────────────────────────────────────────────────────────────────┘
                              │
                              ▼ all stages done; sections closed
┌────────────────────────────────────────────────────────────────────┐
│ ship-final (Opus xhigh)                                            │
│   - assert all sections closed                                     │
│   - assert all stages done                                         │
│   - validate:all (parallel) on cumulative diff                     │
│   - git tag {slug}-v{N}                                            │
│   - flip ia_master_plans.closed_at                                 │
│   - journal_append closeout row                                    │
└────────────────────────────────────────────────────────────────────┘
                              │
                              ▼ next version
                       master-plan-new {slug} --parent v{N}
                              creates v{N+1} row
```

------

## Open Implementation Details (deferred to ship-plan stages)

1. `validate:fast` path-map JSON exact schema (Stage 3.3).
2. Handoff YAML full JSON-schema spec for `validate-handoff-schema.mjs` (Stage 2.2).
3. `master_plan_bundle_apply` exact MCP signature + error shapes (Stage 1.0.2).
4. `ia_stages.status='partial'` enum migration + downstream query updates (Stage 3.2).
5. Speculative-drafting (option O) — explicitly DEFERRED to a v2 master plan; not in scope of v1.
6. Persistent ship-cycle daemon (option S) — explicitly DEFERRED; v1 keeps stateless invocation.

