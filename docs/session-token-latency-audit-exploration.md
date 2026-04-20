---
purpose: "Exploration — remediation plan for the external 2026-04-19 token-economy + latency audit (everything BELOW the locked lifecycle planner/executor split): MCP descriptor bloat, doc-ambient redundancy, hook shell-fork latency, preamble de-dupe, progressive disclosure, harness-state unification."
audience: both
loaded_by: ondemand
slices_via: none
---

# Session token + latency audit — remediation — exploration (stub)

> **Status:** Draft exploration stub — pending `/design-explore docs/session-token-latency-audit-exploration.md` polling + expansion.
> **Created:** 2026-04-19
> **Last updated:** 2026-04-19
> **Source audit:** [`docs/ai-mechanics-audit-2026-04-19.md`](ai-mechanics-audit-2026-04-19.md) — in-repo persistent copy of the external audit (original: `/Users/javier/teselagen/lims-2/TEMP-territory-developer-ai-audit-2026-04-19.md`, disposable). Written 2026-04-19 by an external reviewing agent.
> **External design review:** [`docs/session-token-latency-design-review-2026-04-19.md`](session-token-latency-design-review-2026-04-19.md) — input for Phase 0.5 interview. Flags 3 structural gaps (G1 Theme-B ownership split into mcp-lifecycle extension; G2 Theme-0 FREEZE contradiction; G3 baseline measurement as Stage 0) + 5 smaller gaps (G4–G8) + proposed answers to open questions §1–§11. Hypotheses to verify, not facts to transcribe. Also written 2026-04-19 by the same external reviewing agent; original `TEMP-…` file in `lims-2` remains disposable.
> **Proposed owner plan:** new umbrella `ia/projects/session-token-latency-master-plan.md` (multi-step orchestrator, authored post-lifecycle-refactor M8 sign-off).
> **Sibling master plans (coordination required — see §Sibling master plan coordination below):**
> - [`ia/projects/lifecycle-refactor-master-plan.md`](../ia/projects/lifecycle-refactor-master-plan.md) — In Progress, Stage 5. Stage 10 (post-M8, Q9-gated) lands Tier 1 + Tier 2 cache layer that touches Theme A / F surfaces.
> - [`ia/projects/mcp-lifecycle-tools-opus-4-7-audit-master-plan.md`](../ia/projects/mcp-lifecycle-tools-opus-4-7-audit-master-plan.md) — In Progress, Step 2 / Stage 2.2. Stages 1–5 Done (envelope + alias drop shipped). Stages 6–16 Draft (composites, mutations, IA-authorship, bridge, journal, post-review addendum).
> **FREEZE note:** the lifecycle-refactor freeze (`ia/state/lifecycle-refactor-migration.json`, CLAUDE.md §5) blocks `/master-plan-new` + `/master-plan-extend` + `/stage-decompose` + `/stage-file` against anything outside `ia/projects/lifecycle-refactor-master-plan.md` until M8 sign-off. This exploration is safe to author + expand during the freeze; only the subsequent `/master-plan-new` invocation waits on M8.

## Scope bar

**In scope.** Everything **beneath** the locked planner/executor split: MCP tool descriptors, the doc triangle (CLAUDE.md / AGENTS.md / `agent-lifecycle.md`), non-lifecycle skills, always-loaded rules, slash-command bodies, subagent bodies, hooks, `.claude/settings.json`, output styles, repo-root clutter, auto-memory pointers, cache-breakpoint placement policy.

**Out of scope.**

- The cognitive split itself (planner/executor, Step→Task collapse, plan-apply pair contract) — locked in [`docs/lifecycle-opus-planner-sonnet-executor-exploration.md`](lifecycle-opus-planner-sonnet-executor-exploration.md). This exploration is a consumer of that split, not a competitor.
- Unity runtime, bridge transport (`agent_bridge_job` Postgres protocol), web dashboard tree rendering, product-correctness changes.
- MCP **reshape** (composite bundles, envelope unification, IA-authorship mutation surface) — already owned by [`docs/mcp-lifecycle-tools-opus-4-7-audit-exploration.md`](mcp-lifecycle-tools-opus-4-7-audit-exploration.md). This exploration covers the **surface-area-reduction** angle on MCP (descriptor bloat, tool visibility gating, doc drift, cold-start latency) that complements but does not overlap the composite-bundles work.
- Backlog-yaml + parser work — owned by [`docs/backlog-yaml-mcp-alignment-exploration.md`](backlog-yaml-mcp-alignment-exploration.md).

## Problem

External audit observed that the repository already does most of the right things (parser cache, YAML-first backlog, MCP slicing, typed Plan-Apply contracts, `domain-context-load` subskill collapsing 8 inline copies into one). Under the hood, however, systemic token-drag and latency-drag points remain. The locked lifecycle refactor **inherits** these unless fixed first — every new bundle-assembly path, every Sonnet dispatch, every Plan-Apply pair seam pays the tax.

Five compounding themes:

1. **Always-loaded ambient is ~7–8k tokens** (CLAUDE.md + five `@`-imported rules). Much duplicates `AGENTS.md` and `docs/agent-lifecycle.md`. A single-source-of-truth pass would shrink ambient ~40–50% with zero expressive loss.
2. **Tool descriptors are bloated.** 34 MCP tools register unconditionally via `enabledMcpjsonServers: ["territory-ia"]`. Roughly half (Unity bridge, Postgres journal, compute) are never used in IA-authoring sessions but still occupy `tools/list`. `spec_section` and `spec_sections` additionally expose 7 "Rejected alias" params per tool whose only runtime behaviour is to throw `invalid_input`.
3. **Preamble redundancy.** The caveman directive is restated in ~40 surfaces (13 subagent bodies, ~30 skills, every slash-command "forward verbatim" block, output styles, skill seed prompts). The rule is already `alwaysApply: true` in an `@`-imported file.
4. **Slash-command / subagent / skill triple-statement.** `/implement` command body restates the exact Mission + Phase loop + Hard boundaries from `.claude/agents/spec-implementer.md` and `ia/skills/project-spec-implement/SKILL.md`. Triple storage, triple billing when a main session dispatches a subagent.
5. **Hook shell forks.** `bash-denylist.sh` + `cs-edit-reminder.sh` fork `python3` per tool call for JSON extraction. On macOS ≈ 20–60 ms per fork; hundreds of tool calls per session accumulate seconds of wall-clock latency.

Biggest leverage: descriptor pruning + ambient collapse + preamble de-dupe land in days, not weeks, and recover an estimated **20–30k tokens/session + ~1 s wall time** before any lifecycle refactor runs against the optimized base. Per-session recovery sits at ~4–6× the measured rev-4 Tier 1 block (5,192 tokens) — the proportional gain is outsized relative to implementation effort.

## Sibling master plan coordination

Item-level dedup + sequencing vs the two in-flight master plans. Each audit item mapped against current stage state so `/master-plan-new` against this exploration does not re-propose shipped or conflicting work.

Legend: **Shipped** = already Done in a sibling plan; drop from this exploration. **Conflict** = touches the same source files as a Draft sibling stage; sequencing dependency required. **Complement** = adjacent / stackable with sibling work; no conflict. **Independent** = no interaction.

### vs `mcp-lifecycle-tools-opus-4-7-audit-master-plan.md` (In Progress — Step 2 / Stage 2.2)

| Audit item | Sibling stage | Status | Disposition |
|---|---|---|---|
| **B2** — drop `spec_section` rejected-alias params | Stage 5 T5.1 **TECH-426** | **Shipped (2026-04-18)** | Drop B2 row from Theme B table. `spec-section.ts` + `spec-sections.ts` + `project-spec-journal.ts` already reject aliases with typed `invalid_input` envelope. Audit finding C2 is obsolete. |
| **B6** — MCP doc drift (README vs `registerTool()` count) | Stage 9 T9.4 (Draft) | **Complement** | Stage 9 T9.4 rewrites `docs/mcp-ia-server.md` catalog to add composite tools + `rule_section` + flag alias-drop migration. B6 lint (`npm run validate:mcp-readme`) stacks on top — author AFTER T9.4 lands to avoid lint-fails-during-rewrite churn. |
| **B3** — per-agent MCP tool allowlist audit (`verifier`, `closeout`, `stage-decompose`, etc.) | none (MCP plan touches server-side descriptors, not agent frontmatter) | **Independent** | Audit subagent `tools:` frontmatter under `.claude/agents/`. Unrelated to MCP plan scope. |
| **B4** — on-disk parse cache + `dist/` build | none | **Independent** | Cold-start perf; distinct from MCP descriptor surface. |
| **B5** — progressive disclosure (`spec_outline depth=1`, `list_rules alwaysApply`) | none | **Independent** | Default-response shape change; MCP plan does not touch these tools' defaults. |
| **B7** — flip `DEBUG_MCP_COMPUTE=1` in `.mcp.json` env | none | **Independent** | Env flag flip; zero surface overlap. |
| **B8** — `backlog-parser.ts` yaml-first call order + mtime cache | none (Stage 4 envelope wrap touches `backlog-*.ts` handlers, not parser) | **Independent** | Parser layer below handlers; no overlap. |
| **B9** — `.describe()` ≤ 120 char lint | none | **Independent** | Descriptor-prose lint; MCP plan trims descriptors by removing params, not shortening surviving prose. |
| Composite bundles / envelope / IA-authorship mutations (audit O3 / parts of C1) | Stages 7–16 (Draft) | **Out of scope** (already in Theme B ▷ row) | Owned by MCP reshape exploration — this doc does not propose duplicates. |

### vs `lifecycle-refactor-master-plan.md` (In Progress — Stage 5)

