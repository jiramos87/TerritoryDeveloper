# Cursor agent — master-plan tasks quick guide

> Legacy note: this document is historical.  
> Canonical operational runbook lives at `docs/cursor-skill-harness.md` (Master-plan task execution rules + tiered rule harness).
>
> Keep this file for traceability of the original gap-phase guide.

**Audience:** Cursor Composer agent working tasks in `territory-developer` during Claude Code gap.
**Purpose:** Pick a task from a master plan, implement it, verify via MCP bridge, commit. No lifecycle slash commands (Cursor does not have `/stage-file`, `/ship`, `/closeout`, etc.).
**Companion:** `docs/cursor-agent-mcp-bridge.md` (bridge + verify surface).

---

## 1. Master-plan doc anatomy

Path: `ia/projects/{slug}-master-plan.md`. Permanent orchestrator — never delete, never close.

Hierarchy (per `ia/rules/project-hierarchy.md`):

```
Step → Stage → Phase → Task
```

- **Step** — large slice (weeks of work). H2 in doc.
- **Stage** — implementation unit (days). H3 under a Step. Has `Status`, `Objectives`, `Exit` criteria, `Tasks` table.
- **Phase** — checkbox bullet inside a Stage. Groups ≥2 related tasks.
- **Task** — one row in the Stage's `Tasks` table. Has `Issue` id (or `_pending_`), `Status`, `Intent`.

Status enum (per cell): `_pending_` → `Draft` → `In Review` → `In Progress` → `Done (archived)`.

---

## 2. Find a task

Two paths:

### 2.1 MCP lookup (preferred)

```
master_plan_next_pending { "plan_path": "ia/projects/blip-master-plan.md" }
```

Returns next `_pending_` or `In Progress` task row. Read `Intent` + `Issue` fields.

### 2.2 Direct read

Open the master-plan doc. Find the first Stage with `Status: In Progress` or `Draft`. Scan its `Tasks` table for rows where `Status ≠ Done (archived)`. Prefer rows with a filed `TECH-XXX` `Issue` id over `_pending_` rows.

---

## 3. Task types

### 3.1 Filed task (has `TECH-XXX` id)

Spec exists at `ia/projects/{TECH-XXX}.md`. Yaml record at `ia/backlog/{TECH-XXX}.yaml`. Full workflow:

1. `backlog_issue {"id": "TECH-XXX"}` — read summary + router hints.
2. `spec_section {"id": "TECH-XXX", "section": "Implementation Plan"}` — load the plan, not the full spec.
3. (Optional) `spec_section` for `Acceptance` + `Test plan` if scope unclear.
4. Implement per the Implementation Plan — minimal diff, no scope creep.
5. Verify — see §5.
6. Commit — see §6.

### 3.2 Pending task (`_pending_` in Issue column)

No filed spec — the Task row's `Intent` paragraph IS the spec. Workflow:

1. Read the Stage's `Objectives` + `Exit` criteria (grounds the scope).
2. Read the Task row `Intent` column in full.
3. Implement per that Intent — treat it as a one-paragraph Implementation Plan.
4. Verify — see §5.
5. Commit — see §6.
6. **Do NOT** create `ia/projects/TECH-XXX.md`, `ia/backlog/TECH-XXX.yaml`, or BACKLOG row. Claude Code retrofits those via `/stage-file` on return.

Note in commit body: `(pending task — retrofit via /stage-file on Claude Code return)`.

---

## 4. Scope discipline

- **Implementation Plan is the contract.** Do not add features, refactors, or abstractions beyond what the plan/intent lists.
- **No scope creep into sibling tasks.** One task = one commit (ideally).
- **Stay inside the Stage's `Exit` criteria.** If the work can't satisfy Exit, stop and flag rather than half-finish.
- **Respect invariants.** Load `invariants_summary` if touching `GridManager` / `HeightMap` / roads / water / cliffs. Invariant #3 (no `FindObjectOfType` in hot loops) + #4 (no new singletons) are the common tripwires.

---

## 5. Verify (Cursor-side substitute for `/verify-loop`)

Full `/verify-loop` runs in Claude Code. Cursor substitute per task:

1. `unity_compile` — must return zero errors. Warnings are OK if pre-existing.
2. `npm run validate:all` (Bash) — IA + frontmatter + fixtures + indices. Must exit 0.
3. Bridge round-trip when behaviorally testable — `unity_bridge_command` + `unity_bridge_get` per `cursor-agent-mcp-bridge.md` §3.
4. If task has an `Acceptance` block that calls out a scenario (e.g. `playmode_load_scenario`), run it via bridge.

