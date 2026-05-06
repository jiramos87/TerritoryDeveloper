# Asset Pipeline Standard v1

**Status:** active — DEC-A25 `asset-pipeline-standard-v1` (2026-05-05).
**Tier authority:** DB-wins. DB row = source of truth for every UI asset; bake output = derived artifact.

---

## §Mission

Canonical mechanism for authoring, baking, validating, and rendering game UI assets end-to-end. Governs every panel, button, and interactive widget from DB row to in-game render.

---

## §Roles

| Role | Owner |
|---|---|
| Schema authority (asset-registry rows) | DB (Postgres `catalog_entity` table; 2-tier via DEC-A25) |
| Bake executor | `AssetRegistryBake.cs` (Editor-only) invoked by bridge kind `bake_asset_registry` |
| IR consumer | `UiBakeHandler.cs` — reads JSON IR → writes prefabs + `UiTheme.asset` |
| CI gate | `validate:asset-pipeline` npm script (schema-only; hard gate in `validate:all`) |
| Design token source | `ds-*` columns on asset-registry row; resolved at bake → IR |
| Motion enum source | `motion` jsonb column on asset-registry row (`{enter, exit, hover}` → enum values `fade`|`slide`|`none`) |

---

## §Canonical asset paths

Canonical location for every new sprite, prefab, or audio asset in the Unity project. Onboarding agent reads this section to resolve "where does X go" without reading code.

### Family taxonomy

| # | Family | Unity path prefix | Example |
|---|--------|-------------------|---------|
| 1 | ui-panel | `Assets/UI/Prefabs/Generated/` | `demo-panel.prefab` |
| 2 | ui-button | `Assets/UI/Prefabs/Generated/Buttons/` | `confirm-button.prefab` |
| 3 | ui-icon | `Assets/Art/Sprites/UI/Icons/` | `bulldoze-icon.png` |
| 4 | terrain-sprite | `Assets/Art/Sprites/Terrain/` | `grass-flat.png` |
| 5 | road-sprite | `Assets/Art/Sprites/Roads/` | `road-h.png` |
| 6 | building-sprite | `Assets/Art/Sprites/Buildings/` | `residential-sm.png` |
| 7 | water-sprite | `Assets/Art/Sprites/Water/` | `lake-shore-n.png` |
| 8 | audio-sfx | `Assets/Audio/SFX/` | `bulldoze-hit.wav` |
| 9 | audio-music | `Assets/Audio/Music/` | `city-ambient.ogg` |
| 10 | vfx-particle | `Assets/Art/VFX/` | `dust-puff.prefab` |

### Naming convention

All file stems MUST match:

```
^[a-z0-9]+(-[a-z0-9]+)*$
```

Kebab-case; lowercase alphanumeric segments only; no underscores, spaces, or uppercase. Suffix = Unity-native extension (`.prefab`, `.png`, `.wav`, `.ogg`).

### Validator

`validate:asset-tree-canonical` — checks every file under `Assets/Art/`, `Assets/Audio/`, `Assets/UI/Prefabs/Generated/` against the family path prefixes + naming regex above. Exits non-zero on first violation. Integrated into `validate:all`.

---

## §Authority

**DB-wins contract (non-negotiable):**

1. Asset-registry row is the single source of truth. Bake outputs (prefabs, IR JSON) are derived — never edit them directly.
2. Bake MUST be deterministic: same DB state → identical output on every run.
3. Non-conformance blocks landing (CI `validate:asset-pipeline` exits non-zero = red build).
4. `AssetPostprocessor` type-routes: Unity asset imports route by kind — `kind=ui` → asset-registry DB tier; `kind=sprite|prefab` → sprite-catalog DB tier (Stage 9.6).

**2-Tier model:**

| Tier | DB surface | Kind gate |
|---|---|---|
| asset-registry | `catalog_entity` (UI panels, buttons, interactives) | `kind=ui` |
| sprite-catalog | `catalog_sprite` (raw sprites, atlases) | `kind=sprite\|prefab` |

---

## §Validator

`validate:asset-pipeline` is a schema-only validator. It does NOT require Unity Editor or DB connection.

**What it checks:**

1. Every asset-registry row in `catalog_entity` has required fields: `slug`, `kind`, `ds_tokens` (non-null jsonb), `motion` (non-null jsonb with `enter`/`exit`/`hover` keys).
2. `motion` enum values are within allowed set: `fade`, `slide`, `none`.
3. No orphaned `asset_detail` rows (FK integrity).
4. Bake output prefabs under `Assets/UI/Prefabs/Generated/` exist for every published asset-registry row.