| Audit item | Sibling stage | Status | Disposition |
|---|---|---|---|
| **A1** — collapse CLAUDE.md / AGENTS.md / `agent-lifecycle.md` duplication | Stage 10 T10.2 (Draft, Q9-gated) | **Conflict** | Stage 10 T10.2 authors `ia/skills/_preamble/stable-block.md` concatenating `invariants` + `terminology-consistency` + `mcp-ia-default` + `agent-output-caveman` + `agent-lifecycle` + `project-hierarchy` + `orchestrator-vs-spec` + glossary preamble as a single cacheable block. That fixes the CACHE tax but leaves the authority-chain duplication (same facts in CLAUDE.md §3 + AGENTS.md + `docs/agent-lifecycle.md`). **Order:** A1 must run AFTER T10.2 so the stable-block concat target is already the canonical ingestion path when duplicates are removed upstream. |
| **A2** — de-dupe caveman preambles across 40 surfaces | Stage 10 T10.2 (same `stable-block.md` includes `agent-output-caveman`) | **Conflict** | Same dependency as A1. After T10.2 lands, caveman rule is in the stable-block; per-surface restatements can collapse to a single reference line with no behavioral change. **Order:** A2 after T10.2. |
| **A3** — revisit `CLAUDE_CODE_DISABLE_1M_CONTEXT=1` post-split | Stage 10 (validates P1 savings band) | **Complement** | 1M decision naturally falls out of Stage 10 replay (Q9 baseline vs cache-enabled). Fold A3 as a recorded decision in T10.8 sign-off report rather than separate item. |
| **A4** — promote oversized MEMORY.md entries to `.claude/memory/{slug}.md` | none | **Independent** | Memory-file hygiene; unrelated to refactor. |
| **B3** — per-agent MCP allowlist audit | Stage 10 T10.4 F5 tool-allowlist uniformity validator | **Complement** | T10.4 enforces uniformity across 6 pair-seam agents (`plan-reviewer`, `plan-applier`, `stage-file-planner`, `stage-file-applier`, `opus-code-reviewer`, `stage-closeout-planner`). B3 narrows the OTHER 7 subagents (`verifier`, `closeout`, `stage-decompose`, `project-new`, `spec-kickoff`, `design-explore`, `test-mode-loop`). Disjoint sets. Run B3 independently. |
| **C1 / C2** — flatten slash-command / subagent / skill triple-statement | Stage 10 T10.2 (stable-block lands in agent BODIES) | **Conflict** | Stage 10 T10.2 edits all 6 pair-seam agent bodies to emit stable-block as first `messages` content. C1/C2 also edit agent bodies (strip restatements). **Order:** C1/C2 after T10.2 + T10.4 (F5 uniformity) to avoid churning the same bodies twice. |
| **F1** — planner context snapshot as first-class artifact (`ia/state/{stage-id}-context-bundle.md`) | Stage 10 T10.3 (Tier 2 per-Stage bundle via `domain-context-load` Phase N) | **Superseded** | T10.3 materializes the planner context bundle as a RUNTIME `messages` content block with `cache_control`, not a committed `ia/state/` file. Same content, different persistence model. T10.3 wins on cache efficiency (no filesystem round-trip); F1's committed-artifact angle (diffability across Stages) becomes a follow-up question, not a primary proposal. **Order:** drop F1 primary; promote F1 diffability angle to Open Q. |
| **F3** — harness-level caveman enforcement (`PreCompletion` hook) | none | **Independent** | Claude Code harness capability tracking; unrelated to refactor. |
| **F4** — unified `.claude/runtime-state.json` | none | **Independent** | Flat-file marker unification. |
| **F5** — agent-authored cache-breakpoint plan MCP tool | Stage 10 T10.7 D3 single-block rule + 20-block guardrail | **Complement** | T10.7 forbids multi-block stable prefix at skill-author time. F5 adds a tool that RECOMMENDS the 4-anchor layout per Stage. Adjacent: T10.7 = prohibitive, F5 = prescriptive. No conflict. |
| **F6** — `skill_for_task` MCP tool | none | **Independent** | Skills-index navigator; unrelated to refactor. |
| Stable-prefix / cache-breakpoint hooks (D2 cache-prefix stability) | Stage 10 T10.2 (F2 sizing gate CI) + T10.7 (20-block guardrail) | **Complement** | D2 deterministic preamble complements T10.2 runtime cache emission. Same direction, different layer (hook vs agent body). |

### Summary

- **1 item dropped outright** (B2 — shipped).
- **1 item superseded** (F1 — replaced by T10.3 runtime bundle; diffability angle demoted to Open Q).
- **4 items carry a sequencing dependency** (A1, A2, C1, C2 — must run AFTER lifecycle-refactor Stage 10 T10.2).
- **2 items fold into sibling reporting** (A3 into T10.8 sign-off; B6 stacks on T9.4 catalog rewrite).
- **Remaining 24 items independent** of both master plans.

## Audit findings — grouped by remediation theme

Audit enumeration retained verbatim in the cross-reference column so a downstream `/design-explore` pass can walk the source doc section-by-section without re-lookup. Sibling-exploration rows (prefix ▷) are out of scope here and tracked only for cross-link.

### Theme A — Ambient context collapse (always-loaded)

**Sequencing note.** A1 + A2 **must land AFTER** lifecycle-refactor Stage 10 T10.2 (stable-block emission) — see §Sibling master plan coordination. Rationale: T10.2 builds `ia/skills/_preamble/stable-block.md` concatenating the 8 rule files as a cacheable block; once that canonical assembly is in place, upstream duplicate text can be safely collapsed. Running A1/A2 before T10.2 would churn the same agent bodies twice. A3 folds into T10.8 sign-off report; A4 is independent.

| # | Audit id | Finding | Effort | Est. saving | Confidence |
|---|---|---|---|---|---|
| A1 | C3 | Collapse CLAUDE.md / AGENTS.md / `ia/rules/agent-lifecycle.md` / `docs/agent-lifecycle.md` — same lifecycle taxonomy restated 4×. Always-loaded slice ≈ 3k tokens. Declare `docs/agent-lifecycle.md` as sole authority; shrink rule to 10-line stub; shrink CLAUDE.md §3 Key files to ~15 lines. **Depends on** lifecycle-refactor T10.2 landed. | 1 d | 2–2.5k tokens/session | high |
| A2 | C4 | De-dupe caveman preambles. Rule is `alwaysApply: true` + `@`-imported into CLAUDE.md. Replace every full-text restatement (13 subagents + ~30 skills + commands + handoff seeds) with a single canonical reference line (≤15 tokens). **Depends on** lifecycle-refactor T10.2 landed (caveman rule enters stable-block). | 2 h | 2.5–3k tokens across multi-stage run | high |
| A3 | m3 | Revisit `CLAUDE_CODE_DISABLE_1M_CONTEXT=1` + autocompact 65% once planner/executor split lands; Opus stages reading full master plan + all Task specs benefit from 1M. Record decision in `prompt-caching-mechanics.md`. | 30 min (decision + doc edit) | latent (planner stages) | medium |
| A4 | m7 | Promote oversized `MEMORY.md` entries (≥10 lines) to `.claude/memory/{slug}.md` per the pointer pattern documented in CLAUDE.md §3. `.claude/memory/` is currently empty. | 1 h | ~0.5k tokens (per-session-visible MEMORY shrink) | high |

### Theme B — MCP surface-area reduction (complements reshape)

**Scope trim.** Audit finding C2 (B2 — drop `spec_section` rejected aliases) is **already SHIPPED** via MCP plan Stage 5 T5.1 **TECH-426 Done (2026-04-18)**. Row retained below as a strikethrough cross-ref so `/design-explore` readers walking the audit sequence understand the finding is closed. B3 complements lifecycle-refactor Stage 10 T10.4 (disjoint agent sets — see §Sibling master plan coordination). B6 stacks on MCP plan Stage 9 T9.4 (run AFTER T9.4 lands).

| # | Audit id | Finding | Effort | Est. saving | Confidence |
|---|---|---|---|---|---|
| B1 | C1 | Split `territory-ia` MCP into IA-core + Unity-bridge servers (or enable `defer_loading: true` per-tool). Default IA-only; opt-in bridge via a hook that flips `enabledMcpjsonServers` when a verify/impl stage is about to run. | 2–3 d | 5–8k tokens/session | high |
| ~~B2~~ | ~~C2~~ | ~~Drop 7 "Rejected alias" optional fields from `spec_section` + `spec_sections` descriptors.~~ **Shipped** — MCP plan Stage 5 T5.1 TECH-426 Done (2026-04-18). Rejection logic kept server-side with typed `invalid_input` envelope. Do not re-propose. | — | already recovered | — |
| B3 | M4 | Per-agent MCP tool allowlist audit. `spec-implementer` already uses `tools:` frontmatter well; audit `verifier`, `closeout`, `stage-decompose`, `project-new`, `spec-kickoff`, `design-explore`, `test-mode-loop` (disjoint from lifecycle-refactor T10.4 F5 uniformity validator, which covers 6 pair-seam agents). | 2 h | 0.4–0.8k per dispatch × N | high |
| B4 | M5 | On-disk parse cache keyed by mtime (`tools/mcp-ia-server/.cache/parse-cache.json`) + switch `.mcp.json` entry from `tsx` on sources to compiled `dist/` build. | 3 h | MCP cold-start ~7× faster (1500 ms → 200 ms) | high |
| B5 | M6 | Progressive disclosure in `spec_outline` (default `depth=1`) and `list_rules` (default `alwaysApply: true` only). Opt-in full response via new param. | 1 h | 1–2k per call | high |
| B6 | M7 | Fix MCP doc drift — README says 29 tools; `src/index.ts` registers 34. Add CI check comparing `registerTool(` call count to README table row count; ideally auto-generate the table. **Depends on** MCP plan Stage 9 T9.4 landing `docs/mcp-ia-server.md` catalog rewrite first (avoid lint-fails-during-rewrite churn). | 1 h | agent first-call success rate ↑ | very high |
| B7 | m4 | Flip `DEBUG_MCP_COMPUTE=1` on by default in `.mcp.json` env — stderr path is stdio-safe; per-tool cold-start timing pays for itself on first-of-day sessions. | 5 min | observability (latent saving) | very high |
| B8 | m5 | Audit `backlog-parser.ts` call order: post-yaml-migration most reads should resolve from `ia/backlog/{id}.yaml`; verify yaml is first-checked before `BACKLOG.md` fallback. Cache yaml manifest with mtime invalidation. | 1 h | cumulative on most-frequent MCP tool | high |
| B9 | m6 | Enforce `.describe()` style rule: tool descriptors ≤120 chars per param. Current `unity_bridge_command` pushes 300+ chars per param. Add a lint. | 1 h | incremental descriptor trim | high |
| ▷ | — | Composite bundles (`issue_context_bundle`, `lifecycle_stage_context`, `orchestrator_snapshot`), envelope unification, IA-authorship mutation surface. **Out of scope — owned by** [`mcp-lifecycle-tools-opus-4-7-audit-exploration.md`](mcp-lifecycle-tools-opus-4-7-audit-exploration.md). | — | — | — |

