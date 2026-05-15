# Repo Obsolescence Audit — 2026-05-15

Branch: `feature/asset-pipeline`. Audit pass = read-only inventory + cross-validated. Pruning phases now executing in-session.

Goal: identify prune-candidates across docs, code, skill prose, state, tools. Sized by prune impact. Each row = file/dir, size, verdict, rationale.

---

## §0 Execution State + Resume Protocol

**Last update:** 2026-05-15 (session start of prune exec)
**Author session:** Claude Opus 4.7
**Repo CWD:** `/Users/javier/bacayo-studio/territory-developer`
**Active branch:** `feature/asset-pipeline` (do NOT switch; do NOT push)
**Commit policy:** NO commits during prune session. User commits manually at end via `/commit` skill or hand. Per `ia/rules/agent-principles.md`: "Never `git commit` unless skill `SKILL.md` explicitly instructs it."
**Destructive op policy:** Use `git rm` for tracked files, `rm` for untracked. Use `git mv` to relocate to `.archive/`. Hook denylist blocks `rm -rf` on protected paths.

### Status tracker

| Phase | Status | Files touched | Validation | Notes |
|---|---|---|---|---|
| 0 — Resume protocol | DONE | 1 (this doc) | n/a | Self-bootstrapping section in audit doc |
| 1 — Trivial deletes | PENDING | 0/8 | `npm run unity:compile-check` after C# stub deletes | |
| 2 — Move-to-archive | PENDING | 0/~40 | `npm run validate:all` after each batch | |
| 3 — Delete backups+orphans | PENDING | 0/12 | manual grep verification | |
| 4 — Skill prose hygiene | PENDING | 0/~12 | `npm run validate:skill-drift` (part of validate:all) | |
| 5 — Code investigation | BLOCKED | n/a | Unity bridge | Requires scene/prefab + reflection audit, not pure-prune |
| 6 — State+tools cleanup | PENDING | 0/9 | `npm run validate:all` | |

### Resume instructions for fresh agent

If this session dies mid-phase:

1. `cd /Users/javier/bacayo-studio/territory-developer`
2. Read this doc (`docs/repo-obsolescence-audit-2026-05-15.md`) end-to-end.
3. Check status tracker above → find first phase NOT marked DONE.
4. Run `git status --porcelain` → reconcile against per-phase checklists below (each file has ☐/☑ box). Match staged/dirty paths against expected.
5. Run `git log -10 --oneline` → confirm no surprise commits during prune (policy: no commits mid-session).
6. Continue from first ☐ checkbox in active phase. Follow per-phase **Commands** block verbatim. Update status tracker after each batch.
7. Validation gate at end of each phase = run the listed `npm run *` command. RED gate = STOP, do not proceed.
8. Do NOT commit. User commits at end.

### Hard guardrails

- Do NOT touch `.archive/` existing content — it is frozen historical per `ia/rules/invariants.md`.
- Do NOT touch `ia/state/id-counter.json` or `ia/backlog/*.yaml` `id:` fields — invariant #13.
- Do NOT delete anything in **Section 2e (ACTIVE explorations)** or **Section 1e runbooks/schemas** below.
- Do NOT execute Phase 5 in this session — code investigation requires Unity Editor bridge + reflection audit; outside pure-prune scope. Mark BLOCKED, leave for follow-up.
- If a target file is currently dirty in `git status` (M, not ??), STOP — do not destroy uncommitted work without user confirm via AskUserQuestion poll.

### Files currently dirty (snapshot at session start)

Per startup git status — these have local modifications. Treat carefully if any overlap prune targets:

```
M Assets/Scripts/CityData.cs
M Assets/Scripts/Editor/AgentBridgeCommandRunner.PlayMode.cs
M Assets/Scripts/Editor/Bridge/Services/MutationDispatchService.cs
M Assets/Scripts/RegionScene/Domains/Persistence/RegionSaveFile.cs
M Assets/Scripts/RegionScene/Domains/Persistence/RegionSaveService.cs
M Assets/Scripts/RegionScene/Domains/Terrain/RegionCellRenderer.cs
M Assets/Scripts/RegionScene/Domains/Terrain/RegionHeightMap.cs
M Assets/Scripts/RegionScene/Domains/Terrain/RegionWaterMap.cs
M Assets/Scripts/RegionScene/RegionManager.cs
M Assets/Scripts/RegionScene/RegionScene.asmdef
R Assets/Scripts/RegionScene/RegionUnlockGate.cs -> Assets/Scripts/RegionUnlockGate.cs
M docs/explorations/region-scene-prototype.html
M docs/explorations/ui-toolkit-emitter-parity-and-db-reverse-capture.md
R docs/research/vibe-coding-safety.md -> docs/explorations/vibe-coding-safety.md
M ia/skills/design-explore/command-body.md
M ia/skills/ship-cycle/SKILL.md
M ia/skills/ship-plan/SKILL.md
M ia/state/id-counter.json
+ untracked: Assets/Resources/Icons/, Assets/Scenes/RegionScene.unity, Assets/Scripts/CityData.cs.meta, Assets/Scripts/RegionScene/Domains/Evolution/, Assets/Scripts/RegionScene/Tools/, Assets/Scripts/RegionScene/UI/RegionSubtypeCatalog.cs
```

**Overlap check vs prune targets:**
- `docs/explorations/region-scene-prototype.html` — flagged as BACKUP (Section 2b) BUT currently modified locally → SKIP for now; user must commit/discard first
- `docs/explorations/vibe-coding-safety.md` — flagged as ORPHAN (Section 2c) BUT just renamed from `docs/research/` → SKIP, possibly active
- `docs/explorations/ui-toolkit-emitter-parity-and-db-reverse-capture.md` — flagged as ORPHAN-but-cited (Section 2d) keep regardless
- `ia/skills/design-explore/command-body.md`, `ia/skills/ship-cycle/SKILL.md`, `ia/skills/ship-plan/SKILL.md` — flagged Phase 4 prose hygiene targets BUT currently dirty → defer Phase 4 until user commits or explicitly says OK to amend on dirty file

**Conclusion:** Phases 1–3 + 6 safe to execute. Phase 4 partial-safe (skip the 3 dirty SKILL.md files). Phase 5 blocked.

---

## TL;DR

