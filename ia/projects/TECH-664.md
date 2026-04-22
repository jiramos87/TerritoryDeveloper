---
purpose: "TECH-664 — Write to Unity consumable path."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: ia/projects/grid-asset-visual-registry-master-plan.md
task_key: T2.1.3
phases:
  - "Phase 1 — File writer"
---
# TECH-664 — Write to Unity consumable path

> **Issue:** [TECH-664](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-22
> **Last updated:** 2026-04-22

## 1. Summary

Wire export CLI to emit file under agreed Unity path (e.g. Assets/StreamingAssets/catalog/catalog-snapshot.json); document tradeoffs and generated asset policy.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Single authoritative output path documented in repo.
2. Idempotent write + mkdir -p behavior.
3. README or exploration pointer for Unity load contract.

### 2.2 Non-Goals (Out of Scope)

1. Editor file watcher (Stage 2.2 hot-reload stub).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | Run export; file appears under Unity tree | Path documented; repeat run overwrites cleanly |

## 4. Current State

### 4.1 Domain behavior

No generated snapshot in repo yet.

### 4.2 Systems map

tools/catalog-export writer, Assets/StreamingAssets or Assets/Resources target, .gitignore/.meta conventions per team policy.

## 5. Proposed Design

### 5.1 Target behavior (product)

Unity loads snapshot from known relative path at boot (consumption in 2.2).

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

Prefer StreamingAssets for raw JSON; document .meta if committing generated asset.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-22 | Scope from Stage 2.1 orchestrator | Filed via stage-file | — |
| 2026-04-22 | Output path `Assets/StreamingAssets/catalog/grid-asset-catalog-snapshot.json` | Raw JSON; no addressable name required; hot-reload stub in exploration §8.2 | `Resources/` load (larger player cache) |

## 7. Implementation Plan

### Phase 1 — File writer

- [ ] fs write + path resolve from repo root into `Assets/StreamingAssets/catalog/` (default) unless §6 records a `Resources` exception; mkdir -p; overwrite idempotent.
- [ ] Add §6 row citing `docs/grid-asset-visual-registry-exploration.md` §8.2 for hot-reload expectation stub.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Tooling | Node | `npm run validate:all` | |
| Path smoke | Manual | Run CLI; assert file exists | |

## 8. Acceptance Criteria

- [ ] Single authoritative output path documented in repo.
- [ ] Idempotent write + mkdir -p behavior.
- [ ] README or exploration pointer for Unity load contract.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| — | — | — | — |

## 10. Lessons Learned

- …

## §Plan Digest

### §Goal

Write serializer output to a single Unity-visible path under repo (`StreamingAssets` vs `Resources`) with documented `.meta` + reload notes.

### §Acceptance

- [ ] Export CLI writes bytes to chosen path idempotently.
- [ ] Stage 2.1 Exit doc names path + rationale in §7 or §Findings.
- [ ] No Unity C# changes in this task.

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| path_write | temp out dir | file bytes match serializer | node |

### §Examples

| Choice | Pros | Cons |
|--------|------|------|
| StreamingAssets | raw JSON friendly | platform path quirks |
| Resources | simple `Resources.Load` | size + cache semantics |

### §Mechanical Steps

#### Step 1 — document path decision in spec

**Goal:** Lock filesystem contract for Stage 2.2 loader.

**Edits:**

- `ia/projects/TECH-664.md` — **before**:

```
- [ ] fs write + path resolve from repo root; document in Stage 2.1 Exit / exploration cross-link.
```

  **after**:

```
- [ ] fs write + path resolve from repo root into `Assets/StreamingAssets/` subtree (default) unless §6 records Resources exception; mkdir -p; overwrite idempotent.
- [ ] Add §6 row citing `docs/grid-asset-visual-registry-exploration.md` §8.2 for hot-reload expectation stub.
```

**Gate:**

```bash
npm run validate:dead-project-specs
```

**STOP:** Revert §7 if master-plan anchor link breaks.

**MCP hints:** `backlog_issue`

#### Step 2 — mention output path in exploration

**Goal:** Single canonical human reference for Unity integrators.

**Edits:**

- `docs/grid-asset-visual-registry-exploration.md` — **before**:

```
        └── Export step → Unity-consumable snapshot + sprite import hygiene (PPU, pivot)
```

  **after**:

```
        └── Export step → Unity-consumable snapshot + sprite import hygiene (PPU, pivot) — default file path documented in Stage 2.1 TECH-664 §7/§Findings (`Assets/StreamingAssets/...` unless otherwise decided)
```

**Gate:**

```bash
npm run validate:dead-project-specs
```

**STOP:** If exploration anchor not unique, expand **before** with surrounding tree indentation from HEAD.

**MCP hints:** `plan_digest_resolve_anchor`

## Open Questions (resolve before / during implementation)

1. None — tooling only; see §8.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._
