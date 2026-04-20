# Anthropic Prompt-Caching Mechanics — Reference

> **Purpose:** Single source-of-truth for Anthropic prompt-caching facts (F1–F6), rev 4 amendments, and refinements (R1–R5) that drive cache-optimization decisions across lifecycle skills + agents + subagent bundle assembly.
>
> **Origin:** Rev 4 tier D1 candidate in `docs/lifecycle-opus-planner-sonnet-executor-exploration.md` (2026-04-19 rev 4). Pending Q9 baseline before any runtime fold.
>
> **Status:**
> - **Facts (§1 F1–F6) + decisions (§2 amendments, §3 architecture, §4 R1–R5) — locked + normative.** Signed off 2026-04-19. Skill authors treat as authoritative source.
> - **Runtime wiring — Draft.** Cache-block emission, CI sizing gate, Tier 1/Tier 2 assembly ships as part of Stage 10 in `ia/projects/lifecycle-refactor-master-plan.md` (post-merge + Q9-baseline-gated).
>
> **Authority chain:** This doc ≻ skill preambles ≻ agent bodies. Conflicts on facts or decisions resolve in favor of this doc. Skill bodies MUST NOT restate F1–F6 or R1–R5; link here instead.

---

## 1. Verified facts (F1–F6)

### F1 — Cost ratios + break-even

- Cache **read** = `0.1×` base input token cost.
- Cache **write** = `1.25×` base input token cost for 5-minute TTL; `2×` base for 1-hour TTL.
- Break-even (writes recouped by reads):
  - 5m TTL: 1 subsequent read recovers write premium (0.1 + 1.25 vs 2.0 → net win at 1 read).
  - 1h TTL: 2 subsequent reads required (0.2 + 2.0 vs 3.0 → net win at 2 reads; see §4 R5 for Stage-level band).

### F2 — Minimum cacheable block (silent-fail floor)

- Opus 4.7 minimum cacheable block size = **4,096 tokens**.
- Sonnet 4.6 minimum = 1,024 tokens (confirm per-model at bundle-author time).
- Below floor: silent no-cache. **No error. No warning. No telemetry flag.** Block is sent as fresh input every call.
- **Implication:** glossary-discover anchors alone (~500–2,000 tok) fail F2 silently on Opus. Inflate to rules-only floor (~5,192 tok measured, ×1.27 clearance) or full-glossary ceiling (~20,029 tok, ×5 clearance).

### F3 — Concurrent requests do not share cache hits

- Cache is keyed per request. Concurrent requests with identical prompts do not share hits; each pays either a write or a miss-then-write.
- **Implication:** bulk fan-out patterns (e.g. Stage-scoped bulk plan-author, bulk opus-audit) MUST stagger calls sequentially to reuse the first call's write. Parallel dispatch regresses to per-call write cost.

### F4 — Default TTL regression (2026-03-06)

- Anthropic silently regressed default TTL from `1h` → `5m` on 2026-03-06. Deployments relying on the old default saw 12× more writes overnight.
- Explicit opt-in required for 1h: `cache_control: {"type": "ephemeral", "ttl": "1h"}` on the block.
- **Implication:** every `cache_control` declaration in skill preambles or agent bodies MUST specify `ttl` explicitly. Never rely on platform default.

### F5 — Invalidation cascade

- Invalidation cascades **downstream only**:
  - `tools:` change → invalidates `system` + `messages`.
  - `system` change → invalidates `messages`.
  - `messages` change → invalidates later message blocks only.
- Upstream changes never invalidate (i.e. a `messages` edit does not invalidate `system` or `tools`).
- **Implication:** MCP tool registration edits cascade down to cached Stage bundles. Treat `tools/mcp-ia-server/` edits as cache-invalidating across all downstream bundles.

### F6 — 20-block lookback per breakpoint

- Each `cache_control` breakpoint has a 20-content-block lookback window for hit matching.
- Applies to **API content-block count**, not `@`-include file count. `@`-concatenation at doc-assembly time collapses 20 files into 1 block.
- **Implication:** option (a) single concatenated block eliminates F6 risk. Option (b) multi-block emission for stable prefix is forbidden — risks falling outside lookback window as conversation grows.

---

## 2. Rev 4 amendments (folded into rev 4 candidate pool)

