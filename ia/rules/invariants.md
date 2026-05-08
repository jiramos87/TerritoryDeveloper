---
purpose: "Universal-safety invariants + IA tooling guardrails — force-loaded, never violate"
audience: agent
loaded_by: always
slices_via: none
description: Universal-safety invariants + IA tooling guardrails (cross-domain; Unity rules split to unity-invariants.md)
alwaysApply: true
---

# System Invariants (NEVER violate)

12. Specs under `ia/specs/` for permanent domains only; use `ia/projects/` for issue-specific specs
13. Monotonic id source = `ia/state/id-counter.json` via `tools/scripts/reserve-id.sh`; never hand-edit the counter file or the `id:` field of an existing yaml record

# Guardrails (IF → THEN)

- IF closing a project spec → THEN migrate lessons learned to canonical docs before deleting
- IF creating a project spec → THEN use `ia/templates/project-spec-template.md`, name `{ISSUE_ID}.md` under `ia/projects/`
- IF adding a `flock` guard on a mutation path → THEN dedicate a distinct lockfile per concurrency domain (id-counter → `.id-counter.lock`; closeout → `.closeout.lock`; materialize-backlog → `.materialize-backlog.lock`; runtime-state → `.runtime-state.lock`); read-only validators skip `flock` entirely

# Universal safety (cross-harness)

- **MCP first.** Prefer `mcp__territory-ia__*` tools over reading whole `ia/specs/*.md`. Order: `backlog_issue` → `router_for_task` → `glossary_discover` / `glossary_lookup` (English only — translate from the conversation) → `spec_outline` / `spec_section` / `spec_sections` → `invariants_summary` / `list_rules` / `rule_content`. Server caches schema at session start; restart Claude Code / MCP host after editing tool descriptors. Fallback when MCP unavailable: `ia/rules/agent-router.md` + targeted file reads.
  - **Operational (not spec knowledge):** `runtime_state` — last `verify:local` / `db:bridge-preflight` / queued test scenario in `ia/state/runtime-state.json`. Not a substitute for `glossary_*` / `spec_section`.
- **Unity invariants.** Rules 1–11 + Unity-specific IF→THEN live in `ia/rules/unity-invariants.md` (`loaded_by: on-demand`). Touching `Assets/Scripts/**/*.cs`, `GridManager`, `HeightMap`, roads, water, cliffs → fetch via `rule_content unity-invariants` or `invariants_summary` (merges both files). Not needed for web/ / docs/ / IA / MCP-server tasks.
- **Hook denylist.** Bash PreToolUse hook blocks `git push --force*`, `git reset --hard*`, `git clean -fd*`, `rm -rf {ia,MEMORY.md,.claude,.git,/,~}*`, `sudo *` (exit 2). Scripts + rationale: `.claude/settings.json` + `tools/scripts/claude-hooks/`.
- **No invented skill flags / tool names.** Fetch schemas via MCP `list_*` / skill SKILL.md body; do not guess from `docs/mcp-ia-server.md` alone (catalog can lag).
- **`.archive/` = frozen historical.** `.ignore` hides from Grep; Glob may surface. Never edit, never cite as current.

# Numbering

Rules numbered 12–13 to preserve cardinal continuity with Unity rules 1–11. Merged total across both files = 13 invariants + 10 guardrails. MCP `invariants_summary` returns the merged shape.
