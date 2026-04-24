# Backlog YAML ↔ MCP alignment — Master Plan

> **Last updated:** 2026-04-18
>
> **Status:** In Progress — Step 4 / Stage 4.2
>
> **Scope:** Align MCP territory-ia tool surface + `ParsedBacklogIssue` type + validator + skill docs with the per-issue yaml backlog refactor. Nine Implementation Points (HIGH band IP1–IP5, MEDIUM/LOW band IP6–IP9) plus one correctness fix (soft-dep marker preservation, folded into IP1). Steps 3–6 (parent-plan + step/stage locator fields + MCP reverse-lookup tooling + skill patches + late-hardening / archive backfill) appended 2026-04-18 via `/master-plan-extend`. Pure tooling / MCP / validator / skill-docs work — zero Unity runtime touches, zero save-schema touches.
>
> **Exploration source:** `docs/backlog-yaml-mcp-alignment-exploration.md` (§Problem, §Design Expansion block, §Deferred decomposition hints) for Steps 1–2. `docs/parent-plan-locator-fields-exploration.md` (§Design Expansion, Phase 6 Implementation Points) for Steps 3–6. Blocks are ground truth.
>
> **Locked decisions (do not reopen in this plan):**
> - Per-issue yaml layout (`ia/backlog/{id}.yaml`, `ia/backlog-archive/{id}.yaml`) + section manifests (`ia/state/backlog-sections.json`, `ia/state/backlog-archive-sections.json`) stay byte-compatible.
> - Monotonic id source stays `ia/state/id-counter.json` via `tools/scripts/reserve-id.sh` under flock (invariant #13).
> - Materialize stays deterministic — `BACKLOG.md` + `BACKLOG-ARCHIVE.md` are generated views, never hand-edited.
> - Minimal yaml parser in `backlog-yaml-loader.ts` stays — no migration to a real yaml lib in this plan.
> - `proposed_solution` field fate decided by Grep gate (zero consumers → drop; ≥1 consumer → add to yaml schema). Decision captured in IP2 Stage.
> - Approach B selected for locator fields (Steps 3–6): full yaml schema v2 extension (`parent_plan` + `task_key` required; `step` / `stage` / `phase` / `router_domain` / `surfaces` / `mcp_slices` / `skill_hints` optional) + MCP reverse-lookup tools (`master_plan_locate`, `master_plan_next_pending`, `parent_plan_validate`) + dual-mode validator (advisory default + `--strict` flip). Source: `docs/parent-plan-locator-fields-exploration.md` §Recommendation + §Phase 2.
> - Spec-frontmatter mirror = 2 fields only (`parent_plan` + `task_key`); step/stage/phase derivable from `task_key` parser. Lazy — populated on next `/kickoff`, never retroactive rewrite.
> - `surfaces` auto-populated by `stage-file` from plan task-row "Relevant surfaces"; `spec-kickoff` append-only in §4 / §5.2 regions (never reorder / rewrite / drop).
> - `skill_hints` advisory hint only — `stage-file` / `project-new` write; kickoff / implement read as routing suggestion, not mandate.
> - Migration scope hybrid — open-yaml one-shot backfill in Step 3; archive deferred with `--skip-unresolvable` in Step 6; plans zero backfill.
>
> **Hierarchy rules:** `ia/rules/project-hierarchy.md` (step > stage > phase > task; ≥2 tasks per phase). `ia/rules/orchestrator-vs-spec.md` (this doc = orchestrator, **never closeable** via `/closeout`).
>
> **Sibling orchestrators in flight:**
> - `ia/projects/multi-scale-master-plan.md` — Unity runtime C# + save schema. Disjoint surface (no `tools/mcp-ia-server/**` touches). No collision.
> - `ia/projects/web-platform-master-plan.md` — Next.js at `web/`. Disjoint surface. No collision.
> - `ia/projects/blip-master-plan.md` — runtime C# audio. Disjoint. No collision.
> - **Parallel-work rule:** do NOT run `/stage-file` or `/closeout` against two sibling orchestrators concurrently on the same branch — MCP index regens (`npm run mcp:regen-index`) must sequence.
>
> **Read first if landing cold:**
> - `docs/backlog-yaml-mcp-alignment-exploration.md` — full problem analysis + 9 Implementation Points (Steps 1–2).
> - `docs/parent-plan-locator-fields-exploration.md` — Approach B + Phase 6 Implementation Points (Steps 3–6).
> - `tools/mcp-ia-server/src/parser/backlog-parser.ts` + `backlog-yaml-loader.ts` — current parser surface.
> - `tools/mcp-ia-server/src/parser/types.ts` (or equivalent) — `ParsedBacklogIssue` shape.
> - `tools/scripts/reserve-id.sh` + `tools/scripts/materialize-backlog.sh` + `tools/scripts/materialize-backlog.mjs` — ID + materialize flow.
> - `tools/validate-backlog-yaml.mjs` — current validator scope.
> - `ia/skills/stage-file/SKILL.md` + `ia/skills/project-new/SKILL.md` + Stage-scoped `/closeout` pair (`ia/skills/stage-closeout-plan/SKILL.md` → `ia/skills/plan-applier/SKILL.md` Mode `stage-closeout`) — skills that write/mutate yaml.
> - `ia/rules/project-hierarchy.md` + `ia/rules/orchestrator-vs-spec.md` — phase/task cardinality + permanent-orchestrator rule.
>
> **Invariants implicated:**
> - #12 (`ia/projects/` for issue-specific specs — applies to every `_pending_` row filed under this orchestrator).
> - #13 (monotonic id source = `reserve-id.sh` — IP3 MCP wrapper calls the script, never hand-edits the counter).
> - Invariants #1–#11 NOT implicated — no Unity runtime / no `GridManager` / no road / no HeightMap touches.

---

## Stages

> **Tracking legend:** Step / Stage `Status:` uses enum `Draft | In Review | In Progress — {active child} | Final`. Phase bullets use `- [ ]` / `- [x]`. Task tables carry a **Status** column: `_pending_` (not filed) → `Draft` → `In Review` → `In Progress` → `Done (archived)`. Markers flipped by lifecycle skills: `stage-file` → task rows gain `Issue` id + `Draft`; `/author` (`plan-author`) → `In Review`; `/implement` → `In Progress`; the Stage-scoped `/closeout` pair (`stage-closeout-plan` → `plan-applier` Mode `stage-closeout`) → task rows `Done (archived)` + stage `Final` + stage-level rollup.

---

### Stage index

- [Stage 1 — HIGH band (IP1–IP5) / Types + yaml loader (IP1 + IP2)](stage-1-types-yaml-loader-ip1-ip2.md) — _Final_
- [Stage 2 — HIGH band (IP1–IP5) / MCP tools batch 1 (IP3 + IP4 + IP5)](stage-2-mcp-tools-batch-1-ip3-ip4-ip5.md) — _Final_
- [Stage 3 — HIGH band (IP1–IP5) / Skill wiring + docs](stage-3-skill-wiring-docs.md) — _Final_
- [Stage 4 — MEDIUM / LOW band (IP6–IP9) / Script hardening (IP7)](stage-4-script-hardening-ip7.md) — _In Progress (TECH-355, TECH-356, TECH-357 filed)_
- [Stage 5 — MEDIUM / LOW band (IP6–IP9) / Validator extensions (IP8)](stage-5-validator-extensions-ip8.md) — _In Progress — 2026-04-24 (5 tasks filed)_
- [Stage 6 — MEDIUM / LOW band (IP6–IP9) / MCP extensions (IP6 + IP9)](stage-6-mcp-extensions-ip6-ip9.md) — _Draft — tasks `_pending_`._
- [Stage 7 — yaml schema v2 + backfill + validator MVP (locator fields) / yaml schema v2 + parser](stage-7-yaml-schema-v2-parser.md) — _Final_
- [Stage 8 — yaml schema v2 + backfill + validator MVP (locator fields) / Template frontmatter + backfill script](stage-8-template-frontmatter-backfill-script.md) — _Final_
- [Stage 9 — yaml schema v2 + backfill + validator MVP (locator fields) / `parent_plan_validate` + `backlog_record_validate` v2](stage-9-parent-plan-validate-backlog-record-validate-v2.md) — _Final_
- [Stage 10 — MCP reverse-lookup tooling / `master_plan_locate` + `master_plan_next_pending`](stage-10-master-plan-locate-master-plan-next-pending.md) — _Final_
- [Stage 11 — MCP reverse-lookup tooling / `backlog_list` filter extensions + catalog docs](stage-11-backlog-list-filter-extensions-catalog-docs.md) — _In Progress_
- [Stage 12 — Skill patches + plan consumers / Seed skills (`project-new`, `stage-file`)](stage-12-seed-skills-project-new-stage-file.md) — _Draft (tasks _pending_ — not yet filed)_
- [Stage 13 — Skill patches + plan consumers / Read skills (author / implement)](stage-13-implement.md) — _Draft (tasks _pending_ — not yet filed; T13.1 + T13.3 cancelled by M6 collapse)_
- [Stage 14 — Skill patches + plan consumers / Dispatcher consumers (`/ship`, `release-rollout-enumerate`)](stage-14-ship-release-rollout-enumerate.md) — _Draft (tasks _pending_ — not yet filed)_
- [Stage 15 — Late-hardening + archive backfill (deferred) / Flip validator default to blocking](stage-15-flip-validator-default-to-blocking.md) — _Draft (tasks _pending_ — not yet filed)_
- [Stage 16 — Late-hardening + archive backfill (deferred) / Archive backfill pass](stage-16-archive-backfill-pass.md) — _Draft (tasks _pending_ — not yet filed)_

## Acceptance (whole orchestrator)

- All 9 Implementation Points (IP1–IP9) shipped + soft-dep marker fix folded in.
- `npm run validate:all` green across the whole chain (lint + typecheck + MCP tests + validator + concurrency harness).
- `unity:compile-check` N/A — zero Unity / C# touches in this plan.
- `ia/state/id-counter.json` never hand-edited — all writes through `reserve-id.sh` (direct) or `reserve_backlog_ids` MCP tool (indirect).
- Soft-dep markers (e.g. `(soft)`, `[optional]`) preserved end-to-end across yaml round-trip.
- Deterministic materialize remains byte-identical for workloads unaffected by this plan.
- Parallel `stage-file` + parallel MCP `backlog_record_create` runs race-free.
- `ia/skills/stage-file/SKILL.md` + `ia/skills/project-new/SKILL.md` document the MCP-first call path with bash fallback.
- `docs/mcp-ia-server.md` + `CLAUDE.md` §2 updated with new tools.

## Non-goals

- Replace the minimal yaml parser with a real yaml library.
- Change per-issue yaml layout or section manifest shape.
- Migrate `BACKLOG.md` / `BACKLOG-ARCHIVE.md` away from the generated-view model.
- Touch Unity runtime, save schema, glossary entries, or any other IA surface beyond skill docs + tool catalog.
- File the BACKLOG rows for this plan — user runs `/stage-file ia/projects/backlog-yaml-mcp-alignment-master-plan.md Stage 1.1` (etc) in a separate agent session.

## Handoff

Next: `/stage-file ia/projects/backlog-yaml-mcp-alignment-master-plan.md Stage 1.1` — file the Stage 1.1 tasks as BACKLOG rows + per-issue yaml records. Repeat per stage, priority order (1.1 → 1.2 → 1.3 → 2.1 → 2.2 → 2.3). Do NOT file Stage 2.* before Stage 1.* completes — Step 2 depends on Step 1 outputs (shared lint core, reserve tool, flock-guarded materialize).
