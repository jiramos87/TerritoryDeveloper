---
purpose: "Architecture coherence system — split ARCHITECTURE.md into ia/specs/architecture/*, DB-index arch surfaces + decisions + changelog, drift-scan skill, design-explore arch-decide phase, MCP tools."
audience: master-plan-new
created: 2026-04-27
status: ready-for-master-plan-new
size_target: 4 stages
---

# Architecture Coherence System — Exploration

## Why this doc exists

Master plans drift from architectural decisions made after they were authored. Existing `ARCHITECTURE.md` (176 lines, root) is incomplete, sometimes obsolete, and not linked to in-flight planning. Drift discovered late = wasted plan stages + rework.

Goal: keep repo + planning architecture aligned, ordered, easily reachable, easily modifiable. Detect drift early. Poll humans when in-flight plans diverge from new arch decisions.

## Approach (locked via 5-round polling)

| Decision | Choice | Rationale |
|---|---|---|
| **Source of truth** | Doc-primary, DB-indexed | Humans edit markdown; DB indexes relations for MCP queries + drift scans |
| **Doc home** | `ia/specs/architecture/{layers,data-flows,interchange,decisions}.md` | First-class IA — `list_specs`, `spec_outline`, `spec_section` work natively; glossary cross-links |
| **Root `ARCHITECTURE.md`** | Becomes index pointing to sub-specs | Keeps muscle-memory entry; no content duplication |
| **Plan→arch link** | Stage-level `arch_surfaces[]` field | Per-stage declaration; drift scan walks declared set |
| **Drift triggers** | (a) on arch decision write (b) on-demand `/arch-drift-scan` | No pre-stage-file / pre-implement gate — keeps lifecycle fast |
| **Drift reaction** | Drift report + relentless human polling | Matches global agent guidance — never auto-rewrite in-flight plans |
| **Arch authoring** | New phase inside `/design-explore` (`Architecture Decision`) | Standard exploration flow becomes the canonical entry; no new top-level command needed for the common case |
| **Drift scan command** | `/arch-drift-scan {plan?}` | Optional plan arg; default = scan all open plans |
| **Changelog trigger** | Any sub-doc edit | Complete timeline; decision rows cross-reference |
| **Bootstrap** | Stage 1 agent-led split | One commit splits root file into 4 sub-specs + seeds DB |
| **Backfill** | Stage 2 agent-led pass | Existing open master plans gain `arch_surfaces[]` w/ user polling on ambiguity |
| **DB relations** | `arch_decisions`, `arch_surfaces`, `arch_changelog`, `stage_arch_surfaces` (link) | All four enabled — decision→surface, surface→spec section, plan/stage→surface, append-only timeline |

## Architecture (target end-state)

```
docs/
  ARCHITECTURE.md                       # short index → ia/specs/architecture/*
ia/specs/architecture/
  layers.md                             # layer stack + manager dep map
  data-flows.md                         # init order, sim tick, input, persistence
  interchange.md                        # JSON schemas, MCP, Postgres bridge contracts
  decisions.md                          # numbered decisions w/ status (active/superseded)
db/migrations/0032_architecture_index.sql
  arch_surfaces        (id, slug, kind {layer|flow|contract|decision}, spec_path, spec_section, last_edited_at)
  arch_decisions       (id, slug, title, status {active|superseded}, rationale_md, alternatives_md, superseded_by_id, surface_id FK, created_at)
  arch_changelog       (id, surface_id FK, decision_id FK?, summary, kind {edit|decide|supersede}, author, ts)
  stage_arch_surfaces  (stage_id FK ia_stages, surface_slug, declared_at)  -- link table

tools/mcp-ia-server/src/tools/arch.ts
  arch_decision_get, arch_decision_list
  arch_surface_resolve         (input: stage_id | task_id → surfaces[] + spec sections)
  arch_drift_scan              (input: plan_id? → affected_stages[] + suggested polling questions)
  arch_changelog_since         (input: since_ts | since_commit → entries[])

ia/skills/
  arch-drift-scan/SKILL.md     # /arch-drift-scan {plan?} — drift report + AskUserQuestion polling
  design-explore/SKILL.md      # extend: new "Architecture Decision" phase between Select + Expand
```

## Subsystem impact

