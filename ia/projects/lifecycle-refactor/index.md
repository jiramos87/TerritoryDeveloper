# Lifecycle Refactor — Opus Planner / Sonnet Executor + Hierarchy Collapse — Master Plan (Umbrella)

> **Status:** In Progress — Stage 10
>
> **Scope:** Big-bang collapse of Step/Stage/Phase/Task hierarchy to Stage/Task. Introduce Plan-Apply pair pattern (5 seams) with Opus pair-heads and Sonnet pair-tails. Sonnet-ify spec enrichment. Add Opus audit + code-review inline stages. Migrate all 16 open master plans + open project specs + backlog yaml in place. Tooling surface only — zero Unity runtime C# touch.
>
> **Exploration source:** `docs/lifecycle-opus-planner-sonnet-executor-exploration.md` (§Design Expansion: Chosen Approach, Architecture, Subsystem Impact, Implementation Points, Review Notes). Rev 2–4 extensions appended: plan-author + progress-emit (rev 2); stage-end bulk closeout (rev 2); stage-end bulk plan-author + audit + spec-enrich fold (rev 3); **rev 4 candidates + cache-mechanics amendments (2026-04-19 rev 4)** — prompt-caching optimization layer; Stage 10 post-merge fold gated by Q9 baseline.
>
> **Locked decisions (do not reopen in this plan):**
> - Q1 = Approach B — full hierarchy collapse, big-bang sequential.
> - Q2 = migrate all in place; no dual-schema window.
> - Q3 = parent layer renamed to **Stage** (minimizes rename surface: `project-stage-close` / `/ship-stage` / web `/dashboard` already say "Stage"). Old Step+Stage pair collapses → new Stage; old Phase rows merge up into parent Stage.
> - Q4 = per-task project specs (`ia/projects/{ISSUE_ID}.md`) preserved across migration; only frontmatter phase fields dropped.
> - Q5 = one sequential big-bang pass; multi-session resumable via `ia/state/lifecycle-refactor-migration.json` (keyed per phase + per file).
>
> **Hierarchy rules:** `ia/rules/project-hierarchy.md` (step > stage > phase > task — current schema; rewritten in Stage 1.2 to Stage > Task). `ia/rules/orchestrator-vs-spec.md` (this doc = orchestrator, never closeable).
>
> **Note — self-migration:** This orchestrator itself will be subject to Stage 2.1 transform. The `migrate-master-plans.ts` script must run this file last in the batch, after all other plans are validated. Self-migration is idempotent: the script reads snapshot, emits to current path.
>
> **Read first if landing cold:**
> - `docs/lifecycle-opus-planner-sonnet-executor-exploration.md` — full design + architecture + review notes. Design Expansion block is ground truth.
> - `ia/rules/project-hierarchy.md` + `ia/rules/orchestrator-vs-spec.md` — doc semantics + phase/task cardinality rule (≥2 tasks per phase — applies until Stage 1.2 rewrites the rule to ≥2 tasks per stage).
> - `ia/rules/terminology-consistency.md` — canonical vocabulary: **Plan-Apply pair**, **Orchestrator document**, **Project spec**, **Backlog record**.
> - Related orchestrator: `ia/projects/mcp-lifecycle-tools-opus-4-7-audit-master-plan.md` — composite MCP bundle proposal; Stage 3.1 builds on its `lifecycle_stage_context` pattern.
> - MCP: `backlog_issue {id}` per referenced id once tasks file; never full `BACKLOG.md` read.

---

## Stages

> **Tracking legend:** Step / Stage `Status:` uses enum `Draft | Skeleton | Planned | In Review | In Progress — {active child} | Final` (per `ia/rules/project-hierarchy.md`; `Skeleton` + `Planned` authored by `master-plan-new` / `stage-decompose`). Phase bullets use `- [ ]` / `- [x]`. Task tables carry a **Status** column: `_pending_` (not filed) → `Draft` → `In Review` → `In Progress` → `Done (archived)`. Markers flipped by lifecycle skills: `stage-file` → task rows gain `Issue` id + `Draft` status; `stage-file` also flips Stage header `Draft/Planned → In Progress` (R2) and plan top Status `Draft → In Progress — Step {N} / Stage {N.M}` on first task ever filed (R1); `stage-decompose` → Step header `Skeleton → Draft (tasks _pending_)` (R7); `/author` (`plan-author`) → `In Review`; `/implement` → `In Progress`; the Stage-scoped `/closeout` pair (`stage-closeout-plan` → `plan-applier` Mode `stage-closeout`) → task rows `Done (archived)` + stage `Final` + stage-level step rollup + plan top Status `→ Final` when all Steps read `Final` (R5); `master-plan-extend` → plan top Status `Final → In Progress — Step {N_new} / Stage {N_new}.1` when new Steps appended to a Final plan (R6).

---

### Stage index

