---
purpose: "Seed doc for design-explore — close the UI Toolkit emitter gap so DB rows become canonical for the current shipping UI via clean unity:bake-ui round-trip."
audience: agent
loaded_by: on-demand
created_at: 2026-05-14
status: seed-for-design-explore
related_docs:
  - docs/ui-toolkit-parity-recovery-plan.html
  - docs/explorations/ui-as-code-state-of-the-art-2026-05.md
  - docs/explorations/ui-panel-tree-db-storage.md
  - docs/explorations/ui-bake-pipeline-hardening-v2.md
related_decisions:
  - DEC-A24  # legacy prefab bake path
  - DEC-A28  # UI Toolkit strangler
related_tech_tickets:
  - TECH-34678  # UxmlEmissionService walk panel_child tree → nested VisualElements
  - TECH-34679  # UxmlEmissionService emit per-child USS rules from params_json + token resolution
  - TECH-34680  # UxmlEmissionService emit hover/active/focused state classes + pseudo-class rules
  - TECH-34681  # DB-canonical TSS theme emitter — replace hand-authored cream.tss / dark.tss
  - TECH-34682  # unity:bake-ui dispatcher invokes UxmlBakeHandler alongside UiBakeHandler
  - TECH-34683  # DB schema for programmatic / runtime-spawned VE surfaces
  - TECH-34684  # DB schema rewrite — panel_child rows match iter-43 UI Toolkit structure per slug
  - TECH-34685  # Host Q-lookup rewrite per cutover slug — DB-emitted UXML names match Host bindings
  - TECH-34686  # ui_def_drift_scan extends to triple-output drift (UXML + USS + TSS) vs DB
---

# UI Toolkit emitter parity + DB reverse capture — seed for design-explore

## §1. Background

The asset pipeline target is **DB-canonical end-to-end**: web auth → REST → DB → bake → game UI. The UI Toolkit recovery (recovery plan §1–§15.4) shipped iter-43 — the current-branch visual is the accepted contract. The recovery plan stands as the historical authoring record; the goal of this exploration is for the **DB row state to truthfully reflect what ships** so future ingest paths (Figma, Claude Design, A2UI) target a DB that mirrors shipping reality + so `unity:bake-ui` round-trips without divergence.

## §2. Pre-flight audit (5 reality checks, verbatim)

1. `npm run unity:bake-ui` dispatches `bake_ui_from_ir` → `UiBakeHandler.Bake` → **prefabs** at `Assets/UI/Prefabs/Generated/` (legacy uGUI DEC-A24 path). It does NOT produce UXML/USS at `Assets/UI/Generated/`.

2. `UxmlBakeHandler` + `UxmlEmissionService` (DEC-A28 sidecar) exist but are **not wired into `unity:bake-ui`**. The emitter is a 22-line shell stub:
   - `BuildUxml(row)` emits only outer `<ui:VisualElement name="{slug}" class="{slug}"/>` — no children.
   - `BuildUss(row)` emits only outer `.{slug} { bg + color + padding }` — no per-child rules.

3. DB has 13 published panels with `panel_child` rows describing the **legacy uGUI prefab structure**. Example: hud-bar DB has 14 children (play-pause-button / speed-cycle-button / new-game-button / city-name-label / sim-date-readout / population-readout / zoom-in/out / budget-button / save-button / load-button / stats-button / auto-button / map-button) — different layout than iter-43 disk UXML (pause / btn-speed1/2/3 / hud-city-name / hud-pop / hud-money / hud-zoom-in/out / hud-stats / hud-auto / hud-map / hud-budget).

4. Hosts (`HudBarHost`, `BudgetPanelHost`, `MapPanelHost`, etc.) `Q<Button>("hud-pause")` style — bound to iter-43 disk UXML names, NOT DB slugs. Rewriting DB to match would orphan the prefab pipeline; rewriting Hosts would break iter-43 click wiring.

5. Runtime-only iter-43 surfaces (`map-panel` runtime VE iter-39, `hover-info` card iter-28) have **no UXML/USS files at all** — pure C# programmatic VE construction in Hosts.

## §3. Three-path decision matrix

| Path | Cost | Outcome |
|---|---|---|
| **A — Lock + document only** | days | DB stays prefab-canonical; UI Toolkit world stays hand-authored; parity deferred behind emitter tickets. |
| **B — Rewrite DB to mirror iter-43** | weeks | DB becomes iter-43-canonical for cutover slugs; prefab path retired per slug. |
| **C — Two-tree DB** | medium | **REJECTED** at DEC-A28 Q2 (schema churn cost — `target_renderer` column rejected in favour of strangler invariant per slug). |

## §4. Path A status — locked at iter-43 checkpoint

- Pixel goldens committed under `tools/visual-baseline/golden/` (6 surfaces: HUD baseline, stats panel, budget panel, pause menu, subtype picker, MainMenu newgame form).
- TECH backlog tickets filed for emitter capability gaps: **TECH-34678, TECH-34679, TECH-34680, TECH-34681, TECH-34682, TECH-34683, TECH-34684, TECH-34685, TECH-34686**.
- Recovery plan §15.4.3 documents Path A closeout.

## §5. Path B problem statement

Extend the DEC-A28 emitter sidecar (`UxmlBakeHandler` / `UxmlEmissionService` / `TssEmissionService`) to full parity with the legacy `UiBakeHandler` scope (~7300 LOC across 4 partials) for UXML/USS/TSS output. Migrate DB schema from prefab-shape to UI Toolkit-shape per slug. Rewrite Hosts to Q-lookup against DB-emitted slugs. Retire DEC-A24 prefab path per migrated slug.

