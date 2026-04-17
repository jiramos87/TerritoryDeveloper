<!--
  Master plan template — canonical structure for `ia/projects/{slug}-master-plan.md`.

  Authored by `master-plan-new` (fresh orchestrator) + extended by `master-plan-extend`
  (new Steps from exploration / extensions doc). Never closeable via `/closeout`
  (see `ia/rules/orchestrator-vs-spec.md`).

  Placeholders wrapped in `{{...}}` — replace on author. Comments in HTML form get
  stripped by skill authors; keep for template reference only.

  Task table columns (6): Task · Name · Phase · Issue · Status · Intent.
    - Task     = hierarchical id `T{STEP}.{STAGE}.{N}` (e.g. `T1.2.3`).
    - Name     = short ≤6-word handle (used as BACKLOG row title + spec file name).
    - Phase    = integer parent-phase index (1-based, matches `**Phases:**` list order).
    - Issue    = `_pending_` until `stage-file` fills with `**{PREFIX}-NNN**`.
    - Status   = `_pending_ → Draft → In Review → In Progress → Done (archived)`.
    - Intent   = ≤2 sentences naming concrete deliverable (types / methods / file paths).
-->

# {{Title}} — Master Plan ({{SCOPE_LABEL}})

> **Last updated:** {{YYYY-MM-DD}}
>
> **Status:** Draft — Step 1 / Stage 1.1 pending (no BACKLOG rows filed yet)
>
> **Scope:** {{one-line scope — Chosen Approach + Non-scope boundary. Reference scope-boundary doc when provided}}.
>
> **Exploration source:** `{{DOC_PATH}}` (§{{sections of expansion that are ground truth}}).
>
> **Locked decisions (do not reopen in this plan):**
>
> - {{locked decision 1 — MVP scope / architecture lock pulled from exploration}}
> - {{locked decision 2}}
>
> **Sibling orchestrators in flight (shared `{{branch-name}}` branch):**
>
> - `{{sibling-master-plan.md}}` — {{overlap + collision surface + parallel-work note}}.
> - **Parallel-work rule:** do NOT run `/stage-file` or `/closeout` against two sibling orchestrators concurrently — glossary + MCP index regens must sequence on a single branch.
>
> **Hierarchy rules:** `ia/rules/project-hierarchy.md` (step > stage > phase > task). `ia/rules/orchestrator-vs-spec.md` (this doc = orchestrator, never closeable).
>
> **Read first if landing cold:**
>
> - `{{DOC_PATH}}` — full design + architecture + examples. Design Expansion block is ground truth.
> - `{{scope-boundary-doc}}` — scope boundary (what's OUT of MVP / current scope).
> - `ia/rules/project-hierarchy.md` + `ia/rules/orchestrator-vs-spec.md` — doc semantics + phase / task cardinality rule (≥2 tasks per phase).
> - `ia/rules/invariants.md` — {{flagged numbers from MCP `invariants_summary`, e.g. `#3 (no FindObjectOfType in hot loops), #4 (no new singletons)`}}.
> - MCP: `backlog_issue {id}` per referenced id once tasks file; never full `BACKLOG.md` read.

---

## Steps

> **Tracking legend:** Step / Stage `Status:` uses enum `Draft | In Review | In Progress — {active child} | Final` (per `ia/rules/project-hierarchy.md`). Phase bullets use `- [ ]` / `- [x]`. Task tables carry a **Status** column: `_pending_` (not filed) → `Draft` → `In Review` → `In Progress` → `Done (archived)`. Markers flipped by lifecycle skills: `stage-file` → task rows gain `Issue` id + `Draft` status; `/kickoff` → `In Review`; `/implement` → `In Progress`; `/closeout` → `Done (archived)` + phase box when last task of phase closes; `project-stage-close` → stage `Final` + stage-level rollup.

### Step {{N}} — {{Step Name}}

**Status:** Draft (tasks _pending_ — not yet filed)

**Backlog state (Step {{N}}):** 0 filed

**Objectives:** {{2–4 sentences — what this step lands + why. Ties back to Chosen Approach rationale. Human-consumed cold; full English OK per agent-output-caveman exception for Objectives prose}}.

**Exit criteria:**

- {{concrete observable outcome 1 — cites type / method / file path where verifiable}}
- {{outcome 2}}
- ...

**Art:** {{None / list of art assets needed from Design Expansion; else `None`}}.

**Relevant surfaces (load when step opens):**

- {{exploration doc ref + sections}}
- {{MCP-routed spec section refs (via Phase 2 / Phase 1 tool recipe)}}
- {{invariant numbers from Subsystem Impact}}
- {{prior step outputs (Steps 2+) — surfaces shipped by Step {{N-1}}}}
- {{code paths — entry / exit points from Design Expansion Architecture block; mark `(new)` for non-existent paths per surface-path pre-check}}

#### Stage {{N}}.{{M}} — {{Stage Name}}

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** {{1–3 sentences — what this stage lands}}.

**Exit:**

- {{observable outcome 1 — cites type / method / file path}}
- {{outcome 2}}
- {{glossary row additions, if canonical terms introduced}}

**Phases:**

- [ ] Phase 1 — {{shippable increment description — one compilable green-bar landing}}.
- [ ] Phase 2 — {{...}}.
- [ ] Phase N — {{...}}.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T{{N}}.{{M}}.1 | {{short name ≤6 words}} | 1 | _pending_ | _pending_ | {{≤2 sentences — concrete deliverable: types, methods, file paths. Reference existing patterns where applicable (e.g. `GameNotificationManager.cs` DontDestroyOnLoad pattern, `OnValidate` clamps). Avoid vague verbs like "add support for X" — cite the thing being shipped}} |
| T{{N}}.{{M}}.2 | {{short name}} | 1 | _pending_ | _pending_ | {{...}} |
| T{{N}}.{{M}}.3 | {{short name}} | 2 | _pending_ | _pending_ | {{...}} |

<!--
  Repeat `#### Stage {{N}}.{{M}}` block per stage (target 2–4 stages per step).
  Repeat `### Step {{N}}` block per step (target 1–4 steps; all fully decomposed — no lazy skeletons).

  Cardinality gate (enforced by `master-plan-new` Phase 6 + `master-plan-extend` Phase 5 + `stage-decompose` Phase 3):
    - ≥ 2 tasks per phase (hard).
    - ≤ 6 tasks per phase (soft — split at ≥ 7).
    - Task sizing: 2–5 files per task / one algorithm layer per task; merge trivial
      single-function tasks; split tasks spanning > 3 unrelated subsystems.
-->

---

## Orchestration guardrails

**Do:**

- Open one stage at a time. Next stage opens only after current stage's `project-stage-close` runs.
- Run `/stage-file {{this-doc}} Stage {{N}}.{{M}}` to materialize pending tasks → BACKLOG rows + `ia/projects/{ISSUE_ID}.md` stubs.
- Update stage / step `Status` + phase checkboxes as lifecycle skills flip them — do NOT edit by hand.
- Preserve locked decisions (see header block). Changes require explicit re-decision + sync edit to exploration doc + scope-boundary doc.
- Keep this orchestrator synced with umbrella issue (if one exists) — per `project-spec-close` / `closeout` skill umbrella-sync rule.
- Extend via `/master-plan-extend {{this-doc}} {{source-doc}}` when a new exploration or extensions doc introduces new Steps — do NOT hand-insert Step blocks.

**Do not:**

- Close this orchestrator via `/closeout` — orchestrators are permanent (see `ia/rules/orchestrator-vs-spec.md`). Only the terminal step landing triggers a final `Status: Final`; the file stays.
- Silently promote post-MVP items into MVP stages — they belong in the scope-boundary doc.
- Merge partial stage state — every stage must land on a green bar.
- Insert BACKLOG rows directly into this doc — only `stage-file` materializes them.
- Hand-insert new Steps past the last persisted `### Step N` block — run `/master-plan-extend` so MCP context + cardinality gate + progress regen fire.

---
