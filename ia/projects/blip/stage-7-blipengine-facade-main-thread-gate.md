### Stage 7 — Bake + facade + PlayMode smoke / BlipEngine facade + main-thread gate


**Status:** Done — 4 tasks archived (TECH-188..TECH-191) 2026-04-15

**Objectives:** Static `BlipEngine` facade lands. Stateless dispatch per invariant #4 — state lives on `BlipCatalog` / `BlipPlayer` MonoBehaviours; facade caches refs in static fields per invariant #3 (no `FindObjectOfType` on hot path). Main-thread assert gates all entry points. `Play` routes catalog → cooldown → baker → router → player.

**Exit:**

- `BlipEngine` static class at `Assets/Scripts/Audio/Blip/BlipEngine.cs` — `Play(BlipId, float pitchMult = 1f, float gainMult = 1f)` + `StopAll(BlipId)`.
- Main-thread assert at every entry point — compares `Thread.CurrentThread.ManagedThreadId` to cached main-thread id (captured in `BlipBootstrap.Awake`; new `BlipBootstrap.MainThreadId` static read-only accessor).
- `Bind(BlipCatalog)` / `Bind(BlipPlayer)` / `Unbind(*)` static setters consumed by Stage 2.2 hosts + lazy `FindObjectOfType` fallback on first call if not bound. Cached in static fields — no per-frame lookup.
- `Play` dispatch queries cooldown, bails silently when blocked; picks variant; bakes via `BlipBaker.BakeOrGet`; resolves mixer group; forwards to `BlipPlayer.PlayOneShot`.
- Phase 1 — Facade skeleton + main-thread assert + Bind/Unbind + cached lazy resolution.
- Phase 2 — Play + StopAll dispatch bodies through catalog → cooldown → baker → router → player.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T7.1 | Facade skeleton + main-thread gate | **TECH-188** | Done (archived) | New file `Assets/Scripts/Audio/Blip/BlipEngine.cs` — `public static class BlipEngine`. Declares `Play(BlipId id, float pitchMult = 1f, float gainMult = 1f)` + `StopAll(BlipId id)` w/ empty bodies for now. Private `AssertMainThread()` helper compares `Thread.CurrentThread.ManagedThreadId` to cached `BlipBootstrap.MainThreadId` (new static read-only prop set in `BlipBootstrap.Awake` → `Thread.CurrentThread.ManagedThreadId`). Throws `InvalidOperationException` w/ diagnostic message on mismatch. Invoked first line of every entry point. |
| T7.2 | Bind/Unbind + cached lazy resolution | **TECH-189** | Done (archived) | In `BlipEngine`: `static BlipCatalog _catalog; static BlipPlayer _player;`. `Bind(BlipCatalog c)` / `Bind(BlipPlayer p)` setters (null-safe overwrite). `Unbind(BlipCatalog)` / `Unbind(BlipPlayer)` nullers. Private `ResolveCatalog() → BlipCatalog` / `ResolvePlayer() → BlipPlayer` — return cached field if non-null, else `FindObjectOfType<BlipCatalog>()` / `FindObjectOfType<BlipPlayer>()` fallback + cache (invariant #3 — one-time lookup, not per-frame). Consumed by `BlipCatalog.Awake` (T2.2.2) + `BlipPlayer.Awake` (T2.2.5). |
| T7.3 | Play dispatch body | **TECH-190** | Done (archived) | `BlipEngine.Play(BlipId id, float pitchMult, float gainMult)` body: `AssertMainThread()` → `var cat = ResolveCatalog(); if (cat == null \ | \ | !cat.IsReady) return;` → `var nowDsp = AudioSettings.dspTime; ref readonly var patch = ref cat.Resolve(id); if (!cat.CooldownRegistry.TryConsume(id, nowDsp, patch.cooldownMs)) return;` → variant index = deterministic (fixed 0) if `patch.deterministic` else xorshift on per-id RNG state held on catalog → `AudioClip clip = cat.Baker.BakeOrGet(in patch, variantIndex);` → `AudioMixerGroup group = cat.MixerRouter.Get(id);` → `ResolvePlayer().PlayOneShot(clip, pitchMult, gainMult, group);`. Expose `cat.IsReady`, `cat.CooldownRegistry`, `cat.Baker`, `cat.MixerRouter` internals via `internal` props on `BlipCatalog`. |
| T7.4 | StopAll dispatch body | **TECH-191** (archived) | Done (archived) | `BlipEngine.StopAll(BlipId id)` body: `AssertMainThread()` → resolve catalog + player → query `cat.Baker` for all cached `AudioClip` refs matching `(patchHash, *)` for this `id` (expose `BlipBaker.EnumerateClipsForPatchHash(int patchHash) → IEnumerable<AudioClip>` helper). Iterate `BlipPlayer._pool`; call `source.Stop()` where `source.clip` matches any enumerated clip. Non-destructive — does not evict baked clips from cache. |

#### §Stage File Plan

> Opus `stage-file-plan` writes structured `{operation, target_path, target_anchor, payload}` tuples here per pending Task. Sonnet `stage-file-apply` reads tuples and materializes BACKLOG rows + spec stubs. Contract: `ia/rules/plan-apply-pair-contract.md`.

_pending — populated by `/stage-file` planner pass._

#### §Plan Fix

> Opus `plan-review` writes targeted fix tuples here when a Stage's Task specs need tightening before first `/implement`. Sonnet `plan-applier` Mode plan-fix reads tuples and applies edits. Contract: `ia/rules/plan-apply-pair-contract.md`.

_pending — populated by `/plan-review` when fixes are needed._

#### §Stage Audit

> Opus `opus-audit` writes one `§Audit` paragraph per Task row here (Stage-scoped bulk, non-pair) once every Task reaches Done post-verify. Feeds `§Stage Closeout Plan` migration tuples downstream. Contract: `ia/rules/plan-apply-pair-contract.md` Stage-scoped non-pair row.

_retroactive-skip — Stage archived prior to 2026-04-24 lifecycle refactor that introduced the canonical `§Stage Audit` subsection (see `docs/MASTER-PLAN-STRUCTURE.md` §3.4 + Changelog entry 2026-04-24). Task-level §Audit prose captured in per-Task specs during Stage-scoped closeout before spec deletion; no retroactive re-run needed._

#### §Stage Closeout Plan

> Opus `stage-closeout-plan` writes unified tuple list here ONCE per Stage when all Task rows reach `Done` post-verify. Sonnet `stage-closeout-apply` reads tuples and applies verbatim. Contract: `ia/rules/plan-apply-pair-contract.md` seam #4.

_pending — populated by `/closeout {{this-doc}} Stage {{N.M}}` planner pass when all Tasks reach `Done`._

---