### Theme C — Dispatch path flattening (commands ↔ subagents ↔ skills)

**Sequencing note.** C1 + C2 edit the SAME pair-seam agent bodies that lifecycle-refactor Stage 10 T10.2 (stable-block emission) + T10.4 (F5 tool-uniformity validator) touch. **Order:** C1/C2 must run AFTER T10.2 + T10.4 to avoid editing bodies twice + re-triggering the F5 uniformity validator mid-refactor. C3 is independent.

| # | Audit id | Finding | Effort | Est. saving | Confidence |
|---|---|---|---|---|---|
| C1 | M1 | Flatten `/slash-command → subagent-body → skill-body` triple-statement. `/implement`, `/kickoff`, `/verify-loop`, `/closeout`, `/ship`, `/ship-stage` all carry a `Subagent prompt (forward verbatim)` block restating the subagent's own body. Command forwards only parameters (ISSUE_ID, resolved paths, gate boundary); subagent body is authoritative. **Depends on** lifecycle-refactor T10.2 + T10.4 landed. | 4 h | 8–12k tokens on a full `/ship` run | medium-high |
| C2 | m10 | `/ship` slash command is 192 lines. Collapse to ~60 lines that dispatch + gate, trusting subagent bodies. Same pattern as C1 but specific to `/ship`. **Depends on** lifecycle-refactor T10.2 + T10.4 landed. | 1 h | 2–4k tokens per `/ship` invocation | high |
| C3 | M8 | Skill-seed lint — every `Seed prompt` code block in `ia/skills/*/SKILL.md` must name an existing target subagent + reference files that exist. `npm run validate:skill-seeds`. Prevents silent subagent-rename drift. | 2 h | debug-time saver (not a token saver) | high |

### Theme D — Hook-plane hygiene

| # | Audit id | Finding | Effort | Est. saving | Confidence |
|---|---|---|---|---|---|
| D1 | C5 | Replace `verification-reminder.sh` Stop hook with conditional version (fires only when branch ≠ main, last verify failure, HEAD ahead of upstream) or delete. Unconditional reminder is pure noise + destabilises 1 h-cache suffix eligibility. | 5 min | signal-to-noise + cache stability | very high |
| D2 | M2 | `session-start-prewarm.sh` emits volatile facts (branch name, dirty count) at message prefix — destabilises cached prefix on every session. Route banner to stderr; or emit a **deterministic** cacheable preamble (fixed block naming active freeze + MCP server version + enabled ruleset) so Tier 1 cache hits on first real message. | 2 h | cache-hit eligibility on first turn | medium |
| D3 | M3 | Convert `bash-denylist.sh` + `cs-edit-reminder.sh` python3-fork JSON parsing to pure-shell (existing sed fallback is correct; promote to primary). Fallback to `jq` when on-path. | 1 h | 30–60 ms × N tool calls per session | very high |
| D4 | m9 | Add a `PreCompact` (or Stop-inspecting) hook that writes compact-state summary to `.claude/last-compact-summary.md`. Agents currently lose task context at compact boundaries with no persistence. | 2 h | compact-survival UX | medium |

### Theme E — Repo-root + memory hygiene

| # | Audit id | Finding | Effort | Est. saving | Confidence |
|---|---|---|---|---|---|
| E1 | m1 | `.gitignore` + untrack `Bury_20260307_131752.json`, `mono_crash.*`. Agents' orientation `ls` shortens by dozens of lines. | 15 min | orientation-turn brevity | very high |
| E2 | m2 | SessionStart banner says `memory: MEMORY.md + ~/.claude-personal/.../memory/` but actual per-project memory path under the harness standard (`.claude/projects/`) differs. Audit + correct the banner string. | 15 min | prevents misguided memory lookups | very high |
| E3 | m8 | Trim `.claude/output-styles/verification-report.md` (87 lines) to ~30 lines; reference `docs/agent-led-verification-policy.md` for field semantics, keep only Part 1 / Part 2 structure + example block. | 1 h | per-activation descriptor size | high |

### Theme F — Out-of-the-box / larger bets (rev-4-era alignment)

**Scope trim.** F1 (planner context snapshot as committed artifact) is **SUPERSEDED** by lifecycle-refactor Stage 10 T10.3 — same bundle content, but emitted as a runtime `messages` content block with `cache_control` rather than a committed `ia/state/` file. T10.3 wins on cache efficiency (no filesystem round-trip). The "committed artifact = diffable across Stages" angle remaining from F1 becomes Open Q11 below, not a primary proposal. F2–F7 unchanged.

| # | Audit id | Finding | Effort | Rationale | Confidence |
|---|---|---|---|---|---|
| ~~F1~~ | ~~O1~~ | ~~Planner context snapshot as a first-class artifact (`ia/state/{stage-id}-context-bundle.md`).~~ **Superseded** — lifecycle-refactor Stage 10 T10.3 (Tier 2 per-Stage bundle via `domain-context-load` Phase N) emits the same content as a runtime cacheable block. Diffability-across-Stages angle demoted to Open Q11. | — | — | — |
| F2 | O2 | Per-session `tool-usage.log` (`.claude/tool-usage.jsonl`) written by a PostToolUse hook capturing `{tool_name, args_hash, result_hash, ts}`. Subagents read before acting; re-use within Stage window instead of re-calling MCP. Content-addressable MCP memoisation at session boundary. Exploits F3 fact (concurrent requests don't share cache). | 2–3 d | repeat `glossary_discover` + `router_for_task` savings | medium |
| F3 | O3 | Compiler-style output-compression directive — frontmatter `output-style: caveman` parsed by a harness `PreCompletion` hook; rule enforcement at the harness layer instead of ~40 surface restatements. **Harness-support-dependent** — Claude Code team signal required. Floated as direction, not commitment. | harness-gated | zero per-surface cost for caveman | low (feasibility uncertain) |
| F4 | O4 | Unified `.claude/runtime-state.json` replacing scattered flat-file markers (`last-verify-exit-code`, `last-bridge-preflight-exit-code`, `.queued-test-scenario-id`). SessionStart emits a single deterministic preamble (feeds D2). Hooks append fields without naming contention. | 1 d | operationalises D2 | high |
| F5 | O5 | Agent-authored cache-breakpoint plan per stage. New MCP tool `cache_breakpoint_recommend(stage_id)` returning the 4 anchors (Tier 1 prefix end, Tier 2 bundle end, spec end, last executor-mutable block) per [prompt-caching-mechanics.md](prompt-caching-mechanics.md) §3. Skill preambles annotate emitted prompts with explicit breakpoint hints — Anthropic no longer has to guess longest-prefix. | 2 d | per-stage cache-write reuse | medium |
| F6 | O6 | Skills-index navigator MCP tool `skill_for_task(keywords, lifecycle_stage)` returning best-matching skill + URL + (optionally) first-phase body. Replaces the pattern-match-yourself rule today (`ia/skills/README.md` is 150 lines). | 1 d | single round-trip replaces 2–3 file reads per routing | high |
| F7 | O7 | Track Anthropic `defer_loading: true` roll-out. Antidote to B1 when two-server split is not viable. Tracking issue only — no code change until Claude Code harness confirms support semantics. | tracking only | gated on harness support | — |

## Cross-cutting themes

Four themes cut across multiple items above. Each wants a design principle rather than an item-by-item fix:

1. **Authority-chain single source of truth.** Every duplicated fact (lifecycle taxonomy A1, caveman rule A2, slash-command Mission block C1, output-style verification-report E3) is an authority-chain violation. Design principle: declare one authoritative file per topic; all other surfaces link — never restate. Mirrors the `authority chain` clause already in `prompt-caching-mechanics.md` (this doc ≻ skill preambles ≻ agent bodies).
2. **Cache-prefix stability as a first-class constraint.** Volatile bytes at message prefix (D2 banner, implicit compact behaviour D4, tool descriptor churn B1/B2) all destabilise the 5m/1h cache window. Design principle: prefix positions 0..K are deterministic; variable content lives further down the stream or behind `cache_control` breakpoints per [`prompt-caching-mechanics.md`](prompt-caching-mechanics.md) §F5.
3. **Progressive disclosure over exhaustive descriptors.** `spec_outline` (B5), `list_rules` (B5), `unity_bridge_command` (B9), `ia/skills/README.md` (F6), `.claude/output-styles/verification-report.md` (E3) all present "everything" when "top-level" would answer ~80% of queries. Design principle: default response = navigation summary; opt-in `depth=` / `include=` / `expand=` for full payload. Matches 2026 guidance (buildtolaunch.substack.com ~15k tokens/session saved from progressive disclosure) and the Anthropic `defer_loading` model (F7).
4. **Hook budget = zero shell forks per tool call.** Every hook currently forks `python3` or `bash` per tool-call event (D3) or prints noise per Stop (D1). Design principle: hooks run in pure-shell primary path; heavy-weight interpreters are fallback only; hooks with no load-bearing content get deleted. Compounds to seconds of wall time saved per session.

## Approaches surveyed

### Approach A — Big-bang umbrella (all themes, one master plan)

Single `ia/projects/session-token-latency-master-plan.md` orchestrator with 6 Steps (Theme A through Theme F). Each Step = one design intent, fully decomposed into Stages + Tasks before first `/stage-file`. Long-lived branch; lockstep refactor; coordinated CI gating.

**Pros.** One review surface; cross-theme conflicts (e.g. preamble de-dupe A2 touching the same files as dispatch flattening C1) caught early in a single plan; all per-surface policy (authority chain, cache-prefix stability, progressive disclosure, zero-shell-fork hook budget) declared once.

**Cons.** Blocks the lifecycle-refactor completion path if authored pre-M8 (FREEZE). Large coordinated cut with moderate rebase cost. Cross-theme work concentrates blast radius on ~40 surfaces at once.

**Effort.** ~3–4 weeks wall-clock across 6 Steps; ~20–25 Tasks total.

### Approach B — Theme-by-theme sibling orchestrators

Six separate master plans, one per theme: `session-ambient-collapse-master-plan.md`, `mcp-surface-pruning-master-plan.md`, `dispatch-flattening-master-plan.md`, `hook-hygiene-master-plan.md`, `repo-memory-hygiene-master-plan.md`, `rev4-larger-bets-master-plan.md`. Priority-ordered; ships independently.

**Pros.** Each shippable in isolation; lower per-orchestrator cognitive cost; small-risk themes (D, E) can land fast while bigger ones (B, F) wait on prerequisites; theme-F larger bets can be deferred indefinitely.

**Cons.** Authority-chain cross-theme policy (e.g. "single source of truth for lifecycle taxonomy") risks re-statement across themes. Higher `master-plan-extend` volume if later items bleed across original theme boundaries. `/release-rollout` pattern fits poorly (no umbrella tracker).

**Effort.** Similar total (~4 weeks) but staggered wall-clock; first ship in ~3 days (Theme D + E quick wins).

### Approach C — Quick-wins-only drop-ins (defer structural work)

Ship only items with effort ≤ 2 h and confidence ≥ high: B2, B6, B7, B9, C3, D1, D3, E1, E2 (9 items). Single `tech-quick-wins-master-plan.md` umbrella; all items as Stage-1 Tasks. Defer C1, C2, A1, A2, B1, B4, F* to a later exploration.

**Pros.** ~1 week total; zero structural risk; recovers ~5–8k tokens/session alone (B2 + E1 + E2 + B6 aggregated) plus hook-latency fixes (D3); easy to review.

**Cons.** Leaves the largest per-session recoveries (A1 + A2 + C1 + B1) on the table. Authority-chain violations persist. Rev-4 Tier 1 block still sits in volatile prefix area.

**Effort.** ~4–5 days.

### Approach D — Defer to after lifecycle-refactor M8

Park this exploration until lifecycle-refactor master-plan M8 sign-off. Re-audit at that point (the cognitive split may itself collapse ~half the identified duplication: new plan-review stage, Sonnet-enrichment demotion, inline Opus audit all touch the same surfaces enumerated here). Then run `/design-explore` against the post-refactor repo state and re-select A / B / C.

**Pros.** No wasted work if cognitive-split refactor implicitly resolves ≥50% of items (A1, A2, C1, C2 are plausible self-resolutions via the new skill structure). Avoids merge conflict with lifecycle-refactor branches.

**Cons.** Leaves 20–30k token/session drag in place for ~2–3 more weeks (estimated M8 ETA). Some items (B2, D1, E1, E2) are lossless drop-ins with zero interaction with the refactor — deferring them is pure waste.

**Effort.** 0 d now; unknown later.

## Recommendation *(tentative — user confirms during Phase 2 gate)*

**Approach B — theme-by-theme sibling orchestrators** with one concession borrowed from Approach C: a **Theme 0 "quick-wins"** master plan authored FIRST containing all ≤ 2 h / high-confidence drop-ins that don't interact with the lifecycle refactor (~~B2 alias drop (shipped)~~, B6 doc drift [after MCP T9.4], B7 DEBUG_MCP flag, B9 descriptor-prose lint, C3 skill-seed lint, D1 stop-hook delete, D3 pure-shell JSON, E1 repo-root blobs, E2 memory-path-drift, E3 output-style trim). Recovers ~4–7k tokens + 30–60 ms × N tool calls in ≤ 1 week without touching cognitive-split work (B2's ~800-token recovery already banked via TECH-426).

