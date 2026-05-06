---
slug: game-ui-catalog-bake
target_version: 2
stages:
  - id: "9.7"
    title: "Picker → catalog conformance"
    exit: "SubtypePickerController reads panel shape + tile archetype + sprite slugs from asset-registry / sprite-catalog. Behavior parity for R/C/I/S. validate:asset-registry asserts motion.hover ∈ {tint,glow,scale}."
    red_stage_proof: |
      # Tracer: panel row → bake → IR → runtime tile renders with catalog sprite + theme tokens + hover motion.
      # Pre: subtype-picker panel row + picker-tile-72 archetype + 16 sprite-catalog rows seeded.
      seed_panel_row("subtype-picker", anchor=(0.5,0,0.5,0), sizeDelta=(0,88), layout="horizontal", padding=(10,10,8,8))
      seed_archetype("picker-tile-72", geom=(72,72), icon_inset=(6,18,-6,-6), motion={"hover":"tint"})
      for fam in ["residential","commercial","industrial"]:
          for tier in ["light","medium","heavy"]:
              seed_sprite(f"picker-{fam}-{tier}-icon-72", png_72x72)
      for s in range(7):
          seed_sprite(f"picker-state-{s}-icon-72", png_72x72)
      ir = bake_asset_registry()
      assert ir["panels"]["subtype-picker"]["sizeDelta"] == [0, 88]
      assert ir["archetypes"]["picker-tile-72"]["motion"]["hover"] == "tint"
      assert "picker-residential-light-icon-72" in ir["sprites"]
      controller.Show(uiManager, ToolFamily.Residential, defaultKey=0)
      assert panel_root.sizeDelta == (0, 88)
      assert tile_count() == 3
      tile = first_tile()
      assert tile.layoutElement.preferredWidth == 72
      assert tile.icon.sprite.name == "picker-residential-light-icon-72"
      assert tile.button.colors.highlightedColor == lerp(baseColor, white, 0.18)
      assert run_validator("asset-registry") == 0
    tasks:
      - id: "9.7.1"
        title: "Seed asset-registry rows — subtype-picker panel + picker-tile-72 archetype + 16 sprite-catalog rows (R/C/I tiers + S 7)"
        prefix: TECH
        kind: code
        depends_on: []
        digest_outline: "Add `subtype-picker` panel row + `picker-tile-72` archetype row to DB seed migration. Add 16 `sprite-catalog` rows: R/C/I × light/medium/heavy + S × 7. 72×72 PNGs under `Assets/UI/Sprites/Picker/` — Stage 9.6 AssetPostprocessor auto-registers. Archetype carries `motion: { hover: \"tint\" }` jsonb."
        touched_paths:
          - "tools/db/migrations/"
          - "Assets/UI/Sprites/Picker/"
      - id: "9.7.2"
        title: "Refactor SubtypePickerController to catalog-driven build (drop ResolveZoningSprite)"
        prefix: TECH
        kind: code
        depends_on: ["TECH-9.7.1"]
        digest_outline: "Replace `EnsureRuntimePanelRootIfNeeded` with catalog-driven panel build (read `subtype-picker` row from `UiAssetCatalog`). Replace `ResolveZoningSprite` with `sprite-catalog` lookup by slug `picker-{family}-{tier}-icon-72`. Pull tile geometry + motion.hover from `picker-tile-72` archetype. Behavior parity for R/C/I/S — no regression. Cache catalog accessor in Awake (rule 3)."
        touched_paths:
          - "Assets/Scripts/UI/SubtypePickerController.cs"
          - "Assets/Scripts/UI/TokenCatalog.cs"
      - id: "9.7.3"
        title: "motion.hover enum + validate:asset-registry constraint"
        prefix: TECH
        kind: code
        depends_on: ["TECH-9.7.1"]
        digest_outline: "Define enum `motion.hover ∈ {tint, glow, scale}` in archetype schema. Extend `validate:asset-registry` to assert every tile-role archetype carries `motion.hover` ∈ enum set. v1 implementation = tint only — `Button.colors.highlightedColor = LerpToward(baseColor, white, 0.18f)`. Glow/scale stub branches throw `NotImplemented`."
        touched_paths:
          - "tools/scripts/validate-asset-registry.mjs"
          - "Assets/Scripts/UI/SubtypePickerController.cs"
      - id: "9.7.4"
        title: "Tracer — PlayMode test: seed → bake → render → assert catalog-driven shape"
        prefix: TECH
        kind: code
        depends_on: ["TECH-9.7.2", "TECH-9.7.3"]
        digest_outline: "PlayMode test seeds 16 sprite rows + panel + archetype, runs bake, opens picker for Residential, asserts panel sizeDelta from catalog (NOT hardcoded), tile preferredWidth=72, icon sprite name=`picker-residential-light-icon-72`, hover color=lerp(baseColor, white, 0.18). Hard-fails if controller falls back to legacy literals."
        touched_paths:
          - "Assets/Tests/PlayMode/SubtypePickerCatalogTracerTest.cs"
  - id: "9.8"
    title: "Family subtype expansion — Power / Roads / Water / Forests"
    exit: "Every toolbar family except Bulldoze opens picker (universal rule). Power = coal/solar/wind, Roads = street/interstate, Water = water-treatment, Forests = forest. ContributorArchetypeRegistry seeded. Smoke validator walks toolbar prefab + asserts dispatch."
    red_stage_proof: |
      # Tracer: picker open + tile render + place per family + smoke validator on universal-rule.
      # Pre: 9.7 shipped. Now seed family subtypes.
      seed_prefab("PowerPlantCoal", pollution="high")
      seed_prefab("PowerPlantSolar", pollution="zero")
      seed_prefab("PowerPlantWind", pollution="zero")
      for slug in ["picker-power-coal-icon-72","picker-power-solar-icon-72","picker-power-wind-icon-72",
                   "picker-roads-street-icon-72","picker-roads-interstate-icon-72",
                   "picker-water-treatment-icon-72","picker-forest-forest-icon-72"]:
          seed_sprite(slug)
      seed_contributor_registry({
        "Power":   [{"subtype":"coal","prefab":"PowerPlantCoal","icon":"picker-power-coal-icon-72","baseCost":5000}],
        "Roads":   [{"subtype":"street","manager":"RoadManager","icon":"picker-roads-street-icon-72"},
                    {"subtype":"interstate","manager":"InterstateManager","icon":"picker-roads-interstate-icon-72"}],
        "Water":   [{"subtype":"water-treatment","prefab":"WaterPlant","icon":"picker-water-treatment-icon-72","baseCost":3000}],
        "Forests": [{"subtype":"forest","prefab":"Forest","icon":"picker-forest-forest-icon-72","baseCost":500}],
      })
      for family in ["Residential","Commercial","Industrial","StateService","Roads","Forests","Power","Water"]:
          click_toolbar_family(family)
          assert picker_visible() == True
          assert picker_tile_count() == expected_count[family]   # 3,3,3,7,2,1,3,1
          cancel_picker()
      click_toolbar_family("Bulldoze")
      assert picker_visible() == False
      for family, default_subtype in [("Power","coal"),("Roads","street"),("Water","water-treatment"),("Forests","forest")]:
          click_toolbar_family(family)
          click_picker_tile(default_subtype)
          place_at(grid_x=10, grid_y=10)
          assert cell_at(10,10).has_subtype(default_subtype)
    tasks:
      - id: "9.8.1"
        title: "Power expansion — 3 prefabs (coal/solar/wind) + sprite rows + ContributorArchetypeRegistry entries + OnPowerFamilyButtonClicked"
        prefix: TECH
        kind: code
        depends_on: ["TECH-9.7.4"]
        digest_outline: "Author `PowerPlantCoal/Solar/Wind` prefabs under `Assets/Resources/Buildings/`. Each implements `IBuilding` + carries pollution enum (`high`/`zero`) per utilities-exploration v1 line 129. Add 3 sprite-catalog rows. Seed 3 entries to `Resources/UI/picker-contributors.json`. Wire `OnPowerFamilyButtonClicked` → `ClearCurrentTool` + `ShowSubtypePicker(ToolFamily.Power)`. Route placement through `BuildingPlacementService` (rule 5)."
        touched_paths:
          - "Assets/Resources/Buildings/"
          - "Assets/Resources/UI/picker-contributors.json"
          - "Assets/Scripts/Managers/UnitManagers/"
          - "Assets/Scripts/Managers/GameManagers/UIManager.Toolbar.cs"
      - id: "9.8.2"
        title: "Roads expansion — 2 sprite rows (street + interstate) + dispatch wiring (no new prefabs)"
        prefix: TECH
        kind: code
        depends_on: ["TECH-9.7.4"]
        digest_outline: "Add 2 sprite-catalog rows. Picker tile dispatch: street → existing `RoadManager.OnTwoWayRoadButtonClicked` flow, interstate → existing `InterstateManager` flow. No new prefabs (both road tiers already shipped — verified `RoadManager.cs:39, 186`). Seed 2 entries to `picker-contributors.json` with `managerHook` field. Wire `OnRoadsFamilyButtonClicked` → `ShowSubtypePicker(ToolFamily.Roads)`."
        touched_paths:
          - "Assets/Resources/UI/picker-contributors.json"
          - "Assets/Scripts/Managers/GameManagers/UIManager.Toolbar.cs"
      - id: "9.8.3"
        title: "Single-subtype families — Water + Forests batched (1 sprite + 1 registry entry each)"
        prefix: TECH
        kind: code
        depends_on: ["TECH-9.7.4"]
        digest_outline: "Add 2 sprite-catalog rows (water-treatment + forest). Seed 1 entry each to `picker-contributors.json`: Water → existing `WaterPlant` prefab, Forests → existing `Forest` prefab. Wire `OnWaterFamilyButtonClicked` + `OnForestsFamilyButtonClicked` → `ShowSubtypePicker(ToolFamily.{Water,Forests})`. Single-tile picker each — forward-compat for future subtype additions."
        touched_paths:
          - "Assets/Resources/UI/picker-contributors.json"
          - "Assets/Scripts/Managers/GameManagers/UIManager.Toolbar.cs"
      - id: "9.8.4"
        title: "Dispatch wiring — ToolbarDataAdapter re-route + SubtypePickerController.BuildRows + ToolFamily enum extension"
        prefix: TECH
        kind: code
        depends_on: ["TECH-9.8.1", "TECH-9.8.2", "TECH-9.8.3"]
        digest_outline: "`ToolbarDataAdapter`: re-route `OnRoadClick`, `OnForestClick(0)` (sparse=primary), `OnBuildingClick(0)` (power), `OnBuildingClick(1)` (water) → family handlers. `SubtypePickerController.BuildRows` switch: 4 new cases (Roads, Forests, Power, Water) — each iterates `ContributorArchetypeRegistry.GetEntries(family)` + emits tile per entry. `ToolFamily` enum: 4 new values."
        touched_paths:
          - "Assets/Scripts/UI/Toolbar/ToolbarDataAdapter.cs"
          - "Assets/Scripts/UI/SubtypePickerController.cs"
      - id: "9.8.5"
        title: "Universal-rule smoke validator + tracer — walks toolbar prefab, asserts non-Bulldoze → picker_visible=true"
        prefix: TECH
        kind: code
        depends_on: ["TECH-9.8.4"]
        digest_outline: "PlayMode test walks every `IlluminatedButton` slug in baked toolbar prefab, clicks each via reflection on private dispatch (matches `MvpUiCloseoutSmokeTest.AssertToolbarSlotOpensPicker` shape). Asserts: family ≠ Bulldoze → `picker_visible == true`, family == Bulldoze → `picker_visible == false`. Catches Reg 1 orphaned-dispatch class from `phase-b-fixes-2026-05-05.md`. Validator runs on bake output (IR), not authoring scene."
        touched_paths:
          - "Assets/Tests/PlayMode/MvpUiCloseoutSmokeTest.cs"
