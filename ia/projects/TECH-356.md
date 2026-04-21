---
purpose: "TECH-356 — Concurrency test materialize-concurrent.sh."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/backlog-yaml-mcp-alignment-master-plan.md"
task_key: "T2.1.3"
---
# TECH-356 — Concurrency test materialize-concurrent.sh (Stage 2.1 Phase 2)

> **Issue:** [TECH-356](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-18
> **Last updated:** 2026-04-21

## 1. Summary

Author `tools/scripts/test/materialize-concurrent.sh` (per orchestrator Stage 4): spawn **N=8** parallel `materialize-backlog.sh` invocations; after join, `diff` regenerated `BACKLOG.md` + `BACKLOG-ARCHIVE.md` against a **serial baseline** from the same repo state; fail on any byte difference. Mirrors `tools/scripts/test/reserve-id-concurrent.sh` structure.

## 7. Implementation Plan

- [ ] Phase 1 — Author `tools/scripts/test/materialize-concurrent.sh` w/ parallel workers + baseline compare.
- [ ] Document usage in script header; exit non-zero on mismatch.

## §Plan Author

### §Audit Notes

- Risk: test mutates workspace outputs — must run in temp dir or restore BACKLOG files after run. Mitigation: follow `reserve-id-concurrent.sh` pattern (copy to tmp or git checkout).
- Risk: flaky timing if flock broken — test would falsely pass. Mitigation: assert N parallel pids actually overlap (sleep in child) optional; at minimum verify script invokes 8 subshells.
- Ambiguity: baseline generation order — must use same `materialize-backlog.mjs` args as parallel path. Resolution: one function `run_serial_baseline` shared.
- Invariant touch: read-only compare only; no hand-edit of `ia/state/id-counter.json`.

### §Examples

| Step | Expected |
|------|----------|
| Serial baseline | Single `materialize-backlog.sh` → capture `BACKLOG.md` checksum |
| Parallel burst | 8 concurrent invocations → same checksum |

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| materialize_concurrent | repo w/ yaml records | exit 0; no diff vs baseline | bash |
| mismatch_injection | broken flock mock (future) | exit non-zero | deferred |

### §Acceptance

- [ ] Script lives under `tools/scripts/test/`; N=8 parallelism; compares to serial baseline.
- [ ] Non-zero exit on BACKLOG or archive diff.
- [ ] Header cites `reserve-id-concurrent.sh` as structural reference.

### §Findings

- Stub was thin — expanded Summary + frontmatter `parent_plan` / `task_key` for locator parity; align w/ backlog yaml record if ids differ.

## Open Questions (resolve before / during implementation)

None — tooling only; depends on TECH-355 flock wrapper landing first.