**Concrete capability surface needed** (from TECH ticket bundle):
- Tree walker emits nested `<ui:VisualElement>` / `<ui:Button>` / `<ui:Label>` / `<ui:Slider>` / `<ui:Toggle>` / `<ui:DropdownField>` / `<ui:ScrollView>` / `<ui:TextField>` / `<ui:IntegerField>` per `panel_child` row.
- Per-child USS rule emit from `params_json` + token resolution (literal hex per plan-scope rule).
- USS state classes (`:hover`, `--active`, `--selected`) + pseudo-class rules + transitions.
- TSS theme emitter replaces hand-authored `cream.tss` / `dark.tss` from `token_detail` rows grouped by theme.
- `unity:bake-ui` dispatches both `UiBakeHandler` (prefab) AND `UxmlBakeHandler` (UXML/USS) — feature-flag per slug for staged cutover.
- DB schema decision for programmatic / runtime-spawned VE surfaces (`map-panel`, `hover-info`).
- DB schema rewrite per iter-43 surface so `panel_child` mirrors iter-43 UXML structure.
- Host Q-lookup rewrites per cutover slug.
- `ui_def_drift_scan` extends to triple-output drift (UXML + USS + TSS).

## §6. Open design questions (grill targets for /design-explore)

- **Cutover unit**: per panel? per scene? big-bang? Staged per slug (DEC-A28 strangler) suggests per-panel; concrete order TBD (likely toolbar/hud first → modals → main-menu sub-views).
- **Host rewrite**: in-place vs new Host per slug + retire old? `HudBarHost` already has iter-43-bound Q-lookups; do those stay (DB child slug aliasing) or do Hosts get rewritten atomically with the DB cutover?
- **DB schema**: extend existing `panel_detail` + `panel_child` (adding `node_kind`, `uss_class[]`, `style_props_json`?) or new `panel_node_uxml` tree table? Trade-off: schema churn vs row-shape duplication.
- **Emitter coverage**: does it need to emit USS animations? Hover/active state classes? Programmatic VEs from C#? Runtime-spawned surfaces? What's the minimum for parity vs the maximum for future ingest paths (Figma / A2UI)?
- **Source of truth for tokens**: `cream.tss` / `dark.tss` files (current state — hand-authored) vs DB `token_detail` rows + TSS emitter (DEC-A28 Q5 / I1 envisioned)? Bootstrap via reverse-import the existing TSS into `token_detail`?
- **Runtime-only surfaces**: do `hover-info` / `map-panel` (runtime VE iter-39+42) enter DB at all, or stay code-canonical with Host as source of truth + emitter aware to skip them?
- **Strangler retirement**: when does the prefab pipeline (`UiBakeHandler*.cs` ~7300 LOC) get deleted? Per-slug or end-of-migration? What's the rollback path if a cutover panel regresses?
- **Drift gate**: does `ui_def_drift_scan` extend to triple-output (UXML + USS + TSS) drift in a single invocation, or split into three gates wired into `validate:all`?
- **Asset-pipeline web side**: what REST surface does Path B need to expose for designer ingest? Auth flow + endpoints + payload shapes? (Deferred to a downstream exploration, but flagged here.)
- **Visual baseline diff workflow**: who owns `ui_visual_diff_run` regressions when a Path B emitter change touches an iter-43 surface? CI gate or human gate?
- **Host C# coupling**: per DEC-A28 invariant I4, Hosts stay outside the bake pipeline. But Q-lookup names are bake-pipeline-coupled. How does the renaming flow avoid violating I4?
- **Test surface**: red-stage tests for emitter parity — per-panel pixel-diff harness? Snapshot UXML/USS string-compare?

## §7. Out of scope for the Path B master plan

- **Figma / Claude Design / A2UI ingest** — separate exploration once DB is canonical for the shipping UI.
- **Web `asset-pipeline` REST endpoints** — separate plan; depends on which DB surface is exposed after Path B.
- **uGUI legacy adapter retirement** beyond what DEC-A28 already scopes (the per-slug strangler invariant covers cutover; full removal of `UiBakeHandler*.cs` is a closeout-after-Path-B concern).
- **New visual changes** — Path B is parity preservation, not parity extension. Any new visual lives in a follow-up plan.

## §8. Cross-refs

- `docs/ui-toolkit-parity-recovery-plan.html` §15.4 (iter-43 checkpoint) + §15.4.3 (Path A closeout — audit + 3-path matrix + ticket list + Path B handoff).
- `docs/explorations/ui-as-code-state-of-the-art-2026-05.md` §2 (audit of current pipeline) + §4.9 (`ui_panel_get` tree extension) + §4.8 (visual baseline).
- `docs/explorations/ui-panel-tree-db-storage.md` (panel-tree DB authoring patterns).
- `docs/explorations/ui-bake-pipeline-hardening-v2.md` (bake pipeline + runtime contract).
- `ia/specs/architecture/decisions.md` DEC-A24 (legacy prefab bake) + DEC-A28 (UI Toolkit strangler).
- Memory feedback: `feedback_db_primary_pivot`, `feedback_ui_bake_prefab_rebake`.
- TECH tickets: TECH-34678..TECH-34686 (emitter capability + DB schema + Host rewrite + drift scan extensions).