notes:
  - "Plan extension: parent = game-ui-catalog-bake v1 (slug-keyed table — no integer id; field omitted). target_version=2."
  - "Stage order: 9.5 (in flight) → 9.7 → 9.8 → 7 (MVP closeout). 9.7 + 9.8 inserted between 9.6 (done) and 7 (pending)."
  - "Universal picker rule: every toolbar family except Bulldoze opens picker (forward-compat for single-subtype families)."
  - "Consumes DEC-A25 asset-pipeline-standard-v1 — no new arch decision."
  - "ContributorArchetypeRegistry is NEW (rule 6 — extract responsibility from ZoneSubTypeRegistry which is zoning-only)."
---

# Picker catalog conformance + family subtype expansion (Stages 9.7 + 9.8)

## Problem

Two coupled gaps surfaced during Phase B + post-Stage-9.6 review on `feature/asset-pipeline`:

### Gap 1 — `SubtypePickerController` bypasses asset-pipeline catalog

Despite DEC-A25 `asset-pipeline-standard-v1` (DB-wins 2-tier: `asset-registry` + `sprite-catalog`, both live post-9.6), the picker is built imperatively in C# at runtime:

- No `subtype-picker` row in `asset-registry` — panel shape (anchor, sizeDelta, layout group, content size fitter) hand-rolled in `EnsureRuntimePanelRootIfNeeded()`.
- No `picker-tile-72` button archetype — tile geometry (72×72, 6/18 px insets, 12 px caption strip) hard-coded in `AddIconTile()`.
- No sprite slugs for picker tile icons — R/C/I read `zoneManager.GetRandomZonePrefab(zoneType, 1).GetComponentInChildren<SpriteRenderer>().sprite` (yanked from gameplay prefab); StateService reads `ZoneSubTypeRegistry.Entries[i].icon`. Sprite-catalog is bypassed.
- No `motion.hover` enum on tile — hover tint (`LerpToward(baseColor, white, 0.18f)`) is magic-number C#, not schema-validated.
- No layout preset for picker per family — width/spacing/tile-count vary per family but no DB row carries the spec.

