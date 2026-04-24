<!--
  Master plan template — canonical structure for `ia/projects/{slug}-master-plan.md`.

  Authored by `master-plan-new` (fresh orchestrator) + extended by `master-plan-extend`
  (new Stages from exploration / extensions doc). Never closeable via `/closeout`
  (see `ia/rules/orchestrator-vs-spec.md`).

  Placeholders wrapped in `{{...}}` — replace on author. Comments in HTML form get
  stripped by skill authors; keep for template reference only.

  Hierarchy is 2-level: Stage > Task. Phase layer is REMOVED (post lifecycle-refactor).

  Task table columns (5): Task · Name · Issue · Status · Intent.
    - Task     = hierarchical id `T{STAGE}.{N}` (e.g. `T1.3`).
    - Name     = short ≤6-word handle (used as BACKLOG row title + spec file name).
    - Issue    = `_pending_` until `stage-file-apply` fills with `**{PREFIX}-NNN**`.
    - Status   = `_pending_ → Draft → In Review → In Progress → Done (archived)`.
    - Intent   = ≤2 sentences naming concrete deliverable (types / methods / file paths).

  Plan-Apply pair sections (per `ia/rules/plan-apply-pair-contract.md`):
    - `§Stage File Plan` — Opus pair-head writes `{operation, target_path, target_anchor, payload}` tuples per Stage; `stage-file-apply` Sonnet reads + applies.
    - `§Plan Fix` — Opus `plan-review` writes targeted fix tuples; `plan-fix-apply` Sonnet reads + applies.
-->

# {{Title}} — Master Plan ({{SCOPE_LABEL}})