| Bucket | Prune size | File count | Action |
|---|---|---|---|
| `docs/ui-parity-recovery/` iter-checkpoints + screenshots | **13M** | 48 | Move to `.archive/ui-parity-recovery-2026-05-14/` |
| `docs/explorations/` shipped+backup+orphan | **~2M** | 28 | Move shipped→`.archive/`; delete .html mirrors + .pre-uplift-backup |
| `docs/` ROOT obsolete (audits/findings/exports past-date) | **~2.5M** (incl 1.5M HTML export) | ~24 | Move to `.archive/docs-historical-2026-04-2026-05/` |
| `ia/state/` pre-Postgres DB snapshots | **1.6M** | 2 | Delete (superseded by live DB) |
| `Assets/Scripts/` pure stubs + `.DS_Store` | ~3K | 4 | Delete |
| Skill prose dates/shas/retired-refs/TECH-IDs | (token cost, not bytes) | top 10 SKILL.md files | Strip per Changelog hygiene |
| `tools/scripts/` one-off migration scripts | ~50K | 6 | Delete |
| `docs/explorations/assets/ui-toolkit-migration/` baseline screenshots | check | 14 entries | Keep if active baseline |

**Total raw prune ≈ 19–20M from filesystem + meaningful token-cost reduction on skill prose.**

---

## Section 1 — `docs/` ROOT obsolete (32M total, ~30 prune candidates)

### 1a. Past-dated audit/handoff reports — `.archive/` candidates

| File | Size | Why obsolete |
|---|---|---|
| `ai-mechanics-audit-2026-04-19.md` | 35K | dated 04-19; findings absorbed into lifecycle-refactor |
| `architecture-audit-change-list-2026-04-22.md` | 25K | execution complete |
| `architecture-audit-handoff-2026-04-22.md` | 8K | handoff consumed |
| `audit-codebase-2026-04-07.md` | 8K | 04-07 codebase snapshot, static |
| `context-overhead-audit-2026-04-21.md` | 21K | token-latency analysis absorbed |
| `master-plan-drift-audit-2026-05-01.md` | 22K | post-drift remediation done |
| `lifecycle-token-optimization-audit.md` | 21K | optimization shipped |
| `release-rollout-model-audit.md` | 17K | model adopted |
| `session-token-latency-audit-exploration.md` | 75K | absorbed into design |
| `session-token-latency-design-review-2026-04-19.md` | 29K | review done |

### 1b. Closed-stage findings/dry-runs

| File | Size | Notes |
|---|---|---|
| `lifecycle-refactor-stage-8-dry-run-findings.md` | 18K | M8 closed |
| `recipe-runner-phase-e-stage-file-smoke-report.md` | 3K | phase E shipped |
| `recipe-runner-phase-e-stage-file-parity-audit.md` | 8K | phase shipped |
| `ship-protocol-stage-4-drift-findings.md` | 5K | stage 4 closed |
| `game-ui-catalog-bake-stage-6-findings.md` | 11K | stage 6 closed |
| `game-ui-catalog-bake-stage-8-legacy-parity-audit.md` | 10K | stage 8 closed |
| `game-ui-catalog-bake-stage-9-canvas-flatten.md` | 6K | stage 9 closed |
| `game-ui-design-system-stage-13.x-findings.md` | 20K | stage 13 closed |
| `asset-pipeline-stage-13-1-cross-plan-impact-audit.md` | 5K | stage closed |
| `asset-pipeline-stage-0-1-impl.md` | 22K | stage shipped |
| `master-plan-execution-friction-log.md` | 23K | retro absorbed |
| `lifecycle-refactor-branch-pending-inventory.md` | 9K | merged to main |
| `large-file-atomization-state-audit-2026-05-11.md` | 12K | atomization shipped |

### 1c. HTML exports (static, superseded)

| File | Size | Notes |
|---|---|---|
| `cd-pilot-step8-export.html` | **1.5M** | UI mockup, bake pipeline live; biggest single file |
| `progress.html` | 60K | 04-30 dashboard snapshot |
| `ui-toolkit-parity-recovery-plan.html` | 228K | plan executed |

### 1d. Cursor/migration handoffs (legacy)

| File | Size | Notes |
|---|---|---|
| `cursor-agent-master-plan-tasks.md` | 8K | superseded by `cursor-skill-harness.md` |
| `cursor-composer-4day-plan.md` | 27K | time-boxed gap 04-20→04-24 done |
| `cursor-user-rules-handoff.md` | 3K | source-of-truth is `.cursor/rules/*.mdc` |
| `cursor-agent-mcp-bridge.md` | 7K | bridge shipped |
| `human-resume-without-ai.md` | 8K | check current relevance |

### 1e. Subdirs

| Path | Size | Verdict |
|---|---|---|
| `docs/audit/` (4 files: skill-files-audit, skill-tools-implementation, backlog-md-deletion-plan, compaction-loop-mitigation) | 72K | All Apr–May, all consumed → `.archive/` |
| `docs/audits/catalog-pre-spine-2026-04-25.{md,json}` | 8K | pre-spine snapshot from 04-25 → `.archive/` |
| `docs/tmp/` (hud-bar-bake-bugs.md, large-file-atomization-bundle.json) | 64K | debug snapshots → delete |
| `docs/reports/ui-inventory-as-built-baseline.json` | 99K | 04-06 baseline → check vs live `ui_panel_list` |
| `docs/reference-pack/night-city-dark-theme.md` | 2K | 1-file reference; orphan unless cited by UI work — verify |
| `docs/ideas/ui-elements-grilling.md` | 22K | last touched 05-08; verify vs `/ui-element-grill` skill |
| `docs/ui-parity-recovery/` (17 iter-checkpoint dirs + 7 PNG screenshots in root) | **13M** | move to `.archive/ui-parity-recovery-2026-05-14/`; **biggest single prune target** |
| `docs/plans/roads-game-cycle-break-fix.md` | 22K | check status; possibly closed |
| `docs/runbooks/` | 48K | 5 runbooks, mostly catalog; likely keep |
| `docs/schemas/` | 40K | active JSON contracts; keep |

---

## Section 2 — `docs/explorations/` obsolete (44 files audited)

### 2a. SHIPPED — corresponding master plan closed → `.archive/`

