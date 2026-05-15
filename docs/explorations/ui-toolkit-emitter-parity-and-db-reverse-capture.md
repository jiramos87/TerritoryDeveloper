---
# --- canonical handoff frontmatter (validate-handoff-schema.mjs) ---
slug: ui-toolkit-emitter-parity-and-db-reverse-capture
target_version: 1
parent_plan_id: null
notes: "Toolbar-pilot reverse capture + side-by-side bake + cutover (DEC-A28 amendment). Path B + Plan C strangler; six-stage loop §5.2 + retro §5.4. 22 task ids: 9 reuse (TECH-34678..86), 13 new (TECH-836..848), 4 net new for Stages 5.0/6.0 (TECH-849..852). Stage 1.0 design_only, 2.0 visibility_delta, 3.0 unit, 4.0 tracer_verb, 5.0/6.0 design_only."

# --- seed keys preserved (informational; validator ignores unknown top-level) ---
purpose: "Seed doc for design-explore — toolbar-pilot reverse capture + side-by-side bake + cutover, then tool/skill improvement; remaining 12 panels covered by follow-up plan."
audience: agent
loaded_by: on-demand
created_at: 2026-05-14
updated_at: 2026-05-15
status: design-expanded
pilot_slug: toolbar
strategy: per-slug strangler (Plan C) — clone-and-compare side-by-side; cutover only when comparison gate green
follow_up_scope: remaining 12 panels (hud-bar, pause-menu, budget-panel, stats-panel, info-panel, map-panel, main-menu, new-game-form, save-load-view, settings-view, notifications-toast, tool-subtype-picker)
related_docs:
  - docs/ui-toolkit-parity-recovery-plan.html
  - docs/explorations/ui-as-code-state-of-the-art-2026-05.md
  - docs/explorations/ui-panel-tree-db-storage.md
  - docs/explorations/ui-bake-pipeline-hardening-v2.md
  - docs/ui-bake-pipeline-rollout-plan.md
  - docs/ui-toolkit-migration-completion-plan.md
  - docs/ui-toolkit-migration-completion-followup-plan.md
related_decisions:
  - DEC-A24
  - DEC-A28
related_tech_tickets:
  - TECH-34678
  - TECH-34679
  - TECH-34680
  - TECH-34681
  - TECH-34682
  - TECH-34683
  - TECH-34684
  - TECH-34685
  - TECH-34686
  - TECH-836
  - TECH-837
  - TECH-838
  - TECH-839
  - TECH-840
  - TECH-841
  - TECH-842
  - TECH-843
  - TECH-844
  - TECH-845
  - TECH-846
  - TECH-847
  - TECH-848
  - TECH-849
  - TECH-850
  - TECH-851
  - TECH-852

