# UI Bake Pipeline Rollout — Process Plan

> **Role.** Process-level plan covering DB-first UI bake pipeline rollout: hud-bar achieved → toolbar debt repay → calibration tooling → design-system promotion → convergence test before Region UI grilling. NOT a master plan (no DB rows). NOT an exploration (no decision space). NOT a single backlog issue (cuts across many). **Living doc — update tracker as tracks land.**
>
> **Source.** Distilled from `docs/mvp-scope.md` (locked 2026-05-07), `docs/ui-element-definitions.md` (Phase 1 panel locks), `docs/ideas/ui-elements-grilling.md` (process spec + calibration design), `docs/tmp/hud-bar-bake-bugs.md` (bug catalog + improvements), `docs/hud-bar-bake-test-process.md` (rebake-1..7 iteration log).
>
> **Invariant (cardinal).** Database always first when changing UI. Flow: DB → snapshot → bake → prefab → scene. Never edit baked artifacts. Never edit scene yaml directly to drive UI shape.

---

## Status snapshot — 2026-05-08

| Surface | DB-first? | Status |
|---|---|---|
| `hud-bar` panel + 14 children | ✅ | Locked. Baked. Visually validated rebake-7. |
| `toolbar` panel | ❌ | Position lives in scene yaml override (commit `0e9060a2`). No DB seed. **Debt — Track A.** |
| `docs/ui-element-definitions.md` ↔ DB | partial | hud-bar locked def written; DB rect_json shape not reflected back. Toolbar locked on paper, not in DB. |
| `ia/specs/ui-design-system.md` | not started | Upgrade target. Empty file or stub. |
| Calibration corpus + verdicts log | not started | hud-bar grilling decisions un-recorded. |
| MCP slices for UI calibration | not started | 6 tools proposed in `ui-elements-grilling.md §10.3`. |
| `ia/skills/ui-element-grill/SKILL.md` | not started | Formalization deferred until calibration tools land. |
| Bake-pipeline improvements Imp-1..8 | 1 of 8 done | Imp-3 landed (bake handler emits warnings in mutation_result). |

---

## Tracks

### Track A — Pay scene-edit debt (immediate)

**Goal.** Migrate toolbar position from scene yaml override into DB. Restore DB-first invariant.

| Step | Action | Surface touched | Done? |
|---|---|---|---|
| A.1 | Migration: add `toolbar` row to `catalog_entity` + `panel_detail` with `rect_json` matching current scene values (top inset 152, full-height bottom 200, left edge anchor) | `db/migrations/0110_seed_toolbar_panel.sql` | ☐ |
| A.2 | Snapshot exporter naturally picks up toolbar (already orders panels by slug ASC) — verify panels.json now carries toolbar row | `Assets/UI/Snapshots/panels.json` | ☐ |
| A.3 | Extend bake handler `ApplyPanelRectJsonOverlay` to handle non-bake-spawned panels: find prefab instance in scene by slug, write `PrefabInstance` rect modifications from DB rect_json | `Assets/Scripts/Editor/Bridge/UiBakeHandler.cs` | ☐ |
| A.4 | Revert `CityScene.unity` toolbar PrefabInstance hand-edit; run bake; confirm bake re-writes the same overrides programmatically | `Assets/Scenes/CityScene.unity` (revert) | ☐ |
| A.5 | Visual verify: bridge screenshot — toolbar no longer overlaps hud-bar; matches rebake-7 visual | screenshot under `tools/reports/` | ☐ |
| A.6 | Commit: `feat(ui-bake): toolbar DB-first rect overlay + revert scene yaml debt` | git | ☐ |

**Exit criteria.** Toolbar root rect comes from `panel_detail.rect_json` row. `git diff CityScene.unity` shows zero toolbar yaml drift after a clean bake.

---

### Track B — Doc alignment (next session)

**Goal.** Reflect achieved DB shape in `docs/ui-element-definitions.md`. Seed calibration corpus from hud-bar grilling decisions.

| Step | Action | Surface | Done? |
|---|---|---|---|
| B.1 | Add "DB shape achieved" sub-block per locked panel section (hud-bar + toolbar) — mig id, entity slug, rect_json snapshot, schema_v4 children list | `docs/ui-element-definitions.md` | ☐ |
| B.2 | Capture hud-bar grilling decisions verbatim into `ia/state/ui-calibration-corpus.jsonl` — one row per decision (prompt → resolution → rationale) | new file | ☐ |
| B.3 | Capture rebake-1..7 verdicts into `ia/state/ui-calibration-verdicts.jsonl` — per-rebake outcome with bug/improvement IDs and resolution path | new file | ☐ |
| B.4 | Cross-link `docs/ui-element-definitions.md` ↔ `docs/ui-bake-pipeline-rollout-plan.md` (this doc) ↔ `docs/ideas/ui-elements-grilling.md` headers | three docs | ☐ |

**Exit criteria.** Reading `docs/ui-element-definitions.md` § hud-bar tells me both the LOCKED def AND the DB row that backs it, with a pointer to migration id.

---

### Track C — Calibration tooling

**Goal.** Build the MCP slices + drift gate that make grilling repeatable and self-checking for next panels.