Rationale:

- Theme B's tool descriptor work partially interlocks with [`mcp-lifecycle-tools-opus-4-7-audit-exploration.md`](mcp-lifecycle-tools-opus-4-7-audit-exploration.md) Design Expansion (envelope + alias drop) — sibling ordering lets the two explorations coordinate Step sequencing without merging scope.
- Theme A's ambient collapse + Theme C's dispatch flattening touch overlapping surfaces (CLAUDE.md §3, `agent-lifecycle.md`, slash-commands) and SHOULD NOT run concurrently. Sibling plans make the dependency explicit via `depends_on:` links.
- Theme F's larger bets (O1 planner snapshot, O2 tool-usage log, O5 cache-breakpoint plan) benefit from the locked rev-4 Tier 1/Tier 2 design landing first. Naturally defer to Step 6 / after lifecycle M8.
- Approach A (single umbrella) loses too much per-theme iteration speed. Approach C (quick-wins only) leaves the largest recoveries on the table. Approach D wastes recoverable context for 2–3 weeks.

Tentative theme-to-Step sequencing (post-M8):

1. Theme 0 (quick wins) — ~1 week, ships during late lifecycle-refactor if the FREEZE lifts for this subset.
2. Theme A (ambient collapse) — ~3–4 days; single-source-truth authority chain.
3. Theme C (dispatch flattening) — ~1 week; depends on A (shared surfaces).
4. Theme B (MCP surface pruning) — ~1–2 weeks; coordinates with sibling composite-bundle plan.
5. Theme D (hook hygiene) — ~3–4 days; independent.
6. Theme E (repo hygiene) — ~1–2 days; can be folded into any Theme if scope permits.
7. Theme F (larger bets) — deferred umbrella; re-explore post-lifecycle-M8 + post-Theme-B.

## Open questions

1. **Theme 0 vs FREEZE interaction.** Can the quick-wins master plan run DURING lifecycle-refactor (FREEZE active) given that none of its items touch master-plan authorship flow? Or does the FREEZE's "no new master plans outside `lifecycle-refactor-master-plan.md`" clause apply unconditionally?
2. ~~**Theme B ↔ MCP reshape sibling ordering.**~~ **Resolved (2026-04-19).** MCP plan Stage 5 T5.1 TECH-426 Done absorbed B2 entirely; row struck through + §Sibling master plan coordination documents it. Remaining Theme B items (B1, B3–B9) independent of MCP plan Stages 6–16, except B6 which stacks on Stage 9 T9.4 (noted in B6 row).
3. **Theme F3 (harness-level caveman enforcement) feasibility.** Needs Claude Code harness support for `PreCompletion` hook + frontmatter-parsed output-style. Worth floating to Anthropic (dogfooding journal) or park until harness capability confirmed?
4. **Theme A1 scope split.** Does CLAUDE.md §3 Key files collapse into AGENTS.md §2, or into a new `docs/repo-surfaces.md`? Audit suggests the former; but AGENTS.md is not `@`-imported — agents load it on demand only. Authority chain needs a decision: always-loaded vs on-demand for the file inventory.
5. **Theme C1 backward compatibility.** Flattening slash-command bodies to "parameters only, no mission statement" changes the human-readable payload of `.claude/commands/*.md`. Humans paste slash commands at the CLI and occasionally read the body to debug. Do we preserve a "What this does" block at the top (10–20 lines) for humans, or drop entirely and rely on `/help` + subagent body?
6. **Theme F1 (planner context snapshot) ↔ rev-4 Tier 2 bundle.** Rev-4 design already assembles Tier 2 bundles at runtime via `domain-context-load`. Does F1 materialise that bundle as a committed file, or as an `ia/state/`-written artifact that is deliberately gitignored (single-branch-state)? Tradeoff: diffability vs repo-state churn.
7. **Theme B4 (parse cache + dist)** — `tsx`-on-source in `.mcp.json` is convenient for iteration. Keep it in developer mode (env flag) + production dist otherwise, or switch unconditionally?
8. **Theme D4 (PreCompact hook) payload** — what belongs in `.claude/last-compact-summary.md`? Current task id + active stage + last 3 tool calls? User-gate: agent re-orientation UX after compact.
9. **Theme F5 (cache-breakpoint plan MCP tool) dependency on `prompt-caching-mechanics.md` §3.** §3 currently names Tier 1 + Tier 2 anchors. F5 proposes adding Tier 3 (spec end) + Tier 4 (last executor-mutable block). Does the 4-anchor cap (F5 fact from [`prompt-caching-mechanics.md`](prompt-caching-mechanics.md)) accommodate this recipe, or must we collapse two anchors?
10. **Quantified recap verification.** Audit quoted ~20–30k tokens/session + ~1 s wall time saved across C + M items (~4–6 days effort). This is a rough estimate; should Theme 0 include a **baseline measurement** Step (record current ambient tokens via `claude -p '.' --model sonnet-4.6 --show-cost` + repeat post-Theme-0) to validate vs projected?
11. **F1 diffability angle (demoted from primary proposal).** Lifecycle-refactor Stage 10 T10.3 emits the Tier 2 bundle as a runtime cacheable block (no filesystem artifact). The F1 side-benefit — "bundles become diff-able across Stages" — is lost in that model. Is Stage-to-Stage bundle diff valuable enough (planner context drift detection; Opus context-replay during incident forensics) to ALSO write an `ia/state/{stage-id}-context-bundle.md` artifact alongside T10.3's runtime block, or is runtime-only sufficient? Defer until Stage 10 T10.8 P1 savings replay reveals whether debugging tooling wants bundle history.

## Proposed next step

Once lifecycle-refactor M8 sign-off lands (or, if FREEZE allows, immediately for Theme 0):