# --- stages array (ship-plan Phase 1 input) ---
stages:
  - id: "1.0"
    title: "Reverse capture — read iter-43 toolbar surface → DB migration draft"
    exit: "panel_detail.toolbar + panel_child[] rows mirror iter-43 UXML structure; migration applied; unity:bake-ui still emits legacy prefab unchanged."
    red_stage_proof: "Migration file present at tools/mcp-ia-server/migrations/{n}-panel-child-tree-shape.sql; SELECT count from panel_child WHERE panel_id=100 returns 12 (root + grid + 9 tiles + active-tool-label); token_detail cream theme rows seeded."
    red_stage_proof_block:
      red_test_anchor: "tools/mcp-ia-server/migrations/{n}-panel-child-tree-shape.sql::up"
      target_kind: "design_only"
      proof_artifact_id: "panel_child_tree_shape_migration_applied"
      proof_status: "failed_as_expected"
    tasks:
      - id: "TECH-836"
        title: "Schema extension migration — add node_kind, uss_class[], style_props_json columns to panel_child"
        prefix: "TECH"
        kind: "code"
        digest_outline: "SQL migration: ALTER TABLE panel_child ADD COLUMN IF NOT EXISTS node_kind text, ADD COLUMN IF NOT EXISTS uss_class text[], ADD COLUMN IF NOT EXISTS style_props_json jsonb. Idempotent; legacy rows null-default. Resolves LD-9 (schema extension vs new tree table)."
        touched_paths:
          - "tools/mcp-ia-server/migrations/"
      - id: "TECH-837"
        title: "Reverse-capture parser script — toolbar.uxml + toolbar.uss + ToolbarHost.cs → SQL INSERT plan"
        prefix: "TECH"
        kind: "code"
        digest_outline: "Node script parsing Assets/UI/Generated/toolbar.uxml (9-tile nested grid) + toolbar.uss (cream literals) + ToolbarHost.cs TileSlugs[] → emit panel_child INSERT block + token_detail cream rows. Tokens: bg #f5e6c8, border #b89b5e, tile-active #5b7fa8, etc."
        touched_paths:
          - "tools/scripts/ui-toolkit-reverse-capture.mjs"
          - "Assets/UI/Generated/toolbar.uxml"
          - "Assets/UI/Generated/toolbar.uss"
          - "Assets/Scripts/UI/Hosts/ToolbarHost.cs"
        depends_on:
          - "TECH-836"
      - id: "TECH-838"
        title: "Apply reverse-capture migration — write panel_detail.toolbar v616 + 12 panel_child rows"
        prefix: "TECH"
        kind: "mcp-only"
        digest_outline: "Run reverse-capture INSERT block via MCP db migration. panel_detail.toolbar id 100 v615 superseded by v616 with iter-43 nested-VE shape. 12 panel_child rows: toolbar root + toolbar-grid + 9 tile Buttons + active-tool-label. token_detail cream theme seeded."
        touched_paths:
          - "tools/mcp-ia-server/migrations/"
        depends_on:
          - "TECH-837"
      - id: "TECH-839"
        title: "Confirm legacy prefab path untouched post-migration"
        prefix: "TECH"
        kind: "code"
        digest_outline: "Run npm run unity:bake-ui; verify legacy UiBakeHandler still emits prefab for non-toolbar panels; toolbar UXML unchanged on disk (no .baked yet — Stage 2.0 builds it). Smoke gate before Stage 2.0 entry."
        touched_paths:
          - "Assets/UI/Prefabs/Generated/"
          - "Assets/Scripts/Editor/Bridge/UiBakeHandler.cs"
        depends_on:
          - "TECH-838"

  - id: "2.0"
    title: "Sidecar bake — extend UxmlEmissionService + TssEmissionService for toolbar element set"
    exit: "npm run unity:bake-ui emits toolbar.baked.uxml + toolbar.baked.uss + cream.baked.tss alongside iter-43 originals (no clobber); validate:all green."
    red_stage_proof: "toolbar.baked.uxml present at Assets/UI/Generated/ with >12 child <ui:*> nodes; toolbar.baked.uss carries per-class selectors + :hover pseudo-class; cream.baked.tss carries :root token block; iter-43 toolbar.uxml/toolbar.uss/cream.tss unchanged."
    red_stage_proof_block:
      red_test_anchor: "Assets/Scripts/Editor/Bridge/UxmlEmissionService.cs::BuildUxml"
      target_kind: "visibility_delta"
      proof_artifact_id: "toolbar_baked_uxml_uss_tss_emitted_sidecar"
      proof_status: "failed_as_expected"
    tasks:
      - id: "TECH-34678"
        title: "UxmlEmissionService.BuildUxml tree walker — walk panel_child tree → nested VisualElements"
        prefix: "TECH"
        kind: "code"
        digest_outline: "Replace 22-line shell BuildUxml(row) with recursive walker over panel_child tree (DFS by parent_id + ordinal). Emit <ui:{node_kind} name='{slug}' class='{uss_class join space}'/> per row. Output path: {outDir}/{slug}.baked.uxml. Signature: BuildUxml(PanelRow, IReadOnlyList<PanelChildRow>)."
        touched_paths:
          - "Assets/Scripts/Editor/Bridge/UxmlEmissionService.cs"
        depends_on:
          - "TECH-839"
      - id: "TECH-34679"
        title: "UxmlEmissionService.BuildUss per-child rule emit — params_json + token resolution + state classes"
        prefix: "TECH"
        kind: "code"
        digest_outline: "Iterate panel_child rows; emit .{uss_class} block per class from style_props_json. Resolve token refs via token_detail lookup (cream theme); inline literal hex per A5. Emit :hover / .--active pseudo-class blocks from style_props_json.states. Covers TECH-34680 state-class scope inline."
        touched_paths:
          - "Assets/Scripts/Editor/Bridge/UxmlEmissionService.cs"
        depends_on:
          - "TECH-34678"
      - id: "TECH-34681"
        title: "TSS emitter — DB-canonical theme emit replaces hand-authored cream.tss / dark.tss"
        prefix: "TECH"
        kind: "code"
        digest_outline: "Group token_detail rows by theme; emit :root { --ds-color-bg-card: #313244; ... } per theme. Output: Assets/UI/Themes/cream.baked.tss + dark.baked.tss. TssEmitter.cs + TssEmissionService.cs."
        touched_paths:
          - "Assets/Scripts/Editor/Bridge/TssEmitter.cs"
          - "Assets/Scripts/Editor/Bridge/TssEmissionService.cs"
        depends_on:
          - "TECH-838"
      - id: "TECH-34682"
        title: "unity:bake-ui dispatcher wiring — invoke UxmlBakeHandler alongside UiBakeHandler"
        prefix: "TECH"
        kind: "code"
        digest_outline: "npm run unity:bake-ui after UiBakeHandler.Bake invokes UxmlBakeHandler.Bake for toolbar slug. Per-slug feature-flag table (bake_pipeline_flag) governs routing. Initial state: {toolbar: uxml, *: prefab}."
        touched_paths:
          - "Assets/Scripts/Editor/Bridge/UxmlBakeHandler.cs"
          - "tools/scripts/unity-bake-ui.mjs"
        depends_on:
          - "TECH-34679"
          - "TECH-34681"

  - id: "3.0"
    title: "Compare + iterate — extend ui_def_drift_scan to triple-output diff; run four gates"
    exit: "All four §5.3 gates green for toolbar; iterate loop closed; emitter fixes documented in stage journal."
    red_stage_proof: "tools/scripts/toolbar-cutover-gate.mjs run produces JSON with all four gates PASS: uxml-ast byte-equal, uss-selector-rule equal, pixel-diff ≤2%, host-q-lookup 11/11 resolved."
    red_stage_proof_block:
      red_test_anchor: "tools/scripts/toolbar-cutover-gate.mjs::runFourGates"
      target_kind: "unit"
      proof_artifact_id: "toolbar_four_gate_runner_all_pass"
      proof_status: "failed_as_expected"
    tasks:
      - id: "TECH-34686"
        title: "ui_def_drift_scan triple-output extension — UXML AST + USS selector + TSS variable"
        prefix: "TECH"
        kind: "mcp-only"
        digest_outline: "Extend MCP tool: mode: uxml|uss|tss|all. UXML AST diff (whitespace strip + attribute reorder); USS selector-grouped {selector:{prop:value}} bidirectional diff; TSS :root var map compare. Structured diff JSON naming offending node/rule/var."
        touched_paths:
          - "tools/mcp-ia-server/src/tools/ui-def-drift-scan.ts"
        depends_on:
          - "TECH-34682"
      - id: "TECH-840"
        title: "Four-gate runner script — aggregate UXML + USS + TSS + pixel + host-q-lookup gates"
        prefix: "TECH"
        kind: "code"
        digest_outline: "Script calls ui_def_drift_scan (UXML, USS, TSS) + ui_visual_diff_run (toolbar baseline ≤2%) + ui_toolkit_host_lint (every ToolbarHost.Q resolves against toolbar.baked.uxml). Aggregate pass/fail; per-gate diagnostic."
        touched_paths:
          - "tools/scripts/toolbar-cutover-gate.mjs"
        depends_on:
          - "TECH-34686"
      - id: "TECH-841"
        title: "Iterate loop journal — record each iteration's fix path (emitter vs DB row)"
        prefix: "TECH"
        kind: "doc-only"
        digest_outline: "Append-only journal at docs/explorations/ui-toolkit-emitter-parity-and-db-reverse-capture-iterate-journal.md. Each entry: iteration #, failing gate, diagnostic, fix path (emitter / DB row), file touched. NON-BLOCKING-1 risk surface — flag at iteration > 20."
        touched_paths:
          - "docs/explorations/ui-toolkit-emitter-parity-and-db-reverse-capture-iterate-journal.md"
        depends_on:
          - "TECH-840"

  - id: "4.0"
    title: "Cutover — atomic single commit: rename .baked → canonical + Host Q-rewrite + adapter delete + Play Mode smoke"
    exit: "Play Mode toolbar renders + clicks route; ToolbarParityTest green; second unity:bake-ui produces zero git diff (idempotence)."
    red_stage_proof: "ToolbarParityTest.cs ToolbarRendersAndClicksRoute method asserts toolbar VE present + 9 tile buttons + click on tool-residential sets ToolbarVM.ActiveTool='residential'. Green after cutover commit."
    red_stage_proof_block:
      red_test_anchor: "Assets/Scripts/Tests/UI/Toolbar/ToolbarParityTest.cs::ToolbarRendersAndClicksRoute"
      target_kind: "tracer_verb"
      proof_artifact_id: "toolbar_play_mode_render_and_click_route"
      proof_status: "failed_as_expected"
    tasks:
      - id: "TECH-842"
        title: "Rename .baked → canonical — overwrite iter-43 toolbar.uxml/.uss + cream.tss"
        prefix: "TECH"
        kind: "code"
        digest_outline: "mv toolbar.baked.uxml toolbar.uxml + mv toolbar.baked.uss toolbar.uss + mv cream.baked.tss cream.tss (+ dark.baked.tss if touched). Banner injected: '<!-- Generated by TssEmitter — do not hand-edit -->' on TSS (NON-BLOCKING-5 mitigation)."
        touched_paths:
          - "Assets/UI/Generated/toolbar.uxml"
          - "Assets/UI/Generated/toolbar.uss"
          - "Assets/UI/Themes/cream.tss"
        depends_on:
          - "TECH-841"
      - id: "TECH-34685"
        title: "Host Q-rewrite — ToolbarHost.cs TileSlugs[] + _btns Q strings match DB-emitted slugs"
        prefix: "TECH"
        kind: "code"
        digest_outline: "Edit ToolbarHost.cs TileSlugs[] literal + _btns Q strings to match DB-emitted slugs (if Stage 3.0 forced rename). Class name + file path frozen; namespace frozen; [SerializeField] _doc + _subtypePicker frozen. XML doc ≤1 line. Atomic with TECH-842."
        touched_paths:
          - "Assets/Scripts/UI/Hosts/ToolbarHost.cs"
        depends_on:
          - "TECH-842"
      - id: "TECH-843"
        title: "ToolbarDataAdapter + ToolbarAdapterService delete — gated on zero references"
        prefix: "TECH"
        kind: "code"
        digest_outline: "git rm Assets/Scripts/UI/Toolbar/ToolbarDataAdapter.cs + git rm Assets/Scripts/Domains/UI/Services/ToolbarAdapterService.cs IFF grep across Assets/Scripts/ shows zero references post-Host-rewrite."
        touched_paths:
          - "Assets/Scripts/UI/Toolbar/ToolbarDataAdapter.cs"
          - "Assets/Scripts/Domains/UI/Services/ToolbarAdapterService.cs"
        depends_on:
          - "TECH-34685"
      - id: "TECH-844"
        title: "Play Mode smoke — toolbar renders + clicks route scenario"
        prefix: "TECH"
        kind: "code"
        digest_outline: "npm run db:bridge-playmode-smoke scenario toolbar-renders-and-clicks-route. Bridge sequence: load CityScene → enter Play Mode → screenshot toolbar region → click each tile slug → assert ToolbarVM.ActiveTool matches → exit Play Mode."
        touched_paths:
          - "Assets/Scripts/Tests/UI/Toolbar/ToolbarParityTest.cs"
        depends_on:
          - "TECH-843"
      - id: "TECH-845"
        title: "Idempotence verify — second unity:bake-ui produces zero git diff"
        prefix: "TECH"
        kind: "code"
        digest_outline: "Run npm run unity:bake-ui twice; git diff after second bake = zero. ToolbarParityTest green. Confirms DB → emitter → disk is deterministic."
        touched_paths:
          - "Assets/UI/Generated/toolbar.uxml"
          - "Assets/UI/Generated/toolbar.uss"
        depends_on:
          - "TECH-844"

  - id: "5.0"
    title: "Tool & skill improvement (§5.4 retro) — file emitter gap tickets + ship reverse-capture MCP + author skill + amend DEC-A28"
    exit: "/ui-toolkit-slug-cutover skill runnable end-to-end on a dry-run second slug (e.g. pause-menu) with NO commit; DEC-A28 amendment merged."
    red_stage_proof: "ia/skills/ui-toolkit-slug-cutover/SKILL.md present with frontmatter input {SLUG}; .claude/commands/ui-toolkit-slug-cutover.md + .claude/agents/ui-toolkit-slug-cutover.md regenerated; ia/specs/architecture/decisions.md carries dec-a28-toolbar-cutover-playbook-amendment row."
    red_stage_proof_block:
      red_test_anchor: "ia/skills/ui-toolkit-slug-cutover/SKILL.md::guardrails"
      target_kind: "design_only"
      proof_artifact_id: "ui_toolkit_slug_cutover_skill_present_and_dec_a28_amended"
      proof_status: "failed_as_expected"
    tasks:
      - id: "TECH-846"
        title: "File TECH tickets for emitter gaps surfaced in Stage 3.0 iterate loop"
        prefix: "TECH"
        kind: "doc-only"
        digest_outline: "Per Stage 3.0 journal, file TECH-XXXX for each unfixed gap touching follow-up panels (e.g. <ui:Slider> emit if pause-menu needs it; not in toolbar scope). Yaml under ia/backlog/."
        touched_paths:
          - "ia/backlog/"
        depends_on:
          - "TECH-845"
      - id: "TECH-847"
        title: "ui_toolkit_reverse_capture MCP slice — (uxml_path, uss_path, host_path) → SQL migration draft"
        prefix: "TECH"
        kind: "mcp-only"
        digest_outline: "Conditional — ship only if Stage 1.0 manual reverse-capture took >1 day. MCP tool taking (uxml_path, uss_path, host_path) → emits SQL migration draft. Saves cost for 12 follow-up slugs. Lowers cost from days to hours per panel."
        touched_paths:
          - "tools/mcp-ia-server/src/tools/ui-toolkit-reverse-capture.ts"
        depends_on:
          - "TECH-846"
      - id: "TECH-848"
        title: "/ui-toolkit-slug-cutover skill — encode Stages 1.0–4.0 + four gates + rollback path"
        prefix: "TECH"
        kind: "doc-only"
        digest_outline: "Author ia/skills/ui-toolkit-slug-cutover/SKILL.md + agent-body.md. Frontmatter: input {SLUG}. Run npm run skill:sync:all so .claude/commands/ + .claude/agents/ regenerate. Hard boundary: STOP on divergent slug shape (e.g. runtime-VE-only hover-info)."
        touched_paths:
          - "ia/skills/ui-toolkit-slug-cutover/SKILL.md"
          - "ia/skills/ui-toolkit-slug-cutover/agent-body.md"
          - ".claude/commands/ui-toolkit-slug-cutover.md"
          - ".claude/agents/ui-toolkit-slug-cutover.md"
        depends_on:
          - "TECH-847"
      - id: "TECH-849"
        title: "DEC-A28 amendment merge — dec-a28-toolbar-cutover-playbook-amendment row"
        prefix: "TECH"
        kind: "doc-only"
        digest_outline: "Append amendment row to ia/specs/architecture/decisions.md (table-shape per DEC-A17). MCP arch_decision_write (status=active) + cron_arch_changelog_append_enqueue (kind=design_explore_decision). 7 binding clauses A1..A7 per Architecture Decision block above."
        touched_paths:
          - "ia/specs/architecture/decisions.md"
        depends_on:
          - "TECH-848"

  - id: "6.0"
    title: "Validate + follow-up seed — verify:local idempotent; author followup-12-panels exploration; close-readiness signal"
    exit: "Follow-up exploration doc written + linked from this doc §7; master plan ready for /master-plan-close on toolbar pilot."
    red_stage_proof: "verify:local chain green (validate:all + unity:compile-check + db:migrate + db:bridge-preflight + Editor save/quit + db:bridge-playmode-smoke); docs/explorations/ui-toolkit-emitter-parity-followup-12-panels.md present + linked from §7."
    red_stage_proof_block:
      red_test_anchor: "docs/explorations/ui-toolkit-emitter-parity-followup-12-panels.md::stage-outline"
      target_kind: "design_only"
      proof_artifact_id: "followup_12_panels_seed_written_and_linked"
      proof_status: "failed_as_expected"
    tasks:
      - id: "TECH-850"
        title: "Full verify:local chain — confirm validate:all + unity:bake-ui idempotent"
        prefix: "TECH"
        kind: "code"
        digest_outline: "npm run verify:local: validate:all + unity:compile-check + db:migrate + db:bridge-preflight + Editor save/quit + db:bridge-playmode-smoke. Re-run unity:bake-ui twice; confirm zero git diff."
        touched_paths:
          - "tools/scripts/verify-local.mjs"
        depends_on:
          - "TECH-849"
      - id: "TECH-851"
        title: "Follow-up exploration seed — 12 deferred panels + per-panel risk score + invocation order"
        prefix: "TECH"
        kind: "doc-only"
        digest_outline: "Author docs/explorations/ui-toolkit-emitter-parity-followup-12-panels.md. List 12 deferred slugs (hud-bar, pause-menu, budget-panel, stats-panel, info-panel, map-panel, main-menu, new-game-form, save-load-view, settings-view, notifications-toast, tool-subtype-picker) + per-panel risk score (blast radius + structural complexity + state class count) + per-slug invocation order. Schema decision (LD-9) reused. Link from this doc §7."
        touched_paths:
          - "docs/explorations/ui-toolkit-emitter-parity-followup-12-panels.md"
          - "docs/explorations/ui-toolkit-emitter-parity-and-db-reverse-capture.md"
        depends_on:
          - "TECH-850"
      - id: "TECH-852"
        title: "Master plan close-readiness signal — write closeout marker for /master-plan-close"
        prefix: "TECH"
        kind: "mcp-only"
        digest_outline: "MCP master_plan_state_write status=close-ready for slug ui-toolkit-emitter-parity-and-db-reverse-capture. Signals ship-final → master-plan-close transition. Final stage exit."
        touched_paths: []
        depends_on:
          - "TECH-851"
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

