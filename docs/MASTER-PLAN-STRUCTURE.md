# Master-plan structure — canonical

**Purpose:** single source of truth for every `ia/projects/*-master-plan.md` orchestrator. Defines the file shape, Stage block shape, Task table shape, Status enum, lifecycle flip matrix, and cardinality rules. Every skill that authors, extends, decomposes, files, or closes a Stage cites this doc.

**Scope:** orchestrator docs only (`*-master-plan.md`). Per-issue project specs (`ia/projects/{ISSUE_ID}.md`) follow `ia/templates/project-spec-template.md` + `ia/rules/plan-apply-pair-contract.md` — NOT this doc.

**Authority:** this file + [`ia/rules/project-hierarchy.md`](../rules/project-hierarchy.md) are authoritative. [`ia/templates/master-plan-template.md`](../templates/master-plan-template.md) is the seed fixture — conforms to this doc. Any skill with inline structure definition is DRIFT; must defer here.

---

## 1. Hierarchy — two levels, flat

```
master plan (file)
└── Stage N.M           (### H3 heading — shippable compilable increment)
    └── Task            (row in Stage Tasks table — 1 BACKLOG id + 1 spec)
```

**Removed (post lifecycle-refactor — do NOT reintroduce):**

- `### Step N` headings — deleted. Stages are flat siblings under `## Stages`.
- `#### Stage N.M` (H4) — Stages are H3, not H4.
- `**Phases:**` checkbox blocks inside Stage — deleted. One Stage = one atomic shippable unit, decomposed directly into Tasks.
- `Phase` column in Task table — deleted. Table is 5 columns, not 6.
- Stage skeletons / "decomposition deferred" blocks — `master-plan-new` decomposes ALL Stages at author time. Lazy-decompose fires only when a new Stage skeleton is intentionally appended by `master-plan-extend` with a deferred marker — `stage-decompose` expands it in place.

---

## 2. File canonical shape

### 2.1 Filename + placement

```
ia/projects/{slug}-master-plan.md
```

`{slug}` = kebab-case domain handle (e.g. `sprite-gen`, `grid-asset-visual-registry`). Never numbered — numbers belong to Stages, not the file.

### 2.2 Top-level headings (required, in order)

```markdown
# {Title} — Master Plan ({SCOPE_LABEL})

> **Last updated:** YYYY-MM-DD
> **Status:** {Draft | In Review | In Progress — Stage N.M / TECH-XX | Final}
> **Scope:** {one-line scope}
> **Exploration source:** `{DOC_PATH}`
> **Locked decisions (do not reopen):** {bulleted list}
> **Sibling orchestrators in flight:** {optional; parallel-work rule}
> **Hierarchy rules:** `docs/MASTER-PLAN-STRUCTURE.md` (this doc; Stage > Task 2-level) · `ia/rules/orchestrator-vs-spec.md` · `ia/rules/plan-apply-pair-contract.md`
> **Read first if landing cold:** {bulleted list}

---

## Stages

> **Tracking legend:** {Stage / Task status flip matrix summary — see §6 below}

### Stage 1.1 — {Stage Name}
...

### Stage 1.2 — {Stage Name}
...

---

## Orchestration guardrails

**Do:** {bulleted list}

**Do not:** {bulleted list}
```

### 2.3 Header field rules

| Field | Type | Rules |
|-------|------|-------|
| H1 title | string | `# {Title} — Master Plan ({SCOPE_LABEL})`. `SCOPE_LABEL` = bracketed tag (`MVP`, `IA Infrastructure`, `Post-MVP Extension`, etc.). |
| `Last updated` | date | ISO `YYYY-MM-DD`. Updated by every skill that mutates the file. |
| `Status` | enum | See §6.1. Flipped by lifecycle skills, never by hand. |
| `Scope` | sentence | Chosen Approach + Non-scope boundary. Reference scope-boundary doc when present. |
| `Exploration source` | path list | Relative paths under `docs/` or `ia/`. Ground-truth link. |
| `Locked decisions` | bullets | MVP scope locks / architecture locks lifted from exploration. Do NOT reopen in Stage-level work. |
| `Sibling orchestrators` | optional | Shared-branch collisions; parallel-work rule (no concurrent `/stage-file` or `/closeout` on siblings). |
| `Hierarchy rules` | path list | MUST cite this doc first, then `orchestrator-vs-spec.md` + `plan-apply-pair-contract.md`. |
| `Read first if landing cold` | bullets | 4–6 entries. Must include MCP-first directive + invariant refs flagged by `invariants_summary`. |

