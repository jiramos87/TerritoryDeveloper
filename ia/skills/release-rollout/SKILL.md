---
purpose: "Umbrella rollout orchestration — track + drive every child master-plan under an umbrella (e.g. full-game-mvp) through the 7-column lifecycle (a)–(g) to step (f) ≥1-task-filed."
audience: agent
loaded_by: skill:release-rollout
slices_via: backlog_issue, backlog_search, list_specs, spec_outline, router_for_task, glossary_discover, glossary_lookup, rule_content
name: release-rollout
description: >
  Use when a multi-bucket umbrella master-plan (e.g. `full-game-mvp-master-plan.md`) needs a repeatable
  rollout process that drives each child orchestrator through the lifecycle (a)–(g) up to step (f)
  ≥1-task-filed. Orchestrates per-row handoffs to `/design-explore`, `/master-plan-new`,
  `/master-plan-extend`, `/stage-decompose`, `/stage-file`. Owns the tracker doc
  (`ia/projects/{umbrella-slug}-rollout-tracker.md`) + invokes helper skills
  (`release-rollout-enumerate`, `release-rollout-track`, `release-rollout-skill-bug-log`). Does NOT close
  issues (= `/closeout`). Does NOT execute Tier A→E rollout body directly — dispatches to per-row
  subagents in fresh context. Triggers: "/release-rollout {row-slug}", "rollout next row",
  "drive child plan to task-filed", "release rollout track".
model: inherit
---

# Release rollout — umbrella orchestration skill

Caveman default — [`agent-output-caveman.md`](../../rules/agent-output-caveman.md). Exceptions: tracker prose + disagreements appendix (human-consumed cold; 2–4 sentences where required); commit messages; verbatim MCP payloads.

No MCP from skill body beyond the Tool recipe below.

**Lifecycle:** AFTER an umbrella `ia/projects/{umbrella-slug}-master-plan.md` is authored AND the sibling tracker `ia/projects/{umbrella-slug}-rollout-tracker.md` is seeded (by `release-rollout-enumerate`). BEFORE per-row `/design-explore` / `/master-plan-new` / `/master-plan-extend` / `/stage-decompose` / `/stage-file`. Drives rollout until every row reaches column (f) `✓`.

`design-explore` → `master-plan-new` → `master-plan-extend` → `stage-decompose` → `stage-file-planner` → `stage-file-applier` → `plan-author` → `plan-digest` → `plan-reviewer` (→ `plan-applier` on critical, cap=1) → `project-new` → `project-spec-implement` → `/closeout` (Stage-scoped). Release-rollout sits ABOVE this chain — it does not replace it; it sequences multiple child chains under one umbrella.

**Related:** [`release-rollout-enumerate`](../release-rollout-enumerate/SKILL.md) · [`release-rollout-track`](../release-rollout-track/SKILL.md) · [`release-rollout-skill-bug-log`](../release-rollout-skill-bug-log/SKILL.md) · [`master-plan-new`](../master-plan-new/SKILL.md) · [`master-plan-extend`](../master-plan-extend/SKILL.md) · [`stage-file-plan`](../stage-file-plan/SKILL.md) · [`stage-file-apply`](../stage-file-apply/SKILL.md) · [`ia/rules/orchestrator-vs-spec.md`](../../rules/orchestrator-vs-spec.md) · [`ia/rules/project-hierarchy.md`](../../rules/project-hierarchy.md) · [`ia/skills/README.md`](../README.md).

**Shape ref:** [`docs/full-game-mvp-rollout-tracker.md`](../../../docs/full-game-mvp-rollout-tracker.md) (canonical tracker shape — 11 rows + disagreements + skill iteration log).

---

## Inputs

| Parameter | Source | Notes |
|-----------|--------|-------|
| `UMBRELLA_SPEC` | User prompt | Path to `ia/projects/{slug}-master-plan.md`. Required. Must exist + match orchestrator shape + carry a bucket-table section (one row per child plan). |
| `TRACKER_SPEC` | Derived | `ia/projects/{slug}-rollout-tracker.md`. Required. Must exist (seeded by `release-rollout-enumerate`). Missing → STOP, route to enumerate skill. |
| `ROW_SLUG` | User prompt | Optional — specific row to advance (e.g. `city-sim-depth`, `zone-s-economy`). If absent, umbrella picks next row per Tier ordering + parallel-work rule. |
| `OPERATION` | User prompt | Optional mode — `advance` (default: tick next cell for ROW_SLUG), `status` (read-only snapshot), `next` (return Tier-ordered next-row recommendation). |

