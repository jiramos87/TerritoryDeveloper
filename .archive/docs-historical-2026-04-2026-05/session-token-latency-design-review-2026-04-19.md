# Design review — `session-token-latency-audit-exploration.md`

> **Origin.** Written 2026-04-19 by an external reviewing agent in `/Users/javier/teselagen/lims-2` (`TEMP-session-token-latency-design-review-2026-04-19.md`, disposable). Copied into this repo 2026-04-19 as input for the `/design-explore docs/session-token-latency-audit-exploration.md` Phase 0.5 interview — companion to the source audit [`ai-mechanics-audit-2026-04-19.md`](ai-mechanics-audit-2026-04-19.md). Original `TEMP-…` file in `lims-2` remains disposable.
>
> **Status.** External-view hypotheses to verify, not facts to transcribe. Several quantitative claims (token counts, cold-start timings) are estimates from file-size × 0.25 heuristics; the `/design-explore` Opus agent should cross-check against real MCP descriptor byte counts, Q9 telemetry (when landed), and observed behaviour before adopting into the exploration doc.
>
> **Reviewed doc:** [`session-token-latency-audit-exploration.md`](session-token-latency-audit-exploration.md) (278 lines, created 2026-04-19).
>
> **Sibling plans consulted:**
> - [`ia/projects/lifecycle-refactor-master-plan.md`](../ia/projects/lifecycle-refactor-master-plan.md) (384 lines; In Progress Stage 5; Stage 10 is Draft/Q9-gated and post-M8).
> - [`ia/projects/mcp-lifecycle-tools-opus-4-7-audit-master-plan.md`](../ia/projects/mcp-lifecycle-tools-opus-4-7-audit-master-plan.md) (491 lines; In Progress Step 2 / Stage 2.2; Stages 1–5 Done, 6–16 Draft; Stage 17+ green-field).

---

## 0. Headline verdict

The exploration is thorough, well-sequenced against siblings, and honest about conflicts. It is **not yet the right shape to dispatch `/design-explore` + `/master-plan-new` on as written**. Three structural problems need addressing before the authoring pass:

1. **Theme B belongs inside `mcp-lifecycle-tools-opus-4-7-audit-master-plan.md`, not a new orchestrator.** It writes to the same surface (`tools/mcp-ia-server/`, `docs/mcp-ia-server.md`, agent frontmatter `tools:`) as Stages 6–16 Draft in mcp-lifecycle. Two master plans with overlapping write paths is a split-brain risk the rules explicitly try to avoid (MEMORY.md: *"Parallel-work rule for sibling orchestrators — NEVER run `/stage-file` or `/closeout` against two sibling master-plans concurrently on same branch"*). Extend mcp-lifecycle by ≈3 stages instead.
2. **Theme 0 "early-escape during FREEZE" contradicts the FREEZE itself.** The proposal says quick-wins file under a parent `tech-quick-wins-master-plan.md` **authored post-M8**, yet also proposes running them during FREEZE. Post-M8 and during-FREEZE are mutually exclusive by construction. Pick one; the doc should be explicit.
3. **Baseline measurement (Open Q10) must be a Stage 0, not an open question.** Every cost claim in the audit (5–8k tokens here, 2–3k there, ~20–30k/session total) is an estimate. Without a pre/post measurement harness, Stage sign-offs have no objective gate. Fold Q10 into a named Stage 0 that runs first and gates everything.

The remainder (sibling coordination, theme grouping, sequencing notes, cross-cutting principles) is sound. Fixes below are scoped and keep the audit's structure.

---

## 1. Design strengths (keep)

