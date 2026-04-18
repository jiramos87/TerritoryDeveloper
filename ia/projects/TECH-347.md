---
purpose: "TECH-347 — BuildInfo ScriptableObject type."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-347 — BuildInfo ScriptableObject type

> **Issue:** [TECH-347](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-18
> **Last updated:** 2026-04-18

## 1. Summary

Land `Assets/Scripts/Runtime/Distribution/BuildInfo.cs` — ScriptableObject data model carrying `version` / `gitSha` / `buildTimestamp` for the Unsigned installer tier distribution path. Inert runtime SO; editor-gated `WriteFields` writer consumed later by Stage 1.2 ReleaseBuilder and Stage 2.3 UpdateNotifier. First deliverable of Distribution Bucket 10; satisfies Stage 1.1 Exit criterion "BuildInfo compiles + `[CreateAssetMenu]` populates menu + `WriteFields` gated on `#if UNITY_EDITOR`".

## 2. Goals and Non-Goals

### 2.1 Goals

1. Author `BuildInfo.cs` ScriptableObject w/ private serialized `version` / `gitSha` / `buildTimestamp` fields, defaults `"0.0.0-dev"` / `"unknown"` / `"unknown"`.
2. Public property getters `Version` / `GitSha` / `BuildTimestamp`.
3. Editor-gated `WriteFields(string version, string gitSha, string buildTimestamp)` under `#if UNITY_EDITOR` — sets fields + `EditorUtility.SetDirty(this)`.
4. `[CreateAssetMenu(fileName = "BuildInfo", menuName = "Territory/BuildInfo")]` attribute populates Unity Assets → Create menu.
5. `npm run unity:compile-check` green.

### 2.2 Non-Goals (Out of Scope)

1. Creating the `.asset` instance — that is TECH-348.
2. Build pipeline wiring — Stage 1.2 ReleaseBuilder.
3. Runtime notifier consumer — Stage 2.3 UpdateNotifier.
4. Signing / notarization — unsigned tier locked per umbrella.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | As dev, want BuildInfo SO type so future ReleaseBuilder can stamp version metadata into a committed asset. | Type compiles + `[CreateAssetMenu]` registers. |

## 4. Current State

### 4.1 Domain behavior

No distribution runtime surface today. No version metadata embedded in Unity builds. Credits screen carries static placeholder text.

### 4.2 Systems map

| Path | Role | Status |
|---|---|---|
| `Assets/Scripts/Runtime/Distribution/BuildInfo.cs` | SO type authored here | new |
| `Assets/Resources/BuildInfo.asset` | Instance (TECH-348) | new, next task |
| `Assets/Editor/ReleaseBuilder.cs` | Writer via `WriteFields` (Stage 1.2) | future |
| `Assets/Scripts/UI/Distribution/UpdateNotifier.cs` | Reader via `Resources.Load` (Stage 2.3) | future |

Router domain: `Domain terms` (new glossary row in TECH-350 references this file). No gameplay / grid / road / water invariant touch — pure data SO.

## 5. Proposed Design

### 5.1 Target behavior (product)

None — developer-facing data model. No player-visible behavior in this task.

### 5.2 Architecture / implementation

```csharp
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Territory.Distribution
{
    [CreateAssetMenu(fileName = "BuildInfo", menuName = "Territory/BuildInfo")]
    public sealed class BuildInfo : ScriptableObject
    {
        [SerializeField] private string version = "0.0.0-dev";
        [SerializeField] private string gitSha = "unknown";
        [SerializeField] private string buildTimestamp = "unknown";

        public string Version => version;
        public string GitSha => gitSha;
        public string BuildTimestamp => buildTimestamp;

#if UNITY_EDITOR
        public void WriteFields(string newVersion, string newGitSha, string newTimestamp)
        {
            version = newVersion;
            gitSha = newGitSha;
            buildTimestamp = newTimestamp;
            EditorUtility.SetDirty(this);
        }
#endif
    }
}
```

Namespace `Territory.Distribution` isolates distribution surfaces. Sealed class — no inheritance needed.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-18 | Single SO w/ three string fields | Matches Design Expansion IP-3 verbatim; JSON-serializable; Inspector-friendly | Split into 3 SOs (over-granular); static constants (not editor-writable) |

## 7. Implementation Plan

### Phase 1 — Author BuildInfo SO

- [ ] Create folder `Assets/Scripts/Runtime/Distribution/` + `.meta`.
- [ ] Write `BuildInfo.cs` matching §5.2 skeleton.
- [ ] Run `npm run unity:compile-check` — green.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Compiles under Unity | Unity | `npm run unity:compile-check` | Requires `$UNITY_EDITOR_PATH` |
| Menu populates | Manual editor check | Unity Assets → Create → Territory → BuildInfo visible | Smoke only |

## 8. Acceptance Criteria

- [ ] `Assets/Scripts/Runtime/Distribution/BuildInfo.cs` compiles.
- [ ] `[CreateAssetMenu]` adds `Territory/BuildInfo` menu entry.
- [ ] `WriteFields` gated on `#if UNITY_EDITOR`.
- [ ] `npm run unity:compile-check` green.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| — | — | — | — |

## 10. Lessons Learned

- …

## Open Questions (resolve before / during implementation)

1. None — tooling / data-model only; behavior surfaces later stages (Stage 1.2 writer, Stage 2.3 reader).
