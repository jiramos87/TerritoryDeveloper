---
purpose: "Project spec for TECH-26 — Mechanical CI/repo checks: FindObjectOfType in hot paths and optional grid gate."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-26 — Mechanical CI/repo checks: FindObjectOfType in hot paths and optional grid gate

> **Issue:** [TECH-26](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-02
> **Last updated:** 2026-04-02

**Related tooling:** [docs/agent-tooling-verification-priority-tasks.md](../../docs/agent-tooling-verification-priority-tasks.md) — tasks **1**, **6**. Complements **BUG-14** (code fix).

## 1. Summary

Implement a **Node or shell** scanner that flags **`FindObjectOfType`** (and variants) inside **`Update`**, **`LateUpdate`**, **`FixedUpdate`**, or other configured per-frame methods — aligned with [`.cursor/rules/invariants.mdc`](../../.cursor/rules/invariants.mdc). Optionally add a **`rg`-based CI gate** that **fails** when new **`gridArray`** / **`cellArray`** references appear outside **`GridManager`** (**TECH-04**). **Phase 2:** maintain a **hot-path manifest** (from `ARCHITECTURE.md` / managers-reference) so the scanner can **prioritize** or **scope** reports for **AUTO** / simulation participants.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Script runs locally and optionally in CI with documented exit codes.
2. False-positive policy documented (e.g. allowlist file for rare exceptions).
3. Phase 2: manifest file `tools/hot-path-manifest.json` (or similar) listing high-priority types/paths.

### 2.2 Non-Goals (Out of Scope)

1. Fixing all violations in the same PR as the scanner — **BUG-14** / **TECH-04** own the fixes.
2. Parsing C# with full Roslyn unless team chooses — regex/heuristic is acceptable first pass with documented limits.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | I want CI to catch new per-frame **FindObjectOfType**. | CI fails on new violations. |
| 2 | AI agent | I want a script I can run before push. | `node tools/...` documented in backlog **Notes**. |

## 4. Current State

### 4.1 Domain behavior

N/A — enforcement of existing **invariants**.

### 4.2 Systems map

| Area | Pointer |
|------|---------|
| Rules | `invariants.mdc` — no **FindObjectOfType** in **Update** loops |
| Related bug | **BUG-14** |

## 5. Proposed Design

### 5.1 Target behavior (product)

N/A.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

- **Phase 1:** scan `Assets/Scripts/**/*.cs` for patterns; output TSV or JSON to stdout.
- **Optional gate:** `git diff` + `rg` for `gridArray`/`cellArray` outside `GridManager.cs`.
- **Phase 2:** load manifest; sort output or filter to manifest paths first.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-02 | Phase 2 bundled in TECH-26 | Roadmap task 6 | Separate issue |

## 7. Implementation Plan

### Phase 1 — Scanner + optional CI

- [ ] Implement FindObjectOfType-in-Update heuristic.
- [ ] Document usage; optional GitHub Action / local hook.

### Phase 2 — gridArray gate + manifest

- [ ] Add optional `rg` gate for **TECH-04**.
- [ ] Generate or hand-author hot-path manifest; integrate with scanner output.

## 8. Acceptance Criteria

- [ ] Running on current repo produces a report (may list existing debt).
- [ ] If CI is enabled: new violations fail; document **allowlist** process.
- [ ] **BUG-14** remains the issue for fixing **UIManager** / **CursorManager** paths.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
|  |  |  |  |

## 10. Lessons Learned

- 

## Open Questions (resolve before / during implementation)

None — tooling only; CI blocking vs advisory is a **Decision Log** entry.