Consequence: when an agent rewrites the controller (as happened twice in Phase B + 2026-05-06 hotfix sweep), there's no diffable surface to compare against. The "what should the picker look like?" answer lives only in git history (stash 898045ab, before that nowhere). Each rewrite re-invents shape + sprite resolution + hover behavior from scratch → recurrence cost.

### Gap 2 — Power / Water / Roads / Forests have no picker / no subtypes

Phase B Issue 3 was deferred ("low value vs. cost") and the deferral persisted into the master plan (`Deferred / out of scope`). But Phase B postmortem §Problem bucket 2 also flagged: "Power plant + water plant family buttons don't open `SubtypePicker` (other family buttons do; pattern broken for these two)" — orphaned debt that no Stage 9.x carries.

Today: clicking Roads / Forests / Power / Water family buttons selects a single tool directly (no subtype choice). User has now reversed the deferral and asked for picker rows for all four families. This implies non-trivial gameplay-side authoring:

- **Subtypes per family** — undefined. Power = coal/gas/solar/nuclear/wind? Water = pump/tower/treatment/desalinator? Roads = dirt/asphalt/highway? Forests = tree-types? No spec, no glossary entries.
- **Prefab assets per subtype** — none exist. Each subtype needs a prefab with a `SpriteRenderer`, a placement variant, and (for Power/Water) economy params.
- **Sprite assets** — need to be authored or sprite-gen'd; sprite-catalog rows seeded via 9.6 AssetPostprocessor.
- **Picker dispatch wiring** — `ToolbarDataAdapter.OnZoningClick` only routes R/C/I/S today; needs new dispatch + `UIManager.OnXFamilyButtonClicked → ShowSubtypePicker(family)` for Roads/Forests/Power/Water.
- **Default subtype per family** — what does the family button select if user doesn't open the picker?

Hard constraint: both must ship before `Stage 7 — MVP closeout` (`pending` with 3 tasks). Plan extension order: 9.5 (in flight) → 9.7 (catalog conformance) → 9.8 (subtype expansion) → 7 (closeout).

## Approaches surveyed

### A — One combined Stage 9.7 (catalog conformance + subtype expansion together)

- **Pros:** single ship-cycle pass, single closeout, picker rewrite + subtype seed land together so subtype rows go straight into catalog (no controller-rewrite-then-row-rewrite churn).
- **Cons:** task count = 8+ (3 conformance + 4 family expansions + tracer); blows ship-cycle 80k token cap; subtype gameplay decisions bottleneck the catalog port; partial stage = `partial` row; mechanism + content split usually fails together.
- **Effort:** very high author + ship complexity.

### B — Split into two stages (recommended): 9.7 (conformance) → 9.8 (expansion)

- **Stage 9.7 — picker→catalog conformance.** Port `SubtypePickerController` to read panel shape + tile archetype + sprite slugs from catalog. ~3-4 tasks: (a) seed `subtype-picker` panel row + `picker-tile-72` archetype + tile sprite slugs in sprite-catalog (R/C/I/S only — known set); (b) refactor controller to read from catalog (no behavior change for R/C/I/S); (c) `motion.hover` enum on tile archetype + validator entry; (d) red-proof tracer (panel row → bake → IR → runtime tile renders with catalog sprite + theme tokens + hover motion).
- **Stage 9.8 — family subtype expansion (Roads / Forests / Power / Water).** Subtype list per family + new prefabs + new sprite-catalog rows + new ZoneSubTypeRegistry entries (or equivalent for non-zoning tools) + `OnXFamilyButtonClicked → ShowSubtypePicker` dispatch wiring. ~4-5 tasks (one per family + dispatch + tracer).
- **Pros:** 9.7 lands clean controller + catalog wiring → 9.8 subtype additions = pure DB row inserts (no controller edits); each stage ≤5 tasks → fits ship-cycle cap; clear closeout per stage; Gap 1 fix unblocks faster diff-against-design for any future picker rewrite.
- **Cons:** 9.8 subtype gameplay decisions need design lock before authoring (subtype lists + economy params). Mitigation: Phase 0.5 grill resolves before persist.
- **Effort:** medium-low author per stage, balanced ship.

### C — Catalog conformance only (defer subtype expansion to v2 plan)

- **Pros:** ships fastest user-visible truth-artifact fix; subtype expansion = post-MVP polish.
- **Cons:** user explicitly asked for subtype rows now ("This was asked several times before"); deferral re-creates same orphaned-debt pattern Phase B postmortem flagged.
- **Effort:** low author, low ship — but high deferral cost (recurrence of "asked several times").

### D — Subtype expansion first, conformance second