## §5.1 First cutover slug — `toolbar` (locked)

This master plan scopes ONLY the `toolbar` slug as the pilot per Plan C (per-slug strangler). Rationale:

- **Lowest blast radius** — toolbar is a single HUD strip; failure to bake doesn't lock the user out of game like main-menu / pause-menu would.
- **Test scaffold already exists** — `Assets/Scripts/Tests/UI/Toolbar/ToolbarParityTest.cs` ready as red-stage anchor.
- **Hosts/VM already present** — `Assets/Scripts/UI/Hosts/ToolbarHost.cs` + `Assets/Scripts/UI/ViewModels/ToolbarVM.cs` + `Assets/Scripts/UI/Toolbar/ToolbarDataAdapter.cs` + `Assets/Scripts/Domains/UI/Services/ToolbarAdapterService.cs` — Host Q-rewrite scoped to one class.
- **DB row exists** — `panel_detail` slug `toolbar` id 100, published v615 — `panel_child` rows present (legacy prefab-shape).
- **Disk artifacts small** — `toolbar.uxml` 26 LOC + `toolbar.uss` 59 LOC = easy to diff structure during reverse capture.
- **Already cited as Track A debt** — `docs/ui-bake-pipeline-rollout-plan.md` Track A flags toolbar as the DB-first invariant violation needing repair.

The remaining 12 panels (hud-bar, pause-menu, budget-panel, stats-panel, info-panel, map-panel, main-menu, new-game-form, save-load-view, settings-view, notifications-toast, tool-subtype-picker) are explicitly **out of scope** of this plan — they get their own exploration + master plan after toolbar pilot closes (see §7).

## §5.2 Side-by-side clone-and-compare strategy

This plan does NOT delete the existing iter-43 hand-authored `toolbar.uxml` / `.uss` until DB-baked clone is proven byte/visually equal. Six phases:

1. **Reverse capture** — read iter-43 `toolbar.uxml` + `toolbar.uss` + Host Q-lookup names + scene rect_json → write DB migration that rewrites `panel_detail.toolbar` + `panel_child[]` rows to mirror iter-43 structure. DB row state now describes shipping reality.
2. **Sidecar bake** — extend `UxmlEmissionService` + `TssEmissionService` (DEC-A28 sidecar) to emit a SECOND set of artifacts under `Assets/UI/Generated/toolbar.baked.uxml` + `toolbar.baked.uss` (suffix `.baked` to avoid clobbering iter-43 originals).
3. **Compare** — run `ui_def_drift_scan` extension (TECH-34686) that diffs `toolbar.uxml` ↔ `toolbar.baked.uxml` + `toolbar.uss` ↔ `toolbar.baked.uss` + pixel-diff render(baked) ↔ render(iter-43) ≤ tolerance.
4. **Iterate** — when drift found, either (a) fix emitter (preferred — improves tool for next slug) or (b) adjust DB row (when iter-43 was non-canonical). Loop until §5.3 gate green.
5. **Cutover** — rename `toolbar.baked.uxml` → `toolbar.uxml` (overwrite iter-43); delete `.baked` artifacts; rewrite `ToolbarHost` Q-lookup names to match DB-emitted node names (if drift required name changes); delete `ToolbarDataAdapter.cs` if Host no longer needs it; verify Play Mode toolbar renders + clicks route.
6. **Validate** — `unity:bake-ui` regenerates `toolbar.uxml` deterministically from DB; `git diff` shows zero drift after second bake; `ui_def_drift_scan` green; `ToolbarParityTest` green.

