# Skill files audit — post DB-primary refactor

Branch: `feature/skill-files-audit`. Started 2026-04-25 after IA dev DB-primary refactor merged to `main` (`30ef247`, Steps 1–12).

## Objective (revised)

**Phase A — full retirement scrub.** Delete every `_retired/` surface from the repo. Scrub all live refs (in active commands + agents + skills + rules + templates + force-loaded docs + validators) so no live surface points at a retired name. Historical docs (explorations, audits, implementation logs, friction logs) keep their refs — frozen history.

**Phase B — new single `/ship` skill.** Replace the current 5-stage `/ship` (readiness → implement → verify-loop → code-review → audit) with one mechanical skill: **plan → implement → verify → close**. No separate code-review pass, no separate audit pass — simpler shipping path for single-task work.

Out of scope: durable-doc repaint outside lifecycle surfaces (TECH-875 — separate ticket); generated-view file deletion (TECH-879); CI cadence (TECH-874).

## Context — what changed in the refactor

| Surface | Pre-refactor | Post-refactor (Step 9.x) |
|---|---|---|
| Backlog rows | `ia/backlog/{id}.yaml` files | `ia_tasks` row (DB) |
| Archive | `ia/backlog-archive/{id}.yaml` files | `ia_tasks.archived_at` timestamp |
| Backlog markdown | `BACKLOG.md` canonical | regen view from DB |
| Task spec body | `ia/projects/{ISSUE_ID}.md` flat file | `ia_tasks.body` column + `ia_task_spec_history` audit |
| Master plan | `ia/projects/{slug}-master-plan.md` | `ia_master_plans.preamble` + `ia_stages` + `ia_master_plan_change_log` |
| Sibling docs | `ia/projects/{slug}-{name}.md` | `docs/{name}.md` (Step 9.6.10) |
| Runtime state | `ia/state/runtime-state.json` | `ia_runtime_state` row |
| Project journal | flat md | `ia_project_spec_journal` |
| Ship-stage journal | flat md | `ia_ship_stage_journal` |

`ia/projects/` directory **does not exist** on disk. Spec body lives in DB; reads via `task_spec_body` / `task_spec_section` MCP, writes via `task_spec_section_write`.

---

## Phase A — Retirement scrub

### A1 Retirement inventory (31 files in `_retired/` dirs)

| Tombstoned at | Path | Replaced by (active) | Notes |
|---|---|---|---|
| agent | `closeout.md` | `stage-closeout-applier` (also retired — see below) → inline in `/ship-stage` Step 3.5 / `/closeout {plan} {stage}` | umbrella close gone |
| agent | `code-fix-applier.md` | `plan-applier` Mode code-fix | TECH-506 unified pair-tail |
| agent | `plan-author.md` | `stage-authoring` | folded post-Step-7 |
| agent | `plan-digest.md` | `stage-authoring` | folded post-Step-7 |
| agent | `plan-fix-applier.md` | `plan-applier` Mode plan-fix | TECH-506 unified pair-tail |
| agent | `plan-reviewer.md` | `plan-reviewer-mechanical` + `plan-reviewer-semantic` | split |
| agent | `project-new.md` | `project-new-planner` + `project-new-applier` pair | (still pair-based, both active) |
| agent | `spec-kickoff.md` | absorbed into authoring | retired |
| agent | `stage-closeout-applier.md` | `plan-applier` Mode stage-closeout | TECH-506 unified pair-tail |
| agent | `stage-closeout-planner.md` | (still active? confirm) | check |
| agent | `stage-file-applier.md` | `stage-file` (merged) | folded Step 6 |
| agent | `stage-file-planner.md` | `stage-file` (merged) | folded Step 6 |
| agent | `stage-file.md` | `stage-file` (merged DB-backed) | older monolith |
| command | `author.md` | `stage-authoring` | folded post-Step-7 |
| command | `closeout.md` | `closeout` (Stage-scoped only) | umbrella close gone |
| command | `kickoff.md` | absorbed | retired |
| command | `plan-digest.md` | `stage-authoring` | folded post-Step-7 |
| skill | `code-fix-apply` | `plan-applier` Mode code-fix | tombstoned |
| skill | `plan-author` | `stage-authoring` | folded |
| skill | `plan-digest` | `stage-authoring` | folded |
| skill | `plan-fix-apply` | `plan-applier` Mode plan-fix | tombstoned |
| skill | `project-new-plan` | `project-new` (active) + `project-new-apply` (active) | retired predecessor |
| skill | `project-spec-close` | Stage-scoped closeout | retired umbrella |
| skill | `project-spec-kickoff` | absorbed | retired |
| skill | `project-stage-close` | Stage-scoped `closeout` skill | retired |
| skill | `stage-closeout-apply` | (active? confirm) | check |
| skill | `stage-closeout-plan` | `plan-applier` Mode stage-closeout | tombstoned |
| skill | `stage-file-apply` | `stage-file` (merged) | folded |
| skill | `stage-file-monolith` | `stage-file` (merged DB-backed) | older monolith |
| skill | `stage-file-plan` | `stage-file` (merged) | folded |

