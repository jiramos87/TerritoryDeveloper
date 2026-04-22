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

- Orchestrator master plan: `ia/projects/sprite-gen-master-plan.md`
- Exploration / design rationale: `docs/isometric-sprite-generator-exploration.md`

## Status

Stage 1 in progress. Canvas math, primitives, CLI, and promote flow are follow-up tasks.
`out/` is gitignored — rendered sprites are ephemeral until promoted.
