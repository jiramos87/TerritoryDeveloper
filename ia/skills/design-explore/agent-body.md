# Mission

Expand exploration doc at `{DOC_PATH}` into a reviewed, persisted design. Args: `DOC_PATH` (required), optional `APPROACH_HINT`, optional `--against {AGAINST_DOC}`. Output: `## Design Expansion` block appended (or updated in place). Does NOT create master plan / BACKLOG row — propose next step at end.

# Recipe

Follow `ia/skills/design-explore/SKILL.md` end-to-end. Phase sequence (gated):

0. **Load** — Read `{DOC_PATH}`. Extract problem statement, approaches list, existing recommendation, open questions. Then run **locked-doc detection**:
   - Has Approaches list → standard mode, continue Phase 1.
   - No Approaches list + `AGAINST_DOC` set → **gap-analysis mode**: skip to Phase 0b below.
   - No Approaches list + no `AGAINST_DOC` → STOP, offer three options: (A) add Approaches section + re-run, (B) pass `--against {UMBRELLA_DOC}` for gap analysis, (C) skip to `/ship-plan --version-bump` if no gaps expected.
   - Unreadable → STOP, report path error.

**Standard mode** (has Approaches list):

0.5. **Interview (user gate)** — Before Phase 1, run a short interview. Ask **ONE question per turn, stop, wait for the user's answer** before asking the next. Do NOT list questions. Pull from: (1) open questions in the doc, (2) up to 3 inferred questions about scope boundaries, blocking constraints, or priority trade-offs. Max 5 questions; stop early if answers already cover remaining ones. After the last answer emit a one-paragraph summary, then proceed. No extra confirmation prompt.
1. **Compare + Exit Gate** — BEFORE building the criteria matrix, run a relentless `AskUserQuestion` polling loop (per [`ia/rules/agent-human-polling.md`](../../rules/agent-human-polling.md)) to resolve ALL unresolved decisions. Each round: list outstanding decisions as numbered preamble → ask 1-4 questions → pause. Loop re-runs while ≥1 decision remains open. Phase 1 exits ONLY when (a) zero decisions remain AND (b) human types `phase-1-done` OR picks "close phase 1" in a poll. Then build criteria matrix (constraint fit, effort, output control, maintainability, dependencies/risk) and emit Markdown table.
2. **Select (user gate)** — If recommendation unambiguous AND no `APPROACH_HINT` → proceed. Else → present table + leading candidate, PAUSE, ask user confirm/override.
2.5. **Architecture Decision (DEC-A15 lock)** — fires when selected approach touches `arch_surfaces` (skip-clause: zero arch hits → silent no-op). 4 sequential AskUserQuestion polls (slug → rationale → alternatives → affected `arch_surfaces[]`). MCP writes (in order): `arch_decision_write` (status=active) → `cron_arch_changelog_append_enqueue` (kind=`design_explore_decision`, fire-and-forget; cron drains to `arch_changelog`) → `arch_drift_scan` against open master plans. Drift report appended inline to exploration doc under `### Architecture Decision` block (sibling of `### Architecture` Phase 4 block). Stop on any MCP write failure.
3–9. Expand → Architecture + Red-Stage Proofs + YAML Emitter → Subsystem impact → Implementation points → Examples → Subagent review → Persist under `## Design Expansion`.

**Phase 4 additions (inherit verbatim):**
- Per-stage red-stage proof block: mandatory pseudo-code (5–15 lines, Python-flavoured), one block per Stage, under `### Red-Stage Proof — Stage {N}`. References glossary terms only.
- Per-task proof: opt-in (emit only when human signals during grilling).
- Lean YAML frontmatter at top of `docs/explorations/{slug}.md` (or `{slug}-v{N+1}.md` on --resume). Required keys: `slug`, `parent_plan_slug`, `target_version`, `stages[]`, `tasks[]` (each task: `prefix`, `depends_on`, `digest_outline`, `touched_paths`, `kind`). Bounded by `---` fences.
- `--resume {slug}` mode: read existing plan via `master_plan_render` + `master_plan_lineage` MCP (TECH-14103); re-grill ONLY stages where `backfilled = true` OR band = `partial`; skip `present_complete` stages. Versioned filename: `{slug}-v{N+1}.md` when `target_version > 1`. Exit Phase 4 with YAML `target_version = existing_max_version + 1`.

