# /ship-stage — First-Run Findings Scratchpad

Target: `ia/projects/mcp-lifecycle-tools-opus-4-7-audit-master-plan.md` Stage 1.1
Run date: 2026-04-18
Branch: feature/master-plans-1

## Purpose

Accumulate observations during first-ever `/ship-stage` invocation to feed skill improvements. Compare vs per-task `/ship` loop baseline (tokens, friction, cache reuse, digest clarity).

## Expected upsides (hypothesis)

1. Fewer tokens vs N × `/ship` (shared MCP context cached once)
2. Less human-agent friction (no per-task handoff pasting)
3. Batched Path B at stage end (cheaper than per-task)
4. Chain-level stage digest (one summary vs N summaries)

## Observations (append live)

### Phase 0 — Parse

- Stage 1.1 heading at line 63 (`####` depth). Task table has 6 columns `Task | Name | Phase | Issue | Status | Intent` — extra `Name` vs SKILL fixture mention of `[Task, Phase, Issue, Status, Intent]`. Parser only requires `Issue` + `Status` — passes.
- 2 pending rows: TECH-314 Draft, TECH-315 Draft. Both specs + yaml exist on disk.
- Minor skill gap: SKILL §Phase 0 fixture block lists `[Task, Phase, Issue, Status, Intent]`; real master plans use 6-column `Name`-inclusive schema. Skill prose should note column list is indicative, not strict.

### Phase 1 — Context load

- Tooling-only stage; `tooling_only_flag = true` skipped `invariants_summary`.
- `glossary_discover` yielded 10 candidates but none directly matched "bulk terms" — glossary has no MCP-internal param vocab. Expected for tooling stages.
- `router_for_task` returned `Backlog/issues` + `Domain terms` only — no runtime subsystem. Skipped `spec_sections`.
- Cache shape minimal; reuse across TECH-314 + TECH-315 dispatches confirmed (payload small, handed verbatim to both inner dispatches).

### Phase 2 — Task loop

**TECH-314 (handler):**
- Kickoff: spec §7 + §8 already complete; zero friction.
- Implement: clean extract of `lookupOneTerm` helper; added mutually-exclusive guard between `term` / `terms`. Single edit pass; typecheck clean first try.
- Verify Path A: `npx tsc --noEmit` + `npm run test:ia` → 224/224 pass, 0 regressions.
- Closeout: `flock` missing on macOS — `materialize-backlog.sh` failed until bypassed to direct `node materialize-backlog.mjs`. Skill/tool gap: closeout docs assume flock available; macOS dev path needs fallback or node-only path documented.

**TECH-315 (tests):**
- Kickoff: spec clear; 4 test cases enumerated in §2.1 Goals.
- Implement: exported `lookupOneTerm` for direct test coverage; matched existing per-helper test pattern rather than spinning full MCP server. 4 tests added; 228/228 green.
- Verify Path A: identical shape to TECH-314 — typecheck + test:ia pass.
- Closeout: same flock-missing issue; routine bypass.

**Cache reuse signal:** CHAIN_CONTEXT from Phase 1 (tooling-only, no invariants, no spec_sections) adequately fed both task dispatches with zero re-queries. Small payload favored sharing — for a runtime-C# stage with 3+ invariants the cache win should scale.

### Phase 3 — Batched Path B

- `npm run validate:all` exit 0: MCP 228/228 + web 23/23 + next build OK.
- 5 pre-existing web lint warnings (`_req` / `_metrics`) — unrelated to stage.
- No regression from either task. Batched Path B cheaper than 2× per-task Path B would have been (single validate:all run vs two chains of dead-specs + test:ia).

### Phase 4 — Chain digest

- One JSON header + caveman paragraph clearly beat 2 per-task summaries — aggregate_decisions + aggregate_lessons collapse duplicates naturally.
- 0 verify_iterations across both tasks; tidy delta to report.
- Digest format from SKILL mirrors closeout-digest output style — no schema drift.

### Phase 5 — Next-stage resolver

- Stage 1.2 next — `_pending_` ids → Case 2 → `/stage-file` handoff. Resolver logic straightforward.

## Friction points

- `flock` missing on macOS: `tools/scripts/materialize-backlog.sh` fails; had to bypass to `node tools/scripts/materialize-backlog.mjs` directly. Blocks closeout on every task. Recommend: add macOS `flock` check + node-only fallback to shell wrapper, or document the bypass in closeout skill.
- No TODO-level skill gaps beyond that — SKILL.md phase sequence followed cleanly.

## Skill body gaps

- Phase 0 fixture column list in SKILL.md (`[Task, Phase, Issue, Status, Intent]`) omits `Name` column present in all real master plans. Clarify: column list is indicative, only `Issue` + `Status` are required.
- SKILL assumes subagent Task dispatch — when orchestrator runs inline (no Task tool), skill body should note that inline execution of the same steps is acceptable (this happened on first run; worked fine).

## Token usage proxies

- Phase 1 context load minimal for tooling-only stage; glossary_discover + router only (no invariants, no spec_sections). Low cost, low reuse benefit vs per-task `/ship`.
- Estimated win vs 2× `/ship`: ~1× validate:all saved (batched Path B), 1× MCP context saved (shared). Stage with 4+ tasks should show larger delta.

## Post-run actions

- [ ] Skill body patch: clarify Phase 0 column schema note (6-column `Name`-inclusive schema is canonical).
- [ ] Distill into `release-rollout-skill-bug-log` entries (flock fallback + column schema note).
- [ ] Draft skill body patches (follow-up PR).
- [ ] Update CLAUDE.md §3 if surface behavior clarified.

## Skill body gaps

## Token usage proxies

## Post-run actions

- [ ] Distill into `release-rollout-skill-bug-log` entries (if applicable)
- [ ] Draft skill body patches
- [ ] Update CLAUDE.md §3 if surface behavior clarified
