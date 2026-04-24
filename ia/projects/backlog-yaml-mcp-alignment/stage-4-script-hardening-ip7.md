### Stage 4 — MEDIUM / LOW band (IP6–IP9) / Script hardening (IP7)

**Status:** In Progress (TECH-355, TECH-356, TECH-357 filed)

**Objectives:** Flock-guard `materialize-backlog.sh` so parallel stage-file runs + parallel MCP `backlog_record_create` callers serialize on the regen step. Add a concurrency test mirroring the existing `reserve-id-concurrent.sh` harness. No schema or behavior change for single-writer callers.

**Exit:**

- `tools/scripts/materialize-backlog.sh` invocations route through `flock ia/state/.backlog.lock` (create the lock file if absent; same pattern as `reserve-id.sh`).
- `tools/scripts/test/materialize-concurrent.sh` — N=8 parallel invocations, assert BACKLOG.md + BACKLOG-ARCHIVE.md byte-identical to a serial baseline regen. Runs under `npm run validate:all` or a dedicated `npm run validate:materialize-concurrent` script.
- `ia/state/.backlog.lock` documented in `tools/scripts/materialize-backlog.sh` header comment.
- Phase 1 — flock wrapper + lock-file creation + self-documenting header (single task per Decision Log 2026-04-18: flock wrap + header = atomic edit on same file; split creates thrash).
- Phase 2 — concurrency harness + CI wire-in.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T4.1 | Flock-guard `materialize-backlog.sh` + self-documenting header | **TECH-355** | Draft | Wrap the `node tools/scripts/materialize-backlog.mjs …` invocation inside `tools/scripts/materialize-backlog.sh` with `flock ia/state/.materialize-backlog.lock` (invariant #13 lockfile-per-domain — supersedes earlier `.backlog.lock` prose). Create the lock file if absent (touch under flock trap, same pattern as `reserve-id.sh`). Caveman header comment documents lock path + rationale ("parallel stage-file + MCP `backlog_record_create` writers serialize here") + cross-ref to `reserve-id.sh`. Merges original T2.1.1 + T2.1.2 per Decision Log. |
| T4.3 | Concurrency test `materialize-concurrent.sh` | **TECH-356** | Draft | Author `tools/scripts/test/materialize-concurrent.sh` — spawn N=8 parallel `materialize-backlog.sh` invocations; after all complete, diff BACKLOG.md + BACKLOG-ARCHIVE.md against a serial baseline regen; fail on any diff. Mirrors `tools/scripts/test/reserve-id-concurrent.sh` structure. |
| T4.4 | Wire concurrency test into validate chain | **TECH-357** | Draft | Add `validate:materialize-concurrent` script to root `package.json`; chain into `validate:all` OR a new `validate:concurrency` sub-chain (match existing convention). Document in `ARCHITECTURE.md` Local verification table if listed there. |

### §Plan Fix — PASS (no drift)

> plan-review exit 0 — Stage 4 script-hardening Task specs (TECH-355..357) aligned w/ Stage block + §Plan Author + backlog yaml `parent_plan` / `task_key`. No fix tuples. Downstream: `/ship-stage` Pass 1 per task.
