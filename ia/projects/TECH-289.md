---
purpose: "TECH-289 — Split BACKLOG into per-issue YAML, parallel-safe stage-file / closeout."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-289 — Split BACKLOG into per-issue YAML — parallel-safe stage-file / closeout

> **Issue:** [TECH-289](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-17
> **Last updated:** 2026-04-17

<!--
  Filename: `ia/projects/{ISSUE_ID}-{description}.md` (e.g. `BUG-37-zone-cleanup.md`,
  `FEAT-44-water-junction.md`). Legacy bare `{ISSUE_ID}.md` accepted for back-compat.
  Structure guide: ../projects/PROJECT-SPEC-STRUCTURE.md
  Glossary: ../specs/glossary.md (spec wins on conflict).
  Stub only — `/kickoff` enriches §4–§6, §7 phase detail, §7b, §8 acceptance, §Open Questions.
  Authoring style: caveman prose (drop articles/filler/hedging; fragments OK). Tables, code, seed prompts stay normal.
-->

## 1. Summary

`BACKLOG.md` + `BACKLOG-ARCHIVE.md` single shared mutable files. Every mutator skill (`project-new`, `stage-file`, `project-spec-close`, `project-stage-close`, `release-rollout-enumerate`, `release-rollout-track`) scans both files for max id then row-inserts / row-removes. Two parallel agents → duplicate ids, insert-conflict, validator FS races, orphan project spec ↔ row pairs. Closeout purge pass (ripgrep + edit closed id across `ia/specs/**`, `docs/**`, `ia/rules/**`, code comments) = worst blast radius; two parallel closeouts rewriting same `glossary.md` / `progress.html` → inevitable conflict. Refactor: replace monolithic md w/ per-issue YAML record under `ia/backlog/**`, monotonic id counter in `ia/state/id-counter.json`, materialize md as generated artifact. Unblocks parallel agent orchestration across project-new / stage-file / closeout.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Per-issue yaml record — one file per open / closed issue under `ia/backlog/{id}.yaml` + `ia/backlog-archive/{id}.yaml`. File-disjoint across issues.
2. Monotonic id reservation — `tools/scripts/reserve-id.sh` + `ia/state/id-counter.json` under `flock`, atomic increment, zero duplicate id risk across parallel agents.
3. Materializer — `tools/scripts/materialize-backlog.sh` regenerates `BACKLOG.md` + `BACKLOG-ARCHIVE.md` from yaml dir. Round-trip safe (diff vs current md = whitespace-only).
4. Mutator skills rewritten — `project-new`, `stage-file`, `project-spec-close`, `project-stage-close`, `release-rollout-enumerate`, `release-rollout-track`, `master-plan-new`, `master-plan-extend`, `stage-decompose` use yaml dir + counter, not md regex scan.
5. MCP readers migrated — `backlog-parser.ts`, `backlog-issue`, `backlog-search`, `invariant-preflight`, `router-for-task`, `project-spec-closeout-parse.ts` load yaml; public MCP tool response shapes preserved (no agent-session regression).
6. Concurrency proven — parallel soak test (3 `project-new` in worktrees + 2 `closeout` disjoint purge sets) → zero dup ids, zero orphan specs, zero git conflicts.
7. Dashboard + release-rollout unaffected — `/dashboard` renders master plans, rollout tracker cells still advance through (a)–(f) w/ link-resolve intact.

### 2.2 Non-Goals (Out of Scope)

1. Backlog schema evolution beyond minimum viable yaml fields — `spec-kickoff` pins extension points, not this refactor.
2. Delete `BACKLOG.md` / `BACKLOG-ARCHIVE.md` from git — recommendation = keep as generated artifact, GitHub + dashboard back-compat.
3. Rename glossary / spec terminology surfaces beyond two new rows (**backlog record** / **backlog view**).
4. Refactor validators / CI pipelines beyond what migration requires.
5. Distributed / cross-repo lock — single-repo `flock` scope only.
6. Decouple release-rollout tracker from master plans — tracker format unchanged, only link resolution updated.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Agent orchestrator | I run 3 parallel `/project-new` in worktrees, get 3 distinct TECH ids, no md merge conflict | Parallel soak test §8 passes |
| 2 | Agent orchestrator | I run 2 parallel `/closeout` on disjoint purge sets, no git conflict, no orphan spec | Soak test passes + `validate:dead-project-specs` green |
| 3 | Developer | I browse `BACKLOG.md` on GitHub, rows still human-readable | Generated md matches pre-refactor format byte-for-byte (whitespace-only diff) |
| 4 | Agent | `backlog_issue TECH-X` MCP call returns same response shape as pre-refactor | MCP response schema unchanged |

## 4. Current State

### 4.1 Domain behavior

Single `BACKLOG.md` + `BACKLOG-ARCHIVE.md` hold all issue rows. Max-id scan = regex across both files per mutator call. Insert / remove = row-level edit. Id purge on closeout = repo-wide ripgrep + edit. Parallel agents = unsafe.

### 4.2 Systems map

Writer surfaces:
- `ia/skills/project-new/SKILL.md`, `ia/skills/stage-file/SKILL.md`, `ia/skills/project-spec-close/SKILL.md`, `ia/skills/project-stage-close/SKILL.md`.
- `ia/skills/master-plan-new/SKILL.md`, `ia/skills/master-plan-extend/SKILL.md`, `ia/skills/stage-decompose/SKILL.md`.
- `ia/skills/release-rollout-enumerate/SKILL.md`, `ia/skills/release-rollout-track/SKILL.md`, `ia/skills/release-rollout/SKILL.md`.

Reader surfaces:
- `tools/mcp-ia-server/src/parser/backlog-parser.ts`.
- `tools/mcp-ia-server/src/tools/backlog-issue.ts`, `backlog-search.ts`, `invariant-preflight.ts`, `router-for-task.ts`.
- `tools/mcp-ia-server/src/parser/project-spec-closeout-parse.ts`.
- `tools/validate-dead-project-spec-paths.mjs`.
- `tools/mcp-ia-server/scripts/project-spec-dependents.ts`.
- `ia/projects/full-game-mvp-rollout-tracker.md` (link resolution).

Docs / rules touched:
- `CLAUDE.md` §3, `AGENTS.md`, `docs/agent-lifecycle.md`.
- `ia/rules/invariants.md`, `ia/rules/terminology-consistency.md`.
- `ia/specs/glossary.md` (2 new rows).

### 4.3 Implementation investigation notes

- Existing `backlog-parser.ts` row parser = viable seed for one-time migration script.
- `flock` available on macOS + Linux agent hosts (not native — `util-linux` via brew on mac; test on agent machine).
- `id-counter.json` single-file lock = low contention (µs-scale critical section vs seconds of yaml write).
- Materialize post-hook vs end-of-session sweep — pick post-hook, simpler reasoning + always-consistent md on disk.

## 5. Proposed Design

### 5.1 Target behavior (product)

- Agents call `reserve-id.sh TECH` → prints next id, updates counter atomically under `flock`.
- Agents write `ia/backlog/{id}.yaml` + `ia/projects/{id}-{description}.md` in parallel w/ other agents (file-disjoint).
- On closeout: `git mv ia/backlog/{id}.yaml ia/backlog-archive/{id}.yaml` + flip `status: closed`, run id-purge w/ disjoint-set gate.
- `materialize-backlog.sh` runs post-mutation per skill → regenerates md view → commit yaml + md together.
- MCP readers load yaml dir, never md; response shapes unchanged for agent back-compat.

### 5.2 Architecture / implementation

Layer sketch (agent-owned unless user locks a decision):

1. **State layer** — `ia/backlog/*.yaml`, `ia/backlog-archive/*.yaml`, `ia/state/id-counter.json`.
2. **Lock layer** — `tools/scripts/reserve-id.sh` (`flock` wrapper); optional `.validator.lock` for `validate:dead-project-specs`.
3. **Materializer** — `tools/scripts/materialize-backlog.sh` (yaml → md; deterministic ordering = priority section + insertion order metadata).
4. **Readers** — MCP parser + tools load via yaml glob; response shapes preserved.
5. **Writers** — skill bodies rewritten to yaml write + post-hook materialize.
6. **Migration** — one-shot `tools/scripts/migrate-backlog-to-yaml.mjs` + round-trip gate.

Concurrency model:

| Operation | Concurrency | Mechanism |
|---|---|---|
| `project-new` N parallel | safe | `reserve-id.sh` flock on counter; yaml writes file-disjoint |
| `stage-file` task fanout | parallel | batch id reservation up front, parallel yaml + spec writes, single materialize at end |
| `closeout` N parallel | safe if purge-sets disjoint | pre-scan repo for id hits per closeout; orchestrator gates overlapping purges |
| `validate:dead-project-specs` | single-writer | `flock` on `.validator.lock` during FS walk |
| MCP readers | always parallel-safe | yaml dir glob is read-only |

### 5.3 Method / algorithm notes

YAML minimum viable schema (spec-kickoff confirms + extends):

```yaml
id: TECH-289
type: tech
title: "Split BACKLOG into per-issue YAML ..."
priority: medium
status: open
files:
  - BACKLOG.md
  - tools/mcp-ia-server/src/parser/backlog-parser.ts
spec: ia/projects/TECH-289.md
notes: |
  caveman prose body ...
acceptance: |
  - parallel soak test passes
  - round-trip migration gate empty
depends_on: []
related: []
created: 2026-04-17
section: "Medium Priority"   # priority section header for materialize
```

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-17 | File issue as P2 (Medium), not P1 | Infra unblocks throughput; no hotfix / save-corruption | P1 (rejected — no acute bug), P3 (rejected — blocks umbrella rollout speed-up) |

## 7. Implementation Plan

Stub — `/kickoff` refines phases + per-phase acceptance.

### Phase 1 — Migration script + round-trip gate

- [ ] Write `tools/scripts/migrate-backlog-to-yaml.mjs` reusing `backlog-parser.ts`.
- [ ] Emit `ia/backlog/{id}.yaml` + `ia/backlog-archive/{id}.yaml` from current md.
- [ ] Write `ia/state/id-counter.json` w/ per-prefix max.
- [ ] Write `tools/scripts/materialize-backlog.sh`.
- [ ] Gate: `migrate → materialize → diff vs original md` = whitespace-only / empty.

### Phase 2 — Lock primitives + reserve-id

- [ ] Write `tools/scripts/reserve-id.sh` (`flock` on counter file, atomic increment).
- [ ] Smoke-test concurrent invocation (N parallel calls → N distinct ids).

### Phase 3 — MCP readers migrate

- [ ] `backlog-parser.ts` → yaml loader (glob + per-file parse).
- [ ] `backlog-issue.ts`, `backlog-search.ts`, `invariant-preflight.ts`, `router-for-task.ts` — swap data source; preserve response shapes.
- [ ] `project-spec-closeout-parse.ts` — archive-side read via yaml.
- [ ] `npm test` under `tools/mcp-ia-server` green w/ yaml fixtures.

### Phase 4 — Writer skills rewrite

- [ ] `ia/skills/project-new/SKILL.md` — step 2 via `reserve-id.sh`, step 4 = write yaml.
- [ ] `ia/skills/stage-file/SKILL.md` — batch reserve, parallel yaml + spec, single materialize.
- [ ] `ia/skills/project-spec-close/SKILL.md` — `git mv` yaml + flip status; add disjoint-purge gate.
- [ ] `ia/skills/project-stage-close/SKILL.md` — yaml-aware refs.
- [ ] `ia/skills/master-plan-new/SKILL.md`, `ia/skills/master-plan-extend/SKILL.md`, `ia/skills/stage-decompose/SKILL.md` — yaml glob for max-id.

### Phase 5 — Release-rollout rails

- [ ] `ia/skills/release-rollout-enumerate/SKILL.md` — seed tracker from yaml dir.
- [ ] `ia/skills/release-rollout-track/SKILL.md` — "filed" signal = yaml exists + spec exists.
- [ ] End-to-end smoke: full-game-mvp umbrella → tracker → advance 1 row through (f) ≥1-task-filed.
- [ ] `ia/projects/full-game-mvp-rollout-tracker.md` link resolution verified.

### Phase 6 — Validators + materialize post-hook

- [ ] `tools/validate-dead-project-spec-paths.mjs` — reconcile `ia/projects/*.md` ↔ yaml dirs.
- [ ] `tools/mcp-ia-server/scripts/project-spec-dependents.ts` — yaml-aware.
- [ ] Add `npm run validate:backlog-yaml` — schema + id uniqueness + counter consistency.
- [ ] `validate:dead-project-specs` under `flock .validator.lock`.
- [ ] Wire materialize post-hook into every mutator skill.

### Phase 7 — Docs / rules / glossary

- [ ] `CLAUDE.md` §3 — new state dir + scripts.
- [ ] `AGENTS.md` — BACKLOG = view, yaml = record terminology.
- [ ] `docs/agent-lifecycle.md` — updated file map.
- [ ] `ia/rules/invariants.md` — monotonic id source = counter.
- [ ] `ia/rules/terminology-consistency.md` — id appearance in `ia/backlog/**/*.yaml` filenames.
- [ ] `ia/specs/glossary.md` — add **backlog record** + **backlog view** rows.
- [ ] Regenerate glossary + spec indexes (`npm run mcp-ia-index`).

### Phase 8 — Concurrency proof

- [ ] Parallel soak test — 3 `project-new` in worktrees.
- [ ] Parallel soak test — 2 `closeout` disjoint purge sets.
- [ ] Dashboard `/dashboard` render smoke on Vercel.
- [ ] Final `npm run validate:all` green.

## 7b. Test Contracts

Stub — `/kickoff` populates rows.

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Round-trip migration = empty diff | Node | `tools/scripts/migrate-backlog-to-yaml.mjs` + `materialize-backlog.sh` + `diff` | Gate before Phase 3 |
| Parallel id reservation zero-dup | Shell | `reserve-id.sh` concurrent soak | N=8 parallel, assert N distinct ids |
| MCP response shape preserved | Node | `tools/mcp-ia-server` `npm test` w/ yaml fixtures | `backlog_issue`, `backlog_search` schema unchanged |
| Parallel closeout disjoint-purge | Agent + shell | 2 `/closeout` runs on disjoint id hits; assert no git conflict | Soak doc per `ia/skills/release-rollout-skill-bug-log/SKILL.md` |
| Dashboard still renders | Manual / Vercel | `npm run deploy:web:preview` + visit `/dashboard` | Verify master-plans fetch + ISR |
| `validate:all` green | Node | `npm run validate:all` | Full chain |

## 8. Acceptance Criteria

- [ ] All skills / MCP readers / validators migrated; old `BACKLOG.md` scan paths removed from writer code.
- [ ] `npm run validate:all` green.
- [ ] Round-trip migration gate: `migrate → materialize → diff` = empty / whitespace-only.
- [ ] Parallel soak test — 3 `project-new` concurrent worktrees, 2 `closeout` disjoint purge sets: zero duplicate ids, zero orphan specs, zero git conflicts.
- [ ] Release-rollout end-to-end smoke: seed tracker → advance 1 row through (f) ≥1-task-filed → verify tracker link resolves.
- [ ] Dashboard `/dashboard` renders on Vercel post-deploy.
- [ ] `docs/agent-lifecycle.md` + `CLAUDE.md` §3 updated.
- [ ] Glossary rows **backlog record** + **backlog view** added + indexes regenerated.
- [ ] `npm run validate:backlog-yaml` exists + green.
- [ ] Public MCP tool response shapes (`backlog_issue`, `backlog_search`) unchanged — agent session back-compat.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | … | … | … |

## 10. Lessons Learned

- …

## Open Questions (resolve before / during implementation)

1. Keep generated `BACKLOG.md` + `BACKLOG-ARCHIVE.md` in git or gitignore? Recommendation: keep — GitHub browsing + dashboard back-compat + external-reader safety net.
2. YAML schema — minimum viable fields + extension points. Align w/ existing `backlog-parser.ts` row model; decide whether `notes` stays free-form prose or gains structured sub-fields.
3. Purge-set disjointness gate — orchestrator-level (release-rollout pre-scan) or per-closeout shell pre-scan + file-level lock on hit paths?
4. `id-counter.json` — single file (all prefixes) vs per-prefix file (lower contention, more state files)?
5. Materialize trigger — post-hook on every writer skill, or end-of-session sweep (simpler, possibly stale md between mutations)?
6. Rename `BACKLOG.md` surface terminology in skill bodies ("file a row" → "file an issue yaml") or keep old verb for minimal churn + render md as view label?
7. Windows agent host support — `flock` not native. Degraded single-writer mode OK, or block Windows entirely for mutator skills?