---

## 3. Stage block canonical shape

Every Stage block under `## Stages` is an H3 heading + the subsection sequence below. Order is fixed.

### 3.1 Heading

```markdown
### Stage N.M — {Stage Name}
```

- `N` = major increment id (1, 2, 3…).
- `M` = minor increment id within `N` (1, 2, 3…). `N.M` pair uniquely identifies the Stage.
- `Stage Name` = ≤8 words, noun-phrase, describes the shippable outcome.

### 3.2 Required subsections (in order)

```markdown
**Status:** {Draft | In Review | In Progress | Final}

**Notes:** {one-line context — e.g. "tasks _pending_ — not yet filed"}

**Backlog state (Stage N.M):** {X filed}

**Objectives:** {2–4 sentences — what this Stage lands + why}

**Exit criteria:**
- {concrete observable outcome 1}
- {outcome 2}

**Art:** {None / list from Design Expansion}

**Relevant surfaces (load when stage opens):**
- {exploration doc refs + sections}
- {MCP-routed spec refs}
- {invariant numbers}
- {prior stage surfaces}
- {code paths — mark `(new)` for non-existent}

**Tasks:**

| Task | Name | Issue | Status | Intent |
|---|---|---|---|---|
| T{N}.{M}.1 | {short name ≤6 words} | _pending_ | _pending_ | {≤2 sentences — concrete deliverable} |

#### §Stage File Plan
_pending — populated by `/stage-file` planner pass._

#### §Plan Fix
_pending — populated by `/plan-review` when fixes are needed._

#### §Stage Audit
_pending — populated by `/audit` when all Tasks reach Done post-verify (pre-closeout)._

#### §Stage Closeout Plan
_pending — populated by `/closeout {MASTER_PLAN_PATH} Stage N.M` when all Tasks reach `Done`._
```

### 3.3 Task table — written schema (5 columns, no Phase column)

| Column | Required | Type / format | Filled by | Rules |
|--------|----------|---------------|-----------|-------|
| `Task` | yes | string `T{N}.{M}.{K}` | `master-plan-new` / `master-plan-extend` / `stage-decompose` | Hierarchical id `T{STAGE_N}.{STAGE_M}.{TASK_K}`. Monotonic within Stage. Never renumbered after filing. |
| `Name` | yes | string ≤6 words | Author | Short handle. Also used as BACKLOG row title + project-spec file name hint. |
| `Issue` | yes | `_pending_` OR `**{PREFIX}-NNN**` | `_pending_` at author time; `stage-file-apply` fills with `**TECH-NNN**` (or `BUG-`, `FEAT-`, `ART-`, `AUDIO-`). Bold formatting required. |
| `Status` | yes | enum | `stage-file-apply` / `plan-author` / `spec-implementer` / `plan-applier` Mode stage-closeout | `_pending_ → Draft → In Review → In Progress → Done (archived)`. See §6.2. |
| `Intent` | yes | string ≤2 sentences | Author | Concrete deliverable — cite types / methods / file paths. Avoid vague verbs (`add support for X`, `improve Y`). |

**Column order is fixed.** Do NOT insert extra columns (Priority, Owner, Phase, etc.). Per-Task Priority lives in the BACKLOG yaml, not the master plan table.

**Cardinality:**

- **Hard ≥2 Tasks per Stage.** Single-task Stage requires a Decision Log waiver in master-plan header `Locked decisions` block.
- **Soft ≤6 Tasks per Stage.** Split at ≥7 — large Stages usually hide nested grouping.

### 3.4 Stage subsections — purpose + ordering

The four `####` subsections under every Stage are lifecycle pair-seam anchors. Order is canonical (below). Empty subsections carry a `_pending — populated by {skill} {when}_` sentinel line.