- [Stage 1 — Foundation: Freeze, Templates & Rules / Branch + Snapshot + Migration State](stage-1-branch-snapshot-migration-state.md) — _Final_
- [Stage 2 — Foundation: Freeze, Templates & Rules / Templates + Rules + Glossary + Plan-Apply Contract](stage-2-templates-rules-glossary-plan-apply-contract.md) — _Final_
- [Stage 3 — Data Migration: Master Plans + Backlog Schema / Transform Script + Master-Plan In-Place Migration](stage-3-transform-script-master-plan-in-place-migration.md) — _Final_
- [Stage 4 — Data Migration: Master Plans + Backlog Schema / Phase Layer Fold: Specs + YAML Schema](stage-4-phase-layer-fold-specs-yaml-schema.md) — _Done_
- [Stage 5 — Infrastructure + Execution Surface / MCP Server: Drop Phase + plan_apply_validate](stage-5-mcp-server-drop-phase-plan-apply-validate.md) — _Final_
- [Stage 6 — Infrastructure + Execution Surface / Web Dashboard: Parser + PlanTree Collapse](stage-6-web-dashboard-parser-plantree-collapse.md) — _Final_
- [Stage 7.1 — Skills Layer / Pair Skills + Retirement + Existing Skill Updates](stage-7.1-pair-skills-retirement-existing-skill-updates.md) — _Done_
- [Stage 7.2 — Wiring Layer / Agents + Commands + Rule Docs + Validation](stage-7.2-agents-commands-rule-docs-validation.md) — _Pending (blocked on Stage 7.1)_
- [Stage 8 — Validation + Merge / Dry-Run + Full Validation](stage-8-dry-run-full-validation.md) — _Final (closed 2026-04-19)_
- [Stage 9 — Validation + Merge / Sign-Off + Merge](stage-9-sign-off-merge.md) — _Done_
- [Stage 10 — Prompt-Caching Optimization Layer (Post-Merge, Q9-Gated)](stage-10-prompt-caching-optimization-layer-post-merge-q9-gated.md) — _In Progress_

## Orchestration guardrails

**Do:**

- Open one Stage at a time. Next Stage opens only after current Stage's `/closeout` pair (`stage-closeout-plan` → `plan-applier` Mode `stage-closeout`) runs.
- Run `claude-personal "/stage-file ia/projects/lifecycle-refactor-master-plan.md Stage 1.1"` to materialize pending tasks → BACKLOG rows + `ia/projects/{ISSUE_ID}.md` stubs.
- Update Stage / Step `Status` + phase checkboxes as lifecycle skills flip them — do NOT edit by hand.
- Preserve locked decisions (see header block). Q1–Q5 are closed; changes require explicit re-decision + sync edit to exploration doc.
- Respect migration JSON: always read current state before resuming; write per-file progress immediately after each file is processed (crash safety).
- Consult `ia/state/pre-refactor-snapshot/` for the canonical source of pre-refactor state. Never use current `ia/projects/*master-plan*.md` as M2 input — always read from snapshot.
- Self-migration note: when running `migrate-master-plans.ts` in Stage 2.1, run `lifecycle-refactor-master-plan.md` itself last in the M2 batch after all other plans are validated.
- **Tooling-only verify fast-path (M0–M10):** For refactor task closeouts in Stages 5, 6, 7, 9, 10 (MCP TypeScript / web Next.js / skills + agents + commands markdown / docs / scripts — zero Unity runtime C# touch), use `npm run validate:all` directly OR dispatch `/verify-loop --tooling-only` (skips Steps 0, 1, 3, 4a, 4b, 5, 6 of the decision matrix; runs Step 2 + Step 7 only). Full `/verify-loop` (compile gate + Path A / Path B + bridge) reserved for Stage 8 T8.3 where `npm run verify:local` is the explicit acceptance gate. Plan header (line 5) guarantees `Tooling surface only — zero Unity runtime C# touch`; enforce at task-close time. Skill-level flag definition: `ia/skills/verify-loop/SKILL.md` §Inputs + §Pre-matrix mode gate.

**Do not:**

- Close this orchestrator via `/closeout` — orchestrators are permanent (see `ia/rules/orchestrator-vs-spec.md`). Only Stage 4.2 Final triggers `Status: Final`; the file stays.
- Skip the user gate at Stage 4.2 T4.2.1 — merge requires explicit human sign-off.
- Run new `/master-plan-new`, `/master-plan-extend`, `/stage-decompose`, or `/stage-file` calls outside this orchestrator during the freeze window (M0–M8). The freeze is declared in `CLAUDE.md` by T1.1.1.
- Merge partial stage state — every Stage must reach green-bar before the `/closeout` pair runs.
- Insert BACKLOG rows directly into this doc — only `stage-file-apply` materializes them.
- Use `migrate-master-plans.ts` on any spec file other than orchestrator master plans — project specs (`ia/projects/{ISSUE_ID}.md`) are handled by T2.2.1 (separate targeted edit, not the batch transform script).
- Open Stage 10 before M8 sign-off. Stage 10 is a post-merge optimization layer. Precondition gate (Q9 baseline ≥ 3 pair-head reads/Stage median) WAIVED by user 2026-04-19; T10.1 opens unconditionally but M8 merge + freeze lift must precede. Pre-Stage-10 work limited to reference doc + candidate-pool persistence (already landed 2026-04-19) — never runtime cache wiring during freeze window.