- **Item-level dedup vs siblings** in §Sibling master plan coordination is exemplary — 1 dropped, 1 superseded, 4 sequenced, 2 folded, 24 independent. This alone justifies the exploration existing as a separate doc.
- **Scope bar** explicitly lists out-of-scope siblings with file paths. Matches the CLAUDE.md pattern and prevents scope creep.
- **Cross-cutting themes** (authority chain, cache-prefix stability, progressive disclosure, hook zero-fork budget) are the right level of abstraction — these become design principles for the authored orchestrator, not item-level rules.
- **Provenance block** retains audit ids (C1–C5, M1–M8, m1–m10, O1–O7). Correct — downstream Task titles will carry them for traceability.
- **Theme F retention** (speculative / rev-4-era / harness-gated items) in a separate bucket with a lower confidence bar is correct. F3 (`output-style: caveman` frontmatter) is genuinely uncertain on feasibility; tagging it that way is honest.

---

## 2. Design gaps / fixes

### G1. Theme B should be folded into `mcp-lifecycle-tools-opus-4-7-audit-master-plan.md` as new Stages

**Why.** Every Theme B item writes to territory Stage 6–16 already owns:

| Theme B item | Writes to | mcp-lifecycle Stage that co-owns |
|---|---|---|
| B1 server split / `defer_loading` | `.mcp.json`, `tools/mcp-ia-server/src/index.ts` | 7, 8 (composite tools register here) |
| B3 per-agent allowlist | `.claude/agents/*.md` `tools:` frontmatter | 9 T9.4 (subagent-body sweep) |
| B4 parse cache + dist build | `tools/mcp-ia-server/src/config.ts`, `.mcp.json` | 3–16 (every MCP stage builds against dist) |
| B5 progressive disclosure | `tools/mcp-ia-server/src/tools/spec-outline.ts`, `list-rules.ts` | 4 (envelope wrap) |
| B6 doc drift lint | `docs/mcp-ia-server.md`, `tools/scripts/validate-mcp-readme.ts` | 9 T9.4 (explicitly cited) |
| B7 `DEBUG_MCP_COMPUTE=1` flag | `.mcp.json` env | any |
| B8 yaml-first parser order | `tools/mcp-ia-server/src/parser/backlog-parser.ts` | 4 (envelope wrap touches same file) |
| B9 `.describe()` ≤120 char lint | all `src/tools/*.ts` descriptor prose | 9 T9.4 + all Stages that author new tools |

Concretely: B4 switches `.mcp.json` from `tsx` to `dist/`. Stage 11–16 mutation tools ship as `registerTool` calls. If B4 lands in a sibling plan and mcp-lifecycle has a mid-flight composite-tool stage open, the mcp-lifecycle subagent's `tsx` path resolution breaks on the next restart. **This is a merge-conflict-in-disguise.**

**Recommendation.** Do not author a new orchestrator for Theme B. Instead, run `/master-plan-extend docs/session-token-latency-audit-exploration.md --against ia/projects/mcp-lifecycle-tools-opus-4-7-audit-master-plan.md` with a scope limited to Theme B. The master plan gains ≈ 3 new Stages:

- **Stage 17 — Server surface pruning** (B1 + B9 + B6) — defer_loading / descriptor shrink / README lint.
- **Stage 18 — Cold-start + parser hygiene** (B4 + B8 + B7) — dist/ + yaml-first + DEBUG flag.
- **Stage 19 — Progressive disclosure** (B5 + B3) — `spec_outline depth=1` / `list_rules alwaysApply` / per-agent `tools:` audit.

The new `session-token-latency-master-plan.md` then owns **only** Themes A, C, D, E, F — no MCP-surface work. That's a clean surface split.

**Side benefit.** The `docs/session-token-latency-audit-exploration.md` doc stays authoritative for the full audit trail; the `--against {UMBRELLA_DOC}` flag on `/master-plan-extend` is already the supported pattern for "take a subset of an exploration and route it into an existing orchestrator" (see `ia/rules/agent-lifecycle.md` and `master-plan-extend` skill).

### G2. Theme 0 FREEZE contradiction — pick one path

The doc currently says:

> *"Theme 0 (quick wins) — ~1 week, ships during late lifecycle-refactor if the FREEZE lifts for this subset."*

And at the bottom:

