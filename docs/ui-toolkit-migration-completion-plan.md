# UI Toolkit Migration — Completion Plan

**Status**: Draft (2026-05-13) · **Owner**: TBD · **Branch**: `feature/asset-pipeline`

Goal — finish the `ui-toolkit-migration` so CityScene + MainMenu render the canonical UI defined in `Assets/UI/Snapshots/panels.json` using UIToolkit, replacing all old UGUI Canvas surfaces.

---

## 1. Audit — what shipped vs what's missing

### What `ui-toolkit-migration` Stages 1–6 actually delivered

| Layer | Status | Path |
|---|---|---|
| UXML files (27 panels) | Generated | `Assets/UI/Generated/*.uxml` |
| USS files (27 panels) | Generated, **no screen anchors** | `Assets/UI/Generated/*.uss` |
| Host MonoBehaviours | Created, **mostly stub logic** | `Assets/Scripts/UI/Hosts/*Host.cs` |
| VM POCOs | Created, dataSource-bindable | `Assets/Scripts/UI/ViewModels/*VM.cs` |
| `ModalCoordinator` | Class exists | `Assets/Scripts/UI/Modals/ModalCoordinator.cs` |
| Old UGUI primitives | Quarantined `[Obsolete]` | `Assets/Scripts/UI/Themed/*.cs` |

### What was NEVER done (the gap)

| Missing | Symptom |
|---|---|
| **UIDocument GameObjects in scenes** | Stage 5.0 removed the Overlay Canvas component but added no UIToolkit replacement → empty scene UI |
| **Per-panel screen-anchor rules** | UXML/USS describe content, not screen position → all panels render at panel top-left, overlap |
| **`ModalCoordinator` GameObject in CityScene** | Removed with Canvas; `FindObjectOfType` returns null → modal hosts skip `RegisterMigratedPanel` |
| **`ModalDocumentHost`** | Referenced in `ModalCoordinator._modalDocument` docstring; class never written |
| **UIToolkit prefab bake** | `Assets/UI/Prefabs/Generated/*.prefab` are OLD UGUI prefabs (RectTransform + CanvasRenderer); no `UIDocument` |
| **Bake handler emits UIToolkit prefabs** | `UiBakeHandler.cs` still emits UGUI prefabs |
| **Per-panel sortingOrder design** | All UIDocuments default to `sortingOrder=0` → no z-control between HUD vs modals |
| **Host ↔ game state binding** | Most Hosts log stubs ("wire X in next pass") instead of reading EconomyManager/TimeManager/etc. |
| **Modal show/hide triggers** | Pause button, building click, glossary key, etc. not wired to `ModalCoordinator.Show(slug)` |
| **panels.json ↔ generated UXML mismatch** | panels.json has 13 panels; Generated/ has 27 UXMLs (Stage 5.0 hand-authored 14 extras with no canonical source) |
| **Old UGUI scene cleanup** | Stage 5.0 removed Canvas component; left `UI Canvas` GameObject + child prefab instances with missing-script refs |

### Session repair work (2026-05-13, this branch — already applied)

| Repair | Where |
|---|---|
| YAML parse fix (HTML comment removed) | `Assets/Scenes/CityScene.unity` line 177 |
| Bridge MenuItem to wire UIDocument GOs | `Assets/Scripts/Editor/AgentBridgeCommandRunner.UiDocumentWiring.cs` |
| ModalCoordinator GO re-added to CityScene | scene |
| HUD hosts no longer call `RegisterMigratedPanel` (toolbar, time-controls, mini-map, overlay-toggle-strip) | `Assets/Scripts/UI/Hosts/*Host.cs` |
| 18 UIDocument GOs created in CityScene + MainMenu | scene |
| Per-panel sortingOrder set (HUD=0, modal=10, pause/onboarding=20) | scene serialized |
| Stub absolute-anchor USS rules added to 5 HUD panels | `Assets/UI/Generated/{hud-bar,toolbar,mini-map,time-controls,overlay-toggle-strip}.uss` |
| Old UGUI root GOs deleted (hud-bar, toolbar, MiniMapPanel, ControlPanel, DebugPanel, ProposalUI, UIRegistries, UI Canvas, glossary-panel) | scene |

