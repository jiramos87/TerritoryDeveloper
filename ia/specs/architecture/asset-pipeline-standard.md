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

**Drift case: partial-class MonoBehaviour binding**
Unity binds `: MonoBehaviour` to the file whose stem matches the class name. Secondary partial files MUST NOT redeclare the base. Symptom: component shows "missing script" at scene load. Fix: move `: MonoBehaviour` to the canonical file stem.

**Drift case: bake-output-truth**
When a toolbar slot shows wrong icon, audit the bake output (`_detail.iconSpriteSlug`) NOT the bake source YAML. In-place prefab edits are valid for one-off fixes; bake source touched only on pattern recurrence.

**Drift case: empty `_streamingRelativePath` benign**
v1 catalog snapshot path superseded by per-kind exports + `CatalogLoader` (TECH-2675). `Debug.LogError` on empty = stale dev noise. Pattern: silent early return + comment referencing supersession ticket.

**Drift case: bridge mutations ephemeral without `save_scene`**
`create_gameobject` / `attach_component` / `assign_serialized_field` apply to in-memory scene only. Any C# edit triggers domain reload discarding unsaved mutations. Always chain `save_scene` immediately after mutation batch, before C# edits. Prefer code-side lazy-init over scene authoring.