> *"Theme-0 early-escape path (if user approves running quick-wins during FREEZE): `/project-new` per item for B6 / B7 / B9 / C3 / D1 / D3 / E1 / E2 / E3 individually (B2 already shipped via TECH-426), filed under a parent `tech-quick-wins-master-plan.md` authored post-M8."*

These contradict. FREEZE blocks `/master-plan-new` / `/master-plan-extend` / `/stage-decompose` / `/stage-file`. It does **not** block `/project-new` (single-issue path, no orchestrator coupling).

**Recommendation.** Split Theme 0 into two sub-classes:

- **Theme 0a — Standalone quick-wins (FREEZE-compatible).** Items that live as individual BACKLOG issues with no umbrella: **E1** (gitignore root blobs), **E2** (SessionStart memory-path fix), **D1** (Stop-hook delete), **D3** (pure-shell JSON), **B7** (DEBUG flag flip). All file-scoped, zero master-plan coupling. File via `/project-new` *during FREEZE* — the rules permit it. Parent: none. Tracker: ad-hoc "Quick wins" section in `BACKLOG.md` if grouping is wanted, or just issue tags.
- **Theme 0b — Grouped quick-wins with telemetry (post-M8).** Items that benefit from pre/post measurement coordination: **B6** (doc lint, wait on mcp-lifecycle T9.4), **B9** (describe lint), **C3** (skill-seed lint), **E3** (output-style trim). File post-M8 under the new `session-token-latency-master-plan.md` Stage 0 or Stage 1.

This resolves the contradiction and unlocks ~5 genuinely drop-in fixes during the FREEZE window (D1, D3, E1, E2, B7 = probably 2–3 hours total work, recovers 30–60 ms × N tool calls + ~0.5–1k tokens + noise reduction).

### G3. Open Q10 (baseline measurement) should be Stage 0, not an open question

Q10 asks whether to add a baseline measurement. Don't ask — require it. The exploration is explicitly argued on the premise that 20–30k tokens/session are recoverable. Without `pre` and `post` measurements, every Stage close is narrative, not quantified. Fold as:

**Stage 0 — Baseline measurement.** First Stage of the new orchestrator. Tasks:
- **T0.1** — Author `tools/scripts/measure-session-tokens.ts` that spawns a minimal Claude Code session with a fixed prompt (`"hi"` or equivalent no-op), captures the pre-work ambient tokens via the `--show-cost` / debug logs, and writes to `ia/state/token-baseline.json`.
- **T0.2** — Record N=3 baseline runs (cold cache, warm cache, post-autocompact) under the current repo state; commit baseline JSON.
- **T0.3** — Repeat post each Theme's sign-off; attach delta to the Stage close digest. Reject theme if measured delta < 30% of predicted; re-investigate root cause.

This also directly answers Q10 and makes Theme closes gating on data, not prose.

### G4. D2 (deterministic SessionStart preamble) needs a contract with Tier 1

The audit notes that volatile bytes at message prefix destabilise the 5m/1h cache window. The exploration correctly sequences D2 to complement lifecycle-refactor Stage 10 T10.2. But there's an unstated question: **does the hook stdout land BEFORE or AFTER the `@`-imported rules?** If before, the hook's output is now the cache prefix — and even a deterministic hook must be as cache-stable as Tier 1 itself. If after, the hook output sits between Tier 1 and the first message; its determinism matters less for Tier 1 but still destabilises Tier 2.

**Recommendation.** Add a Stage F sub-task that formalises the hook-output ordering constraint in `docs/prompt-caching-mechanics.md` §5 (sizing gate measures). One of:

- (a) Route all dynamic banner content to **stderr** (visible to humans, not part of message stream) — banner becomes information only.
- (b) Keep stdout banner but gate it behind `cache_control` breakpoint #0 (position < Tier 1). Banner lives in its own cache block with 5m TTL (acceptable — banner changes per branch).
- (c) Delete the banner entirely; surface session-state facts via a new MCP tool `session_context()` the agent calls on demand.

