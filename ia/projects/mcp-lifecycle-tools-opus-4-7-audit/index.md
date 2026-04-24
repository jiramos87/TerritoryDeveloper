# MCP Lifecycle Tools — Opus 4.7 Audit — Master Plan (IA Infrastructure)

> **Last updated:** 2026-04-19
>
> **Status:** In Progress — Stage 10
>
> **Scope:** Reshape `territory-ia` MCP surface (32 tools) from 4.6-era sequential-call design to 4.7-era composite-bundle + structured-envelope architecture. Phased: quick wins → breaking envelope cut → composite bundles → mutation/authorship surface → bridge/journal lifecycle. Out of scope: backlog-yaml mutations (sibling master plan), Sonnet skill extractions (TECH-302), bridge transport rewrite, web dashboard tooling, computational-family batching.
>
> **Exploration source:**
> - `docs/mcp-lifecycle-tools-opus-4-7-audit-exploration.md` (§Design Expansion — ground truth for Stages 1–16).
> - `docs/session-token-latency-audit-exploration.md` (§Design Expansion — Post-M8 Authoring Shape, Pass 2) — extension source for Stage 17 (Theme B MCP surface remainder: B4 / B5 / B6 / B8 / B9).
>
> **Locked decisions (do not reopen in this plan):**
> - Approach B selected — phased sequencing (P1 quick wins → P2 envelope → P3 composites → P4 mutations → P5 bridge/journal → P6 graph).
> - Breaking envelope cut: no dual-mode migration; all 32 handlers rewritten in one PR; caller sweep lands in same PR.
> - Hybrid bridge ceiling: `UNITY_BRIDGE_PIPELINE_CEILING_MS` env var (default 30 000 ms).
> - Caller-agent allowlist source of truth: `tools/mcp-ia-server/src/auth/caller-allowlist.ts`.
> - Journal `content_hash` dedup: 3-step migration (nullable column → batched SHA-256 backfill → NOT NULL).
> - Composite core vs optional sub-fetch: core fail → `ok: false`; optional fail → `meta.partial` tick, `ok: true`.
> - IA-authorship server split rejected — stays in `territory-ia` MCP, guarded by `caller_agent`.
>
> **Hierarchy rules:** `ia/rules/project-hierarchy.md` (step > stage > phase > task). `ia/rules/orchestrator-vs-spec.md` (this doc = orchestrator, never closeable).
>
> **Read first if landing cold:**
> - `docs/mcp-lifecycle-tools-opus-4-7-audit-exploration.md` — full audit + design expansion + examples + review notes.
> - `docs/session-token-latency-audit-exploration.md` — Theme B cross-plan coordination (Pass 2 Stage 17 source).
> - `docs/mcp-ia-server.md` — current MCP tool catalog (pre-reshape).
> - `tools/mcp-ia-server/src/tools/` — 22 existing handler files.
> - `ia/rules/project-hierarchy.md` + `ia/rules/orchestrator-vs-spec.md` — doc semantics + phase/task cardinality (≥2 tasks per phase).
> - `ia/rules/invariants.md` — **#12** (specs under `ia/specs/` / orchestrators under `ia/projects/` — mutation tools validate path) + **#13** (monotonic id counter never hand-edited — mutation tools never touch `id:` field).
> - MCP: `backlog_issue {id}` per referenced id once tasks file; never full `BACKLOG.md` read.

---

## Stages

> **Tracking legend:** Step / Stage `Status:` uses enum `Draft | In Review | In Progress — {active child} | Final` (per `ia/rules/project-hierarchy.md`). Phase bullets use `- [ ]` / `- [x]`. Task tables carry a **Status** column: `_pending_` (not filed) → `Draft` → `In Review` → `In Progress` → `Done (archived)`. Markers flipped by lifecycle skills: `stage-file` → task rows gain `Issue` id + `Draft` status; `/author` (`plan-author`) → `In Review`; `/implement` → `In Progress`; the Stage-scoped `/closeout` pair (`stage-closeout-plan` → `plan-applier` Mode `stage-closeout`) → task rows `Done (archived)` + stage `Final` + stage-level rollup.

---

### Stage index