**Outcome**: scene loads + Play Mode runs + no modal stack-spam, but rendering does not match the canonical UI definitions — only checkbox stubs + empty mini-map frame visible. Hosts return stubs, no panel chrome, no anchoring fidelity.

---

## 2. Goal-state contract

A successful end state:

1. CityScene Play Mode shows the HUD as designed in `Assets/UI/Snapshots/panels.json` (hud-bar at top, toolbar on left, time-controls visible, mini-map top-right, overlay-toggle-strip in canonical location) — **content + chrome matches UXML/USS rendering at 1920×1080 reference**.
2. MainMenu Play Mode shows main-menu, new-game-form, splash, etc. with the same fidelity.
3. Modal panels (pause-menu, alerts-panel, glossary-panel, etc.) hidden by default; show on canonical trigger (key/button); stack via `sortingOrder`; close via coordinator.
4. **Zero** UGUI Canvas / CanvasRenderer / Image / ThemedPanel components in any scene.
5. `npm run unity:bake-ui` re-runnable end-to-end and idempotent — bake output ⇒ scene UI matches.
6. `npm run validate:all` + `unity:compile-check` green.
7. No "missing script" / NullReferenceException at scene load.

---

## 3. Phased implementation plan

### Phase A — Panel layout spec (source of truth for screen anchors)

Status: BLOCKING — every downstream phase depends on this.

| Task | File | Outcome |
|---|---|---|
| A1. Extend `panels.json` schema with `anchor` field | `Assets/UI/Snapshots/panels.json` + `tools/blueprints/panel-schema.yaml` | Each panel row carries `anchor: {kind: top-strip\|left-strip\|right-strip\|bottom-strip\|top-left\|top-right\|bottom-left\|bottom-right\|center\|center-top\|center-bottom\|fill, offset: {top?, left?, right?, bottom?}, size: {width?, height?}}` |
| A2. Fill anchor for 13 canonical panels | same | hud-bar=top-strip, toolbar=left-strip, time-controls=bottom-strip/right, stats-panel=center, etc. |
| A3. Decide fate of 14 extra UXMLs not in panels.json | `Assets/UI/Generated/` | Either add canonical rows (mini-map, time-controls, overlay-toggle-strip, zone-overlay, city-stats, building-info, tooltip, alerts-panel, glossary-panel, growth-budget-panel, splash, onboarding, load-view, onboarding-overlay) OR delete the orphaned UXML/USS pairs. Decision per slug. |
| A4. Add `scene` field per panel | panels.json | `scene: CityScene\|MainMenu\|Both` — drives which scene each panel deploys to. |
| A5. Add `default_visibility` field | panels.json | `visible\|hidden` — coordinator uses this on `RegisterMigratedPanel` to skip hide on HUD panels. |

Acceptance — `panels.json` validates against extended schema; every panel has anchor + scene + default_visibility.

### Phase B — UxmlBakeHandler emits per-anchor USS

Status: depends on A.

| Task | Outcome |
|---|---|
| B1. Update `Assets/Scripts/Editor/Bridge/UxmlBakeHandler.cs` to read `anchor` from panel record | bake emits `.{slug} { position: absolute; <anchor-rules>; }` automatically |
| B2. Add anchor-kind → USS template (top-strip ⇒ `top:0; left:0; right:0; height:auto;`, left-strip ⇒ `top:0; left:0; bottom:0; width:auto;`, center ⇒ `top:50%; left:50%; translate:-50% -50%;`, fill ⇒ `top:0; left:0; right:0; bottom:0;`) | shared helper `EmitAnchorRules(slug, anchorKind)` |
| B3. Bake handler also emits `pickingMode: ignore` on root panel element for non-interactive HUDs (hud-bar passive label strip) | USS adds `picking-mode: ignore` selectively |
| B4. Re-run `npm run unity:bake-ui` regenerates 13 USS with anchors | bake idempotent |