| Step | Action | Surface | Priority | Done? |
|---|---|---|---|---|
| C.1 | File backlog issue: `ui_def_drift_scan` MCP tool — diff `docs/ui-element-definitions.md` panel block ↔ `panel_detail` row. Run as CI gate. | new TECH-* | P0 | ☐ |
| C.2 | File backlog issue: `ui_calibration_corpus_query` + `ui_calibration_verdict_record` MCP tools | new TECH-* | P0 | ☐ |
| C.3 | File backlog issue: `ui_panel_get` / `ui_panel_list` / `ui_panel_publish` MCP slices (mirrors of `catalog_panel_*` but with calibration-aware projection) | new TECH-* | P1 | ☐ |
| C.4 | File backlog issue: `ui_token_*` + `ui_component_*` MCP slices once tokens + components seeded in DB | new TECH-* | P2 | ☐ |
| C.5 | File backlog issues for bake-pipeline improvements Imp-1, Imp-2, Imp-4, Imp-5, Imp-6, Imp-7, Imp-8 (Imp-3 already landed) | new TECH-*/BUG-* | mixed | ☐ |
| C.6 | Implement `ui_def_drift_scan` first (highest leverage — gate prevents future drift) | MCP server + CI | P0 | ☐ |

**Exit criteria.** CI red on any `docs/ui-element-definitions.md` ↔ DB drift. Corpus + verdicts queryable via MCP.

---

### Track D — Design system promotion

**Goal.** Promote tokens + components from `docs/ui-element-definitions.md` into `ia/specs/ui-design-system.md` as authoritative export. Formalize grilling skill.

**Trigger.** ≥3 panels DB-locked (hud-bar done, toolbar Track A, plus one more — likely `tool-subtype-picker` or `info-panel`).

| Step | Action | Surface | Done? |
|---|---|---|---|
| D.1 | Promote § Tokens + § Components from `docs/ui-element-definitions.md` to `ia/specs/ui-design-system.md`. Definitions doc references spec; spec is canonical | new spec file | ☐ |
| D.2 | Migrate token + component rows into DB if not already (`catalog_token` / catalog_component or equivalent) | DB migration | ☐ |
| D.3 | Write `ia/skills/ui-element-grill/SKILL.md` — encodes 5-phase process (baseline → panels → buttons → bindings → interactions → loop), corpus + verdicts loop, MCP slice usage order | new skill | ☐ |
| D.4 | Run `npm run skill:sync:all` so the new skill produces `.claude/agents/ui-element-grill.md` + `.claude/commands/ui-element-grill.md` | generated | ☐ |

**Exit criteria.** `ia/specs/ui-design-system.md` is the single source of truth for tokens + components. Skill can be invoked via `/ui-element-grill {panel-slug}`.

---

### Track E — Convergence test before Region UI grilling

**Goal.** Prove the calibrated grilling pipeline is human-light enough to handle a novel panel autonomously. Gate to Region UI grilling readiness.

| Step | Action | Surface | Done? |
|---|---|---|---|
| E.1 | Pick a novel small panel — candidate: `tooltip` primitive or `notifications-toast` (small surface area, isolated from other panels) | choice | ☐ |
| E.2 | Grill the panel WITHOUT human input — agent uses corpus + design-system + MCP slices alone | grilling run | ☐ |
| E.3 | Score agent output against independently-authored definition: structure match, token usage match, action+bind correctness, drift detection | scoring rubric | ☐ |
| E.4 | Gate: ≥85% match → declare grilling agent-autonomous; else iterate corpus / verdicts / skill body until threshold met | verdict | ☐ |
| E.5 | Once gate cleared → ready to grill Region UI panels at scale | ready signal | ☐ |

**Exit criteria.** Agent grilling produces ≥85%-match panel definitions on novel surfaces using only IA + DB context.

---

## Cross-track dependencies

```
A (toolbar DB-first) ──┬──> B (doc alignment + corpus seed) ──> C (calibration tools)
                       │                                          │
                       └────────────────────────────> D (design-system + skill) ──> E (convergence test)
```

A blocks nothing strictly but unblocks confidence in DB-first invariant.
B seeds C's corpus/verdicts sources.
C provides the drift gate D needs to publish a stable design-system spec.
D provides the skill E exercises.

---

## Out of scope (this plan)

- Region UI grilling itself (post-Track E). Will spawn its own master plan.
- Country UI (no scene per `docs/mvp-scope.md §2`).
- Full rebuild of toolbar.prefab as bake-pipeline-owned artifact (Track A only owns root rect; children stay hand-authored).
- Full token migration of every component (Track D handles tokens + components surface-only; deeper component refactor = post-MVP).
- 12 hud-bar bake bugs A–L cleanup beyond what rebake-1..7 already resolved (residual bugs absorbed into Imp-* tracking).

---

## References

| Doc | Role |
| --- | --- |
| `docs/ui-element-definitions.md` | Panel definitions + DB-shape annotation + Calibration ledger schema (Stage 2 source of truth) |
| `docs/ideas/ui-elements-grilling.md` | Process spec — grilling protocol, polling templates, calibration design vision |
| `ia/state/ui-calibration-corpus.jsonl` | Grilling decision ledger — append-only corpus rows |
| `ia/state/ui-calibration-verdicts.jsonl` | Rebake verdict ledger — append-only verdict rows |

---

## Changelog

| Date | Change |
|---|---|
| 2026-05-08 | Plan created. Tracks A–E enumerated. Track A starting same session. |
| 2026-05-08 | Stage-2.AUDIT: §References added. Cross-links to definitions + grilling-ideas + state files wired. |
