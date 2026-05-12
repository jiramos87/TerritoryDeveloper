---
purpose: "Research dump on state-of-the-art ui-as-code / agentic UI authoring for Unity (May 2026), plus audit + critique + improvement ideas for current territory-developer ui-as-code pipeline."
audience: agent
loaded_by: on-demand
created_at: 2026-05-12
related_docs:
  - docs/explorations/ui-implementation-mvp-rest.md
  - docs/explorations/ui-bake-pipeline-hardening-and-closed-loop-validation.md
  - docs/explorations/ui-bake-pipeline-hardening-v2.md
  - docs/explorations/ui-panel-tree-db-storage.md
  - web/lib/design-system.md
related_mcp_slices:
  - ui_panel_get
  - ui_panel_list
  - ui_component_get
  - ui_token_get
  - ui_def_drift_scan
---

# UI-as-code — state of the art (May 2026)

## §1. Research findings (pure)

### 1.1. Unity UI Toolkit — current baseline

- **UI Toolkit** = Unity's modern retained-mode UI framework. Replaces uGUI (legacy) + IMGUI for new work.
- Authoring triad: **UXML** (HTML-inspired structure), **USS** (CSS-inspired styling), **C#** (logic + bindings).
- **Retained mode**: framework owns the `VisualElement` tree in memory; framework reconciles renders against state diffs.
- **Procedural authoring**: `VisualElement` can be created + nested + styled fully from C# without UXML; pattern used for dynamic / data-driven UIs (inventory grids, dialog list, dungeon HUDs).
- UXML is the recommended authoring format (designer + diff friendly), but pure-C# is supported and used in production.
- Stable on mobile + desktop + console as of Unity 6 / 2026 cycle.

### 1.2. Unity 2026 roadmap deltas (Unite 2025 → 2026 releases)

- **World-space UI** in UI Toolkit (UI panels in 3D world coords, not just screen overlay).
- **Custom shaders + filters + vector graphics** in UI Toolkit.
- **Scene hierarchy rewrite** using UI Toolkit — scales to millions of objects.
- **CoreCLR migration** + verified packages (Unity 6.4+).
- **ECS-GameObject unification** — ECS becomes a core engine package; ECS components attach to GameObjects without re-architecture.
- **Unity AI** in-editor agent (open beta 2026): generates scripts, scenes, UI; aware of scene hierarchy, target platform, installed packages; supports **AI Gateway** to plug in Claude / GPT / custom models without consuming Unity credits.

### 1.3. UI Toolkit data binding (Unity 6 generation)

- **Runtime data binding system** — properties of `VisualElement` link to data source paths; source change auto-propagates to UI, and vice versa (two-way).
- Sources: `ScriptableObject`, `MonoBehaviour`, plain C# objects with `INotifyPropertyChanged`.
- Bindings authored from **UI Builder** (visual) or C# / UXML attribute.
- Fully extensible: custom converters, custom binding modes, `UxmlAttribute` + `UxmlElement` codegen attributes for custom controls.
- Available in Editor windows + runtime; same binding shape in both.

### 1.4. MVVM / MVP patterns on UI Toolkit

- **MVVM** is the natural fit on UI Toolkit because of two-way data binding. View = UXML + USS, ViewModel = `MonoBehaviour` or POCO with `INotifyPropertyChanged`, Model = `ScriptableObject` or game state.
- **MVP** remains a fallback for projects on uGUI where binding is missing.
- **Unity App UI package** ships a first-party MVVM toolkit (`com.unity.dt.app-ui`) with dependency injection + commands + observable ViewModels.
- Community toolkits: `LibraStack/UnityMvvmToolkit`, `bustedbunny/com.bustedbunny.mvvmtoolkit`, Microsoft Community Toolkit MVVM (`CommunityToolkit.Mvvm`) — all interop with UI Toolkit binding system.
- Trade-off: binding setup overhead (source, path, mode, converter) — MVVM mostly worth it for large / complex panels.

### 1.5. Fluent / declarative UI authoring on top of UI Toolkit

- **UTK-Fluent-extension** (`breadnone/UTK-Fluent-extension-for-UIToolkit`): method-chained C# wrapping of `VisualElement` (e.g. `el.BackgroundColor(...).Top(10).OnClick(...)`).
- **Affinity UI** (`mattolenik/AffinityUI`): declarative functional GUI for Unity with data binding + reactive state.
- **MarkLight** (`koyoki.github.io/marklight`): XUML — Unity-specific HTML-like markup.
- **inFluent** (`rubit0/inFluent`): generic fluent API for GameObject composition (extends to UI).
- **CSharpForMarkup** (`VincentH-Net/CSharpForMarkup`): concise declarative C# UI markup for .NET browser / native UI (port-able pattern).
- These all sit *on top of* UI Toolkit (or uGUI) — they do not replace it; they wrap it for code-first authoring.

