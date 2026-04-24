### Stage 10 — Prompt-Caching Optimization Layer (Post-Merge, Q9-Gated)

**Status:** In Progress

**Objectives:** Land the rev 4 prompt-caching optimization layer on top of the merged refactor. Add Tier 1 stable cross-Stage cache block + Tier 2 per-Stage ephemeral bundle. Enforce F2 sizing gate at CI time (R2). Stagger bulk Opus fan-out per F3. Retire R11 §Findings gate (B2) + unify pair-tail Sonnet appliers (B4). Wire SSE cache-commit event gate (R1). Land invalidation-cascade + 20-block guardrail notes (D2/D3). Validate P1 savings band (−30% to −57% per Stage at ≥3 pair-head reads) against post-landing measurement (T10.8) — gate relaxed to post-hoc verification (precondition waived).

**Extension source:** `docs/lifecycle-opus-planner-sonnet-executor-exploration.md` §Design Expansion — rev 4 candidates + cache-mechanics amendments (2026-04-19 rev 4). Reference doc: `docs/prompt-caching-mechanics.md`.

**Precondition gate:** WAIVED 2026-04-19 by user ("I wish to ship the stage without the precondition gate. I trust the optimization proposal."). Stage 10 T10.1 opens unconditionally. P1 savings band validation moves from pre-gate to post-hoc T10.8 measurement — if actual delta > ±5% of R5 predicted band at measured read count, investigate + patch before M9 sign-off (but Stage does not reject). Original gate text (superseded): _Stage 9 T9.4 Q9 baseline data must record pair-head read count per Stage for ≥ 3 distinct post-merge Stages (any open master plan) before Stage 10 T10.1 may open. If measured read count < 3/Stage on all sampled Stages → Stage 10 REJECTED (P1 economics not viable); record rejection in migration JSON M9 stub + close Stage 10 as `Status: Rejected (Q9 baseline < 3 reads/Stage)`._

**Exit:**