### A2 Live ref counts

Refs to retired surface names (excluding `_retired/`, `node_modules/`, `dist/`, `coverage/`, `pre-refactor-snapshot/`, `search-index.json`):

- **127 files** total grep-hit (broad pattern across all lifecycle surface names).
- Most are **historical scratchpads** (`docs/*-exploration.md`, `docs/*-audit-*.md`, `docs/architecture-audit-*`, `docs/lifecycle-refactor-*`, `docs/master-plan-execution-friction-log.md`, `docs/ia-dev-db-refactor-implementation.md`) — frozen history, **do not touch**.
- **Live surfaces requiring scrub** (subset):
  - `.claude/agents/` — `opus-auditor.md`, `plan-reviewer-mechanical.md`, `plan-reviewer-semantic.md`, `project-new-applier.md`, `project-new-planner.md`, `ship-stage.md`, `spec-implementer.md`, `stage-authoring.md`, `stage-file.md`, `verify-loop.md`
  - `.claude/commands/` — `audit.md`, `implement.md`, `plan-review.md`, `project-new.md`, `ship.md`, `stage-authoring.md`, `stage-file-main-session.md`, `stage-file.md`
  - `.claude/output-styles/closeout-digest.md`
  - `ia/skills/` — `design-explore`, `ide-bridge-evidence`, `master-plan-extend`, `master-plan-new`, `opus-audit`, `plan-applier`, `plan-review-semantic`, `plan-review`, `progress-regen`, `project-implementation-validation`, `project-new-apply`, `project-new`, `release-rollout`, `ship-stage`, `stage-authoring`, `stage-decompose`, `stage-file-main-session`, `stage-file`, `subagent-progress-emit`, `verify-loop`, `README.md`
  - `ia/rules/` — `agent-router.md`, `orchestrator-vs-spec.md`, `plan-apply-pair-contract.md`, `project-hierarchy.md`, `unity-scene-wiring.md`
  - `ia/templates/` — `master-plan-template.md`, `project-spec-template.md`, `project-spec-review-prompt.md`
  - `CLAUDE.md`, `AGENTS.md`, `docs/agent-lifecycle.md`, `docs/PROJECT-SPEC-STRUCTURE.md`, `docs/MASTER-PLAN-STRUCTURE.md`, `docs/mcp-ia-server.md`
  - `tools/scripts/validate-agent-tools.ts`, `validate-agent-tools-uniformity.ts`, `validate-cache-block-sizing.ts`

### A3 Removal sequence (proposed)

