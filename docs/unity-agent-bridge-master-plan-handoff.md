# Handoff — Unity Agent Bridge master plan

> **Target outcome:** File a new TECH issue for the Unity Agent Bridge program (per `docs/unity-ide-agent-bridge-analysis.md` §7.2), then author `ia/projects/unity-agent-bridge-master-plan.md` via `/master-plan-new` consuming the same analysis doc.
>
> **Run from:** `/Users/javier/bacayo-studio/territory-developer` (territory-ia MCP + `.claude/commands/` + `ia/skills/` all need to be loaded).
>
> **Caveman** applies. Standard exceptions: code, commits, security/auth, structured MCP payloads, orchestrator header prose.

---

## 1. Context — what's already shipped, what's missing

**Already shipped (no rework needed):**

- **Phase 1 bridge MCP tools exist:** `unity_bridge_command`, `unity_bridge_get` in `tools/mcp-ia-server/src/` — backed by Postgres `agent_bridge_job` queue. See analysis doc §5 + §8.1.
- **Unity side:** `AgentBridgeCommandRunner.cs` already present under `Assets/Scripts/Editor/` per BACKLOG TECH-83 Files field.
- **Skills:** `ia/skills/ide-bridge-evidence/SKILL.md` documents current evidence-capture contract.

**Missing (this handoff's job):**

- **No TECH backlog row** owning the bridge as a program (analysis doc §7.2 flagged this, never filed).
- **No master plan** orchestrating analysis doc §10 tiers B/C/D (hardening → HTTP transport → streaming + screenshots). TECH-83 drags in bridge scope without an orchestrator.

**Existing filed specs that touch bridge (Draft — do NOT reopen or block on):**

- **TECH-83** (see [`BACKLOG.md`](../BACKLOG.md)) — Agent-driven simulation parameter tuning; names `AgentBridgeCommandRunner.cs` as new bridge-command target. Bridge-adjacent consumer, not the program itself.
- **TECH-78** (see [`BACKLOG.md`](../BACKLOG.md)) — Skill chaining engine (`suggest_skill_chain` MCP tool). Agent-tooling sibling.
- **TECH-251** (see [`BACKLOG.md`](../BACKLOG.md)) — Opus 4.7 adoption across agent lifecycle; touches `ide-bridge-evidence` skill.

---

## 2. Gate 1 — File new TECH backlog row

Create a new TECH issue in `BACKLOG.md` tracking the bridge program.

**Issue title (from analysis doc §7.2):**

```
Unity Agent Bridge: file-based command queue + MCP tools for agent-triggered exports
```

**Issue body — write directly as a new row in `BACKLOG.md`** (no GitHub issue — territory-developer tracks backlog in-repo per `ia/rules/`). Use the next available TECH-{N} where N = max existing TECH-ID + 1 (grep `^- \[ \] \*\*TECH-\d+\*\*` to find max).

**Row format (mirror existing BACKLOG rows like TECH-83 @ line 255):**

```markdown
- [ ] **TECH-{N}** — **Unity Agent Bridge** program — file-based command queue + MCP tools for agent-triggered exports
  - Type: tooling / agent enablement
  - Files: `tools/mcp-ia-server/src/` (existing `unity_bridge_command` / `unity_bridge_get` + sugar wrappers); `Assets/Scripts/Editor/AgentBridgeCommandRunner.cs` (existing; extended); `.claude/skills/` (new Cursor skills per analysis doc §6); `docs/mcp-ia-server.md` (tool docs); `ia/skills/ide-bridge-evidence/SKILL.md` (evidence contract)
  - Spec: `ia/projects/unity-agent-bridge-master-plan.md` (to be authored by /master-plan-new in Gate 2 of this handoff)
  - Notes: Phase 1 (file-based command queue + `unity_bridge_command` / `unity_bridge_get`) already shipped per analysis doc §8.1. Program tracks analysis doc §10 tiers B (hardening — parameterized exports, sugar tools, Cursor skills), C (HTTP transport, log streaming, screenshot automation), D (deterministic replay, visual diff). Explicitly excludes `-batchmode` / headless CI per analysis doc §4.1 + §11.
  - Acceptance: master plan authored at `ia/projects/unity-agent-bridge-master-plan.md` with ≥3 Steps landing on green-bar boundaries; at minimum Step 1 = analysis §10-B hardening; each stage has 2–6 tasks per phase (cardinality gate); all analysis doc §10 items mapped into a step OR explicitly deferred to post-MVP extensions doc.
  - Depends on: none hard. Soft: existing `unity_bridge_command` MCP tool contract (do not break); `ia/specs/unity-development-context.md` §10 Reports contract alignment.
  - Related: TECH-83 (consumer — sim params uses bridge); TECH-78 (sibling agent tooling); TECH-251 (Opus 4.7 touches `ide-bridge-evidence`).
```

Placement in `BACKLOG.md`: in the **Agent Enablement** / Tools cluster near existing TECH-77..TECH-83 (around line 207). Use Grep to confirm exact section header.

**Do NOT** file a GitHub issue via `gh` — this repo tracks backlog in `BACKLOG.md` only. No `gh issue create`.

**Commit separately:** `chore(backlog): add TECH-{N} Unity Agent Bridge program row` — user will review the commit before Gate 2.

---

## 3. Gate 2 — Author master plan via `/master-plan-new`

### 3a. Phase 0 pre-check

`/master-plan-new` Phase 0 requires `## Design Expansion` OR a Phase-0 semantic-equivalent in the source doc. The analysis doc `docs/unity-ide-agent-bridge-analysis.md` does NOT carry a literal `## Design Expansion` block — it carries §10 "Recommended Next Steps" structured as tiers A/B/C/D with numbered items.

**Check first:** Read `ia/skills/master-plan-new/SKILL.md` Phase 0 mapping table. Confirm whether §10-A/B/C/D structure qualifies as a semantic equivalent (sections like "Recommended Next Steps" or "Proposed architecture" commonly do).

**Two outcomes:**

- **Equivalent accepted** → proceed directly to 3b.
- **Equivalent rejected** → run `/design-explore docs/unity-ide-agent-bridge-analysis.md` first to persist a `## Design Expansion` block. Then return to 3b. Do NOT hand-author a Design Expansion block outside `/design-explore`.

### 3b. Run `/master-plan-new`

```
/master-plan-new docs/unity-ide-agent-bridge-analysis.md unity-agent-bridge
```

Arguments:
- `DOC_PATH` = `docs/unity-ide-agent-bridge-analysis.md`
- `SLUG` = `unity-agent-bridge` → orchestrator path `ia/projects/unity-agent-bridge-master-plan.md`
- No `SCOPE_BOUNDARY_DOC` — deferred items go into a future `docs/unity-agent-bridge-post-mvp-extensions.md` if/when needed.

### 3c. Step decomposition hints (for the master-plan-new subagent's Phase 4)

Analysis doc §10 already groups next steps into tiers. Natural step mapping:

- **Step 1 — Hardening (analysis §10-B):** parameterized exports, sugar MCP tools, `debug-sorting-order` Cursor skill, close-dev-loop supersession.
- **Step 2 — HTTP transport + observability (analysis §10-C):** HTTP bridge, log streaming, screenshot automation, health-check auto-export.
- **Step 3 — Optional depth (analysis §10-D):** Phase 3 polish (richer streaming, comparison helpers), deterministic replay, visual diff automation.

Skip **§10-A** — already shipped (Phase 1 file-based loop). Note in orchestrator header as `Pre-shipped: §10-A (Phase 1 bridge MVP)`.

Cardinality gate (SKILL.md Phase 6): each phase in each stage's task table must have **≥2 tasks AND ≤6 tasks**. Analysis doc §10 items are currently 15 items across 4 tiers — ample for 3 steps × 2–3 stages × 2–4 phases of 2–6 tasks.

### 3d. Locked decisions (merge into orchestrator header §Locked decisions)

Pull verbatim from analysis doc:

- **File-based Phase 1 already shipped** (§8.1). Master plan strictly orchestrates §10-B/C/D — do NOT re-scope Phase 1.
- **No `-batchmode` / headless CI** (§4.1 + §11). Bridge is Editor-centric. Hard out-of-scope.
- **Postgres-backed** (`agent_bridge_job` queue) — do NOT replace with file-only transport.
- **Command interchange contract matches `ia/specs/unity-development-context.md` §10 Reports** — re-use, do not re-define.
- **Cursor skills land as `.claude/skills/*/SKILL.md` entries per analysis doc §6** — not in `ia/skills/`.

### 3e. Relevant surfaces (Phase 2 Glob pre-check input)

Expected entry/exit points for Glob `(new)` marking:

- `tools/mcp-ia-server/src/` — existing dir; new tool files `(new)` per stage.
- `Assets/Scripts/Editor/AgentBridgeCommandRunner.cs` — existing; extended.
- `.claude/skills/debug-sorting-order/SKILL.md` — `(new)` per §10-B item 7.
- `docs/mcp-ia-server.md` — existing; updated.
- `ia/skills/ide-bridge-evidence/SKILL.md` — existing; possibly updated.
- Postgres migration file under `db/migrations/` — `(new)` per streaming / screenshot tables if introduced.

---

## 4. Exit criteria for this handoff

- [ ] Gate 1: new TECH-{N} row added to `BACKLOG.md` in Agent Enablement cluster; committed separately.
- [ ] Gate 2a: Phase 0 pre-check resolved — either analysis doc accepted or `/design-explore` run first (separate commit if Design Expansion block added).
- [ ] Gate 2b: `ia/projects/unity-agent-bridge-master-plan.md` authored by `/master-plan-new` subagent; header references analysis doc as Exploration source; TECH-{N} referenced as Parent issue.
- [ ] Cardinality gate passed on all stages (2–6 tasks per phase).
- [ ] Locked decisions from §3d merged into orchestrator header.
- [ ] Pre-shipped §10-A (Phase 1) noted in header as out-of-scope.
- [ ] `npm run validate:all` green.
- [ ] `npm run progress` regenerates dashboard without errors.
- [ ] Handoff emits next step: `claude-personal "/stage-file ia/projects/unity-agent-bridge-master-plan.md Stage 1.1"`.

---

## 5. Hard boundaries (do NOT)

- Do NOT re-scope Phase 1 (file-based bridge MVP) — shipped per §8.1.
- Do NOT introduce `-batchmode` / headless CI scope — §4.1 + §11 explicit.
- Do NOT file a GitHub issue via `gh` — backlog is in-repo (`BACKLOG.md`).
- Do NOT touch **TECH-83**, **TECH-78**, **TECH-251** (see [`BACKLOG.md`](../BACKLOG.md)) — adjacent specs, separate ownership.
- Do NOT hand-author the orchestrator `.md` file directly — `/master-plan-new` owns authoring.
- Do NOT hand-author a `## Design Expansion` block on the analysis doc — use `/design-explore` if Phase 0 rejects §10 as semantic equivalent.
- Do NOT commit the orchestrator for the user — user reviews and decides on commit.

---

## 6. Handoff next step (after master plan authored)

```
claude-personal "/stage-file ia/projects/unity-agent-bridge-master-plan.md Stage 1.1"
```

That kicks off per-task spec filing + `/author` bulk §Plan Author pass for Stage 1.1.
