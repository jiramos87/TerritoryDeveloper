### Stage 5 — Bake + facade + PlayMode smoke / Bake-to-clip pipeline


**Status:** Done — TECH-159 / TECH-160 / TECH-161 / TECH-162 Done (archived) 2026-04-15

**Objectives:** `BlipBaker` plain class ships. Renders `BlipPatchFlat` through `BlipVoice.Render` into `float[]` then wraps via `AudioClip.Create` + `AudioClip.SetData`. LRU cache keyed by `(patchHash, variantIndex)` with 4 MB default memory budget + eviction on overflow. Main-thread only; no MonoBehaviour. Consumed by `BlipCatalog` (Stage 2.2) + `BlipEngine.Play` (Stage 2.3).

**Exit:**

- `BlipBaker` plain class at `Assets/Scripts/Audio/Blip/BlipBaker.cs` — `BakeOrGet(in BlipPatchFlat patch, int variantIndex) → AudioClip`.
- Cache hit path: O(1) `Dictionary<BlipBakeKey, LinkedListNode<BlipBakeEntry>>` lookup + LRU-tail promote.
- Cache miss path: render → `AudioClip.Create(name, lengthSamples, 1, sampleRate, stream: false)` + `SetData(buffer, 0)` → insert at tail + evict head until under budget.
- Memory budget enforced (default 4 MB via ctor param `long budgetBytes = 4 * 1024 * 1024`). Evicted clips destroyed via `UnityEngine.Object.Destroy(clip)`.
- Phase 1 — Baker core + bake key + cache hit/miss dispatch.
- Phase 2 — LRU eviction + memory budget accounting.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T5.1 | BlipBaker core + render path | **TECH-159** | Done (archived) | New file `Assets/Scripts/Audio/Blip/BlipBaker.cs`. Plain class (not MonoBehaviour). `BakeOrGet(in BlipPatchFlat patch, int patchHash, int variantIndex) → AudioClip`. `sampleRate` is baker ctor param (default `AudioSettings.outputSampleRate`) — not per-call, not a flat field. `patchHash` passed per-call (flat struct defers hash per Stage 1.2 source line 162). Main-thread assert at entry via `BlipBootstrap.MainThreadId` — TECH-159 lands the minimal static prop + `Awake` capture (T2.3.1 reuses). Computes `lengthSamples = (int)(patch.durationSeconds * _sampleRate)`, allocates `float[lengthSamples]`, initializes `BlipVoiceState` default + calls `BlipVoice.Render(buffer, 0, lengthSamples, _sampleRate, in patch, variantIndex, ref state)`, wraps via `AudioClip.Create(name, lengthSamples, 1, _sampleRate, stream: false)` + `clip.SetData(buffer, 0)`. |
| T5.2 | Bake key + cache hit dispatch | **TECH-160** | Done (archived) | New file `Assets/Scripts/Audio/Blip/BlipBakeKey.cs` — `readonly struct BlipBakeKey(int patchHash, int variantIndex)` w/ `IEquatable<BlipBakeKey>` + hash combine. In `BlipBaker`: `Dictionary<BlipBakeKey, LinkedListNode<BlipBakeEntry>> _index` + `LinkedList<BlipBakeEntry> _lru`. `BakeOrGet` first probes `_index`; hit → move node to tail + return cached `AudioClip`; miss → invoke render path (T2.1.1) + handoff to Phase 2 eviction. |
| T5.3 | LRU ordering + access tracking | **TECH-161** (archived) | Done (archived) | `BlipBakeEntry` private nested class/struct holding `BlipBakeKey key`, `AudioClip clip`, `long byteCount`. `_lru` access order: newest at tail, oldest at head. Hit → `_lru.Remove(node); _lru.AddLast(node)`. Miss insert → `_lru.AddLast(entry)` after render. Unit-test-able helper `TryEvictHead() → bool` for Phase 2 budget loop. |
| T5.4 | Memory budget + eviction loop | **TECH-162** (archived) | Done (archived) | Ctor param `long budgetBytes = 4L * 1024 * 1024`. Track `_totalBytes` running sum. Each entry `byteCount = lengthSamples * sizeof(float)`. On insert, loop: while `_totalBytes + newByteCount > budgetBytes && _lru.First != null` → pop head, `UnityEngine.Object.Destroy(evicted.clip)`, subtract `evicted.byteCount` from `_totalBytes`, remove from `_index`. Then add new entry + `_totalBytes += newByteCount`. |

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