1. **Tighten P1 delta + F2 gate** — P1 savings band conditional on F2 clearance + bundle ≥ 40% of Stage prefix + ≥ 3 pair-head reads per Stage (see §4 R5).
2. **A2 dispatcher F3 fix** — bulk plan-author staggers Opus calls sequentially; concurrent dispatch breaks cache hits.
3. **B2 + R11 commit ordering** — B2 retires R11 §Findings gate; commit B2 before any opus-audit refactor.
4. **F5 tool-allowlist uniformity** — all pair seams share identical `tools:` frontmatter; prevents cascade-induced invalidation across seams within one Stage.
5. **Q14a / Q14b reframe** — Q14a resolved (lock 1h both tiers). Q14b closed as unrealistic under 5m TTL + N=4.

---

## 3. Two-tier cache architecture

### Tier 1 — Stable cross-Stage block

- **Content:** rules (invariants, terminology-consistency, mcp-ia-default, agent-output-caveman, agent-lifecycle, project-hierarchy, orchestrator-vs-spec) + glossary preamble + router snapshot.
- **Assembly:** `@`-concatenation at skill-preamble author time; single content block emitted per request.
- **Placement:** `messages` block (Claude Code subagent host does not expose `system` to skill authors).
- **`cache_control`:** `{"type": "ephemeral", "ttl": "1h"}`.
- **Reuse scope:** all pair seams within one Stage (plan-review, plan-fix-apply, stage-file-plan, stage-file-apply, code-review, code-fix-apply, stage-closeout-plan, stage-closeout-apply).
- **F2 clearance:** measured ~5,192 tok rules-only; ~20,029 tok with full glossary. Clears F2 floor.

### Tier 2 — Ephemeral per-Stage bundle

- **Content:** Stage-scoped glossary subset (from `glossary_discover` + `glossary_lookup`) + `spec_sections` for Tasks in Stage + `invariants_summary` filtered to Stage-touched subsystems.
- **Assembly:** `domain-context-load` skill Phase N concatenates MCP aggregator output into a single content block.
- **Placement:** `messages` block, positioned after Tier 1 prefix.
- **`cache_control`:** `{"type": "ephemeral", "ttl": "1h"}`.
- **Reuse scope:** all Tasks within the Stage.
- **F2 clearance:** depends on Stage size + glossary subset; sizing gate (§5 C1/R2) enforces floor at CI time.

Both tiers use **option (a) single concatenated block** to eliminate F6 risk.

---

## 4. Refinements R1–R5 (folded)

### R1 — SSE event gate for cache-write commit

- Anthropic docs do not specify exact SSE event marking cache-write commit (filed Q17).
- Conservative gate: `message_start` event carries `usage.cache_creation_input_tokens`. Use as commit signal.
- Safe fallback: `content_block_delta` (first output token). Guaranteed post-commit.
- Skills MUST NOT rely on millisecond-latency heuristics.

### R2 — CI-gate sizing check (promoted from runtime warning)

- Fail CI if Tier 1 or Tier 2 block token estimate < F2 floor for target model.
- Never ship silent no-cache. Runtime warning is too late (already billed).
- Validator: `tools/scripts/validate-cache-block-sizing.ts` (post-Stage-10 authored).

### R3 — Q14 TTL resolution

- Both tiers lock 1h TTL. Walkthrough under 5m default at N=4:
  - T+0: plan-author write (cached)
  - T+2min: plan-review hit
  - T+4min: plan-fix-apply + re-entry (cache near expiry)
  - T+6min: executor fan-out — **5m expired**, each executor pays fresh write
- 1h TTL covers full Stage lifecycle with margin.

### R4 — F6 assembly mode quantification

- Option (a) single concatenated block: eliminates F6 block-count risk.
- Option (b) multi-block emission: forbidden for stable prefix.
- Both Tier 1 + Tier 2 use option (a): Tier 1 via `@`-concat pre-assembly; Tier 2 via `domain-context-load` Phase N concatenation.

### R5 — P1 savings band (recalibrated under 1h TTL)

| Reads/Stage | Cache cost (×base) | Baseline cost (×base) | Savings |
|-------------|---------------------|------------------------|---------|
| 2 | 2.2 | 2.0 | **−10% (net loss)** |
| 3 | 2.3 | 3.0 | +23% |
| 5 | 2.5 | 5.0 | +50% |
| 6 (N=4 typical: plan-author + plan-review + fix re-entry + 4 executors) | 2.6 | 6.0 | +57% |

- **Precondition:** ≥ 3 pair-head reads per Stage (not "any cache use").
- Stages with < 3 reads/Stage (e.g. N=1 single-task path) → cache disabled or Tier 2 skipped.

---

## 5. Sizing gate (C1 / R2) — measured baseline

