---
purpose: "Cross-cutting friction log for master-plan execution (stage-file + ship-stage + adjacent skills). Captures drift, errors, staleness, stoppages, digest/author misses per stage run. Feeds iteration on simplification, token economy, execution speed."
audience: both
loaded_by: ondemand
slices_via: none
---

# Master-plan execution friction log

> **Status:** Living doc — append per stage run. Do NOT rewrite history. Do NOT promote fixes here; promote to skill `§Changelog` or file a TECH issue.
> **Created:** 2026-04-24
> **Scope:** Every `/stage-file`, `/ship-stage`, `/closeout`, `/author`, `/plan-digest`, `/plan-review`, `/audit` invocation on a live master plan. Adjacent friction (digest missing, author stale, drift caught late) counts.
> **Out of scope:** Single-task `/project-new` + `/ship` pipeline (file separate BUG-/TECH- against skills directly). Per-skill `§Changelog` covers applied fixes; this doc covers **unresolved** friction awaiting triage.

---

## 1. Why this log exists

Stage-file + ship-stage chains touch 6–12 skills, 5–30 min wall-clock, 100k+ tokens per stage. Drift + staleness + digest gaps currently surface **during** execution → operator restarts, partial commits, stale specs. Fix path is invisible without a capture surface. This log = capture surface.

**Targets (baseline → aim):**

| Dimension | Current pain | Aim |
|---|---|---|
| Simplification | 6 skill files per stage run (plan → apply → author → digest → review → ship) | Fewer seams, merged skills where safe |
| Token economy | Re-reads of master plan + spec stubs across N tasks | Shared bundle fetched once per stage |
| Execution speed | Stage wall-clock dominated by sequential subagent spawns | Parallelize Pass 1 per-task implement where DAG permits |
| Drift rate | Stale §Plan Digest / missing §Plan Author caught at `/ship-stage` gate | Caught at `/stage-file` tail or earlier |
| Master-plan size | Single `.md` grows 2k–8k lines (all stages + full task tables + Objectives/Exit/Deferred prose); every subagent re-reads it | Under ~800 lines core plan; per-stage detail externalized; closed content archived out |

---

## 2. How to log a friction entry

**When:** Right after the stage run stops (green or red). Do not wait. Fresh memory > perfect form.

**Where:** Append one entry under `## 3. Entries` as a new `### {YYYY-MM-DD} — {stage-id} — {one-line symptom}` block.

**Template (copy verbatim):**

```markdown
### {YYYY-MM-DD} — {master-plan-slug} Stage {X.Y} — {one-line symptom}

**Run:** `/stage-file` | `/ship-stage` | `/closeout` | `/author` | `/plan-digest` | `/plan-review` | `/audit` | chain
**Outcome:** green | red-stopped | red-partial | red-drift | red-stale
**Wall-clock:** {N min} (optional)
**Tokens (estimate):** {N k} (optional — from `.claude/traces/` if captured)
**Iterations:** {N retries / fix-verify loops}

**Symptom (caveman):** what observer saw. One-two sentences.

**Trigger:** {skill-name / subagent / MCP tool / validator / git state} that surfaced it.

**Root cause guess:** {stale spec | missing digest | drift vs invariants | id collision | validator gap | subagent prompt gap | other}

**Blast radius:** {single task | single stage | whole master plan | cross-plan}

**Recovery action taken:** what you did to unstick. None = left red.

**Category tag:** `drift` | `staleness` | `digest-miss` | `author-miss` | `validator-gap` | `subagent-prompt` | `resume-gap` | `token-bloat` | `skill-seam` | `other`

**Candidate fix (optional):** skill-edit | new-task | new-validator | new-rule | observation-only
```

**Guardrails:**

- Do NOT edit past entries. If new info → append new entry referencing prior id.
- Do NOT fix in this doc. Fix lands in skill `§Changelog` or new TECH issue; link back here via "Resolved by:" footer edit (one-liner only).
- Keep symptom in caveman; keep verbatim error strings in fenced code.
- Stage id format: `{slug}-master-plan Stage X.Y` (matches `MASTER-PLAN-STRUCTURE.md`).

---

## 3. Entries

<!-- Append new entries below this line. Newest last. -->

---

## 4. Triage cadence

