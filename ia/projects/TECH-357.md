---
purpose: "TECH-357 — Wire materialize-concurrent into validate chain."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/backlog-yaml-mcp-alignment-master-plan.md"
task_key: "T2.1.4"
---
# TECH-357 — Wire materialize-concurrent into validate chain (Stage 2.1 Phase 2)

> **Issue:** [TECH-357](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-18
> **Last updated:** 2026-04-21

## 1. Summary

Add `validate:materialize-concurrent` (or equivalent) to root `package.json`, chaining `tools/scripts/test/materialize-concurrent.sh`. Integrate into `validate:all` **or** a dedicated concurrency sub-chain per repo convention. Update `ARCHITECTURE.md` **Local verification** table if it lists validate scripts. Depends on TECH-356.

## 7. Implementation Plan

- [ ] Phase 1 — Add npm script calling `materialize-concurrent.sh` with stable cwd = repo root.
- [ ] Phase 2 — Wire into `validate:all` or `validate:concurrency`; document in `ARCHITECTURE.md` if required by convention.

## §Plan Author

### §Audit Notes

- Risk: doubles CI time — keep script fast (8 short flock-held runs). Mitigation: run after yaml-heavy validators only if needed; consider optional CI job if too slow.
- Risk: script path typo breaks Windows agents. Mitigation: use `bash` explicitly in npm script; document WSL requirement if any.
- Ambiguity: orchestrator says “validate:all OR validate:concurrency” — pick one pattern matching existing `validate:cache-block-sizing` style. Resolution: grep `package.json` for `validate:` scripts and mirror.
- Invariant touch: no change to MCP server code — package.json + docs only.

### §Examples

| package.json fragment | Behavior |
|-----------------------|----------|
| `"validate:materialize-concurrent": "bash tools/scripts/test/materialize-concurrent.sh"` | Local + CI callable |

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| npm_script | `npm run validate:materialize-concurrent` | exit 0 | node |
| validate_all | `npm run validate:all` | includes new chain per decision | node |

### §Acceptance

- [ ] New script entry in `package.json` with documented name.
- [ ] Integration point recorded (validate:all or sibling).
- [ ] `ARCHITECTURE.md` updated when convention requires it.
- [ ] `npm run validate:all` green after wire-in.

### §Findings

- Stub expanded for plan-author parity; orchestrator task ids (e.g. T4.1) vs backlog yaml `task_key` (e.g. T2.1.1) may differ — specs mirror yaml for MCP `parent_plan` / `task_key` fields.

## Open Questions (resolve before / during implementation)

None — tooling only; blocked on TECH-356 script existing.
