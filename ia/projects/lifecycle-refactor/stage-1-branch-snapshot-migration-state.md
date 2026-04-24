### Stage 1 — Foundation: Freeze, Templates & Rules / Branch + Snapshot + Migration State

**Status:** Final

**Objectives:** Create migration branch. Snapshot current master plans + open specs + backlog yaml so M2/M3 can always re-read from clean state. Write migration JSON with resumability keys.

**Exit:**

- Branch `feature/lifecycle-collapse-cognitive-split` checked out.
- `ia/state/pre-refactor-snapshot/` contains tarball (or flat copy) of all `ia/projects/*master-plan*.md`, `ia/backlog/*.yaml`, `ia/backlog-archive/*.yaml`, and open `ia/projects/{ISSUE_ID}.md` files at snapshot time.
- `ia/state/lifecycle-refactor-migration.json` written with M0–M8 phase entries (`pending` / `done`) + per-file progress arrays for M2 and M3.
- Migration JSON M0 flipped to `done`.
- Phase 1 — Branch creation + freeze note + initial migration JSON.
- Phase 2 — Snapshot pre-refactor state + validate integrity.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T1.1 | Branch + freeze setup | **TECH-442** | Done (archived) | Create `feature/lifecycle-collapse-cognitive-split` via `git checkout -b`; add freeze note to `CLAUDE.md` §Key commands warning against running `/master-plan-new`, `/master-plan-extend`, `/stage-decompose`, `/stage-file` until M8 sign-off; write initial `ia/state/lifecycle-refactor-migration.json` (M0 done, M1–M8 pending; per-file arrays for M2: list of all `*master-plan*.md` paths, each `pending`; per-file array for M3: list of all `ia/backlog/*.yaml` + open `ia/projects/{ISSUE_ID}.md` paths). |
| T1.2 | Pre-refactor snapshot | **TECH-443** | Done (archived) | Copy all `ia/projects/*master-plan*.md`, `ia/backlog/*.yaml`, `ia/backlog-archive/*.yaml`, and open `ia/projects/{ISSUE_ID}.md` into `ia/state/pre-refactor-snapshot/` (preserve relative paths); write `ia/state/pre-refactor-snapshot/manifest.json` with file list + counts + git SHA; update migration JSON referencing snapshot path; flip M0 `done` in JSON. |

---
