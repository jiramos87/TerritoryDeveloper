---
purpose: "Seed doc for design-explore — propose the agent-side MCP slice surface (read + author + wire + verify) for UI Toolkit panel authoring + modification, so the Path B emitter-parity master plan has a mechanical primitive set ready before it sizes stages."
audience: agent
loaded_by: on-demand
created_at: 2026-05-14
status: seed-for-design-explore
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
