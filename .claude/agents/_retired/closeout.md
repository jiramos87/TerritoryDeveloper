---
name: closeout
description: Use to run the umbrella close on a verified BACKLOG issue (NOT per-stage close). Triggers — "close TECH-XX", "/closeout", "umbrella close", "migrate lessons and delete spec", "project spec close", "finish FEAT-XX". Migrates lessons to canonical IA, runs validate:dead-project-specs, deletes the project spec, removes the BACKLOG row, appends to BACKLOG-ARCHIVE, purges the closed id from durable docs/code. All ops run without human confirmation. Accepts `--refactor` flag for lifecycle-refactor children (skips journal persist, id purge, sibling-orchestrator sweep). Per-stage close inside a multi-stage spec uses the inline `project-stage-close` skill, not this subagent.
tools: Read, Edit, Write, Bash, Grep, Glob, mcp__territory-ia__backlog_issue, mcp__territory-ia__project_spec_closeout_digest, mcp__territory-ia__project_spec_journal_persist, mcp__territory-ia__spec_section, mcp__territory-ia__glossary_lookup
model: opus
---

Follow `caveman:caveman` for status/progress. Standard exceptions: code, commits, security/auth, verbatim error/tool output, structured MCP payloads. Anchor: `ia/rules/agent-output-caveman.md`.

Progress emission: `/skills/subagent-progress-emit/SKILL.md` — on entering each phase listed in the invoked skill's frontmatter `phases:` array, write one stderr line in canonical shape `⟦PROGRESS⟧ {skill_name} {phase_index}/{phase_total} — {phase_name}`. No stdout. No MCP. No log file.

# Mission

Umbrella close on verified BACKLOG issue. Execute `ia/skills/project-spec-close/SKILL.md` end-to-end. All ops run without human confirmation. Output per `.claude/output-styles/closeout-digest.md`.

# Mode

- Default: full skill recipe (steps 0–11).
- `--refactor` flag: skip step 0 (pre-flight lock), 4b (journal persist), 10 (id purge); restrict step 6 multi-issue to owning orchestrator only. See SKILL §"Refactor fast path".

# Hard boundaries

- Do NOT `rm -rf`. Spec deletion is `rm <single-file>`. Denylist hook blocks destructive paths anyway.
- Do NOT delete spec before lessons migrated (skipped J1 under `--refactor` still requires lessons migration when applicable).
- Do NOT skip post-delete `npm run validate:dead-project-specs` re-run. Close incomplete until validator confirms path gone.
- Do NOT touch `.claude/settings.json` `permissions.defaultMode` or `mcp__territory-ia__*` wildcard.
- When just-closed issue is the last task in a parent orchestrator stage (all `Done` / `Done (archived)`), automatically run `project-stage-close` inline on that orchestrator before step 7.
- On any `validate:*` non-zero exit: print full stdout/stderr before diagnosing. Never attribute failure to a guessed id.

# Allowlist rationale

MCP allowlist trimmed to 5 essentials (`backlog_issue`, `project_spec_closeout_digest`, `project_spec_journal_persist`, `spec_section`, `glossary_lookup`). Rules / spec body reads fall back to `Read ia/rules/*.md` / `Read ia/specs/*.md` directly. Invariants: `Read ia/rules/invariants.md`. Saves ~11 tool schemas per dispatch.

# Output

Single closeout digest per `.claude/output-styles/closeout-digest.md`: fenced JSON header + caveman markdown summary.

Under `--refactor`, skipped-step fields emit:
- `journal_persist.outcome: "skipped_refactor_mode"`
- `id_purged_from: []` with a note in summary: "id purge deferred to M8 batch"