```
claude-personal "/design-explore docs/session-token-latency-audit-exploration.md"
```

Run Phase 0.5 interview covering open questions §1–§10 above. Expand `## Approaches` into Phase 1 comparison matrix; record Phase 2 selection. Then:

```
claude-personal "/master-plan-new docs/session-token-latency-audit-exploration.md"
```

to seed the orchestrator (`ia/projects/session-token-latency-master-plan.md` — or the sibling-set preferred by Phase 2 selection). Fully decomposed Step → Stage → Phase → Task at author time per `master-plan-new` contract.

Theme-0 early-escape path (if user approves running quick-wins during FREEZE): `/project-new` per item for B6 / B7 / B9 / C3 / D1 / D3 / E1 / E2 / E3 individually (B2 already shipped via TECH-426), filed under a parent `tech-quick-wins-master-plan.md` authored post-M8. Each is a standalone issue (no master-plan coupling) so `/project-new` is sufficient. B6 requires MCP plan Stage 9 T9.4 landed first.

## Provenance

Source audit is now persisted in-repo at [`docs/ai-mechanics-audit-2026-04-19.md`](ai-mechanics-audit-2026-04-19.md). Original external file (`/Users/javier/teselagen/lims-2/TEMP-territory-developer-ai-audit-2026-04-19.md`) remains disposable and is not the source-of-truth. Audit item ids (C1–C5, M1–M8, m1–m10, O1–O7) preserved in the Findings table for traceability; downstream `/design-explore` and `/master-plan-new` passes should retain these ids in Step / Stage / Task titles (`Theme-B2: drop rejected-alias descriptors (audit C2)`).

Sources cited by audit (retained for `/design-explore` Phase 1 evidence):