| Source | Bytes | Approx tokens (×0.25) | F2 status (Opus 4.7 floor = 4,096) |
|--------|-------|------------------------|------------------------------------|
| Rules only (invariants + terminology-consistency + mcp-ia-default + agent-output-caveman + agent-lifecycle + project-hierarchy + orchestrator-vs-spec) | 23,629 | ~5,907 | ✅ clears ×1.44 |
| Rules + glossary preamble slice (header + authorship note; Tier 1 measured baseline) | 25,717 | ~6,429 | ✅ clears ×1.57 |
| Rules + full glossary | 80,116 | ~20,029 | ✅ clears ×5.0 |
| Glossary-discover anchors only | ~2,000–8,000 | ~500–2,000 | ❌ silent no-cache |
| Full per-Stage bundle (rules + glossary subset + Stage spec_sections) | variable | target ≥ 6,000 | sizing gate enforces |

**Tier 1 measured baseline (TECH-503):** 8 agent bodies (plan-reviewer, plan-fix-applier, stage-file-planner, stage-file-applier, opus-code-reviewer, stage-closeout-planner, plan-author, opus-auditor) measured at **7,185–7,834 tok** post-emission (validator confirmed, 2026-04-19). Clearance ×1.75–×7.65 over respective F2 floors.

**Validator command:** `npm run validate:cache-block-sizing` — parses `.claude/agents/*.md` for `cache_control` declarations; estimates block token count (bytes × 0.25); fails exit 1 if below F2 floor. Wired into `npm run validate:all` chain.

**Authoring rule for future agent bodies:** any new agent that declares a `cache_control` block MUST pass `npm run validate:cache-block-sizing`. Silent no-cache (block below F2 floor) blocks CI — never ships. Non-emitting agents (no `cache_control` declared) are skipped — opt-in surface per F3 stagger economics.

**Authoring rule:** skill preamble `@`-concatenation MUST include rules-only minimum (Tier 1). Tier 2 bundle author (domain-context-load Phase N) MUST assert token estimate ≥ F2 before emitting `cache_control`.

---

## 6. Invalidation cascade (F5) — practical impact

| Edit target | Cascade | Skill-author action |
|-------------|---------|---------------------|
| `tools/mcp-ia-server/` (MCP tool registration) | invalidates all Tier 1 + Tier 2 blocks system-wide | warn in PR description; expect full cache re-warm next Stage |
| `ia/rules/*.md` (any rule in Tier 1 `@`-concat) | invalidates Tier 1 + Tier 2 | expect Stage-boundary re-warm |
| `ia/specs/glossary.md` (glossary row add/edit) | invalidates Tier 1 (if glossary included) + Tier 2 | expect Stage-boundary re-warm |
| `ia/projects/{id}.md` spec edit | invalidates only Task-local context, not Tier 1 or Tier 2 | no action |
| `ia/projects/*-master-plan.md` edit | invalidates only plan-reading seams | no action |

---

## 7. Skill-author checklist (per skill with `cache_control`)

Before declaring `cache_control` in a skill preamble or agent body:

- [ ] Tier 1 vs Tier 2 placement decision recorded in skill §Overview.
- [ ] Explicit `ttl` specified (never rely on default — F4).
- [ ] Single concatenated block (option (a)) — no multi-block emission (F6/R4).
- [ ] F2 sizing gate referenced (either CI check R2 or runtime assertion).
- [ ] F3 staggered dispatch if bulk-fan-out (no concurrent identical prompts).
- [ ] F5 cascade note: any `tools:` allowlist change cross-referenced with downstream seams.
- [ ] Read-count expectation documented: if < 3 reads/Stage expected, cache disabled.

---

## 8. Open questions

- **Q9 (baseline)** — instrumented-Stage measurement recording **pair-head read count per Stage** (not just total tokens). Precondition for Stage 10 fold. Captured in master plan T9.4 telemetry TECH stub.
- **Q17 (new)** — exact SSE event for cache-write commit guarantee. Filed with Anthropic. Current gate: `message_start` conservative; `content_block_delta` safe fallback.

---

## 9. Authoritative neighbors

- `docs/lifecycle-opus-planner-sonnet-executor-exploration.md` §Design Expansion — rev 4 candidates + cache-mechanics amendments (2026-04-19 rev 4) — candidate pool + verdict table + priority matrix.
- `ia/projects/lifecycle-refactor-master-plan.md` Stage 10 — Prompt-Caching Optimization Layer — task rows that land this reference.
- `ia/skills/domain-context-load/SKILL.md` — Tier 2 bundle assembly site.
- `ia/rules/plan-apply-pair-contract.md` — pair seams that inherit Tier 1 prefix.
- Anthropic docs — <https://docs.anthropic.com/en/docs/build-with-claude/prompt-caching> (verify F1–F6 against current version at author time; Anthropic revises silently per F4 precedent).