1. **Confirm tombstone status** — verify each `_retired/` surface has zero live caller (sample MCP validators + skill imports).
2. **Scrub validator allowlists** — `tools/scripts/validate-agent-tools*.ts` + `validate-cache-block-sizing.ts` likely list retired agent ids; remove rows.
3. **Scrub `agent-tools-allowlist`** in `.claude/settings.json` if it lists retired agents.
4. **Scrub live refs in Tier-1 + lifecycle surfaces** — one commit per surface (CLAUDE.md, AGENTS.md, agent-lifecycle.md, agent-router.md, project-hierarchy.md, plan-apply-pair-contract.md, orchestrator-vs-spec.md, unity-scene-wiring.md, ia/skills/README.md).
5. **Scrub command + agent + skill body refs** — one commit per `.claude/commands/{name}.md` + `.claude/agents/{name}.md` + `ia/skills/{name}/SKILL.md` that points at a retired name.
6. **Scrub templates** — `master-plan-template.md`, `project-spec-template.md`, `project-spec-review-prompt.md`.
7. **Delete `_retired/` directories** — `rm -rf .claude/agents/_retired/ .claude/commands/_retired/ ia/skills/_retired/`. One commit.
8. **Run `npm run validate:all`** — gate. Fix new red.

Each commit message: `chore(skill-files-audit): scrub {surface} retired refs` (or `delete {N} retired tombstones` for Step 7).

---

## Phase B — New single `/ship` skill

### B1 Design intent (user statement)

> "Single 'ship' skill which will not be quite like the old one. This new ship skill just has to mechanically plan, implement and verify, then close."

### B2 Comparison

| Aspect | Old `/ship` (5 stages) | New `/ship` (4 stages) |
|---|---|---|
| Stage 1 | Readiness gate (§Plan Digest populated) | **Plan** — author §Plan Digest if missing (mechanical, no Opus pass) |
| Stage 2 | Implement (`spec-implementer`) | **Implement** — same |
| Stage 3 | Verify-loop (`verify-loop`) | **Verify** — same |
| Stage 4 | Code-review (`opus-code-reviewer` + fix loop) | (folded into verify-loop iteration cap, OR dropped — TBD) |
| Stage 5 | Audit (`opus-auditor`) | (dropped — single-task audit was N=1 degenerate) |
| Close | (manual / batched into Stage close) | **Close** — flip `task_status_flip(done)` + archive + commit |

### B3 Design decisions (locked 2026-04-25)

| # | Question | Decision |
|---|---|---|
| 1 | Plan source | **(a) `/ship` authors §Plan Digest itself.** Fully self-contained — no `/stage-authoring` prereq. |
| 2 | Code review | **Drop.** No `opus-code-reviewer` dispatch. Verify-loop fix iter covers regression catches. |
| 3 | Audit | **Drop.** N=1 was degenerate. |
| 4 | Close behavior | **(a) Always close inline.** Status flip + archive at end. |
| 5 | Commit policy | **No commit.** User decides when to stage + commit. |
| 6 | Verify cap | **Keep `MAX_ITERATIONS=2`.** No flag. |

**Scope constraint (user, 2026-04-25):** `/ship` is **standalone-task only**. Master-plan-owned tasks must use `/ship-stage`. Step 0 gate rejects any issue whose `stage_id` is non-null.

### B4 Sketch (locked)

```
/ship {ISSUE_ID}

Step 0  CONTEXT + GATE
  - backlog_issue {id} → fetch row
  - REJECT if stage_id non-null (master-plan-owned → must use /ship-stage)
  - REJECT if status ∈ {done, archived}
  - Banner: SHIP {id} — {title}

Step 1  PLAN
  - task_spec_section {id, "§Plan Digest"}
  - If empty / _pending_:
    - Mechanical author from §Goal + §Acceptance + glossary_lookup + invariants_summary
    - task_spec_section_write {id, "§Plan Digest", body}
  - Gate: §Plan Digest non-empty

Step 2  IMPLEMENT
  - Dispatch spec-implementer subagent (reads body via task_spec_body)
  - Gate: IMPLEMENT_DONE marker in subagent output

Step 3  VERIFY
  - Dispatch verify-loop subagent (MAX_ITERATIONS=2)
  - Gate: verdict=pass

Step 4  CLOSE
  - task_status_flip {id, done} → archived_at set automatically
  - NO commit (user stages + commits)
  - Output: SHIP {id} PASSED — {title}. Worktree dirty; commit when ready.
```