- **Pros:** ships visible gameplay surface fastest.
- **Cons:** subtype expansion done in current imperative C# controller = more hand-rolled tile additions × 4 families = more debt to rip out in 9.8b conformance pass; reverses the natural "fix the truth-artifact, then add content using it" order.
- **Effort:** medium author, medium ship — but doubles content-rewrite work.

## Recommendation

**Approach B** (two stages — 9.7 conformance → 9.8 expansion). Reasons: 9.7 unblocks 9.8 to land subtype additions as pure DB row inserts; each stage fits ship-cycle 80k cap; 9.7 closes the truth-artifact gap that caused the Phase B picker regression loop; 9.8 design-locks subtype lists before authoring.

Stage order: `9.5 (in flight) → 9.7 → 9.8 → 7 (MVP closeout)`.

## Open questions

1. **Picker panel anchor scope** — keep current center-bottom strip with horizontal layout for ALL families, or allow per-family layout (e.g. 2×2 grid for power plants if 4 subtypes)? Constrains panel row schema.
2. **Tile sprite resolution path** — once catalog-driven, R/C/I/S tile sprites read from sprite-catalog slug `picker-{family}-{tier}-icon-72`. Should we author dedicated picker icon sprites per tile (decoupled from gameplay prefab SpriteRenderer), or stash a slug pointer to gameplay prefab sprite? Decoupled = cleaner but doubles asset count.
3. **Power subtype list** — coal / gas / solar / nuclear / wind? Or smaller MVP set (coal + solar)? Each adds prefab + economy.
4. **Water subtype list** — pump / tower / treatment / desalinator? Or just two (well + treatment)? Each adds prefab + economy.
5. **Roads subtype list** — dirt / asphalt / highway? Or width-based (1-lane / 2-lane / 4-lane)? Affects pathfinding cost params.
6. **Forests subtype list** — tree-types (oak / pine / palm / mixed) cosmetic-only? Or do forest types differ in pollution/happiness contribution?
7. **Default subtype on family-button click** — picker auto-pre-selects first subtype (current Light tier behavior for R/C/I), OR family button stays tool-less until user picks a tile?
8. **Sprite asset pipeline for new subtype icons** — author by hand, sprite-gen via `tools/sprite-gen`, or placeholder dim-tint (current fallback for missing icons) until assets land?
9. **Hover motion enum values** — `tint` (current Phase B fix behavior), `glow` (outline accent), `scale` (RectTransform punch)? Pick one for MVP or allow per-archetype override?
10. **Per-family SFX confirm hook** — single `sfxPickerConfirm` clip for all families (current), or per-family clip (e.g. industrial = clank, residential = chime)? Couples to 9.5 audio-catalog stub.
11. **Economy / gameplay params for new subtypes** — out of scope for these stages (defer to economy v2 plan), or seed default params inline so subtypes are playable on land?
12. **9.8 task split granularity** — one task per family (4 tasks) + 1 dispatch + 1 tracer = 6 tasks (over cap), or pair families (Power+Water as utility pair, Roads+Forests as terrain pair) = 2 tasks + dispatch + tracer = 4 tasks?

## Scope locks (2026-05-06 — user-confirmed, honor arch)

Decision: **honor existing arch enumeration** (utilities-exploration + full-game-mvp-exploration). Subtype picker shown for **every family button except Bulldoze**, even when family has 1 subtype today (forward-compat for future expansion — picker is the canonical entry surface, never bypassed).

### Answers to open questions (1–12)

| # | Question | Lock |
|---|---|---|
| 1 | Panel anchor scope | Single shared shape — center-bottom strip, horizontal layout, fit-content width, for ALL families. No per-family grids in v1. |
| 2 | Tile sprite resolution path | **Decoupled** — sprite-catalog slug `picker-{family}-{subtype}-icon-72`, hand-authored 72×72 sprites per tile. Stops the "yank from gameplay prefab `SpriteRenderer`" anti-pattern. |
| 3 | Power subtype list | **coal / solar / wind** (3) — locks utilities-exploration v1 contributor archetypes. |
| 4 | Water subtype list | **water treatment** (1) — single subtype today; sewage treatment is its own family (not a Water plant subtype). Picker still shown (1 tile) for forward-compat. |
| 5 | Roads subtype list | **street / interstate** (2) — shipped tiers only. Avenue + arterial deferred (full-game-mvp-exploration line 106 — out of MVP). Picker shows 2 tiles. |
| 6 | Forests subtype list | **forest** (1) — single subtype, no tree-types in MVP (cosmetic-only variants deferred). Picker shown (1 tile) for forward-compat. |
| 7 | Default subtype on family-button click | Auto-pre-select first subtype (current Light tier behavior). Picker opens with default highlighted, user can re-pick or place immediately. |
| 8 | Sprite asset pipeline for new subtype icons | **Hand-authored** per subtype for MVP (3 power + 1 water + 1 forest + 2 road + existing R/C/I/S). Sprite-catalog rows seeded via 9.6 AssetPostprocessor on `.png` drop. Placeholder = dim-tint fallback (current behavior) until asset lands. |
| 9 | Hover motion enum values | `tint` for MVP (current Phase B fix — `LerpToward(baseColor, white, 0.18f)` via `Button.colors`). Enum schema admits `glow` / `scale` future, no per-archetype override v1. |
| 10 | Per-family SFX confirm hook | Single `sfxPickerConfirm` clip for all families (current). Couples to Stage 9.5 audio-catalog `audio-registry` stub — slug `sfx-picker-confirm-default`. Per-family clips deferred. |
| 11 | Economy / gameplay params for new subtypes | **Deferred to utilities-master-plan / economy v2.** Seed default params inline so subtypes are placeable (cost, footprint, sprite) but gameplay distinction (pollution per type, output curve) lands later. Power coal/solar/wind already carry pollution differentials per utilities-exploration line 129 — that minimum holds. |
| 12 | 9.8 task split granularity | 5 tasks ≤ ship-cycle cap: **(a) Power expansion** (3 subtypes + prefabs + sprite-catalog rows + ZoneSubTypeRegistry-equiv entries), **(b) Roads expansion** (2 subtypes — street + interstate as picker rows, dispatch into existing manager), **(c) Single-subtype families** (Water + Forest — 1 tile picker rows each, batched), **(d) Dispatch wiring** (`ToolbarDataAdapter.OnZoningClick` slot routing for Roads / Forests / Power / Water), **(e) Tracer** (red-proof picker open + tile render + place per family). |