**Gap-analysis mode** (`--against {AGAINST_DOC}` set, locked doc):

0b. **Load reference doc** — Read `{AGAINST_DOC}`. Extract every cross-reference to the system in `DOC_PATH`: exit gates, tier conditions, interface contracts, locked decisions that constrain this system. Assign each requirement an id (R1, R2, …).
1g. **Gap inventory** — Compare requirements against current `DOC_PATH` design. Build gap table: `Req | Source | Current coverage | Gap severity (Blocking/Additive/Deferred)`.
2g. **Confirm gate** — Present gap table. PAUSE — ask user to confirm gaps or trim before expanding.
3–7. **Expand gaps** — same as standard Phases 3–7, scoped to confirmed gaps. Phase 4 Architecture only if gaps introduce new components. Phase 6 one checklist block per gap.
8. **Subagent review** — same prompt template.
9g. **Persist** — derive context title from `AGAINST_DOC` slug (e.g. `full-game-mvp-exploration.md` → `## Design Expansion — MVP Alignment`; bare master-plan slug `full-game-mvp` → same). Append as new named section after any existing `## Design Expansion` block (never overwrite it). Never overwrite Problem / Approaches surveyed / Recommendation / Open questions.

# Tool recipe (Phase 5 only)

Skip `invariants_summary` for tooling/pipeline-only designs that touch no runtime C#.

1. `mcp__territory-ia__glossary_discover` — `keywords` JSON array: English tokens from selected-approach components + Phase 3 interface names.
2. `mcp__territory-ia__glossary_lookup` — high-confidence terms from discover.
3. `mcp__territory-ia__router_for_task` — 1–3 domains from component responsibilities.
4. `mcp__territory-ia__spec_sections` — implied by touched subsystems; set `max_chars`. No full spec reads.
5. `mcp__territory-ia__invariants_summary` — if approach touches runtime C# / Unity subsystems.

# Hard boundaries

- IF approach not confirmed after Phase 2 → STOP, ask user. Do NOT guess.
- IF subagent review returns BLOCKING items → resolve, re-run Phase 8, then persist.
- IF `{DOC_PATH}` unreadable → stop, report path error.
- IF touched subsystem spec unavailable via MCP → note gap in Subsystem Impact, continue.
- Do NOT overwrite Problem / Approaches surveyed / Recommendation / Open questions — only write the `## Design Expansion` block.
- Do NOT create master plan, BACKLOG row, or invoke `project-new` — user triggers next step after review.
- Do NOT commit — user decides when.
- Do NOT load whole reference specs when `spec_section` / `spec_sections` slices cover it.
- Do NOT skip `invariants_summary` when runtime C#/Unity subsystems touched.

# Persist structure

Write sections in order under `## Design Expansion`: Chosen Approach → Architecture Decision (Phase 2.5 — skip block when no arch surfaces hit) → Architecture → Subsystem Impact → Implementation Points → Examples → Review Notes → Expansion metadata (Date ISO, Model, Approach selected, Blocking items resolved N).

# Output

Single concise caveman message:

1. Doc path + mode (standard: approach id + name; gap-analysis: gap count confirmed).
2. Phases completed (0–9) + any skipped (reason).
3. Subsystem impact summary (count touched, invariants flagged by number).
4. Review results (BLOCKING resolved count, NON-BLOCKING carried into Review Notes).
5. Persist diff summary (sections written / updated, line delta).
6. Next step — standard: `ship-plan` or `project-new`; gap-analysis: `claude-personal "/ship-plan --version-bump {SLUG} {DOC_PATH}"`.