| # | Subsection | Pair-head (Opus) | Pair-tail (Sonnet) | Purpose |
|---|-----------|------------------|--------------------|---------|
| 1 | `#### §Stage File Plan` | `stage-file-plan` | `stage-file-apply` | Seam #2 (`plan-apply-pair-contract.md`). Reserves ids + materializes BACKLOG rows + spec stubs for every pending Task in the Stage. |
| 2 | `#### §Plan Fix` | `plan-review` | `plan-applier` Mode plan-fix | Seam #1. Stage-wide drift scan after `stage-file-apply`; emits targeted fix tuples before first `/implement`. |
| 3 | `#### §Stage Audit` | `opus-audit` | *(non-pair — writes directly)* | Bulk-authors one `§Audit` paragraph per Task post-verify. Feeds §Stage Closeout Plan migration tuples. |
| 4 | `#### §Stage Closeout Plan` | `stage-closeout-plan` | `plan-applier` Mode stage-closeout | Seam #4. Shared migration tuples + N per-Task archive / delete / status-flip / id-purge / digest ops. Fires ONCE per Stage when every Task row is Done. |

Retired variants (**do NOT reintroduce**): per-Task `§Closeout Plan` inside project specs (collapsed into Stage-scoped §Stage Closeout Plan under `stage-closeout-plan` seam #4). Legacy `§Stage Audit` absence (some plans had only §Stage Closeout Plan) — §Stage Audit is now mandatory pre-closeout.

---

## 4. Section ordering — full master plan

```
1. H1 title + header block (§2.2)
2. --- separator
3. ## Stages  (umbrella H2 — single occurrence)
4. ### Stage 1.1 — ...  (H3 — repeat per Stage)
   4.1 Status / Notes / Backlog state / Objectives / Exit criteria / Art / Relevant surfaces / Tasks table
   4.2 #### §Stage File Plan
   4.3 #### §Plan Fix
   4.4 #### §Stage Audit
   4.5 #### §Stage Closeout Plan
5. --- separator (after last Stage)
6. ## Orchestration guardrails  (H2 — single occurrence, terminal)
```

No other H2 headings are permitted between `## Stages` and `## Orchestration guardrails`. No Step H3 / Phase H4 ever. Appendices (Decision Log, Open Questions) move into `docs/` or the exploration doc — not the master plan body.

---

## 5. Cardinality gate (hard vs soft)

| Gate | Rule | Enforced by |
|------|------|-------------|
| Hard | ≥2 Tasks per Stage | `master-plan-new` Phase N · `master-plan-extend` Phase N · `stage-decompose` Phase N · `stage-file-plan` re-check |
| Soft | ≤6 Tasks per Stage | Same skills; warn + recommend split, don't block |
| Hard | ≥1 Stage per master plan | `master-plan-new` Phase N |
| Hard | Every Stage has `### Stage File Plan` + `### Plan Fix` + `### Stage Audit` + `### Stage Closeout Plan` subsections (sentinel or populated) | `master-plan-new` / `master-plan-extend` / `stage-decompose` |
| Hard | Task table has exactly 5 columns (`Task | Name | Issue | Status | Intent`) | Same + `stage-file-plan` parser |

---

## 6. Status enum

### 6.1 Master-plan header `Status`

```
Draft | In Review | In Progress — Stage N.M / TECH-XX | Final
```

| State | Meaning | Flip trigger |
|-------|---------|--------------|
| `Draft` | Initial — no Task filed yet. | `master-plan-new` writes `Draft`. |
| `In Review` | Mid-authoring — `master-plan-extend` re-author pass. | `master-plan-extend` Phase 7 (temporary). |
| `In Progress — Stage N.M / TECH-XX` | ≥1 Task filed; plan actively worked. | `stage-file-apply` R1 flips on first Task ever filed. |
| `Final` | All Stages are `Final`. | `plan-applier` Mode stage-closeout R5 on last Stage close. |

`master-plan-extend` R6 demotes `Final → In Progress` when new Stages appended to a Final plan.

### 6.2 Stage header `Status`

```
Draft | In Review | In Progress | Final
```

| State | Meaning | Flip trigger |
|-------|---------|--------------|
| `Draft` | Authored, no Task filed. | `master-plan-new` / `master-plan-extend` writes `Draft`. |
| `In Review` | Post-`plan-review` drift scan pending fix-apply. | `plan-review` writes `In Review` when `§Plan Fix` non-empty. |
| `In Progress` | ≥1 Task filed in this Stage. | `stage-file-apply` R2 flips on first Task filed in the Stage. |
| `Final` | Every Task row in Stage = `Done`; closeout applied. | `plan-applier` Mode stage-closeout R3 on last Task archived. |

### 6.3 Task row `Status`

```
_pending_ → Draft → In Review → In Progress → Done (archived)
```

| State | Meaning | Flip trigger |
|-------|---------|--------------|
| `_pending_` | Not yet filed — no BACKLOG id. | `master-plan-new` / `master-plan-extend` / `stage-decompose` writes at author time. |
| `Draft` | BACKLOG yaml + spec stub exist; plan-author not run. | `stage-file-apply` flips on row materialization. |
| `In Review` | §Plan Author populated → §Plan Digest compiled. | `plan-author` / `plan-digest` write sequence. |
| `In Progress` | `/implement` dispatched. | `spec-implementer` Phase 0. |
| `Done (archived)` | BACKLOG yaml archived + spec deleted. | `plan-applier` Mode stage-closeout per-Task tuple. |

**Retired values (do NOT reintroduce):** `Skeleton`, `Planned`. Both replaced by `_pending_` + `Draft` post lifecycle-refactor.

---

## 7. Lifecycle skill flip matrix

One-line binding from skill → structural surface it mutates. Every authoring skill MUST cite this doc as hierarchy authority.

| Skill | Reads | Writes | Section authority |
|-------|-------|--------|------------------|
| `master-plan-new` | Exploration doc §Design Expansion | New master plan file; all Stages + Tasks `_pending_` | §2 (file shape), §3 (Stage block), §6 (Status `Draft`) |
| `master-plan-extend` | Exploration / extensions doc; existing master plan | Appends new Stage blocks at end | §3, §6 R6 (demote Final → In Progress) |
| `stage-decompose` | Deferred Stage skeleton in master plan | Expands Stage skeleton into Tasks in-place | §3.3 (Task table), §5 (cardinality) |
| `stage-file-plan` | Stage block Tasks table `_pending_` rows | `#### §Stage File Plan` tuples | §3.4 subsection #1 |
| `stage-file-apply` | `§Stage File Plan` tuples | BACKLOG yaml + spec stubs; flips Task `_pending_ → Draft`; Stage `Draft → In Progress` (R2); master plan `Draft → In Progress` (R1) | §6.1 R1, §6.2 R2, §6.3 `_pending_ → Draft` |
| `plan-author` | Task specs post-filing | `§Plan Author` in each Task spec | Task status `Draft → In Review` (§6.3) |
| `plan-digest` | `§Plan Author` in each Task spec | `§Plan Digest` in each Task spec; `docs/implementation/{slug}-stage-{ID}-plan.md` | — (intra-spec) |
| `plan-review` | Stage + all Task specs | `#### §Plan Fix` tuples | §3.4 subsection #2 |
| `plan-applier` Mode plan-fix | `§Plan Fix` tuples | Edits Task specs verbatim | — |
| `spec-implementer` | Task spec §Plan Digest | Source code + Task spec §7 | Task status `In Review → In Progress` (§6.3) |
| `opus-code-reviewer` | Task diff vs spec | Task spec §Code Review / `§Code Fix Plan` | — (intra-spec) |
| `opus-audit` | N Task specs post-verify | `#### §Stage Audit` paragraphs | §3.4 subsection #3 |
| `stage-closeout-plan` | Stage header + N §Audit + N §Findings | `#### §Stage Closeout Plan` tuples | §3.4 subsection #4 |
| `plan-applier` Mode stage-closeout | `§Stage Closeout Plan` tuples | Archives BACKLOG yaml; deletes specs; flips Task rows `Done (archived)`; Stage `In Progress → Final` (R3); master plan `In Progress → Final` (R5) | §6.1 R5, §6.2 R3, §6.3 `Done (archived)` |
| `ship-stage` | Stage block | Dispatches per-Task implement + Stage-end verify + code-review + audit + closeout | — (chain) |

Full seam contract: [`ia/rules/plan-apply-pair-contract.md`](../rules/plan-apply-pair-contract.md). Status flip matrix: [`ia/rules/orchestrator-vs-spec.md`](../rules/orchestrator-vs-spec.md).

---

## 8. Orchestration guardrails — canonical block

Every master plan terminates with the `## Orchestration guardrails` H2. Canonical body:

```markdown
## Orchestration guardrails

**Do:**

- Open one Stage at a time. Next Stage opens only after current Stage's closeout runs.
- Run `/stage-file {this-doc} Stage N.M` to materialize `_pending_` Tasks.
- Update Stage + Task `Status` via lifecycle skills — do NOT edit by hand.
- Preserve locked decisions. Changes require explicit re-decision + sync edit to exploration + scope-boundary docs.
- Extend via `/master-plan-extend {this-doc} {source-doc}` — do NOT hand-insert new Stage blocks.

**Do not:**

- Close the orchestrator via `/closeout` — orchestrators are permanent (`orchestrator-vs-spec.md`).
- Silently promote post-MVP items into MVP Stages — they belong in scope-boundary doc.
- Merge partial Stage state — every Stage lands on a green bar.
- Insert BACKLOG rows directly into this doc — only `stage-file-apply` materializes them.
- Hand-insert new Stages past the last persisted `### Stage N.M` block — run `/master-plan-extend`.
```

Plans MAY append plan-specific Do/Do-not bullets but MUST preserve the canonical lines.

---

## 9. Validators

| Validator | What it checks | When it runs |
|-----------|----------------|--------------|
| `npm run validate:master-plan-status` | Header `Status` ↔ Stage `Status` ↔ Task row status ↔ BACKLOG yaml on-disk consistency (R1–R6) | CI + post-`/stage-file` + post-`/closeout` |
| `npm run validate:backlog-yaml` | BACKLOG yaml schema | CI + post-`/stage-file` |
| `npm run validate:dead-project-specs` | Orphan `ia/projects/{ISSUE_ID}.md` specs with no yaml | CI + post-`/closeout` |
| `npm run validate:all` | Aggregate — runs all of the above | CI + every Stage seam closure |

Any structural drift (Step heading, Phase column, H4 Stage, missing §Stage Audit subsection) surfaces as `validate:master-plan-status` non-zero exit.

---

## 10. Cross-references

- [`ia/rules/project-hierarchy.md`](../rules/project-hierarchy.md) — hierarchy table, cardinality rationale, lazy materialization, learnings-flow-backward, ephemeral spec lifecycle.
- [`ia/rules/orchestrator-vs-spec.md`](../rules/orchestrator-vs-spec.md) — orchestrator vs project-spec distinction + full Status flip matrix R1–R6.
- [`ia/rules/plan-apply-pair-contract.md`](../rules/plan-apply-pair-contract.md) — `§Plan` tuple shape + 4 pair seams + validators + escalation rule + idempotency.
- [`ia/templates/master-plan-template.md`](../templates/master-plan-template.md) — seed fixture for `master-plan-new` (conforms to this doc).
- [`ia/templates/project-spec-template.md`](../templates/project-spec-template.md) — per-issue spec shape (NOT master-plan; sibling doc).

## 11. Changelog

### 2026-04-24 — Initial

First canonical master-plan structure doc. Consolidates shape rules previously scattered across `master-plan-template.md` HTML comments + `project-hierarchy.md` table + inline skill definitions in `master-plan-new` / `master-plan-extend` / `stage-decompose` / `stage-file-plan` / `stage-file-apply` / `ship-stage`. Adds written schema for 5-column Task table (previously only in template HTML comment). Adds mandatory `§Stage Audit` subsection in Stage block (previously convention-only in some plans). Removes Steps + Phases + Phase column + H4 Stages (all retired post lifecycle-refactor).