**Exit codes:** 0 = green; non-zero = schema fault (blocks `validate:all`).

**Run:** `npm run validate:asset-pipeline`

---

## §Checklist (per asset authored)

Before marking an asset-registry row `published`:

- [ ] DB row present: `slug`, `kind=ui`, `ds_tokens` set, `motion` defaults set.
- [ ] `npm run validate:asset-pipeline` → 0 (green).
- [ ] Bake run: `bake_asset_registry` bridge kind → no errors in response.
- [ ] Prefab exists at `Assets/UI/Prefabs/Generated/{slug}.prefab`.
- [ ] Runtime spot-check: panel visible in Play Mode with `ds-*` tokens applied.

---

## §Playbook

Step-by-step recipe to author one UI asset end-to-end.

### Step 1 — Seed DB row

```sql
-- Insert into catalog_entity (asset-registry tier)
INSERT INTO catalog_entity (slug, kind, display_name, ds_tokens, motion, status)
VALUES (
  'demo-panel',
  'ui',
  'Demo Panel',
  '{"bg": "ds-surface-1", "border": "ds-border-subtle", "text": "ds-text-primary"}'::jsonb,
  '{"enter": "fade", "exit": "fade", "hover": "none"}'::jsonb,
  'draft'
);
```

### Step 2 — Validate schema

```bash
npm run validate:asset-pipeline
# Expected: exit 0, output: "asset-pipeline: green (N rows validated)"
```

### Step 3 — Run bake

Via MCP bridge (Editor must be open):
```
bridge kind: bake_asset_registry
args: { slug: "demo-panel", out_dir: "Assets/UI/Prefabs/Generated" }
```

Expected response: `{ ok: true, prefab_path: "Assets/UI/Prefabs/Generated/demo-panel.prefab" }`

### Step 4 — Verify runtime

Open Play Mode. `demo-panel` prefab must:
- Be visible (not empty RectTransform).
- Render with `ds-surface-1` background color token applied.
- Have `motion.enter=fade` component wired.

### Step 5 — Publish

```sql
UPDATE catalog_entity SET status='published' WHERE slug='demo-panel';
```

Re-run `npm run validate:asset-pipeline` → still green.

### Drift cases (Phase B lessons)

Each entry: **symptom** → **root cause** → **fix recipe**.

---

#### DC-1 — Partial-class MonoBehaviour binding

**Symptom.** Component shows "missing script" at scene load (Inspector → yellow warning). `m_Script` GUID may already point to the correct `.meta` file yet bind fails. `[ZoneSubTypeRegistry] GridAssetCatalog not found in scene` in console.

**Root cause.** Unity resolves `: MonoBehaviour` by matching the declaration file stem to the class name. If the base class declaration lives in a secondary partial file (`GridAssetCatalog.Dto.cs` instead of `GridAssetCatalog.cs`), Unity cannot bind the script reference at scene load even when both files compile cleanly.

**Fix recipe.**
1. Move `: MonoBehaviour` (and any other base specs) to the file whose stem matches the class name (canonical file).
2. Secondary partial files: `partial class X` only — no base spec.
3. Update scene `m_Script` GUID to the `.meta` of the canonical file if it drifted.
4. Verify: `prefab_manifest` or scene reopen → `is_missing_script: false`.

---

#### DC-2 — Bake output is the truth for slot ordering

**Symptom.** Toolbar slot renders wrong icon (e.g., S-zoning slot shows Bulldoze icon). Bake source YAML looks correct.

**Root cause.** Bake output (`_detail.iconSpriteSlug` in generated prefab YAML) diverged from the intended slot mapping. The bake source and output are separate artifacts — bake output wins at runtime.

**Fix recipe.**
1. Inspect the bake output prefab YAML (e.g., `toolbar.prefab` lines near the suspect slot) — read `iconSpriteSlug` + `m_Sprite GUID`.
2. For a one-off fix: edit `iconSpriteSlug` and `m_Sprite` in-place in the generated prefab YAML (valid; bake output is a derived file).
3. Touch bake source only if the wrong mapping recurs after a re-bake (indicates upstream config fault).
4. Verify: Play Mode → slot renders correct icon.

---

#### DC-3 — Empty `_streamingRelativePath` is benign