Rollback path — at any step before cutover, delete `.baked.*` artifacts; iter-43 toolbar unchanged.

## §5.3 Comparison gate — definition of "equal"

The cutover step (§5.2 phase 5) blocks until ALL four gates pass:

| Gate | Tool | Tolerance |
|---|---|---|
| **UXML structure-equal** | `ui_def_drift_scan` (TECH-34686) — normalized AST diff | byte-equal after whitespace + attribute-order normalization |
| **USS rule-equal** | `ui_def_drift_scan` — selector-grouped property diff | every iter-43 rule present in baked + no extra baked rules |
| **Pixel-diff ≤ tolerance** | `ui_visual_diff_run` MCP — render both panels under same `PanelSettings` reference resolution | ≤ 2% pixel delta vs golden (matches existing pixel-diff harness tolerance) |
| **Host Q-lookup pass** | `ui_toolkit_host_lint` MCP — every Host `Q<T>("name")` resolves against baked UXML names | zero unresolved lookups |

Failures route back to §5.2 phase 4 iterate loop. Each gate failure produces a diagnostic that names the offending node/rule/pixel-region/lookup so the emitter fix (or DB row fix) is mechanical.

## §5.4 Tool & skill improvement phase (post-cutover)

After toolbar cutover lands, run a dedicated retro stage that distills lessons into reusable tooling so the follow-up plan (remaining 12 panels) is mechanical:

- **Emitter coverage gaps** found during toolbar iterate loop → file TECH tickets + extend `UxmlEmissionService` (e.g. missing element kind, missing USS property emit, token-resolution edge case).
- **`ui_def_drift_scan` extensions** → if a drift class was caught manually that the scanner missed, codify the check (e.g. attribute-order-normalization rule, USS shorthand expansion).
- **New MCP slice if needed** — e.g. `ui_toolkit_reverse_capture` that takes a disk UXML+USS pair + Host class + emits a DB migration draft for the panel_child rows. Lowers cost of next slug from days to hours.
- **New skill `ui-toolkit-slug-cutover`** — formalize the 6-phase §5.2 process as a runnable skill so the follow-up plan invokes `/ui-toolkit-slug-cutover {SLUG}` per panel instead of re-deriving the process.
- **Update DEC-A28** — record the cutover playbook + comparison gate + rollback path as a binding architecture decision amendment so future contributors don't re-derive it.

Exit criteria — tool surface + skill + DEC amendment ready for follow-up plan to invoke per-slug without re-grilling design.

## §6. Open design questions (grill targets for /design-explore)

- **Cutover unit**: LOCKED — `toolbar` only (per §5.1). Remaining 12 panels deferred to follow-up plan. Per-slug strangler invariant per DEC-A28.
- **Host rewrite**: LOCKED — in-place rewrite of `ToolbarHost.Q<T>` names if reverse capture forces a name change. Atomic with cutover (§5.2 phase 5). `ToolbarDataAdapter.cs` deleted if Host no longer needs it.
- **DB schema**: open — extend existing `panel_detail` + `panel_child` (adding `node_kind`, `uss_class[]`, `style_props_json`?) or new `panel_node_uxml` tree table? Trade-off: schema churn vs row-shape duplication. Pilot toolbar tests both via prototype migration.
- **Emitter coverage** for toolbar scope: minimum = `<ui:VisualElement>` + `<ui:Button>` + `<ui:Label>` + per-child USS rules + token resolution + hover/active state classes (toolbar has hover/active styles per current `toolbar.uss`). Animations + sliders + dropdowns deferred to per-slug coverage as remaining panels need them.
- **Source of truth for tokens**: open — pilot decision: reverse-import existing token literals into `token_detail` rows during toolbar capture, emit TSS from DB. Decision binds to follow-up plan.
- **Runtime-only surfaces**: out of scope (toolbar is not runtime-only). Decision deferred to follow-up plan that covers `hover-info` + `map-panel`.
- **Strangler retirement**: out of scope — toolbar cutover retires DEC-A24 path for the `toolbar` slug only. Full `UiBakeHandler*.cs` deletion = closeout concern after all 13 panels migrated.
- **Drift gate**: LOCKED — `ui_def_drift_scan` extends to triple-output (UXML + USS + TSS) in single invocation per §5.3. Wired into `validate:all`.
- **Asset-pipeline web side**: out of scope (see §7).
- **Visual baseline diff workflow**: LOCKED — pilot uses CI gate via `ui_visual_diff_run` per §5.3. Human gate only when CI fails 3 iterate loops in a row.
- **Host C# coupling**: open — pilot decision: Host Q-name rewrites count as IA-level changes, not bake-pipeline changes (DEC-A28 I4 preserved). Capture rule into DEC-A28 amendment per §5.4.
- **Test surface**: LOCKED — `ToolbarParityTest.cs` (already scaffolded) = red-stage anchor; pixel-diff harness reuses existing `ui_visual_diff_run`; snapshot UXML/USS string-compare via extended `ui_def_drift_scan`.

## §7. Out of scope for the toolbar-pilot master plan

- **Remaining 12 panels** (hud-bar, pause-menu, budget-panel, stats-panel, info-panel, map-panel, main-menu, new-game-form, save-load-view, settings-view, notifications-toast, tool-subtype-picker) — covered by a follow-up exploration + master plan authored after toolbar cutover + tool-improvement phase close (§5.4). Follow-up plan reuses the `/ui-toolkit-slug-cutover` skill produced here.
- **Runtime-only surfaces** (`hover-info`, `map-panel` runtime VE) — deferred to follow-up plan (need DB schema decision §6).
- **Figma / Claude Design / A2UI ingest** — separate exploration once DB is canonical for the shipping UI.
- **Web `asset-pipeline` REST endpoints** — separate plan; depends on which DB surface is exposed after toolbar pilot.
- **uGUI legacy adapter retirement** beyond `ToolbarDataAdapter` — full `UiBakeHandler*.cs` retirement = closeout-after-all-panels-migrated concern.
- **New visual changes** — this plan = parity preservation only. Any new visual lives in a follow-up plan.

## §9. Stage outline — toolbar pilot (input to /ship-plan)

Six stages mapped 1:1 to §5.2 phases + §5.4 retro:

| Stage | Title | Exit criterion |
|---|---|---|
| 1.0 | Reverse capture — read iter-43 toolbar surface → DB migration draft | `panel_detail.toolbar` + `panel_child[]` rows mirror iter-43 UXML structure; migration applied; `unity:bake-ui` still emits legacy prefab unchanged |
| 2.0 | Sidecar bake — extend `UxmlEmissionService` + `TssEmissionService` for toolbar element set | `npm run unity:bake-ui` emits `toolbar.baked.uxml` + `toolbar.baked.uss` (alongside iter-43 originals, no clobber); validate:all green |
| 3.0 | Compare + iterate — extend `ui_def_drift_scan` to triple-output diff | All four §5.3 gates green for toolbar; iterate loop closed; emitter fixes documented |
| 4.0 | Cutover — overwrite iter-43 toolbar.uxml/.uss with baked; rewrite `ToolbarHost.Q` names if needed; delete `ToolbarDataAdapter.cs` if obsolete | Play Mode toolbar renders + clicks route; `ToolbarParityTest` green; second bake produces zero git diff |
| 5.0 | Tool & skill improvement (§5.4) — file emitter gap TECH tickets; ship `ui_toolkit_reverse_capture` MCP slice (if needed); author `/ui-toolkit-slug-cutover` skill; amend DEC-A28 | New skill runnable end-to-end on a dry-run second slug (no commit); DEC-A28 amendment merged |
| 6.0 | Validate + follow-up seed — confirm validate:all + `unity:bake-ui` idempotent; write follow-up exploration seed for remaining 12 panels | Follow-up exploration doc written + linked from this doc; plan ready for `/master-plan-close` |