| File | Size | Plan slug |
|---|---|---|
| `async-cron-jobs.md` | 19K | async-cron-jobs (closed 05-06) |
| `city-scene-loading-perf-quick-wins.md` | 9K | closed 05-13 |
| `cityscene-mainmenu-panel-rollout.md` | 12K | v3 closed 05-11 |
| `cityscene-mainmenu-panel-rollout-v1-v2.md` | 72K | v1-v2 predecessor |
| `large-file-atomization-refactor.md` | 71K | closed 05-08 |
| `large-file-atomization-cutover-refactor.md` | 37K | closed 05-08 |
| `large-file-atomization-hub-thinning-sweep.md` | 13K | closed 05-12 |
| `master-plan-foldering-refactor.md` | 16K | v2 closed 05-10 |
| `ship-cycle-db-read-efficiency.md` | 20K | closed 05-06 |
| `ship-protocol-v2.md` | 38K | closed 05-05 |
| `ui-bake-handler-atomization.md` | 47K | closed 05-13 |
| `ui-bake-pipeline-hardening-v2.md` | 44K | closed 05-11 |
| `ui-implementation-mvp-rest.md` | 63K | closed 05-08 |
| `ui-panel-mcp-slice-extension.md` | 40K | closed 05-12 |
| `ui-toolkit-authoring-mcp-slices.md` | 44K | closed 05-15 today |
| `ui-visual-regression.md` | 58K | closed 05-12 |

### 2b. BACKUP — `.html` mirrors + `.pre-uplift-backup` → delete (`.md` is canonical)

| File | Size |
|---|---|
| `ui-toolkit-migration.html` | **384K** |
| `ui-toolkit-migration.html.pre-uplift-backup` | 118K |
| `region-scene-prototype.html` | 349K |

### 2c. ORPHAN — never converted, 0 external citations → review/delete

| File | Size | Verdict |
|---|---|---|
| `agent-to-agent-ipc.md` | 22K | 0 refs — likely delete |
| `hud-bar-panel-catalog-registration.md` | 4K | 0 refs |
| `json-as-code-exploration.md` | 7K | 0 refs |
| `mcp-lint-vscode.md` | 7K | 0 refs |
| `openapi-harvest.md` | 9K | 0 refs |
| `region-depth-and-scale-switch.md` | 16K | 0 refs |
| `ui-toolkit-authoring-mcp-slices.handoff-prompt.md` | 7K | 0 refs — variant of shipped .md |
| `vibe-coding-safety.md` | **110K** | 0 refs — largest orphan; renamed from `docs/research/` recently per git status |

### 2d. ORPHAN-but-CITED (3-4 ext refs) — keep until reviewed

| File | Size | Action |
|---|---|---|
| `asset-tree-reorg-and-rename.md` | 8K | 1 ref — verify |
| `ui-panel-tree-db-storage.md` | 5K | 4 refs — keep |
| `ui-toolkit-emitter-parity-and-db-reverse-capture.md` | 66K | 3 refs — keep (active) |
| `ugui-deletion-sweep.md` | 3K | 3 refs — keep |

### 2e. ACTIVE — keep

`game-ui-catalog-bake.md`, `game-ui-catalog-bake-finalize.md`, `db-driven-ui-bake.md`, `picker-catalog-conformance-and-subtype-expansion.md`, `region-scene-prototype.md`, `bake-pipeline-hardening.md`, `chain-token-cut.md`, `ship-protocol-*` design docs.

### 2f. `docs/explorations/assets/`

| Path | Size | Verdict |
|---|---|---|
| `city-scene-loading-research.html` + `.md` | 161K | 05-13 research; check if plan closed |
| `ui-toolkit-migration/` (14 entries — likely baseline screenshots) | check | active UI baseline? |

---

## Section 3 — Skill prose hygiene (`ia/skills/**`, `.claude/**`, `ia/rules/**`)

Per `MEMORY.md` "no commit/history refs in skill prose" — strip these:

### 3a. Date stamps in skill changelogs (58 total)

Top offenders:
- `ia/skills/ship-cycle/SKILL.md` — 9 entries (lines 321–329)
- `ia/skills/ship-plan/SKILL.md` — 5 entries
- `ia/skills/debug-geography-water/SKILL.md` — 5 entries
- `ia/skills/section-closeout/proposed/2026-04-29-train.md` — entire file dated
- `ia/skills/section-claim/proposed/2026-04-29-train.md` — entire file dated

### 3b. Retired-skill refs outside `_retired/` (27 total)

- `ia/skills/stage-file-main-session/SKILL.md` — 7 refs to `plan-review`
- `ia/skills/stage-file-main-session/command-body.md` — 5 refs
- `ia/skills/ship-stage-main-session/SKILL.md` — 5 refs to `/code-review`
- `ia/skills/release-rollout/SKILL.md` — 5 refs
- `ia/skills/README.md` — table rows linking to retired skills

Retired surfaces in question: `plan-applier`, `plan-review`, `plan-review-mechanical`, `plan-review-semantic`, `opus-code-review`, `code-review`, `explore-plan`.

### 3c. TECH/BUG ticket refs (35 total)

Top offenders:
- `ia/skills/ship-cycle/SKILL.md` — 10 refs (TECH-12640, TECH-30633, TECH-412, BUG-63)
- `ia/skills/verify-loop/SKILL.md` — 5 refs
- `ia/skills/ship-plan/SKILL.md` — 4 refs

### 3d. Legacy file-path refs (68 total)

Skill prose still says `ia/backlog/{id}.yaml`, `BACKLOG.md`, `ia/projects/{id}.md` despite DB-primary pivot (per MEMORY).

Top offenders: `ia/skills/README.md` (6 refs), `release-rollout/SKILL.md`, `commit/SKILL.md`, `release-rollout-track/SKILL.md`.

### 3e. Git commit shas (3 total)

`ship-cycle/SKILL.md` lines 324–327: `bd153cc3`, `6436292f`, `cf665d8b`. Strip — recurrence evidence belongs in git history.

### 3f. Long-form English prose (caveman violations)