**Symptom.** `Debug.LogError` fires at runtime: `"_streamingRelativePath is empty"` from `GridAssetCatalog` or `TokenCatalog`. No functional breakage observed.

**Root cause.** v1 catalog snapshot path (`_streamingRelativePath`) is superseded by per-kind exports + `CatalogLoader` (Stage 13.1, TECH-2675). The field is populated only in legacy flows. Empty = expected production state.

**Fix recipe.**
1. Replace `Debug.LogError` with a silent early return.
2. Add inline comment: `// _streamingRelativePath empty = expected; superseded by CatalogLoader (TECH-2675).`
3. Do NOT add runtime fallback logic — the field is intentionally unused.
4. Verify: Play Mode console → LogError gone; no functional regression.

---

#### DC-4 — Lazy-init notification panel (SerializeField unwired)

**Symptom.** `LogError` on Awake: `"notificationPanel is null"` (or similar). `GameNotificationManager` present in scene but panel reference unset. Notification overlay non-functional.

**Root cause.** `SerializeField` `notificationPanel` was never wired in the Inspector (or was discarded during a domain reload without `save_scene`). Bridge scene mutations are ephemeral without an explicit save (see DC-Bridge below).

**Fix recipe.**
1. Implement code-side lazy-init in the manager's Awake / first-use path: `LazyCreateNotificationUi()` — create a hidden Canvas child + `TextMeshProUGUI` at runtime if the field is null.
2. Tag the created object so domain reloads can find + reuse it (`FindObjectsByType` scan before create).
3. Do NOT rely on `[ExecuteAlways]` `OnEnable` to wire RectTransform fields — this is the ThemedPanel anti-pattern.
4. Verify: Play Mode → notification overlay renders; no `LogError` on Awake.

---

## §Retrospective — Bake stages 1.0–9.1 + Phase B

Lessons synthesized from all bake stages and Phase B ad-hoc sweep. Format: lesson | symptom that triggered it | mitigation now encoded in pipeline or §Playbook.

| Lesson | Symptom | Mitigation |
|---|---|---|
| DB-wins contract must be established on day 0 | Stage 1.0: prefab edits lost on re-bake; team edited derived output | §Authority DB-wins contract; bake = deterministic derived artifact |
| Bake output is the slot-ordering truth | Toolbar slot rendered wrong icon; bake source looked correct | DC-2: audit `_detail.iconSpriteSlug` in output, not source |
| `CanvasWrapper` flatten is mandatory before descendant-ban | Stage 9.1: nested Canvas children blocked `validate:asset-pipeline` | `UiBakeHandler` now flattens `CanvasWrapper` subtree before emitting IR |
| Single-canvas root prevents HUD dedup errors | Stage 8: HUD bar duplicated across bake runs | Bake enforces single Canvas root per panel; dedup gate in validator |
| Partial-class base declaration must match file stem | Phase B: `GridAssetCatalog` showed "missing script" at scene load | DC-1 + `validate:asset-pipeline` schema gate on MonoBehaviour file stem |
| Bridge mutations require `save_scene` before any C# edit | Phase B: `create_gameobject` results discarded on domain reload | DC-4 + architectural principle: prefer code-side lazy-init over scene authoring |
| Empty `_streamingRelativePath` is expected post-v1 | Phase B: spurious `Debug.LogError` on every Play Mode enter | DC-3: silent early return + supersession comment; stale error removed |
| Lazy-init over scene wiring for runtime-only UI | Phase B: notification panel null on Awake (Inspector field never set) | DC-4 recipe: `LazyCreateNotificationUi()` pattern; ThemedPanel anti-pattern doc |
| `[ExecuteAlways]` OnEnable must NOT mutate RectTransform | Stage bake audit: bake authoring clobbered by runtime `OnEnable` override | MEMORY entry: ThemedPanel runtime rect anti-pattern; blocked in coding conventions |
| IR coverage must span all published panels | Stage 9.1 coverage gap: 9 IR panels sentinel-baked, 8 off-viewport | Deferred TECH follow-up; `prefab_manifest` walk as spot-check recipe |
| Prototype-first tracer slice required before full bake scope | Stages 2+ planned without tracer → late integration pain | Stage 1.0 tracer mandate in `prototype-first-methodology.md` |
| `validate:asset-pipeline` must run pre-land not post-land | Early stages: schema faults discovered post-merge | CI gate in `validate:all`; blocks PR merge on non-zero exit |
