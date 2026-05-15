---
name: spec-freeze
purpose: >-
  Freeze the design spec for a master-plan slug: reads the source exploration
  document, validates zero open questions, INSERTs ia_master_plan_specs row
  with frozen_at=NOW(), emits arch_changelog kind=spec_frozen. /ship-plan
  rejects non-frozen or open-question specs unless --skip-freeze bypass.
audience: agent
loaded_by: "skill:spec-freeze"
slices_via: none
description: >-
  Single-step skill: calls master_plan_spec_freeze MCP with {slug, source_doc_path}.
  Fails fast if open_questions_count > 0. Emits frozen artifact path + spec_id
  to user. Bypass via --skip-freeze logs arch_changelog kind=spec_freeze_bypass.
  Prerequisite for /ship-plan Phase A gate.
triggers:
  - /spec-freeze {SLUG}
  - spec freeze
  - freeze spec for plan
argument_hint: "{slug} [--source {path}] [--version {N}] [--skip-freeze]"
model: sonnet
reasoning_effort: low
input_token_budget: 8000
tools_role: lifecycle-helper
tools_extra:
  - mcp__territory-ia__master_plan_spec_freeze
  - mcp__territory-ia__cron_arch_changelog_append_enqueue
caveman_exceptions:
  - code
  - commits
  - verbatim error/tool output
  - structured MCP payloads
hard_boundaries:
  - "Do NOT call ship-plan — this skill is a prerequisite gate only."
  - "Do NOT freeze if open_questions_count > 0 unless --skip-freeze explicitly provided."
  - "Do NOT write the spec body to filesystem — DB sole source of truth (ia_master_plan_specs)."
  - "Emit spec_id + frozen_at to user on success."
caller_agent: spec-freeze
---

# Spec-freeze skill

Caveman default — [`agent-output-caveman.md`](../../rules/agent-output-caveman.md).

**Role.** Single-step prerequisite gate. Freezes the design spec for a plan slug so `/ship-plan` can proceed. Writes one `ia_master_plan_specs` row via `master_plan_spec_freeze` MCP and emits an audit row to `arch_changelog`.

**Contract.** Spec frozen iff `frozen_at IS NOT NULL AND open_questions_count = 0`. `/ship-plan` Phase A queries `ia_master_plan_specs WHERE slug=$slug ORDER BY version DESC LIMIT 1` and rejects if contract not met.

---

## Inputs

| Param | Source | Notes |
|-------|--------|-------|
| `SLUG` | 1st arg | Bare master-plan slug. |
| `--source {path}` | optional | Override source doc path. Default: `docs/explorations/{slug}.md`. |
| `--version {N}` | optional | Spec version (default 1). Increment for revised spec freeze. |
| `--skip-freeze` | optional | Bypass open-questions gate. Calls `master_plan_spec_freeze` with `force=true`. Logs `kind=spec_freeze_bypass`. |

---

## Phase 1 — Resolve source path

Default: `docs/explorations/{slug}.md`. When `docs/explorations/{slug}.html` also exists, use the `.md` sidecar (refreshed by design-explore). Never extract HTML here — that is ship-plan Phase A.0.

Stop: path not found → `STOPPED — source_doc_missing: docs/explorations/{slug}.md`.

---

## Phase 2 — Call master_plan_spec_freeze MCP

```
master_plan_spec_freeze({
  slug: {SLUG},
  source_doc_path: {resolved_path},
  version: {N},
  force: {--skip-freeze present}
})
```

**Success → Phase 3.**

**open_questions_unresolved error** (and no `--skip-freeze`) → emit structured halt:

```json
{"escalation": true, "phase": 2, "reason": "open_questions_unresolved", "open_questions_count": N, "hint": "Resolve all open questions in source doc then re-run /spec-freeze {SLUG}."}
```

---

## Phase 3 — Emit result to user

Success output (product-language):

```
spec-freeze done. SLUG={slug} VERSION={version} SPEC_ID={spec_id} FROZEN_AT={frozen_at}
open_questions_count={N}
arch_changelog: kind={changelog_kind}
Next: /ship-plan {slug}
```

Bypass output when `--skip-freeze` used:

```
spec-freeze bypassed. SLUG={slug} VERSION={version} SPEC_ID={spec_id}
WARNING: open_questions_count={N} — bypass logged to arch_changelog kind=spec_freeze_bypass
Next: /ship-plan {slug} --skip-freeze
```

---

## Escalation shape

```json
{"escalation": true, "phase": <1|2|3>, "reason": "source_doc_missing | open_questions_unresolved | mcp_unavailable | db_error", "slug": "{slug}", "stderr": "<opt>"}
```