### B5 Files to create

- `ia/skills/ship/SKILL.md` — new skill body (replaces current logic).
- `.claude/agents/ship.md` — new agent dispatcher (one new + replace old).
- `.claude/commands/ship.md` — rewrite to dispatch new agent.

Old contents tombstoned to `_retired/` per project convention OR deleted directly (if Phase A removes _retired/ first, then new retirements skip the tombstone step → straight delete).

### B6 Surface impact (after Phase B)

- `opus-code-reviewer` agent + `opus-code-review` skill — keep for `/ship-stage` (cross-task drift scan). `/ship` drops dispatch.
- `opus-auditor` agent + `opus-audit` skill — keep for `/ship-stage` (Stage-scoped §Audit). `/ship` drops dispatch.
- `spec-implementer` agent + `project-spec-implement` skill — keep, dispatched by `/ship` Step 2.
- `verify-loop` agent + skill — keep, dispatched by `/ship` Step 3.

### B7 Files touched in Phase B

**Replace** (rewrite body, no tombstone — Phase A already cleared `_retired/`):

- `.claude/commands/ship.md` — new dispatcher (4 steps, ~80 lines).
- `.claude/agents/ship.md` (new — current `/ship.md` dispatches inline; needs subagent for cache locality).
- `ia/skills/ship/SKILL.md` (new — current logic in `project-spec-ship` retires; replace with mechanical 4-step body).

**Retire (post-Phase-A direct delete, no tombstone):**

- `ia/skills/project-spec-ship/SKILL.md` (current 5-stage skill body).

---

## Findings log

### F1 — `/ship` broken end-to-end (2026-04-25)

**Surfaces:** `.claude/commands/ship.md`, `.claude/agents/spec-implementer.md`, `ia/skills/project-spec-implement/SKILL.md`.

`/ship.md` Step 0 globs `ia/projects/$ARGUMENTS*.md` — directory deleted Step 9.6.11. Step 1 readiness gate reads spec body from filesystem — fails. 14+5 stale path refs in those two files.

Phase B replaces this surface entirely; ref scrub absorbed into B5 file rewrite.

### F2 — 31 retired tombstones live in repo (2026-04-25)

`.claude/agents/_retired/` (13 files) + `.claude/commands/_retired/` (4 files) + `ia/skills/_retired/` (13 dirs / 14 files). All tombstoned per refactor; ready for delete after live-ref scrub.

### F3 — Validators may list retired agent ids (2026-04-25, refined)

Inventory pass (Phase A Step 1) result:

| Validator | Retired ids hardcoded | Status |
|---|---|---|
| `validate-agent-tools.ts` | NONE | clean — `NARROWED_AGENTS` lists 7 active stems |
| `validate-agent-tools-uniformity.ts` | 4 stale (`plan-reviewer`, `stage-file-planner`, `stage-closeout-planner`, `stage-file-applier`) | **stale-but-passing** — validator scans live dir, retired stems silent-skip |
| `validate-cache-block-sizing.ts` | NONE | clean — dynamic dir scan, skips `_retired/` |
| `.claude/settings.json` | NONE | clean — no `agent-tools-allowlist` block |

Only `validate-agent-tools-uniformity.ts` HEAD_AGENTS/TAIL_AGENTS need scrub. Final state after scrub:
- HEAD_AGENTS: `opus-code-reviewer`, `project-new-planner` (2 entries).
- TAIL_AGENTS: `plan-applier`, `project-new-applier` (2 entries).

---

## Decisions log

### D1 — Audit scope excludes durable doc repaint (2026-04-25)

TECH-875 covers stale flat-spec refs in `docs/`, `ARCHITECTURE.md`, `BACKLOG.md`, `web/public/search-index.json`, MCP server source. This audit branch focuses on **executable** lifecycle surfaces.

### D2 — Generated views (`BACKLOG.md`, `BACKLOG-ARCHIVE.md`) stay (2026-04-25)