- **Per-stage:** After each red run, operator appends entry same session.
- **Weekly sweep:** Opus pass reads all entries since last sweep → groups by `Category tag` → proposes fixes. Output: (a) skill-edit PRs, (b) new TECH rows, (c) closed friction entries with resolution link.
- **Monthly retro:** Aggregate categories → rank by frequency × blast radius → feed next master plan of lifecycle refactor work.

Sweep trigger command (future — not yet a skill): `/friction-sweep --since {YYYY-MM-DD}`.

---

## 5. Rank-ordered optimization strategy (stage-file + ship-stage)

**Ordering principle:** simplification first (removes code paths → removes drift sources), then token economy (once seams are stable), then speed (parallelism last — hardest to debug).

### 5.1 Simplification — reduce skill seams

| Move | Target | Expected win | Risk |
|---|---|---|---|
| Merge `stage-file-plan` + `stage-file-apply` into single `stage-file` skill with internal pair-tail | removes 2 SKILL.md files + 2 subagent prompts | Lower drift surface; fewer cache floors to validate | Breaks existing `/stage-file` command contract — need migration |
| Merge `plan-author` + `plan-digest` bulk passes into single bulk `stage-authoring` pass | 1 Opus pass instead of 2 | Shared MCP bundle re-use; fewer stage-boundary handoffs | Digest gate (mechanical) currently run separately — must keep lint invariant |
| Fold `plan-review` drift scan into `stage-file` tail (pre-ship gate) | drift caught at file-time, not ship-time | Removes `/plan-review` as separate step | Reviewers want fresh scan right before ship — need freshness TTL |
| Retire `§Plan Author` as permanent section (already ephemeral per lifecycle-refactor) + collapse into `§Plan Digest` | one normative section per spec | Reduces spec file size + drift targets | Already in flight; confirm before acting |

### 5.2 Token economy — fetch once per stage

| Move | Target | Expected win |
|---|---|---|
| `stage_bundle` MCP tool — returns master-plan header + all pending Task spec stubs + invariants summary + glossary discover in one payload | Replaces N × (`spec_section` + `backlog_issue` + `glossary_lookup`) | 40–60 % fewer MCP round-trips per stage |
| Per-task spec slice cap — subagents request §-slices not whole files | enforce via `spec_section` only | Stops accidental whole-spec reads |
| Shared preamble cache floor for stage-scoped subagents | all of stage-file + ship-stage subagents read identical Tier-1 block | F5 cache hit rate ↑ |
| Drop `ship-stage` Step 0 re-read of master plan when `{CHAIN_JOURNAL}` already has stage header hashed | resume-gate path | Saves one full master-plan read per resumed run |

### 5.3 Execution speed — parallelize what's safe

| Move | Target | Expected win | Guardrail |
|---|---|---|---|
| Pass 1 per-task implement runs parallel when Depends-on DAG has no intra-stage edges | `ship-stage` Step 2 | Wall-clock ÷ N for independent tasks | Must verify DAG at Step 1.6; serial fallback on cycle detect |
| `validate:all` + `unity:compile-check` run in parallel after Pass 1 (independent I/O) | Step 3.1 pre-verify-loop | Saves 20–40 s per stage | None — already independent |
| Bulk `§Audit` authoring (already single Opus pass) stays serial but pre-loads Stage MCP bundle from `ship-stage` journal | avoid re-fetch | Saves 1 MCP round-trip | None |
| `/closeout` stage migration ops run batched (shared `flock`) | `plan-applier` Mode stage-closeout | Fewer `flock` contention waits | Lockfile contract intact |

### 5.4 Master-plan size — foldered master plans

**Headline proposal:** convert monolithic `{slug}-master-plan.md` into a folder `{slug}-master-plan/` with one `index.md` + one file per Stage. Agents navigate by stage id → read only the target stage file (~100–200 lines) instead of the whole plan (~2k–8k lines).

**Folder shape:**

```
ia/projects/{slug}-master-plan/
  index.md              # Goal + Exit + Steps table + Stage pointers + rollup status + cross-stage invariants
  stage-1.1.md          # Stage header + task table + Objectives + Exit + Phase bullets + Deferred hints
  stage-1.2.md
  stage-2.1.md
  ...
  _closed/              # archived Done stages (one-line pointer stays in index)
    stage-0.1.md
```

**Why this beats a single file:**