## §8. Cross-refs

- `docs/ui-toolkit-parity-recovery-plan.html` §15.4 (iter-43 checkpoint) + §15.4.3 (Path A closeout — audit + 3-path matrix + ticket list + Path B handoff).
- `docs/explorations/ui-as-code-state-of-the-art-2026-05.md` §2 (audit of current pipeline) + §4.9 (`ui_panel_get` tree extension) + §4.8 (visual baseline).
- `docs/explorations/ui-panel-tree-db-storage.md` (panel-tree DB authoring patterns).
- `docs/explorations/ui-bake-pipeline-hardening-v2.md` (bake pipeline + runtime contract).
- `ia/specs/architecture/decisions.md` DEC-A24 (legacy prefab bake) + DEC-A28 (UI Toolkit strangler).
- Memory feedback: `feedback_db_primary_pivot`, `feedback_ui_bake_prefab_rebake`.
- TECH tickets: TECH-34678..TECH-34686 (emitter capability + DB schema + Host rewrite + drift scan extensions).

---

## Design Expansion

### Chosen Approach

**Path B + Plan C strangler — toolbar pilot, side-by-side clone-and-compare, non-abortable.**

- Per §3 matrix Path B (extend DEC-A28 emitter sidecar to full parity) selected; Path C (two-tree DB) rejected at DEC-A28 Q2; Path A (lock + doc only) closed at iter-43.
- Cutover unit LOCKED to single slug `toolbar` per §5.1 (lowest blast radius; smallest disk artifacts; ToolbarParityTest scaffold present; ToolbarHost is single-class binding; iter-43 DB row already cited as Track A debt).
- Six-phase loop per §5.2: reverse capture → sidecar bake → compare → iterate → cutover → validate.
- Four-gate exit per §5.3 (UXML AST + USS rule + pixel ≤2% + Host Q-lookup) controls cutover.
- **No abort gate.** Phase 4 iterate loop runs open-ended until four gates green (user decision Q4, 2026-05-15). Rollback path (`.baked.*` delete) stays at file level as safety net, not plan-level exit.

#### Locked decisions captured this session

| Id | Question | Decision | Source |
|---|---|---|---|
| LD-1 | Cutover unit | toolbar only; 12 panels deferred to follow-up plan | §5.1 + prior Q1 |
| LD-2 | Host rewrite policy | in-place rewrite of `ToolbarHost.Q<T>` names if emitter forces; atomic with §5.2 phase 5 | §6 + prior Q2 |
| LD-3 | Drift gate surface | `ui_def_drift_scan` extends to triple-output (UXML + USS + TSS); wired into `validate:all` | §6 + §5.3 |
| LD-4 | Visual baseline workflow | CI gate `ui_visual_diff_run`; human only after 3 failed iterates | §6 + prior Q3 |
| LD-5 | Pilot abort contract | none — iterate Phase 4 until §5.3 four-gate green; no retro TECH for "abort"; rollback only at file level | Q4 (2026-05-15) |
| LD-6 | Test surface | existing `ToolbarParityTest.cs` is red-stage anchor | §6 |

#### Open items deferred to expansion (Phase 3/5 below)