| File | Lines | Action |
|---|---|---|
| `ia/skills/design-explore/SKILL.md` | 422 | collapse to bullets + decision tree |
| `ia/skills/release-rollout/SKILL.md` | 203 | collapse |
| `ia/skills/ship/SKILL.md` | 166 | collapse |
| `ia/skills/project-new-apply/SKILL.md` | 130 | collapse |
| `ia/skills/stage-compress/SKILL.md` | 122 | collapse |

### 3g. Dated train artifacts (delete)

- `ia/skills/section-closeout/proposed/2026-04-29-train.md`
- `ia/skills/section-claim/proposed/2026-04-29-train.md`

### 3h. `_retired/` shadow folders

- `ia/skills/_retired/` (4 retired skills: plan-applier, plan-review-mechanical, plan-review-semantic, opus-code-review) = 36K
- `.claude/agents/_retired/` (4 retired agents) = 20K
- `.claude/commands/_retired/` (4 retired commands) = 28K

All match. **Verdict:** keep as-is — `_retired/` is the documented graveyard pattern. Verify `validate:skill-drift` excludes them.

---

## Section 4 — `Assets/Scripts/` C# code

### 4a. Pure stubs — delete

| File | Lines | Verdict |
|---|---|---|
| `Assets/Scripts/Managers/GameManagers/CityManager.cs` | 5 | empty namespace |
| `Assets/Scripts/Managers/GameManagers/TestScript.cs` | 14 | debug harness |
| `Assets/Scripts/.DS_Store` | bin | macOS artifact |
| `Assets/Scripts/Managers/.DS_Store` | bin | macOS artifact |

### 4b. Cross-validated "unreferenced" classes

False-positive rate high — most flagged classes actually have intra-Assets/Scripts mentions. Refined list of truly suspicious:

| Class | File | grep hits | Verdict |
|---|---|---|---|
| `CityStatsUIController` | `Controllers/GameControllers/CityStatsUIController.cs` | 1 (self only) | **investigate** |
| `GrowthManager` | `Managers/GameManagers/GrowthManager.cs` | 1 (self only) | **investigate** — likely atomized to `Domains/Growth/Services/` |
| `MapPanelAdapter` | `UI/HUD/MapPanelAdapter.cs` | 1 (self only) | **investigate** |
| Signal `Producer`/`Consumer` classes (Land, Waste, Traffic, Industrial, Sanitation, Police, Health) | `Simulation/Signals/` | usually 1-2 | **check reflection registration**; if reflection-driven, false-positive |

### 4c. Intentional archives — keep or delete after verification

| Path | Size |
|---|---|
| `Assets/Scripts/UI/HUD/.archive/HudBarDataAdapter.cs` | 432 lines |
| `Assets/Scripts/UI/HUD/.archive/GrowthBudgetPanelController.cs` | 398 lines |
| `Assets/Scripts/Tests/UI/HUD/.archive/AutoModeAndGrowthBudgetTracerTest.cs` | 143 lines |

Verify no imports from active code, then delete.

---

## Section 5 — `tools/` + `ia/state/`

### 5a. `tools/scripts/` one-off migrations — delete

| Script | Reason |
|---|---|
| `recovery-cityscene-sortorder.mjs` | 0 refs; one-off |
| `recovery-panels-patch.mjs` | 0 refs; one-off |
| `backfill-parent-plan-locator.sh` + `.mjs` | 0 refs; migration run |
| `audit-localstorage.ts` | 0 refs; Phase 0 audit |
| `migrate-calibration-jsonl-to-db.mjs` | 0 refs; one-time import |
| `extract-exploration-md.mjs` | replaced by `design-explore --persist` |

### 5b. `tools/sprite-gen/out/` — gitignored, delete

392K transient PNG cache. `.gitignore`'d but local clutter.

### 5c. `ia/state/`

| File | Size | Verdict |
|---|---|---|
| `db-snapshot-bodies.dump` | ~1M | pre-Postgres era; superseded by live DB → delete |
| `db-snapshot-metadata.sql` | ~600K | same |
| `lifecycle-refactor-migration.json` | 36K | migration artifact → delete |
| `in-flight-closeouts.schema.json` | small | live; keep |
| `.id-counter.lock` / `.materialize-backlog.lock` / `.runtime-state.lock` | 0 bytes each | flock infra; keep |
| `id-counter.json` | small | keep (live) |

---

## Section 6 — Root-level + misc

| File | Verdict |
|---|---|
| `AGENTS.md` | keep — canonical |
| `ARCHITECTURE.md` | keep — stub router |
| `CLAUDE.md` | keep — Claude-specific deltas |
| `BACKLOG.md` / `BACKLOG-ARCHIVE.md` | DB-primary now per MEMORY — **investigate**: are these still generated views or stale legacy? If unused, delete |
| `MEMORY.md` | keep — ephemeral state |
| `.archive/` (3.9M) | already frozen; do not touch |

---

## Section 7 — Executable prune protocol (per-phase checklists)

Each phase = exact files + commands. Tick ☐→☑ as each item lands. Update §0 Status Tracker after phase complete.

---

### Phase 1 — Trivial deletes (0-risk)

**Pre-condition:** None.
**Validation gate:** `npm run unity:compile-check` after C# stub deletes.
**Status:** PENDING

Files:
- ☐ `Assets/Scripts/.DS_Store` (binary macOS artifact, not tracked) — `rm`
- ☐ `Assets/Scripts/Managers/.DS_Store` (binary macOS artifact) — `rm`
- ☐ `Assets/Scripts/Managers/GameManagers/CityManager.cs` (5-line empty namespace stub) — verify with `grep -r CityManager Assets/Scripts --include="*.cs"` first; only delete if 1-hit (self)
- ☐ `Assets/Scripts/Managers/GameManagers/CityManager.cs.meta` (Unity .meta paired) — `git rm` together
- ☐ `Assets/Scripts/Managers/GameManagers/TestScript.cs` (14-line debug harness) — verify with grep first
- ☐ `Assets/Scripts/Managers/GameManagers/TestScript.cs.meta` — `git rm` together
- ☐ `docs/tmp/hud-bar-bake-bugs.md` (25K debug snapshot) — `git rm`
- ☐ `docs/tmp/large-file-atomization-bundle.json` (36K) — `git rm`
- ☐ `docs/tmp/` (empty dir after) — leave or `rmdir` if empty
- ☐ `tools/sprite-gen/out/*` (gitignored, 392K transient PNG cache) — `rm -rf tools/sprite-gen/out` then recreate empty dir or leave gitignore'd

