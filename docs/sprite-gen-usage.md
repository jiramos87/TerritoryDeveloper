# Sprite-gen usage (Territory)

Working notes for the **sprite-gen** CLI (Python package under `tools/sprite-gen/`).

- **Render:** `python -m src render <archetype>` or `python -m src render --all` (from `tools/sprite-gen/` with dependencies installed; `PYTHONPATH=.` is set by local pytest).
- **Layered Aseprite:** `python -m src render <archetype> --layered` co-emits `.aseprite` next to PNG variants.
- **Promote:** `python -m src promote out/<file>.png --as <slug>` copies into `Assets/Sprites/Generated/` and writes Unity `.meta`. Use `--no-push` to skip the catalog HTTP step.
- **Registry push:** set `TG_CATALOG_API_URL` to the base URL of the web app (no trailing slash required), or add `[catalog]` / `url = "..."` in `tools/sprite-gen/config.toml`. If push is enabled and the URL is missing, the CLI exits with code **5**.
- **Exit codes:** **0** success; **1** spec / IO / promote usage error; **2** argparse; **4** Aseprite binary not found (`promote --edit` on `.aseprite`); **5** catalog configuration, transport, or unrecoverable HTTP when pushing to the registry.

See `ia/projects/sprite-gen-master-plan.md` Stage 5 for the full curation and registry contract.
