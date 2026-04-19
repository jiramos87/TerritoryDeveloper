---
purpose: "TECH-442 — Branch + freeze setup + initial migration JSON (Stage 1.1 Phase 1)."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/lifecycle-refactor-master-plan.md"
task_key: "T1.1.1"
---
# TECH-442 — Branch + freeze setup + initial migration JSON (Stage 1.1 Phase 1)

> **Issue:** [TECH-442](../../BACKLOG.md)
> **Status:** In Review
> **Created:** 2026-04-18
> **Last updated:** 2026-04-18

## 1. Summary

Bootstrap big-bang lifecycle refactor. Cut migration branch `feature/lifecycle-collapse-cognitive-split`. Add freeze note to `CLAUDE.md` blocking lifecycle authoring commands until M8 sign-off. Write initial `ia/state/lifecycle-refactor-migration.json` (M0 done; M1–M8 pending; M2 + M3 per-file arrays seeded). Satisfies Stage 1.1 Exit bullets 1, 3, 4 of `lifecycle-refactor-master-plan.md`.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Branch `feature/lifecycle-collapse-cognitive-split` checked out from `main`.
2. Freeze note appended to `CLAUDE.md` §Key commands. Note explicitly forbids `/master-plan-new`, `/master-plan-extend`, `/stage-decompose`, `/stage-file` until M8 sign-off (T4.2.1).
3. `ia/state/lifecycle-refactor-migration.json` written. Schema: `{phases: {M0..M8: {status: 'done'|'pending', ...}}, files: {M2: [...], M3: {specs: [...], yaml: [...]}}}`. M0 status `done`. M1–M8 status `pending`. M2 array lists every `ia/projects/*master-plan*.md` path, each `pending`. M3.yaml lists every `ia/backlog/*.yaml`, each `pending`. M3.specs lists every open `ia/projects/{ISSUE_ID}.md`, each `pending`.
4. `npm run validate:all` exit 0 after edits.

### 2.2 Non-Goals (Out of Scope)

1. No snapshot copy — TECH-443 owns snapshot + manifest.
2. No template / rule / glossary edits — Stage 1.2 owns those.
3. No Unity runtime touch.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | As lifecycle refactor pilot, I want a dedicated branch + freeze note + migration JSON so concurrent authoring during M0–M8 cannot corrupt mid-flight state. | Branch + freeze + JSON all present; subsequent agents read JSON to resume. |

## 4. Current State

### 4.1 Domain behavior

Repo on `feature/master-plans-1`. No freeze active. No migration JSON. Concurrent `/stage-file` calls would race against forthcoming Stage 1.2 template rewrites.

### 4.2 Systems map

- `CLAUDE.md` — freeze note insertion target.
- `ia/state/` — new file `lifecycle-refactor-migration.json`.
- Lifecycle skills (`stage-file`, `master-plan-new`, `master-plan-extend`, `stage-decompose`) — read-only beneficiaries of freeze note (no code change here).

## 5. Proposed Design

### 5.1 Target behavior (product)

After this task: any agent / human inspecting `CLAUDE.md` sees explicit "DO NOT run X/Y/Z until M8" notice. Migration JSON exposes per-phase + per-file resumability.

### 5.2 Architecture / implementation

1. `git checkout -b feature/lifecycle-collapse-cognitive-split` from current branch tip.
2. Insert freeze note block in `CLAUDE.md` §5 Key commands (top of section, callout style).
3. Generate migration JSON via small one-shot script or direct write. Seed M2 with `ls ia/projects/*master-plan*.md`. Seed M3.yaml with `ls ia/backlog/*.yaml`. Seed M3.specs by enumerating `ia/projects/*.md` minus `*master-plan*.md` minus directory entries.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-18 | File Stage 1.1 with 1 task per Phase (T1.1.1 + T1.1.2) | Stage 1.1 phases hold 1 task each by design — sequential infra ops; rule rewrite in Stage 1.2 (T1.2.3) lifts cardinality from per-Phase to per-Stage where 2 tasks satisfies. | Splitting branch creation from freeze note → adds churn without parallelism; migration JSON seeding into separate task → adds dependency without value. |

## 7. Implementation Plan

### Phase 1 — Branch + freeze note

- [ ] Create branch from `main` (or current feature tip, depending on user direction at kickoff).
- [ ] Append freeze note block to `CLAUDE.md` §5.
- [ ] Commit with message referencing TECH-442.

### Phase 2 — Migration JSON

- [ ] Enumerate seed paths via shell.
  - Example: `ls ia/projects/*master-plan*.md`, `ls ia/backlog/*.yaml`, `ls ia/projects/*.md | grep -v master-plan`.
- [ ] Write `ia/state/lifecycle-refactor-migration.json`.
  - Shape: `{"phases":{"M0":{"status":"done"},"M1":{"status":"pending"},…,"M8":{"status":"pending"}},"files":{"M2":[{"path":"ia/projects/foo-master-plan.md","status":"pending"},…],"M3":{"specs":[…],"yaml":[…]}}}`.
- [ ] Validate JSON parses + matches expected schema.
- [ ] `npm run validate:all` green.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Migration JSON valid + per-file arrays seeded | Node | `node -e "JSON.parse(require('fs').readFileSync('ia/state/lifecycle-refactor-migration.json'))"` + assertion script | Tooling-only; no Unity touch. |
| IA validators green after CLAUDE.md edit | Node | `npm run validate:all` | Same chain CI runs. |

## 8. Acceptance Criteria

- [ ] Branch `feature/lifecycle-collapse-cognitive-split` exists.
- [ ] Freeze note in `CLAUDE.md` §Key commands.
- [ ] `ia/state/lifecycle-refactor-migration.json` written with M0 done + M1–M8 pending + M2 + M3 per-file arrays seeded.
- [ ] `npm run validate:all` exit 0.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | … | … | … |

## 10. Lessons Learned

- …

## Open Questions (resolve before / during implementation)

None — tooling only; see §8 Acceptance criteria. Possible runtime decision at kickoff: branch off `main` vs current `feature/master-plans-1` tip — implementer asks user before `git checkout -b`.
