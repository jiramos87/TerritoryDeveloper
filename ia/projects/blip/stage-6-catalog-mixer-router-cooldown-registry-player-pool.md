### Stage 6 — Bake + facade + PlayMode smoke / Catalog + mixer router + cooldown registry + player pool


**Status:** Done (6 tasks archived 2026-04-15 — **TECH-169**..**TECH-174**)

**Objectives:** MonoBehaviour hosts + plain-class services wire together under `BlipBootstrap`. `BlipCatalog` flattens authoring SOs, owns `BlipBaker` + `BlipMixerRouter` + `BlipCooldownRegistry` as instance fields (invariant #4 — no new singletons). `BlipPlayer` exposes 16-source pool. No static facade yet; `BlipEngine.Bind` callbacks reserved for Stage 2.3.

**Exit:**

- `BlipPatchEntry` serializable struct at `Assets/Scripts/Audio/Blip/BlipPatchEntry.cs` — `public BlipId id; public BlipPatch patch;`.
- `BlipCatalog : MonoBehaviour` at `Assets/Scripts/Audio/Blip/BlipCatalog.cs` — `SerializeField BlipPatchEntry[] entries`, flattens to `BlipPatchFlat[]`, builds `Dictionary<BlipId, int>` index, owns `BlipBaker` + `BlipMixerRouter` + `BlipCooldownRegistry` instance fields, `Resolve(BlipId) → ref readonly BlipPatchFlat`. Ready flag set last in `Awake`.
- `BlipMixerRouter` plain class at `Assets/Scripts/Audio/Blip/BlipMixerRouter.cs` — `Get(BlipId) → AudioMixerGroup`, built from authoring-only `BlipPatch.mixerGroup` refs.
- `BlipCooldownRegistry` plain class at `Assets/Scripts/Audio/Blip/BlipCooldownRegistry.cs` — `TryConsume(BlipId, double nowDspTime, double cooldownMs) → bool`.
- `BlipPlayer : MonoBehaviour` at `Assets/Scripts/Audio/Blip/BlipPlayer.cs` — child of `BlipBootstrap`, 16-source pool + round-robin `PlayOneShot(AudioClip, float pitch, float gain, AudioMixerGroup)`.
- Phase 1 — `BlipPatchEntry` + `BlipCatalog` flatten + resolve + ready flag.
- Phase 2 — `BlipMixerRouter` + `BlipCooldownRegistry` plain-class services owned by catalog.
- Phase 3 — `BlipPlayer` 16-source pool + round-robin `PlayOneShot`.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T6.1 | BlipPatchEntry + catalog flatten | **TECH-169** | Done (archived) | New files `Assets/Scripts/Audio/Blip/BlipPatchEntry.cs` (`[Serializable] public struct BlipPatchEntry { public BlipId id; public BlipPatch patch; }`) + `BlipCatalog.cs` (`sealed : MonoBehaviour`). `[SerializeField] private BlipPatchEntry[] entries`. `Awake` iterates `entries`, builds parallel `BlipPatchFlat[] _flat` via `BlipPatchFlat.FromSO(entry.patch)` (Stage 1.2 helper) + `Dictionary<BlipId, int> _indexById`. Throws `InvalidOperationException` w/ index + id on duplicate `BlipId` or null patch ref. |
| T6.2 | Catalog Resolve + ready flag + Engine bind | **TECH-170** | Done (archived) | `BlipCatalog.Resolve(BlipId id) → ref readonly BlipPatchFlat` via `_indexById` lookup (throws on unknown id). `bool isReady` private field set to `true` as the last statement in `Awake` — scene-load suppression contract per Stage 1.1 T1.1.4. Calls `BlipEngine.Bind(this)` (method added Stage 2.3 T2.3.2 — declare stub signature here; null-safe). `OnDestroy` → `BlipEngine.Unbind(this)` stub. |
| T6.3 | BlipMixerRouter plain class | **TECH-171** | Done (archived) | New file `Assets/Scripts/Audio/Blip/BlipMixerRouter.cs`. `public sealed class BlipMixerRouter` plain class. Ctor takes `BlipPatchEntry[] entries` + builds `Dictionary<BlipId, AudioMixerGroup> _map` reading authoring-only `entry.patch.mixerGroup` ref (NOT in `BlipPatchFlat` — Stage 1.2 T1.2.4 Decision Log). `Get(BlipId) → AudioMixerGroup` lookup (throws on unknown id). Instantiated in `BlipCatalog.Awake` + held as instance field `_mixerRouter`. |
| T6.4 | BlipCooldownRegistry plain class | **TECH-172** | Done (archived) | New file `Assets/Scripts/Audio/Blip/BlipCooldownRegistry.cs`. `public sealed class BlipCooldownRegistry` plain class. `Dictionary<BlipId, double> _lastPlayDspTime`. `TryConsume(BlipId id, double nowDspTime, double cooldownMs) → bool` — if `!_lastPlayDspTime.TryGetValue(id, out var last) |  | (nowDspTime - last) * 1000.0 >= cooldownMs` → write `_lastPlayDspTime[id] = nowDspTime` + return `true`; else return `false`. Instantiated in `BlipCatalog.Awake` + held as instance field `_cooldownRegistry`. |
| T6.5 | BlipPlayer pool construction | **TECH-173** | Done (archived) | New file `Assets/Scripts/Audio/Blip/BlipPlayer.cs` (`: MonoBehaviour`). `[SerializeField] private int poolSize = 16`. `Awake` instantiates `poolSize` child GameObjects (`new GameObject("BlipVoice_0".."BlipVoice_15")`) parented under this transform, each with `AudioSource` component (`playOnAwake = false`, `loop = false`). Holds `AudioSource[] _pool` + `int _cursor = 0`. Placed as child of `BlipBootstrap` prefab. Calls `BlipEngine.Bind(this)` at end of `Awake`. |
| T6.6 | BlipPlayer PlayOneShot dispatch | **TECH-174** | Done (archived) | `BlipPlayer.PlayOneShot(AudioClip clip, float pitch, float gain, AudioMixerGroup group)` — selects `var source = _pool[_cursor]; _cursor = (_cursor + 1) % _pool.Length;`, stops prior clip if still playing (voice-steal overwrite — no crossfade, post-MVP per orchestration guardrails), sets `source.clip = clip; source.pitch = pitch; source.volume = gain; source.outputAudioMixerGroup = group;` then `source.Play()`. |

#### §Stage File Plan

> Opus `stage-file-plan` writes structured `{operation, target_path, target_anchor, payload}` tuples here per pending Task. Sonnet `stage-file-apply` reads tuples and materializes BACKLOG rows + spec stubs. Contract: `ia/rules/plan-apply-pair-contract.md`.

_pending — populated by `/stage-file` planner pass._

#### §Plan Fix

> Opus `plan-review` writes targeted fix tuples here when a Stage's Task specs need tightening before first `/implement`. Sonnet `plan-applier` Mode plan-fix reads tuples and applies edits. Contract: `ia/rules/plan-apply-pair-contract.md`.

_pending — populated by `/plan-review` when fixes are needed._

#### §Stage Audit

> Opus `opus-audit` writes one `§Audit` paragraph per Task row here (Stage-scoped bulk, non-pair) once every Task reaches Done post-verify. Feeds `§Stage Closeout Plan` migration tuples downstream. Contract: `ia/rules/plan-apply-pair-contract.md` Stage-scoped non-pair row.

_retroactive-skip — Stage archived prior to 2026-04-24 lifecycle refactor that introduced the canonical `§Stage Audit` subsection (see `ia/projects/MASTER-PLAN-STRUCTURE.md` §3.4 + Changelog entry 2026-04-24). Task-level §Audit prose captured in per-Task specs during Stage-scoped closeout before spec deletion; no retroactive re-run needed._

#### §Stage Closeout Plan

> Opus `stage-closeout-plan` writes unified tuple list here ONCE per Stage when all Task rows reach `Done` post-verify. Sonnet `stage-closeout-apply` reads tuples and applies verbatim. Contract: `ia/rules/plan-apply-pair-contract.md` seam #4.

_pending — populated by `/closeout {{this-doc}} Stage {{N.M}}` planner pass when all Tasks reach `Done`._

---