- `docs/prompt-caching-mechanics.md` — reference doc landed (authored ahead of Stage 10 activation per D1 tier).
- Tier 1 stable cross-Stage cache block implemented: `@`-concatenated rules preamble emitted as single `messages` content block with `cache_control: {"type":"ephemeral","ttl":"1h"}`; inherited by all 4 pair seams + `plan-author` + `opus-audit` within one Stage (per rev 4 A1 + A4).
- Tier 2 per-Stage ephemeral bundle implemented: `ia/skills/domain-context-load/SKILL.md` Phase N concatenates MCP aggregator output (glossary subset + spec_sections + invariants_summary) into a single content block with `cache_control: {"type":"ephemeral","ttl":"1h"}` (per rev 4 A3 + R3 + R4).
- F2 sizing gate CI check landed: `tools/scripts/validate-cache-block-sizing.ts` asserts each emitted cache block ≥ F2 floor (4,096 tok Opus 4.7 / 1,024 tok Sonnet 4.6); CI fails on silent no-cache (per rev 4 C1 + R2).
- F3 bulk-dispatcher stagger fix: `plan-author` + `opus-audit` Stage-scoped bulk invocations staggered sequentially (no concurrent identical-prompt fan-out); documented in skill Phase 0 guardrail (per rev 4 A2 + amendment 2).
- R11 §Findings gate retired (rev 4 B2): `plan-author` Phase N writes §Findings inline per Task; `opus-audit` Phase 0 drops the "assert every Task has non-empty §Findings" gate and reads §Findings from plan-author output directly. Commit ordering: B2 lands BEFORE any opus-audit refactor (amendment 3).
- Unified pair-applier (rev 4 B4): `ia/skills/plan-fix-apply/`, `ia/skills/code-fix-apply/`, `ia/skills/stage-closeout-apply/` consolidated into single `ia/skills/plan-applier/SKILL.md` Sonnet skill reading any `§*Fix Plan` / `§Stage Closeout Plan` tuple shape; per-pair applier skills retired with tombstones. Resolves legacy Open Q11.
- SSE cache-commit event gate (rev 4 C3/R1): subagent progress-emit reads `message_start.usage.cache_creation_input_tokens` as commit signal; `content_block_delta` safe fallback; no ms-latency heuristic. Filed Q17 upstream with Anthropic.
- F5 tool-allowlist uniformity (amendment 4): all pair-seam agents (`.claude/agents/plan-reviewer.md`, `plan-applier.md`, `stage-file-planner.md`, `stage-file-applier.md`, `opus-code-reviewer.md`, `stage-closeout-planner.md`) share identical `tools:` frontmatter; validator asserts uniformity.
- F6 invalidation cascade + 20-block guardrail notes landed: `docs/mcp-ia-server.md` §Cache impact note added (D2); skill-author guide `ia/rules/subagent-progress-emit.md` adds D3 single-block rule ("NEVER emit multi-block stable prefix").
- P1 savings band validated: Q9 baseline replay under Tier 1 + Tier 2 cache enabled shows actual savings within ±5% of R5 predicted band at measured read count; if delta > ±5% → investigate + patch before sign-off.
- `npm run validate:all` + `npm run verify:local` green post-Stage 10.
- Migration JSON M9 entry written (new phase; not in M0–M8 core refactor) with Stage 10 sign-off timestamp.
- Phase 1 — Reference doc landed + Q9 gate waiver recorded (precondition WAIVED 2026-04-19).
- Phase 2 — Tier 1 stable block + F2 sizing gate CI.
- Phase 3 — Tier 2 per-Stage bundle + domain-context-load Phase N concat.
- Phase 4 — F3 stagger + F5 uniformity + B2 retire R11.
- Phase 5 — B4 unified plan-applier consolidation.
- Phase 6 — R1 SSE commit gate + C4 progress-emit extension.
- Phase 7 — D2/D3 docs + 20-block guardrail.
- Phase 8 — P1 validation replay + sign-off + M9 flip.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T10.1 | D1 reference doc verify (Q9 gate WAIVED) | **TECH-502** | Done (archived) | Verify `docs/prompt-caching-mechanics.md` present (authored 2026-04-19 ahead of Stage 10 activation); record "Precondition gate waived by user 2026-04-19" in migration JSON M9.waiver field with timestamp + user rationale quote; open Stage 10 Phase 2 gate unconditionally. Q9 baseline read-count measurement (if available from T9.4) recorded as informational context only — not a gate. No rejection branch. |
| T10.2 | Tier 1 stable cross-Stage block + F2 sizing gate CI | **TECH-503** | Done (archived) | Implement Tier 1 stable block: author `ia/skills/_preamble/stable-block.md` (rules `@`-concat target — invariants + terminology-consistency + mcp-ia-default + agent-output-caveman + agent-lifecycle + project-hierarchy + orchestrator-vs-spec + glossary preamble); emit as single `messages` content block with `cache_control: {"type":"ephemeral","ttl":"1h"}` in each pair-seam agent body + `plan-author` + `opus-audit` agent bodies (A1+A4). Author `tools/scripts/validate-cache-block-sizing.ts` CI validator: parses agent bodies for `cache_control` declarations; estimates block token count (bytes × 0.25); fails if count < F2 floor (4,096 Opus 4.7 / 1,024 Sonnet 4.6); wired into `npm run validate:all` chain via `package.json`. Document in `docs/prompt-caching-mechanics.md` §5. |
| T10.3 | Tier 2 per-Stage bundle + domain-context-load Phase N concat | **TECH-504** | Done (archived) | Edit `ia/skills/domain-context-load/SKILL.md`: add Phase N (final concatenation phase) that assembles glossary subset + spec_sections + invariants_summary from MCP aggregator output into single content block; emit with `cache_control: {"type":"ephemeral","ttl":"1h"}`; Phase N asserts token estimate ≥ F2 floor before emit (runtime safety net complementing T10.2 CI gate); document in skill §Overview. Update `stage-file-plan` skill to invoke `domain-context-load` exactly once per Stage (shared Tier 2 bundle reused across all Tasks). Update `ia/rules/plan-apply-pair-contract.md` to cite Tier 2 bundle reuse contract. Run `validate:all`; verify validator accepts new Phase N. |
| T10.4 | F3 stagger + F5 tool-allowlist uniformity + B2 retire R11 | **TECH-505** | Done (archived) | Edit `ia/skills/plan-author/SKILL.md` Phase 0 guardrail: sequential dispatch (no concurrent Opus calls for Stage-scoped bulk N→1 invocation; F3 guardrail per rev 4 A2 + amendment 2). Same edit in `ia/skills/opus-audit/SKILL.md` Phase 0. Audit all pair-seam agent bodies (`.claude/agents/plan-reviewer.md`, `plan-applier.md` [or legacy per-pair appliers pending T10.5], `stage-file-planner.md`, `stage-file-applier.md`, `opus-code-reviewer.md`, `stage-closeout-planner.md`): enforce identical `tools:` frontmatter (F5 uniformity per amendment 4); author `tools/scripts/validate-agent-tools-uniformity.ts` validator wired to `validate:all`. **B2 retire R11:** edit `ia/skills/plan-author/SKILL.md` Phase N output contract to include per-Task `§Findings` sub-section alongside `§Plan Author` 4-part output; edit `ia/skills/opus-audit/SKILL.md` Phase 0 to drop the "assert every Task has non-empty §Findings" gate (rev 3 R11) and read §Findings from plan-author output directly. **Commit ordering:** B2 plan-author edit lands in the same commit as opus-audit Phase 0 drop (never partial — prevents mid-flight ordering breakage per amendment 3). |
| T10.5 | B4 unified plan-applier consolidation | **TECH-506** | Draft | Author `ia/skills/plan-applier/SKILL.md`: Sonnet literal-applier reading any `§*Fix Plan` or `§Stage Closeout Plan` tuple shape (`{operation, target_path, target_anchor, payload}`); dispatches per operation type (fs edit, glossary row, BACKLOG archive, id purge, spec delete, status flip, digest emit); escalates to Opus on anchor ambiguity; bounded 1 retry on transient; resolves legacy Open Q11. Retire `ia/skills/plan-fix-apply/` + `ia/skills/code-fix-apply/` + `ia/skills/stage-closeout-apply/` → move each to `ia/skills/_retired/{name}/` with tombstone redirect header ("Retired — use `plan-applier` (unified)"). Retire corresponding agents `.claude/agents/plan-fix-applier.md` + `code-fix-applier.md` + `stage-closeout-applier.md` → move to `.claude/agents/_retired/`. Author `.claude/agents/plan-applier.md` (Sonnet; caveman preamble; tools uniformity per T10.4). Update all pair-head skills + commands (`/plan-review`, `/code-review`, `/closeout`) to dispatch `plan-applier` instead of legacy per-pair applier. Update `ia/rules/plan-apply-pair-contract.md` to reflect unified applier. |
| T10.6 | R1 SSE cache-commit event gate + C4 progress-emit extension | **TECH-507** | Draft | Edit `ia/skills/subagent-progress-emit/SKILL.md`: add §SSE cache-commit gate section documenting `message_start.usage.cache_creation_input_tokens` as conservative commit signal + `content_block_delta` safe fallback (R1); forbid ms-latency heuristics in skill bodies. Extend `⟦PROGRESS⟧` marker shape with optional `cache:{written|hit|miss|n/a} tokens:{N}` suffix when `usage` data available (rev 4 C4 fold — zero regression at default). Document Q17 upstream-pending note in skill §Caveats. Update all 15 lifecycle skills' `phases:` frontmatter to optionally consume SSE usage data (backwards compatible — no change required for skills that don't surface cache telemetry). |
| T10.7 | D2 cascade note + D3 20-block guardrail note | **TECH-508** | Draft | Edit `docs/mcp-ia-server.md`: add §Cache invalidation impact section — any `tools/mcp-ia-server/` edit cascades down to cached Stage bundles per F5; PR author must flag tool-registration edits in PR description + expect Stage-boundary re-warm. Edit `ia/skills/subagent-progress-emit/SKILL.md` (or new `ia/rules/subagent-caching-guardrails.md` — author's call): add D3 single-block rule — NEVER emit multi-block stable prefix; `@`-concatenation at skill-preamble author time is the ONLY supported assembly mode; multi-`@`-load with separate `cache_control` per block is forbidden (risks falling outside F6 20-block lookback as conversation grows). Cross-link both notes from `docs/prompt-caching-mechanics.md` §6 + §7. |
| T10.8 | P1 validation replay + sign-off + M9 flip | **TECH-509** | Draft | Select ≥ 3 post-merge Stages measured in Stage 9 T9.4 Q9 baseline; replay each under Tier 1 + Tier 2 cache enabled; capture actual cache-hit-rate + write-count + read-count + token-delta per Stage; compute actual savings % vs R5 predicted band (−10% at 2 reads / +23% at 3 reads / +50% at 5 reads / +57% at 6 reads); assert actual within ±5% of predicted band at measured read count; if delta > ±5% → investigate + patch (likely F2 sizing gate regression or F5 cascade not honored) + re-replay. Present validation report to user; wait for explicit sign-off ("LGTM" / "ship cache layer"); flip migration JSON M9 `done` + stamp sign-off timestamp. Run final `npm run validate:all` + `npm run verify:local` on main post-Stage-10 to confirm clean state. |

#### §Decision Log

- **2026-04-19 — Cardinality gate waiver (1 task per phase).** Stage 10 waives the ≥2-tasks/phase rule; phase boundaries = rev-4 exit bullets; each task atomic per commit-ordering (T10.4 B2) + retirement semantics (T10.5 B4) + CI gate (T10.2 F2). User confirmed via stage-file-plan cardinality gate prompt.

#### §Stage File Plan

Tuple list (one per Task). Sonnet pair-tail (`stage-file-apply`) reads verbatim and materializes `ia/backlog/{id}.yaml` + `ia/projects/{id}.md` stubs + flips task-row `Issue` / `Status` columns.

- **T10.1 → TECH-502**
  - operation: `file_task`
  - title: `D1 prompt-caching reference doc verify + Q9 precondition waiver record`
  - priority: `medium`
  - issue_type: `TECH`
  - depends_on: [`TECH-492`]
  - related: []
  - parent_plan: `ia/projects/lifecycle-refactor-master-plan.md`
  - task_key: `T10.1`
  - stage: `10`
  - phase: `1`
  - notes: Verify `docs/prompt-caching-mechanics.md` present (authored 2026-04-19 ahead of Stage 10 activation per D1 tier). Record waiver in migration JSON M9.waiver field: `{timestamp: "2026-04-19", quote: "I wish to ship the stage without the precondition gate. I trust the optimization proposal.", effect: "Stage 10 T10.1 opens unconditionally; P1 savings validation deferred to post-hoc T10.8."}`. Open Stage 10 Phase 2 gate unconditionally. Q9 baseline read-count (if TECH-492 telemetry available) recorded as informational only — never gating. No rejection branch.
  - acceptance:
    - `docs/prompt-caching-mechanics.md` present at repo root path (read-only check).
    - Migration JSON M9.waiver field written with timestamp + user quote verbatim + effect note.
    - Stage 10 T10.2 row `Status` column flipped to `_pending_` → Ready (next-task unblocked).
  - stub_body_sections: §1 Intent, §2.1 Scope, §4.2 Plan, §7 Acceptance, §Open Questions.

- **T10.2 → TECH-503**
  - operation: `file_task`
  - title: `Tier 1 stable cross-Stage cache block + F2 sizing gate CI`
  - priority: `high`
  - issue_type: `TECH`
  - depends_on: [`TECH-502`]
  - related: []
  - parent_plan: `ia/projects/lifecycle-refactor-master-plan.md`
  - task_key: `T10.2`
  - stage: `10`
  - phase: `2`
  - notes: Author `ia/skills/_preamble/stable-block.md` (rules `@`-concat target: invariants + terminology-consistency + mcp-ia-default + agent-output-caveman + agent-lifecycle + project-hierarchy + orchestrator-vs-spec + glossary preamble). Emit as single `messages` content block with `cache_control: {"type":"ephemeral","ttl":"1h"}` in each pair-seam agent body + `plan-author` + `opus-audit` agent bodies (rev 4 A1 + A4). Author `tools/scripts/validate-cache-block-sizing.ts` — parses agent bodies for `cache_control` declarations; estimates block token count (bytes × 0.25); fails if count < F2 floor (4,096 tok Opus 4.7 / 1,024 tok Sonnet 4.6); wired into `npm run validate:all`. Document in `docs/prompt-caching-mechanics.md` §5.
  - acceptance:
    - `ia/skills/_preamble/stable-block.md` present with `@`-concat target list.
    - Each of 8 target agent bodies (`.claude/agents/{plan-reviewer,plan-applier,stage-file-planner,stage-file-applier,opus-code-reviewer,stage-closeout-planner,plan-author,opus-auditor}.md`) carries single `cache_control: ephemeral 1h` block.
    - `tools/scripts/validate-cache-block-sizing.ts` present + wired to `package.json` `validate:all` chain.
    - `npm run validate:all` green.
  - stub_body_sections: §1, §2.1, §4.2, §7, §Open Questions.

- **T10.3 → TECH-504**
  - operation: `file_task`
  - title: `Tier 2 per-Stage ephemeral bundle + domain-context-load Phase N`
  - priority: `high`
  - issue_type: `TECH`
  - depends_on: [`TECH-503`]
  - related: []
  - parent_plan: `ia/projects/lifecycle-refactor-master-plan.md`
  - task_key: `T10.3`
  - stage: `10`
  - phase: `3`
  - notes: Edit `ia/skills/domain-context-load/SKILL.md`: add Phase N (final concat) assembling glossary subset + spec_sections + invariants_summary from MCP aggregator into single content block; emit with `cache_control: {"type":"ephemeral","ttl":"1h"}`; Phase N asserts token estimate ≥ F2 floor before emit (runtime safety net complementing T10.2 CI gate); document in skill §Overview. Update `ia/skills/stage-file-plan/SKILL.md` to invoke `domain-context-load` exactly once per Stage (shared Tier 2 bundle reused across all Tasks). Update `ia/rules/plan-apply-pair-contract.md` to cite Tier 2 bundle reuse contract. Verify validator accepts new Phase N.
  - acceptance:
    - `domain-context-load` SKILL carries Phase N with `cache_control` emit + runtime token-floor assert.
    - `stage-file-plan` SKILL invokes `domain-context-load` exactly once per Stage (per-Task calls removed).
    - `ia/rules/plan-apply-pair-contract.md` cites Tier 2 bundle reuse contract.
    - `npm run validate:all` green.
  - stub_body_sections: §1, §2.1, §4.2, §7, §Open Questions.

- **T10.4 → TECH-505**
  - operation: `file_task`
  - title: `F3 bulk stagger + F5 tool-allowlist uniformity + B2 retire R11 §Findings gate`
  - priority: `high`
  - issue_type: `TECH`
  - depends_on: [`TECH-503`]
  - related: [`TECH-504`]
  - parent_plan: `ia/projects/lifecycle-refactor-master-plan.md`
  - task_key: `T10.4`
  - stage: `10`
  - phase: `4`
  - notes: Edit `ia/skills/plan-author/SKILL.md` + `ia/skills/opus-audit/SKILL.md` Phase 0 guardrail — sequential dispatch for Stage-scoped bulk N→1 (no concurrent Opus fan-out; rev 4 A2 + amendment 2). Audit pair-seam agent bodies (`plan-reviewer`, `plan-applier` [T10.5 unified] OR legacy appliers if T10.5 not yet landed, `stage-file-planner`, `stage-file-applier`, `opus-code-reviewer`, `stage-closeout-planner`): enforce identical `tools:` frontmatter (F5). Author `tools/scripts/validate-agent-tools-uniformity.ts` wired to `validate:all`. **B2 retire R11 (commit-ordering critical):** same commit lands plan-author §Findings inline output + opus-audit Phase 0 drops "assert non-empty §Findings" gate. Never split — prevents mid-flight ordering breakage (amendment 3).
  - acceptance:
    - `plan-author` + `opus-audit` Phase 0 carry F3 sequential-dispatch guardrail text.
    - `validate-agent-tools-uniformity.ts` present + wired; `validate:all` green.
    - Single commit lands plan-author §Findings inline + opus-audit gate drop (git log verifies atomic pair).
    - `plan-author` Phase N output contract documents per-Task §Findings inline sub-section.
  - stub_body_sections: §1, §2.1, §4.2, §7, §Open Questions.

- **T10.5 → TECH-506**
  - operation: `file_task`
  - title: `B4 unified plan-applier consolidation (retire 3 per-pair appliers)`
  - priority: `high`
  - issue_type: `TECH`
  - depends_on: [`TECH-505`]
  - related: []
  - parent_plan: `ia/projects/lifecycle-refactor-master-plan.md`
  - task_key: `T10.5`
  - stage: `10`
  - phase: `5`
  - notes: Author `ia/skills/plan-applier/SKILL.md` — Sonnet literal-applier reading any `§*Fix Plan` / `§Stage Closeout Plan` tuple shape (`{operation, target_path, target_anchor, payload}`). Dispatch per operation type (fs edit, glossary row, BACKLOG archive, id purge, spec delete, status flip, digest emit). Escalate to Opus on anchor ambiguity. Bounded 1 retry on transient. Resolves legacy Open Q11. Retire `ia/skills/{plan-fix-apply,code-fix-apply,stage-closeout-apply}/` → move to `ia/skills/_retired/{name}/` with tombstone redirect header ("Retired — use `plan-applier` (unified)"). Retire agents `.claude/agents/{plan-fix-applier,code-fix-applier,stage-closeout-applier}.md` → move to `.claude/agents/_retired/`. Author `.claude/agents/plan-applier.md` (Sonnet; caveman preamble; tools uniformity per T10.4). Update pair-head skills + commands (`/plan-review`, `/code-review`, `/closeout`) to dispatch `plan-applier`. Update `ia/rules/plan-apply-pair-contract.md`.
  - acceptance:
    - `ia/skills/plan-applier/SKILL.md` present with dispatch table + escalation contract.
    - `.claude/agents/plan-applier.md` present (Sonnet, caveman, uniform tools frontmatter).
    - 3 retired skills + 3 retired agents moved to `_retired/` with tombstone headers.
    - `/plan-review`, `/code-review`, `/closeout` command dispatcher files point to `plan-applier`.
    - `ia/rules/plan-apply-pair-contract.md` references unified applier.
    - `npm run validate:all` green.
  - stub_body_sections: §1, §2.1, §4.2, §7, §Open Questions.

- **T10.6 → TECH-507**
  - operation: `file_task`
  - title: `R1 SSE cache-commit event gate + C4 progress-emit marker extension`
  - priority: `medium`
  - issue_type: `TECH`
  - depends_on: [`TECH-503`]
  - related: []
  - parent_plan: `ia/projects/lifecycle-refactor-master-plan.md`
  - task_key: `T10.6`
  - stage: `10`
  - phase: `6`
  - notes: Edit `ia/skills/subagent-progress-emit/SKILL.md`: add §SSE cache-commit gate documenting `message_start.usage.cache_creation_input_tokens` as conservative commit signal + `content_block_delta` fallback (R1); forbid ms-latency heuristics in skill bodies. Extend `⟦PROGRESS⟧` marker shape with optional `cache:{written|hit|miss|n/a} tokens:{N}` suffix when `usage` available (rev 4 C4 fold — zero regression at default). Document Q17 upstream-pending note in §Caveats. Update 15 lifecycle skills' `phases:` frontmatter to optionally consume SSE usage (backwards compatible).
  - acceptance:
    - `subagent-progress-emit` SKILL carries §SSE cache-commit gate + §Caveats Q17 note + ms-latency-heuristic forbidden clause.
    - `⟦PROGRESS⟧` marker spec extended with optional `cache:` + `tokens:` suffix; default emit unchanged.
    - 15 lifecycle skills' `phases:` frontmatter audited; backwards-compat confirmed (no forced schema break).
    - `npm run validate:all` green.
  - stub_body_sections: §1, §2.1, §4.2, §7, §Open Questions.

- **T10.7 → TECH-508**
  - operation: `file_task`
  - title: `D2 cache-invalidation cascade note + D3 20-block single-block guardrail`
  - priority: `low`
  - issue_type: `TECH`
  - depends_on: [`TECH-503`, `TECH-504`]
  - related: []
  - parent_plan: `ia/projects/lifecycle-refactor-master-plan.md`
  - task_key: `T10.7`
  - stage: `10`
  - phase: `7`
  - notes: Edit `docs/mcp-ia-server.md`: add §Cache invalidation impact section — any `tools/mcp-ia-server/` edit cascades down to cached Stage bundles per F5; PR author must flag tool-registration edits in PR description + expect Stage-boundary re-warm. Edit `ia/skills/subagent-progress-emit/SKILL.md` (or author new `ia/rules/subagent-caching-guardrails.md` — author's call): add D3 single-block rule — NEVER emit multi-block stable prefix; `@`-concatenation at skill-preamble author time is the ONLY supported assembly mode; multi-`@`-load with separate `cache_control` per block is forbidden (risks falling outside F6 20-block lookback as conversation grows). Cross-link both notes from `docs/prompt-caching-mechanics.md` §6 + §7.
  - acceptance:
    - `docs/mcp-ia-server.md` carries §Cache invalidation impact section with PR-flag requirement.
    - D3 single-block rule documented in `subagent-progress-emit` SKILL OR dedicated `subagent-caching-guardrails.md` rule.
    - `docs/prompt-caching-mechanics.md` §6 + §7 cross-link both D2 + D3 notes.
    - `npm run validate:all` green.
  - stub_body_sections: §1, §2.1, §4.2, §7, §Open Questions.

- **T10.8 → TECH-509**
  - operation: `file_task`
  - title: `P1 savings-band validation replay + user sign-off + M9 migration flip`
  - priority: `high`
  - issue_type: `TECH`
  - depends_on: [`TECH-502`, `TECH-503`, `TECH-504`, `TECH-505`, `TECH-506`, `TECH-507`, `TECH-508`, `TECH-492`]
  - related: []
  - parent_plan: `ia/projects/lifecycle-refactor-master-plan.md`
  - task_key: `T10.8`
  - stage: `10`
  - phase: `8`
  - notes: Select ≥ 3 post-merge Stages measured in Stage 9 T9.4 Q9 baseline (TECH-492 telemetry). Replay each under Tier 1 + Tier 2 cache enabled; capture actual cache-hit-rate + write-count + read-count + token-delta per Stage. Compute actual savings % vs R5 predicted band (−10% at 2 reads / +23% at 3 reads / +50% at 5 reads / +57% at 6 reads). Assert actual within ±5% of predicted band at measured read count; delta > ±5% → investigate + patch (likely F2 sizing gate regression or F5 cascade violation) + re-replay. Present validation report to user; wait for explicit sign-off ("LGTM" / "ship cache layer"); flip migration JSON M9 `done` + stamp sign-off timestamp. Run final `npm run validate:all` + `npm run verify:local` on main post-Stage-10.
  - acceptance:
    - ≥ 3 Stages replayed under cache-enabled config; per-Stage telemetry captured (hit-rate + write/read counts + token-delta).
    - Actual savings % computed + compared against R5 band at measured read count; within-±5% assertion documented in validation report.
    - User sign-off recorded verbatim in migration JSON M9.signoff field with timestamp.
    - Migration JSON M9 `done: true` + `signoff_timestamp` stamped.
    - `npm run validate:all` + `npm run verify:local` green on main post-Stage-10.
  - stub_body_sections: §1, §2.1, §4.2, §7, §Open Questions.

#### §Plan Fix — PASS (no drift)

> plan-review exit 0 — all Task specs (TECH-506, TECH-507, TECH-508, TECH-509) aligned after `phases:` frontmatter merge. No pending tuples. Downstream pipeline continue.

---
