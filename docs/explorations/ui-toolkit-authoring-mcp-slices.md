---
# Design-explore handoff (Phase 4 canonical YAML — consumed by /ship-plan)
slug: ui-toolkit-authoring-mcp-slices
title: "UI Toolkit authoring MCP slices — 9-tool surface, phased rollout"
target_version: 1
stages:
  - id: "1"
    title: "Inspect — read-only surface + IUIToolkitPanelBackend boundary"
    exit: "DiskBackend reads `.uxml`/`.uss`/scene UIDocument/Host C# AST; DbBackend stub returns parked error; ui_toolkit_panel_get + ui_toolkit_host_inspect MCP tools register; Stage 1 red-stage test green."
    red_stage_proof: "tools/mcp-ia-server/tests/tools/ui-toolkit-stage1-inspect.test.ts::stage1_inspect_surface_complete"
    red_stage_proof_block:
      red_test_anchor: "tools/mcp-ia-server/tests/tools/ui-toolkit-stage1-inspect.test.ts::stage1_inspect_surface_complete"
      target_kind: "tracer_verb"
      proof_artifact_id: "ui-toolkit-stage1-inspect-tracer"
      proof_status: "failed_as_expected"
    tasks:
      - id: "T1.0"
        title: "IUIToolkitPanelBackend boundary + DiskBackend stub + factory"
        prefix: TECH
        kind: code
        depends_on: []
        digest_outline: "IUIToolkitPanelBackend interface + DiskBackend stub + factory wiring + env flag UI_TOOLKIT_BACKEND=disk|db"
        touched_paths:
          - tools/mcp-ia-server/src/ia-db/ui-toolkit-backend.ts
          - tools/mcp-ia-server/src/ia-db/uss-parser.ts
          - tools/mcp-ia-server/tests/tools/ui-toolkit-stage1-inspect.test.ts
      - id: "T1.1"
        title: "ui_toolkit_panel_get composite read tool"
        prefix: TECH
        kind: mcp-only
        depends_on: ["T1.0"]
        digest_outline: "ui_toolkit_panel_get — composite read: UXML + USS + host inspect + scene UIDocument + golden manifest"
        touched_paths:
          - tools/mcp-ia-server/src/tools/ui-toolkit-panel-get.ts
          - tools/mcp-ia-server/src/server-registrations.ts
          - tools/mcp-ia-server/tests/tools/ui-toolkit-stage1-inspect.test.ts
      - id: "T1.2"
        title: "ui_toolkit_host_inspect Host C# AST scan tool"
        prefix: TECH
        kind: mcp-only
        depends_on: ["T1.0"]
        digest_outline: "ui_toolkit_host_inspect — Host C# AST scan: Q-lookups, click handlers, modal slug, runtime VE construction"
        touched_paths:
          - tools/mcp-ia-server/src/tools/ui-toolkit-host-inspect.ts
          - tools/mcp-ia-server/src/ia-db/csharp-host-parser.ts
          - tools/mcp-ia-server/src/server-registrations.ts
          - tools/mcp-ia-server/tests/tools/ui-toolkit-stage1-inspect.test.ts
  - id: "2"
    title: "Author — mutation surface (disk-canonical, idempotent on natural key)"
    exit: "panel-schema.yaml extended with panel_kind: ui-toolkit-overlay + 9 element kinds; node_upsert + node_remove + uss_rule_upsert tools register, idempotent on natural key, allow-list gated; Stage 2 red-stage test green; manual golden re-record protocol documented in done-def."
    red_stage_proof: "tools/mcp-ia-server/tests/tools/ui-toolkit-stage2-author.test.ts::stage2_author_idempotent_mutations"
    red_stage_proof_block:
      red_test_anchor: "tools/mcp-ia-server/tests/tools/ui-toolkit-stage2-author.test.ts::stage2_author_idempotent_mutations"
      target_kind: "tracer_verb"
      proof_artifact_id: "ui-toolkit-stage2-author-tracer"
      proof_status: "failed_as_expected"
    tasks:
      - id: "T2.0"
        title: "Idempotency contract + panel-schema.yaml ui-toolkit-overlay extension"
        prefix: TECH
        kind: code
        depends_on: ["T1.0"]
        digest_outline: "Idempotency contract + panel-schema.yaml extension (panel_kind: ui-toolkit-overlay) + per-kind Zod validators"
        touched_paths:
          - tools/blueprints/panel-schema.yaml
          - tools/mcp-ia-server/src/tools/_ui-toolkit-shared.ts
          - tools/mcp-ia-server/tests/tools/ui-toolkit-stage2-author.test.ts
      - id: "T2.1"
        title: "ui_toolkit_panel_node_upsert UXML tree mutation tool"
        prefix: TECH
        kind: mcp-only
        depends_on: ["T2.0"]
        digest_outline: "ui_toolkit_panel_node_upsert — UXML tree mutation + optional --seed-uss stub. Allow-list: spec-implementer, plan-author"
        touched_paths:
          - tools/mcp-ia-server/src/tools/ui-toolkit-panel-node-upsert.ts
          - tools/mcp-ia-server/src/server-registrations.ts
          - tools/mcp-ia-server/tests/tools/ui-toolkit-stage2-author.test.ts
      - id: "T2.2"
        title: "ui_toolkit_panel_node_remove cascade delete + orphan USS drift report tool"
        prefix: TECH
        kind: mcp-only
        depends_on: ["T2.0"]
        digest_outline: "ui_toolkit_panel_node_remove — cascade delete + orphan USS drift report (no auto-delete). Allow-list gated."
        touched_paths:
          - tools/mcp-ia-server/src/tools/ui-toolkit-panel-node-remove.ts
          - tools/mcp-ia-server/src/server-registrations.ts
          - tools/mcp-ia-server/tests/tools/ui-toolkit-stage2-author.test.ts
      - id: "T2.3"
        title: "ui_toolkit_uss_rule_upsert literal-hex preserving rule tool"
        prefix: TECH
        kind: mcp-only
        depends_on: ["T2.0"]
        digest_outline: "ui_toolkit_uss_rule_upsert — literal-hex preservation + idempotent on (slug, selector). Allow-list gated."
        touched_paths:
          - tools/mcp-ia-server/src/tools/ui-toolkit-uss-rule-upsert.ts
          - tools/mcp-ia-server/src/server-registrations.ts
          - tools/mcp-ia-server/tests/tools/ui-toolkit-stage2-author.test.ts
  - id: "3"
    title: "Wire — Host C# code-stub + scene UIDocument validation (DEC-A28 I4)"
    exit: "host_q_bind tool defaults to code-stub return; --apply gate enforced by tracer test; ui_toolkit_scene_uidoc_validate emits structured verdict with bridge-wire OR runtime-spawn suggestion; Stage 3 red-stage test green."
    red_stage_proof: "tools/mcp-ia-server/tests/tools/ui-toolkit-stage3-wire.test.ts::stage3_wire_apply_flag_gate"
    red_stage_proof_block:
      red_test_anchor: "tools/mcp-ia-server/tests/tools/ui-toolkit-stage3-wire.test.ts::stage3_wire_apply_flag_gate"
      target_kind: "tracer_verb"
      proof_artifact_id: "ui-toolkit-stage3-wire-tracer"
      proof_status: "failed_as_expected"
    tasks:
      - id: "T3.0"
        title: "DEC-A28 I4 apply-flag gate tracer"
        prefix: TECH
        kind: code
        depends_on: ["T1.2"]
        digest_outline: "DEC-A28 I4 enforcement — host_q_bind without --apply returns snippet only; Host file unchanged. Apply mode requires explicit flag."
        touched_paths:
          - tools/mcp-ia-server/tests/tools/ui-toolkit-stage3-wire.test.ts
      - id: "T3.1"
        title: "ui_toolkit_host_q_bind code-stub + --apply Host rewriter tool"
        prefix: TECH
        kind: mcp-only
        depends_on: ["T3.0"]
        digest_outline: "ui_toolkit_host_q_bind — code-stub default, --apply rewrites Host C# + invokes unity_compile. Allow-list: spec-implementer, plan-author."
        touched_paths:
          - tools/mcp-ia-server/src/tools/ui-toolkit-host-q-bind.ts
          - tools/mcp-ia-server/src/server-registrations.ts
          - tools/mcp-ia-server/tests/tools/ui-toolkit-stage3-wire.test.ts
      - id: "T3.2"
        title: "ui_toolkit_scene_uidoc_validate scene wiring verdict tool"
        prefix: TECH
        kind: mcp-only
        depends_on: ["T1.0"]
        digest_outline: "ui_toolkit_scene_uidoc_validate — scene YAML scan, structured verdict + suggestion (bridge wire OR runtime-spawn pattern)"
        touched_paths:
          - tools/mcp-ia-server/src/tools/ui-toolkit-scene-uidoc-validate.ts
          - tools/mcp-ia-server/src/server-registrations.ts
          - tools/mcp-ia-server/tests/tools/ui-toolkit-stage3-wire.test.ts
  - id: "4"
    title: "Verify — pixel diff backstop + host lint (unlocks Path B DbBackend cutover)"
    exit: "ui_toolkit_panel_pixel_diff wraps existing ui_visual_diff_run; ui_toolkit_host_lint emits structured findings; host-bindings validator wires into validate:all; Stage 4 red-stage test green; allow-list gates verified end-to-end."
    red_stage_proof: "tools/mcp-ia-server/tests/tools/ui-toolkit-stage4-verify.test.ts::stage4_verify_pixel_and_lint_backstop"
    red_stage_proof_block:
      red_test_anchor: "tools/mcp-ia-server/tests/tools/ui-toolkit-stage4-verify.test.ts::stage4_verify_pixel_and_lint_backstop"
      target_kind: "tracer_verb"
      proof_artifact_id: "ui-toolkit-stage4-verify-tracer"
      proof_status: "failed_as_expected"
    tasks:
      - id: "T4.0"
        title: "Pixel diff wrapper tracer — reuses ui_visual_diff_run engine"
        prefix: TECH
        kind: code
        depends_on: ["T2.1"]
        digest_outline: "Pixel diff wraps existing ui_visual_diff_run engine — no new bridge kind; reuses unity_bridge_command capture_screenshot include_ui:true"
        touched_paths:
          - tools/mcp-ia-server/tests/tools/ui-toolkit-stage4-verify.test.ts
      - id: "T4.1"
        title: "ui_toolkit_panel_pixel_diff slug-keyed pixel diff tool"
        prefix: TECH
        kind: mcp-only
        depends_on: ["T4.0"]
        digest_outline: "ui_toolkit_panel_pixel_diff — slug-keyed wrapper over ui_visual_diff_run; tolerance default 0.005 reused"
        touched_paths:
          - tools/mcp-ia-server/src/tools/ui-toolkit-panel-pixel-diff.ts
          - tools/mcp-ia-server/src/server-registrations.ts
          - tools/mcp-ia-server/tests/tools/ui-toolkit-stage4-verify.test.ts
      - id: "T4.2"
        title: "ui_toolkit_host_lint Q-lookup + handler + slug + FindObjectOfType lint tool"
        prefix: TECH
        kind: mcp-only
        depends_on: ["T1.2"]
        digest_outline: "ui_toolkit_host_lint — Q-lookup orphan, handler unsubscribe, ModalCoordinator slug, FindObjectOfType-in-Update. Wires into validate:all."
        touched_paths:
          - tools/mcp-ia-server/src/tools/ui-toolkit-host-lint.ts
          - tools/mcp-ia-server/src/server-registrations.ts
          - tools/scripts/validate-ui-toolkit-host-bindings.mjs
          - tools/mcp-ia-server/tests/tools/ui-toolkit-stage4-verify.test.ts
