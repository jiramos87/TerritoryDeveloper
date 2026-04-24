# Session Token + Latency Remediation — Master Plan (MVP)

> **Last updated:** 2026-04-20
>
> **Status:** In Progress — Step 2 / Stage 2.2
>
> **Scope:** Token-economy and latency remediation across MCP surface pruning (B1/B3/B7 bundle), ambient context collapse (Theme A), dispatch path flattening (Theme C), hook plane remainder (Theme D), repo hygiene remainder (Theme E), and rev-4 larger bets (Theme F). Theme-0-round-1 quick-wins (B7/D1/E1/E2/D3) ship as standalone `/project-new` issues — out of this orchestrator. Theme B MCP-surface remainder (B4/B5/B6/B8/B9) delegated to `/master-plan-extend` against `ia/projects/mcp-lifecycle-tools-opus-4-7-audit-master-plan.md` — separate invocation, out of scope here.
>
> **Exploration source:** `docs/session-token-latency-audit-exploration.md` (`## Design Expansion — Post-M8 Authoring Shape` = ground truth; first `## Design Expansion` governs standalone Theme-0-r1 issues only); `docs/session-token-latency-post-mvp-extensions.md` (§4 Stage 3.3 + §5 pre-authored specs = extension source for Step 5).
>
> **Locked decisions (do not reopen in this plan):**
> - Approach B two-pass: this orchestrator covers Themes A/C/D/E/F + Stage 1 B1+B3+B7 bundle; Theme B MCP-surface remainder via `/master-plan-extend` against MCP plan.
> - Stage 1 bundle = B1 (server split) + B3 (per-agent allowlist) + B7-extended (baseline harness) — one Stage, breadth-first, per-theme commit boundaries.
> - Baseline (Stage 1.1) = blocking gate for Stage 1.2; aggregate p50/p95/p99 only at Stage 1.1; no per-theme attribution until post-Stage-1.3 sweep.
> - Post-Stage 1.3 = one telemetry sweep only (Q4 lock).
> - A1/A2/C1/C2 must run **after** lifecycle-refactor Stage 10 T10.2 + T10.4 land.
> - F1 superseded by lifecycle-refactor Stage 10 T10.3 (runtime cacheable bundle); diffability angle demoted to Open Q in exploration doc.
> - Step 5 (D5 context pack) extension: PreCompact hook shell-only (no `claude -p` subprocess); pack gitignored (session-ephemeral); size cap 300 lines; freshness gate 24 h fixed; model-backed synthesis (`/pack-context`) explicitly out of scope. Semantic placement = Stage 3.3, filed as Step 5 per skill append-only contract — human reviewer may relocate post-apply.
>
> **Hierarchy rules:** `ia/rules/project-hierarchy.md` (step > stage > phase > task). `ia/rules/orchestrator-vs-spec.md` (this doc = orchestrator, never closeable).
>
> **Read first if landing cold:**
> - `docs/session-token-latency-audit-exploration.md` — full design + architecture + examples. `## Design Expansion — Post-M8 Authoring Shape` is ground truth.
> - `docs/session-token-latency-post-mvp-extensions.md` — Stage 3.3 (D5) synthesized context pack extension source; §4 Stage block + §5 pre-authored §Plan Author content for Step 5.
> - `docs/ai-mechanics-audit-2026-04-19.md` — source audit; item ids (B1–B9, C1–C3, D1–D4, E1–E3, F1–F7) traceable here.
> - `ia/projects/lifecycle-refactor-master-plan.md` — Stage 10 T10.2 + T10.4 = pre-conditions for Step 2.
> - `ia/projects/mcp-lifecycle-tools-opus-4-7-audit-master-plan.md` — Theme B MCP-surface extension lands here; B1 server-split decision (Stage 1.2) must precede that extension's B4 dist-build task.
> - `ia/rules/project-hierarchy.md` + `ia/rules/orchestrator-vs-spec.md` — doc semantics + phase/task cardinality rule (≥2 tasks per phase).
> - `ia/rules/invariants.md` — none flagged (zero runtime C# / IA-authoring surface touched by this plan).
> - MCP: `backlog_issue {id}` per referenced id once tasks file; never full `BACKLOG.md` read.

---

## Stages

> **Tracking legend:** Step / Stage `Status:` uses enum `Draft | Skeleton | Planned | In Review | In Progress — {active child} | Final` (per `ia/rules/project-hierarchy.md`; `Skeleton` + `Planned` authored by `master-plan-new` / `stage-decompose`). Phase bullets use `- [ ]` / `- [x]`. Task tables carry a **Status** column: `_pending_` (not filed) → `Draft` → `In Review` → `In Progress` → `Done (archived)`. Markers flipped by lifecycle skills: `stage-file` → task rows gain `Issue` id + `Draft` status; `stage-file` also flips Stage header `Draft/Planned → In Progress` (R2) and plan top Status `Draft → In Progress — Step {N} / Stage {N.M}` on first task ever filed (R1); `stage-decompose` → Step header `Skeleton → Draft (tasks _pending_)` (R7); `/author` (`plan-author`) → `In Review`; `/implement` → `In Progress`; the Stage-scoped `/closeout` pair (`stage-closeout-plan` → `plan-applier` Mode `stage-closeout`) → task rows `Done (archived)` + stage `Final` + stage-level step rollup + plan top Status `→ Final` when all Steps read `Final` (R5); `master-plan-extend` → plan top Status `Final → In Progress — Step {N_new} / Stage {N_new}.1` when new Steps appended to a Final plan (R6).

---

### Stage index

- [Stage 1.1 — Baseline measurement (gating)](stage-1.1-baseline-measurement-gating.md) — _Final_
- [Stage 1.2 — MCP server split (B1)](stage-1.2-mcp-server-split-b1.md) — _Final_
- [Stage 1.3 — Allowlist narrowing + telemetry harness + post-stage sweep (B3 + B7 + sweep)](stage-1.3-allowlist-narrowing-telemetry-harness-post-stage-sweep-b3-b7.md) — _Final_
- [Stage 2.1 — Lifecycle taxonomy authority chain (A1 + A4)](stage-2.1-lifecycle-taxonomy-authority-chain-a1-a4.md) — _In Progress — Pass 2 tail (TECH-577..580 open in backlog until `/closeout`)_
- [Stage 2.2 — Preamble de-dupe + seed lint (A2 + C3)](stage-2.2-preamble-de-dupe-seed-lint-a2-c3.md) — _Draft (tasks _pending_ — not yet filed)_
- [Stage 2.3 — Slash-command dispatch flattening (C1 + C2)](stage-2.3-slash-command-dispatch-flattening-c1-c2.md) — _Draft (tasks _pending_ — not yet filed)_
- [Stage 3.1 — Session-start preamble + compact-survival (D2 + D4)](stage-3.1-session-start-preamble-compact-survival-d2-d4.md) — _Draft (tasks _pending_ — not yet filed)_
- [Stage 3.2 — Output-style surface trim (E3)](stage-3.2-output-style-surface-trim-e3.md) — _Draft (tasks _pending_ — not yet filed)_
- [Stage 4.1 — Session-level MCP memoization + unified runtime state (F2 + F4)](stage-4.1-session-level-mcp-memoization-unified-runtime-state-f2-f4.md) — _Draft (tasks _pending_ — not yet filed)_
- [Stage 4.2 — Cache-breakpoint prescriptive tooling (F5)](stage-4.2-cache-breakpoint-prescriptive-tooling-f5.md) — _Draft (tasks _pending_ — not yet filed)_
- [Stage 4.3 — Skills navigator MCP tool + harness-gated tracking (F6 + F3 + F7)](stage-4.3-skills-navigator-mcp-tool-harness-gated-tracking-f6-f3-f7.md) — _Draft (tasks _pending_ — not yet filed)_
- [Stage 5.1 — PreCompact digest + SessionStart re-injection](stage-5.1-precompact-digest-sessionstart-re-injection.md) — _In Progress — Stage 5.1 (TECH-520, TECH-521, TECH-522, TECH-523)_

## Orchestration guardrails

**Do:**

- Open one stage at a time. Next stage opens only after current stage's `/closeout` runs.
- Run `claude-personal "/stage-file ia/projects/session-token-latency-master-plan.md Stage {N}.{M}"` to materialize pending tasks → BACKLOG rows + `ia/projects/{ISSUE_ID}.md` stubs.
- Stage 1.1 gates Stage 1.2 — confirm `tools/scripts/agent-telemetry/baseline-summary.json` committed before starting Stage 1.2.
- Stage 2.x gates: confirm lifecycle-refactor Stage 10 T10.2 + T10.4 Done before filing Stage 2.1 tasks. Check `ia/projects/lifecycle-refactor-master-plan.md` Stage 10 status.
- Stage 4.2 recommendation: confirm lifecycle-refactor T10.7 Done before filing Stage 4.2 tasks (prescriptive F5 complements T10.7 prohibitive guardrail).
- Update stage / step `Status` + phase checkboxes as lifecycle skills flip them — do NOT edit by hand.
- Preserve locked decisions (see header block). Changes require explicit re-decision + sync edit to exploration doc.
- Pass 2 (`/master-plan-extend` against `mcp-lifecycle-tools-opus-4-7-audit-master-plan.md`) should be invoked after Stage 1.2 B1 server-split decision is durable; B4 dist build in the MCP extension depends on knowing the split target.

**Do not:**

- Close this orchestrator via `/closeout` — orchestrators are permanent (see `ia/rules/orchestrator-vs-spec.md`). Only the terminal step landing triggers `Status: Final`; the file stays.
- Silently promote Theme B MCP-surface items (B4/B5/B6/B8/B9) into this orchestrator — they belong in the `/master-plan-extend` pass against the MCP plan.
- Merge partial stage state — every stage must land on a green bar.
- Insert BACKLOG rows directly into this doc — only `stage-file` materializes them.
- Re-order Stages 2.x before confirming T10.2 + T10.4 landed — same agent bodies edited; churning twice is waste.
- Introduce any C# runtime / Unity bridge / product-correctness changes — all items in this plan are tooling-only.