| Surface | Impact |
|---|---|
| `ia/specs/` | New `architecture/` sub-tree; `list_specs` discovers automatically |
| `docs/MASTER-PLAN-STRUCTURE.md` | Add `arch_surfaces[]` to Stage block schema |
| `ia_stages` table | New nullable JSONB column `arch_surfaces` (or via `stage_arch_surfaces` link table — pick at S2) |
| `ia/skills/design-explore/SKILL.md` | New phase `Architecture Decision`; persisted to `arch_decisions` + `arch_changelog` |
| `ia/skills/master-plan-new/SKILL.md` | Author each Stage with `arch_surfaces[]` declared |
| `ia/skills/stage-decompose/SKILL.md` | Inherit parent Stage's `arch_surfaces[]`; allow per-task override |
| `tools/mcp-ia-server/src/index.ts` | Register 4 new tools (arch_decision_*, arch_surface_resolve, arch_drift_scan, arch_changelog_since) |
| `validate:all` | Add `validate:arch-coherence` — orphan surfaces, dangling FK, stale changelog |
| Root `ARCHITECTURE.md` | Reduced to index stub |
| `CLAUDE.md` + `AGENTS.md` | Update arch pointers to `ia/specs/architecture/*` |

## ## Design Expansion

### Stage breakdown (4 stages)

#### Stage 1 — Doc split + DB schema

**Goal:** Replace root `ARCHITECTURE.md` content with 4 sub-specs under `ia/specs/architecture/`. Land DB migration 0032 carrying `arch_surfaces`, `arch_decisions`, `arch_changelog`, `stage_arch_surfaces`. Seed `arch_surfaces` rows from the new sub-specs.

**Tasks:**
1. Author `ia/specs/architecture/layers.md` from current ARCHITECTURE.md §System Layers + §Helper Services + §Full Dependency Map.
2. Author `ia/specs/architecture/data-flows.md` from §Data Flows (init, sim, input, persistence) + §Interchange JSON.
3. Author `ia/specs/architecture/interchange.md` from §Agent IA + MCP + JSON interchange + Postgres bridge sections.
4. Author `ia/specs/architecture/decisions.md` from §Architectural Decisions + §Known Trade-offs (numbered DEC-A1..N w/ status=active).
5. Reduce root `ARCHITECTURE.md` to short index pointing to sub-specs; update `CLAUDE.md` §3 + `AGENTS.md` arch routing rows.
6. Author `db/migrations/0032_architecture_index.sql` with the 4 tables; run `db:migrate`; seed `arch_surfaces` rows + `arch_decisions` rows from `decisions.md`.

**Exit:** `ls ia/specs/architecture/` shows 4 files; `psql -c "SELECT count(*) FROM arch_surfaces"` ≥ 8 (one per major surface); `arch_decisions` populated; root `ARCHITECTURE.md` ≤ 30 lines; `validate:claude-imports` green.

**arch_surfaces[]:** decisions/source-of-truth-split, layers/all, data-flows/all, interchange/all

---

#### Stage 2 — Plan↔arch backfill + Stage block schema

**Goal:** Extend `ia_stages` w/ `arch_surfaces[]` declaration; backfill all open master plans by walking each stage + polling user when ambiguous; update `MASTER-PLAN-STRUCTURE.md` schema.

**Tasks:**
1. Decide `arch_surfaces` storage: JSONB column on `ia_stages` vs `stage_arch_surfaces` link table. Default = link table (joins cleanly w/ `arch_surfaces.slug`).
2. Migration 0033: add `stage_arch_surfaces` link table OR `ia_stages.arch_surfaces jsonb`. Update `stage_insert` + `stage_render` MCP tools.
3. Update `docs/MASTER-PLAN-STRUCTURE.md`: add `arch_surfaces[]` to Stage block schema; add example.
4. Backfill skill: walk every open `ia_master_plans` row → for each `ia_stages` row, infer `arch_surfaces[]` from §Plan Digest contents (glossary terms + path scans); write to DB; emit polling question per ambiguous stage.
5. Validators: `validate:arch-coherence` — every `stage_arch_surfaces.surface_slug` exists in `arch_surfaces`; flag unlinked open stages.

**Exit:** All open master plans have ≥1 `arch_surfaces` declared per stage (or explicit `none` marker); `validate:arch-coherence` green; `MASTER-PLAN-STRUCTURE.md` updated.

**arch_surfaces[]:** decisions/plan-arch-link-shape, interchange/master-plan-stage-block

---

#### Stage 3 — MCP tools + drift-scan skill

**Goal:** Land 4 MCP tools for arch surface; author `/arch-drift-scan` skill that emits drift report + polls user.

**Tasks:**
1. Implement `arch_decision_get` + `arch_decision_list` in `tools/mcp-ia-server/src/tools/arch.ts`. Register in `index.ts`. Zod input/output schemas.
2. Implement `arch_surface_resolve` (input: `stage_id` | `task_id` → return surfaces[] + their `spec_path` + `spec_section`).
3. Implement `arch_drift_scan` (input: `plan_id?` → list affected stages + suggested polling questions per affected stage). Compares stage's declared `arch_surfaces[]` against changelog entries since stage's last `_pending_` flip.
4. Implement `arch_changelog_since` (input: `since_ts` or `since_commit` → entries[]).
5. Author `ia/skills/arch-drift-scan/SKILL.md` w/ command-body + agent-body. Phases: load plan → call `arch_drift_scan` MCP → render drift report → AskUserQuestion polling per affected stage → write decisions back as plan Change log entries. Run `npm run skill:sync:all`.
6. Add Skill Tool surface to `.claude/agents/arch-drift-scan.md` + `.claude/commands/arch-drift-scan.md` (generated).