notes: |
  Companion seed: docs/explorations/ui-toolkit-emitter-parity-and-db-reverse-capture.md.
  Allow-list mutation tools (is_caller_authorized: ["spec-implementer", "plan-author"]):
    - ui_toolkit_panel_node_upsert
    - ui_toolkit_panel_node_remove
    - ui_toolkit_uss_rule_upsert
    - ui_toolkit_host_q_bind (only when --apply=true)
  Deferred tools (out of scope this plan): ui_toolkit_panel_list (grep covers),
  ui_toolkit_panel_tree (compose from panel_get), ui_toolkit_uss_resolve (grep covers),
  ui_toolkit_panel_node_reorder (compose from upsert), ui_toolkit_tss_token_upsert (Path B work),
  ui_toolkit_panel_uxml_replace (Write covers), ui_toolkit_modal_slug_register (merged into host_lint),
  ui_toolkit_blip_binding_register (defer), ui_toolkit_drift_scan (TECH-34686 Path B),
  ui_toolkit_panel_dependents (defer v2), ui_toolkit_panel_render_preview (folded into panel_pixel_diff).
  Glossary candidates (not yet promoted): IUIToolkitPanelBackend, DiskBackend, DbBackend,
  ui-toolkit-overlay, allow-list mutation tool, golden re-record protocol.