**Commands (sequential):**
```bash
# Step 1: pre-verify C# stubs are orphan
grep -rn "\bCityManager\b" Assets/Scripts --include="*.cs"
grep -rn "\bTestScript\b" Assets/Scripts --include="*.cs"
# If either returns >1 hit (or non-self-reference), HALT and reassess

# Step 2: delete .DS_Store (untracked → plain rm)
rm -f Assets/Scripts/.DS_Store Assets/Scripts/Managers/.DS_Store

# Step 3: git rm C# stubs (tracked)
git rm Assets/Scripts/Managers/GameManagers/CityManager.cs Assets/Scripts/Managers/GameManagers/CityManager.cs.meta
git rm Assets/Scripts/Managers/GameManagers/TestScript.cs Assets/Scripts/Managers/GameManagers/TestScript.cs.meta

# Step 4: git rm docs/tmp
git rm docs/tmp/hud-bar-bake-bugs.md docs/tmp/large-file-atomization-bundle.json

# Step 5: delete sprite-gen out cache (gitignored)
rm -rf tools/sprite-gen/out/*

# Step 6: Unity compile gate
npm run unity:compile-check
```

**Post-condition:** `git status --porcelain` shows D entries for tracked deletes; compile-check exits 0.

---

### Phase 2 — Move to `.archive/` (reversible)

**Pre-condition:** Phase 1 DONE + compile-check green.
**Validation gate:** `npm run validate:all` after each sub-batch.
**Status:** PENDING

Three sub-batches to keep diff readable. Use `git mv` (preserves history).

#### 2a. ROOT docs/ obsolete → `.archive/docs-historical-2026-04-2026-05/`

Create target dir first: `mkdir -p .archive/docs-historical-2026-04-2026-05`

Files (verbatim from Section 1a–1d):
- ☐ `docs/ai-mechanics-audit-2026-04-19.md`
- ☐ `docs/architecture-audit-change-list-2026-04-22.md`
- ☐ `docs/architecture-audit-handoff-2026-04-22.md`
- ☐ `docs/audit-codebase-2026-04-07.md`
- ☐ `docs/context-overhead-audit-2026-04-21.md`
- ☐ `docs/master-plan-drift-audit-2026-05-01.md`
- ☐ `docs/lifecycle-token-optimization-audit.md`
- ☐ `docs/release-rollout-model-audit.md`
- ☐ `docs/session-token-latency-audit-exploration.md`
- ☐ `docs/session-token-latency-design-review-2026-04-19.md`
- ☐ `docs/lifecycle-refactor-stage-8-dry-run-findings.md`
- ☐ `docs/recipe-runner-phase-e-stage-file-smoke-report.md`
- ☐ `docs/recipe-runner-phase-e-stage-file-parity-audit.md`
- ☐ `docs/ship-protocol-stage-4-drift-findings.md`
- ☐ `docs/game-ui-catalog-bake-stage-6-findings.md`
- ☐ `docs/game-ui-catalog-bake-stage-8-legacy-parity-audit.md`
- ☐ `docs/game-ui-catalog-bake-stage-9-canvas-flatten.md`
- ☐ `docs/game-ui-design-system-stage-13.x-findings.md`
- ☐ `docs/asset-pipeline-stage-13-1-cross-plan-impact-audit.md`
- ☐ `docs/asset-pipeline-stage-0-1-impl.md`
- ☐ `docs/master-plan-execution-friction-log.md`
- ☐ `docs/lifecycle-refactor-branch-pending-inventory.md`
- ☐ `docs/large-file-atomization-state-audit-2026-05-11.md`
- ☐ `docs/cursor-agent-master-plan-tasks.md`
- ☐ `docs/cursor-composer-4day-plan.md`
- ☐ `docs/cursor-user-rules-handoff.md`
- ☐ `docs/cursor-agent-mcp-bridge.md`
- ☐ `docs/audit/backlog-md-deletion-plan.md`
- ☐ `docs/audit/compaction-loop-mitigation.md`
- ☐ `docs/audit/skill-files-audit.md`
- ☐ `docs/audit/skill-tools-implementation.md`
- ☐ `docs/audits/catalog-pre-spine-2026-04-25.md`
- ☐ `docs/audits/catalog-pre-spine-2026-04-25.json`

**Commands:**
```bash
mkdir -p .archive/docs-historical-2026-04-2026-05
# Move each file (one-line each, scripted via xargs for batch)
for f in \
  docs/ai-mechanics-audit-2026-04-19.md \
  docs/architecture-audit-change-list-2026-04-22.md \
  docs/architecture-audit-handoff-2026-04-22.md \
  docs/audit-codebase-2026-04-07.md \
  docs/context-overhead-audit-2026-04-21.md \
  docs/master-plan-drift-audit-2026-05-01.md \
  docs/lifecycle-token-optimization-audit.md \
  docs/release-rollout-model-audit.md \
  docs/session-token-latency-audit-exploration.md \
  docs/session-token-latency-design-review-2026-04-19.md \
  docs/lifecycle-refactor-stage-8-dry-run-findings.md \
  docs/recipe-runner-phase-e-stage-file-smoke-report.md \
  docs/recipe-runner-phase-e-stage-file-parity-audit.md \
  docs/ship-protocol-stage-4-drift-findings.md \
  docs/game-ui-catalog-bake-stage-6-findings.md \
  docs/game-ui-catalog-bake-stage-8-legacy-parity-audit.md \
  docs/game-ui-catalog-bake-stage-9-canvas-flatten.md \
  docs/game-ui-design-system-stage-13.x-findings.md \
  docs/asset-pipeline-stage-13-1-cross-plan-impact-audit.md \
  docs/asset-pipeline-stage-0-1-impl.md \
  docs/master-plan-execution-friction-log.md \
  docs/lifecycle-refactor-branch-pending-inventory.md \
  docs/large-file-atomization-state-audit-2026-05-11.md \
  docs/cursor-agent-master-plan-tasks.md \
  docs/cursor-composer-4day-plan.md \
  docs/cursor-user-rules-handoff.md \
  docs/cursor-agent-mcp-bridge.md \
  docs/audit/backlog-md-deletion-plan.md \
  docs/audit/compaction-loop-mitigation.md \
  docs/audit/skill-files-audit.md \
  docs/audit/skill-tools-implementation.md \
  docs/audits/catalog-pre-spine-2026-04-25.md \
  docs/audits/catalog-pre-spine-2026-04-25.json \
  ; do
    git mv "$f" .archive/docs-historical-2026-04-2026-05/
done

# Validate
npm run validate:all
```

