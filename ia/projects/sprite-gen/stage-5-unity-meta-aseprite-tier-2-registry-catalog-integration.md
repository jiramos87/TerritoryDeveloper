### Stage 5 — Layer 5 Curation + Snapshot push hook / Unity meta + Aseprite Tier-2 + Registry catalog integration


**Status:** Final — tasks **TECH-179..183** + **TECH-674..679** archived 2026-04-22. Dependency gate (TECH-640..645) satisfied (archived).

**Objectives:** (1) Close Layer 5 of the composer by shipping the curation CLI (`promote` / `reject`) + Unity `.meta` writer + Aseprite Tier-2 integration (layered `.aseprite` emission + `promote --edit` round-trip) — relocated from Stage 4. (2) On `promote` success, POST the promoted sprite metadata to the `tg-catalog-api` `/api/catalog/assets` endpoint so each approved PNG lands as a Postgres catalog row in the live snapshot automatically. Idempotent by slug — 409 Conflict triggers PATCH via `updated_at` optimistic lock when asset metadata drifted, or skip when already identical. `--no-push` CLI flag for offline / air-gapped work. Closes the sprite-gen → registry feed contract per asset-snapshot-mvp L6 + L7 + L8 (sprite-gen emits PNG + `.meta` only; Postgres owns the catalog; writes go through HTTP not SQL).

**Exit:**

- `promote out/X.png --as final_name` copies PNG to `Assets/Sprites/Generated/` + writes `.meta` (PPU=64, pivot=(0.5, 16/h), Point filter, no compression)
- `reject {archetype}` deletes all `out/{archetype}_*.png` files
- `render --layered {archetype}` emits `.aseprite` alongside flat PNG with named layers `top`, `south`, `east`, `foundation` (only when non-flat); opening in Aseprite shows layers editable separately
- `promote out/X.aseprite --as name --edit` launches Aseprite CLI to flatten, writes PNG + `.meta` to `Assets/Sprites/Generated/`; exits code 4 when Aseprite binary not found with install hint
- `src/registry_client.py` — `RegistryClient(url, timeout=5)` with `create_asset(payload)` + `patch_asset(id, payload, updated_at)` + `get_asset_by_slug(slug) -> Optional[dict]`; error hierarchy `RegistryClientError` → `ConnectionError` / `ConflictError` / `ValidationError`; `requests` added to `requirements.txt`.
- Endpoint resolution order: env `TG_CATALOG_API_URL` → `tools/sprite-gen/config.toml` `[catalog] url` → raise `CatalogConfigError` when push=True and neither set.
- `curate.promote(src, dest_name, edit=False, push=True)` — after PNG + `.meta` land, builds payload (slug = `dest_name`, `world_sprite_path` = `Assets/Sprites/Generated/{dest_name}.png`, `ppu=64`, `pivot = (0.5, 16/canvas_h)`, `generator_archetype_id` from spec meta, `category` from spec class) and POSTs via `RegistryClient.create_asset`.
- Conflict handling: 409 → `get_asset_by_slug` → compare `world_sprite_path` + `generator_archetype_id` → match ⇒ skip (idempotent); mismatch ⇒ `patch_asset` with fresh `updated_at`; retry on 409 at most once; other 4xx ⇒ exit code 5.
- `cli.py promote ... --no-push` flag short-circuits the HTTP step entirely; `promote --edit` path respects `--no-push` (single push per promote).
- `tests/test_registry_client.py` — `responses` library fixtures covering 200 create, 409 identical (skip), 409 drift (PATCH), 422 validation, connection refused.
- `pytest tools/sprite-gen/tests/` exit 0; README CLI table + `docs/sprite-gen-usage.md` updated with `--no-push` + env var + config.toml stanza.

**Dependency gate:** Registry-push half of the stage (T5.6..T5.11) opens only after `grid-asset-visual-registry-master-plan.md` Step 1 Stage 1.3 archives `TECH-640`..`TECH-645` (HTTP `POST /api/catalog/assets` + `PATCH /api/catalog/assets/:id` + 409 optimistic-lock contract live). Curation half (T5.1..T5.5) carries no external dependency — can proceed immediately since issues TECH-179..TECH-183 are already filed.

**Tasks:**