# Seed metadata (preserved from initial doc creation)
purpose: "Seed doc for design-explore — propose the agent-side MCP slice surface (read + author + wire + verify) for UI Toolkit panel authoring + modification, so the Path B emitter-parity master plan has a mechanical primitive set ready before it sizes stages."
audience: agent
loaded_by: on-demand
created_at: 2026-05-14
status: design-explore-complete
related_docs:
  - docs/explorations/ui-toolkit-emitter-parity-and-db-reverse-capture.md
  - docs/explorations/ui-as-code-state-of-the-art-2026-05.md
  - docs/explorations/ui-panel-tree-db-storage.md
  - docs/explorations/ui-bake-pipeline-hardening-v2.md
  - docs/ui-toolkit-parity-recovery-plan.html
  - ia/specs/ui-design-system.md
  - ia/specs/architecture/layers.md
  - ia/specs/architecture/data-flows.md
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
  - TECH-34684  # DB schema rewrite — panel_child rows match current UI structure per slug
  - TECH-34685  # Host Q-lookup rewrite per cutover slug — DB-emitted UXML names match Host bindings
  - TECH-34686  # ui_def_drift_scan extends to triple-output drift (UXML + USS + TSS) vs DB
---

# UI Toolkit authoring MCP slices — seed for design-explore

## §1. Background + intent

The shipping in-game UI runs on Unity UI Toolkit (UXML + USS + TSS triple) per DEC-A28 strangler. Pixel goldens under `tools/visual-baseline/golden/` lock the current UI baseline as the visual contract. Agents need a mechanical MCP surface to **inspect, author, modify, wire, and verify** UI Toolkit panels without hand-stitching `Read` / `Edit` / `grep` / bridge calls per round-trip.

This seed proposes **20 MCP slice tools** under a new `ui_toolkit_*` prefix that mirror the authoring loop stages (Inspect → Author → Wire → Verify). They are additive — the existing legacy-side `ui_panel_*` / `ui_token_*` / `ui_component_*` slices (which target the prefab pipeline DEC-A24) stay in place; the new slices target the UI Toolkit overlay (DEC-A28). The two coexist per strangler invariant.

**Intent** (transmit verbatim to design-explore):

1. **Same MCP surface, swappable backend.** Tools must work in BOTH the current disk-canonical world (`Assets/UI/Generated/*.uxml/uss`, `Assets/UI/Themes/*.tss`) AND the future DB-canonical world (`panel_child` + `token_detail` + emitter sidecar producing the same files). Path B emitter parity (TECH-34678..86) flips the backend silently; the slice contract stays stable.
2. **Pixel goldens are the contract.** Any tool that mutates panel state must be backed by `ui_toolkit_panel_pixel_diff` evidence against the locked goldens. Diff regression = rollback.
3. **DB-primary pivot — no SQL, no yaml hand-edit.** Mutation tools write through MCP slices that own the row write + bake invocation. The `feedback_db_primary_pivot` memory rule applies.
4. **Host C# stays outside the bake pipeline (DEC-A28 invariant I4).** Tools that touch Host source (`ui_toolkit_host_q_bind`, `ui_toolkit_host_lint`) treat Host C# as canonical — they generate code-stubs or scan for drift, never auto-rewrite. Hosts are authored by humans (or by `plan-author`/`spec-implementer` agents inside a controlled stage), not by mechanical slice mutations.
5. **Strangler per slug, not big-bang.** Tools accept a `slug` filter; per-panel feature-flag controls which slugs cut over to DB-canonical first. The legacy `UiBakeHandler` prefab path (~7300 LOC) retires per-slug as cutovers ship.
6. **Recovery plan is historical.** `docs/ui-toolkit-parity-recovery-plan.html` is the authoring record for how the current UI baseline arrived; it is not the canonical source. Canonical context lives in `ia/specs/glossary.md §User interface — UI Toolkit (current UI baseline)` + `ia/specs/ui-design-system.md §Codebase inventory (UI Toolkit overlay — current UI baseline)` + `ia/specs/architecture/layers.md` + `ia/specs/architecture/data-flows.md §UI / UX design system`.
7. **Bridge coordination.** Slices that need Play Mode (preview/pixel diff) follow existing `unity_bridge_lease` + Editor-on-REPO_ROOT preconditions per the bridge-environment-preflight skill.

## §2. Pipeline reality recap

Five reality checks distilled from the pre-flight audit (full audit at `docs/ui-toolkit-parity-recovery-plan.html §15.4.3.1` + `docs/explorations/ui-toolkit-emitter-parity-and-db-reverse-capture.md §2`):

