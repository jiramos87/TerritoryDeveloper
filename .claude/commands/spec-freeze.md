---
description: Single-step skill: calls master_plan_spec_freeze MCP with {slug, source_doc_path}. Fails fast if open_questions_count > 0. Emits frozen artifact path + spec_id to user. Bypass via --skip-freeze logs arch_changelog kind=spec_freeze_bypass. Prerequisite for /ship-plan Phase A gate.
argument-hint: "{slug} [--source {path}] [--version {N}] [--skip-freeze]"
---

# /spec-freeze — Freeze the design spec for a master-plan slug: reads the source exploration document, validates zero open questions, INSERTs ia_master_plan_specs row with frozen_at=NOW(), emits arch_changelog kind=spec_frozen. /ship-plan rejects non-frozen or open-question specs unless --skip-freeze bypass.

Drive `$ARGUMENTS` via the [`spec-freeze`](../agents/spec-freeze.md) subagent.

Follow `caveman:caveman` for all output. Standard exceptions: code, commits, verbatim error/tool output, structured MCP payloads. Anchor: `ia/rules/agent-output-caveman.md`.

## Triggers

- /spec-freeze {SLUG}
- spec freeze
- freeze spec for plan
<!-- skill-tools:body-override -->

# /spec-freeze {SLUG}

Freeze the design spec for master-plan slug `{SLUG}` so `/ship-plan` can proceed.

Reads `docs/explorations/{SLUG}.md`, counts open questions, and INSERTs an `ia_master_plan_specs` row with `frozen_at=NOW()`. Fails if any open questions remain (unless `--skip-freeze` bypass provided).

**Usage:** `/spec-freeze {SLUG} [--source {path}] [--version {N}] [--skip-freeze]`

Run `ia/skills/spec-freeze/SKILL.md` phases 1–3 end-to-end.
