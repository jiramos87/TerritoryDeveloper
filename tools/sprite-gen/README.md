# sprite-gen

Python offline tool for rendering isometric building sprites used in Territory Developer.

Takes YAML archetype specs from `specs/`, renders PNG variants to `out/` (gitignored),
and promotes approved sprites to `Assets/Sprites/Generated/` with Unity `.meta` files.

## Usage

```bash
# Render a single archetype (run from tools/sprite-gen; package root is `src`)
python -m src render <archetype>

# Render all specs in specs/
python -m src render --all

# Co-emit layered .aseprite files
python -m src render <archetype> --layered

# Promote a variant to Assets/Sprites/Generated/ (optional catalog push unless --no-push)
python -m src promote out/<name>_v01.png --as <slug>

# Reject variants under out/
python -m src reject <archetype_stem> --yes
```

Exit codes: 0 = success, 1 = spec/render/promote error, 2 = bad argument, 4 = Aseprite missing (`promote --edit`), 5 = catalog push failure.

## Dependencies

```
pip install -r requirements.txt
```

## Documentation

- Orchestrator: DB-backed master plan slug `sprite-gen` — render via `mcp__territory-ia__master_plan_render({slug: "sprite-gen"})`
- Exploration / design rationale: `docs/isometric-sprite-generator-exploration.md`
- Art / calibration source: `docs/sprite-gen-art-design-system.md` (DAS)
- User-facing field reference: `docs/sprite-gen-usage.md` (ground, `footprint_ratio`, `levels`, R11 `building` block)

## Run as service

`tools/sprite-gen` also runs as a long-lived FastAPI service (DEC-A3) so the
web Authoring Console can drive render/promote without shelling out.

```bash
# Boot FastAPI on 127.0.0.1:8765 (default port)
python -m src serve

# Override port (env, takes precedence)
SPRITE_GEN_PORT=9001 python -m src serve
```

Endpoints (TECH-1433 scope — typed Pydantic bodies arrive in TECH-1434):

| Method | Path | Purpose |
| --- | --- | --- |
| GET | `/list-archetypes` | List archetype slugs from `specs/` |
| GET | `/list-palettes` | List palette ids from `palettes/` |
| POST | `/render` | Run compose pipeline; emit variants under `BLOB_ROOT` |
| POST | `/promote` | Promote a `gen://` blob to a target slug |
| GET | `/healthz` | Liveness probe (dev convenience) |

Render output root: honours `BLOB_ROOT` env var when set; otherwise falls
back to the legacy `tools/sprite-gen/out/` dir until TECH-1435 wires the
canonical `var/blobs/` swap point (DEC-A25).

## BLOB_ROOT env var (DEC-A25 swap point)

`BLOB_ROOT` overrides the canonical blob root used by the service render
output and (post-TECH-1435) the `BlobResolver` lookup path. Default: a
repo-local `var/blobs/` root resolved relative to the repo root. Future
hosted blob stores swap in via this single env-var flip — no module edits
required.

## Archetype YAML format

Each archetype spec lives at `tools/sprite-gen/specs/{slug}.yaml`. Top-level keys (verbatim from `specs/building_residential_small.yaml`):

| Key | Type | Purpose |
| --- | --- | --- |
| `id` | string | Archetype slug (mirrors filename stem) |
| `class` | string | Catalog archetype class for grouping (e.g. `residential_small`) |
| `footprint` | `[w, d]` | Cell footprint in tile units |
| `terrain` | enum | `flat` / `slope` (drives compose pipeline) |
| `ground` | string | Ground auto-layer key (`grass_flat`, etc.) |
| `levels` | int | Vertical floors |
| `seed` | int | Deterministic RNG seed |
| `palette` | string | Palette id under `palettes/` |
| `building` | object | R11 nested spec (composition primitives) |
| `output` | object | `{name, variants}` — output filename + variant count |
| `diffusion` | object | `{enabled}` — optional diffusion pass toggle |

Full field reference (`footprint_ratio`, primitive `composition`, R11 nesting): `docs/sprite-gen-usage.md`.

## `gen://` blob ref convention

Render output is referenced via `gen://` URIs (DEC-A25 swap point). Format: `gen://{archetype_slug}/v{NN}` resolves via `BlobResolver` to a path under `BLOB_ROOT`. Stable across local + hosted blob stores; the resolver mediates the URI → physical path lookup so consumers (catalog `render_run` rows, Unity snapshot loader) never embed absolute paths.

Cross-refs: `render_run`, `archetype_version`, `blob_resolver` glossary terms in `ia/specs/glossary.md`.

## Status

`out/` is gitignored — rendered sprites are ephemeral until promoted.
Stage 6 adds pixel-native primitives, ground auto-layer, and R11 nested specs — see `docs/sprite-gen-usage.md` §Stage 6 fields.