| Task  | Name                      | Issue        | Status          | Intent                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                               |
| ----- | ------------------------- | ------------ | --------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| T5.1  | Unity meta writer         | **TECH-179** | Done (archived) | `src/unity_meta.py` — `write_meta(png_path, canvas_h) → str`: emit Unity `.meta` YAML string with guid (uuid4), textureImporter settings: PPU=64, spritePivot=(0.5, 16/canvas_h), filterMode=Point, textureCompression=None, spriteMode=Single; `src/curate.py` — `promote(src_png, dest_name)`: copy PNG to `Assets/Sprites/Generated/{dest_name}.png`, call `write_meta`, write `.meta` file alongside. *(Relocated from Stage 4 T4.5 on 2026-04-22.)*                                                                                                                             |
| T5.2  | Promote/reject CLI        | **TECH-180** | Done (archived) | `src/cli.py` — `promote out/X.png --as name` command: call `curate.promote()`; assert dest file exists + `.meta` exists; `reject {archetype}` command: glob `out/{archetype}_*.png`, delete all; integration test: promote then reject the same file, assert `Assets/Sprites/Generated/` has promoted file, `out/` is clean after reject. *(Relocated from Stage 4 T4.6 on 2026-04-22.)*                                                                                                                                                                                             |
| T5.3  | Aseprite bin resolver     | **TECH-181** | Done (archived) | `src/aseprite_bin.py` — `find_aseprite_bin() → Path`: resolve in order `$ASEPRITE_BIN` env var → `tools/sprite-gen/config.toml` `[aseprite] bin` → platform default probes (macOS: `/Applications/Aseprite.app/Contents/MacOS/aseprite`, then `~/Library/Application Support/Steam/steamapps/common/Aseprite/Aseprite.app/Contents/MacOS/aseprite`); raise `AsepriteBinNotFoundError` on miss (caught by CLI, exit code 4 with install hint); unit test mocks filesystem + env var. *(Relocated from Stage 4 T4.7 on 2026-04-22.)*                                                   |
| T5.4  | Layered aseprite emit     | **TECH-182** | Done (archived) | `src/aseprite_io.py` — `write_layered_aseprite(dest_path, layers: dict[str, PIL.Image], canvas_size)`: write `.aseprite` via `py_aseprite` (add to `requirements.txt`) with named layers in stacking order (`foundation`, `east`, `south`, `top`); transparent alpha preserved per layer; update `src/compose.py` to split per-face buffers when `layered=True` flag passed; add `--layered` flag to `cli.py render`; composer always co-emits flat PNG so non-Aseprite users stay unblocked. *(Relocated from Stage 4 T4.8 on 2026-04-22.)*                                         |
| T5.5  | Promote --edit round-trip | **TECH-183** | Done (archived) | `src/curate.py` — extend `promote(src, dest_name, edit=False)`: if `src.suffix == '.aseprite'` and `edit=True`, shell-out `{aseprite_bin} --batch {src} --save-as {tmp}.png` (subprocess, check returncode), then run existing PNG promote pipeline on `{tmp}.png`; cleanup tmp after; `src/cli.py` — `promote ... --edit` flag; integration test: render --layered → modify one layer pixel via PIL → promote --edit → assert flattened PNG + `.meta` exist in `Assets/Sprites/Generated/`, assert modified pixel present in output. *(Relocated from Stage 4 T4.9 on 2026-04-22.)* |
| T5.6  | RegistryClient scaffold   | **TECH-674** | Done (archived) | `src/registry_client.py` — class `RegistryClient(url: str, timeout: int = 5)` with `create_asset(payload) -> dict`, `patch_asset(id: int, payload: dict, updated_at: str) -> dict`, `get_asset_by_slug(slug: str) -> Optional[dict]`; exception hierarchy `RegistryClientError` → `ConnectionError` / `ConflictError(existing_row)` / `ValidationError(errors)`; add `requests` to `tools/sprite-gen/requirements.txt`.                                                                                                                                                              |
| T5.7  | Catalog URL resolver      | **TECH-675** | Done (archived) | `src/registry_client.py` — `resolve_catalog_url() -> str`: read env `TG_CATALOG_API_URL` first, `tools/sprite-gen/config.toml` `[catalog] url` second; raise `CatalogConfigError` with hint when neither set and push=True; `--no-push` short-circuits (not called); unit test covers env precedence + config fallback + both-missing.                                                                                                                                                                                                                                               |
| T5.8  | Promote payload + push    | **TECH-676** | Done (archived) | Update `src/curate.py` `promote(src, dest_name, edit=False, push=True)` — after `.meta` writes succeed, call `_build_catalog_payload(dest_name, canvas_h, spec_meta) -> dict` (slug, world_sprite_path, ppu=64, pivot, generator_archetype_id, category) + `RegistryClient(resolve_catalog_url()).create_asset(payload)`. Catch `ConflictError` → compare rows → `patch_asset` on drift; noop on match.                                                                                                                                                                              |
| T5.9  | CLI --no-push flag        | **TECH-677** | Done (archived) | `src/cli.py` — extend `promote` command signature with `--no-push` (default false = push); pass through to `curate.promote(..., push=not args.no_push)`; ensure `promote --edit --no-push` skips HTTP once (single push path across flattened + direct PNG variants); `README.md` CLI usage table updated.                                                                                                                                                                                                                                                                           |
| T5.10 | RegistryClient tests      | **TECH-678** | Done (archived) | `tests/test_registry_client.py` — use `responses` fixture; cases: 200 create happy, 409 with matching existing row (skip, no PATCH), 409 with drifted existing row (PATCH issued with `updated_at`), 422 validation (ValidationError raised + CLI exit 1), `ConnectionError` (exit 5); assert no HTTP call made when `push=False`.                                                                                                                                                                                                                                                   |
| T5.11 | Promote integration smoke | **TECH-679** | Done (archived) | `tests/test_promote_push.py` — end-to-end: spin up `responses`-mocked catalog server; `render building_residential_small` → `promote out/X.png --as residential-small-01` → assert POST `/api/catalog/assets` issued with expected JSON payload; run `--no-push` variant → assert zero HTTP calls; document exit code 5 handling in `docs/sprite-gen-usage.md`.                                                                                                                                                                                                                      |