Acceptance — running bake produces USS where each `.{slug}` rule has `position: absolute` + anchor rules; manual USS edits no longer needed.

### Phase C — UiBakeHandler emits UIToolkit prefabs (not UGUI)

Status: depends on A, B.

| Task | Outcome |
|---|---|
| C1. Rewrite `Assets/Scripts/Editor/Bridge/UiBakeHandler.cs` prefab emission path | for each panel: create `GameObject` + `UIDocument` component + assign `PanelSettings` + assign baked `VisualTreeAsset` (the .uxml asset) + assign `sortingOrder` from anchor.layer field + save as `Assets/UI/Prefabs/Generated/{slug}.prefab` |
| C2. Drop the old UGUI prefab emission (RectTransform/CanvasRenderer/Image/ThemedPanel) | delete legacy bakers; obsolete renderers stay quarantined per Stage 6 |
| C3. Bake adds Host MonoBehaviour to the prefab + wires its `_doc` field via reflection | Host known per panel slug (`{slug}Host` → typed Class lookup) |
| C4. Bake also tags the prefab with `default_visibility` so scene-bootstrap can SetActive(false) on hidden panels | prefab carries an editor-only marker MonoBehaviour `UiPanelMeta { string slug; bool defaultVisible; }` |

Acceptance — every `Assets/UI/Prefabs/Generated/{slug}.prefab` contains exactly: GameObject + UIDocument + Host + UiPanelMeta — zero UGUI components.

### Phase D — Scene UI bootstrap

Status: depends on C.