---

## Phase sequence (gated)

### Phase 0 — Load + validate

1. Read `{UMBRELLA_SPEC}`. Confirm orchestrator shape + bucket-table section present.
2. Read `{TRACKER_SPEC}`. Missing → STOP, route to `release-rollout-enumerate`.
3. If `ROW_SLUG` provided → locate matching row. Missing row → STOP, ask user to pick from tracker OR seed via enumerate.
4. If `OPERATION = status` → skip to Phase 5 (read-only snapshot).
5. If `OPERATION = next` → skip to Phase 4 (next-row pick only).

### Phase 1 — Row state read

Run `rollout-row-state` subskill ([`ia/skills/rollout-row-state/SKILL.md`](../rollout-row-state/SKILL.md)). Inputs: `TRACKER_SPEC`, `ROW_SLUG`.

Subskill returns `{cells, target_col, hard_gate, chain_ready, next_action}`.

Apply hard gates from returned result:
- `hard_gate = "DISAGREEMENT"` → STOP. Surface matching Disagreements appendix entry. Route to user for pick.
- `hard_gate = "EQUIVALENCE_GATE"` → STOP. Surface equivalence question. Route to user for pick.
- `target_col = "(g)"` → route to Phase 3 (align-gate sub-step) before (e) can be ticked.
- `target_col = "terminal"` → row done. Skip to Phase 6 (next-row recommendation).

Output from subskill: target column + next action (e.g. `target_col = (d); next_action = stage-decompose ia/projects/city-sim-depth-master-plan.md Step 1`).

### Phase 2 — MCP context (Tool recipe)

Run Tool recipe. Scope = ROW_SLUG's child orchestrator + target column action. Capture:

- Relevant MCP-routed spec sections → surface to handoff message.
- Glossary canonical names → used in column (g) align gate verification.
- Invariant numbers at risk → per-row guardrail note.
- Existing BACKLOG ids referenced by row (via `backlog_search` on slug).

Skip Tool recipe if `OPERATION = status` (read-only).

### Phase 3 — Align gate check (column (g) — only when target cell = (e))

Run `term-anchor-verify` subskill ([`ia/skills/term-anchor-verify/SKILL.md`](../term-anchor-verify/SKILL.md)) for every NEW domain entity introduced by this row. Inputs: `terms` = English entity names from child orchestrator Objectives.

`all_anchored = true` → column (g) `✓`. `all_anchored = false` → column (g) stays `—` with Skill Iteration Log note naming `unresolved_terms`. STOP; route user to: author glossary row + spec section anchor for each unresolved term before re-fire. Does NOT block columns (a)–(d) or (f) — only (e).

### Phase 4 — Handoff dispatch (autonomous chain)

**Default behavior:** when (b) ✓, chain (c)→(d)→(e)→(f) autonomously via Agent tool without pausing for user. Each step waits for prior subagent to return before dispatching next.

**Autonomous chain (b) ✓ path:**
1. Call Agent `master-plan-new` subagent with exploration doc path.
2. Wait for success. Read authored plan to find first Stage (Stage 1.1 or equivalent).
3. Dispatch the `/stage-file` agent chain against that Stage — sequence these five (or six on critical) subagents in order, waiting for each to return before dispatching the next: (i) `stage-file-planner` → (ii) `stage-file-applier` → (iii) `plan-author` → (iv) `plan-digest` → (v) `plan-reviewer`. If `plan-reviewer` returns PASS → proceed to Phase 5. If `plan-reviewer` returns critical → (vi) dispatch `plan-applier` Mode plan-fix, then re-dispatch `plan-reviewer` once (cap=1). Second critical → abort chain + surface to user.
4. Wait for PASS → (f) ✓. Proceed to Phase 5.