- [Stage 1 — Quick Wins / Glossary Bulk-Terms Extension](stage-1-glossary-bulk-terms-extension.md) — _Done (2026-04-18)_
- [Stage 2 — Quick Wins / Structured Invariants Summary](stage-2-structured-invariants-summary.md) — _Final (2026-04-18)_
- [Stage 3 — Envelope Foundation (Breaking Cut) / Envelope Infrastructure + Auth](stage-3-envelope-infrastructure-auth.md) — _Final (2026-04-18)_
- [Stage 4 — Envelope Foundation (Breaking Cut) / Rewrite 32 Tool Handlers](stage-4-rewrite-32-tool-handlers.md) — _Done_
- [Stage 5 — Envelope Foundation (Breaking Cut) / Alias Removal + Structured Prose + Batch Shape](stage-5-alias-removal-structured-prose-batch-shape.md) — _Final (2026-04-18)_
- [Stage 6 — Envelope Foundation (Breaking Cut) / Caller Sweep + Snapshot Tests + CI Gate](stage-6-caller-sweep-snapshot-tests-ci-gate.md) — _Draft (tasks _pending_ — not yet filed)_
- [Stage 7 — Composite Bundles + Graph Freshness / `issue_context_bundle` + `lifecycle_stage_context`](stage-7-issue-context-bundle-lifecycle-stage-context.md) — _Draft (tasks _pending_ — not yet filed)_
- [Stage 8 — Composite Bundles + Graph Freshness / `orchestrator_snapshot`](stage-8-orchestrator-snapshot.md) — _Draft (tasks _pending_ — not yet filed)_
- [Stage 9 — Composite Bundles + Graph Freshness / Graph Freshness + Skill Recipe Sweep](stage-9-graph-freshness-skill-recipe-sweep.md) — _Done_
- [Stage 10 — Mutations + Authorship + Bridge + Journal Lifecycle / Orchestrator + Rollout Mutations](stage-10-orchestrator-rollout-mutations.md) — _Draft (tasks _pending_ — not yet filed)_
- [Stage 11 — Mutations + Authorship + Bridge + Journal Lifecycle / IA Authorship Tools](stage-11-ia-authorship-tools.md) — _Draft (tasks _pending_ — not yet filed)_
- [Stage 12 — Mutations + Authorship + Bridge + Journal Lifecycle / Bridge Pipeline + Jobs List](stage-12-bridge-pipeline-jobs-list.md) — _Draft (tasks _pending_ — not yet filed)_
- [Stage 13 — Mutations + Authorship + Bridge + Journal Lifecycle / Journal Lifecycle](stage-13-journal-lifecycle.md) — _Draft (tasks _pending_ — not yet filed)_
- [Stage 14 — Critical Misses: Authoring Parity + Atomicity + Dry-run (post-plan review addendum) / Master-plan Authoring Tools](stage-14-master-plan-authoring-tools.md) — _Draft (tasks _pending_ — not yet filed)_
- [Stage 15 — Critical Misses: Authoring Parity + Atomicity + Dry-run (post-plan review addendum) / Transactional Batch](stage-15-transactional-batch.md) — _Draft (tasks _pending_ — not yet filed)_
- [Stage 16 — Critical Misses: Authoring Parity + Atomicity + Dry-run (post-plan review addendum) / Dry-run Preview](stage-16-dry-run-preview.md) — _Draft (tasks _pending_ — not yet filed)_
- [Stage 17 — Theme B MCP Surface Remainder (session-token-latency audit extension) / Parse Cache + Progressive Disclosure + Doc Drift + YAML-First + Descriptor Lint](stage-17-parse-cache-progressive-disclosure-doc-drift-yaml-first-desc.md) — _Done (2026-04-19)_

## Orchestration guardrails

**Do:**

- Open one stage at a time. Next stage opens only after current stage's `/closeout` pair (`stage-closeout-plan` → `plan-applier` Mode `stage-closeout`) runs.
- Run `claude-personal "/stage-file ia/projects/mcp-lifecycle-tools-opus-4-7-audit-master-plan.md Stage 1.1"` to materialize pending tasks → BACKLOG rows + `ia/projects/{ISSUE_ID}.md` stubs.
- Update stage / step `Status` + phase checkboxes as lifecycle skills flip them — do NOT edit by hand.
- Preserve locked decisions (see header block). Changes require explicit re-decision + sync edit to exploration doc.
- **Step 2 breaking cut:** land caller sweep + tool rewrite in the same PR; never split — half-state leaves skills referencing envelope while tools still return legacy shapes.
- **Invariant #12 guard:** all mutation tools (`orchestrator_task_update`, `rollout_tracker_flip`, IA-authorship tools) must validate their target file path before writing. Reject anything outside `ia/projects/` (orchestrators) or `ia/specs/` / `ia/rules/` (authorship).
- **Invariant #13 guard:** mutation tools never touch `id:` fields in YAML backlog records. Never regenerate `ia/state/id-counter.json`.

**Do not:**

- Close this orchestrator via `/closeout` — orchestrators are permanent (see `ia/rules/orchestrator-vs-spec.md`). Only the terminal Step 4 landing triggers `Status: Final`; the file stays.
- Silently promote post-MVP items — out-of-scope items enumerated in §Non-scope of the exploration doc.
- Merge partial stage state — every stage must land on a green bar (`npm run validate:all` passes).
- Insert BACKLOG rows directly into this doc — only `stage-file` materializes them.
- Commit the master plan from the skill — user decides when to commit the new orchestrator.