#### §Stage File Plan



```yaml
- reserved_id: ""
  title: "RegistryClient scaffold (sprite-gen → tg-catalog-api)"
  priority: high
  notes: |
    `tools/sprite-gen/src/registry_client.py` — shared HTTP client for catalog rows; add `requests` to requirements. Depends on grid registry Stage 1.3 (TECH-640..645 archived) for live POST/PATCH/409 contract.
  depends_on:
    - TECH-640
    - TECH-641
    - TECH-642
    - TECH-643
    - TECH-644
    - TECH-645
  related: []
  stub_body:
    summary: |
      New `RegistryClient` class in sprite-gen: create/patch/get-by-slug against tg-catalog-api; exception hierarchy; timeout + `requests` session.
    goals: |
      1. `create_asset`, `patch_asset`, `get_asset_by_slug` on JSON API. 2. Typed errors. 3. `requests` dependency declared.
    systems_map: |
      `tools/sprite-gen/src/registry_client.py` (new); `tools/sprite-gen/requirements.txt`.
    impl_plan_sketch: |
      Phase 1 — Implement client + exception classes; wire minimal JSON encode/decode; no CLI yet.
- reserved_id: ""
  title: "Catalog URL resolver (env + config.toml)"
  priority: high
  notes: |
    `resolve_catalog_url()` in `registry_client` module — env `TG_CATALOG_API_URL` beats `config.toml` `[catalog] url`; `CatalogConfigError` when push needs URL and both missing.
  depends_on:
    - TECH-640
    - TECH-641
    - TECH-642
    - TECH-643
    - TECH-644
    - TECH-645
  related: []
  stub_body:
    summary: |
      Central URL resolution for registry HTTP calls; testable without live server.
    goals: |
      1. Precedence order. 2. Error when no URL and push path requires one. 3. Unit tests for env, config, both-missing.
    systems_map: |
      `tools/sprite-gen/src/registry_client.py`; `tools/sprite-gen/config.toml` (add `[catalog]` if absent).
    impl_plan_sketch: |
      Phase 1 — Pure function + tests; no promote wiring yet.
- reserved_id: ""
  title: "Promote payload build + create/patch (409 drift)"
  priority: high
  notes: |
    Extend `curate.promote` to POST catalog asset after `.meta` write; 409 path — compare row, idempotent skip or `patch_asset` w/ `updated_at`; single retry.
  depends_on:
    - TECH-640
    - TECH-641
    - TECH-642
    - TECH-643
    - TECH-644
    - TECH-645
  related: []
  stub_body:
    summary: |
      Connect promotion pipeline to `RegistryClient` — slug, world path, ppu, pivot, generator_archetype_id, category.
    goals: |
      1. `_build_catalog_payload` from dest + spec meta. 2. 409: skip identical; PATCH on drift. 3. other 4xx → exit 5.
    systems_map: |
      `tools/sprite-gen/src/curate.py`; `registry_client.py`.
    impl_plan_sketch: |
      Phase 1 — Helper + call site after successful promote writes.
- reserved_id: ""
  title: "CLI `promote` `--no-push` flag + README"
  priority: high
  notes: |
    `cli.py` promote subcommand: `--no-push` skips HTTP; pass through to `curate.promote(..., push=...)`; works with `--edit` path; README table update.
  depends_on:
    - TECH-640
    - TECH-641
    - TECH-642
    - TECH-643
    - TECH-644
    - TECH-645
  related: []
  stub_body:
    summary: |
      User-facing opt-out of catalog push (offline / local Unity workflow).
    goals: |
      1. Argparse flag default false. 2. No HTTP when set. 3. Document in README.
    systems_map: |
      `tools/sprite-gen/src/cli.py`; `tools/sprite-gen/README.md`.
    impl_plan_sketch: |
      Phase 1 — Wire flag + one regression test.
- reserved_id: ""
  title: "RegistryClient + promote HTTP contract tests (responses)"
  priority: high
  notes: |
    `tests/test_registry_client.py` + responses fixtures — 200, 409 skip, 409 patch, 422, connection error; `push=False` no HTTP.
  depends_on:
    - TECH-640
    - TECH-641
    - TECH-642
    - TECH-643
    - TECH-644
    - TECH-645
  related: []
  stub_body:
    summary: |
      Isolated unit/integration tests for HTTP edge cases; `responses` added to dev requirements if not already.
    goals: |
      1. Cover all exit branches in client + curate error mapping. 2. pytest clean.
    systems_map: |
      `tools/sprite-gen/tests/test_registry_client.py` (new).
    impl_plan_sketch: |
      Phase 1 — responses mocks for `/api/catalog/assets`.
- reserved_id: ""
  title: "Promote → catalog smoke test + `sprite-gen-usage` exit codes"
  priority: high
  notes: |
    `tests/test_promote_push.py` end-to-end with mocked base URL; document exit code 5 in `docs/sprite-gen-usage.md` for registry failures.
  depends_on:
    - TECH-640
    - TECH-641
    - TECH-642
    - TECH-643
    - TECH-644
    - TECH-645
  related: []
  stub_body:
    summary: |
      Higher-level E2E: render fixture archetype, promote, assert POST body shape; `--no-push` → zero calls.
    goals: |
      1. E2E test file. 2. Docs: exit 5, env/config.
    systems_map: |
      `tools/sprite-gen/tests/test_promote_push.py`; `docs/sprite-gen-usage.md`.
    impl_plan_sketch: |
      Phase 1 — Smoke + doc edits.

```