**Do NOT** run:
- `validate:dead-project-specs` — expects Claude Code closeout choreography.
- `/verify` / `/verify-loop` — Claude Code only.

Record the verify outcome in the commit body (§6).

---

## 6. Commit per task

One task = one commit. Branch: work directly on `feature/master-plans-1` (parent). Normal commit hygiene.

```
<type>(<scope>): <short summary referencing the task>

- TECH-XXX: <one-line what-changed>
- Stage X.Y Phase Z
- Verify: compile OK, validate:all OK, bridge scenario <name> OK (or N/A)

Co-Authored-By: Cursor Composer 2 <noreply@cursor.com>
```

For `_pending_` tasks (no id yet), reference by master-plan coordinate:

```
feat(blip): implement dsp envelope AHDSR (blip-master-plan T3.2.4)

- blip-master-plan Stage 3.2 Phase 2 T3.2.4 (pending task — retrofit via /stage-file on Claude Code return)
- AHDSR linear/exp per BlipEnvShape; 1 ms min clamp per stage
- Verify: compile OK, validate:all OK, bridge N/A

Co-Authored-By: Cursor Composer 2 <noreply@cursor.com>
```

Don't squash across tasks. Don't bundle unrelated changes.

---

## 7. Do NOT do

- **Do NOT edit `Status` column cells** in master-plan task tables. Status flips belong to `/kickoff`, `/implement`, `/closeout`, `project-stage-close` (Claude Code).
- **Do NOT edit `Issue` column** to add `TECH-XXX`. Id reservation goes through `tools/scripts/reserve-id.sh` under `flock` + yaml file creation + BACKLOG materialize — Claude Code owns that chain.
- **Do NOT delete `ia/projects/TECH-XXX.md`** specs. Closeout chain (`/closeout`) owns deletion + archive move + lessons migration.
- **Do NOT write to `ia/state/id-counter.json`** directly. Invariant #13.
- **Do NOT modify `BACKLOG.md` / `BACKLOG-ARCHIVE.md` / `ia/backlog/*.yaml` / `ia/backlog-archive/*.yaml`.** Generated + gated by Claude Code skills.
- **Do NOT run sibling orchestrator work in parallel.** If two master plans touch shared C# (e.g. `GridManager.cs`), pick one and finish its task before switching.
- **Do NOT close anything.** No `Done (archived)`, no spec delete, no yaml move.

---

## 8. Parallel-work + sibling collision

Scan the target master-plan's front-matter banner for `Sibling orchestrators in flight` warnings. If your task touches a file mentioned there (e.g. `GridManager.cs`, `GameSaveManager.cs`), double-check the sibling orchestrator hasn't filed an overlapping task. When unsure, stop and ask human.

---

## 9. Handoff back to Claude Code

After each task:

1. Commit per §6.
2. Push to `feature/master-plans-1`.
3. Leave `Status` cells untouched. Leave `Issue` cell untouched if pending.
4. On Claude Code return, human runs:
   - `/stage-file ia/projects/{slug}-master-plan.md Stage X.Y` — retrofits ids for pending commits.
   - `/kickoff TECH-XXX` / `/implement TECH-XXX` / `/closeout TECH-XXX` — flips Status through the lifecycle.
   - `/verify-loop` — closed-loop verification + Path A/B.

Your commits land as normal code + get absorbed into the lifecycle bookkeeping on return.

---

## 10. Reference

- `ia/rules/project-hierarchy.md` — step/stage/phase/task semantics + cardinality rule.
- `ia/rules/orchestrator-vs-spec.md` — permanent orchestrator vs temporary spec.
- `ia/rules/invariants.md` — hard invariants (never violate).
- `ia/rules/agent-output-caveman.md` — output style (caveman default + full-English exceptions).
- `docs/cursor-agent-mcp-bridge.md` — MCP bridge tool surface.
- `docs/cursor-composer-4day-plan.md` — 40-task 4-day plan.
- Canonical master plans in flight: `ia/projects/blip-master-plan.md`, `ia/projects/multi-scale-master-plan.md`, `ia/projects/sprite-gen-master-plan.md`, `ia/projects/full-game-mvp-master-plan.md`, `ia/projects/mcp-lifecycle-tools-opus-4-7-audit-master-plan.md`.
