# Sprite-gen usage (Territory)

Working notes for the **sprite-gen** CLI (Python package under `tools/sprite-gen/`).

- **Render:** `python -m src render <archetype>` or `python -m src render --all` (from `tools/sprite-gen/` with dependencies installed; `PYTHONPATH=.` is set by local pytest).
- **Layered Aseprite:** `python -m src render <archetype> --layered` co-emits `.aseprite` next to PNG variants.
- **Promote:** `python -m src promote out/<file>.png --as <slug>` copies into `Assets/Sprites/Generated/` and writes Unity `.meta`. Use `--no-push` to skip the catalog HTTP step.
- **Registry push:** set `TG_CATALOG_API_URL` to the base URL of the web app (no trailing slash required), or add `[catalog]` / `url = "..."` in `tools/sprite-gen/config.toml`. If push is enabled and the URL is missing, the CLI exits with code **5**.
- **Exit codes:** **0** success; **1** spec / IO / promote usage error; **2** argparse; **4** Aseprite binary not found (`promote --edit` on `.aseprite`); **5** catalog configuration, transport, or unrecoverable HTTP when pushing to the registry.

See `ia/projects/sprite-gen-master-plan.md` Stage 5 for the full curation and registry contract.

## Stage 6 — YAML / composer fields (DAS)

Calibrated in `docs/sprite-gen-art-design-system.md` (DAS). Prefer citing DAS section ids in specs instead of duplicating numbers.

| Field | Location | Semantics |
| --- | --- | --- |
| `class` | top-level | Archetype id (e.g. `residential_small`). Drives default **ground** and **footprint_ratio** when those keys are omitted. |
| `ground` | top-level | Ground material for `iso_ground_diamond` (auto-prepended). Class defaults apply when missing. String `none` disables the ground layer. |
| `levels` | top-level | Repeats `role: wall` entries `N` times with per-floor height from `LEVEL_H` in `src/constants.py` (DAS §2.4) when the wall entry has no `h` / `h_px`. |
| `building.footprint_ratio` | `building` | `[w, d]` scale factors in \([0,1]\) applied to each primitive’s resolved `w_px`/`d_px` (or tile `w`/`d`) before dispatch. Omitted → class default from DAS §2.5. |
| `building.composition` | `building` | R11: alternative to top-level `composition`. Loader aliases either form to the same list. |
| `role` | composition entry | `wall` \| `roof` — wall stacking and roof `offset_z_role: above_walls` interact with `levels`. |
| `w_px`, `d_px`, `h_px` | composition entry | Pixel-native sizes (DAS §2, R2). Tile aliases `w`, `d`, `h` still work (`w`,`d` × 32 for footprint). |

**§2.5 / §4.1 / §4.2** in DAS list default per-class `ground` and `footprint_ratio` keys.
