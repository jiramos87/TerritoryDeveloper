---
purpose: "TECH-245 — BlipBootstrap SfxMutedKey constant + boot-time mute restore."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-245 — BlipBootstrap SfxMutedKey constant + boot-time mute restore

> **Issue:** [TECH-245](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-16
> **Last updated:** 2026-04-16

## 1. Summary

Add `public const string SfxMutedKey = "BlipSfxMuted";` to `BlipBootstrap.cs` (after `SfxVolumeDbDefault` constant). Extend `BlipBootstrap.Awake` to read `PlayerPrefs.GetInt(SfxMutedKey, 0)` and clamp `db = -80f` before `blipMixer.SetFloat(SfxVolumeParam, db)` when muted. Ensures mute state persists across app launches even before `BlipVolumeController.OnEnable` fires — cold-start player with `BlipSfxMuted = 1` gets silent audio from first sample, not after opening Options. Satisfies Stage 4.2 Exit bullet "`BlipBootstrap.cs` — new `public const string SfxMutedKey` constant; `Awake` reads `PlayerPrefs.GetInt(SfxMutedKey, 0)` after volume read; if muted, overrides `db = -80f` before `blipMixer.SetFloat`".

## 2. Goals and Non-Goals

### 2.1 Goals

1. `BlipBootstrap.cs` gains `public const string SfxMutedKey = "BlipSfxMuted";` after `SfxVolumeDbDefault` (line ~32).
2. `BlipBootstrap.Awake` reads `PlayerPrefs.GetInt(SfxMutedKey, 0)` after `PlayerPrefs.GetFloat(SfxVolumeDbKey, ...)`; on `muted != 0` overrides `db = -80f` before `blipMixer.SetFloat(SfxVolumeParam, db)` call.
3. Cold-start semantics: player who muted in prior session hears silence from first Blip play in new session (no unmuted window before Options opens).
4. `npm run unity:compile-check` green.

### 2.2 Non-Goals

1. Volume UI controller logic — TECH-243 + TECH-244 own (both archived).
2. Glossary row update — TECH-246 owns.
3. Migration path for existing `PlayerPrefs` (no key = default unmuted = legacy behavior preserved).
4. Event-driven runtime sync between `BlipBootstrap` + `BlipVolumeController` — static constant lookup suffices (both read `PlayerPrefs` directly).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Player | As player who muted SFX last session, new session boots silent — no click burst before I can open Options | First Blip play after launch respects stored mute state |
| 2 | Developer | As audio maintainer, `SfxMutedKey` constant single-source referenced by both bootstrap + controller | Both files import via `BlipBootstrap.SfxMutedKey`, no string literals |

## 4. Current State

### 4.1 Domain behavior

Stage 4.1 shipped `BlipBootstrap.Awake` reading only `SfxVolumeDbKey`. Mute toggle UI exists (Stage 4.1 T4.1.2 `SfxMuteToggle`) but no persistence read path — first-frame audio plays at stored dB regardless of last-session mute state. Cold-start click-burst risk until Options opened + `BlipVolumeController.OnEnable` primes toggle + applies mute.

### 4.2 Systems map

- `Assets/Scripts/Audio/Blip/BlipBootstrap.cs` — add constant + 2-line `Awake` extension.
- `Assets/Scripts/Audio/Blip/BlipVolumeController.cs` — consumer via `BlipBootstrap.SfxMutedKey` (TECH-243, TECH-244 archived — read this constant).
- `ia/specs/audio-blip.md §5.1`, `§5.2` — bootstrap lifecycle cross-ref; no edit.
- Invariants: #3 (`Awake` only — no per-frame read), #4 (no new singleton — static `const` is OK, `BlipBootstrap` already MonoBehaviour).

## 5. Proposed Design

### 5.2 Architecture / implementation

**Constant add (`BlipBootstrap.cs` ~line 32):**

```csharp
// Public constants — future Settings UI binds same keys without duplication.
public const string SfxVolumeDbKey = "BlipSfxVolumeDb";
public const string SfxVolumeParam = "SfxVolume";
public const float SfxVolumeDbDefault = 0f;
public const string SfxMutedKey = "BlipSfxMuted";  // ← new
```

**`Awake` extension (after existing `float db = PlayerPrefs.GetFloat(SfxVolumeDbKey, SfxVolumeDbDefault)`):**

```csharp
float db = PlayerPrefs.GetFloat(SfxVolumeDbKey, SfxVolumeDbDefault);

// Boot-time mute restore — clamps dB ahead of mixer apply if persisted mute = 1.
// Cold-start guarantee: muted state honored from first Blip play, not only after Options opens.
int muted = PlayerPrefs.GetInt(SfxMutedKey, 0);
if (muted != 0)
{
    db = -80f;
}

if (blipMixer == null) { /* ... unchanged warn + return ... */ }

if (blipMixer.SetFloat(SfxVolumeParam, db)) { /* ... unchanged ... */ }
```

**Log line adjustment:** existing `Debug.Log($"[Blip] SfxVolume bound headless: {db} dB")` naturally reflects `-80` when muted — no additional log needed.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-16 | `SfxMutedKey` `public const` on `BlipBootstrap` (not `BlipVolumeController`) | Bootstrap loads first; consumer controller already reads `SfxVolumeDbKey` from bootstrap — same owner pattern | `BlipVolumeController.SfxMutedKey` — rejected, reverses read dep direction (bootstrap would need to know controller type) |
| 2026-04-16 | Clamp at `-80 dB` (not mute via `SetParameterValueAtTime` or mixer snapshot) | Matches Stage 4.1 T4.1.4 + Stage 4.2 Phase 1 semantics — single dB value on `SfxVolume` param | Mixer snapshot swap — rejected, over-engineered for single param |

## 7. Implementation Plan

### Phase 1 — Constant + Awake extension

- [ ] Open `Assets/Scripts/Audio/Blip/BlipBootstrap.cs`.
- [ ] Add `public const string SfxMutedKey = "BlipSfxMuted";` after `SfxVolumeDbDefault` decl.
- [ ] In `Awake`, after `float db = PlayerPrefs.GetFloat(SfxVolumeDbKey, SfxVolumeDbDefault);` (current line 57), insert `int muted = PlayerPrefs.GetInt(SfxMutedKey, 0); if (muted != 0) db = -80f;`.
- [ ] Run `npm run unity:compile-check` — verify TECH-243 + TECH-244 (both archived) compile cleanly against new constant.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|---|---|---|---|
| Compile clean | Unity compile | `npm run unity:compile-check` | |
| Full chain green | Node | `npm run validate:all` | repo root |
| Manual: set `BlipSfxMuted = 1` via editor, relaunch → first Blip silent | Manual / Play Mode | PlayerPrefs editor tool + MainMenu play test | |

## 8. Acceptance Criteria

- [ ] `BlipBootstrap.cs` contains `public const string SfxMutedKey = "BlipSfxMuted";`.
- [ ] `BlipBootstrap.Awake` reads `PlayerPrefs.GetInt(SfxMutedKey, 0)` after `SfxVolumeDbKey` read.
- [ ] Muted path clamps `db = -80f` before `blipMixer.SetFloat(SfxVolumeParam, db)`.
- [ ] Cold-start with `BlipSfxMuted = 1` → mixer bound at `-80 dB` from `Awake`.
- [ ] `npm run unity:compile-check` exit 0.

## 10. Cross-refs

- Parent: [`blip-master-plan.md`](blip-master-plan.md) Step 4 Stage 4.2 Phase 2.
- Depends on: TECH-235..TECH-238 (archived — Stage 4.1 scaffolding landed `BlipMixer` accessor).
- Sibling: TECH-243 + TECH-244 (both archived) — consumers of `SfxMutedKey` constant (land order coordinated so TECH-245 commits first or same commit as siblings to avoid compile break).
- [`TECH-246.md`](TECH-246.md) — glossary row update reflecting boot-time mute restore.

## Open Questions

None — tooling-adjacent; behavior locked by Stage 4.2 Exit bullets.
