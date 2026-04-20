---
purpose: "TECH-523 — Re-orientation integration test + validate:all."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/session-token-latency-master-plan.md"
task_key: "T5.1.4"
---
# TECH-523 — Re-orientation integration test + validate:all

> **Issue:** [TECH-523](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-20
> **Last updated:** 2026-04-20

## 1. Summary

Final gating task for Stage 5.1. Validates end-to-end re-orientation UX via manual
test protocol. Evidence-linked Verification block. Docs extended. `validate:all` green
unblocks Stage 5.1 closeout.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Manual integration test executed per extensions §5 protocol; evidence captured.
2. Pack file content verified: Active focus, Relevant surfaces (all 4 files), ≥1 Recent decision, Last tool outputs (4 actions).
3. Resumed session's SessionStart preamble includes pack content.
4. Agent cites active task + stage + ≥2 relevant surfaces with zero pre-answer Reads.
5. `docs/agent-led-verification-policy.md` §Session continuity has ≥3-line re-injection paragraph.
6. `npm run validate:all` green.

### 2.2 Non-Goals (Out of Scope)

1. Digest script authoring (handled in TECH-520 + TECH-521).
2. Re-injection wiring (handled in TECH-522).
3. Automated CI harness for integration test (manual protocol per extensions doc §5).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | After /compact + resume, agent orients without pre-answer Read calls | Agent response cites active task + stage + ≥2 surfaces; zero Read tool calls observed before first answer |
| 2 | Developer | Integration test evidence is linked in Verification block | Screenshot + tool-call log URLs present in task Verification block |

## 4. Current State

### 4.1 Domain behavior

No end-to-end test protocol has been run for the context-pack pipeline. TECH-520–522 deliver the components; this task validates the assembled system.

### 4.2 Systems map

Touches: `docs/agent-led-verification-policy.md` §Session continuity.
Reads: `.claude/context-pack.md`, session preamble stdout.
No Unity / C# / runtime surface touched.

## 5. Proposed Design

### 5.1 Target behavior (product)

Test protocol (extensions doc §5 T3.3.4 §Examples):
1. Start session on filed task.
2. Perform 2 Read + 2 Edit on 4 distinct source files.
3. Run `/compact`.
4. Inspect `.claude/context-pack.md` — verify Active focus, Relevant surfaces (all 4 files), ≥1 Recent decision, Last tool outputs (4 actions).
5. Resume session (new terminal).
6. Verify SessionStart preamble includes pack content.
7. Ask agent "what are you working on?"; confirm cites active task + stage + ≥2 relevant surfaces with zero Read calls before first answer.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

Phase 2 — Integration test + docs.
1. Run test protocol (steps above).
2. Capture screenshot + tool-call log; attach to Verification block.
3. Extend `§Session continuity` sub-section with ≥3-line re-injection contract paragraph.
4. `npm run validate:all` green.
5. Stage 5.1 closeout unblocked.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-20 | Manual protocol (not automated CI) | Re-orientation test requires live session + human judgment on agent response quality | Automated headless — can't verify agent reasoning quality |

## 7. Implementation Plan

### Phase 2 — Integration test + docs

- [ ] Run test protocol: 2 Read + 2 Edit → `/compact` → inspect pack → resume → query agent.
- [ ] Capture screenshot + tool-call log; attach to Verification block.
- [ ] Extend `docs/agent-led-verification-policy.md` §Session continuity with ≥3-line re-injection contract paragraph.
- [ ] `npm run validate:all` green.
- [ ] Stage 5.1 closeout unblocked.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| IA tooling changes (docs update) | Node | `npm run validate:all` | Chains validate:dead-project-specs, test:ia, validate:fixtures |
| Re-orientation UX | Manual agent session | Screenshot + tool-call log linked in Verification block | Zero pre-answer Read calls = pass condition |

## 8. Acceptance Criteria

- [ ] Manual integration test executed per extensions §5 protocol; evidence captured.
- [ ] Pack file content verified: Active focus, Relevant surfaces (all 4 files), ≥1 Recent decision, Last tool outputs (4 actions).
- [ ] Resumed session's SessionStart preamble includes pack content.
- [ ] Agent cites active task + stage + ≥2 relevant surfaces with zero pre-answer Reads.
- [ ] `docs/agent-led-verification-policy.md` §Session continuity has ≥3-line re-injection paragraph.
- [ ] `npm run validate:all` green.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | … | … | … |

## 10. Lessons Learned

- …

## §Plan Author

### §Audit Notes

- Risk: manual protocol reproducibility — results vary by tester attention, session length, hook timing. Mitigation: extensions doc §5 T3.3.4 protocol is exact step list; evidence capture (screenshot + tool-call log) enforced via Verification block gate.
- Risk: gating task runs before TECH-520/521/522 are Done → premature test against incomplete pipeline. Mitigation: dispatch order enforced via `/ship-stage` Pass 1 sequential; TECH-523 filed last in Stage Tasks table.
- Risk: "zero Read calls before first answer" gate too strict — agent may legitimately need to verify. Resolution: clarify in protocol — zero Read on source files listed in Active focus / Relevant surfaces; MCP / glossary lookups are fine (pack cites them as loaded context sources).
- Risk: evidence (screenshot / tool-call log) lost across session compaction. Mitigation: capture artifacts into repo-tracked location (e.g. commit attachment or reference a fixed URL), link in Verification block at capture time, not retroactively.
- Ambiguity: acceptance says "≥2 relevant surfaces" — what counts. Resolution: surface = file path OR `ia/projects/{id}.md` spec reference; MCP tool output does not count.

### §Examples

| Input | Expected output | Notes |
|-------|-----------------|-------|
| Test run on TECH-520 as active task, 2 Read + 2 Edit | Pack shows TECH-520 in Active focus, 4 files in Relevant surfaces | Happy-path setup |
| Post-compact pack inspection | Active focus + Relevant surfaces + ≥1 Recent decision + Last tool outputs (4 rows) all present | Pack content gate |
| Resume in new terminal | SessionStart preamble stdout includes pack content after `---` | Re-injection wire |
| Agent queried "what are you working on?" | Response cites TECH-520 + Stage 5.1 + ≥2 of the 4 surfaces; tool-call log shows zero Read before first text reply | UX gate |
| Agent response uses MCP `spec_section` lookup pre-answer | Still counts as PASS — MCP lookups orthogonal to Read gate | Gate clarification |

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| session_on_filed_task | start Claude on TECH-520 | runtime-state active_task_id = TECH-520 | manual |
| tool_interaction_bootstrap | 2 Read + 2 Edit on 4 distinct files | telemetry jsonl has 4 rows; tool-usage (if wired) has entries | manual |
| precompact_pack_write | `/compact` trigger | `.claude/context-pack.md` exists post-compact | manual |
| pack_content_shape | inspect pack | Active focus populated; Relevant surfaces lists 4 files; ≥1 Recent decision; Last tool outputs = 4 rows | manual |
| resume_preamble_inject | new terminal session | SessionStart stdout includes pack content | manual |
| agent_orientation_no_reads | ask "what are you working on?" | response cites task + stage + ≥2 surfaces; zero Read before first reply | manual + tool-call log |
| evidence_attached | Verification block | screenshot URL + tool-call log URL present | grep |
| docs_re_injection_paragraph | `docs/agent-led-verification-policy.md` §Session continuity | paragraph ≥3 lines on re-injection contract | grep |
| validate_all | post-implementation | `npm run validate:all` green | node |

### §Acceptance

- [ ] Manual integration test executed per extensions doc §5 T3.3.4 protocol; evidence captured (screenshot + tool-call log).
- [ ] Pack content verified: Active focus + Relevant surfaces (all 4 files) + ≥1 Recent decision + Last tool outputs (4 rows).
- [ ] Resumed session SessionStart preamble includes pack content.
- [ ] Agent cites active task + Stage 5.1 + ≥2 relevant surfaces with zero pre-answer Reads on source files (MCP/glossary lookups exempt).
- [ ] Evidence URLs linked in Verification block at capture time.
- [ ] `docs/agent-led-verification-policy.md` §Session continuity extended with re-injection contract ≥3 lines.
- [ ] `npm run validate:all` green.

### §Findings

_none — gating integration test; depends on TECH-520/521/522 merged Done first (enforced by `/ship-stage` Pass 1 sequential dispatch)._


## Open Questions (resolve before / during implementation)

None — tooling only; see §8 Acceptance criteria.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes. One paragraph: what shipped, what worked, what to watch._

## §Code Review

_pending — populated by `/code-review`. Verdict: PASS | minor (fix-in-place / deferred) | critical (writes `§Code Fix Plan` below)._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed. Sonnet `code-fix-apply` reads tuples + applies + re-enters `/verify-loop`._