- [Anthropic — Tool Definition Bloat fix (deferred tool loading)](https://medium.com/@DebaA/anthropic-just-shipped-the-fix-for-tool-definition-bloat-77464c8dbec9)
- [Anthropic Prompt Caching 2026: Cost, TTL, Latency Planning](https://aicheckerhub.com/anthropic-prompt-caching-2026-cost-latency-guide)
- [Anthropic Prompt Caching — 4 breakpoint limit + longest-matching-prefix](https://blog.illusioncloud.biz/2026/01/13/prompt-caching-anthropic-cache-breakpoints/)
- [Claude Code Token Optimization: Full System Guide (2026)](https://buildtolaunch.substack.com/p/claude-code-token-optimization)
- [Best Practices for Claude Code](https://code.claude.com/docs/en/best-practices)
- [Claude API Prompt Caching docs](https://platform.claude.com/docs/en/build-with-claude/prompt-caching)

## Tooling Lessons

Lessons harvested from Stage 1.1 baseline-telemetry tooling (TECH-510, TECH-511):

- **macOS BSD `date` lacks `%3N`** — epoch-ms capture in shell scripts must use a portability fallback chain (`python3 -c 'time.time()*1000'` → `perl Time::HiRes` → `s * 1000`). Raw `date -u +%s%3N` emits literal `N` on BSD and compounding `000` suffix on GNU — both wrong. Any future shell-based ms-timestamp capture under `tools/scripts/` inherits this fallback.
- **Prefer Node `readline` streaming over `readFileSync` for JSONL validators** — streams keep heap flat on large captures (>100k rows) and give natural file:line diagnostics on JSON.parse failures. Default choice for any future `tools/scripts/validate-*.mjs` consuming line-delimited data.

## Design Expansion

### Chosen Approach

**Approach B — theme-by-theme sibling orchestrators**, narrowed this round to **Theme 0 subset shipped as 5 standalone `/project-new` issues** during FREEZE. No orchestrator authored. Ship order locked: **B7 → D1 → E1 → E2 → D3**.

Selected per Phase 0.5 interview:

- Q1 FREEZE interaction → option (b): standalone `/project-new` per item, no master plan
- Q2 ship order → B7 → D1 → E1 → E2 → D3
- Q3 baseline measurement → skip, trust audit estimate
- Q4 A1 refined scope → content-class split (invariants → `ia/rules/invariants.md`, inventory → `AGENTS.md §2–3`) but **deferred to a later issue**, not in Theme 0 this round

Approaches A (single umbrella), C (batch into 1 mega-issue), D (defer until post-M8) rejected: A violates FREEZE, C blocks 4 wins on 1 review, D leaves recoverable context on table for 2–3 weeks.

### Architecture

```mermaid
graph TD
    subgraph "Ship order (FREEZE-safe, standalone /project-new per item)"
        B7[B7: DEBUG_MCP_COMPUTE=1<br>.mcp.json env add<br>~5 min]
        D1[D1: delete verification-reminder.sh<br>.claude/settings.json + script unlink<br>~5 min]
        E1[E1: gitignore Bury_*.json + mono_crash.*<br>.gitignore + git rm --cached<br>~15 min]
        E2[E2: banner memory-path fix<br>session-start-prewarm.sh<br>~15 min]
        D3[D3: pure-shell JSON<br>bash-denylist.sh + cs-edit-reminder.sh<br>~1 h]
    end

    start[/project-new per item/] --> B7
    B7 --> D1 --> E1 --> E2 --> D3
    D3 --> done[closeout → archive TECH-{id}.yaml]
```

Entry: `claude-personal "/project-new {slug}"` per item. Exit: `/closeout {TECH-id}` per item.

No cross-item data flow. Five independent patches. Per item: edit file(s) → `npm run validate:all` → commit.

### Subsystem Impact

| Item | Touched files | Runtime C# | Specs | Invariants |
|---|---|---|---|---|
| B7 | `.mcp.json` | no | none | none |
| D1 | `.claude/settings.json`, `tools/scripts/claude-hooks/verification-reminder.sh` (delete) | no | `docs/agent-led-verification-policy.md` (cross-ref only, no edit) | none |
| E1 | `.gitignore`, tracked `Bury_*.json` + `mono_crash.*` (untrack via `git rm --cached`) | no | none | none |
| E2 | `tools/scripts/claude-hooks/session-start-prewarm.sh` | no | none | none |
| D3 | `tools/scripts/claude-hooks/bash-denylist.sh`, `cs-edit-reminder.sh` | no | `CLAUDE.md §4 Hooks` (behavior unchanged; may update dev-env prereq note for `jq`) | hook denylist security boundary must remain identical (exit 2 on all current deny patterns) |

Zero runtime C# touched. Zero MCP reference spec sections apply. `router_for_task` dry (tooling domain); `glossary_discover` dry (no hook/MCP domain terms). `invariants_summary` skipped per tool-recipe (tooling/pipeline only, no runtime C#).

### Implementation Points

**B7 — DEBUG_MCP_COMPUTE flag flip (~5 min):**

- [ ] open `.mcp.json`, locate `territory-ia` server block
- [ ] add/merge `"env": { "DEBUG_MCP_COMPUTE": "1" }` — do NOT overwrite existing env block
- [ ] verify stderr emission on MCP cold start (fresh session)
- [ ] confirm stdio unaffected (stderr-only timing output)
- [ ] commit `.mcp.json`

**D1 — Stop-hook delete (~5 min):**

- [ ] remove `.claude/settings.json` `hooks.Stop` array entry for `verification-reminder.sh`
- [ ] `git rm tools/scripts/claude-hooks/verification-reminder.sh`
- [ ] grep `.claude/`, `ia/`, `docs/` for residual references to script name; clean drift (NB-3)
- [ ] commit

**E1 — Repo-root gitignore (~15 min):**

- [ ] append to `.gitignore`: `Bury_*.json`, `mono_crash.*`
- [ ] `git rm --cached` currently tracked matches (working tree retained)
- [ ] `git status` confirms `.gitignore` modified + files deleted from index only
- [ ] commit

**E2 — SessionStart banner memory-path fix (~15 min):**

- [ ] read `tools/scripts/claude-hooks/session-start-prewarm.sh`; locate literal `memory:` line (NB-1 — verify exact wording, do not assume)
- [ ] correct path to `~/.claude-personal/projects/-Users-javier-bacayo-studio-territory-developer/memory/MEMORY.md`
- [ ] fresh-session test: banner text matches actual path
- [ ] commit

**D3 — Pure-shell JSON conversion (~1 h):**

- [ ] audit `bash-denylist.sh` — replace `python3` JSON parse block (lines ~40–50) with `jq` primary + sed fallback
- [ ] `jq` as NEW primary (resolves BL-2 — handles escaped quotes natively)
- [ ] sed fallback retained with conservative-deny behavior when ambiguous
- [ ] drop `python3` branch entirely
- [ ] repeat in `cs-edit-reminder.sh` (extraction of `tool_input.file_path`)
- [ ] document `jq` as dev-env prereq in `CLAUDE.md §4 Hooks`
- [ ] add test fixture: stdin JSON with denylisted command → exit 2 (NB-4)
- [ ] benchmark cold vs warm invocation — expect 20–60 ms saved per call
- [ ] commit

**Deferred / out of scope (this round):**

- Theme 0 remaining items (B6 blocked on MCP Stage 9 T9.4, B9 descriptor lint, C3 skill-seed lint, E3 output-style trim) — file as separate Theme 0 round 2 issues post-T9.4 land
- Theme A (A1 refined ambient collapse, A2 caveman de-dupe, A3 1M revisit, A4 MEMORY promote) — blocked on lifecycle-refactor Stage 10 T10.2
- Theme B structural (B1 server split, B3 allowlist, B4 parse cache, B8 yaml-first)
- Theme C (C1 dispatch flatten, C2 /ship collapse) — blocked on T10.2 + T10.4
- Theme D (D2 banner → cacheable preamble, D4 PreCompact hook)
- Theme F (all larger bets)
- Baseline measurement (per Q3)

### Examples

**B7 — `.mcp.json` before/after:**

Before:
```json
{
  "mcpServers": {
    "territory-ia": {
      "command": "npx",
      "args": ["-y", "tsx", "tools/mcp-ia-server/src/index.ts"]
    }
  }
}
```

After:
```json
{
  "mcpServers": {
    "territory-ia": {
      "command": "npx",
      "args": ["-y", "tsx", "tools/mcp-ia-server/src/index.ts"],
      "env": { "DEBUG_MCP_COMPUTE": "1" }
    }
  }
}
```

Edge case: existing `env` block with `NODE_ENV` → merge, don't overwrite. Grep first with `jq '.mcpServers["territory-ia"].env' .mcp.json`.

**D3 — hook JSON extraction before/after:**

Before (`bash-denylist.sh` lines ~40–53, python3 primary):
```bash
if command -v python3 >/dev/null 2>&1; then
  command_str="$(printf '%s' "$input" | python3 -c '
import json, sys
try:
    data = json.loads(sys.stdin.read() or "{}")
except Exception:
    print("")
    sys.exit(0)
ti = data.get("tool_input") or {}
print(ti.get("command", "") if isinstance(ti, dict) else "")
')"
else
  command_str="$(printf '%s' "$input" | sed -n 's/.*"command"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p' | head -1)"
fi
```

After (jq primary, sed fallback, no python3):
```bash
if command -v jq >/dev/null 2>&1; then
  command_str="$(printf '%s' "$input" | jq -r '.tool_input.command // ""')"
else
  command_str="$(printf '%s' "$input" | sed -n 's/.*"command"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p' | head -1)"
fi
```

Edge cases:

- Escaped quotes in command string (`rm -rf \"some path\"`) — `jq` handles natively; sed fallback fails but conservatively returns empty → no denylist match → hook allows. Safer direction: if suspicious empty match occurs, emit stderr warning. Mitigation: document `jq` as dev-env prereq.
- Missing `tool_input` or `tool_input.command` — both branches return empty → exit 0 (allow). Correct, matches current behavior.
- Non-Bash tool payloads (shouldn't reach this hook due to matcher) — extraction returns empty → exit 0.

**E1 — gitignore git status:**

Before commit:
```
  Bury_20260307_131752.json          (tracked, in index)
  mono_crash.mem.12345.blob          (tracked, in index)
```

After `.gitignore` append + `git rm --cached`:
```
modified:   .gitignore
deleted:    Bury_20260307_131752.json   (index only; file retained in working tree)
deleted:    mono_crash.mem.12345.blob   (index only; file retained in working tree)
```

Future `Bury_*.json` drops from Unity editor auto-ignored; `ls` in fresh sessions no longer carries them.

### Review Notes

**BLOCKING — resolved inline before persist:**

- **BL-1 (D1 verification-adjacent delete during FREEZE)** — `verification-reminder.sh` is unconditional stop-hook reminder, not a gate. `/verify-loop` + CI still enforce verification policy. Deletion structurally safe.
- **BL-2 (D3 escaped-quote correctness regression)** — sed fallback cannot parse JSON-escaped quotes; dropping python3 entirely risks false negatives. Resolution: `jq` as new primary (handles escapes natively), sed as fallback with conservative-deny on ambiguity, `jq` documented as dev-env prereq in `CLAUDE.md §4`.

**NON-BLOCKING (carried forward; address during `/project-new` + `/kickoff` per item):**

- **NB-1 (E2)** — verify literal banner string in `session-start-prewarm.sh` before patching; do not assume wording.
- **NB-2 (B7)** — if `DEBUG_MCP_COMPUTE=1` output voluminous, consider gating on secondary flag; audit recommends unconditional — trust audit.
- **NB-3 (D1)** — grep `.claude/` + `ia/` + `docs/` for residual script-name references after delete; clean drift.
- **NB-4 (D3)** — add test fixture (denylisted stdin JSON → exit 2) to `tools/scripts/claude-hooks/` since hook currently has no unit test.
- **NB-5 (ordering)** — B7 → D1 → E1 → E2 → D3 order is optimal (low-risk first, highest-risk last); keep.

### Expansion metadata

- **Date:** 2026-04-19
- **Model:** Opus 4.7
- **Approach selected:** B (theme-by-theme sibling orchestrators), narrowed to Theme 0 subset via 5 standalone `/project-new` issues during FREEZE
- **Ship order:** B7 → D1 → E1 → E2 → D3
- **Blocking items resolved:** 2 (BL-1 D1 FREEZE-safe delete, BL-2 D3 `jq` primary + sed fallback)
- **Interview questions used:** 4 of 5 (Q5 skipped — remaining open Qs out of Theme 0 scope)

---

## Design Expansion — Post-M8 Authoring Shape

> **Context:** second `/design-explore` pass against this exploration, scoped to the **post-M8 / post-Theme-0 round-1 remainder** — what to author after lifecycle-refactor M8 ships + after the 5 Theme-0 standalone issues (B7 / D1 / E1 / E2 / D3) close. Distinct from the first Design Expansion above, which only covered the FREEZE-window quick-wins subset. Interview answers (4) lock the bundle + cardinality + baseline policy + milestone shape.

### Chosen Approach

**Two-pass authoring with bundled Stage 1.** Two orchestrators authored in sequence:

1. **NEW orchestrator** `ia/projects/session-token-latency-master-plan.md` — covers Themes A (ambient collapse), C (dispatch flattening), D (hook hygiene remainder), E (repo hygiene remainder), F (larger bets), AND a single bundled **Stage 1 = B1 + B3 + B7-baseline-harness**. Theme B's three independent items (B1 server-split, B3 per-agent allowlist audit, B7 baseline-telemetry harness establishing the pre-Theme-0-r2 measurement floor) collapse into one Stage 1 milestone with breadth-first cross-cutting work + per-theme commit boundaries + one post-stage telemetry sweep.
2. **EXTENSION via `/master-plan-extend`** against `ia/projects/mcp-lifecycle-tools-opus-4-7-audit-master-plan.md` — Theme B's MCP-surface remainder (B4 parse cache + dist build, B5 progressive disclosure defaults, B6 doc drift CI, B8 yaml-first parser audit, B9 descriptor-prose lint) folded into the existing MCP plan as a new Stage block (Stage 17 or next available). Rationale: MCP surface is the MCP plan's domain — extending the existing orchestrator preserves single-source-of-truth for MCP descriptor work + leverages locked decisions already in that plan (envelope unification, alias drop, composite bundles).

Selected per Phase 0.5 interview:

- **Q1** authoring shape → option (a) two-pass: separate new orchestrator + `/master-plan-extend` for Theme B MCP-surface fold.
- **Q2** Stage 1 bundle → B1 + B3 + B7 (server split + allowlist audit + baseline harness as one milestone, breadth-first).
- **Q3** baseline measurement → blocking baseline at Stage 0 / Stage 1 entry: aggregate p50/p95/p99 token + latency, 1–2 day spike, **no per-theme attribution at Stage 0** (per-theme attribution deferred to post-stage sweep).
- **Q4** milestone shape → single Stage 1 (not three sub-stages), breadth-first across B1/B3/B7, per-theme commit boundaries inside the Stage, **one** post-stage telemetry sweep (not three).

Approaches rejected: single big-bang umbrella (loses MCP-plan locking), six sibling orchestrators (over-fragments authority chain), defer-everything-to-post-Theme-B (leaves 20–30k tokens/session on table indefinitely).

### Phase 1 Comparison Matrix

| Approach | Constraint fit | Effort | Output control | Maintainability | Dependencies / risk |
|---|---|---|---|---|---|
| **Bundle B1+B3+B7 — single Stage 1, two-pass authoring** *(selected)* | high — respects orchestrator-vs-spec boundary (MCP work in MCP plan); B1 + B3 + B7 are genuinely independent so bundling has no false-coupling cost; baseline-first answers Q3 | medium — ~1 week Stage 1 + extension authoring overhead | high — one Stage scope, per-theme commit boundaries, one telemetry sweep at end | high — `master-plan-extend` is locked-in mechanism for adding MCP work; no new umbrella to maintain | low — B1 server split well-scoped; B3 agent-frontmatter audit zero-runtime; B7 harness reuses cost telemetry already in MCP DEBUG_MCP path |
| B1-only Stage 1 | medium — biggest-leverage item alone | low (~3–4 d) | high | high | medium — ships savings fast but leaves B3 + B7 ungrouped; later Stages need re-justification |
| B3-only Stage 1 | low — B3 is 2 h work, undersized for Stage 1 milestone | very low (~half day) | high | medium — wastes Stage 1 slot on small win | low |
| B7-only Stage 1 | medium — baseline-first matches Q3 but skips B1's largest leverage | low (~1–2 d) | high | high | low — pure observability; no behavior change |
| Theme 0 round-2 superset Stage 1 (B6 + B9 + C3 + E3 + B1 + B3 + B7) | low — over-fills Stage 1; B6/B9/C3/E3 are sub-2h drop-ins inappropriate for milestone bundling | high (~2 weeks) | low — heterogeneous scope blurs commit boundaries | low — too many surfaces edited concurrently | medium — B6 still gated on MCP T9.4 sequencing |

Winner = bundle B1+B3+B7 single Stage 1 (Q2 + Q4 lock).

### Architecture

```mermaid
graph TD
    subgraph "Pass 1 — NEW orchestrator (ia/projects/session-token-latency-master-plan.md)"
        S0[Stage 0: baseline harness scaffold<br>aggregate p50/p95/p99 token + latency<br>1-2 d, no per-theme attribution]
        S1[Stage 1: BUNDLED B1 + B3 + B7-extended<br>breadth-first, per-theme commits<br>one post-stage telemetry sweep]
        S2[Stage 2+: Theme A ambient collapse<br>post-T10.2 dependency]
        S3[Stage N: Theme C dispatch flattening<br>post-T10.2 + T10.4 dependency]
        S4[Stage N+1: Theme D remainder D2 + D4<br>independent]
        S5[Stage N+2: Theme E remainder E3<br>independent]
        S6[Stage N+3: Theme F larger bets<br>F2/F3/F4/F5/F6/F7]
    end

    subgraph "Pass 2 — EXTEND mcp-lifecycle plan via /master-plan-extend"
        EXT[Stage 17 MCP surface fold<br>B4 parse cache + dist<br>B5 progressive disclosure<br>B6 doc drift CI post T9.4<br>B8 yaml-first audit<br>B9 descriptor-prose lint]
    end

    explore[docs/session-token-latency-audit-exploration.md<br>this Design Expansion]
    explore --> mp_new[/master-plan-new this doc/]
    explore --> mp_ext[/master-plan-extend mcp-lifecycle-master-plan this doc/]
    mp_new --> S0
    S0 --> S1
    S1 --> sweep[post-Stage 1 telemetry sweep<br>per-theme attribution emerges here]
    sweep --> S2
    S2 --> S3 --> S4 --> S5 --> S6
    mp_ext --> EXT
    EXT -.coordinates with.-> S1
```

**Entry points:**

- Pass 1: `claude-personal "/master-plan-new docs/session-token-latency-audit-exploration.md"` — seeds new orchestrator covering A / C / D-rest / E-rest / F themes + Stage 1 bundle.
- Pass 2: `claude-personal "/master-plan-extend ia/projects/mcp-lifecycle-tools-opus-4-7-audit-master-plan.md docs/session-token-latency-audit-exploration.md"` — appends Theme B MCP-surface Stage to existing MCP plan.

**Exit points:**

- Pass 1 exit: full decomposition of every Step at author time per `master-plan-new` cardinality gate (≥2 Tasks per Stage); BACKLOG row creation deferred to per-Stage `/stage-file`.
- Pass 2 exit: append-only — never rewrites existing MCP-plan Stages 1–16; new Stage authored fully decomposed.

**Cross-orchestrator coordination:** Stage 1 (Pass 1) AND Pass 2 Stage may both touch `tools/mcp-ia-server/` source. Coordination = explicit `depends_on:` link in Pass 2 Stage frontmatter pointing to Pass 1 Stage 1 (B1 server-split decision must land before Pass 2 B4 dist build, so build target is known).

### Subsystem Impact

| Subsystem / surface | Touched files (Stage 1 bundle) | Runtime C# | Specs touched | Invariants flagged | Breaking vs additive | Mitigation |
|---|---|---|---|---|---|---|
| MCP server packaging (B1) | `.mcp.json`, `tools/mcp-ia-server/src/index.ts` (split into `index-ia.ts` + `index-bridge.ts`), new `tools/mcp-ia-server/src/server-bridge.ts`, possibly new `package.json` server entries | no | none (descriptors are runtime config; not under `ia/specs/`) | none — invariants 1–13 are runtime C# / IA-authoring; MCP packaging untouched | additive (new bridge server entry + opt-in flag); existing single-server path retained behind feature flag during migration | feature flag `MCP_SPLIT_SERVERS=1` default off Stage 1; flip default in follow-up Stage after stability window |
| Subagent frontmatter (B3) | `.claude/agents/verifier.md`, `.claude/agents/spec-implementer.md`, `.claude/agents/stage-decompose.md`, `.claude/agents/project-new-*.md`, `.claude/agents/design-explore.md`, `.claude/agents/test-mode-loop.md` (7 agents disjoint from lifecycle-refactor T10.4 set) | no | none | none — frontmatter `tools:` allowlist is harness config | additive — narrowing tools never breaks an agent that only used a subset; revert-safe via `tools:` removal | per-agent dry-run dispatch test; CI lint comparing pre/post tool set per agent |
| Telemetry harness (B7-extended) | `tools/scripts/agent-telemetry/` (NEW dir), `.claude/settings.json` PostToolUse hook entry, `.mcp.json` env extends `DEBUG_MCP_COMPUTE=1` already shipped per Theme-0-r1 | no | none | none — observability layer | purely additive | hook writes to `.claude/telemetry/{session-id}.jsonl` gitignored; no existing surface modified |
| MCP surface descriptors (Pass 2 B4/B5/B6/B8/B9) — out of Pass 1 scope but cross-coordinates | `tools/mcp-ia-server/src/tools/*.ts` (descriptor `.describe()` strings), `tools/mcp-ia-server/dist/` (NEW build target), `docs/mcp-ia-server.md` (CI lint target) | no | `docs/mcp-ia-server.md` regenerated by lint | none | mostly additive; B5 progressive-disclosure defaults are **breaking** for callers expecting full payload (mitigated by opt-in `expand=true` param) | breaking change documented in MCP plan CHANGELOG entry; caller sweep in same PR per MCP plan locked decision (envelope-cut precedent) |

**Invariants reviewed via tool-recipe analog:** none of the 13 invariants apply (all 13 cover runtime C# / `HeightMap` / roads / water / GridManager / project-spec authoring; nothing in this bundle touches those). `invariants_summary` skipped per design-explore tool-recipe rule (tooling/pipeline only).

**Glossary discover signal:** dry — closest matches (`Stage`, `Gate`, `Ship-stage dispatcher`) are lifecycle terms already used correctly in the doc; no new domain terms introduced.

**Router signal:** dry — `router_for_task` rejected `subagent body authoring caveman preamble cache` (no row in Cursor agent-router table for IA-meta domains). Confirms this work is below the spec-routed surface.

### Implementation Points

**Pass 1 — NEW orchestrator authoring (post-M8, post-Theme-0-round-1 close):**

- [ ] confirm M8 sign-off + FREEZE lifted (`ia/state/lifecycle-refactor-migration.json` M8 = done)
- [ ] confirm Theme-0-round-1 closed (B7 / D1 / E1 / E2 / D3 all `Done (archived)`)
- [ ] run `claude-personal "/master-plan-new docs/session-token-latency-audit-exploration.md"`
- [ ] verify `master-plan-new` produces fully-decomposed Steps for Themes A, C, D-remainder, E-remainder, F + Stage 0 baseline + Stage 1 bundle
- [ ] confirm Stage 1 task table contains exactly 3 task rows (one each for B1 / B3 / B7-extended) plus 1 sweep task — total 4 — meets `≥2 per Stage` cardinality
- [ ] no BACKLOG rows created (per `master-plan-new` contract)

**Stage 0 — baseline harness (1–2 d, blocking):**

- [ ] author `tools/scripts/agent-telemetry/baseline-collect.sh` capturing per-session: total input tokens, cache-read tokens, cache-write tokens, MCP cold-start ms, hook fork count + ms
- [ ] aggregate to `.claude/telemetry/baseline.jsonl` over ≥10 representative sessions (mix of `/implement`, `/ship`, `/stage-file`)
- [ ] compute p50 / p95 / p99 per metric; commit `.claude/telemetry/baseline-summary.json` (gitignored aggregates; summary committed)
- [ ] no per-theme attribution at Stage 0 — single aggregate floor only (per Q3)
- [ ] gate Stage 1 entry on baseline persisted

**Stage 1 — bundle B1 + B3 + B7-extended (breadth-first, per-theme commits):**

- [ ] **B1 server split:** author `tools/mcp-ia-server/src/server-ia.ts` + `tools/mcp-ia-server/src/server-bridge.ts`; behind `MCP_SPLIT_SERVERS=1` flag; existing `index.ts` retained as default; commit boundary 1
- [ ] **B3 allowlist audit:** per-agent `tools:` frontmatter narrowing for `verifier`, `spec-implementer`, `stage-decompose`, `project-new-planner`, `project-new-applier`, `design-explore`, `test-mode-loop`; CI lint added; commit boundary 2
- [ ] **B7-extended harness:** PostToolUse hook writing per-tool token + ms to `.claude/telemetry/{session-id}.jsonl`; reuses Stage-0 schema; commit boundary 3
- [ ] **Post-Stage telemetry sweep (one only, per Q4):** re-run baseline collection over ≥10 sessions; produce `baseline-summary-post-stage1.json`; diff vs Stage-0 baseline; **per-theme attribution emerges here** (compare sessions with B1 server split on/off, with B3 narrowed allowlist on/off); commit sweep report; commit boundary 4

**Pass 2 — `/master-plan-extend` against MCP plan (after Pass 1 Stage 1 sweep lands):**

- [ ] confirm Pass 1 Stage 1 sweep complete + B1 server-split decision durable
- [ ] run `claude-personal "/master-plan-extend ia/projects/mcp-lifecycle-tools-opus-4-7-audit-master-plan.md docs/session-token-latency-audit-exploration.md"`
- [ ] new Stage authored covering B4 (parse cache + dist) + B5 (progressive disclosure defaults) + B6 (doc drift CI — sequencing ok since MCP plan T9.4 either landed or is upcoming) + B8 (yaml-first parser audit) + B9 (descriptor-prose ≤120-char lint)
- [ ] new Stage declares `depends_on: [Pass-1 Stage 1 task ids]` for B4 dist build
- [ ] never rewrite MCP plan Stages 1–16 (per `master-plan-extend` append-only contract)

**Deferred / out of scope (this round, this expansion block):**

- Theme A authoring (ambient collapse) — folded into NEW orchestrator at Stage ≥2; not part of Stage 1 bundle
- Theme C authoring (dispatch flattening) — folded into NEW orchestrator at later Stage; depends on T10.2 + T10.4
- Theme D2 / D4, Theme E3 — folded into NEW orchestrator at later Stages
- Theme F1 (already superseded by T10.3)
- Per-theme attribution AT Stage 0 (deferred to post-Stage-1 sweep per Q3)
- Per-theme telemetry sweeps inside Stage 1 (per Q4 = one sweep only)

### Examples

**B1 server-split — `.mcp.json` + `package.json` shape:**

Before (Theme-0-r1 state, post-B7 baseline DEBUG flag):

```json
{
  "mcpServers": {
    "territory-ia": {
      "command": "npx",
      "args": ["-y", "tsx", "tools/mcp-ia-server/src/index.ts"],
      "env": { "DEBUG_MCP_COMPUTE": "1" }
    }
  }
}
```

After Stage 1 B1 (split, flag default off):

```json
{
  "mcpServers": {
    "territory-ia": {
      "command": "npx",
      "args": ["-y", "tsx", "tools/mcp-ia-server/src/index.ts"],
      "env": { "DEBUG_MCP_COMPUTE": "1", "MCP_SPLIT_SERVERS": "0" }
    },
    "territory-ia-bridge": {
      "command": "npx",
      "args": ["-y", "tsx", "tools/mcp-ia-server/src/index-bridge.ts"],
      "env": { "DEBUG_MCP_COMPUTE": "1" },
      "loadCondition": "lifecycle_stage in [verify, implement]"
    }
  }
}
```

Edge case — caller agent that needs BOTH servers in IA-authoring session (e.g. verify-loop calls `unity_bridge_command` mid-flow): handled via per-agent `tools:` frontmatter listing both server prefixes; subagent inheritance picks up bridge server only when the agent declares it. Test fixture: `spec-implementer` dispatch with `MCP_SPLIT_SERVERS=1` + bridge prefix in `tools:` → exit 0; `design-explore` dispatch without bridge prefix + `MCP_SPLIT_SERVERS=1` → bridge tools absent from `tools/list` → expected.

**B3 per-agent allowlist — `verifier.md` frontmatter before/after:**

Before (no allowlist; full inheritance):

```yaml
---
name: verifier
description: Single-pass verification subagent — Path A + Path B + bridge preflight + dashboards.
model: sonnet
---
```

After Stage 1 B3 (narrowed):

```yaml
---
name: verifier
description: Single-pass verification subagent — Path A + Path B + bridge preflight + dashboards.
model: sonnet
tools:
  - Bash
  - Read
  - Grep
  - Glob
  - mcp__territory-ia__backlog_issue
  - mcp__territory-ia__spec_section
  - mcp__territory-ia__invariants_summary
  - mcp__territory-ia__unity_bridge_command
  - mcp__territory-ia__bridge_environment_preflight
---
```

Rationale — verifier needs read + bridge + invariants but NOT mutators (`reserve_backlog_ids`, `backlog_record_validate`, `project_spec_journal_*`). Edge case — verifier needing a tool not in allowlist mid-run: hard fail with clear error; user adds tool to frontmatter (preferred over wildcard).

**B7-extended baseline harness — `.claude/telemetry/` JSONL line shape:**

Per-tool-call line (PostToolUse hook output):

```json
{
  "ts": "2026-04-20T14:23:11.482Z",
  "session_id": "01HXYZ...",
  "tool": "mcp__territory-ia__spec_section",
  "duration_ms": 187,
  "input_tokens_estimate": 1240,
  "output_tokens_estimate": 2890,
  "cache_read_tokens": 0,
  "cache_write_tokens": 1240,
  "agent": "spec-implementer",
  "lifecycle_stage": "implement"
}
```

Aggregate (`baseline-summary-post-stage1.json`):

```json
{
  "baseline_ref": "baseline-summary.json",
  "samples": 14,
  "metrics": {
    "total_input_tokens": { "p50": 187432, "p95": 312890, "p99": 401223, "delta_vs_baseline_pct": -18.4 },
    "mcp_cold_start_ms": { "p50": 198, "p95": 412, "p99": 587, "delta_vs_baseline_pct": -86.2 },
    "hook_fork_total_ms": { "p50": 312, "p95": 891, "p99": 1402, "delta_vs_baseline_pct": -42.1 }
  },
  "per_theme_attribution": {
    "B1_server_split": { "input_token_saving_pct": 11.2, "confidence": "medium" },
    "B3_allowlist_narrowing": { "input_token_saving_pct": 4.8, "confidence": "medium" },
    "B7_harness_overhead": { "input_token_saving_pct": -0.3, "confidence": "high" }
  }
}
```

Edge case — per-theme attribution requires sessions with each theme on/off independently; if only post-everything sessions exist, attribution falls back to "aggregate only, attribution unavailable" rather than spurious causality.

**`/master-plan-extend` Pass 2 invocation shape:**

Resolved command (no placeholders):

```
claude-personal "/master-plan-extend ia/projects/mcp-lifecycle-tools-opus-4-7-audit-master-plan.md docs/session-token-latency-audit-exploration.md"
```

Pre-condition met: this Design Expansion block (and the existing one above) are both `## Design Expansion` semantic equivalents — `/master-plan-extend` Phase 0 will detect and proceed.

Edge case — `/master-plan-extend` rewriting existing MCP-plan Stage by accident: forbidden per append-only contract; new Stage authored at next available index (Stage 17 if 16 currently exists). Verify by reading MCP plan Stages count before invocation.

### Review Notes

Inline self-review performed (no nested `Plan` subagent available at this dispatch depth — design-explore subagent cannot spawn another Agent-tool subagent). Adversarial pass against own draft using same template structure.

**BLOCKING — resolved inline before persist:**

- **BL-3 (Stage 1 cardinality)** — initial draft had 3 task rows (B1 / B3 / B7) but `master-plan-new` cardinality gate is ≥2 per Stage which is satisfied. However the post-Stage telemetry sweep was floating; resolution = sweep is its own task row (4th), pinning Stage 1 at 4 tasks total + clarifying commit boundary count = 4.
- **BL-4 (Pass 2 sequencing risk vs MCP plan T9.4)** — B6 (doc drift CI) requires `docs/mcp-ia-server.md` rewrite first, owned by MCP plan Stage 9 T9.4. Pass 2 might fire before T9.4 lands. Resolution = Pass 2 Stage declares `depends_on: [MCP-plan T9.4 task id]` for the B6 sub-task only; B4 / B5 / B8 / B9 ship independently. Documented in Implementation Points.
- **BL-5 (Stage 0 baseline blocking nature)** — Q3 says "blocking baseline" but Stage 1 work could begin in parallel with measurement. Resolution = Stage 0 strictly blocks Stage 1 entry per Q3 literal reading; ≥10 representative sessions captured before Stage 1 task table flips Draft → In Progress.

**NON-BLOCKING (carried forward; address during `/master-plan-new` planning + Stage 1 author):**

- **NB-6 (B1 server-split rollout strategy)** — flag-default-off first Stage, flag-flip second Stage. Author should explicitly enumerate the flip Stage in NEW orchestrator (Stage 1.5 or Stage 2 prefix) to prevent indefinite flag persistence.
- **NB-7 (B3 allowlist drift prevention)** — once narrowed, future agents need a CI lint preventing accidental wildcard re-introduction. Worth filing as a sub-task within B3.
- **NB-8 (B7 telemetry overhead must be measured)** — the harness itself adds tokens. Per-theme attribution should explicitly show B7 overhead row to keep the measurement honest.
- **NB-9 (per-theme attribution methodology)** — sweep needs sessions with isolated factors (B1 on / off, B3 on / off independently). If natural session diversity insufficient, plan synthetic A/B sessions.
- **NB-10 (sweep timing)** — single post-Stage sweep per Q4. If B1 / B3 / B7 land in tight commit succession, sweep window may be too short to gather ≥10 sessions. Author should pad sweep window to ≥3 working days post last commit boundary.
- **NB-11 (Pass 2 Stage numbering)** — MCP plan currently has Stages 1–16 declared; `/master-plan-extend` should auto-detect next index. Verify before invocation.
- **NB-12 (Stage 0 telemetry summary commit policy)** — `.claude/telemetry/*.jsonl` raw lines gitignored; aggregate summaries committed for diffability across sweeps. Confirm `.gitignore` updated as part of Stage 0.

**SUGGESTIONS:**

- **S1** — name the new orchestrator `session-token-latency-master-plan.md` (matches doc slug); ensures `master-plan-new` slug-derivation succeeds.
- **S2** — title Stage 0 explicitly "Stage 0 — Baseline measurement (gating)" so cardinality validator does not reject 1-task baseline Stage; single-task Stage acceptable when explicitly gating per project-hierarchy carve-out (verify in author phase).
- **S3** — annotate Stage 1 task table with explicit `commit_boundary: N` notes per task so `/ship-stage` chain dispatcher honors per-theme commits.
- **S4** — when authoring Pass 2 Stage, reuse the MCP plan's locked-decisions block format for any sub-decisions emerging during B4 dist build (e.g. dist target dir, Node version pin).
- **S5** — file Theme A first-task as the natural Stage-2 leadoff in the NEW orchestrator (post-Stage 1 sweep produces real per-theme savings → most-leverage A1/A2 next).

### Expansion metadata

- **Date:** 2026-04-19
- **Model:** Opus 4.7
- **Approach selected:** two-pass authoring — NEW orchestrator (Themes A / C / D-rest / E-rest / F + Stage 1 bundle B1+B3+B7) + `/master-plan-extend` against `mcp-lifecycle-tools-opus-4-7-audit-master-plan.md` for Theme B MCP-surface remainder (B4 / B5 / B6 / B8 / B9)
- **Stage 1 bundle:** B1 (server split) + B3 (per-agent allowlist) + B7-extended (baseline harness) — single Stage, breadth-first, per-theme commit boundaries, one post-stage telemetry sweep
- **Baseline policy (Q3):** blocking Stage 0; aggregate p50/p95/p99 only; no per-theme attribution at Stage 0 (emerges from post-Stage-1 sweep)
- **Subsystems impacted:** 4 (MCP server packaging, subagent frontmatter, telemetry harness, MCP surface descriptors via Pass 2)
- **Invariants flagged:** 0 (no runtime C# / IA-authoring touch; `invariants_summary` skipped per tool recipe)
- **Blocking items resolved:** 3 (BL-3 Stage 1 cardinality + sweep task, BL-4 Pass 2 vs MCP T9.4 sequencing, BL-5 Stage 0 strict blocking)
- **Non-blocking carried:** 7 (NB-6 through NB-12)
- **Suggestions carried:** 5 (S1–S5)
- **Interview questions used:** 4 of 5 (Q5 skipped — covered by accumulated answers)
- **Subagent review:** inline self-review (no nested Agent-tool dispatch available at this depth)

