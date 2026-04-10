# TECH-33 — Prefab manifest and scene MonoBehaviour listing

> **Issue:** [TECH-33](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-02
> **Last updated:** 2026-04-02

**Related tooling:** [docs/agent-tooling-verification-priority-tasks.md](../../docs/agent-tooling-verification-priority-tasks.md) — tasks **26**, **27**. Supports **ART-01**–**ART-04**, **UI** / **toolbar** layout work.

## 1. Summary

Provide **Unity Editor or batchmode** scripts that (1) scan **`Assets/Prefabs/`** (or agreed roots) for **missing script** references on prefabs; (2) parse **`MainScene.unity`** (or agreed scene) for **MonoBehaviour** type names and **hierarchy paths** useful for agent navigation. Output **JSON** or **Markdown** under `tools/reports/` or stdout for CI.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Prefab report lists asset path + missing script count / GUID holes.
2. Scene report lists `GameObject path → component type` for **MonoBehaviour** derivatives (filter noise if needed).
3. Documented invocation for humans and agents.

### 2.2 Non-Goals (Out of Scope)

1. Auto-fixing missing scripts.
2. Scanning every scene in repo in v1 — start with **MainScene** unless expanded in Decision Log.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Art / dev | I want to find broken prefabs before playtest. | Report lists paths. |
| 2 | Agent | I want to know what lives on Load Game panel. | Scene dump includes `UIManager` hierarchy slice. |

## 4. Current State

### 4.1 Domain behavior

N/A — asset hygiene.

### 4.2 Systems map

| Area | Pointer |
|------|---------|
| Scene | `Assets/Scenes/MainScene.unity` (confirm path in repo) |
| Prefabs | `Assets/Prefabs/` |

## 5. Proposed Design

### 5.1 Target behavior (product)

N/A.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

- Use `AssetDatabase` in Editor; for CI use `-batchmode -executeMethod` static entry point.
- Scene YAML parse is fragile; prefer Unity **SerializedObject** traversal in Editor.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-02 | Editor-first | Reliability | Raw YAML only |

## 7. Implementation Plan

### Phase 1 — Prefab manifest

- [ ] Implement scan + report.

### Phase 2 — Scene listing

- [ ] Implement MainScene dump; extend paths in Decision Log.

## 8. Acceptance Criteria

- [ ] **Unity:** Script runs without exception; output file readable.
- [ ] **Toolbar**-related rows can cite report paths in **Notes** when used.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
|  |  |  |  |

## 10. Lessons Learned

- 

## Open Questions (resolve before / during implementation)

None — tooling only.