**Human pause conditions (break the chain):**
- (b) INCOMPLETE (no Design Expansion block) → PAUSE. Run product-language interview (≤5 questions in game-design vocabulary — no class names, method signatures, or C# internals; ask about player experience, game rules, economic mechanics). Then call Agent `design-explore`.
- `⚠️` marker → STOP. Surface Disagreements appendix entry + user pick.
- (b) `❓` equivalence gate → STOP. Surface equivalence question + user pick.
- Any subagent returns failure/blocker → STOP. Surface blocker to user.

| Target cell | Dispatch method | Human pause? |
|-------------|-----------------|--------------|
| (a) | Invoke `release-rollout-enumerate` helper skill | No |
| (b) INCOMPLETE | PAUSE + product-language interview → Agent `design-explore` subagent | YES — interview |
| (b) COMPLETE | Agent `master-plan-new` subagent → auto-chain to (f) | No |
| (b) LOCKED + `--against` | Agent `design-explore --against {UMBRELLA_SPEC}` subagent | No |
| (c) NEW | Agent `master-plan-new` subagent | No |
| (c) EXTEND | Agent `master-plan-extend` subagent | No |
| (d)/(e) | Agent `stage-decompose` subagent | No |
| (f) | Sequential chain: `stage-file-planner` → `stage-file-applier` → `plan-author` → `plan-digest` → `plan-reviewer` (→ `plan-applier` Mode plan-fix on critical, cap=1), each step via Agent tool in order. Stage resolved from child plan first Stage. | No |
| (g) | Inline glossary_discover + spec authoring; no subagent | Only if MCP fails |

All `{slug}` / `{N}` / `{M}` / `{UMBRELLA_SPEC}` values MUST be resolved from tracker + child plan before dispatch — never use un-substituted placeholders.

`OPERATION = next` → emit Tier-ordered next-row recommendation only. No dispatch.

After (f) ✓ row terminal, emit summary:
```
{ROW_SLUG} → (f) ✓. chain: master-plan-new ({doc}) → stage-file-planner → stage-file-applier → plan-author → plan-digest → plan-reviewer ({Stage N.M}, {issue-ids}). Tier: {A|B|C|D|E}. Next-row recommendation below.
```

### Phase 5 — Tracker update

After dispatched subagent returns (success signal in its handoff message), dispatch tracker update via Agent tool:

**Cell flip:** call Agent `release-rollout-track` subagent. Inputs: `TRACKER_SPEC`, `ROW_SLUG`, `TARGET_COL`, `NEW_MARKER` (derived from completion evidence), `TICKET` (commit SHA / doc path / issue id), `CHANGELOG_NOTE` (one-line delta).

Subagent flips the target cell, runs column (g) align verify when relevant, appends Change log row, returns handoff line.

**Skill bug branch:** if dispatched subagent reported a skill bug/gap in its handoff message → call Agent `release-rollout-skill-bug-log` subagent. Inputs: `SKILL_NAME`, `TRACKER_SPEC`, `ROW_SLUG`, `BUG_SUMMARY`, `BUG_DETAIL`, `FIX_STATUS`.

Read-only `OPERATION = status` → emit tracker snapshot without edits. Return: row count, rows at each column, disagreements count, open blockers.

Read-only `OPERATION = status` → emit tracker snapshot without edits. Return: row count, rows at each column, disagreements count, open blockers.

### Phase 6 — Next-row recommendation

After tracker update (or on `OPERATION = next`), emit Tier-ordered next-row pick. Heuristic:

1. **Skip `sibling` rows.** Tracker rows whose Tier column = `sibling` are side-quest features — NOT part of the MVP critical path. Skip them unless the user explicitly asks to advance a sibling row (e.g. by passing `ROW_SLUG = music-player` directly). Never surface a sibling row as the next recommended MVP action.
2. Tier A (foundations — save-schema v2→v3 owned by Bucket 3 zone-s-economy) first if (c) `—`.
3. Tier B/B' (parallel polish — city-sim-depth, ui-polish, sprite-gen, blip) — pick row closest to column (f) without parallel-work conflict.
4. Tier C (spine — utilities + landmarks + zone-s-economy spine integration) only AFTER Tier A (c).
5. Tier D (CityStats + web-platform parity) — after column (d) on all Tier B rows.
6. Tier E (distribution) — last.

Parallel-work rule: NEVER emit two sibling rows at same Tier with both targeting `/stage-file` or `/closeout` concurrently. Sequence instead.

---

## Tool recipe (territory-ia) — Phase 2 only

**Composite-first call (MCP available):**

1. Call `mcp__territory-ia__orchestrator_snapshot({ slug: "{umbrella-slug}" })` — first MCP call; returns umbrella orchestrator state, stage/task inventory, and rollout tracker row. Use snapshot to resolve current row state + target column without separate reads.
2. **`list_specs`** — enumerate existing specs for align-gate reference.
3. Run `domain-context-load` subskill ([`ia/skills/domain-context-load/SKILL.md`](../domain-context-load/SKILL.md)). Inputs: `keywords` = English tokens from ROW_SLUG scope (domain entities from umbrella bucket row + child orchestrator Objectives); `brownfield_flag = false`; `tooling_only_flag = false`. Use returned `glossary_anchors` (flag missing rows as column (g) gate signal), `router_domains`, `spec_sections`, `invariants` for Phase 3–5 context.
4. **`backlog_search`** — `ROW_SLUG` as search term. Capture open BACKLOG ids tied to this row.
5. **`backlog_issue`** — only if specific id needs full context.

### Bash fallback (MCP unavailable)

1. **`list_specs`** — enumerate existing specs for align-gate reference.
2. Run `domain-context-load` subskill ([`ia/skills/domain-context-load/SKILL.md`](../domain-context-load/SKILL.md)). Inputs: `keywords` = English tokens from ROW_SLUG scope (domain entities from umbrella bucket row + child orchestrator Objectives); `brownfield_flag = false`; `tooling_only_flag = false`. Use returned `glossary_anchors` (flag missing rows as column (g) gate signal), `router_domains`, `spec_sections`, `invariants` for Phase 3–5 context.
3. **`backlog_search`** — `ROW_SLUG` as search term. Capture open BACKLOG ids tied to this row.
4. **`backlog_issue`** — only if specific id needs full context.

Skip recipe entirely if `OPERATION = status`.

---

## Guardrails

- IF `{TRACKER_SPEC}` does not exist → STOP. Route to `release-rollout-enumerate {UMBRELLA_SPEC}`.
- IF `{UMBRELLA_SPEC}` does not exist → STOP. Route to `/master-plan-new` against the umbrella exploration doc.
- IF `ROW_SLUG` not found in tracker → STOP. Ask user to pick from tracker OR seed via enumerate.
- IF row marker = `⚠️` (active disagreement) → STOP. Surface Disagreements appendix entry; route to user pick. Question stem + option labels use product/domain wording (game/feature semantics), not row slugs or cell coords — Ids and tracker cells go on a trailing `Context:` line. Full rule: [`ia/rules/agent-human-polling.md`](../../rules/agent-human-polling.md).
- IF column (b) = `❓` on a row (design-expansion equivalence gate) → STOP. Surface equivalence question; route to user pick. Same polling-wording rule applies.
- IF column (g) = `—` or `❓` AND target cell = (e) → STOP. Route to Phase 3 align-gate authoring (glossary row + spec section).
- IF align gate (Phase 3) fails → write Skill Iteration Log entry naming unresolved terms; do NOT flip (e) cell.
- IF parallel-work rule would be violated (two sibling rows at `/stage-file` or `/closeout` concurrently on same branch) → STOP. Emit Tier-ordered next-row pick excluding the conflict.
- IF subagent returns failure/blocker at any chain step → STOP. Surface to user; do NOT continue chain.
- IF (b) incomplete (no Design Expansion) → PAUSE. Interview user in product/game-design language ONLY (player experience, game rules, mechanics, UI — no class names, file paths, C# signatures). Max 5 questions one-at-a-time. Then dispatch design-explore. Full polling rule: [`ia/rules/agent-human-polling.md`](../../rules/agent-human-polling.md).
- Do NOT pause between (c)→(f) when (b) ✓ — chain autonomously via Agent tool.
- Do NOT close issues — that is `/closeout`.
- Do NOT directly author child master-plan Steps — delegate to `master-plan-new` / `master-plan-extend` subagents.
- Do NOT touch other rows' cells when advancing one row.
- Do NOT commit — user decides when to commit tracker updates.
- Do NOT touch `.claude/settings.json` `permissions.defaultMode` or `mcp__territory-ia__*` wildcard.
- IF emitting next-row recommendation → wrap as `claude-personal "/release-rollout {UMBRELLA_SPEC} {next-row}"` with ALL placeholders resolved. See `docs/agent-lifecycle.md` §10.

---

## Seed prompt

```markdown
Run the release-rollout workflow against {UMBRELLA_SPEC} and {TRACKER_SPEC}.

Follow ia/skills/release-rollout/SKILL.md end-to-end. Inputs:
  UMBRELLA_SPEC: {path to umbrella master plan}
  TRACKER_SPEC: {path to rollout tracker — usually sibling}
  ROW_SLUG: {optional — specific row to advance}
  OPERATION: {advance | status | next} (default: advance)

Phase 0 validates both specs. Phase 1 reads row state + identifies next-cell-to-tick. Phase 2 runs Tool recipe (skipped on OPERATION=status). Phase 3 runs column-(g) align gate if target = (e). Phase 4: when (b) ✓ → autonomous chain (c)→(f) via Agent tool (master-plan-new → read first Stage → stage-file-planner → stage-file-applier → plan-author → plan-digest → plan-reviewer, with plan-applier on critical, cap=1); human pause ONLY for incomplete (b), ⚠️, ❓, or subagent failure. Phase 5 invokes release-rollout-track after each subagent returns. Phase 6 emits Tier-ordered next-row pick.

Hard STOPs:
- Tracker missing → release-rollout-enumerate first.
- ⚠️ row → Disagreements appendix + user pick.
- (b) = ❓ → equivalence pick + user resolution.
- (g) = — with (e) target → authoring required before advance.
- Parallel-work conflict → next-row pick instead.
- Subagent failure/blocker → surface to user, stop chain.

Do NOT close issues (= /closeout). Do NOT commit. Do NOT pause between (c)→(f) when (b) ✓.
```

---

## Next step

After (f) ✓ + tracker update → Phase 6 emits Tier-ordered next-row recommendation. User runs `claude-personal "/release-rollout {UMBRELLA_SPEC} {next-row}"` (resolve `{next-row}` from pick) OR confirms umbrella complete (every row column (f) `✓`). Agent does NOT auto-start the next row — user picks when to continue.

Umbrella-complete state = rollout terminal. Does NOT close umbrella master-plan (permanent per `ia/rules/orchestrator-vs-spec.md`). Does NOT delete tracker (permanent sibling).

---

## Changelog

### 2026-04-17 — Shell-wrap handoff commands

**Status:** applied (pending commit)

**Symptom:**
Phase 4 dispatch table + §Next step emitted bare slash commands (e.g. `/release-rollout {next-row}`). User pasted into shell, received "command not found" — bare `/release-rollout` is not a terminal binary.

**Root cause:**
Skill authored assuming cold-agent ingest of `/` slash-commands. Actual handoff target = fresh terminal session launched via `claude-personal "..."`. Convention missed at authoring time.

**Fix:**
Phase 4 table + Phase 4 handoff shape + §Next step + Guardrails all updated to require `claude-personal "/..."` wrap with fully-resolved args. Guardrail aligns with `docs/agent-lifecycle.md` §10.

**Rollout row:** all rows (skill-level fix, not row-specific)

**Tracker aggregator:** [`docs/full-game-mvp-rollout-tracker.md#skill-iteration-log`](../../../docs/full-game-mvp-rollout-tracker.md#skill-iteration-log)

---

### 2026-04-17 — Autonomous chain + product-language interview

**Status:** applied (pending commit)

**Symptom:**
When (b) ✓, agent stopped and emitted a paste-ready command for user to run manually. User expected agent to chain autonomously through master-plan-new → stage-file without input.

**Root cause:**
Phase 4 emit-only dispatch. No autonomous chaining. No product-language interview for incomplete (b). Agent tool missing from subagent tools list.

**Fix:**
1. Phase 4 rewritten: (b) ✓ → Agent tool calls master-plan-new → reads plan → Agent tool calls stage-file. Chain (c)→(f) without pausing.
2. Human pause narrowed: only (b) incomplete, ⚠️, ❓, subagent failure.
3. (b) incomplete: product/game-design language interview (no C# internals, ≤5 questions one-at-a-time).
4. `Agent` tool added to subagent tools list.
5. Guardrails + Seed prompt + §Next step updated.

**Rollout row:** all rows (skill-level fix)

**Tracker aggregator:** [`docs/full-game-mvp-rollout-tracker.md#skill-iteration-log`](../../../docs/full-game-mvp-rollout-tracker.md#skill-iteration-log)

---