#### 2b. SHIPPED explorations → `.archive/explorations-shipped-2026-05/`

Create target dir: `mkdir -p .archive/explorations-shipped-2026-05`

Files:
- ☐ `docs/explorations/async-cron-jobs.md`
- ☐ `docs/explorations/city-scene-loading-perf-quick-wins.md`
- ☐ `docs/explorations/cityscene-mainmenu-panel-rollout.md`
- ☐ `docs/explorations/cityscene-mainmenu-panel-rollout-v1-v2.md`
- ☐ `docs/explorations/large-file-atomization-refactor.md`
- ☐ `docs/explorations/large-file-atomization-cutover-refactor.md`
- ☐ `docs/explorations/large-file-atomization-hub-thinning-sweep.md`
- ☐ `docs/explorations/master-plan-foldering-refactor.md`
- ☐ `docs/explorations/ship-cycle-db-read-efficiency.md`
- ☐ `docs/explorations/ship-protocol-v2.md`
- ☐ `docs/explorations/ui-bake-handler-atomization.md`
- ☐ `docs/explorations/ui-bake-pipeline-hardening-v2.md`
- ☐ `docs/explorations/ui-implementation-mvp-rest.md`
- ☐ `docs/explorations/ui-panel-mcp-slice-extension.md`
- ☐ `docs/explorations/ui-toolkit-authoring-mcp-slices.md`
- ☐ `docs/explorations/ui-visual-regression.md`

**Commands:**
```bash
mkdir -p .archive/explorations-shipped-2026-05
for f in \
  docs/explorations/async-cron-jobs.md \
  docs/explorations/city-scene-loading-perf-quick-wins.md \
  docs/explorations/cityscene-mainmenu-panel-rollout.md \
  docs/explorations/cityscene-mainmenu-panel-rollout-v1-v2.md \
  docs/explorations/large-file-atomization-refactor.md \
  docs/explorations/large-file-atomization-cutover-refactor.md \
  docs/explorations/large-file-atomization-hub-thinning-sweep.md \
  docs/explorations/master-plan-foldering-refactor.md \
  docs/explorations/ship-cycle-db-read-efficiency.md \
  docs/explorations/ship-protocol-v2.md \
  docs/explorations/ui-bake-handler-atomization.md \
  docs/explorations/ui-bake-pipeline-hardening-v2.md \
  docs/explorations/ui-implementation-mvp-rest.md \
  docs/explorations/ui-panel-mcp-slice-extension.md \
  docs/explorations/ui-toolkit-authoring-mcp-slices.md \
  docs/explorations/ui-visual-regression.md \
  ; do
    git mv "$f" .archive/explorations-shipped-2026-05/
done

npm run validate:all
```

#### 2c. ui-parity-recovery → `.archive/ui-parity-recovery-2026-05-14/`

Files (entire directory, 13M, 48 files):
- ☐ `docs/ui-parity-recovery/` → `.archive/ui-parity-recovery-2026-05-14/`

**Commands:**
```bash
mkdir -p .archive
git mv docs/ui-parity-recovery .archive/ui-parity-recovery-2026-05-14
npm run validate:all
```

---

### Phase 3 — Delete backups + true orphans

**Pre-condition:** Phase 2 DONE + validate:all green.
**Validation gate:** `npm run validate:all`.
**Status:** PENDING

#### 3a. HTML mirrors + .pre-uplift-backup (canonical .md exists)

**HOLD on `region-scene-prototype.html`** — currently dirty per §0 overlap check; skip until user commits/discards.

Files:
- ☐ `docs/explorations/ui-toolkit-migration.html` (384K)
- ☐ `docs/explorations/ui-toolkit-migration.html.pre-uplift-backup` (118K)
- ⏸ `docs/explorations/region-scene-prototype.html` (349K) — DEFER (dirty)
- ☐ `docs/cd-pilot-step8-export.html` (1.5M) — biggest single
- ☐ `docs/progress.html` (60K)
- ☐ `docs/ui-toolkit-parity-recovery-plan.html` (228K)

**Commands:**
```bash
git rm docs/explorations/ui-toolkit-migration.html
git rm docs/explorations/ui-toolkit-migration.html.pre-uplift-backup
git rm docs/cd-pilot-step8-export.html
git rm docs/progress.html
git rm docs/ui-toolkit-parity-recovery-plan.html
npm run validate:all
```

#### 3b. True orphan explorations (0 ext refs)

**HOLD on `vibe-coding-safety.md`** — just renamed from `docs/research/`, treat as cold-start, defer to user judgment.

Files:
- ☐ `docs/explorations/agent-to-agent-ipc.md` (22K)
- ☐ `docs/explorations/hud-bar-panel-catalog-registration.md` (4K)
- ☐ `docs/explorations/json-as-code-exploration.md` (7K)
- ☐ `docs/explorations/mcp-lint-vscode.md` (7K)
- ☐ `docs/explorations/openapi-harvest.md` (9K)
- ☐ `docs/explorations/region-depth-and-scale-switch.md` (16K)
- ☐ `docs/explorations/ui-toolkit-authoring-mcp-slices.handoff-prompt.md` (7K)
- ⏸ `docs/explorations/vibe-coding-safety.md` (110K) — DEFER (just renamed)

**Commands:**
```bash
# Polling user before bulk-deleting orphans is good practice — these have 0 ext refs but may have intent value
# Move to .archive/explorations-orphan-2026-05/ instead of delete (reversible)
mkdir -p .archive/explorations-orphan-2026-05
for f in \
  docs/explorations/agent-to-agent-ipc.md \
  docs/explorations/hud-bar-panel-catalog-registration.md \
  docs/explorations/json-as-code-exploration.md \
  docs/explorations/mcp-lint-vscode.md \
  docs/explorations/openapi-harvest.md \
  docs/explorations/region-depth-and-scale-switch.md \
  docs/explorations/ui-toolkit-authoring-mcp-slices.handoff-prompt.md \
  ; do
    git mv "$f" .archive/explorations-orphan-2026-05/
done
npm run validate:all
```