**Exit:** Manual `/arch-drift-scan` returns drift report + polls user; 4 MCP tools listed in `list_specs`-equivalent for tools; new skill listed in `available-skills`.

**arch_surfaces[]:** decisions/drift-scan-shape, interchange/mcp-tool-surface

---

#### Stage 4 — Design-explore embed + ARCHITECTURE.md retirement closeout

**Goal:** Add `Architecture Decision` phase to `/design-explore`; auto-trigger drift scan on each new decision write; retire root `ARCHITECTURE.md` to pure index.

**Tasks:**
1. Edit `ia/skills/design-explore/SKILL.md`: insert new phase `Architecture Decision` between `Select Approach` and `Expand`. Phase polls user for decision/rationale/alternatives/affected-surfaces[]; writes `arch_decisions` row + `arch_changelog` entry; calls `arch_drift_scan` against open plans + appends drift report to exploration doc.
2. Wire post-write hook: any commit touching `ia/specs/architecture/**` appends `arch_changelog` row (tools/scripts/claude-hooks/ + tooling). Decide whether trigger lives in PostToolUse hook or `validate:arch-coherence` rebuild.
3. Update `validate:all` to include `validate:arch-coherence`. Update CI workflow.
4. Final retirement: confirm root `ARCHITECTURE.md` is index-only; remove obsolete prose; update `CLAUDE.md` §3 trigger row; update `AGENTS.md` arch references.
5. Author 1-page README under `ia/specs/architecture/README.md` explaining sub-spec roles + lifecycle.

**Exit:** Running `/design-explore` on a sample doc adds Architecture Decision phase output; new decision triggers drift scan + drift report visible in exploration doc; root `ARCHITECTURE.md` is index-only; `validate:all` green w/ new arch-coherence check.

**arch_surfaces[]:** decisions/design-explore-arch-phase, interchange/changelog-trigger

---

### Cross-stage invariants

- Doc edits = source of truth; DB rows track relations for MCP queries — never the other way around.
- `arch_decisions.status` only flips `active → superseded` via new decision row referencing `superseded_by_id`. No deletes.
- `arch_changelog` is append-only. Never rewritten.
- New `arch_surfaces` slug created only when sub-spec section actually exists. `validate:arch-coherence` enforces.
- `/arch-drift-scan` never auto-rewrites plan contents. Only writes drift entries to `master_plan_change_log` + emits polling questions.
- Existing master plans without backfilled `arch_surfaces[]` get treated as `surfaces=unknown` by drift scan (warn, not block).

### Acceptance criteria (master-plan-level)

- Root `ARCHITECTURE.md` reduced to index < 30 lines.
- `ia/specs/architecture/{layers,data-flows,interchange,decisions}.md` all exist + referenced from index.
- DB migration 0032 + 0033 applied; 4 arch tables populated; `stage_arch_surfaces` non-empty for all open plans.
- 4 MCP tools registered + working: `arch_decision_get/list`, `arch_surface_resolve`, `arch_drift_scan`, `arch_changelog_since`.
- `/arch-drift-scan {plan?}` skill operational; emits drift report + polls user; writes plan Change log entries.
- `/design-explore` new `Architecture Decision` phase active; sample run produces decision + changelog + drift scan + report.
- `validate:arch-coherence` integrated into `validate:all`; CI green.
- `CLAUDE.md` §3 + `AGENTS.md` arch rows updated.

### Out of scope (explicit)

- ADR-style multi-paragraph decision records — `decisions.md` stays compact, table-driven.
- Auto-rewriting in-flight plan stages — always human-gated.
- Web workspace arch surfaces — `web/` already has its own design-system spec; this system covers it via `arch_surfaces.kind=cross_cutting` rows but does not re-author web docs.
- Sprite-gen / asset pipeline arch — already captured in their respective explorations + master plans; will be backfilled in Stage 2.
- C# AST extraction / code-derived arch — manual authoring only.

### Glossary terms (new)

- `arch_surface` — addressable architectural surface (layer / flow / contract / decision) with `spec_path` + section anchor
- `arch_decision` — named decision row w/ status, rationale, alternatives, optional `superseded_by`
- `arch_changelog` — append-only timeline of arch sub-spec edits + decision writes
- `architecture coherence system` — the umbrella system this plan delivers
- `architecture drift` — divergence between an in-flight plan stage and the current architectural surfaces it depends on

---

All decisions resolved (5 polling rounds). Ready for `/master-plan-new docs/architecture-coherence-system-exploration.md` to seed the 4-stage orchestrator from `## Design Expansion`.