### 1.6. Design tokens + theming in UI Toolkit

- **USS variables** (`--var-name`) defined in `:root` selectors → referenced via `var(--name)` anywhere; identical mechanic to CSS custom properties.
- **TSS (Theme Style Sheet)** = regular USS file Unity treats as a distinct asset; supports `@import` to compose token + component stylesheets.
- Theme switching at runtime: swap TSS asset on root `VisualElement`.
- Reference open-source design system: `sinanata/unity-ui-document-design-system` — drop-in dark-themed token palette + components + responsive layout, all driven by `var(--color-...)`.
- `App UI` package ships first-party theming over USS variables (colors, typography, motion).
- USS custom properties **can be mutated from C#** at runtime (color picker, theme experiments).

### 1.7. Figma → Unity UI Toolkit toolchain

- **FigmaToUnity** (`TrackMan/Unity.Package.FigmaToUnity`, open source): imports Figma pages → UXML + USS files.
- **Figma to UI Toolkit Converter** (Asset Store): handles auto-layout conversion.
- **Unity UI Exporter** (Figma plugin, community): designer-side export.
- **Unity AI Assistant** has first-party Figma import: extract assets + screen refs → generate UI Toolkit (or uGUI) in chosen framework.
- Tokens travel via Figma variables → exported as USS custom properties in most pipelines.
- Auth: Figma personal access tokens with `file_content:read` scope.

### 1.8. Agentic AI generation of Unity UI (2026)

- **Unity AI** (in-editor): integrated agent powered by frontier models (Gemini / Claude / GPT via AI Gateway). Knows scene hierarchy, target platform, installed packages → contextual UI generation.
- **UI Agent** sub-component of Unity AI: generates → validates → previews → assembles UI Toolkit assets; routed automatically by intent classifier; produces live previews before save.
- **Coplay** (`coplay.dev`): third-party AI assistant for Unity, supports UI generation + scene edits.
- **Unity MCP** ecosystem — multiple implementations:
  - `CoplayDev/unity-mcp` — bridge AI assistants (Claude / Cursor / Windsurf) to Unity Editor via MCP, IPC-channeled.
  - `CoderGamester/mcp-unity` — Node.js MCP server + WebSocket client to Unity Editor.
  - `IvanMurzak/Unity-MCP` — converts any C# method to MCP tool with one-line decorator.
  - `unity-mcp-server` (PyPI) — Python implementation.
- LiberateGames built production AI agents for Unity Editor via Python + Pydantic AI orchestrating LLMs via MCP for stable + cost-efficient UI codegen.
- `undreamai/LLMUnity` — embed LLMs inside Unity scenes for runtime AI characters (orthogonal to UI codegen).

### 1.9. Generative UI patterns (web-side, applicable to game UI)

Generative UI = parts of UI generated / selected / controlled by AI agent at runtime, not predefined.

Three production patterns (2026):

1. **Controlled GenUI** — pre-built component palette, agent picks which to show + passes props. Owner: dev controls layout + styling + interactions; agent controls *when* + *which* + *with what data*.
2. **Declarative GenUI** — agent returns structured UI description (JSON / JSONL); frontend renders it. Standards:
   - **A2UI** (Google, v0.9, May 2026) — JSONL-streamed declarative GenUI spec; messages: `createSurface`, `updateComponents`, `updateDataModel`, `deleteSurface`. Framework-agnostic.
   - **Open-JSON-UI** — open standardization of OpenAI's internal declarative GenUI schema.
   - **MCP Apps** — Anthropic / community spec building on MCP for app-like UI surfaces.