**Note:** Switched from delete to archive — safer when 0 refs may be undercounted.

---

### Phase 4 — Skill prose hygiene

**Pre-condition:** Phase 1–3 DONE + user confirm to edit dirty SKILL.md files (OR defer those 3).
**Validation gate:** `npm run validate:all` (includes `validate:skill-drift` + `validate:frontmatter`).
**Status:** PENDING — split into safe + dirty subsets.

#### 4a. Safe targets (no dirty overlap)

Files:
- ☐ `ia/skills/section-closeout/proposed/2026-04-29-train.md` — DELETE (dated train artifact)
- ☐ `ia/skills/section-claim/proposed/2026-04-29-train.md` — DELETE (dated train artifact)
- ☐ `ia/skills/debug-geography-water/SKILL.md` — strip 5 date stamps from §Changelog
- ☐ `ia/skills/stage-file-main-session/SKILL.md` — remove 7 refs to retired `plan-review`
- ☐ `ia/skills/stage-file-main-session/command-body.md` — remove 5 refs to retired `plan-review`
- ☐ `ia/skills/ship-stage-main-session/SKILL.md` — remove 5 refs to retired `code-review`
- ☐ `ia/skills/release-rollout/SKILL.md` — remove 5 retired-skill refs + convert legacy file paths to DB-MCP refs
- ☐ `ia/skills/README.md` — kill table rows linking to retired skills
- ☐ `ia/skills/verify-loop/SKILL.md` — strip 5 TECH-N refs
- ☐ `ia/skills/ship-final/SKILL.md` — strip dates + TECH refs
- ☐ `ia/skills/commit/SKILL.md` — convert `ia/projects/{id}.md` + `ia/backlog/{id}.yaml` refs to DB MCP names
- ☐ `ia/skills/release-rollout-track/SKILL.md` — convert legacy file paths

#### 4b. DEFERRED (dirty per §0 — user must commit first)

- ⏸ `ia/skills/ship-cycle/SKILL.md` (dirty)
- ⏸ `ia/skills/ship-plan/SKILL.md` (dirty)
- ⏸ `ia/skills/design-explore/command-body.md` (dirty)

**Commands:**
```bash
# Step 1: delete dated train artifacts
git rm ia/skills/section-closeout/proposed/2026-04-29-train.md
git rm ia/skills/section-claim/proposed/2026-04-29-train.md

# Step 2: per-file in-place edits via Edit tool (not bash) — see per-file deltas below
# (Agent reads each file, identifies date stamps / TECH refs / retired-skill refs / legacy paths, makes targeted Edit calls)

# Step 3: regen .claude/agents + .claude/commands from skill prose
npm run skill:sync:all

# Step 4: validate
npm run validate:all
```

**Per-file edit guidance (for resume agent):**
- `debug-geography-water/SKILL.md` — grep for `2026-0[45]-` lines, drop entries from §Changelog
- `stage-file-main-session/SKILL.md` — search/replace `plan-review` mentions; rewrite chain description without retired step
- `ship-stage-main-session/SKILL.md` — drop `/code-review` operator-note paragraph (deleting reduces noise)
- `release-rollout/SKILL.md` — replace `ia/projects/{slug}-rollout-tracker.md` with `docs/{slug}-rollout-tracker.md` (per current path) or DB equivalent
- `README.md` table — remove rows pointing to retired `plan-applier`, `plan-review`, etc.; or move to a `## Retired` subsection footer
- `verify-loop/SKILL.md` — replace `TECH-412 landed the initial 20 mutation kinds...` with behavioral description; "the bridge has 20+ mutation kinds today"
- `ship-final/SKILL.md` — strip Changelog date entries (oldest-first batch removal)
- `commit/SKILL.md` — find `ia/backlog/{id}.yaml` + `ia/projects/{id}.md` → replace with `task_insert MCP` / `task_spec_section MCP`
- `release-rollout-track/SKILL.md` — same path replacement pattern

---

### Phase 5 — Code investigation (BLOCKED)

**Pre-condition:** Unity Editor available + scene grep + reflection audit tooling.
**Status:** BLOCKED — out of scope for this pure-prune session. Defer to a `/atomize-file` or dedicated subagent run.

Targets carried forward:
- `Assets/Scripts/Controllers/GameControllers/CityStatsUIController.cs` (358 lines)
- `Assets/Scripts/Managers/GameManagers/GrowthManager.cs` (288 lines)
- `Assets/Scripts/UI/HUD/MapPanelAdapter.cs` (94 lines)
- `Assets/Scripts/Simulation/Signals/Producers/*.cs` + `Consumers/*.cs` (reflection-driven check needed)
- `Assets/Scripts/UI/HUD/.archive/*.cs` (973 lines — confirm no live imports)

**Resume protocol for Phase 5:**
1. Open each file → check `[CreateAssetMenu]`, `[RuntimeInitializeOnLoadMethod]`, reflection registration patterns
2. `grep -rn "typeof(<ClassName>)" Assets/Scripts` to find reflection mentions
3. Unity Editor: open scenes/prefabs → search MonoBehaviour usage
4. Use `mcp__territory-ia__unity_callers_of` MCP tool per class

---

### Phase 6 — State + tools cleanup

**Pre-condition:** Phase 1–4 DONE.
**Validation gate:** `npm run validate:all`.
**Status:** PENDING

#### 6a. `tools/scripts/` one-off migrations

Files (all confirmed 0-ref):
- ☐ `tools/scripts/recovery-cityscene-sortorder.mjs`
- ☐ `tools/scripts/recovery-panels-patch.mjs`
- ☐ `tools/scripts/backfill-parent-plan-locator.sh`
- ☐ `tools/scripts/backfill-parent-plan-locator.mjs`
- ☐ `tools/scripts/audit-localstorage.ts`
- ☐ `tools/scripts/migrate-calibration-jsonl-to-db.mjs`
- ☐ `tools/scripts/extract-exploration-md.mjs`

