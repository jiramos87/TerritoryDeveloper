### Stage 2.1 — Export CLI + snapshot schema

**Status:** Done

**Objectives:** Deterministic **DB → snapshot** export; **`--check`** mode for CI staleness; embed **`schemaVersion`**.

**Exit:**

- `node tools/...` (or `npm run catalog:export`) produces snapshot; second run stable ordering.
- Document inputs to hash key (exploration §7 baker determinism themes).

**Tasks:**

| Task | Name | Issue | Status | Intent |
|---|---|---|---|---|
| T2.1.1 | Export reads published rows | **TECH-662** | Done | Query joins asset/sprite/bind/economy; filter **`status=published`** for ship; dev flag includes draft. |
| T2.1.2 | Snapshot JSON schema + version | **TECH-663** | Done | Top-level **`schemaVersion`**, **`generatedAt`**, arrays for assets/sprites/bindings; stable sort keys. |
| T2.1.3 | Write to Unity consumable path | **TECH-664** | Done | Choose `StreamingAssets` vs `Resources`; document tradeoff; ensure `.meta` policy for generated file. |
| T2.1.4 | Import hygiene hooks | **TECH-665** | Done | Emit sidecar list of texture paths for allowlisted **`TextureImporter`** adjustment (or embed PPU per exploration §6). |
| T2.1.5 | Stale check mode | **TECH-666** | Done | `catalog:export --check` compares hash vs working tree file; exit non-zero on drift for CI optional gate. |

#### §Stage File Plan

<!-- stage-file-plan output — do not hand-edit; apply via stage-file-apply -->

