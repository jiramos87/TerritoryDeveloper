---
purpose: "TECH-319 — Music — MusicBootstrap.Awake shape."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-319 — Music — MusicBootstrap.Awake shape

> **Issue:** [TECH-319](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-17
> **Last updated:** 2026-04-17

## 1. Summary

Complete `MusicBootstrap : MonoBehaviour` — add `Instance` static accessor + `[SerializeField] private AudioMixer blipMixer` + `Awake` body (`DontDestroyOnLoad` + Instance set + PlayerPrefs read + 2 mixer `SetFloat` calls) + `OnDestroy` Instance clear. Pattern mirror of `BlipBootstrap.cs` L15-85. Satisfies invariant #4 (Inspector-placed MB, not `new`) + invariant #3 (no `FindObjectOfType` in per-frame loops — Awake-only).

## 2. Goals and Non-Goals

### 2.1 Goals

1. `public static MusicBootstrap Instance { get; private set; }` accessor.
2. `[SerializeField] private AudioMixer blipMixer` field.
3. `Awake` body: `DontDestroyOnLoad(transform.root.gameObject)` + `Instance = this` + PlayerPrefs reads (`MasterVolumeDb`, `MusicVolumeDb` w/ defaults 0f) + `blipMixer.SetFloat` twice + null-mixer warn.
4. `OnDestroy` clears `Instance` if match.
5. Invariant #4 satisfied (Inspector-placed MB).

### 2.2 Non-Goals

1. Prefab creation (→ TECH-320).
2. Scene placement (→ TECH-321).
3. Playlist load + `MusicPlayer` handoff (→ Stage 2.1).

## 4. Current State

### 4.2 Systems map

- `Assets/Scripts/Audio/Music/MusicBootstrap.cs` — extended by TECH-318 (consts); this task adds fields + Awake body.
- `Assets/Scripts/Audio/Blip/BlipBootstrap.cs` L15-85 — pattern mirror (Instance accessor, Awake shape, null-mixer warn, OnDestroy clear).
- `ia/specs/unity-development-context.md §3` — `[SerializeField] private` pattern.
- `ia/specs/unity-development-context.md §6` — `DontDestroyOnLoad` Awake timing + SEO caveats.
- `ia/rules/invariants.md` #3 + #4.

## 5. Proposed Design

### 5.2 Architecture / implementation

```csharp
public static MusicBootstrap Instance { get; private set; }

[SerializeField] private AudioMixer blipMixer;

private void Awake()
{
    DontDestroyOnLoad(transform.root.gameObject);
    Instance = this;

    float master = PlayerPrefs.GetFloat(MasterVolumeDbKey, MasterVolumeDbDefault);
    float music  = PlayerPrefs.GetFloat(MusicVolumeDbKey, MusicVolumeDbDefault);

    if (blipMixer == null)
    {
        Debug.LogWarning("[Music] MusicBootstrap: blipMixer ref missing — MasterVolume + MusicVolume not bound");
        return;
    }

    blipMixer.SetFloat(MasterVolumeParam, master);
    blipMixer.SetFloat(MusicVolumeParam, music);
}

private void OnDestroy()
{
    if (Instance == this) Instance = null;
}
```

## 7. Implementation Plan

### Phase 1 — Extend MusicBootstrap

- [ ] Add `Instance` static accessor.
- [ ] Add `[SerializeField] private AudioMixer blipMixer` field.
- [ ] Author `Awake` body per design §5.2.
- [ ] Author `OnDestroy` clear.
- [ ] `npm run unity:compile-check` green.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Awake binds mixer headless | Node | `npm run unity:compile-check` | Runtime smoke deferred to TECH-321 scene placement |

## 8. Acceptance Criteria

- [ ] `Instance` static accessor present.
- [ ] `Awake` calls `DontDestroyOnLoad` + sets `Instance` + reads 2 PlayerPrefs + calls 2 `SetFloat`.
- [ ] Null-mixer warn present (mirror BlipBootstrap L67).
- [ ] `OnDestroy` clears `Instance` if match.
- [ ] Invariant #4 satisfied (MB Inspector-placed, not `new`).
- [ ] `npm run unity:compile-check` green.

## Open Questions

1. None — pattern-mirror authoring.