(a) is simplest and zero-cost. Pick (a) unless the banner's human-visibility during `claude --verbose` debugging is load-bearing.

### G5. A3 (revisit 1M context) should be its own flag-flip task, not folded into T10.8

The exploration folds A3 into lifecycle-refactor Stage 10 T10.8 sign-off. But T10.8 is the P1-savings replay — a report-writing task, not a config change. A3 is a settings edit (`CLAUDE_CODE_DISABLE_1M_CONTEXT` flip + `CLAUDE_AUTOCOMPACT_PCT_OVERRIDE` retune) that wants a dedicated backlog row so the before/after evidence and rollback condition are tracked. Keep A3 separate — 30 min task — filed after T10.8 lands evidence about cache-hit rate at current window size.

### G6. F5 (cache-breakpoint plan MCP tool) — check the 4-anchor cap math

Q9 asks whether the 4-anchor-per-request cap accommodates Tier 1 + Tier 2 + spec + last-executor-mutable. Quick math: Anthropic allows [up to 4 `cache_control` breakpoints per request](https://blog.illusioncloud.biz/2026/01/13/prompt-caching-anthropic-cache-breakpoints/). Rev-4 already uses 2 (Tier 1 + Tier 2). The proposed F5 adds 2 more. That hits the cap exactly. No room for error; also no room for a future Tier 5.

**Recommendation.** F5 should only propose ≤ 2 additional anchors. Drop "last executor-mutable block" as a separate anchor — the longest-matching-prefix rule will hit Tier 2 naturally if positioned right. Document in the MCP tool's return value: `anchors: 4` is the hard ceiling; tool returns `{tier_1, tier_2, spec_end, [reserved]}` — reserve slot 4 for future use.

### G7. Theme F3 (harness-level caveman) isn't just "low confidence" — it's research, not design

F3 depends on Claude Code harness features that may not exist (`PreCompletion` hook). The exploration flags it as "harness-gated" and "tracking issue only". Correct — but it's then listed in the priority table with an effort estimate ("harness-gated"). Remove the effort estimate; this is a research item, not a work item. Option: move F3 to a small "Research tracker" appendix at the bottom of the exploration, separate from the Findings table.

### G8. Cross-cutting principle missing — **observability before optimisation**

The four cross-cutting themes are authority chain / cache stability / progressive disclosure / zero-fork hooks. All correct. A fifth is missing and is arguably more fundamental:

**Observability before optimisation.** Every cost claim in the audit is a token estimate from file sizes × 0.25. The rev-4 work runs against a Q9 baseline measured from post-merge Stage replays. The new orchestrator should adopt the same discipline. Concretely:
- Every Theme close writes a `measurement.json` under `ia/state/token-measurements/{stage-id}.json` with `{baseline, post, delta, predicted, delta_vs_predicted_pct}`.
- A CI lint fails any Theme-close PR that lacks a measurement.
- P1-style regression gate: if `delta_vs_predicted_pct < 0.3`, Theme close is rejected; re-investigate the Tier 1 sizing / Tier 2 subset / descriptor residue.

This operationalises G3 and turns Open Q10 into a hard gate.

---

## 3. Answers to open questions

### Q1. Theme 0 vs FREEZE interaction

**Answer.** FREEZE blocks `/master-plan-new`, `/master-plan-extend`, `/stage-decompose`, `/stage-file`. It does **not** block `/project-new`. Therefore:

- Items viable during FREEZE (file via `/project-new`, standalone, no umbrella): **D1, D3, E1, E2, B7**. Five items, ~2–3 h aggregate effort.
- Items that must wait post-M8: anything needing a master plan or a coordinated telemetry harness (B6 stacks on T9.4; C3 skill-seed lint wants the refactored skill set; B9 descriptor lint co-locates with the mcp-lifecycle sweep).

Don't author `tech-quick-wins-master-plan.md` during FREEZE (self-contradictory). Let the standalone items ship naked.

### Q2. Theme B ↔ MCP reshape sibling ordering

Already marked Resolved (2026-04-19). **Reinforce:** per G1, Theme B items (B1, B3–B9 except B2 shipped) go into `mcp-lifecycle-tools-opus-4-7-audit-master-plan.md` as Stages 17–19, not into the new master plan. Sibling ordering problem dissolves because there's no sibling for Theme B — it's all one master plan.

### Q3. Theme F3 feasibility

**Answer.** Park indefinitely. File one research issue (`TECH-???: harness-level output-style enforcement — tracking`). Move F3 out of the master plan entirely. Re-enter the exploration when Claude Code ships `PreCompletion` or equivalent.

### Q4. A1 scope split — CLAUDE.md §3 into AGENTS.md or new file

**Answer.** Neither. Delete CLAUDE.md §3 "Key files" table and fold its load-bearing content into the relevant files themselves. Example:
- `.claude/settings.json` documentation → a top comment in the JSON itself (or a `.claude/settings.README.md` loaded on demand).
- Agent/command/skill inventory → `AGENTS.md §2` already has this; CLAUDE.md §3 can just point to it.
- `BACKLOG.md`/`MEMORY.md` role → glossary rows.

The authority-chain principle (G cross-cutting) says: **one authoritative surface per topic**. CLAUDE.md's §3 is a catalog, not an authority — its content should not exist twice. Replace §3 with an 8-line index pointing at the five or six authoritative files.

### Q5. C1 backward compatibility — human-readable payload in slash commands

**Answer.** Preserve a **3–5-line** `## What this does` block at the top of each `.claude/commands/*.md` written in normal English (caveman exception already applies to human-polling surfaces). Below it, the forward-verbatim block shrinks to: `{ISSUE_ID}`, resolved paths, gate token, no mission statement. Net savings hold (≈ 80% of current restated body is the mission + hard-boundaries block, not the context banner).

### Q6. F1 vs rev-4 Tier 2 bundle

Already marked Superseded. F1's diffability angle → see answer to Q11.

### Q7. B4 `tsx` vs dist

**Answer.** Both, env-gated. `.mcp.json` ships `tsx` as default (iteration-friendly for contributors); `MCP_USE_DIST=1` in `.mcp.json`'s `env` flips to compiled `dist/` for production sessions. Add a SessionStart-hook signal: if `dist/` is older than any source under `src/`, warn on stderr. Don't force the switch; make it discoverable.

### Q8. D4 PreCompact payload

**Answer.** `.claude/last-compact-summary.md` gets:
- `active_issue_id` (from last matched `BACKLOG-`/`FEAT-`/`TECH-` token)
- `active_spec_path` (from most recent `ia/projects/*.md` read)
- `active_master_plan_path` (from most recent `*-master-plan.md` read)
- `last_3_tool_calls` — tool name + top-level args
- `last_5_files_touched` — paths + action (read/edit/write)
- `stage_pointer` — if a `project-stage-close` handoff was emitted recently, record it

Agent-side: on fresh context after compact, open this file first. Format: markdown-with-frontmatter (follow existing IA convention).

### Q9. F5 4-anchor cap math

Answered in G6. Short form: rev-4 uses 2, F5 should propose at most 2 more, one slot reserved.

### Q10. Baseline measurement

Answered in G3. Don't ask — make it Stage 0.

### Q11. F1 diffability angle (bundle history)

**Answer.** Write a Tier 2 bundle `ia/state/token-measurements/bundles/{stage-id}-{timestamp}.md` **only** when a `--capture-bundle` flag is passed to the stage-file / ship-stage dispatch. By default, runtime-only. This preserves rev-4's cache-efficiency story (no filesystem round-trip in the hot path) while keeping the diffability tool available on demand for incident forensics. Gitignore the `bundles/` subdir — too churny for repo history.

---

## 4. Recommended master-plan structure

Given G1 (Theme B → mcp-lifecycle) and G2 (Theme 0 split), the authored outcome is:

### 4.1 No new orchestrator for Theme B

Theme B items file under **`ia/projects/mcp-lifecycle-tools-opus-4-7-audit-master-plan.md`** via `/master-plan-extend` (scope-limited). Three new Stages (17–19 per G1).

### 4.2 One new orchestrator for Themes A/C/D/E/F

**`ia/projects/session-token-latency-master-plan.md`** — 6 Steps roughly as proposed, but:

| Step | Scope | Sibling dep |
|---|---|---|
| **Step 0** (new) — Baseline measurement harness | Stage 0 (G3) | none |
| **Step 1** — Theme A (ambient collapse) | A1, A2, A3, A4 | lifecycle-refactor T10.2 landed |
| **Step 2** — Theme C (dispatch flattening) | C1, C2, C3 | lifecycle-refactor T10.2 + T10.4 landed |
| **Step 3** — Theme D (hook hygiene) | D1, D2, D3, D4 | none |
| **Step 4** — Theme E (repo + memory hygiene) | E1, E2, E3 | none |
| **Step 5** — Theme F (larger bets) | F2, F4, F5, F6 (F1 superseded, F3 parked, F7 tracking) | post-Stage-10 of lifecycle-refactor |

### 4.3 FREEZE-era work (pre-M8)

Nothing in either master plan files during FREEZE. Instead, five standalone `/project-new` issues:
- `TECH-? — gitignore Bury_*.json + mono_crash.*` (E1)
- `TECH-? — fix SessionStart banner memory path` (E2)
- `TECH-? — remove or conditionalise verification-reminder.sh Stop hook` (D1)
- `TECH-? — pure-shell JSON parsing in bash-denylist + cs-edit-reminder hooks` (D3)
- `TECH-? — flip DEBUG_MCP_COMPUTE=1 in .mcp.json env` (B7)

All trivial, all orthogonal to the refactor, all closeable standalone. This is the correct "Theme 0" — zero parent orchestrator.

---

## 5. Answer to the user's direct question

> **"Is the proposed next step the best approach? Will implementation be in a new master-plan or extending the current mcp-lifecycle-tools master-plan?"**

**Short answer.** The proposed next step is **close but not correct as written**. The better sequence:

### Phase 0 — During FREEZE (now through M8 sign-off)

File five standalone issues (D1, D3, E1, E2, B7) via `/project-new`. No master plan involved. ~2–3 h aggregate; clears noise + ≈30–60 ms × N tool-call latency + small token wins. Zero risk to the refactor.

```
claude-personal "/project-new 'gitignore Bury_*.json + mono_crash.* root clutter (audit E1 / m1)'"
claude-personal "/project-new 'fix SessionStart banner memory path drift (audit E2 / m2)'"
claude-personal "/project-new 'conditionalise or delete verification-reminder.sh Stop hook (audit D1 / C5)'"
claude-personal "/project-new 'pure-shell JSON parsing in bash-denylist + cs-edit-reminder hooks (audit D3 / M3)'"
claude-personal "/project-new 'flip DEBUG_MCP_COMPUTE=1 default (audit B7 / m4)'"
```

Ship each via `/ship {id}` as capacity allows during FREEZE — these touch none of the frozen surfaces.

### Phase 1 — Immediately after M8 sign-off

Update the exploration doc to reflect G1–G8 above (15–30 min editorial). Then:

```
claude-personal "/design-explore docs/session-token-latency-audit-exploration.md"
```

Phase 0.5 interview resolves remaining open questions (Q1–Q11) per §3 answers — should be fast; most have proposed resolutions. Phase 1 comparison matrix adopts Approach B (theme-by-theme sibling orchestrators) with the G1 modification (Theme B lives inside mcp-lifecycle).

### Phase 2 — Author two separate authoring passes

**Pass A — Extend mcp-lifecycle for Theme B:**

```
claude-personal "/master-plan-extend ia/projects/mcp-lifecycle-tools-opus-4-7-audit-master-plan.md --against docs/session-token-latency-audit-exploration.md"
```

The `--against` flag tells `master-plan-extend` to read the exploration as source but restrict scope to Theme B (the skill already supports scope narrowing — just state "Theme B only" in the initial prompt). Adds Stages 17–19 to the existing orchestrator. Uses the **existing** backlog numbering, the **existing** yaml machinery, the **existing** composite-bundle surface work. Zero split-brain.

**Pass B — Author new orchestrator for Themes A/C/D/E/F:**

```
claude-personal "/master-plan-new docs/session-token-latency-audit-exploration.md"
```

Seeds `ia/projects/session-token-latency-master-plan.md` with 6 Steps per §4.2. Stage 0 is baseline measurement (G3). Steps 1/2 carry explicit `depends_on:` pointers to lifecycle-refactor T10.2 + T10.4 so `/stage-file` can read the sibling plan state before green-lighting.

### Phase 3 — Execute

Standard lifecycle per Step:

```
claude-personal "/stage-file ia/projects/session-token-latency-master-plan.md 'Stage 0.1'"
claude-personal "/ship-stage ia/projects/session-token-latency-master-plan.md 'Stage 0.1'"
```

...and so on per the sequencing table in §4.2. Theme A and Theme C block until lifecycle-refactor Stage 10 T10.2/T10.4 flip to Done. The `ship-stage` subagent already enforces Stage-order gating; add the cross-plan dependency to each task's yaml `depends_on:` for double-safety.

### So: new master plan OR extend?

**Both.** One extension (mcp-lifecycle for Theme B), one new orchestrator (session-token-latency for A/C/D/E/F). Not an either/or. This respects ownership boundaries (Theme B = MCP surface = existing owner), keeps surface-area growth bounded (only 3 new Stages in mcp-lifecycle vs the exploration's original 6-Step greenfield plan), and lets the two plans proceed on independent cadences.

The currently-proposed "single session-token-latency-master-plan.md covering everything" is the single biggest design gap and will create merge conflicts with mcp-lifecycle Stages 11–16 when mutation/authorship work lands.

---

## 6. Best possible execution order (concrete commands, sibling-plan coordinated)

Assuming today = 2026-04-19 and lifecycle-refactor is In Progress Stage 5 of 9 (estimated M8 ETA ≈ 2–3 weeks):

### Week 1 (FREEZE active)

**Day 1** — Update exploration doc per G1–G8 (1 h edit). File 5 standalone issues per Phase 0:

```
/project-new 'gitignore Bury_*.json + mono_crash.* (audit E1)'
/project-new 'fix SessionStart banner memory path drift (audit E2)'
/project-new 'conditionalise verification-reminder.sh Stop hook (audit D1)'
/project-new 'pure-shell JSON in bash-denylist + cs-edit-reminder (audit D3)'
/project-new 'DEBUG_MCP_COMPUTE=1 default (audit B7)'
```

**Days 2–5** — Ship each via `/ship {id}` as capacity allows. All five are self-contained; run in parallel if desired. Record before/after measurements using a draft `measure-session-tokens.ts` — even a hand-rolled count is fine at this stage.

### Weeks 2–3 (FREEZE continues; lifecycle-refactor finishing)

No master-plan authoring. Monitor lifecycle-refactor progress via `/dashboard`. When M8 gate is 48 h away, draft the authoring prompts (so the `/design-explore` run happens immediately post-M8 without warm-up cost).

### Week 4 (post-M8 sign-off)

**Monday** — `/design-explore docs/session-token-latency-audit-exploration.md`. Phase 0.5 interview resolves Q1–Q11 per §3.

**Tuesday** — Author `tools/scripts/measure-session-tokens.ts` prototype + capture pre-refactor baseline. (Does NOT require a master plan yet; land as a direct commit under a standalone `TECH-?` issue.)

**Wednesday** — `/master-plan-extend ia/projects/mcp-lifecycle-tools-opus-4-7-audit-master-plan.md --against docs/session-token-latency-audit-exploration.md` (scope: Theme B only). Adds Stages 17–19.

**Thursday** — `/master-plan-new docs/session-token-latency-audit-exploration.md` (scope: Themes A/C/D/E/F). Authors `ia/projects/session-token-latency-master-plan.md` with 6 Steps per §4.2.

**Friday** — `/stage-file ia/projects/session-token-latency-master-plan.md 'Stage 0.1'`. Materialise baseline-measurement Task rows. Ship Stage 0 first — gates everything downstream.

### Weeks 5–8 (execution)

Follow the §4.2 sequencing. Key cross-plan gates:

| Blocks on | When unblocks |
|---|---|
| Theme A Stage | lifecycle-refactor Stage 10 T10.2 Done |
| Theme C Stage | lifecycle-refactor Stage 10 T10.2 + T10.4 Done |
| Theme B (mcp-lifecycle Stage 17) | mcp-lifecycle Stage 9 T9.4 Done (B6 doc lint) |
| Theme B Stage 18 | no cross-plan block |
| Theme F Stage | lifecycle-refactor Stage 10 T10.8 Done (Tier 1 + Tier 2 live with measured data) |

Every Theme close writes `measurement.json` (G3 / G8). No Theme closes without delta-vs-predicted ≥ 30%.

### Total wall-clock estimate

- FREEZE window: 5 standalone issues (2–3 h each, parallel) = ~1 week calendar.
- Post-M8 authoring: 1 week (design-explore + 2 master-plan authoring passes + baseline harness).
- Theme execution: ~4–6 weeks (themes parallel where dependencies allow).
- Theme F deferral: indefinite, re-audited post-Tier 1/Tier 2 measurement data.

**Net.** ~7–8 weeks from now to full audit resolution, with 30% of the token-savings already banked in Week 1 via the FREEZE-safe standalone issues.

---

## 7. Summary of proposed edits to the exploration doc

Before running `/design-explore`:

1. **§Sibling master plan coordination** — add a G1 row documenting "Theme B items fold into mcp-lifecycle via `/master-plan-extend`, not into this orchestrator."
2. **§Approaches** — amend Approach B to split Theme B out explicitly as an mcp-lifecycle extension.
3. **§Recommendation** — restate as "Two-pass authoring: extend mcp-lifecycle for Theme B + new orchestrator for A/C/D/E/F."
4. **§Open questions** — mark Q1 answered (FREEZE allows `/project-new`, 5 standalone issues named); Q3 parked; Q10 becomes Stage 0 (delete from questions). Re-word remainder per §3 answers.
5. **§Proposed next step** — replace the single `/master-plan-new` with the two-pass sequence (§5 of this review).
6. **New §Cross-cutting principle (Observability before optimisation)** — per G8.
7. **Theme 0 section** — rename to **Phase 0 — FREEZE-era standalone issues**; list the five `/project-new` commands verbatim; make clear these have no parent orchestrator (G2).

These edits don't change any Finding; they clarify the authoring path.

---

## 8. Sources

- [Anthropic Prompt Caching — 4 breakpoint limit + longest-matching-prefix (IllusionCloud)](https://blog.illusioncloud.biz/2026/01/13/prompt-caching-anthropic-cache-breakpoints/)
- [How Claude Code works — official docs](https://code.claude.com/docs/en/how-claude-code-works)
- [Claude Code multi-agent orchestration patterns (Shipyard 2026)](https://shipyard.build/blog/claude-code-multi-agent/)
- [5 Claude Code Agentic Workflow Patterns (MindStudio)](https://www.mindstudio.ai/blog/claude-code-agentic-workflow-patterns)
- In-repo: `ia/projects/lifecycle-refactor-master-plan.md` §Stage 10, `ia/projects/mcp-lifecycle-tools-opus-4-7-audit-master-plan.md` §Stages 6–16, `docs/prompt-caching-mechanics.md`, `docs/session-token-latency-audit-exploration.md`.

---

*End of design review.*