| Task | Outcome |
|---|---|
| D1. Write `Assets/Scripts/UI/UiSceneBootstrap.cs` | scene MonoBehaviour, in `Awake`/`OnEnable` instantiates every prefab in `Assets/UI/Prefabs/Generated/` whose `UiPanelMeta.scene` matches the current scene name |
| D2. Bootstrap reads `UiPanelMeta.defaultVisible` → `gameObject.SetActive(visible)` | HUD active, modals inactive |
| D3. Bootstrap also creates `ModalCoordinator` GameObject if missing | self-heals scene |
| D4. CityScene + MainMenu each get a single `UiSceneBootstrap` MonoBehaviour mounted on the `UI` root GO | one place owns scene UI assembly |
| D5. Delete the in-scene wired UIDocument GOs and `ModalCoordinator` GO created by `AgentBridgeCommandRunner.UiDocumentWiring` (this session's repair) | scene is clean — bootstrap re-creates at Awake |
| D6. Retire `AgentBridgeCommandRunner.UiDocumentWiring.cs` (delete file) | one-off scaffolding gone |

Acceptance — opening CityScene fresh and pressing Play populates the scene at runtime with the correct panel set from the prefabs.

### Phase E — Host ↔ game state wiring

Status: depends on D.

| Task | Outcome |
|---|---|
| E1. Audit each Host's TODO/stub list | matrix `HOST × GAME_SERVICE` — e.g. HudBarHost ↔ CityStats+EconomyManager+TimeManager (already done), ToolbarHost ↔ ToolService, BuildingInfoHost ↔ BuildingSelectionService, etc. |
| E2. Implement each unwired Host's binding | each Host's OnEnable subscribes to the relevant service event, pushes values to VM properties |
| E3. Add modal trigger wiring | e.g. Esc key → `ModalCoordinator.Show("pause-menu")`; building click → `Show("building-info")`; G key → `Show("glossary-panel")` |
| E4. Wire close commands → `ModalCoordinator.HideMigrated(slug)` | consistent close-on-close-button across modals |
| E5. Re-enable `RegisterMigratedPanel` for HUD panels when bootstrap passes `defaultVisible=true` so coordinator can also force-hide them if needed | bootstrap calls `coordinator.RegisterMigratedPanel(slug, root, defaultVisible)` overload — Coordinator extended |

Acceptance — manual playtest checklist (per Host): HUD updates with money/date/happiness; pause menu opens on Esc; tool selection routes; building click opens info panel; etc.

### Phase F — Validation + cleanup

Status: depends on E.

| Task | Outcome |
|---|---|
| F1. `npm run validate:no-legacy-ugui-refs` — make it scan-only and assert no Canvas/CanvasRenderer in CityScene/MainMenu | CI blocks regression |
| F2. Delete `Assets/UI/Prefabs/Generated/*.prefab` that have no panels.json entry | orphan prefabs gone |
| F3. Delete `Assets/UI/Generated/*.uxml` + `.uss` that have no panels.json entry | orphan UXML/USS gone |
| F4. Delete unused old UGUI Themed* MonoBehaviour scripts (already `[Obsolete]`) | dead code removed |
| F5. Add `tests/ui-toolkit-migration-completion/stage{N}.test.mjs` per stage | red→green protocol per `ia/skills/ship-cycle/SKILL.md` |
| F6. Update `Assets/UI/Snapshots/panels.json` schema docs in `docs/ui-design-system/` | docs aligned |

Acceptance — `npm run validate:all` + `unity:compile-check` green; CI gate blocks any UGUI Canvas re-introduction in CityScene/MainMenu.

### Phase G — MainMenu + auxiliary scenes (mirror of A–F for MainMenu surface)

Status: depends on F.

| Task | Outcome |
|---|---|
| G1. MainMenu scene gets the same `UiSceneBootstrap` mount | bootstrap pattern reused |
| G2. main-menu, new-game-form, save-load-view, settings-view, splash, onboarding panels render via UIToolkit | MainMenu identical fidelity to CityScene |

Acceptance — fresh project launch shows splash → main-menu → new-game-form, all UIToolkit, no UGUI.

---

## 4. Stage decomposition for `ship-cycle`

Each Phase A–G becomes one Stage in a new master plan `ui-toolkit-migration-completion`. Stage 1.0 = tracer slice (Phase A schema field + one panel migrated end-to-end through B/C/D/E). Stage 2.0 = remaining 12 canonical panels through B/C/D/E. Stage 3.0 = validation + cleanup (F). Stage 4.0 = MainMenu (G).

Branch — fresh `feature/ui-toolkit-migration-completion` off `main` (current `feature/asset-pipeline` carries session repair scaffolding that Phase D step D6 removes).

---

## 5. Risks + open questions

| Risk | Mitigation |
|---|---|
| Anchor schema in panels.json conflicts with web tooling (`web/design-refs/step-1-game-ui/cd-bundle/panels.json` shares schema) | Bump schema_version; web mirror reads same field shape; check `tools/scripts/__tests__/fixtures/cd-game-ui/` fixtures regenerate |
| 14 extra UXMLs (mini-map, time-controls, overlay-toggle-strip, etc.) — were they intended to be canonical? | Open question for design owner; default = add canonical rows, do not delete |
| `PanelSettings` reference resolution 1920×1080 — confirm this matches target deploy resolution | playtest at multiple aspect ratios, may need 16:10 / 16:9 / 21:9 variants |
| Old UGUI prefabs still referenced by code (e.g., `ToolbarDataAdapter`) | Phase F4 audits + retires; if a Host depends on adapter, port to Host code |
| Hosts depend on services not present in MainMenu (TimeManager etc.) | Bootstrap per-scene; MainMenu hosts have null-guard |
| Bake handler partial-class extension may exceed file size budget | atomize per Strategy γ when triggered |

---

## 6. Estimated effort

| Phase | Effort | Risk |
|---|---|---|
| A | 0.5d | low |
| B | 1d | low |
| C | 2d | medium (prefab generation testing) |
| D | 1d | low |
| E | 3–5d | high (per-Host binding count) |
| F | 1d | low |
| G | 1d | low |

Total — 9–12 working days. Critical-path = E (Host wiring).

---

## 7. Next action

1. Open BACKLOG issue `TECH-XXXXX` — `ui-toolkit-migration-completion` (umbrella).
2. Run `/master-plan-new` with this doc as exploration seed → master plan with Stages 1–4.
3. Stage 1.0 tracer = pick `hud-bar` (already partially-wired) + end-to-end migrate it through Phase A → E, prove the pipeline.
