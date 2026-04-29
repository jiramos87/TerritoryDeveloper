# tools/recipes/

Recipe YAML files for the DEC-A19 recipe engine.

Each `{slug}.yaml` is a deterministic flow consumed by `tools/recipe-engine`. Step kinds:
`mcp.{tool}` · `bash.{script}` · `sql.{op}` · `seam.{name}` · `gate.{validator}` · `flow.{seq|parallel|when|until}`.

Schema: [`../recipe-engine/schema/recipe.schema.json`](../recipe-engine/schema/recipe.schema.json).

## Authoring

1. Pick a slug (kebab-case) — must match `^[a-z][a-z0-9]*(-[a-z0-9]+)*$`.
2. Author `{slug}.yaml`. See `noop-smoke.yaml` for the minimal shape.
3. Validate via `npm run validate:recipe-drift`.
4. Run dry: `npm run recipe:run -- {slug} --dry-run`.

## Phase B caveats

- `mcp.*` steps require an injected MCP client; CLI mode returns
  `phase_b_no_mcp_client`. Run inside a subagent (Phase C wiring) for live MCP.
- `seam.*` steps are validate-only — pass `expected_output:` for round-trip
  validation. LLM dispatch lands in Phase C.
- `retry:` is rejected on seam steps (Q5 escalate-to-human policy).
