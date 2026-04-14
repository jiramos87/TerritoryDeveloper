---
purpose: "TECH-115 — patchHash content hash on BlipPatch + glossary rows."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-115 — patchHash content hash on BlipPatch + glossary rows

> **Issue:** [TECH-115](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-14
> **Last updated:** 2026-04-14

## 1. Summary

Content-hash digest over serialized scalar fields of `BlipPatch` (osc freqs, env timings, env shapes, filter cutoff, jitter values, cooldown). Stable across Unity GUID churn + version bumps. `[SerializeField] private int patchHash` persisted on `OnValidate`; `Awake` (or first flatten) recomputes + asserts match (log warning on mismatch). Adds glossary rows for **Blip patch**, **Blip patch flat**, **patch hash**. Closes Stage 1.2 Phase 2 + Stage 1.2 exit criterion on hash + glossary.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Content-hash function — FNV-1a / xxhash64 digest over `BlipPatch` scalar fields.
2. Persist hash in `[SerializeField] private int patchHash`.
3. Recompute on `OnValidate` (author-time).
4. Verify on `Awake` / first flatten (runtime); log warning on mismatch.
5. Glossary rows — **Blip patch**, **Blip patch flat**, **patch hash** (under Audio category).

### 2.2 Non-Goals

1. Cache keying in `BlipBaker` (Step 2; consumer of hash).
2. Save-data hash (hash is authoring-side only).
3. Bit-exact cross-version hash (MVP tolerates structural drift — document post-MVP stabilization in `docs/blip-post-mvp-extensions.md` if needed).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | Patch edits cause hash change | `OnValidate` produces new hash when scalar field changes; identical fields → identical hash across sessions. |
| 2 | Developer | Glossary covers new terms | `glossary_lookup` returns **Blip patch** / **Blip patch flat** / **patch hash**. |

## 4. Current State

### 4.2 Systems map

- Same file as T1.2.1 `BlipPatch.cs` (or `BlipPatchHash.cs` helper).
- Glossary edit: `ia/specs/glossary.md` — 3 rows under Audio category (peers of `Blip bootstrap` / `Blip mixer group`).
- MCP index regenerated via `npm run validate:all` chain.

## 5. Proposed Design

### 5.1 Target behavior

Author-time hash stable; runtime assert guards against editor serialization bugs. Mismatch warning only (no crash) — allows dev to save-fix.

### 5.2 Architecture / implementation

FNV-1a 32-bit over bytewise reinterpret of each scalar (endianness fixed). Field order: oscillators[0..3] (waveform + freq + detune + duty + gain), envelope timings + shapes + sustain, filter kind + cutoff, jitter triplet, cooldownMs, variantCount, voiceLimit, priority, durationSeconds, deterministic, useLutOscillators.

## 7. Implementation Plan

### Phase 1 — Hash function

- [ ] Implement FNV-1a digest helper.
- [ ] `ComputeHash()` method on `BlipPatch`.
- [ ] Persist `[SerializeField] private int patchHash`.
- [ ] Write in `OnValidate` after clamp.
- [ ] Assert in `Awake` / first flatten w/ `Debug.LogWarning` on mismatch.

### Phase 2 — Glossary

- [ ] Add **Blip patch** row (Audio category).
- [ ] Add **Blip patch flat** row.
- [ ] Add **patch hash** row.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Hash stable across sessions | Unity compile + EditMode | `npm run unity:compile-check` + Stage 1.4 determinism test (T1.4.6) | Hash feeds cache key Step 2 — deeper coverage there. |
| Glossary rows validate | Node | `npm run validate:all` | Chains `generate:ia-indexes --check`. |

## 8. Acceptance Criteria

- [ ] `patchHash` recomputes on `OnValidate`; identical fields → identical hash.
- [ ] `Awake` assert logs warning on mismatch.
- [ ] 3 glossary rows land (Blip patch / Blip patch flat / patch hash).
- [ ] `validate:all` + `unity:compile-check` green.

## Open Questions

1. Hash algo — FNV-1a sufficient MVP? (xxhash64 faster but adds dep). Decide at kickoff; default FNV-1a unless dep already present.