- Subagents read one stage file, not whole plan → 80 %+ token drop on hot path.
- Parallel stage edits safe (different files → no merge conflicts).
- Closed-stage archive = `mv stage-X.Y.md _closed/` + flip index pointer. Hot surface stops growing.
- Per-stage PR scope natural (diff bounded to one file).
- Cross-plan grep for a stage id stays cheap.

**Impact surfaces (must update together):**

| Surface | Change |
|---|---|
| MCP `master_plan_locate` | Resolve `{slug}` → folder; route `Stage X.Y` → `{slug}/stage-X.Y.md` |
| MCP `spec_section` / `spec_sections` | Accept `{slug}/stage-X.Y.md#anchor` paths |
| `validate:master-plan-status` | Aggregate across `index.md` + all `stage-*.md` |
| `stage-file` Step 0 | Read `index.md` for stage pointer; load target stage only |
| `ship-stage` Step 0 + journal | Same; journal records folder + stage file path |
| `stage-decompose` / `master-plan-extend` | Append new `stage-X.Y.md`; update index table |
| `master-plan-new` | Emit folder shape from template, not single file |
| Migration tool | One-shot script converts the ~6 live plans |
| Cross-refs in docs/specs | Path rewrites (mechanical find-replace) |

**Complementary (smaller) size moves — keep after foldering lands:**

| Move | Expected win | Risk |
|---|---|---|
| **Drop inline §Plan Author from specs** — already ephemeral per lifecycle-refactor; enforce via validator | Specs stay ~200 lines not 600+ | Confirm in-flight with refactor |
| **Externalize §Plan Digest aggregate** — stage-level `docs/implementation/{slug}-stage-X.Y-plan.md` already exists; make it primary not duplicate | Cuts duplication between spec + aggregate | None |
| **Compact Stage header format** — YAML-ish frontmatter (`status: / tasks-done: N/M / blocker: …`) | 30–40 % fewer lines per stage file | Parser update (small inside foldered shape) |
| **Task-table slim** — drop `Intent` column once §Plan Digest lands | 5-col → 4-col table | `plan-review-mechanical` reroutes to digest |

**Cross-stage ordering:**

1. **Prototype foldering on one plan first** (e.g. smallest live master plan) — measure token + wall-clock delta before broad rollout.
2. If win confirmed, migrate remaining plans one at a time; each migration lands with its own TECH issue + validator green gate.
3. Complementary moves (header compact / column slim / digest consolidation) layer on afterward inside the new shape.

**Open questions (operator to confirm before master plan starts):**

- Keep `.md` extension on folder? → `{slug}-master-plan/` (folder) vs `{slug}-master-plan.md` (current file). Folder wins; existing `.md` convention applies to children only.
- Index anchor naming: `#stage-1-1` vs `#1.1` vs filename route? Filename route (`./stage-1.1.md`) cheapest.
- Per-task specs (`TECH-XXX.md`) — stay single file (already small enough) or also folder? → Stay single file; decision out of scope for this proposal.

### 5.5 Observability — make friction visible

| Move | Target |
|---|---|
| `stdout tee` + `PostToolUse` hook JSONL per stage run — already documented in `chain-execution-token-analysis.md` | Default on for `/ship-stage` runs until M8 harness lands |
| Append `friction-log` stub when `/ship-stage` exits red → prompt operator to fill in | Make capture the default path, not manual |
| Per-stage wall-clock + token estimate emitted in stage-end digest | So log entries have numbers not guesses |

---

## 6. Sequencing (proposed)

1. **Week 1** — turn on `.claude/traces/` capture by default for `/ship-stage`; land auto-append stub when red exit. Measure current master-plan line counts (`wc -l ia/projects/*master-plan*.md`) as size baseline.
2. **Weeks 1–2** — collect ≥5 friction entries; run first triage sweep; categorize.
3. **Weeks 2–3** — **foldered master-plan prototype** on smallest live plan (design → migration script → MCP + validator updates → one plan migrated → measure). Gate rollout on measured win.
4. **Weeks 3–4** — act on top friction category (likely `staleness` or `digest-miss`): ship merge of `plan-author`+`plan-digest` or tighten `stage-file` tail drift gate.
5. **Week 5** — if foldering win confirmed, migrate remaining master plans one at a time. Complementary size moves (header compact, column slim, digest consolidation) layer on inside the new shape.
6. **Week 6** — re-measure wall-clock + tokens baseline vs after; decide on Pass 1 parallelism.
7. **Ongoing** — monthly retro rolls categories into master-plan backlog; keep log as the single source of unresolved friction.

