# sprite-gen

Python offline tool for rendering isometric building sprites used in Territory Developer.

Takes YAML archetype specs from `specs/`, renders PNG variants to `out/` (gitignored),
and promotes approved sprites to `Assets/Sprites/Generated/` with Unity `.meta` files.

## Usage

```bash
# Render a single archetype
python -m sprite_gen render <archetype>

# Render all specs in specs/
python -m sprite_gen render --all

# Override terrain field (flat works end-to-end; other slope ids pending Stage 1.4)
python -m sprite_gen render <archetype> --terrain flat
python -m sprite_gen render --all --terrain flat
```

Exit codes: 0 = success, 1 = spec/render error, 2 = bad argument.

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