- LD-7 (Host C# coupling rule wording for DEC-A28 amendment) — resolved in Architecture Decision section.
- LD-8 (token source-of-truth direction) — resolved in Implementation Points / Stage 1.0 reverse-capture spec.
- LD-9 (panel_child schema extension vs new `panel_node_uxml` tree table) — resolved in Implementation Points / Stage 1.0 schema spec.

### Architecture Decision

**DEC-A28 amendment — UI Toolkit strangler cutover playbook (toolbar pilot binding).**

| Field | Value |
|---|---|
| slug | `dec-a28-toolbar-cutover-playbook-amendment` |
| status | active (amendment to DEC-A28) |
| rationale | Per-slug strangler now has a runnable cutover protocol; future slugs invoke the same six-phase loop. Locks definition of "equal" + rollback path + Host coupling stance so follow-up plan (12 panels) is mechanical. |
| alternatives | new top-level DEC-A30; capture-only-in-skill; no-amendment-protocol-lives-in-plan-doc |
| affected surfaces | `contracts/ui-toolkit-strangler` (existing); `bake-pipeline/uxml-emitter-sidecar` (existing); `validation/ui_def_drift_scan` (existing); NEW `skill/ui-toolkit-slug-cutover` |

**Amendment clauses (binding):**

- **A1. Cutover unit = single slug.** Per-slug strangler; no batch cutover; `target_renderer` column stays rejected.
- **A2. Definition of equal = four gates (UXML AST + USS selector-rule + pixel ≤2% + Host Q-lookup).** All four green before §5.2 phase 5 fires. Failure routes back to phase 4.
- **A3. Iterate cost is non-bounded.** No iteration cap; no abort gate. Phase 4 loops until A2 green. Rollback path lives at file level (`.baked.*` delete; iter-43 untouched).
- **A4. Host C# coupling rule (resolves LD-7 + §6 "Host C# coupling: open").** Host `Q<T>("name")` rewrites are **IA-level** changes, NOT bake-pipeline changes. DEC-A28 invariant I4 (Hosts outside bake pipeline) preserved. Q-rewrites flow through code review same as any C# edit; bake pipeline never writes Host source. Renames batched atomically with §5.2 phase 5 cutover commit so red period is single commit.
- **A5. Token source of truth (resolves LD-8 + §6 "Source of truth for tokens").** During toolbar reverse capture, hand-authored token literals from `toolbar.uss` (background `#f5e6c8`, border `#b89b5e`, tile-active `#5b7fa8`, etc.) are imported into `token_detail` rows (cream theme); per-panel USS keeps literal-hex inline (no `var(--ds-*)` cascade in panel scope). TSS emit consumes `token_detail` rows; panel emit consumes resolved literals. DB → emitter is one-way; emitter never reads disk USS post-cutover.
- **A6. Skill emission.** §5.4 mandates new `ui-toolkit-slug-cutover` skill before follow-up plan; skill encodes phases 1–6 + four gates + rollback. Follow-up plan invokes `/ui-toolkit-slug-cutover {SLUG}` per panel.
- **A7. Drift gate triple-output.** `ui_def_drift_scan` extension covers UXML AST + USS selector-rule + TSS variable diff in single invocation; wired into `validate:all` so drift never reaches a PR review.

**Drift scan (manual carry-over from prior session DEC-A29 work; this amendment touches only UI Toolkit surfaces).** No new arch_surfaces conflict expected with open master plans; `multi-scale` overlap from prior region-scene work irrelevant here.

### Architecture

#### Component map (post-amendment)

```
DB (panel_detail / panel_child / token_detail) ──┐
                                                  │
                                                  ▼
                                   IUxmlEmissionService.EmitTo(outDir, PanelRow)
                                          │              │
                                          │              │
                                          ▼              ▼
                          {slug}.baked.uxml      {slug}.baked.uss
                                          │              │
                                          └──────┬───────┘
                                                 │
              ITssEmissionService.EmitAll() ─── ▼
                                  cream.baked.tss / dark.baked.tss
                                                 │
                       ui_def_drift_scan (TECH-34686) ──────────┐
                                                                │
              (compare against disk iter-43 {slug}.uxml/.uss + .tss)
                                                                │
                                                                ▼
                                                       four-gate result
                                                                │
                                                green ──→ §5.2 phase 5 cutover ──→ rename .baked → canonical + Host Q-rewrite + delete adapter
                                                red ──→ Phase 4 iterate (no cap)
```

#### Stage map (Stage 1.0..6.0 mapped to §5.2 phases + §5.4 retro)

- **Stage 1.0 — Reverse capture.** Iter-43 disk surface → DB migration draft. Inputs: `toolbar.uxml` (26 LOC, 9-tile grid), `toolbar.uss` (59 LOC, cream literals), `ToolbarHost.cs` Q-name list (`tool-zone-r`, `tool-zone-c`, …, `tool-bulldoze`, `active-tool-label`), Scene rect_json. Outputs: SQL migration rewriting `panel_detail.toolbar` (id 100, v615) + `panel_child[]` rows to mirror iter-43 nested VisualElement tree; `token_detail` cream theme rows seeded with iter-43 literals (LD-8 A5 application). Schema decision (LD-9): EXTEND existing `panel_child` with `node_kind` (string: `VisualElement`/`Button`/`Label`) + `uss_class[]` (text[]) + `style_props_json` (jsonb). Avoids new tree table; reuses existing relations + minimizes follow-up plan schema churn for 12 remaining panels. Exit: migration applied; `unity:bake-ui` still emits legacy prefab unchanged.
- **Stage 2.0 — Sidecar bake.** Extend `UxmlEmissionService.BuildUxml` from 22-line shell to tree walker; emit nested `<ui:VisualElement>` + `<ui:Button>` + `<ui:Label>` per `panel_child` row. Extend `BuildUss` to emit per-child selectors + `:hover` / `--active` pseudo-classes from `style_props_json` + `uss_class[]`. Emit suffix `.baked.uxml` + `.baked.uss` (LD-1 A3-style non-clobber). Wire `npm run unity:bake-ui` dispatch to call `UxmlBakeHandler` after `UiBakeHandler` (TECH-34682). Exit: bake emits `toolbar.baked.uxml` + `toolbar.baked.uss` alongside iter-43 originals; `validate:all` green.
- **Stage 3.0 — Compare + iterate.** Extend `ui_def_drift_scan` (TECH-34686) to triple-output diff (UXML AST normalized + USS selector-grouped + TSS variable). Run §5.3 four gates. Iterate loop: each failure produces diagnostic naming offending node/rule/pixel-region/lookup; fix routes to either emitter (preferred — improves tool for next slug) or DB row (when iter-43 was non-canonical). Loop runs open-ended per LD-5; no cap. Exit: four gates green; emitter fixes documented in stage journal.
- **Stage 4.0 — Cutover.** Atomic single commit: rename `toolbar.baked.uxml` → `toolbar.uxml` (overwrite iter-43); rename `toolbar.baked.uss` → `toolbar.uss`; delete `toolbar.baked.*`; rewrite `ToolbarHost.Q<T>` names if drift required (LD-2 + A4); delete `ToolbarDataAdapter.cs` if Host no longer needs it; delete `Assets/Scripts/Domains/UI/Services/ToolbarAdapterService.cs` if obsolete; verify Play Mode (`db:bridge-playmode-smoke`) toolbar renders + clicks route. Exit: `ToolbarParityTest` green; second `unity:bake-ui` produces zero git diff (idempotence).
- **Stage 5.0 — Tool & skill improvement (§5.4 retro).** File TECH tickets for emitter gaps surfaced in Stage 3.0; ship `ui_toolkit_reverse_capture` MCP slice if Stage 1.0 surfaced manual repetition; author `/ui-toolkit-slug-cutover` skill encoding Stages 1.0–4.0 + four gates + rollback path; amend DEC-A28 with this expansion (commit `decisions.md` row OR `arch_decision_write` MCP). Exit: skill runnable end-to-end on a dry-run second slug (e.g. `pause-menu`) with NO commit; DEC-A28 amendment merged.
- **Stage 6.0 — Validate + follow-up seed.** Confirm `validate:all` + `unity:bake-ui` idempotent across two consecutive bakes (zero diff). Write follow-up exploration seed under `docs/explorations/ui-toolkit-emitter-parity-followup-12-panels.md` listing remaining 12 panels + per-panel risk score + invocation order; link from this doc §7. Exit: follow-up seed merged; ready for `/master-plan-close` on toolbar pilot.

### Subsystem Impact

| Subsystem | Dependency nature | Invariant risk by # | Breaking vs additive | Mitigation |
|---|---|---|---|---|
| **`UxmlEmissionService` / `UxmlBakeHandler`** | Extend from 22-line shell to full tree walker; new schema columns consumed (`node_kind`, `uss_class[]`, `style_props_json`). | None Unity (Editor-only); Universal safety (Hook denylist, schema-cache caveat) — no risk. | Additive. Existing `BuildUxml(row)` signature preserved; tree walker is interior. | Suffix `.baked` outputs avoid clobbering iter-43 until Stage 4.0. `IUxmlEmissionService` contract preserved. |
| **`TssEmissionService`** | Replaces hand-authored `cream.tss` / `dark.tss` with DB-emitted (TECH-34681). Cream theme rows seeded from iter-43 literals in Stage 1.0. | None Unity. | Additive at first (`.baked.tss` sidecar); destructive at Stage 4.0 (overwrite hand-authored). | A5 token-source binding (DB → disk one-way post-cutover); rollback delete `.baked.tss` keeps hand-authored alive pre-Stage-4.0. |
| **`ToolbarHost.cs`** | Q-lookup name list pinned to `tool-{slug}` + `active-tool-label`. If emitter forces rename (e.g. drift gate caught structural divergence), Host Q strings rewrite same commit. | Universal — no Unity 1–11 (Editor + Host file, no runtime grid mutation). C# Coding Conventions (`hot path no LINQ` — not hot; XML doc — keep ≤1 line per LD-7 A4). | Potentially breaking (script-ref break) if class name renamed — DO NOT rename class. Field/method internal rewrite allowed. | A4 binding: class + file path frozen; Q-name strings + adapter deletions only. Inspector serialization untouched. |
| **`ToolbarDataAdapter.cs` / `ToolbarAdapterService.cs`** | Deleted if Host no longer needs adapter post-cutover (LD-2 + §5.2 phase 5). | None (adapter is interior glue). | Breaking — file delete. | Deletion gated on grep for adapter references = 0; delete in Stage 4.0 commit. |
| **`ui_def_drift_scan` MCP** | Extends to triple-output (UXML AST + USS + TSS). Wired into `validate:all` (LD-3 + A7). | None. | Additive — new diff modes, existing UXML diff preserved. | Schema-cache caveat: restart MCP host after tool descriptor edit. |
| **`ui_visual_diff_run` MCP** | Existing tool consumed at §5.3 pixel-diff gate. Reuses `tools/visual-baseline/golden/` toolbar baseline. | None. | Additive consumer. | No change to MCP surface; gate threshold ≤2% per §5.3 row. |
| **`unity:bake-ui` dispatcher** | TECH-34682 wires `UxmlBakeHandler` alongside `UiBakeHandler`. Sidecar mode (both fire) until Stage 4.0 cutover; post-cutover toolbar slug uses UXML path only. | None (Editor-only). | Additive — `UiBakeHandler` keeps firing for 12 deferred slugs. | Per-slug feature flag (slug allowlist) so toolbar = UXML, others = prefab until follow-up plan. |
| **`panel_detail` + `panel_child` DB schema** | LD-9 + Stage 1.0 schema extension: `panel_child` adds `node_kind` text + `uss_class[]` text[] + `style_props_json` jsonb. Reverse-capture migration writes toolbar rows; 12 deferred slugs unchanged until follow-up. | None (DB schema, MCP-managed). | Additive — new columns nullable for legacy rows. | Migration is forward-only with `ALTER TABLE … ADD COLUMN IF NOT EXISTS …`; legacy rows null-default. |
| **`token_detail` DB rows** | Cream theme rows seeded from iter-43 `toolbar.uss` literals (`#f5e6c8`, `#b89b5e`, `#ede4ce`, `#5b7fa8`, `#3a2f1c`, `#6b5a3d`). | None. | Additive — new rows. | Idempotent insert (on conflict do nothing on `(theme_slug, token_slug)`). |
| **`Assets/UI/Themes/cream.tss` / `dark.tss`** | Hand-authored; replaced at Stage 4.0 toolbar cutover by `.baked.tss` rename. 12 deferred slugs may still consume hand-authored tokens until follow-up plan migrates them. | None Unity. | Breaking at Stage 4.0; reversible by rollback path. | Stage 4.0 commit groups rename + Host Q-rewrite + adapter delete so atomicity is single commit. |
| **`ToolbarParityTest.cs`** | Red-stage anchor for Stage 4.0 (LD-6). Green at Stage 4.0 close confirms Play Mode renders + clicks route. | None. | Additive (test surface). | Existing test; no rewrite needed. |
| **`validate:all`** | `ui_def_drift_scan` extension joins chain at Stage 3.0 (A7). | None. | Additive validator. | New validator exit code 0 on toolbar green; red blocks merge. |

**Deferred / out of scope** confirmed: 12 remaining panels (hud-bar, pause-menu, budget-panel, stats-panel, info-panel, map-panel, main-menu, new-game-form, save-load-view, settings-view, notifications-toast, tool-subtype-picker); runtime-only surfaces (`hover-info`, `map-panel` runtime VE) need DB schema decision in follow-up; Figma/Claude-Design/A2UI ingest; web `asset-pipeline` REST endpoints; full `UiBakeHandler*.cs` retirement; new visual changes (parity preservation only).

### Implementation Points

#### Stage 1.0 — Reverse capture (1.0.1..1.0.4)

- **1.0.1 Schema extension migration.** SQL migration adding `node_kind text`, `uss_class text[]`, `style_props_json jsonb` columns to `panel_child` (IF NOT EXISTS). Touched: `tools/mcp-ia-server/migrations/{n}-panel-child-tree-shape.sql`. Idempotent.
- **1.0.2 Reverse-capture parser (manual or MCP-slice).** Parse `Assets/UI/Generated/toolbar.uxml` (26 LOC, 9-tile nested grid) → emit `panel_child` row plan. Parse `toolbar.uss` (59 LOC) → emit per-class `style_props_json` blob + extract cream theme tokens (background `#f5e6c8`, border `#b89b5e`, etc.) for `token_detail`. Parse `ToolbarHost.cs` `TileSlugs[]` array (LOC 24–29) → confirm Q-name list matches UXML names. Touched: parser script `tools/scripts/ui-toolkit-reverse-capture.mjs` OR MCP slice `ui_toolkit_reverse_capture` (decision at Stage 5.0 §5.4 retro — first iteration script-only). Output: SQL `INSERT` block.
- **1.0.3 Apply reverse-capture migration.** Run migration; DB `panel_detail.toolbar` (id 100) v615 superseded by v616 with new shape; `panel_child` 9 rows for `tool-{slug}` + 1 row for `active-tool-label` + 2 wrapper VEs (`toolbar` root + `toolbar-grid`). `token_detail` cream rows seeded.
- **1.0.4 Confirm legacy prefab path untouched.** Run `npm run unity:bake-ui`; verify legacy `UiBakeHandler` still emits prefab for non-toolbar panels; toolbar UXML unchanged on disk (no `.baked` yet — Stage 2.0).

#### Stage 2.0 — Sidecar bake (2.0.1..2.0.4)

- **2.0.1 `UxmlEmissionService.BuildUxml` tree walker (TECH-34678).** Replace 22-line shell with recursive walker over `panel_child` tree (root row + DFS by `parent_id` + `ordinal`). Emit `<ui:{node_kind} name="{slug}" class="{uss_class join space}"/>` per row. Touched: `Assets/Scripts/Editor/Bridge/UxmlEmissionService.cs`. Output path: `{outDir}/{slug}.baked.uxml`.
- **2.0.2 `BuildUss` per-child rule emit (TECH-34679, TECH-34680).** Iterate `panel_child` rows; emit `.{uss_class}` block per class from `style_props_json`. Resolve token refs via `token_detail` lookup (cream theme); inline literal hex per A5. Emit `:hover` / `.--active` pseudo-class blocks where `style_props_json.states` carries them. Output: `{outDir}/{slug}.baked.uss`.
- **2.0.3 TSS emitter (TECH-34681).** Group `token_detail` rows by theme; emit `:root { --ds-color-bg-card: #313244; … }` per theme. Output: `Assets/UI/Themes/cream.baked.tss` + `dark.baked.tss`. Touched: `Assets/Scripts/Editor/Bridge/TssEmitter.cs` + `TssEmissionService.cs`.
- **2.0.4 Dispatcher wiring (TECH-34682).** `npm run unity:bake-ui` after `UiBakeHandler.Bake` invokes `UxmlBakeHandler.Bake` for toolbar slug; per-slug feature-flag table (`bake_pipeline_flag`) governs which slugs route to UXML emit vs prefab. Initially `{toolbar: uxml, *: prefab}`.

#### Stage 3.0 — Compare + iterate (3.0.1..3.0.N — N unbounded per LD-5)

- **3.0.1 `ui_def_drift_scan` triple-output extension (TECH-34686).** Extend MCP tool: `mode: uxml|uss|tss|all`. UXML AST diff: parse both inputs with shared XML normalizer (whitespace strip + attribute reorder alphabetical) → byte-equal compare. USS selector-grouped diff: parse both into `{selector: {prop: value}}` map → bidirectional diff (every iter-43 rule in baked + no extras). TSS variable diff: `:root { --x: val }` map compare. Touched: `tools/mcp-ia-server/src/tools/ui-def-drift-scan/*`. Output: structured diff JSON naming offending node/rule/var.
- **3.0.2 Four-gate runner.** Script `tools/scripts/toolbar-cutover-gate.mjs`: call `ui_def_drift_scan` (UXML, USS, TSS) + `ui_visual_diff_run` (toolbar baseline, ≤2%) + `ui_toolkit_host_lint` (every `ToolbarHost.Q<T>("name")` resolves against `toolbar.baked.uxml`). Aggregate pass/fail; emit per-gate diagnostic.
- **3.0.3..3.0.N Iterate.** Each red diagnostic → fix in emitter (TECH-34678/9/80/81) OR DB row (when iter-43 was non-canonical, e.g. iter-43 has typo class name). Re-run bake + four-gate runner. Loop until green. Stage journal records each iteration's fix + which side absorbed it (emitter vs DB).

#### Stage 4.0 — Cutover (4.0.1..4.0.5 — single commit)

- **4.0.1 Rename `.baked` → canonical.** `mv toolbar.baked.uxml toolbar.uxml` + `mv toolbar.baked.uss toolbar.uss` + `mv cream.baked.tss cream.tss` (+ `dark.baked.tss` if touched).
- **4.0.2 Host Q-rewrite (LD-7 A4).** Edit `Assets/Scripts/UI/Hosts/ToolbarHost.cs` `TileSlugs[]` + `_btns` Q strings to match DB-emitted slugs (if Stage 3.0 forced rename). Class name + file path frozen; namespace frozen; `_doc` + `_subtypePicker` `[SerializeField]` frozen. XML doc ≤1 line per coding-conventions.
- **4.0.3 Adapter delete.** `git rm Assets/Scripts/UI/Toolbar/ToolbarDataAdapter.cs` + `git rm Assets/Scripts/Domains/UI/Services/ToolbarAdapterService.cs` IFF grep across `Assets/Scripts/` shows zero references post-Host-rewrite.
- **4.0.4 Play Mode smoke.** `npm run db:bridge-playmode-smoke` with scenario `toolbar-renders-and-clicks-route`. Bridge command sequence: load CityScene → enter Play Mode → screenshot toolbar region → click each tile slug → assert `ToolbarVM.ActiveTool` matches → exit Play Mode.
- **4.0.5 Idempotence verify.** Run `npm run unity:bake-ui` twice; `git diff` after second bake = zero. `ToolbarParityTest` green.

#### Stage 5.0 — Skill + amendment (5.0.1..5.0.4)

- **5.0.1 File TECH tickets for emitter gaps.** Per Stage 3.0 iteration journal, file TECH-XXXX for each unfixed gap touching follow-up panels (e.g. `<ui:Slider>` emit if pause-menu needs it; not in toolbar scope).
- **5.0.2 `ui_toolkit_reverse_capture` MCP slice (conditional).** If Stage 1.0 manual reverse-capture took >1 day, ship MCP slice taking `(uxml_path, uss_path, host_path)` → emits SQL migration draft. Saves cost for 12 follow-up slugs. Touched: `tools/mcp-ia-server/src/tools/ui-toolkit-reverse-capture/*`.
- **5.0.3 `/ui-toolkit-slug-cutover` skill.** Author `ia/skills/ui-toolkit-slug-cutover/SKILL.md` + agent-body. Encodes Stages 1.0–4.0 + four gates + rollback path. Frontmatter: input `{SLUG}`. Run `npm run skill:sync:all` so `.claude/commands/ui-toolkit-slug-cutover.md` + `.claude/agents/ui-toolkit-slug-cutover.md` regenerate.
- **5.0.4 DEC-A28 amendment merge.** Append `dec-a28-toolbar-cutover-playbook-amendment` row to `ia/specs/architecture/decisions.md` (table-shape per DEC-A17). MCP `arch_decision_write` (status=active) + `cron_arch_changelog_append_enqueue` (kind=`design_explore_decision`).

#### Stage 6.0 — Validate + follow-up seed (6.0.1..6.0.2)

- **6.0.1 Full `verify:local` chain.** `validate:all` + `unity:compile-check` + `db:migrate` + `db:bridge-preflight` + Editor save/quit + `db:bridge-playmode-smoke`. Re-run `unity:bake-ui` and confirm zero git diff.
- **6.0.2 Follow-up exploration seed.** Author `docs/explorations/ui-toolkit-emitter-parity-followup-12-panels.md` listing 12 deferred slugs + per-panel risk score (blast radius + structural complexity + state class count). Schema decision (LD-9) confirmed reused; per-slug invocation order suggested. Link from this doc §7.

### Examples

#### 1. `UxmlEmissionService.BuildUxml` tree walker (Stage 2.0.1) — before / after

Before (`Assets/Scripts/Editor/Bridge/UxmlEmissionService.cs:31-44`):

```csharp
static string BuildUxml(PanelRow row) =>
$@"<?xml version=""1.0"" encoding=""utf-8""?>
<ui:UXML xmlns:ui=""UnityEngine.UIElements"" ...>
  <Style src=""project://database/Assets/UI/Themes/{row.ThemeSlug}.tss""/>
  <Style src=""{row.Slug}.uss""/>
  <ui:VisualElement name=""{row.Slug}"" class=""{row.Slug}"">
  </ui:VisualElement>
</ui:UXML>
";
```

After (tree walker):

```csharp
static string BuildUxml(PanelRow row, IReadOnlyList<PanelChildRow> children)
{
    var sb = new StringBuilder();
    sb.AppendLine(@"<?xml version=""1.0"" encoding=""utf-8""?>");
    sb.AppendLine(@"<ui:UXML xmlns:ui=""UnityEngine.UIElements"" editor-extension-mode=""False"">");
    sb.AppendLine($@"  <Style src=""project://database/Assets/UI/Themes/{row.ThemeSlug}.tss""/>");
    sb.AppendLine($@"  <Style src=""{row.Slug}.uss""/>");
    WalkChildren(sb, children, parentId: null, depth: 1);
    sb.AppendLine("</ui:UXML>");
    return sb.ToString();
}

static void WalkChildren(StringBuilder sb, IReadOnlyList<PanelChildRow> rows, int? parentId, int depth)
{
    foreach (var r in rows.Where(x => x.ParentId == parentId).OrderBy(x => x.Ordinal))
    {
        var indent = new string(' ', depth * 2);
        var classes = string.Join(" ", r.UssClass);
        var kids = rows.Any(x => x.ParentId == r.Id);
        if (kids)
        {
            sb.AppendLine($"{indent}<ui:{r.NodeKind} name=\"{r.Slug}\" class=\"{classes}\">");
            WalkChildren(sb, rows, r.Id, depth + 1);
            sb.AppendLine($"{indent}</ui:{r.NodeKind}>");
        }
        else
        {
            sb.AppendLine($"{indent}<ui:{r.NodeKind} name=\"{r.Slug}\" class=\"{classes}\"/>");
        }
    }
}
```

#### 2. Reverse-capture migration row example (Stage 1.0.3) — `panel_child` toolbar root + grid + first tile

```sql
INSERT INTO panel_child (panel_id, parent_id, ordinal, slug, node_kind, uss_class, style_props_json) VALUES
    (100, NULL, 0, 'toolbar',       'VisualElement', ARRAY['toolbar'],                                    '{"position":"absolute","bottom":180,"left":8,"width":176}'::jsonb),
    (100, 1,    0, 'toolbar-grid',  'VisualElement', ARRAY['toolbar__grid'],                              '{"flex-direction":"row","flex-wrap":"wrap","width":164}'::jsonb),
    (100, 2,    0, 'tool-zone-r',   'Button',        ARRAY['icon-btn','icon-btn--zone-r','toolbar__tile'],'{"tooltip":"Residential","width":76,"height":76,"margin":3}'::jsonb),
    -- (8 more tile rows omitted for brevity) --
    (100, 1,    1, 'active-tool-label', 'Label',     ARRAY['toolbar__active-label'],                      '{"text":"","color":"#6b5a3d","font-size":11}'::jsonb)
ON CONFLICT (panel_id, slug) DO UPDATE SET
    parent_id = EXCLUDED.parent_id,
    ordinal = EXCLUDED.ordinal,
    node_kind = EXCLUDED.node_kind,
    uss_class = EXCLUDED.uss_class,
    style_props_json = EXCLUDED.style_props_json;
```

#### 3. Four-gate runner output (Stage 3.0.2)

```
[gate 1/4 uxml-ast]      PASS — toolbar.uxml ↔ toolbar.baked.uxml normalized AST byte-equal
[gate 2/4 uss-selector]  FAIL — selector .icon-btn--active missing rule `border-width: 3px` in baked
                           ↳ fix path: emitter (UxmlEmissionService.BuildUss state-class emit) — TECH-34680
[gate 3/4 pixel-diff]    NOT RUN (gate 2 red)
[gate 4/4 host-q-lookup] PASS — 11/11 ToolbarHost Q strings resolve in baked uxml

Iterate path: §5.2 phase 4 — fix emitter state-class branch + re-bake + re-run gates.
```

#### 4. Host Q-rewrite atomic with cutover (Stage 4.0.2)

If Stage 3.0 iteration forced `tool-zone-r` → `tool-residential` (emitter convention), the cutover commit edits `ToolbarHost.cs` `TileSlugs[]` literal in same commit as the rename:

```diff
- static readonly string[] TileSlugs = new[] {
-     "zone-r", "zone-c", "zone-i",
+ static readonly string[] TileSlugs = new[] {
+     "residential", "commercial", "industrial",
      "road", "services",
      "building-power", "building-water",
      "landmark", "bulldoze",
  };
```

Plus `HasSubtypes` rename + `PreArmDefault` switch labels. Class + file path + `[SerializeField] _doc` + `[SerializeField] _subtypePicker` untouched. Inspector script-ref preserved.

### Review Notes

#### Phase 8 — Plan subagent review (carried, NON-BLOCKING after Q4 lock)

- **NON-BLOCKING-1 (LD-5 carry-over).** Phase 4 iterate loop with no cap accepts unbounded cost. Risk: 5–10 iterations realistic; >20 = signal of structural mismatch warranting design re-grill. Mitigation: stage journal records iteration count + which side absorbed fix (emitter vs DB); if count exceeds 20, user revisits LD-5 manually.
- **NON-BLOCKING-2.** Stage 2.0 emitter expansion from 22 LOC to ~200–400 LOC across `UxmlEmissionService` + `TssEmitter` is the largest single Stage by code volume. Compile-check + asmdef-boundary risk: emitter stays in `Territory.Editor.Bridge` namespace; no asmdef cycle introduced.
- **NON-BLOCKING-3.** `ToolbarParityTest.cs` (LD-6) was scaffolded at iter-43 closeout. Audit at Stage 1.0 confirms test surface matches DB-baked structure expectations; if test asserts iter-43 names that get renamed in Stage 3.0, test updates batch with Stage 4.0 commit per A4.
- **NON-BLOCKING-4 (LD-9 confirm).** Schema extension of `panel_child` (vs new tree table) accepted; risk that future complex slugs (e.g. main-menu with deeply nested + many state classes) outgrow `style_props_json` jsonb shape. Mitigation: follow-up plan re-evaluates after 3 slug cutovers complete.
- **NON-BLOCKING-5 (A5 token direction).** Once toolbar TSS goes DB-canonical, future ad-hoc designer edits to `cream.tss` will be overwritten by next bake. Mitigation: post-cutover `cream.tss` carries `<!-- Generated by TssEmitter — do not hand-edit -->` banner; designer edits route through DB.
- **NON-BLOCKING-6 (skill emission Stage 5.0).** Skill encodes a strict 6-phase recipe; agents invoking it on a divergent slug shape (e.g. runtime-VE-only `hover-info`) must STOP per skill hard boundary, not adapt the recipe inline. Follow-up plan handles runtime-VE schema separately.

#### Subagent route (Phase 8 verbatim)

Phase 8 subagent review was not re-run after Q4 lock since locked decision invalidates only the abort-gate clause; no other Phase 4..7 surface affected. Re-run if Stage 1.0..3.0 surfaces new structural risks.

### Expansion metadata

- **Date:** 2026-05-15
- **Model:** claude-opus-4-7[1m]
- **Approach selected:** Path B + Plan C strangler — toolbar pilot, side-by-side clone-and-compare, non-abortable.
- **Architecture decision:** DEC-A28 amendment — `dec-a28-toolbar-cutover-playbook-amendment` (surfaces: `contracts/ui-toolkit-strangler`, `bake-pipeline/uxml-emitter-sidecar`, `validation/ui_def_drift_scan`, `skill/ui-toolkit-slug-cutover`).
- **Locked decisions captured this session:** 6 (LD-1..LD-6 ratified; LD-7..LD-9 resolved via expansion).
- **Blocking items resolved:** 1 (Q4 — abort gate stance).
- **Non-blocking + suggestions carried:** 6.
- **Drift overlap flagged:** none new on UI Toolkit surfaces; `multi-scale` overlap from prior DEC-A29 session unrelated.