### §Plan Review (Stage 5 — registry file batch)

`PASS` — 2026-04-22 — six new tasks **TECH-674**..**TECH-679** carry §Plan Digest + aggregate `docs/implementation/sprite-gen-stage-5-plan.md`. Prior curation issues **TECH-179**..**183** unchanged (already had digest).

#### §Plan Fix

> Opus `plan-review` writes targeted fix tuples here when a Stage's Task specs need tightening before first `/implement`. Sonnet `plan-applier` Mode plan-fix reads tuples and applies edits. Contract: `ia/rules/plan-apply-pair-contract.md`.

_pending — populated by `/plan-review` when fixes are needed._

#### §Stage Audit

> Opus `opus-audit` writes one `§Audit` paragraph per Task row here (Stage-scoped bulk, non-pair) once every Task reaches Done post-verify. Feeds `§Stage Closeout Plan` migration tuples downstream. Contract: `ia/rules/plan-apply-pair-contract.md` Stage-scoped non-pair row.

_retroactive-skip — Stage archived prior to 2026-04-24 lifecycle refactor that introduced the canonical `§Stage Audit` subsection (see `ia/projects/MASTER-PLAN-STRUCTURE.md` §3.4 + Changelog entry 2026-04-24). Task-level §Audit prose captured in per-Task specs during Stage-scoped closeout before spec deletion; no retroactive re-run needed._

#### §Stage Closeout Plan

> Opus `stage-closeout-plan` writes unified tuple list here ONCE per Stage when all Task rows reach `Done` post-verify. Sonnet `stage-closeout-apply` reads tuples and applies verbatim. Contract: `ia/rules/plan-apply-pair-contract.md` seam #4.

_pending — populated by `/closeout {{this-doc}} Stage {{N.M}}` planner pass when all Tasks reach `Done`._

---