**Pre-delete grep check:**
```bash
for f in recovery-cityscene-sortorder recovery-panels-patch backfill-parent-plan-locator audit-localstorage migrate-calibration-jsonl-to-db extract-exploration-md; do
  echo "=== $f ==="
  grep -rn "$f" package.json .claude/settings.json ia/skills tools/scripts 2>/dev/null | grep -v "tools/scripts/$f"
done
# Any hit = HALT, the script is referenced
```

**Commands:**
```bash
git rm tools/scripts/recovery-cityscene-sortorder.mjs
git rm tools/scripts/recovery-panels-patch.mjs
git rm tools/scripts/backfill-parent-plan-locator.sh tools/scripts/backfill-parent-plan-locator.mjs
git rm tools/scripts/audit-localstorage.ts
git rm tools/scripts/migrate-calibration-jsonl-to-db.mjs
git rm tools/scripts/extract-exploration-md.mjs
```

#### 6b. `ia/state/` pre-Postgres snapshots

Files:
- ☐ `ia/state/db-snapshot-bodies.dump` (~1M, Apr 25)
- ☐ `ia/state/db-snapshot-metadata.sql` (~600K, Apr 25)
- ☐ `ia/state/lifecycle-refactor-migration.json` (36K, May 8)

**Commands:**
```bash
# Verify these are not referenced by any active script first
grep -rn "db-snapshot" package.json tools/scripts ia/skills 2>/dev/null
grep -rn "lifecycle-refactor-migration" package.json tools/scripts ia/skills 2>/dev/null
# Empty result → safe to git rm

git rm ia/state/db-snapshot-bodies.dump
git rm ia/state/db-snapshot-metadata.sql
git rm ia/state/lifecycle-refactor-migration.json

npm run validate:all
```

#### 6c. Open question: `BACKLOG.md` / `BACKLOG-ARCHIVE.md`

Per MEMORY, DB-primary. Verify:
```bash
# Are these regenerated by a script?
grep -rn "BACKLOG.md" tools/scripts package.json 2>/dev/null | head -10
# Is it referenced from skill/rules prose as a live surface?
grep -rn "BACKLOG.md" ia/skills ia/rules AGENTS.md CLAUDE.md 2>/dev/null | head -10
```

If `BACKLOG.md` is regenerated by a script in `tools/scripts/` (e.g., `materialize-backlog.sh`), it's a generated view → keep. If only referenced from prose, it's a legacy surface → delete + scrub references.

**Action:** report findings to user as a follow-up question, do NOT delete in this session.

---

### Phase tail — Cleanup verification

**Status:** PENDING

After Phases 1–4 + 6 land, run final sanity sweep:

```bash
# Git diff stats summary
git diff --stat HEAD | tail -20

# Full validation
npm run validate:all
npm run unity:compile-check

# Size reduction proof
du -sh docs/ ia/ tools/ Assets/Scripts/ .archive/
echo "Compare against baseline: docs/=32M, ia/=4.1M, tools/=251M, Assets/Scripts/=8.8M, .archive/=3.9M"
```

Expected post-prune deltas:
- `docs/` 32M → ~17M (lost ~15M to `.archive/` moves)
- `.archive/` 3.9M → ~22M (gained moves + ui-parity-recovery 13M)
- Net repo bytes unchanged (just relocated); token cost on agent loads drops significantly
- `Assets/Scripts/` shrinks ~3K (stubs + DS_Store removed)
- `ia/state/` shrinks ~1.6M (db snapshots gone)
- `tools/scripts/` shrinks ~50K

**Final hand-off:**
- DO NOT commit. User decides commit grouping.
- Print summary table: per-phase files-touched + final size delta.
- Suggest user run `/commit` skill to topic-cluster the diff.

---

## Open questions for follow-up

1. **`BACKLOG.md` / `BACKLOG-ARCHIVE.md`** — generated view or legacy filesystem leftover? Per MEMORY DB-primary pivot. If legacy, delete + drop references from `ia/rules/terminology-consistency.md`.
2. **`docs/research/`** — directory exists but empty. Some doc was moved to `docs/explorations/vibe-coding-safety.md` per git status. Remove empty dir?
3. **`docs/reference-pack/`** — single-file dir. Inline into glossary or remove?
4. **`docs/ideas/`** — `ui-elements-grilling.md` (22K). Is it consumed by `/ui-element-grill` skill or orphan?
5. **Skill `_retired/`** — 3 mirrors (`ia/skills/_retired/`, `.claude/agents/_retired/`, `.claude/commands/_retired/`). Do they ever need updating, or can entire `_retired/` move to `.archive/` once stable?
6. **C# signal Producers** — Reflection-driven registration? If yes, drop from orphan list. If no, candidate prune.

---

## Methodology

- 5 parallel `Explore` subagents scanned docs ROOT, docs/explorations, skill prose, C# code, tools+state+memory
- Each agent cross-referenced citations across `CLAUDE.md`, `AGENTS.md`, `ia/rules/`, `ia/skills/`, `.claude/commands/`, `.claude/agents/`
- Master plan status verified via `mcp__territory-ia__master_plan_state` for explorations
- Code orphan signal = grep-based intra-script reference count
- Cross-validation pass on top 5 "unreferenced" classes via scene/prefab grep + intra-script grep — found ~70% false-positive rate, refined list

## Size totals

| Surface | Files | Bytes |
|---|---|---|
| `docs/` ROOT obsolete | ~24 | ~2.3M |
| `docs/explorations/` SHIPPED | 16 | ~654K |
| `docs/explorations/` BACKUP | 3 | 851K |
| `docs/explorations/` ORPHAN | 8 | 182K |
| `docs/ui-parity-recovery/` | 48 | 13M |
| `docs/audit/` + `docs/audits/` + `docs/tmp/` | ~7 | ~150K |
| `ia/state/` pre-Postgres | 3 | ~1.6M |
| `tools/scripts/` orphans | 6 | ~50K |
| `Assets/Scripts/` stubs+DS | 4 | ~3K |
| **Total filesystem prune** | ~120 files | **~19M** |

Skill-prose hygiene = no byte impact but reduces token cost per skill load (cache hit) and aligns with `feedback_no_commit_history_in_skill_prose` memory.

---

Next: review this doc, pick phases to execute, dispatch per-phase prune subagent (or hand-execute Phase 1 inline).