3. **Open-ended GenUI** — agent emits raw component code (TSX / UXML / C#) per request. Highest freedom + highest variance.

### 1.10. Agent ↔ UI runtime protocol (AG-UI)

- **AG-UI** = bidirectional Agent-User Interaction protocol; carries events between agentic backend + frontend.
- Pairs with A2UI / Open-JSON-UI / MCP Apps as the *transport* under the *content spec*.
- Supports streaming partial UI updates (progressive rendering), human-in-the-loop, tool calls visible in UI.
- Reference impl: `ag-ui-protocol/ag-ui` (GitHub).

### 1.11. Generative UI frameworks (React-side)

- **CopilotKit** — first-party AG-UI + GenUI integration for React; supports A2UI + Open-JSON-UI + custom specs.
- **Tambo** (`tambo-ai`) — full-stack GenUI SDK for React; bridges LLMs ↔ React component library; conversation-aware.
- **LangGraph / LangSmith** — generative UI on React via tool-call descriptions.
- **Open Agent Specification** (Oracle / Open Source) — defines what an agent runs; AG-UI + A2UI handle interaction + presentation.

### 1.12. ECS + UI Toolkit interop

- Pure-ECS projects lack first-party UI binding; bridge pattern needed.
- **ecs-mvvm** (`mchamplain/ecs-mvvm`) — MVVM bridging ECS state ↔ UI Toolkit.
- **Leopotam/ecs-ui** — small UI binding for Leo ECS.
- Unity's 2026 ECS-GameObject unification is expected to bring ECS-aware data sources to UI Toolkit binding system (preview as of 2026.x).

### 1.13. Hot reload + iteration speed

- Built-in: Domain Reload toggle + Burst-aware editor cycles in Unity 6.
- **FastScriptReload** / **Hot Reload for Unity** (`hotreload.net`) — patch C# methods live during Play Mode (<1 s iteration), no domain reload.
- UI Builder live preview: edit UXML / USS → see UI update in panel without play.

### 1.14. Visual regression / snapshot testing

- **Sauce Visual** / **Applitools Eyes** / **Percy** — AI-diff cloud visual regression.
- Open source: **BackstopJS**, **Playwright `toHaveScreenshot()`**, **Cypress**, **Vitest Visual Regression Testing**.
- Pattern: capture baseline → compare → flag layout shifts / color shifts / missing elements.
- Stabilization features: animation freeze, dynamic-region masking, "safe area" excludes.
- Unity native: no built-in; community uses screenshot-export → off-engine diff (Percy / Playwright / custom Imagemagick) or Test Framework Image Comparison helpers.

### 1.15. JSON / IR-driven UI authoring (general)

- Pattern: structured JSON / YAML / DSL doc → compile to UI tree (HTML / UXML / native widgets).
- Web side: Telerik Reporting, JSON Forms, Microsoft Adaptive Cards, React JSON Schema Form.
- Game / native: A2UI (above), MarkLight XUML, Affinity UI declarative blocks.
- Drivers: round-trip designer ↔ engine; agent emit / read; multi-platform export.

### 1.16. ScriptableObject as UI data source

- Canonical Unity pattern: `ScriptableObject` holds data + optional behavior; binds to UI Toolkit via `SerializedObject` binding.
- Use cases: quest data, perk data, settings profile, enemy stats, theme palette, button registry.
- Pattern naturally supports MVVM Model layer + asset-database asset reuse (`Resources.Load` / Addressables).

---

## §2. Audit — current ui-as-code system in territory-developer

### 2.1. Architecture overview

System sits on **uGUI** (not UI Toolkit) with code-driven prefab generation. Authoring flow is **DB → JSON snapshot → C# bake handler → prefab on disk → scene load**.

### 2.2. Authoritative source = Postgres (DB-primary)

- **Catalog tables**: `catalog_entity` (kind ∈ panel | component | token | button | sprite | audio | …), versioned via `current_published_version_id`; retire flag.
- **Detail tables**: `panel_detail` (rect / layout / padding / gap / params), `panel_child` (parent panel × slot × order × child entity ref), `component_detail` (role + default props + variants), `token_detail` (token kind + value), `button_detail` (sprite ref + state palette).
- **Versioning**: catalog entries publishable + version-pinned via `catalog_*_publish` MCP tools. Retired rows kept for history.
- Mig 0031 introduced `panel_detail` + `panel_child`; mig 0137 seeded stats-panel; ~150+ migrations in `tools/postgres-ia/migrations/`.

### 2.3. Snapshot export = JSON intermediate representation (IR)

- Script: `tools/scripts/snapshot-export-game-ui.mjs`.
- Reads published rows + joins `panel_child` + `button_detail.sprite_icon_entity_id` → `sprite_detail.assets_path`.
- Writes three JSON files under `Assets/UI/Snapshots/`:
  - `panels.json` — schema v4: snapshot id + items[panel{slug, fields{display_name, layout_template, layout, gap_px, padding_json, params_json, rect_json}, children[{ord, kind, params_json, sprite_ref, layout_json, instance_slug}]}].
  - `tokens.json` — schema v1: items[token{slug, display_name, token_kind, value_json}].
  - `components.json` — schema v1: items[component{slug, display_name, role, default_props_json, variants_json}].
- Gate: `current_published_version_id IS NOT NULL AND retired_at IS NULL`.

### 2.4. Bake handler = Editor-side C# transformer

- Files: `Assets/Scripts/Editor/Bridge/UiBakeHandler*.cs` — 4 partials, ~7300 LOC total.
  - `UiBakeHandler.cs` (3167) — IR DTOs + parse + slot validation + bake orchestrator.
  - `UiBakeHandler.Archetype.cs` (2545) — archetype-specific bake (panels / interactives).
  - `UiBakeHandler.Frame.cs` (1049) — frame / decoration bake.
  - `UiBakeHandler.Button.cs` (551) — button bake (palette ramp + atlas slot + motion).
- DTO shape mirrors IR JSON; uses `JsonUtility` (no Newtonsoft) — discriminated unions modeled as optional-with-zero-default fields.
- Reads `panels.json` or `IR/*.json`; writes prefabs to `Assets/UI/Prefabs/Generated/` (49 prefabs current — `hud-bar`, `budget-panel`, `stats-panel`, `main-menu`, `pause-menu`, `settings-view`, `save-load-view`, `new-game-form`, themed-* + studio control + decoration prefabs).
- Theme asset: `UiTheme.asset` baked alongside, holds resolved token tables.

### 2.5. Runtime UI = MonoBehaviour primitives + adapters

- **Themed primitives**: `Assets/Scripts/UI/Themed/` — `ThemedButton`, `ThemedLabel`, `ThemedFrame`, `ThemedSlider`, `ThemedToggle`, `ThemedList`, `ThemedTabBar`, `ThemedTooltip`, `ThemedBadge`, etc. All extend `ThemedPrimitiveBase` → cache `UiTheme` ref in `Awake`, call `ApplyTheme(theme)` to consume token slugs.
- **Studio controls**: `Assets/Scripts/UI/StudioControls/` — `Knob`, `Fader`, `VUMeter`, `Oscilloscope`, `SegmentedReadout`, `IlluminatedButton`, `LED`, `DetentRing`. Each has a paired `Renderer` for visual layer.
- **Juice**: `Assets/Scripts/UI/Juice/` — `JuiceBase`, `NeedleBallistics`, `OscilloscopeSweep`, `TweenCounter`, `PulseOnEvent`, `ShadowDepth`, `SparkleBurst`.
- **Adapters**: `Assets/Scripts/UI/Modals/` + `Assets/Scripts/UI/HUD/` — bind game state ↔ themed primitives (`BudgetPanelAdapter`, `StatsPanelAdapter`, `HudBarDataAdapter`, `MapPanelAdapter`, `InfoPanelAdapter`, etc.).

### 2.6. Bind + Action registries (reactive layer)

- `UiBindRegistry` (MonoBehaviour) — typed `Set<T>` / `Get<T>` / `Subscribe<T>` over `Dictionary<string, object>`. Mounted per-scene under UI host. Notifies subscribers on Set.
- `UiActionRegistry` — typed action lookup by id (`budget.close`, `pause.resume`, …).
- `UiActionTrigger` — invokes registry actions from button bake output.
- `UiVisibilityBinder` — subscribes to bind ids → toggles GameObject active state.
- Bake output references registries by string id (e.g. `bindId: "budget.title"`, `actionId: "budget.close"`).

### 2.7. MCP slices (agent surface)

- `tools/mcp-ia-server/src/tools/ui-*.ts`:
  - `ui_panel_get` / `ui_panel_list` / `ui_panel_publish` / `panel_detail_update` — panel CRUD + version flip (shell only; children not surfaced yet — see exploration doc).
  - `ui_token_get` / `ui_token_list` / `ui_token_publish`.
  - `ui_component_get` / `ui_component_list` / `ui_component_publish`.
  - `ui_def_drift_scan` — file-level drift between DB snapshot + baked prefab.
  - `ui_calibration_corpus_query` / `ui_calibration_verdict_record` — agent-led calibration loop for screenshot verdicts.
  - `ui_bake_history_query` — track bake invocations.
- Shared DAL: `tools/mcp-ia-server/src/ia-db/ui-catalog.ts` (also imported by `web/asset-pipeline` backend).

### 2.8. Validators

- `tools/scripts/validate-ui-def-drift.mjs` — DB ↔ snapshot.json drift.
- `tools/scripts/validate-panel-blueprint-harness.mjs` — panel structural invariants.
- `tools/scripts/validate-ui-id-consistency.mjs` — bind id / action id consistency across registries.
- All wired into `validate:all` chain.

### 2.9. Theme + tokens

- USS-style variable mechanic via `UiTheme.asset` ScriptableObject; tokens resolved by slug at bake + at runtime (`ApplyTheme(theme)`).
- Web design system mirrored in `web/lib/design-system.md` + `web/lib/design-tokens.ts` (used by `asset-pipeline` web app, not Unity).
- DesignTokens not stored as USS variables (Unity uGUI, not UI Toolkit) — stored as DB rows + baked into `UiTheme.asset`.

### 2.10. Calibration corpus + agent loop

- `ia/state/ui-calibration-corpus.jsonl` — agent-recorded verdicts on baked panels (visual diff outcomes).
- `ui-element-grill` skill: 5-phase guided panel authoring (corpus → grill → draft → bake + verdict loop → publish).
- Per-panel calibration verdicts feed back to MCP slice via `ui_panel_get.corpus_rows[]`.

### 2.11. Bake → publish lifecycle

- `npm run unity:bake-ui` — invokes Unity editor in batchmode → `UiBakeHandler` → writes prefabs.
- Play-mode loads cached prefabs from `Assets/UI/Prefabs/Generated/`.
- Prefab edit ≠ source of truth — DB row is. Edits to prefab without re-bake are erased on next bake.

### 2.12. Scene hookup

- Two main UI hosts: `MainMenu.unity` (out-of-game) + `CityScene.unity` (in-game HUD + modals).
- Modal coordinator (`ModalCoordinator`) — push / pop modal prefabs.
- Adapter scripts attached to scene host GameObject; reference baked prefab refs via `UiAssetCatalog` + `CatalogPrefabRef`.

---

## §3. Critique — strengths + weaknesses

### 3.1. Strengths

- **DB-primary single source of truth.** Panel + component + token definitions live in versioned Postgres rows. No "designer edits prefab in scene → diverges from spec" failure mode common to Unity uGUI projects.
- **Bake pipeline is reproducible.** Same DB state → same JSON snapshot → same prefab output. CI-friendly + deterministic.
- **Versioning + retirement built in.** `catalog_entity` row lifecycle (draft → published → retired) gives clean rollback path + history.
- **Agent-friendly surface.** MCP slices (`ui_panel_get`, `ui_panel_list`, `ui_token_get`, `ui_def_drift_scan`) let agents query + author UI without touching files. Calibration corpus + verdicts give closed-loop feedback for agentic UI iteration.
- **Theme-driven primitives.** All themed components route through `UiTheme` → token swap → palette experiments are one ScriptableObject diff.
- **Drift gates exist.** `validate-ui-def-drift`, `validate-panel-blueprint-harness`, `validate-ui-id-consistency` enforce DB ↔ snapshot ↔ runtime parity.
- **Reactive bind layer.** `UiBindRegistry` decouples game state mutation from view update; subscribers compose freely.
- **JSON IR is human + agent readable.** `Assets/UI/Snapshots/panels.json` is grep-able + Cursor-readable; agents can inspect bake input without DB roundtrip.
- **Production-tested on real game.** Driving 49 prefabs across 2 scenes + multiple modals + a HUD bar; not just a toy.

### 3.2. Weaknesses

- **Built on uGUI, not UI Toolkit.** Misses retained-mode renderer perf, USS variables, runtime data binding, world-space UI roadmap, AI Assistant UI Agent native support. Stuck on legacy Unity UI stack as Unity 2026 actively deprecates it.
- **Bake handler is one massive file family.** ~7300 LOC across 4 partials in `UiBakeHandler*.cs`. Hub-thinning candidate (Strategy γ Tier D). Hard to onboard / extend / unit-test atomically.
- **No retained-mode reconciliation.** Bake regenerates prefab whole; any in-scene state (scroll position, focus) lost on rebake. No diff / patch path.
- **Bake requires Unity Editor in batchmode.** Slow round-trip — agent edits IR → must invoke Editor → wait for compile + bake → reload scene. No live preview from JSON.
- **JSON IR ≠ UXML / standard schema.** IR shape is bespoke (`IrRoot`, `IrPanel`, `IrTokens`, `IrPanelSlot`) — not portable to other engines, not consumable by Figma plugins, not aligned with A2UI / Open-JSON-UI / MCP Apps standards.
- **`panel_child.child_kind` CHECK constraint narrow.** Only 7 values (`button, panel, label, spacer, audio, sprite, label_inline`) but bake snapshot shows richer set (`tab-strip, range-tabs, chart, stacked-bar-row, service-row`) — routing through generic `panel` with `params_json.kind` is a hack.
- **`ui_panel_get` MCP slice returns shell only.** No `children[]` surfaced; agents see half-spec; cross-link to `panel_consumers[]` empty. Per exploration doc.
- **No visual regression / screenshot diff baseline.** Calibration corpus is verdict-style (agent says "good / bad"), not pixel-diff against locked baseline. Drift can pass verdict-loop while pixels shift.
- **No design tool round-trip.** No Figma import; no Figma export. Designer iteration runs entirely in DB rows or `ui-element-grill` skill prompts — no shared design canvas with non-engineer collaborators.
- **No hot reload.** UI changes require full Editor recompile + scene reload. FastScriptReload integration absent.
- **No fluent / declarative C# DSL.** Code-side authoring is direct `MonoBehaviour` + `RectTransform` manipulation in adapters; bake handler is imperative `Instantiate + SetActive + AddComponent`. Verbose; high bug surface; hard to compose.
- **No runtime UI authoring path.** Cannot author or hot-swap panels at runtime without re-baking; bake is Editor-only.
- **No generative UI runtime.** Agent can author panel DB rows + bake offline, but cannot drive runtime UI from agent decisions in-session. Missing AG-UI / A2UI-style transport.
- **MVVM pattern absent.** Adapters are MVP-ish but no first-party data binding mechanism between game state + view. Hand-rolled subscribe/publish via `UiBindRegistry`.
- **macOS case-insensitive FS hazard.** Domain services (`Domains/X/Services/`) need careful naming — already burned by `ZoneSService.cs` vs `ZonesService.cs` collision.
- **No ECS-aware bindings.** As game evolves toward DOTS, current `UiBindRegistry` does not bridge ECS components → view.
- **Theme token storage diverges between Unity + web.** Tokens live in `UiTheme.asset` (Unity) + `web/lib/design-tokens.ts` (web). Drift risk between the two surfaces.

---

## §4. Exploration — 10 improvements grounded in 2026 state-of-the-art

### 4.1. Migrate to UI Toolkit retained-mode renderer (modernization)

**Methodology**: incremental adopt UI Toolkit alongside uGUI. New panels author as UXML + USS + C# binding. Old panels stay uGUI until rebaked. Bake handler grows a second output path: `panels.json` → UXML + USS files (instead of prefab). Adopt **TSS theme switching** — `UiTheme.asset` tokens emit a generated USS variable block + TSS file at bake; runtime swaps TSS asset for theme variants.

**Wins**: world-space UI, perf, modern data binding, Unity AI Agent native compatibility.

### 4.2. Adopt MVVM data binding via UI Toolkit binding system

**Methodology**: ViewModels = `ScriptableObject` or POCO with `INotifyPropertyChanged`. Generated by codegen from `panel_detail.params_json` + `panel_child` entries (one VM per panel, one binding path per child with `bindId`). View = UXML emitted by bake. Replace `UiBindRegistry` hand-rolled subscribers with native UI Toolkit binding (path-based, two-way). Consider `LibraStack/UnityMvvmToolkit` or first-party `com.unity.dt.app-ui` MVVM toolkit.

**Wins**: less boilerplate, two-way binding, designer-readable bindings in UI Builder, testability.

### 4.3. Adopt A2UI / Open-JSON-UI as canonical IR

**Methodology**: replace bespoke `panels.json` schema with **A2UI v0.9 JSONL** format. Snapshot export emits `createSurface` + `updateComponents` + `updateDataModel` messages. Bake handler consumes A2UI stream + emits UXML or uGUI prefabs.

**Wins**: portable to non-Unity surfaces (web `asset-pipeline`, external tools), agent-ready (any LLM trained on A2UI spec can emit panels), standards alignment.

### 4.4. Add AG-UI runtime transport for generative UI

**Methodology**: stand up an **AG-UI server** inside Unity (TCP / WebSocket on a known port, or piggyback existing `unity_bridge_command` IPC). Runtime receives streamed UI updates from agent backend → reconciles to UI Toolkit tree without rebaking. Pattern = **Controlled GenUI** (curated themed-primitive palette; agent picks which + passes bind data). Power: in-session UI changes from agent decisions (dynamic tooltips, custom modals, AI-narrated dialog overlays).

**Wins**: live agentic UI surfaces inside playing game, no Editor roundtrip, opens door to runtime LLM-driven onboarding / tutoring / accessibility overlays.

### 4.5. Build a fluent C# UI DSL on top of UI Toolkit

**Methodology**: thin fluent extension layer wrapping `VisualElement` — pattern from `breadnone/UTK-Fluent-extension-for-UIToolkit` or `mattolenik/AffinityUI`. Inverts bake handler from imperative `Instantiate + AddComponent` to declarative chained C# (e.g. `Panel("budget-panel").Layout(VStack).Padding(0).Children(...)`).

**Wins**: bake handler shrinks from ~7300 LOC to ~1500; readable code-first authoring path for one-off / dynamic panels; lower bug surface; composable.

### 4.6. Atomize UiBakeHandler into per-archetype services (hub-thinning)

**Methodology**: apply Strategy γ atomization. Extract `UiBakeHandler.Archetype.cs` (2545 LOC) into `Domains/UI/Services/`:
- `PanelArchetypeService` — panel bake.
- `ButtonArchetypeService` — button bake (move `UiBakeHandler.Button.cs`).
- `FrameArchetypeService` — frame bake (move `UiBakeHandler.Frame.cs`).
- `StudioControlArchetypeService` — knob / fader / VU / oscilloscope.
- `JuiceArchetypeService` — juice layer attachment.

Hub `UiBakeHandler` becomes a thin coordinator. Composed test exercises whole pipeline; per-service tests unit-cover archetype edge cases. Cross-link to existing `large-file-atomization-hub-thinning-sweep` master plan.

**Wins**: faster onboarding, unit-testable bake fragments, parallel agent edits without conflict.

### 4.7. Add Figma round-trip

**Methodology**: integrate **FigmaToUnity** or build custom importer reading Figma REST API → emit DB rows (`panel_detail`, `panel_child`, `token_detail`). Reverse: snapshot exporter emits Figma plugin payload (JSON spec) for designer review. Tokens flow: Figma variables ↔ `token_detail.value_json` ↔ USS variables ↔ `UiTheme.asset`.

**Wins**: designer onboarding (non-engineer collab), tokens centralized cross-surface, visual change review before bake.

### 4.8. Pixel-level visual regression with baseline screenshots

**Methodology**: extend calibration corpus from agent-verdict shape to pixel-diff shape. Each published panel version snapshots a **golden screenshot** (rendered via Test Framework Image Comparison or Path A `unity:testmode-batch`). Subsequent bakes compare → flag layout shifts / token regressions. Use **Percy** / **BackstopJS** / Unity's `ImageAssert.AreEqual` for diff engine. Add `ui_visual_baseline_get` / `ui_visual_baseline_record` MCP slices.

**Wins**: catch silent regressions (token swap that shifts pixels but passes structural validators), version-locked visual contract, agent verdict + pixel verdict cross-check.

### 4.9. Extend `ui_panel_get` MCP slice to full tree + add `ui_panel_render_mock` slice

**Methodology**: per existing `ui-panel-tree-db-storage.md` exploration — join `panel_child` + recursive resolve to component spine. Add new slice `ui_panel_render_mock` that returns an ASCII / SVG / DOM mock of the panel tree from DB state alone (no Editor invocation). Agents inspect panels without bake. Pair with `ui_def_drift_scan` extended to tree-level drift (DB vs bake snapshot vs prefab).

**Wins**: agent self-service panel inspection, faster verdict loops, drift coverage closes shell-only gap.

### 4.10. Integrate Unity AI Agent + MCP for agent-led UI iteration

**Methodology**: register existing `territory-ia` MCP server as a tool surface for **Unity AI Assistant** (open beta 2026) via AI Gateway. Unity AI Agent gains read access to `ui_panel_get`, write access to `panel_detail_update` + `catalog_panel_publish`. Pair with **FastScriptReload** for sub-second iteration in Editor. Agent loop: read calibration verdict → mutate DB row → bake → screenshot → verdict-record → repeat. Front-end via Claude Code (existing `ui-element-grill` skill) + back-end via Unity AI for in-Editor preview + visual diff.

**Wins**: closed agentic loop *inside* Editor (no IDE bounce), live previews, designer-style iteration speed without designer.

---

Sources:
- [Unity UI Toolkit Manual (6000.3)](https://docs.unity3d.com/6000.3/Documentation/Manual/ui-systems/introduction-ui-toolkit.html)
- [Unity 2026 Roadmap — CoreCLR + ECS + AI tools](https://digitalproduction.com/2025/11/26/unitys-2026-roadmap-coreclr-verified-packages-fewer-surprises/)
- [Unity AI Open Beta Guide 2026](https://www.buildfastwithai.com/blogs/unity-ai-open-beta-guide-2026)
- [Unity AI's Open Beta — Unity Discussions](https://discussions.unity.com/t/unity-ai-s-open-beta-now-live-for-unity-6/1718560)
- [UI Toolkit Data Binding — Unity Learn](https://learn.unity.com/tutorial/ui-toolkit-in-unity-6-crafting-custom-controls-with-data-bindings)
- [MVVM Pattern — Unity App UI Docs](https://docs.unity3d.com/Packages/com.unity.dt.app-ui@0.4/manual/mvvm-sample.html)
- [Model-View-ViewModel pattern — Unity Learn](https://learn.unity.com/tutorial/model-view-viewmodel-pattern)
- [LibraStack/UnityMvvmToolkit](https://github.com/LibraStack/UnityMvvmToolkit)
- [bustedbunny/com.bustedbunny.mvvmtoolkit](https://github.com/bustedbunny/com.bustedbunny.mvvmtoolkit)
- [UTK Fluent Extension](https://github.com/breadnone/UTK-Fluent-extension-for-UIToolkit)
- [Affinity UI Framework](https://github.com/mattolenik/AffinityUI)
- [MarkLight XUML](https://koyoki.github.io/marklight/)
- [inFluent — Unity GameObject fluent API](https://github.com/rubit0/inFluent)
- [CSharpForMarkup](https://github.com/VincentH-Net/CSharpForMarkup)
- [sinanata/unity-ui-document-design-system](https://github.com/sinanata/unity-ui-document-design-system)
- [Unity USS Custom Properties / Variables](https://docs.unity3d.com/Manual/UIE-USS-CustomProperties.html)
- [Unity Theme Style Sheet (TSS)](https://docs.unity3d.com/Manual/UIE-tss.html)
- [TrackMan/Unity.Package.FigmaToUnity](https://github.com/TrackMan/Unity.Package.FigmaToUnity)
- [Unity Figma Bridge — simonoliver](https://github.com/simonoliver/UnityFigmaBridge)
- [Unity AI Assistant — Figma UI Generation](https://docs.unity3d.com/Packages/com.unity.ai.assistant@2.7/manual/assets/figma-ui.html)
- [Unity AI Assistant — UI Agent](https://docs.unity3d.com/Packages/com.unity.ai.assistant@2.0//manual/ui-agents-intro.html)
- [Coplay AI Assistant for Unity](https://coplay.dev/)
- [CoplayDev/unity-mcp](https://github.com/CoplayDev/unity-mcp)
- [CoderGamester/mcp-unity](https://github.com/CoderGamester/mcp-unity)
- [IvanMurzak/Unity-MCP](https://github.com/IvanMurzak/Unity-MCP)
- [LiberateGames Production AI Agents for Unity](https://www.liberate.games/blog/liberate-games-ai-agents)
- [undreamai/LLMUnity](https://github.com/undreamai/LLMUnity)
- [Generative UI Developer Guide 2026 — CopilotKit](https://www.copilotkit.ai/blog/the-developer-s-guide-to-generative-ui-in-2026)
- [Generative UI Frameworks 2026 — Medium](https://medium.com/@akshaychame2/the-complete-guide-to-generative-ui-frameworks-in-2026-fde71c4fa8cc)
- [A2UI Specification v0.9](https://a2ui.org/specification/v0.9-a2ui/)
- [A2UI — Google Developers Blog](https://developers.googleblog.com/a2ui-v0-9-generative-ui/)
- [AG-UI Protocol Overview](https://docs.ag-ui.com/introduction)
- [AG-UI GitHub](https://github.com/ag-ui-protocol/ag-ui)
- [AG-UI vs A2UI — CopilotKit](https://www.copilotkit.ai/ag-ui-and-a2ui)
- [Tambo — UI Toolkit for React Agents](https://www.blog.brightcoding.dev/2026/04/27/tambo-the-revolutionary-ui-toolkit-for-react-agents)
- [ecs-mvvm — ECS × UIToolkit MVVM](https://github.com/mchamplain/ecs-mvvm)
- [Leopotam/ecs-ui](https://github.com/Leopotam/ecs-ui)
- [FastScriptReload](https://github.com/handzlikchris/FastScriptReload)
- [Hot Reload for Unity](https://hotreload.net/)
- [Visual Regression Testing 2026 — Percy](https://percy.io/blog/visual-screenshot-testing)
- [Best Visual Regression Testing Tools 2026 — Sauce Labs](https://saucelabs.com/resources/blog/comparing-the-20-best-visual-testing-tools-of-2026)
- [Open Source Visual Regression Tools 2026 — Percy](https://percy.io/blog/open-source-visual-regression-testing-tools)