### Universal picker rule (locks for 9.7 + 9.8)

> Every toolbar family button opens `SubtypePicker`. Bulldoze is the only family that bypasses (direct-tool select). This rule survives subtype-count changes — adding/removing subtypes never re-introduces direct-tool dispatch on a non-Bulldoze family.

Validator follow-up (Stage 9.8 task or its tracer): a smoke assertion that walks every toolbar family slot (excluding Bulldoze) and confirms `ShowSubtypePicker` fires on click. Catches the same dispatch-routing regression class flagged in `phase-b-fixes-2026-05-05.md` Reg 1.

## Notes / context

- DEC-A25 `asset-pipeline-standard-v1` already locks 2-tier DB-wins authority (asset-registry + sprite-catalog).
- Stage 9.6 sprite-catalog AssetPostprocessor + backfill landed → `.png` drops auto-register sprite-catalog rows.
- Stage 9.5 (in flight) introduces `motion: {enter, exit, hover}` enum column on asset-registry → 9.7 tile archetype consumes this column directly.
- Phase B postmortem `docs/phase-b-fixes-2026-05-05.md` documents the recurrence cost of the Gap 1 anti-pattern; Stage 9.7 closes the loop.
- Current `SubtypePickerController.cs` is the post-2026-05-06 hotfix version (fit-content + sprite tiles + hover via `Button.colors`) — Stage 9.7 ports this behavior into catalog-driven shape, no behavior regression for R/C/I/S.
- `ZoneSubTypeRegistry` carries StateService entries today; Roads/Forests/Power/Water are NOT zoning. Stage 9.8 needs a parallel registry or extension to ZoneSubTypeRegistry to carry non-zoning subtype rows.
- `ToolbarDataAdapter.OnZoningClick` switch dispatch needs new cases for non-zoning family slots — current switch handles slots 0-9 (R/C/I/S only).

---

## Design Expansion

### Chosen Approach

**Approach B — split into Stage 9.7 (picker→catalog conformance) → Stage 9.8 (family subtype expansion).** Confirmed by user via `APPROACH_HINT=B` + locked answers in §Scope locks (2026-05-06).

Stage order: `9.5 (in flight) → 9.7 → 9.8 → 7 (MVP closeout)`. 9.7 closes the truth-artifact gap so 9.8 ships subtype rows as pure DB inserts.

### Architecture Decision

Consumes **DEC-A25 `asset-pipeline-standard-v1`** (existing — covers `asset-registry`, `sprite-catalog`, contributor archetype). Stage 9.7 = consumer of existing decision, NOT a new arch decision. No `arch_decision_write` / `arch_changelog_append` needed. Stage 9.8 inherits same surface.

### Architecture

#### Stage 9.7 — picker→catalog conformance