**Next action for operator:** after the next `/ship-stage` or `/stage-file` run (green or red), append one entry per §2 template. That's the whole protocol — one entry per run until the log has signal.

---

## 7. Cross-links

- Per-skill applied fixes: `ia/skills/{skill}/SKILL.md §Changelog` (source of truth for done work).
- Release-rollout skill bugs: `ia/skills/release-rollout-skill-bug-log/SKILL.md` (row-scoped, not stage-scoped — different surface).
- Token capture recipes: [`docs/chain-execution-token-analysis.md`](chain-execution-token-analysis.md).
- Canonical optimization roadmap: [`docs/lifecycle-token-optimization-audit.md`](lifecycle-token-optimization-audit.md).
- Canonical master-plan shape: [`docs/MASTER-PLAN-STRUCTURE.md`](../docs/MASTER-PLAN-STRUCTURE.md).


---

## 8. IA dev DB-primary refactor — closing retrospective (2026-04-25)

The `feature/ia-dev-db-refactor` branch (12 steps, design `docs/master-plan-foldering-refactor-design.md` → implementation `docs/ia-dev-db-refactor-implementation.md`) flips the IA system from filesystem-primary (yaml under `ia/backlog/` + flat task specs under `ia/projects/`) to **Postgres-primary** (`ia_*` schema; specs persist in `ia_tasks.body` with `body_tsv` GIN index; backlog views regen from DB; flat specs deleted). This section closes the friction signal that drove the refactor.

### 8.1 Pre- vs post-refactor comparison

| Surface | Pre-refactor (filesystem-primary) | Post-refactor (DB-primary) |
|---|---|---|
| **Source of truth** | `ia/backlog/{ID}.yaml` + `ia/projects/{ID}.md` (flat) + `ia/projects/{slug}-master-plan.md` | `ia_tasks` row (id + body) + `ia_master_plans` + `ia_stages` |
| **Id assignment** | `tools/scripts/reserve-id.sh` + `flock` on `id-counter.json` | Per-prefix Postgres sequences via `task_insert` MCP |
| **BACKLOG.md regen** | `materialize-backlog.sh` parses yaml | `materialize-backlog-from-db.mjs` reads `raw_markdown` column (byte-faithful) |
| **Spec authoring** | `plan-author` (Opus bulk) → `plan-digest` (Opus mechanizer) Sonnet pair-tail | `stage-authoring` single Opus pass writes §Plan Digest direct via `task_spec_section_write` MCP |
| **Stage closeout** | `stage-closeout-planner` (Opus) → `stage-closeout-applier` (Sonnet) pair | `stage_closeout_apply` MCP tool inline in `ship-stage` Phase 7 |
| **Per-task commits** | One `feat({id}):` commit per task in Pass A | Single stage commit `feat({slug}-stage-X.Y):` covers all tasks (E13) |
| **Resume gate** | git scan for `feat(id):` / `fix(id):` regex | DB query on `task_state` + `task_status` enum |
| **History audit** | None — overwriting a spec lost prior content | `ia_task_spec_history` row per `task_spec_section_write` (actor / change_reason / git_sha) |
| **FTS over specs** | `grep -r ia/projects/` | `task_spec_search` MCP using `body_tsv` GIN + `websearch_to_tsquery` + `ts_headline` |
| **Web dashboard** | None | Next.js `/ia` routes + 5 API routes + 3 server-component pages |
| **Snapshot strategy** | None (yaml + .md were the snapshot) | Daily split: 1.28 MB diffable plain SQL + 450 KB compressed binary |

### 8.2 Friction categories — what shrank, what shifted

