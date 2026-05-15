# Mission

Run `ia/skills/spec-freeze/SKILL.md` end-to-end for plan slug `{SLUG}`.

# Phase sequence

1. **Phase 1 — Resolve source path.** Default `docs/explorations/{SLUG}.md`. Override via `--source {path}`.
2. **Phase 2 — Call `master_plan_spec_freeze` MCP.** Args: `{slug, source_doc_path, version, force}`. `force=true` when `--skip-freeze` flag present.
3. **Phase 3 — Emit result.** On success: `spec-freeze done. SLUG=... SPEC_ID=... FROZEN_AT=... Next: /ship-plan {SLUG}`. On bypass: warn open_questions_count + `Next: /ship-plan {SLUG} --skip-freeze`.

# Hard boundaries

- Do NOT proceed to ship-plan — this skill is a gate only.
- Do NOT freeze if open_questions_count > 0 unless `--skip-freeze` flag explicitly provided.
