---
purpose: "TECH-318 — Music — Author MusicBootstrap constants."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-318 — Music — Author MusicBootstrap constants

> **Issue:** [TECH-318](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-17
> **Last updated:** 2026-04-17

## 1. Summary

Author public const string + float fields on `MusicBootstrap` class at `Assets/Scripts/Audio/Music/MusicBootstrap.cs`. Downstream consumers (mixer binding in TECH-319, Settings sliders Stage 3.1, first-run toast Stage 3.3) reference these constants — single source of truth for PlayerPrefs keys + mixer param names. Mirrors `BlipBootstrap` constants pattern (L30-33).

## 2. Goals and Non-Goals

### 2.1 Goals

1. 7 `public const string` fields: `MasterVolumeDbKey`, `MasterVolumeParam`, `MusicVolumeDbKey`, `MusicVolumeParam`, `MusicLastTrackIdKey`, `MusicEnabledKey`, `MusicFirstRunDoneKey`.
2. 2 `public const float` fields: `MasterVolumeDbDefault = 0f`, `MusicVolumeDbDefault = 0f`.
3. No re-declaration of Blip's `SfxVolumeDbKey` / `SfxVolumeParam` (consumed from `BlipBootstrap` where needed).

### 2.2 Non-Goals (Out of Scope)

1. `Awake` body + mixer binding (→ TECH-319).
2. Prefab creation (→ TECH-320).

## 4. Current State

### 4.2 Systems map

- `Assets/Scripts/Audio/Music/MusicBootstrap.cs` **(new)** — constants hosted here.
- `Assets/Scripts/Audio/Blip/BlipBootstrap.cs` L30-33 — pattern mirror (`SfxVolumeDbKey`, `SfxVolumeParam`, `SfxVolumeDbDefault`).
- `ia/specs/unity-development-context.md §3` — `[SerializeField] private` + Inspector wiring (relevant to subsequent tasks).

## 5. Proposed Design

### 5.2 Architecture / implementation

Class skeleton:

```csharp
public class MusicBootstrap : MonoBehaviour
{
    public const string MasterVolumeDbKey = "MasterVolumeDb";
    public const string MasterVolumeParam = "MasterVolume";
    public const string MusicVolumeDbKey = "MusicVolumeDb";
    public const string MusicVolumeParam = "MusicVolume";
    public const string MusicLastTrackIdKey = "MusicLastTrackId";
    public const string MusicEnabledKey = "MusicEnabled";
    public const string MusicFirstRunDoneKey = "MusicFirstRunDone";
    public const float MasterVolumeDbDefault = 0f;
    public const float MusicVolumeDbDefault = 0f;
    // Awake + fields → TECH-319
}
```

## 7. Implementation Plan

### Phase 1 — Author consts

- [ ] Create `Assets/Scripts/Audio/Music/MusicBootstrap.cs` w/ class skeleton + 9 const fields.
- [ ] Verify no collision w/ `BlipBootstrap.SfxVolumeDbKey` / `SfxVolumeParam`.
- [ ] `npm run unity:compile-check` green.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| C# compiles | Node | `npm run unity:compile-check` | |

## 8. Acceptance Criteria

- [ ] 7 string consts + 2 float consts present on `MusicBootstrap`.
- [ ] No re-declaration of Blip `SfxVolume*` keys.
- [ ] `npm run unity:compile-check` green.

## Open Questions

1. None — structural authoring only.