- **`staleness` (was top category)** — refactor eliminates two stale-spec failure modes: (1) yaml `id:` field drift vs filename — now structurally impossible (DB sequence + PK); (2) BACKLOG.md row vs spec-body skew — collapsed by `raw_markdown` round-trip + `materialize-backlog-from-db.mjs` byte-faithful regen.
- **`digest-miss`** — `stage-authoring` skill collapses the two-step author→digest into one Opus pass (B6); §Plan Digest is now the only authored section per design D8. Aggregate `docs/implementation/{slug}-stage-X.Y-plan.md` doc dropped (was duplication source).
- **`token-bloat`** — flat task specs (~600+ lines each at peak) now live in `ia_tasks.body` and load on demand via `task_spec_section` slice. ~137 flat specs deleted in Step 9.5; project folders deleted in Step 9.6.11.
- **`pair-tail-thrash` (NEW shifted category)** — three retired pair-tail seams (`stage-closeout-applier`, `code-fix-applier`, `plan-fix-applier`) collapsed into single `plan-applier` skill (576 → ~180 lines, single seam #1 plan-fix mode survives — `plan-review` still emits §Plan Fix tuples). Two seams (`stage-closeout-applier`, `code-fix-applier`) absorbed inline by `ship-stage` per E14 (critical fix in place, cap=1) + C10 (`stage_closeout_apply` MCP).
- **`commit-noise` (NEW shifted category)** — per-task commit ceremony retired (E13). Stage-end single commit covers Pass A diffs + code-review fixes + closeout `git mv`. Reduces ~5–10× commit volume per stage.

### 8.3 New friction surfaces introduced (followup work)

- **`validate:dead-project-specs` red after Step 9.5 flat-spec deletion** — durable docs (`docs/implementation/grid-asset-visual-registry-stage-3.3-plan.md`, `docs/architecture-audit-change-list-2026-04-22.md`, `docs/unity-agent-bridge-master-plan-handoff.md`, BACKLOG.md row anchor lines for closed-but-not-archived issues) reference deleted flat specs. Refactor branch ships with this gate red; repaint paths to BACKLOG.md by id or restore archive shape. Followup ticket filed.
- **Web dashboard architectural deviation** — Step 10 spec called for Next.js routes proxying through MCP read tools. Subprocess spawn per request would block; chose direct pg via `getSql()` lazy singleton matching existing catalog API pattern. MCP-as-HTTP-bridge or per-request worker pool deferred to followup.
- **Snapshot scheduling not automated** — `npm run db:snapshot` runs manually per dev session. Local cron vs GitHub Actions schedule deferred to followup ticket; binary size budget revisit at >10 MB triggers LFS decision.
- **`mechanicalization_preflight_lint` advisory hatch scope** — Step 7 self-smoke surfaced TECH-776 hatch scope-expansion: hatch covers `picks` field but not `validators` field; `tupleCount` regex matches across whole spec body, not §Plan Digest section only. Logged in Step 7 commit, deferred here.
- **MCP schema staleness during live-session subagent dispatch** — Steps 6 + 7 self-smokes blocked dispatching subagents that referenced new MCP tools (server caches schema at session start). Production runs after MCP-host restart will work; documented in invariants. Pattern to watch: any future MCP tool added to a skill that's invoked by an in-flight subagent in the same session.

### 8.4 What this enables

- **Live dashboard** — `/ia` renders plan progress + change log + FTS over spec bodies in real time (no static-site regen step).
- **Body history audit** — every `task_spec_section_write` snapshots prior body with `actor` / `change_reason` / `git_sha`. Enables retroactive "who changed §Plan Digest of TECH-X and why" queries.
- **Concurrency-safe id assignment** — parallel `task_insert` calls hit per-prefix DB sequence, no advisory lock dance.
- **Single-commit stage flow** — fewer commits to scan for ship history; resume gate is a DB query, not git archaeology.
- **Lighter spec footprint** — flat specs (~137) gone; `ia/projects/` is an empty parent dir at this point. New specs land directly in DB via `task_insert` body field.

### 8.5 Sequencing for the next quarter

1. **Snapshot CI cadence** — file ticket: GitHub Action on schedule (daily) commits `ia/state/db-snapshot-*.{sql,dump}` to `chore/db-snapshot-{date}` branch + opens PR for review (or auto-merges if smoke green).
2. **`validate:dead-project-specs` repaint** — file ticket: rewrite stale durable doc references to BACKLOG.md anchor lines or DB-id-only refs.
3. **Web dashboard MCP transport revisit** — file ticket: evaluate per-request MCP HTTP bridge vs in-process MCP client for Next.js routes.
4. **`mechanicalization_preflight_lint` hatch scope-expansion** — file ticket (TECH-776 follow-up): widen hatch to cover `validators` field + scope `tupleCount` regex to §Plan Digest section only.
5. **Per-prefix sequence drift audit** — `tech_id_seq` advanced from 817 to ~880 during refactor; sequence gaps from rollback (e.g. TECH-859 lost in Step 6 self-smoke) are fine — `nextval` doesn't replay. Periodic audit script to confirm no id collisions.