~30 live refs depend on them. TECH-879 followup. Out of scope.

### D3 — One commit per surface scrub (2026-04-25)

Allows bisect + selective revert. Final `_retired/` delete = one commit.

### D4 — Phase A first, Phase B second (2026-04-25)

User directive: full retirement scrub before new `/ship` design lands. Avoids new ship skill referencing retired surfaces during transition.

### D5 — Historical docs frozen (2026-04-25)

`docs/*-exploration.md`, `docs/*-audit-*.md`, `docs/lifecycle-refactor-*`, `docs/master-plan-execution-friction-log.md`, `docs/ia-dev-db-refactor-implementation.md`, `docs/architecture-audit-*` — all frozen. Refs to retired names left as-is (record of refactor history).

### D6 — `/ship-stage` keeps code-review + audit stages (2026-04-25)

Multi-task Stage shipping retains opus passes for cross-task drift scan. Only `/ship` (single-task) drops them.

---

## Fix plan (Phase A)

1. **Inventory caller-allowlist + validators** — list every retired agent id reachable from validate scripts. Single read pass.
2. **Scrub validators** — one commit `chore(skill-files-audit): drop retired ids from validators`.
3. **Scrub Tier-1** — CLAUDE.md, AGENTS.md, ia/rules/agent-router.md, ia/skills/README.md. One commit.
4. **Scrub templates** — `master-plan-template.md`, `project-spec-template.md`, `project-spec-review-prompt.md`. One commit.
5. **Scrub rules** — `orchestrator-vs-spec.md`, `plan-apply-pair-contract.md`, `project-hierarchy.md`, `unity-scene-wiring.md`. One commit.
6. **Scrub agent-lifecycle + structure docs** — `docs/agent-lifecycle.md`, `docs/PROJECT-SPEC-STRUCTURE.md`, `docs/MASTER-PLAN-STRUCTURE.md`, `docs/mcp-ia-server.md`. One commit (live workflow doc, not historical).
7. **Scrub commands** — one commit per `.claude/commands/*.md` (audit, implement, plan-review, project-new, ship, stage-authoring, stage-file, stage-file-main-session). 8 commits.
8. **Scrub agents** — one commit per `.claude/agents/*.md` (opus-auditor, plan-reviewer-mechanical, plan-reviewer-semantic, project-new-applier, project-new-planner, ship-stage, spec-implementer, stage-authoring, stage-file, verify-loop). 10 commits.
9. **Scrub skills** — one commit per `ia/skills/*/SKILL.md` (~21 files). 21 commits (or batched by family — TBD).
10. **Scrub output style** — `.claude/output-styles/closeout-digest.md`. One commit.
11. **Delete `_retired/` directories** — `rm -rf .claude/agents/_retired/ .claude/commands/_retired/ ia/skills/_retired/`. One commit.
12. **`npm run validate:all` gate** — must pass before Phase B starts.

Estimated commit count: ~50. Compressible to ~15 if grouped by family.

## Fix plan (Phase B)

B3 locked. Sequence after Phase A `validate:all` green:

1. Author new `ia/skills/ship/SKILL.md` body — 4 steps, mechanical, no Opus.
2. Author new `.claude/agents/ship.md` — frontmatter + cache preamble + dispatch wrapper.
3. Rewrite `.claude/commands/ship.md` — minimal dispatcher pointing at new agent.
4. Delete `ia/skills/project-spec-ship/SKILL.md` (old 5-stage body).
5. Run `npm run validate:all` — gate.
6. Smoke test on standalone task (target candidate TBD when first available).

---

## Running checklist (commits)