| Component | Layer | Role |
|---|---|---|
| `subtype-picker` panel row | asset-registry | Single shared panel shape (anchor center-bottom, sizeDelta 0×88, HorizontalLayoutGroup spacing=8 padding=10/10/8/8, ContentSizeFitter horizontal=PreferredSize). Replaces `EnsureRuntimePanelRootIfNeeded()` magic numbers. |
| `picker-tile-72` archetype row | asset-registry (kind=archetype) | Tile geometry (72×72 LayoutElement, icon offsetMin=(6,18) offsetMax=(-6,-6), caption strip 12 px). Carries `motion: { hover: "tint" }`. Replaces `AddIconTile()` literals. |
| `picker-{family}-{subtype}-icon-72` sprite-catalog rows | sprite-catalog | Per-tile icon sprite, 72×72 PNG. Decoupled from gameplay prefab `SpriteRenderer` (lock #2). Initial seed: R/C/I light/medium/heavy + S 7 entries = 16 rows. |
| `motion.hover = "tint"` enum value | asset-registry tile archetype `motion` jsonb | Enum schema admits `tint | glow | scale`. v1 = `tint` only — `LerpToward(baseColor, white, 0.18f)` via `Button.colors.highlightedColor`. |
| `SubtypePickerController` (refactored) | runtime C# | Reads panel row + tile archetype + sprite slugs from catalog (`GridAssetCatalog` accessor or new `UiAssetCatalog` lookup). Behavior identical for R/C/I/S — no regression. Drops `ResolveZoningSprite()` (zone-prefab `SpriteRenderer` yank). |
| Validator entry | `validate:asset-registry` (Stage 9.5 motion column consumer) | Asserts every `kind=archetype` row carrying tile role has `motion.hover` ∈ enum set. |

#### Stage 9.8 — family subtype expansion

| Component | Layer | Role |
|---|---|---|
| Power subtype prefabs | runtime C# / Resources | 3 prefabs: `PowerPlantCoal`, `PowerPlantSolar`, `PowerPlantWind`. Each implements `IBuilding` + carries pollution differential per utilities-exploration line 129. |
| `picker-power-{coal,solar,wind}-icon-72` sprite-catalog rows | sprite-catalog | Hand-authored 72×72 PNG per subtype (lock #8). |
| `picker-roads-{street,interstate}-icon-72` sprite-catalog rows | sprite-catalog | 2 hand-authored icons. Dispatch routes street → existing `RoadManager`, interstate → existing `InterstateManager` (already shipped — `InterstateManager.CanPlaceStreetFrom` confirmed in `RoadManager.cs:186`). No new prefabs. |
| `picker-water-treatment-icon-72` + `picker-forest-forest-icon-72` sprite-catalog rows | sprite-catalog | 1 each (single-subtype families). |
| Subtype registry extension | runtime C# | Either (a) extend `ZoneSubTypeRegistry` to carry non-zoning rows OR (b) parallel `ContributorArchetypeRegistry` ScriptableObject. **Pick (b)** — Roads/Power/Water/Forest are NOT zoning; mixing them into `ZoneSubTypeRegistry` violates single-responsibility (rule 6 — extract responsibilities). New registry: `Resources/UI/picker-contributors.json` with `{ family, subtype, prefabPath, iconSlug, baseCost }` rows. |
| `OnXFamilyButtonClicked` UIManager methods | runtime C# (`UIManager.Toolbar.cs`) | New: `OnRoadsFamilyButtonClicked`, `OnForestsFamilyButtonClicked`, `OnPowerFamilyButtonClicked`, `OnWaterFamilyButtonClicked` — each calls `ClearCurrentTool()` + `ShowSubtypePicker(ToolFamily.{Roads,Forests,Power,Water})`. Mirrors existing `OnResidentialFamilyButtonClicked`. |
| `ToolFamily` enum extension | runtime C# (`SubtypePickerController.cs`) | Add `Roads, Forests, Power, Water` values. |
| `SubtypePickerController.BuildRows` switch | runtime C# | New cases: `Roads` (street+interstate), `Forests` (forest×1), `Power` (coal/solar/wind), `Water` (water-treatment×1). Each tile dispatch → existing or new placement handler. |
| `ToolbarDataAdapter.OnBuildingClick` / `OnRoadClick` / `OnForestClick` | runtime C# | Re-route family button clicks (slug → family-button) to `OnXFamilyButtonClicked` instead of direct tool select. Existing single-tool dispatch remains as picker-tile commit handlers (called from `BuildRows`). |
| Picker dispatch validator | smoke test (Stage 9.8 tracer) | Walks every `IlluminatedButton` slug in toolbar prefab, asserts non-Bulldoze families fire `ShowSubtypePicker` on click. Catches Reg 1 regression class from `phase-b-fixes-2026-05-05.md`. |

### Red-Stage Proof — Stage 9.7

```python
# Tracer: panel row → bake → IR → runtime tile renders with catalog sprite + theme tokens + hover motion
# Pre: subtype-picker panel row + picker-tile-72 archetype + 16 sprite-catalog rows seeded.
seed_panel_row("subtype-picker", anchor=(0.5,0,0.5,0), sizeDelta=(0,88), layout="horizontal", padding=(10,10,8,8))
seed_archetype("picker-tile-72", geom=(72,72), icon_inset=(6,18,-6,-6), motion={"hover":"tint"})
for fam in ["residential","commercial","industrial"]:
    for tier in ["light","medium","heavy"]:
        seed_sprite(f"picker-{fam}-{tier}-icon-72", png_72x72)
for s in range(7):
    seed_sprite(f"picker-state-{s}-icon-72", png_72x72)

# Bake → IR
ir = bake_asset_registry()
assert ir["panels"]["subtype-picker"]["sizeDelta"] == [0, 88]
assert ir["archetypes"]["picker-tile-72"]["motion"]["hover"] == "tint"
assert "picker-residential-light-icon-72" in ir["sprites"]

# Runtime: open picker for Residential, render
controller.Show(uiManager, ToolFamily.Residential, defaultKey=0)
assert panel_root.sizeDelta == (0, 88)  # from catalog, NOT hardcoded
assert tile_count() == 3
tile = first_tile()
assert tile.layoutElement.preferredWidth == 72  # from archetype
assert tile.icon.sprite.name == "picker-residential-light-icon-72"  # decoupled — NOT from gameplay prefab SpriteRenderer
assert tile.button.colors.highlightedColor == lerp(baseColor, white, 0.18)  # motion.hover=tint
# Validator
assert run_validator("asset-registry") == 0  # all archetype rows carry motion.hover ∈ {tint,glow,scale}
```

### Red-Stage Proof — Stage 9.8

```python
# Tracer: picker open + tile render + place per family + smoke validator on universal-rule
# Pre: 9.7 shipped. Now seed family subtypes.
seed_prefab("PowerPlantCoal", pollution=high); seed_prefab("PowerPlantSolar", pollution=zero); seed_prefab("PowerPlantWind", pollution=zero)
seed_sprite("picker-power-coal-icon-72"); seed_sprite("picker-power-solar-icon-72"); seed_sprite("picker-power-wind-icon-72")
seed_sprite("picker-roads-street-icon-72"); seed_sprite("picker-roads-interstate-icon-72")
seed_sprite("picker-water-treatment-icon-72"); seed_sprite("picker-forest-forest-icon-72")
seed_contributor_registry({
  "Power":   [{"subtype":"coal","prefab":"PowerPlantCoal","icon":"picker-power-coal-icon-72","baseCost":5000}, ...],
  "Roads":   [{"subtype":"street","manager":"RoadManager","icon":"picker-roads-street-icon-72"}, {"subtype":"interstate","manager":"InterstateManager","icon":"picker-roads-interstate-icon-72"}],
  "Water":   [{"subtype":"water-treatment","prefab":"WaterPlant","icon":"picker-water-treatment-icon-72","baseCost":3000}],
  "Forests": [{"subtype":"forest","prefab":"Forest","icon":"picker-forest-forest-icon-72","baseCost":500}],
})

# Click each family button (except Bulldoze) — assert picker opens
for family in ["Residential","Commercial","Industrial","StateService","Roads","Forests","Power","Water"]:
    click_toolbar_family(family)
    assert picker_visible() == True
    assert picker_tile_count() == expected_count[family]   # 3,3,3,7,2,1,3,1
    cancel_picker()

# Click Bulldoze — assert picker NOT shown (universal-rule exception)
click_toolbar_family("Bulldoze")
assert picker_visible() == False

# Place per family (placement smoke)
for family, default_subtype in [("Power","coal"), ("Roads","street"), ("Water","water-treatment"), ("Forests","forest")]:
    click_toolbar_family(family)
    click_picker_tile(default_subtype)
    place_at(grid_x=10, grid_y=10)
    assert cell_at(10,10).has_subtype(default_subtype)
```

### Subsystem Impact

Touched (count = 9):

1. **`Assets/Scripts/UI/SubtypePickerController.cs`** — refactor (9.7) reads from catalog; extend (9.8) with new `ToolFamily` cases. Invariants: rule 3 (cache catalog accessor in `Awake`, not `BuildRows`), rule 4 (Inspector + `FindObjectOfType` fallback for `UiAssetCatalog`).
2. **`Assets/Scripts/Managers/GameManagers/UIManager.Toolbar.cs`** — add `OnRoadsFamilyButtonClicked`, `OnForestsFamilyButtonClicked`, `OnPowerFamilyButtonClicked`, `OnWaterFamilyButtonClicked` (Stage 9.8). Rule 6 — no new responsibilities to `GridManager`; UIManager partial OK.
3. **`Assets/Scripts/UI/Toolbar/ToolbarDataAdapter.cs`** — re-route `OnRoadClick`, `OnBuildingClick`, `OnForestClick` to family handlers; add new family routing case for Water (currently missing — power+water collapsed into `OnBuildingClick` slot index). Rule 4 — Inspector slot wiring stays.
4. **`Assets/Scripts/UI/TokenCatalog.cs`** — add lookup helper for picker panel/archetype tokens (motion.hover enum).
5. **`asset-registry`** — 1 panel row (`subtype-picker`) + 1 archetype row (`picker-tile-72`).
6. **`sprite-catalog`** — 16 rows Stage 9.7 (R/C/I/S) + 7 rows Stage 9.8 (3 power + 1 water + 1 forest + 2 roads).
7. **`ZoneSubTypeRegistry` adjacent** — new `ContributorArchetypeRegistry` (`Resources/UI/picker-contributors.json`). Rule 6 — extract responsibility, do NOT bloat `ZoneSubTypeRegistry`.
8. **`ia/specs/ui-design-system.md` §3.7** — update RCIS subtype picker pattern to "RCIS+Roads+Forests+Power+Water subtype picker"; document catalog-driven shape + `motion.hover` enum + universal-rule (Bulldoze-only bypass).
9. **Power subtype prefabs (`PowerPlantCoal/Solar/Wind`)** — new `IBuilding` implementations under `Assets/Scripts/Managers/UnitManagers/`. Carry pollution differential. Rule 5 — no direct `cellArray` access; route through `BuildingPlacementService`.

Invariant flags: rule 3 (no `FindObjectOfType` per-frame), rule 4 (Inspector + Awake fallback), rule 6 (extract responsibility — new registry, not `ZoneSubTypeRegistry` bloat). No HeightMap / roads-modify / water-place touch points → invariants 1, 2, 7, 8, 10 not triggered.

### Implementation Points

**Stage 9.7 tasks (4):**

1. **9.7.1 — Seed asset-registry rows.** Add `subtype-picker` panel row + `picker-tile-72` archetype row to DB seed migration. Add 16 `sprite-catalog` rows (R/C/I light/medium/heavy + S 7). Sprites: hand-authored or sprite-gen'd 72×72 PNG. Drop into `Assets/UI/Sprites/Picker/` → AssetPostprocessor (Stage 9.6) auto-registers.
2. **9.7.2 — Refactor `SubtypePickerController`.** Replace `EnsureRuntimePanelRootIfNeeded()` with catalog-driven build. Replace `ResolveZoningSprite()` with sprite-catalog lookup by slug `picker-{family}-{tier}-icon-72`. Pull tile geometry + motion.hover from `picker-tile-72` archetype. Behavior parity for R/C/I/S asserted by existing tests.
3. **9.7.3 — `motion.hover` enum + validator.** Add `motion: { hover: "tint" }` jsonb column to `picker-tile-72` archetype. Extend `validate:asset-registry` to assert every tile-role archetype carries `motion.hover` ∈ `{tint, glow, scale}`.
4. **9.7.4 — Tracer (red-stage proof).** PlayMode test: seed → bake → render → assert tile geom + sprite slug + hover color match catalog. Hard-fails if controller falls back to legacy literals.

**Stage 9.8 tasks (5):**

1. **9.8.1 — Power expansion.** Author `PowerPlantCoal/Solar/Wind` prefabs + 3 sprite-catalog rows + 3 `ContributorArchetypeRegistry` entries. Each prefab carries pollution differential per utilities-exploration v1. Wire `OnPowerFamilyButtonClicked` → `ShowSubtypePicker(ToolFamily.Power)`.
2. **9.8.2 — Roads expansion.** 2 sprite-catalog rows (street + interstate). Picker tile dispatch routes street → existing `RoadManager.OnTwoWayRoadButtonClicked` flow, interstate → existing `InterstateManager` flow. No new prefabs (both road tiers shipped). `OnRoadsFamilyButtonClicked` → `ShowSubtypePicker(ToolFamily.Roads)`.
3. **9.8.3 — Single-subtype families (Water + Forest, batched).** 1 sprite-catalog row each (water-treatment + forest). 1 `ContributorArchetypeRegistry` entry each. `OnWaterFamilyButtonClicked` + `OnForestsFamilyButtonClicked` → `ShowSubtypePicker(ToolFamily.{Water,Forests})` (1-tile picker each, forward-compat).
4. **9.8.4 — Dispatch wiring.** `ToolbarDataAdapter`: re-route `OnRoadClick`, `OnForestClick(0)` (sparse=primary), `OnBuildingClick(0)` (power), `OnBuildingClick(1)` (water) → family handlers. `SubtypePickerController.BuildRows` switch: 4 new cases. `ToolFamily` enum: 4 new values.
5. **9.8.5 — Universal-rule smoke validator + tracer.** PlayMode test walks every `IlluminatedButton` in baked toolbar prefab, clicks each, asserts: non-Bulldoze → `picker_visible == true`, Bulldoze → `picker_visible == false`. Catches Reg 1 (orphaned dispatch) class.

### Examples

**Catalog-driven panel build (replaces `EnsureRuntimePanelRootIfNeeded`):**

```csharp
private void EnsureRuntimePanelRootFromCatalog()
{
    if (panelRoot != null) return;
    var canvas = FindObjectOfType<Canvas>();
    if (canvas == null) return;

    // Read panel row from asset-registry via UiAssetCatalog (Stage 9.7).
    if (!uiAssetCatalog.TryGetPanel("subtype-picker", out var panelDef)) return;

    var root = new GameObject("SubtypePickerRoot");
    root.transform.SetParent(canvas.transform, false);
    var rt = root.AddComponent<RectTransform>();
    rt.anchorMin = panelDef.anchorMin;       // catalog-driven
    rt.anchorMax = panelDef.anchorMax;
    rt.pivot     = panelDef.pivot;
    rt.sizeDelta = panelDef.sizeDelta;

    var hlg = root.AddComponent<HorizontalLayoutGroup>();
    hlg.spacing = panelDef.layoutSpacing;    // 8 from catalog
    hlg.padding = panelDef.layoutPadding;    // (10,10,8,8) from catalog
    // …rest copy-flat from panelDef…

    panelRoot = root;
    rowContainer = root.transform;
}
```

**Catalog-driven tile build (replaces `AddIconTile` literals):**

```csharp
private void AddIconTileFromCatalog(int key, string label, string spriteSlug, System.Action onClick)
{
    if (!uiAssetCatalog.TryGetArchetype("picker-tile-72", out var arch)) return;
    if (!uiAssetCatalog.TryGetSprite(spriteSlug, out var sprite)) sprite = null;

    var tile = new GameObject($"PickerTile_{spawnedRows.Count}", typeof(RectTransform), typeof(Button), typeof(Image));
    var le = tile.AddComponent<LayoutElement>();
    le.preferredWidth  = arch.tileWidth;     // 72 from archetype
    le.preferredHeight = arch.tileHeight;    // 72 from archetype

    var btn = tile.GetComponent<Button>();
    var cb  = btn.colors;
    cb.highlightedColor = arch.motion.hover == "tint"
        ? LerpToward(baseColor, Color.white, 0.18f)   // tint enum value
        : baseColor;                                   // future: glow/scale branches
    btn.colors = cb;
    // …icon offsets from arch.iconOffsetMin/Max, caption strip from arch.captionHeight…
}
```

**`ContributorArchetypeRegistry` JSON (Stage 9.8):**

```json
{
  "entries": [
    { "family": "Power",   "subtype": "coal",            "prefabPath": "Buildings/PowerPlantCoal",   "iconSlug": "picker-power-coal-icon-72",      "baseCost": 5000, "pollution": "high" },
    { "family": "Power",   "subtype": "solar",           "prefabPath": "Buildings/PowerPlantSolar",  "iconSlug": "picker-power-solar-icon-72",     "baseCost": 8000, "pollution": "zero" },
    { "family": "Power",   "subtype": "wind",            "prefabPath": "Buildings/PowerPlantWind",   "iconSlug": "picker-power-wind-icon-72",      "baseCost": 6000, "pollution": "zero" },
    { "family": "Roads",   "subtype": "street",          "managerHook": "RoadManager.TwoWay",        "iconSlug": "picker-roads-street-icon-72" },
    { "family": "Roads",   "subtype": "interstate",      "managerHook": "InterstateManager",         "iconSlug": "picker-roads-interstate-icon-72" },
    { "family": "Water",   "subtype": "water-treatment", "prefabPath": "Buildings/WaterPlant",       "iconSlug": "picker-water-treatment-icon-72", "baseCost": 3000 },
    { "family": "Forests", "subtype": "forest",          "prefabPath": "Forests/Forest",             "iconSlug": "picker-forest-forest-icon-72",   "baseCost": 500 }
  ]
}
```

### Review Notes

Reviewer pass identified 3 NON-BLOCKING follow-ups + 0 BLOCKING. Blocking risks resolved inline before persist:

**Resolved during synthesis (would have been BLOCKING):**

- **R1 — Power coal/gas/solar/nuclear/wind list mismatch.** Lock #3 says "coal/solar/wind (3)"; original Approach B prose said "coal/gas/solar/nuclear/wind". Resolution: lock #3 wins per locks-precedence. Synthesis uses 3 subtypes only.
- **R2 — Roads "both already shipped" claim.** Verified `InterstateManager` exists (`RoadManager.cs:39, 186`); street prefabs (`roadTilePrefab1/2/elbow/slope`) ship in `RoadPrefabResolver.cs`. Lock claim valid — picker dispatch only, no new prefabs.
- **R3 — `ZoneSubTypeRegistry` bloat vs. new registry.** Architecture chose new `ContributorArchetypeRegistry` per rule 6 (extract responsibilities). Avoids cross-domain registry mixing zoning + non-zoning rows.

**NON-BLOCKING (carry into Stage 9.8 author phase):**

- **N1 — Power pollution params.** Lock #11 defers economy params to economy v2; pollution differential per utilities-exploration v1 line 129 is the minimum that holds. Stage 9.8.1 must seed pollution as enum (`high|low|zero`) NOT numeric — numeric tuning lands in economy v2.
- **N2 — Sprite-gen vs. hand-author for picker icons.** Lock #8 says hand-authored for MVP; if sprite-gen archetype `picker-icon-72` exists or can be authored cheaply, prefer sprite-gen for consistency. Decision: defer to Stage 9.8 task author — fallback dim-tint covers missing icons.
- **N3 — Universal-rule smoke validator scope.** Validator walks toolbar prefab; if a non-Bulldoze family ships without picker dispatch wiring, validator fails. Confirm post-Stage-9.8: validator must run on bake output (IR), not authoring scene — bake output is source of truth per unity-invariants guardrail "toolbar/prefab slot shows wrong icon after bake → inspect bake output".

**SUGGESTIONS (not gating):**

- **S1 — Single-tile pickers (Water + Forest).** Lock #6 + #4 ship 1-tile pickers for forward-compat. Consider adding visible "+ subtype" disabled affordance in tile strip → signals expansion intent to player. Defer to post-MVP UX polish.
- **S2 — `motion.hover` extension order.** Enum admits `tint | glow | scale`; v1 = `tint`. Add `glow` next (outline accent) — cheaper than `scale` (RectTransform punch needs animator).
- **S3 — Per-family SFX.** Lock #10 keeps single `sfxPickerConfirm`. Once Stage 9.5 audio-catalog stabilizes, per-family clips = single sprite-catalog-style row insert per family. Defer to audio polish stage.

### Expansion metadata

- **Date:** 2026-05-06
- **Model:** claude-opus-4-7
- **Approach selected:** B (Stage 9.7 conformance → 9.8 expansion)
- **Mode:** standard (fast resume — locks pre-resolved)
- **Phases run:** 0, 1, 3, 4, 5, 6, 7, 8, 9
- **Phases skipped:** 0.5 (interview pre-resolved via §Scope locks), 2 (approach pre-confirmed), 2.5 (consumes existing DEC-A25, no new arch decision)
- **BLOCKING items resolved:** 3 (R1, R2, R3)
- **NON-BLOCKING carried:** 3 (N1, N2, N3)
- **Suggestions:** 3 (S1, S2, S3)

