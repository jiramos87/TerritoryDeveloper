---
purpose: "TECH-355 — Flock-guard `materialize-backlog.sh` + self-documenting header."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/backlog-yaml-mcp-alignment-master-plan.md"
task_key: "T2.1.1"
---
# TECH-355 — Flock-guard `materialize-backlog.sh` + self-documenting header

> **Issue:** [TECH-355](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-18
> **Last updated:** 2026-04-21

## 1. Summary

Wrap `node tools/scripts/materialize-backlog.mjs …` invocation inside `tools/scripts/materialize-backlog.sh` w/ `flock ia/state/.materialize-backlog.lock` so parallel stage-file runs + parallel MCP `backlog_record_create` callers serialize on regen step. Add caveman header comment naming lock path + rationale + cross-ref to `reserve-id.sh`. Satisfies Stage 2.1 Phase 1 exit criteria (flock routing + header doc) of `backlog-yaml-mcp-alignment-master-plan.md`.

## 2. Goals and Non-Goals

### 2.1 Goals

1. `materialize-backlog.sh` routes `.mjs` invocation through `flock ia/state/.materialize-backlog.lock`.
2. Lock file auto-created on first run if absent (touch under flock trap, same pattern as `reserve-id.sh`).
3. Header comment documents lock path + rationale + `reserve-id.sh` cross-ref (caveman prose).
4. Single-writer callers see zero behavior change.

### 2.2 Non-Goals (Out of Scope)

1. Concurrency test harness (→ TECH-356).
2. Validate-chain wiring (→ TECH-357).
3. Other flock domains (`.id-counter.lock`, `.closeout.lock`) — already in place.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | As a skill author running parallel stage-file, I want materialize-backlog to serialize so BACKLOG.md + BACKLOG-ARCHIVE.md stay consistent across concurrent writers. | Parallel invocations produce byte-identical output vs serial baseline (proven in TECH-356). |
| 2 | Developer | As a future maintainer reading the script, I want a header comment naming the lock path + rationale so I don't strip flock during refactor. | Header comment present, names `.materialize-backlog.lock`, cross-refs `reserve-id.sh`. |

## 4. Current State

### 4.1 Domain behavior

`tools/scripts/materialize-backlog.sh` currently calls `node tools/scripts/materialize-backlog.mjs …` unguarded. Parallel stage-file runs + future MCP `backlog_record_create` writers race on BACKLOG.md + BACKLOG-ARCHIVE.md regeneration — last writer wins, intermediate state possible.

### 4.2 Systems map

- `tools/scripts/materialize-backlog.sh` — target file.
- `tools/scripts/materialize-backlog.mjs` — inner node script (unchanged).
- `tools/scripts/reserve-id.sh` — reference flock pattern (`.id-counter.lock`, trap-based touch).
- `ia/state/.materialize-backlog.lock` — new lock file, auto-created.
- Invariant #13 (lockfile-per-domain) — canonical naming authority.

### 4.3 Implementation investigation notes

Match `reserve-id.sh` flock invocation style (exec 200> pattern or `flock -x` w/ fd). Trap `ERR EXIT` to ensure touched lock file left on disk for next run. `set -euo pipefail` already present in sibling scripts — mirror.

## 5. Proposed Design

### 5.1 Target behavior

Single-writer invocation: indistinguishable from today (lock acquired instantly, regen runs, lock released). Parallel invocations: each blocks on flock until predecessor finishes, serializing regen — no interleaved writes.

### 5.2 Architecture / implementation

Implementer owns exact flock syntax (fd-based `exec 200>` vs subshell `flock -x`). Must match `reserve-id.sh` convention for consistency.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-18 | Merge original T2.1.1 (flock wrap) + T2.1.2 (header doc) into single issue | flock wrap + self-documenting header = atomic edit on same file; split creates thrash across two tiny PRs | Keep T2.1.1 + T2.1.2 separate per original master plan task table |
| 2026-04-18 | Use lock name `ia/state/.materialize-backlog.lock` (not `.backlog.lock` per orchestrator prose) | Invariant #13 explicitly names `.materialize-backlog.lock` for this domain; spec wins on glossary/invariant conflict per terminology-consistency rule; invariant text authoritative | Use `.backlog.lock` per orchestrator prose (rejected — contradicts invariant) |

## 7. Implementation Plan

### Phase 1 — Flock wrap + header comment

- [ ] Read `tools/scripts/reserve-id.sh` flock invocation pattern.
- [ ] Wrap `.mjs` call in `materialize-backlog.sh` w/ `flock ia/state/.materialize-backlog.lock`.
- [ ] Touch-under-trap so lock file auto-created.
- [ ] Add caveman header comment naming lock path + rationale + cross-ref to `reserve-id.sh`.
- [ ] Smoke-test: single invocation still regens BACKLOG.md + BACKLOG-ARCHIVE.md identically.
- [ ] `npm run validate:all` green.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Single-writer behavior unchanged | Node | `bash tools/scripts/materialize-backlog.sh` then `git diff BACKLOG.md BACKLOG-ARCHIVE.md` | Expect no diff on clean tree |
| Flock serialization observable | Deferred | → TECH-356 | Full N=8 concurrency assertion lives in sibling issue |
| Validate chain green | Node | `npm run validate:all` | CI mirror |

## 8. Acceptance Criteria

- [ ] `tools/scripts/materialize-backlog.sh` invokes `.mjs` under `flock ia/state/.materialize-backlog.lock`.
- [ ] Lock file auto-created if absent (touch-under-trap).
- [ ] Header comment present, names lock path + rationale + `reserve-id.sh` cross-ref.
- [ ] Single-writer smoke test clean.
- [ ] `npm run validate:all` green.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | … | … | … |

## 10. Lessons Learned

- …

## §Plan Author

### §Audit Notes

- Risk: `flock` missing on developer Mac (Homebrew coreutils) — script should still fail loud if `flock` absent, or document dependency. Mitigation: match `reserve-id.sh` pattern; CI runs Linux where `flock` exists.
- Risk: lock file path typo (`materialize-backlog.lock` vs legacy `.backlog.lock` prose in old docs). Mitigation: invariant #13 name wins; header comment states canonical path.
- Risk: nested flock if script called from script — avoid double-lock deadlock. Mitigation: single entrypoint wraps node once only.
- Invariant touch: **#13** lockfile-per-domain — align comment with `ia/rules/invariants.md`.

### §Examples

| Caller | Behavior |
|--------|----------|
| Serial `materialize-backlog.sh` | Brief flock acquire; no starvation |
| 8 parallel callers (TECH-356) | Serialized regen; identical output to serial baseline |

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| single_writer_smoke | clean tree | `bash tools/scripts/materialize-backlog.sh`; no spurious BACKLOG diff | manual |
| validate_all | post-edit | `npm run validate:all` exit 0 | node |
| concurrent_deferred | — | Full N=8 harness in TECH-356 | deferred |

### §Acceptance

- [ ] `flock ia/state/.materialize-backlog.lock` wraps node invocation.
- [ ] Lock auto-created; header documents path + rationale + `reserve-id.sh` pointer.
- [ ] `npm run validate:all` green.

### §Findings

- Orchestrator Open Questions mention patching stale `.backlog.lock` prose — optional doc fix in TECH-357 or here if cheap.

## Open Questions (resolve before / during implementation)

None — tooling only; see §8 Acceptance criteria. Lock name conflict between orchestrator prose (`.backlog.lock`) and invariant #13 (`.materialize-backlog.lock`) resolved in Decision Log (invariant wins); optionally patch orchestrator prose during this issue OR defer to TECH-357 if doc edit cheaper there.