| Date | SHA | Surface | Note |
|---|---|---|---|
| 2026-04-25 | (uncommitted) | `docs/audit/skill-files-audit.md` | audit scaffold revised — Phase A retirement scrub primary objective |
| 2026-04-25 | (uncommitted) | `docs/audit/skill-files-audit.md` | B3 locked — standalone-only `/ship`, drop review/audit, no commit, keep verify cap=2 |
| 2026-04-25 | (uncommitted) | `docs/audit/skill-files-audit.md` | Phase A Step 1 inventory — only `validate-agent-tools-uniformity.ts` needs scrub |
| 2026-04-25 | (uncommitted) | `tools/scripts/validate-agent-tools-uniformity.ts` | Phase A Step 2 — dropped 4 retired stems from HEAD/TAIL constants. Validator green: 2 heads + 2 tails. |
| 2026-04-25 | (uncommitted) | Tier-1 surfaces (`CLAUDE.md`, `AGENTS.md`, `ia/rules/agent-router.md`, `ia/skills/README.md`) | Phase A Step 3 — scrubbed retired refs: `/closeout`+`/author` row → `/stage-authoring`; closeout pair → inline in `/ship-stage` Pass B; `plan-author, plan-digest` → `stage-authoring`; dropped `plan-digest/` row + 2 tombstone HTML comments + `project-spec-kickoff` example; updated plan-applier Modes (dropped stage-closeout); template footer rewritten. |
| 2026-04-25 | (uncommitted) | Templates (`master-plan-template.md`, `project-spec-template.md`, `project-spec-review-prompt.md`, `plan-digest-section.md`, `frontmatter-schema.md`) | Phase A Step 4 — closeout pair refs → `/ship-stage` Pass B inline; `project-spec-kickoff` → `stage-authoring`; `stage-file-apply` → `stage-file` applier pass; `§Stage Closeout Plan` block tombstoned in master-plan template (HTML comment); tracking legend rewritten to map skills → status flips. |
| 2026-04-25 | (uncommitted) | Rules (`agent-principles.md`, `agent-output-caveman.md`, `agent-output-caveman-authoring.md`, `agent-human-polling.md`, `unity-scene-wiring.md`, `project-hierarchy.md`, `orchestrator-vs-spec.md`, `plan-digest-contract.md`, `plan-apply-pair-contract.md`, `stage-sizing-gate.md`) | Phase A Step 5 — closeout-pair refs → `/ship-stage` Pass B inline; `stage-file-apply`/`stage-file-plan` → `stage-file` applier/planner pass; `plan-author`+`plan-digest` rows folded into `stage-authoring` row in pair contract (dropped seam #4 entirely — closeout no longer a pair seam); `closeout-apply` → DB-backed `ia_tasks.archived_at` via `stage_closeout_apply` MCP; `spec-enricher` (kickoff) → `stage-authoring`; status-flip matrix updated to drop `plan-applier` Mode stage-closeout. |
| 2026-04-25 | (uncommitted) | Workflow + structure docs (`docs/MASTER-PLAN-STRUCTURE.md`, `docs/agent-lifecycle.md`) | Phase A Step 6 — major rewrite. MASTER-PLAN: §3.2 dropped §Stage Audit + §Stage Closeout Plan sentinels; §3.3/§3.4 pair table reduced 4→2 rows; §4 dropped 4.4/4.5; §5 cardinality reduced; §6 status flip owners updated to `stage-file applier pass` / `stage-authoring bulk pass` / `/ship-stage Pass B inline closeout`; §7 lifecycle skill flip matrix collapsed plan-author+plan-digest → `stage-authoring`; §8 guardrails (orchestrators permanent; close inline); §9 validators flag retired subsection reintroduction; §10 cross-ref `4 pair seams` → `3 pair seams`; §11 changelog appended (rev 4 entry). LIFECYCLE: rev 4 header; §1 redrew flow diagram (stage-file planner+applier pass + stage-authoring bulk; ship-stage Pass A/B); §2 seam matrix rows 3,4,5,6,7,8,9,C,C1 rewrote; retired seams paragraph; §2a R1–R6 (dropped R11 audit); §3 handoff contract 9 rows; §4 decision tree (`/ship-stage` answer + `/ship` answer); §6 close seam rewritten ("Pass B inline closeout only"); §7 Crashed-ship-stage recovery (was Crashed-closeout — DB resume gate via `task_state` no flock); §5 row 8 "§Findings legacy /audit R11 retired". |
