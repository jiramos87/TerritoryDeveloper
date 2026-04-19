---
purpose: "TECH-350 — Distribution glossary rows in ia/specs/glossary.md."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-350 — Distribution glossary rows

> **Issue:** [TECH-350](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-18
> **Last updated:** 2026-04-18

## 1. Summary

Append four domain-term rows to `ia/specs/glossary.md` covering the Distribution Bucket 10 surface — **BuildInfo ScriptableObject**, **Release manifest (`latest.json`)**, **Update notifier**, **Unsigned installer tier**. Locks terminology before downstream stages wire the code surfaces. Follows `ia/rules/terminology-consistency-authoring.md` glossary authoring conventions.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Row **BuildInfo ScriptableObject** — refs `Assets/Scripts/Runtime/Distribution/BuildInfo.cs` (TECH-347).
2. Row **Release manifest (`latest.json`)** — forward-refs Stage 2.2 `web/public/download/latest.json`.
3. Row **Update notifier** — forward-refs Stage 2.3 `Assets/Scripts/UI/Distribution/UpdateNotifier.cs`.
4. Row **Unsigned installer tier** — forward-refs Stage 2.1 `tools/scripts/package-mac.sh` + `tools/scripts/package-win.ps1`.
5. Alpha-order placement per glossary convention.
6. `npm run validate:all` green (glossary lint + IA index regen check).

### 2.2 Non-Goals

1. Authoring the referenced code files — later stages.
2. Modifying existing glossary rows — append only.
3. Expanding rows into reference spec sections — glossary entries only.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer / Agent | As IA consumer, want canonical distribution terms in glossary so code + specs + BACKLOG rows use consistent vocabulary. | 4 rows present; `validate:all` green. |

## 4. Current State

### 4.1 Domain behavior

Glossary has no distribution-specific terms. Master plan + exploration doc use these terms freely w/o canonical anchor.

### 4.2 Systems map

| Path | Role | Status |
|---|---|---|
| `ia/specs/glossary.md` | Canonical domain vocabulary | edited here |
| `ia/rules/terminology-consistency-authoring.md` | Authoring rules | precondition (read) |
| `ia/projects/distribution-master-plan.md` | Orchestrator — lists these terms | reference |
| `docs/distribution-exploration.md` | Ground-truth design expansion | reference |

## 5. Proposed Design

### 5.1 Target behavior (product)

None — documentation only.

### 5.2 Architecture / implementation

Append rows in alpha-sort order per existing glossary conventions. Each row: `| Term | Definition | Spec reference | Category |` (or whatever column shape glossary.md uses — implementer confirms at kickoff). Example shape:

- **BuildInfo ScriptableObject** — Runtime SO instance at `Assets/Resources/BuildInfo.asset` carrying `version` / `gitSha` / `buildTimestamp` for the running build; written by `ReleaseBuilder` at build time, read by `UpdateNotifier` + Credits screen at runtime. Spec: `ia/projects/distribution-master-plan.md` §Stage 1.1; file: `Assets/Scripts/Runtime/Distribution/BuildInfo.cs`. Category: Distribution.
- **Release manifest (`latest.json`)** — JSON manifest served at `/download/latest.json` describing the current shipped build — `version`, `releasedAt`, `notes`, per-platform download URLs + sizes + SHA256, Gatekeeper/SmartScreen bypass strings. Fetched by in-game `UpdateNotifier` on launch. Spec: `ia/projects/distribution-master-plan.md` §Stage 2.2; file: `web/public/download/latest.json`. Category: Distribution.
- **Update notifier** — In-game MonoBehaviour that fetches the **Release manifest** on launch, compares vs local **BuildInfo ScriptableObject** via **SemverCompare**, and shows a non-blocking toast + `/download` link when a newer version exists. Silent-fail on network error. Spec: `ia/projects/distribution-master-plan.md` §Stage 2.3; file: `Assets/Scripts/UI/Distribution/UpdateNotifier.cs`. Category: Distribution.
- **Unsigned installer tier** — Distribution mode where platform-native installers (mac `.pkg`, win `.exe`) ship without Apple notarization or Authenticode signing; Gatekeeper + SmartScreen bypass documented on `/download` page. MVP scope per umbrella Bucket 10 hard deferral of signing. Spec: `ia/projects/distribution-master-plan.md` §Stage 2.1; files: `tools/scripts/package-mac.sh`, `tools/scripts/package-win.ps1`. Category: Distribution.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-18 | 4 rows in 1 issue (Phase 3 single-task) | Atomic single-doc edit; splitting into 4 issues multiplies orchestration 4x w/ zero parallelism gain (per stage-file sizing heuristic) | Split per term (Draft-bloat) |
| 2026-04-18 | Forward-refs cite Stage id + filepath, not issue ids | Stage 2.x tasks not yet filed — ids do not exist yet; citing master plan stage is stable | Cite placeholder TECH-TBD (breaks grep) |

## 7. Implementation Plan

### Phase 1 — Append rows

- [ ] Read `ia/specs/glossary.md` — confirm column shape + category conventions.
- [ ] Read `ia/rules/terminology-consistency-authoring.md` — follow authoring format.
- [ ] Insert 4 rows in alpha-sort position (likely scattered — B, R, Un, Up).
- [ ] Run `npm run validate:all` — green.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| 4 rows land | grep | `grep -E "BuildInfo ScriptableObject\|Release manifest\|Update notifier\|Unsigned installer tier" ia/specs/glossary.md \| wc -l` → 4+ | — |
| Glossary lint + IA index green | Node | `npm run validate:all` | — |

## 8. Acceptance Criteria

- [ ] 4 rows appended in alpha order.
- [ ] Each row: definition + spec reference + forward-ref citation.
- [ ] `npm run validate:all` green.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| — | — | — | — |

## 10. Lessons Learned

- …

## Open Questions (resolve before / during implementation)

1. None — tooling / documentation only.
