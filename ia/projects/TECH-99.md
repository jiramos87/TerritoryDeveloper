---
purpose: "TECH-99 — Headless SFX volume binding via PlayerPrefs in BlipBootstrap.Awake."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-99 — Headless SFX volume binding via PlayerPrefs in BlipBootstrap.Awake

> **Issue:** [TECH-99](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-13
> **Last updated:** 2026-04-13

## 1. Summary

`BlipBootstrap.Awake` reads `PlayerPrefs.GetFloat("BlipSfxVolumeDb", 0f)` and calls `BlipMixer.SetFloat("SfxVolume", db)`. No Settings UI in MVP — visible slider + mute toggle deferred post-MVP per `docs/blip-post-mvp-extensions.md` §4. Key string lives as `public const string` on `BlipBootstrap` so future UI can bind same key. Satisfies Stage 1.1 Exit criterion "SfxVolume bound headless".

## 2. Goals and Non-Goals

### 2.1 Goals

1. `BlipBootstrap` component exposes `PlayerPrefs` key string constant (e.g. `BlipSfxVolumeDb`).
2. `Awake` reads `PlayerPrefs.GetFloat(key, 0f)` + writes to `BlipMixer` via `SetFloat("SfxVolume", db)`.
3. `SetFloat` success logged once on boot (debug log — missing mixer ref raises single warning, not per-frame).

### 2.2 Non-Goals (Out of Scope)

1. No UI (slider / toggle / Settings panel).
2. No runtime re-bind on key change — value applied once at `Awake`.
3. No Save/Load hook — `PlayerPrefs` only.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | As a developer I want SFX volume bound at boot w/o UI so dev can iterate via `PlayerPrefs` write. | Set `PlayerPrefs BlipSfxVolumeDb = -10` → boot → master attenuation -10 dB. |

## 4. Current State

### 4.1 Domain behavior

`BlipBootstrap` prefab does not exist yet (lands TECH-100). `BlipMixer` asset lands TECH-98.

### 4.2 Systems map

- New code: `Assets/Scripts/Audio/Blip/BlipBootstrap.cs` (Awake binding; full prefab authoring in TECH-100 — this task adds the `Awake` logic + key constant).
- Depends on: TECH-98 (`BlipMixer` asset), TECH-100 (prefab + scene placement).
- `AudioMixer.SetFloat` API; `PlayerPrefs.GetFloat` API.

## 5. Proposed Design

### 5.2 Architecture / implementation

```
public class BlipBootstrap : MonoBehaviour {
    public const string SfxVolumeDbKey = "BlipSfxVolumeDb";
    public const string SfxVolumeParam = "SfxVolume";
    [SerializeField] private AudioMixer blipMixer;

    void Awake() {
        DontDestroyOnLoad(transform.root.gameObject); // TECH-100 adds this line
        float db = PlayerPrefs.GetFloat(SfxVolumeDbKey, 0f);
        if (blipMixer != null && blipMixer.SetFloat(SfxVolumeParam, db)) {
            Debug.Log($"[Blip] SfxVolume bound headless: {db} dB");
        } else {
            Debug.LogWarning("[Blip] BlipMixer ref missing or SetFloat failed");
        }
    }
}
```

Overlap w/ TECH-100 — merge carefully: TECH-99 owns `Awake` body + key constants; TECH-100 owns prefab + `DontDestroyOnLoad` + child slot wiring.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-13 | `PlayerPrefs` only (no Save-slot) | MVP scope; settings outlive save | ScriptableObject settings (post-MVP) |

## 7. Implementation Plan

### Phase 1 — Binding

- [ ] Add `BlipBootstrap.cs` w/ key constants + `AudioMixer` serialized ref.
- [ ] Implement `Awake` binding.
- [ ] Log success / warning once on boot.
- [ ] `npm run unity:compile-check` green.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Compile | Unity | `npm run unity:compile-check` | |
| Binding smoke | PlayMode | Deferred — tested in Step 2 PlayMode smoke | No EditMode harness MVP |

## 8. Acceptance Criteria

- [ ] `BlipBootstrap.cs` committed w/ key constants + `Awake` binding.
- [ ] `npm run unity:compile-check` green.
- [ ] `npm run validate:all` green.

## 10. Lessons Learned

- …

## Open Questions

None — headless binding only; UI deferred post-MVP.