> **Last updated:** {{YYYY-MM-DD}}
>
> **Status:** Draft — Stage 1.1 pending (no BACKLOG rows filed yet)
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
> **Hierarchy rules:** `ia/projects/MASTER-PLAN-STRUCTURE.md` (canonical file + Stage + Task table shape — authoritative). `ia/rules/project-hierarchy.md` (stage > task — 2-level cardinality). `ia/rules/orchestrator-vs-spec.md` (this doc = orchestrator, never closeable). `ia/rules/plan-apply-pair-contract.md` (§Plan section shape for pair seams).
>
> **Read first if landing cold:**
>
> - `{{DOC_PATH}}` — full design + architecture + examples. Design Expansion block is ground truth.
> - `{{scope-boundary-doc}}` — scope boundary (what's OUT of MVP / current scope).
> - `ia/rules/project-hierarchy.md` + `ia/rules/orchestrator-vs-spec.md` — doc semantics + Stage / Task cardinality rule (≥2 tasks per Stage).
> - `ia/rules/invariants.md` — {{flagged numbers from MCP `invariants_summary`, e.g. `#3 (no FindObjectOfType in hot loops), #4 (no new singletons)`}}.
> - MCP: `backlog_issue {id}` per referenced id once tasks file; never full `BACKLOG.md` read.

---

## Stages

> **Tracking legend:** Stage `Status:` uses enum `Draft | In Review | In Progress | Final` (per `ia/projects/MASTER-PLAN-STRUCTURE.md` §6.2). Task tables carry a **Status** column: `_pending_` (not filed) → `Draft` → `In Review` → `In Progress` → `Done (archived)`. Markers flipped by lifecycle skills: `stage-file-apply` → task rows gain `Issue` id + `Draft` status; `plan-author` / `plan-digest` → `In Review`; `spec-implementer` → `In Progress`; `plan-applier` Mode stage-closeout → `Done (archived)` + Stage `Final` rollup.

### Stage {{N}}.{{M}} — {{Stage Name}}

**Status:** Draft

**Notes:** tasks _pending_ — not yet filed

**Backlog state (Stage {{N}}.{{M}}):** 0 filed

**Objectives:** {{2–4 sentences — what this stage lands + why. Ties back to Chosen Approach rationale. Human-consumed cold; full English OK per agent-output-caveman exception for Objectives prose}}.

**Exit criteria:**

- {{concrete observable outcome 1 — cites type / method / file path where verifiable}}
- {{outcome 2}}
- {{glossary row additions, if canonical terms introduced}}

**Art:** {{None / list of art assets needed from Design Expansion; else `None`}}.

**Relevant surfaces (load when stage opens):**

- {{exploration doc ref + sections}}
- {{MCP-routed spec section refs (via Phase 2 / Phase 1 tool recipe)}}
- {{invariant numbers from Subsystem Impact}}
- {{prior stage outputs — surfaces shipped by Stage {{N}}.{{M-1}}}}
- {{code paths — entry / exit points from Design Expansion Architecture block; mark `(new)` for non-existent paths per surface-path pre-check}}

**Tasks:**

| Task | Name | Issue | Status | Intent |
|---|---|---|---|---|
| T{{N}}.{{M}}.1 | {{short name ≤6 words}} | _pending_ | _pending_ | {{≤2 sentences — concrete deliverable: types, methods, file paths. Reference existing patterns where applicable (e.g. `GameNotificationManager.cs` DontDestroyOnLoad pattern, `OnValidate` clamps). Avoid vague verbs like "add support for X" — cite the thing being shipped}} |
| T{{N}}.{{M}}.2 | {{short name}} | _pending_ | _pending_ | {{...}} |
| T{{N}}.{{M}}.3 | {{short name}} | _pending_ | _pending_ | {{...}} |

#### §Stage File Plan

> Opus `stage-file-plan` writes structured `{operation, target_path, target_anchor, payload}` tuples here per pending Task. Sonnet `stage-file-apply` reads tuples and materializes BACKLOG rows + spec stubs. Contract: `ia/rules/plan-apply-pair-contract.md`.

_pending — populated by `/stage-file` planner pass._

#### §Plan Fix

> Opus `plan-review` writes targeted fix tuples here when a Stage's Task specs need tightening before first `/implement`. Sonnet `plan-applier` Mode plan-fix reads tuples and applies edits. Contract: `ia/rules/plan-apply-pair-contract.md`.

_pending — populated by `/plan-review` when fixes are needed._

#### §Stage Audit

> Opus `opus-audit` writes one `§Audit` paragraph per Task row here (Stage-scoped bulk, non-pair) once every Task reaches Done post-verify. Feeds `§Stage Closeout Plan` migration tuples downstream. Contract: `ia/rules/plan-apply-pair-contract.md` Stage-scoped non-pair row.

_pending — populated by `/audit {{this-doc}} Stage {{N}}.{{M}}` when all Tasks reach Done post-verify._

#### §Stage Closeout Plan

> Opus `stage-closeout-plan` writes unified tuple list here ONCE per Stage when all Task rows reach `Done` post-verify (replaces per-Task closeout). Shared migration ops deduped across Tasks (glossary rows / rule edits / doc paragraph edits) + N per-Task ops (`archive_record`, `delete_file`, `replace_section` status flip, `id_purge`, `digest_emit`). Sonnet `stage-closeout-apply` reads tuples and applies verbatim. Contract: `ia/rules/plan-apply-pair-contract.md` seam #4.

_pending — populated by `/closeout {{this-doc}} Stage {{N}}.{{M}}` planner pass when all Tasks reach `Done`._

<!--
  Repeat `### Stage {{N}}.{{M}}` block per stage. Stages are flat siblings — no Step grouping.

  Cardinality gate (enforced by `master-plan-new` + `master-plan-extend` + `stage-decompose`):
    - ≥ 2 tasks per Stage (hard).
    - ≤ 6 tasks per Stage (soft — split at ≥ 7).
    - Task sizing: 2–5 files per task / one algorithm layer per task; merge trivial
      single-function tasks; split tasks spanning > 3 unrelated subsystems.
-->

---

## Orchestration guardrails

**Do:**

- Open one Stage at a time. Next Stage opens only after current Stage's `project-stage-close` runs.
- Run `/stage-file {{this-doc}} Stage {{N}}.{{M}}` to materialize pending tasks → BACKLOG rows + `ia/projects/{ISSUE_ID}.md` stubs (planner→applier pair).
- Update Stage `Status` as lifecycle skills flip them — do NOT edit by hand.
- Preserve locked decisions (see header block). Changes require explicit re-decision + sync edit to exploration doc + scope-boundary doc.
- Keep this orchestrator synced with umbrella issue (if one exists) — per `closeout-apply` skill umbrella-sync rule.
- Extend via `/master-plan-extend {{this-doc}} {{source-doc}}` when a new exploration or extensions doc introduces new Stages — do NOT hand-insert Stage blocks.

**Do not:**

- Close this orchestrator via `/closeout` — orchestrators are permanent (see `ia/rules/orchestrator-vs-spec.md`). Only the terminal Stage landing triggers a final `Status: Final`; the file stays.
- Silently promote post-MVP items into MVP stages — they belong in the scope-boundary doc.
- Merge partial Stage state — every Stage must land on a green bar.
- Insert BACKLOG rows directly into this doc — only `stage-file-apply` materializes them.
- Hand-insert new Stages past the last persisted `### Stage N.M` block — run `/master-plan-extend` so MCP context + cardinality gate + progress regen fire.

---