```yaml
- reserved_id: TECH-662
  title: "Export reads published rows"
  priority: medium
  notes: |
    Postgres catalog export reader: join catalog_asset, catalog_sprite, catalog_asset_sprite, catalog_economy via web/lib/db pool.
    Default filter status=published; dev flag includes draft. Deterministic ORDER BY for stable snapshots. Aligns with Stage 2.1 Exit + exploration §8 snapshot lifecycle.
  depends_on: []
  related:
    - TECH-663
    - TECH-664
    - TECH-665
    - TECH-666
  stub_body:
    summary: |
      Implement Node/TS export path that queries joined catalog tables through existing DB access layer, emits in-memory row set suitable for snapshot serialization, default published-only with optional draft inclusion for dev.
    goals: |
      1. Published rows default; explicit dev mode for drafts.
      2. Deterministic ordering (stable sort keys documented).
      3. Column coverage matches asset/sprite/bind/economy contract from migrations 0011/0012.
    systems_map: |
      web/lib/db/, db/migrations/0011_catalog_core.sql + 0012_catalog_spawn_pools.sql, web/types/api/catalog*.ts DTOs, new tools/catalog-export or tools/scripts entry, package.json npm script alias catalog:export (stub OK until wired).
    impl_plan_sketch: |
      Phase 1 — Reader: implement SQL or Drizzle-free query module, unit/integration smoke against fixture DB or mocked pool; document connection env (DATABASE_URL).

- reserved_id: TECH-663
  title: "Snapshot JSON schema + version"
  priority: medium
  notes: |
    Versioned snapshot envelope: schemaVersion, generatedAt, ordered arrays for assets/sprites/bindings/economy. Stable key ordering. Contract doc for Unity GridAssetCatalog (Stage 2.2).
  depends_on: []
  related:
    - TECH-662
    - TECH-664
    - TECH-665
    - TECH-666
  stub_body:
    summary: |
      Define canonical snapshot JSON shape consumed by Unity loader: top-level metadata plus arrays; enforce stable sort; bump schemaVersion when breaking.
    goals: |
      1. Top-level schemaVersion + generatedAt ISO-8601.
      2. Arrays for assets, sprites, bindings, economy with stable sort keys.
      3. Human-readable schema note or JSON Schema file under tools/docs for agents.
    systems_map: |
      tools/catalog-export (serializer), docs/grid-asset-visual-registry-exploration.md §8.2, web/types/api/catalog*.ts field parity.
    impl_plan_sketch: |
      Phase 1 — Types + serializer: TypeScript interfaces matching DTOs; JSON.stringify with ordered keys; golden fixture test for sort stability.

- reserved_id: TECH-664
  title: "Write to Unity consumable path"
  priority: medium
  notes: |
    Choose StreamingAssets vs Resources; write generated JSON; document .meta policy and hot-reload dev note per master-plan Step 2 Objectives.
  depends_on: []
  related:
    - TECH-662
    - TECH-663
    - TECH-665
    - TECH-666
  stub_body:
    summary: |
      Wire export CLI to emit file under agreed Unity path (e.g. Assets/StreamingAssets/catalog/catalog-snapshot.json); document tradeoffs and generated asset policy.
    goals: |
      1. Single authoritative output path documented in repo.
      2. Idempotent write + mkdir -p behavior.
      3. README or exploration pointer for Unity load contract.
    systems_map: |
      tools/catalog-export writer, Assets/StreamingAssets or Assets/Resources target, .gitignore/.meta conventions per team policy.
    impl_plan_sketch: |
      Phase 1 — File writer: fs write + path resolve from repo root; document in Stage 2.1 Exit / exploration cross-link.

- reserved_id: TECH-665
  title: "Import hygiene hooks"
  priority: medium
  notes: |
    Sidecar or embedded list of texture paths + PPU/pivot hints for allowlisted TextureImporter adjustments (exploration §6). No Unity C# in this task—data for later pipeline.
  depends_on: []
  related:
    - TECH-662
    - TECH-663
    - TECH-664
    - TECH-666
  stub_body:
    summary: |
      Extend snapshot or sibling manifest with texture path hygiene fields so bake/import tooling can enforce PPU/pivot policy on allowlisted assets.
    goals: |
      1. Emit path list aligned with catalog_sprite allowlist rules.
      2. Embed or reference PPU/pivot per exploration §6.
      3. Document consumer (editor script vs manual) as stub if not automated yet.
    systems_map: |
      tools/catalog-export manifest emitter, ia/specs/coding-conventions.md TextureImporter notes, exploration §6.
    impl_plan_sketch: |
      Phase 2 — Hygiene manifest: additional JSON section or sidecar file; validate against sample rows.

- reserved_id: TECH-666
  title: "Stale check mode"
  priority: medium
  notes: |
    catalog:export --check: hash inputs + snapshot bytes vs working tree; non-zero exit on drift for optional CI gate. Tie to exploration §7 baker determinism themes.
  depends_on: []
  related:
    - TECH-662
    - TECH-663
    - TECH-664
    - TECH-665
  stub_body:
    summary: |
      Add CLI mode that recomputes export and compares fingerprint to committed artifact; fails when developers forget to refresh snapshot.
    goals: |
      1. Deterministic hash of inputs (connection string excluded; schema + published rows + export version).
      2. Exit code 0 match, non-zero drift.
      3. Document optional CI wiring (non-blocking advisory acceptable).
    systems_map: |
      tools/catalog-export CLI argv parsing, crypto.createHash or stable stringify, CI doc snippet in task §Findings or README.
    impl_plan_sketch: |
      Phase 2 — --check flag: parse args, run export in memory, diff vs on-disk file, stderr message on mismatch.
```

#### §Plan Fix — PASS (no drift)

> plan-review exit 0 — all Task specs aligned. No tuples emitted. Downstream pipeline continue.

#### §Stage Audit

_retroactive-skip — Stage archived prior to 2026-04-24 lifecycle refactor that introduced the canonical `§Stage Audit` subsection (see `ia/projects/MASTER-PLAN-STRUCTURE.md` §3.4 + Changelog entry 2026-04-24). Task-level §Audit prose captured in per-Task specs during Stage-scoped closeout before spec deletion; no retroactive re-run needed._