1. `npm run unity:bake-ui` dispatches `bake_ui_from_ir` → `UiBakeHandler.Bake` → **prefabs** at `Assets/UI/Prefabs/Generated/` (DEC-A24 path). It does NOT produce UXML/USS at `Assets/UI/Generated/`.
2. `UxmlBakeHandler` + `UxmlEmissionService` + `TssEmissionService` (DEC-A28 sidecar) exist but are **22-line shell stubs** + not wired into `unity:bake-ui`.
3. DB `panel_child` rows describe the legacy uGUI prefab structure (e.g. hud-bar has 14 children matching the prefab pipeline, not the current 3-cluster strip).
4. Hosts (`HudBarHost`, `BudgetPanelHost`, etc.) `Q<Button>("hud-pause")` bind to current disk UXML element names, NOT DB slugs.
5. Runtime-only surfaces (`HoverInfoHost`, `MapPanelHost.BuildRuntimePanel`) have **no UXML/USS files at all** — pure C# programmatic VisualElement construction.

## §3. Proposed MCP slices (20, grouped by authoring loop stage)

### §3.1 Inspect (5 — read state before mutating)

**`ui_toolkit_panel_get`** — Inputs: `slug`. Returns a composite read for the current UI baseline: UXML file content + parsed VisualElement tree (kind / name / class[] / inline-style / params), per-panel USS file content + parsed rule-set, the linked `*Host.cs` source (Q-lookups, click handlers, OnEnable/OnDisable lifecycle, ModalCoordinator registration), scene UIDocument GameObject wiring (UIDocument component path, PanelSettings ref, sourceAsset GUID), and the golden manifest entry under `tools/visual-baseline/golden/` if one exists. Single round-trip replaces 4–5 separate file reads + grep passes.

**`ui_toolkit_panel_list`** — Inputs: optional `include_runtime_only: bool`. Returns every UI Toolkit surface in the repo: disk-rooted panels (one entry per UXML/USS pair under `Assets/UI/Generated/`), runtime-only programmatic surfaces (detected by scanning Host source for VisualElement constructors with no scene UIDocument), and the linked Host class + ModalCoordinator slug per row. Output mirrors `ui_panel_list` so agents can pivot between the two pipelines side-by-side.

**`ui_toolkit_panel_tree`** — Inputs: `slug`, optional `max_depth`. Returns the parsed UXML tree as a recursive JSON node structure (`{name, kind, classes[], inline_style, params, children[]}`). Useful when the structural shape is needed without dragging raw XML text through the conversation window.

**`ui_toolkit_host_inspect`** — Inputs: `host_class` (e.g. `BudgetPanelHost`). Returns the Host's contract surface: `[SerializeField]` fields + types, every `root.Q<T>("name")` lookup grouped by element kind, every `*.clicked +=` / `RegisterValueChangedCallback` binding (target method + manager dependency), `FindObjectOfType<T>()` fallback chain, `ModalCoordinator.RegisterMigratedPanel` slug, `BlipEngine` bindings, and any programmatic `VisualElement` construction (runtime-VE detection). Powers "where does this button click go" reverse-lookups.

**`ui_toolkit_uss_resolve`** — Inputs: `class_name`, optional `theme`. Returns every USS rule that targets the class (file + line + property map), including pseudo-class variants (`:hover`, `--active`) and inherited rules from theme TSS. Powers "what color is this button hover" lookups without reading the full USS file.

### §3.2 Author / modify (6 — mutate panel structure + style)

**`ui_toolkit_panel_node_upsert`** — Inputs: `slug`, `parent_path` (XPath-style — e.g. `hud-bar/hud-bar__right`), `kind` (`button` | `label` | `slider` | `toggle` | `dropdown` | `text-field` | `integer-field` | `scroll-view` | `visual-element`), `name`, `classes[]`, `params{}` (kind-specific: `text` / `tooltip` / `low-value` / `high-value` / `binding-path` / `icon` / `action_id`), `ord` (insertion position), optional `inline_style{}`. Writes/updates the node in the UXML file + emits the corresponding default USS class stub when `--seed-uss` flag set. Atomic — re-runs are idempotent on `(slug, parent_path, name)`.

