# /spec-freeze {SLUG}

Freeze the design spec for master-plan slug `{SLUG}` so `/ship-plan` can proceed.

Reads `docs/explorations/{SLUG}.md`, counts open questions, and INSERTs an `ia_master_plan_specs` row with `frozen_at=NOW()`. Fails if any open questions remain (unless `--skip-freeze` bypass provided).

**Usage:** `/spec-freeze {SLUG} [--source {path}] [--version {N}] [--skip-freeze]`

Run `ia/skills/spec-freeze/SKILL.md` phases 1–3 end-to-end.