**`ui_toolkit_panel_node_remove`** — Inputs: `slug`, `node_path`. Removes the node + cascade-removes its children + flags orphaned USS rules in `Assets/UI/Generated/{slug}.uss` (drift report; doesn't auto-delete to preserve hand-tuned overrides).

**`ui_toolkit_panel_node_reorder`** — Inputs: `slug`, `parent_path`, `child_name`, `new_ord`. Reorders children within a parent. Useful for HUD cluster reordering (e.g. shifting AUTO/MAP/BUDGET buttons in the right cluster) without rewriting the whole UXML.

**`ui_toolkit_uss_rule_upsert`** — Inputs: `slug`, `selector` (e.g. `.budget-panel__row-value` or `.budget-panel__btn:hover`), `properties{}` (e.g. `{"color": "#5b7fa8", "font-size": "13px"}`), optional `position` (`prepend` / `append` / `before:{other-selector}` / `after:{other-selector}`). Writes the rule into `Assets/UI/Generated/{slug}.uss` preserving literal-hex inline values. Idempotent on `(slug, selector)`.

**`ui_toolkit_tss_token_upsert`** — Inputs: `theme` (`cream` | `dark`), `token_slug` (e.g. `--ds-color-bg-card`), `value` (hex string). Writes the token into `Assets/UI/Themes/{theme}.tss` `:root` block. Validates hex format. Lays the groundwork for the eventual DB-canonical TSS emit pathway (TECH-34681) — this tool stays file-rooted in the current pipeline, swaps to DB-rooted automatically once the emitter ships.

**`ui_toolkit_panel_uxml_replace`** — Inputs: `slug`, `uxml` (full document string). Whole-file replacement when node-level edits would be too noisy (e.g. wholesale layout pivot). Schema-validates against `UIElementsSchema/UIElements.xsd` before write + ensures `<Style src="">` references resolve.

### §3.3 Wire (4 — Host + ModalCoordinator + scene UIDocument)

**`ui_toolkit_host_q_bind`** — Inputs: `host_class`, `element_name`, `element_kind` (`Button` | `Slider` | `Toggle` | `Label` | …), `callback_handler` (method name to wire), `target_manager` (e.g. `EconomyManager.SetTaxRate`), optional `value_param` for parameterised actions. Either generates the Q-lookup + click-binding C# code stub to paste into the Host or, if `--apply` is set, rewrites the Host's `OnEnable` block directly. Idempotent on `(host_class, element_name)`. Per DEC-A28 invariant I4, Host C# stays outside the bake pipeline — this tool generates code-stubs but doesn't auto-apply without an explicit flag.

**`ui_toolkit_modal_slug_register`** — Inputs: `slug`, `host_class`, optional `exclusive_group` (e.g. `"hud-modals"`). Records the panel-slug ↔ host-class mapping that `ModalCoordinator.RegisterMigratedPanel` expects + asserts the Host actually calls `_coordinator.RegisterMigratedPanel(slug, root)` in `OnEnable`. Reports drift if Host source doesn't match.

**`ui_toolkit_scene_uidoc_validate`** — Inputs: `slug`, optional `scene` (default `CityScene`). Checks whether `{slug}-uidoc` GameObject exists in scene YAML + the linked UIDocument component has the right `sourceAsset` GUID + the linked Host MonoBehaviour is attached. Returns a structured verdict (`{wired, missing, suggestion}`); when `missing`, the suggestion field outlines either a `wire_ui_documents` bridge call or the `RuntimeInitializeOnLoadMethod` runtime-spawn pattern (the `HoverInfoHost` / `MapPanelHost.Bootstrap` precedent).

**`ui_toolkit_blip_binding_register`** — Inputs: `host_class`, `element_name`, `blip_id` (`UiButtonClick` | `UiButtonHover` | etc.). Asserts the Host's `BindHudBlips` / `BindMainMenuBlips` etc. method includes the element in its `ToolkitBlipBinder.BindClickAndHover` array. Generates the patch when missing.

### §3.4 Verify (5 — post-edit sanity)

**`ui_toolkit_panel_render_preview`** — Inputs: `slug`, optional `panel_state{}` (which sub-view, which active state, which form values to populate). Enters Play Mode via the Unity bridge, opens the panel through `ModalCoordinator.Show(slug)` or via dispatched action, captures `include_ui=true` screenshot, returns the artifact path under `tools/reports/bridge-screenshots/`. Wraps the bridge orchestration so an agent doesn't have to hand-stitch `unity_bridge_command` calls. Requires `unity_bridge_lease(acquire)` per existing bridge protocol.

**`ui_toolkit_panel_pixel_diff`** — Inputs: `slug`, optional `theme` / `resolution`. Renders the panel via `ui_toolkit_panel_render_preview`, compares against the locked golden under `tools/visual-baseline/golden/{slug}*.png`, returns `{pass, pixel_delta_pct, side_by_side_path}`. Powers fast-feedback regression checks during an edit loop. Any non-pass is a rollback signal.

**`ui_toolkit_drift_scan`** — Inputs: optional `slug_filter`. Triple-output drift between (a) `Assets/UI/Generated/{slug}.uxml` disk content and the in-memory result of emitting from current DB `panel_child` rows, (b) `Assets/UI/Generated/{slug}.uss` disk content vs emitted USS, (c) `Assets/UI/Themes/{theme}.tss` disk content vs emitted TSS from `token_detail` rows. Extends existing `ui_def_drift_scan` (which currently only diffs rect_json against `panels.json` snapshot) to the full UI Toolkit triple output. Powers TECH-34686.

**`ui_toolkit_host_lint`** — Inputs: optional `host_class` or all. Lint suite over Host source: every `root.Q<T>("name")` resolves to a real UXML element name, every `.clicked +=` handler has a matching `-=` in `OnDisable`, no `FindObjectOfType` calls inside `Update()`, every `ModalCoordinator.RegisterMigratedPanel` slug has a corresponding panel UXML on disk, `[SerializeField] UIDocument _doc` field is wired in scene (cross-checks with `ui_toolkit_scene_uidoc_validate`), no `Q<T>` lookup returns null without a guard. Output is a structured `{host, file, line, code, severity, fix_hint}` finding list.

**`ui_toolkit_panel_dependents`** — Inputs: `slug`. Returns every cross-panel dependent: parents that include this slug as a sub-panel via `<ui:Template>` (none currently — flagged for Path B authoring), Hosts that Q-lookup elements with names matching this UXML's element names, action ids that toggle this panel's visibility via `ModalCoordinator.Show(slug)`, sibling panels in the same exclusive group, and prior visual baselines. Powers "what breaks if I rename this element" impact analysis before a node remove/rename.

## §4. Boundary notes

- **Pipeline shape.** Tools above sit at the current pipeline split — disk UXML/USS/TSS is canonical for read + author paths, DB rows stay prefab-canonical until Path B emitter parity ships. The `_node_upsert` / `_uss_rule_upsert` / `_tss_token_upsert` mutations write to disk now; once the emitter sidecar is parity-complete (TECH-34678..82), the same tools transparently flip to DB-rooted writes via `panel_child` / `token_detail` upserts + bake roundtrip. Same MCP surface, swapped backend.

- **Out of scope (deliberate).**
  - No tool for "generate a brand-new panel UXML from a prompt" — that's design-explore work, not a mechanical MCP slice.
  - No tool for "run a full visual regression sweep across all panels" — that's a `npm run` script wrapping `ui_toolkit_panel_pixel_diff` over the panel list.
  - No tool for "retire DEC-A24 prefab path" — that's a Stage 6 closeout plan.
  - No web `asset-pipeline` REST surface here — that's a downstream exploration once DB is canonical.
  - No Figma / Claude Design / A2UI ingest — separate exploration.

- **Bridge dependencies.** `ui_toolkit_panel_render_preview` + `ui_toolkit_panel_pixel_diff` require the same lease + Editor-on-REPO_ROOT preconditions as existing `unity_bridge_command capture_screenshot`. Pure read tools (`_get` / `_list` / `_tree` / `_host_inspect` / `_uss_resolve`) work offline against the repo + Postgres only.

- **Versioning.** `ui_toolkit_panel_node_upsert` does NOT auto-publish a catalog version — agents call `ui_panel_publish` (existing slice) when the panel reaches a known-good state. Keeps the publish gate visible. Once Path B ships, a parallel `ui_toolkit_panel_publish` may be needed for the UI Toolkit shape.

- **Naming consistency.** `_get` returns full payload, `_list` returns slug+summary array, `_tree` returns nested structural view, `_inspect` returns code-shape analysis, `_resolve` returns "lookup X → return value/rule/source", `_register` / `_bind` / `_validate` / `_lint` / `_diff` / `_scan` mirror existing convention.

- **Atomicity.** Every mutation tool is idempotent on its natural key. Re-running with the same args is a no-op (or a sanity-pass after a manual edit drift).

- **Test surface.** Every tool needs a red-stage proof — preferred shape: `ia/skills/_preamble/` + per-tool MCP smoke under `tools/mcp-ia-server/src/tools/__tests__/`.

## §5. Open design questions (grill targets for design-explore)

- **Tool delivery cadence.** Big-bang all 20 in one master plan, or per-stage rollout (Inspect first → Author next → Wire → Verify)?
- **Backend abstraction layer.** Should the seed introduce a `IUIToolkitPanelBackend` interface (`DiskBackend` / `DbBackend`) inside `tools/mcp-ia-server/src/ia-db/` so the flip from disk-canonical to DB-canonical is one config switch?
- **Runtime-VE first-class support.** Tools should accept runtime-only panels (`HoverInfoHost`, `MapPanelHost.BuildRuntimePanel`) as targets. How does `_panel_get` represent a panel that has no UXML file? Synthesize a virtual tree from C# AST scan?
- **MCP author allow-list.** Per existing slice convention (`is_caller_authorized`), which agent classes can call the mutation tools? `spec-implementer` + `plan-author` only, or wider?
- **Cache strategy.** `_panel_get` is heavy (UXML parse + USS parse + Host AST scan + scene YAML scan). Cache key + bust trigger?
- **Drift gate wiring.** Does `ui_toolkit_drift_scan` join `validate:all` (recovery plan §15.4.3.4 future state) or stay manual?
- **Schema for `params{}`.** Each `kind` has different required params (`button.action_id`, `slider.low-value/high-value`, `dropdown.choices[]`). Validate via per-kind YAML schema (mirror `tools/blueprints/panel-schema.yaml` pattern)?
- **Pixel-diff tolerance.** What's the per-property tolerance for `_pixel_diff` to pass? Reuse `tolerance_pct` from `ui_visual_baseline_record` (default 0.005)?
- **Lease + concurrency.** Multiple mutation tools touching the same panel in one agent run — lock the slug?
- **Code-stub vs apply mode.** `_host_q_bind` proposes code-stub-or-apply via flag. Does design-explore want a separate `_host_q_apply` tool to keep the surface unambiguous?
- **Test harness.** What red-stage test surface for tools that drive bridge (`_render_preview` / `_pixel_diff`)? Mock the bridge or run against a sentinel scene?
- **Migration path.** When Path B emitter parity ships per slug, how do existing iter-43 disk UXML files migrate to DB rows? Tool-driven (`ui_toolkit_panel_db_seed` one-shot importer) or out-of-band script?

## §6. Cross-refs

- `docs/explorations/ui-toolkit-emitter-parity-and-db-reverse-capture.md` — Path B emitter parity seed (parent context for this slice work).
- `docs/explorations/ui-as-code-state-of-the-art-2026-05.md §4.9` — `ui_panel_get` tree extension precedent.
- `docs/explorations/ui-as-code-state-of-the-art-2026-05.md §4.8` — visual baseline precedent.
- `docs/explorations/ui-panel-tree-db-storage.md` — panel-tree DB authoring patterns.
- `docs/explorations/ui-bake-pipeline-hardening-v2.md` — bake pipeline + runtime contract.
- `ia/specs/architecture/decisions.md` DEC-A24 (legacy prefab bake) + DEC-A28 (UI Toolkit strangler).
- `ia/specs/ui-design-system.md §Codebase inventory (UI Toolkit overlay — current UI baseline)` — canonical context.
- `ia/specs/glossary.md §User interface — UI Toolkit (current UI baseline)` — canonical vocabulary.
- `tools/visual-baseline/golden/` — locked goldens (visual contract).
- `tools/mcp-ia-server/src/tools/ui-*.ts` — existing slice implementations (template for new ones).
- `ia/rules/agent-principles.md` — token economy, MCP-first invariants.
- Memory: `feedback_db_primary_pivot`, `feedback_ui_bake_prefab_rebake`.

## §7. Design Expansion

### Chosen Approach

**Phased rollout — 4 stages: Inspect → Author → Wire → Verify. Narrowed 20-tool seed to 9-tool agent surface.**

| Phase | Tools shipped |
|---|---|
| Inspect (Stage 1) | `ui_toolkit_panel_get`, `ui_toolkit_host_inspect` |
| Author (Stage 2) | `ui_toolkit_panel_node_upsert`, `ui_toolkit_panel_node_remove`, `ui_toolkit_uss_rule_upsert` |
| Wire (Stage 3) | `ui_toolkit_host_q_bind`, `ui_toolkit_scene_uidoc_validate` |
| Verify (Stage 4) | `ui_toolkit_panel_pixel_diff`, `ui_toolkit_host_lint` |

Dropped: `panel_list` (grep covers), `panel_tree` (compose from `panel_get`), `uss_resolve` (grep covers), `panel_node_reorder` (compose from upsert), `tss_token_upsert` (Path B work), `panel_uxml_replace` (Write covers), `modal_slug_register` (merged into `host_lint`), `blip_binding_register` (defer), `drift_scan` (Path B work, TECH-34686), `panel_dependents` (defer v2), `panel_render_preview` (folded into `panel_pixel_diff` — one verify surface).

Rationale vs alternatives:
- **Big-bang** rejected — violates prototype-first; 9 unproven slices land together; one schema drift cascades.
- **Vertical-slice** rejected — backend-swappability invariant requires stable tool surface; per-slug iteration belongs at Path B cutover stage, not slice-build stage.
- **Phased** chosen — Q1 user hint (read-first); Inspect tools usable Stage 1 close; backend interface locks Stage 1 before Stage 2 mutations build on it; verify backstop lands before any Path B cutover.

### Architecture

**`IUIToolkitPanelBackend` boundary** — TypeScript interface at `tools/mcp-ia-server/src/ia-db/ui-toolkit-backend.ts`.

Read surface: `getPanel(slug)`, `listPanels(opts)`. Write surface: `writePanel(write)`, `upsertNode(slug, parent_path, node, ord)`, `removeNode(slug, node_path)`, `upsertUssRule(slug, selector, props, position?)`. Capability flag: `readonly kind: "disk" | "db"`.

**`DiskBackend` (Stage 1)** — reads `Assets/UI/Generated/{slug}.uxml` + `.uss`; parses UXML via `fast-xml-parser` (existing dep); parses USS via new lightweight tokenizer (`tools/mcp-ia-server/src/ia-db/uss-parser.ts`). Writes via direct `fs.writeFileSync` + post-write `unity_bridge_command asset_database_refresh`. Idempotency = re-serialize-then-byte-compare before write.

**`DbBackend` (stub Stage 1; impl Path B)** — JOINs `panel_detail` + `panel_child` rows; projects to `VisualElementNode` tree. Writes UPSERT rows in transaction + invokes `UxmlEmissionService` / `TssEmissionService` sidecar bake (TECH-34678..82). Stage 1 stub returns `{ok: false, error: "db_backend_not_implemented", parked_until: "TECH-34678..82"}`. Factory selects backend via env `UI_TOOLKIT_BACKEND=disk|db` AND per-slug feature flag in `feature-flags.json`.

**`panel_pixel_diff` stays backend-agnostic** — wraps existing `ui_visual_diff_run` engine (R2 review fix). Routes around `IUIToolkitPanelBackend`. Inputs: slug + theme. Calls `unity_bridge_command capture_screenshot include_ui:true scene:CityScene panel_slug:{slug}`. Compares against `tools/visual-baseline/golden/cityscene-{slug}*.png` regardless of backend. Pixel goldens = visual contract; backend swap that mutates rendered pixels = automatic fail.

**`host_inspect` + `host_q_bind` outside backend (DEC-A28 I4)** — both read `Assets/Scripts/UI/Hosts/{HostClass}.cs` directly via AST scan (`tools/mcp-ia-server/src/ia-db/csharp-host-parser.ts`, reuses `csharp-class-summary` utility). Write path (`host_q_bind --apply`) = direct `fs.writeFileSync` on Host source. No DB write, no bake invocation. Host C# is human-canonical per invariant.

### Subsystem Impact

- **New validators in `validate:all`**: `validate:ui-toolkit-panel-schema` (UXML kind/attr/class lint vs extended `panel-schema.yaml`), `validate:ui-toolkit-host-bindings` (runs `ui_toolkit_host_lint` repo-wide, fails CI on orphan Q-lookups / missing unsubscribes / missing modal slug registration).
- **`panel-schema.yaml` extension**: add top-level `panel_kind: ui-toolkit-overlay` alongside existing prefab kinds. New element kinds: `button`, `label`, `slider`, `toggle`, `dropdown`, `text-field`, `integer-field`, `scroll-view`, `visual-element`. Each kind declares required `params{}` keys for Stage 2 Zod validation.
- **Test harness**: `node:test + node:assert/strict` (NOT Vitest — review finding R1 corrected seed wording). Tests at `tools/mcp-ia-server/tests/tools/ui-toolkit-stage{N}-{slug}.test.ts`. Per-tool fixtures at `tools/scripts/test-fixtures/ui-toolkit-{tool}/`.
- **Bridge kinds**: NONE NEW. Existing covers all — `capture_screenshot include_ui:true`, `asset_database_refresh`, `unity_compile`, `get_compilation_status`.
- **Invariants flagged**: DEC-A28 I4 (Host C# outside bake — `host_q_bind --apply` enforces flag-gate); strangler per slug (all tools accept slug filter; mutation tools check `feature-flags.json` cutover gate); pixel goldens = contract (Stage 2 closeout protocol mandates manual golden re-record + human spot-check until Stage 4 lands); DB-primary pivot (mutations route through `IUIToolkitPanelBackend.writePanel`, never raw SQL or yaml edit).
- **Allow-list (R3 review fix)**: 4 mutation tools (`panel_node_upsert`, `panel_node_remove`, `uss_rule_upsert`, `host_q_bind --apply`) restrict via `is_caller_authorized: ["spec-implementer", "plan-author"]`. Read-only + lint tools open.
- **`ui_toolkit_drift_scan` deferral**: parks until Path B emitter ships (TECH-34678..86). Triple-output (UXML/USS/TSS) drift gate joins `validate:all` post-Path-B.

### Implementation Points

| # | Tool | File path | Template | Bridge kinds | Storage |
|---|---|---|---|---|---|
| 1 | `ui_toolkit_panel_get` | `tools/mcp-ia-server/src/tools/ui-toolkit-panel-get.ts` | `ui-panel.ts` | none | DiskBackend reads `.uxml`/`.uss`; DbBackend reads `panel_detail`+`panel_child` |
| 2 | `ui_toolkit_host_inspect` | `tools/mcp-ia-server/src/tools/ui-toolkit-host-inspect.ts` | `csharp-class-summary.ts` | none | Host `.cs` direct read (outside backend) |
| 3 | `ui_toolkit_panel_node_upsert` | `tools/mcp-ia-server/src/tools/ui-toolkit-panel-node-upsert.ts` | `ui-panel.ts` write pattern | `asset_database_refresh` | DiskBackend writes `.uxml`; DbBackend UPSERTs `panel_child` |
| 4 | `ui_toolkit_panel_node_remove` | `tools/mcp-ia-server/src/tools/ui-toolkit-panel-node-remove.ts` | row 3 | `asset_database_refresh` | same |
| 5 | `ui_toolkit_uss_rule_upsert` | `tools/mcp-ia-server/src/tools/ui-toolkit-uss-rule-upsert.ts` | row 3 + new `uss-parser.ts` util | `asset_database_refresh` | DiskBackend writes `.uss`; DbBackend UPSERTs `panel_child.style_props_json` |
| 6 | `ui_toolkit_host_q_bind` | `tools/mcp-ia-server/src/tools/ui-toolkit-host-q-bind.ts` | `csharp-class-summary.ts` + fs write | `unity_compile` + `get_compilation_status` when `--apply` | Host `.cs` direct write |
| 7 | `ui_toolkit_scene_uidoc_validate` | `tools/mcp-ia-server/src/tools/ui-toolkit-scene-uidoc-validate.ts` | `ui-def-drift-scan.ts` scene scan | none | scene YAML read |
| 8 | `ui_toolkit_panel_pixel_diff` | `tools/mcp-ia-server/src/tools/ui-toolkit-panel-pixel-diff.ts` | wraps `ui-visual-diff-run.ts` (R2 reuse) | `capture_screenshot include_ui:true` | reads golden, writes diff artifact under `tools/reports/` |
| 9 | `ui_toolkit_host_lint` | `tools/mcp-ia-server/src/tools/ui-toolkit-host-lint.ts` | `findobjectoftype-scan.ts` lint pattern | none | Host source read |

All 9 register in `tools/mcp-ia-server/src/server-registrations.ts` adjacent to existing `registerUiPanel*` block.

### Examples

End-to-end agent task: "Add a `budget-extra-info` button to the hud-budget panel that opens a modal."

```
1. ui_toolkit_panel_get slug=hud-budget
   → composite read returns uxml_tree + uss_rules + host_inspect + scene_uidoc + golden_manifest in one round-trip

2. ui_toolkit_panel_node_upsert
     slug=hud-budget
     parent_path=budget-panel/budget-panel__footer
     kind=button name=budget-extra-info
     classes=[budget-panel__btn, budget-panel__btn--info]
     params={text: "More info", action_id: "budget-open-extra-info-modal"}
     ord=2 --seed-uss
   → writes .uxml + seeds USS stub + triggers asset_database_refresh

3. ui_toolkit_uss_rule_upsert
     slug=hud-budget
     selector=.budget-panel__btn--info:hover
     properties={color: "#5b7fa8", background-color: "#1b3a5c"}
     position=after:.budget-panel__btn:hover
   → appends rule preserving literal hex, idempotent on (slug, selector)

4. ui_toolkit_host_q_bind
     host_class=BudgetPanelHost element_name=budget-extra-info
     element_kind=Button callback_handler=OnBudgetExtraInfoClicked
     target_manager=ModalCoordinator.Show value_param=budget-extra-info-modal
   → returns C# snippet (no --apply) for human review
   → human re-runs with --apply
   → rewrites BudgetPanelHost.cs + triggers unity_compile → green

5. ui_toolkit_scene_uidoc_validate slug=hud-budget
   → {wired: true} (existing scene wiring intact — slug unchanged)

6. ui_toolkit_host_lint host_class=BudgetPanelHost
   → checks Q-lookup resolves, .clicked has matching -=, modal slug present
   → {findings: [], status: clean}

7. ui_toolkit_panel_pixel_diff slug=hud-budget baseline=cityscene-budget-panel-open
   → captures screenshot via unity_bridge_command capture_screenshot include_ui:true
   → pixel_delta_pct: 0.83 (button added — expected delta)
   → human reviews side-by-side → confirms intentional → re-records golden via ui_visual_baseline_record
   → re-runs pixel_diff → {pass: true}
```

Seven steps, zero raw `Read`/`Edit`/`grep`, full UXML+USS+Host+scene+visual loop closed.

### Review Notes

**BLOCKING (3, all resolved):**
1. **R1** — Test framework drift: seed §3.4 said Vitest; repo uses `node:test + node:assert/strict`. Corrected in Phase 2 task fields + Phase 4 test-harness section. Tests live at `tools/mcp-ia-server/tests/tools/ui-toolkit-stage{N}-{slug}.test.ts`.
2. **R2** — `panel_pixel_diff` overlapped existing `ui_visual_diff_run`. Resolved: pixel_diff becomes thin slug-keyed wrapper over existing diff engine. Saves one implementation; new tool is a contract surface, not a new engine.
3. **R3** — Allow-list parked but 4 mutation tools bypass DB write-gate convention. Resolved: lock allow-list at design time — `is_caller_authorized: ["spec-implementer", "plan-author"]` for mutation tools. Read-only + lint tools stay open.

**NON-BLOCKING (carried into future iterations):**
- **R4** — Slug-lock concurrency: no per-slug mutation lock today. Same as existing `panel_detail_update`. Defer to v2 master plan; documented as known limitation.
- **R5** — Per-kind `params{}` schema: panel-schema.yaml extension sufficient short-term; extract per-kind YAML at Stage 5 if surface area grows beyond ~3 kinds.
- **R6** — Stage 4 verify-late risk: Stage 2 mutation tools ship without pixel-diff backstop for one stage. Mitigation: Stage 2 closeout requires manual `ui_visual_baseline_record` + human spot-check + golden re-record. Documented in Stage 2 done-def.
- **R7** — Runtime-only surfaces (HoverInfoHost, MapPanelHost.BuildRuntimePanel) have no scene UIDocument → `panel_pixel_diff` won't work for them. Scope gap; runtime-VE pixel diff parks until TECH-34683 (DB schema for programmatic/runtime-spawned VE surfaces).

### Expansion metadata

- **Date**: 2026-05-14
- **Model**: claude-opus-4-7[1m]
- **Approach selected**: Phased rollout (Inspect → Author → Wire → Verify); 9 tools narrowed from 20-tool seed.
- **Blocking items resolved**: 3 of 3.
- **Next command**: `claude-personal "/ship-plan ui-toolkit-authoring-mcp-slices"`
