### Stage 12 — Patches + integration + golden fixtures + promotion / Golden fixtures + spec promotion + glossary


**Status:** Done (closed 2026-04-16) — all tasks archived (TECH-227..TECH-230)

**Objectives:** Fixture harness gates DSP output regression. Exploration doc promoted to canonical spec. Glossary rows complete + cross-referenced to spec. After this stage Blip subsystem fully shipped + regression-gated.

**Exit:**

- `tools/fixtures/blip/` dir + 10 JSON fixture files (one per MVP `BlipId`, variant 0). Each: `{ "id": "<BlipId>", "variant": 0, "patchHash": <int>, "sampleRate": 44100, "sampleCount": <int>, "sumAbsHash": <double>, "zeroCrossings": <int> }`.
- `tools/scripts/blip-bake-fixtures.ts` (dev-only) — bakes each patch via `BlipVoice.Render` logic (TS port or Unity batchmode shim) + writes fixture JSONs. CI does NOT run this script; CI runs regression test only.
- `Assets/Tests/EditMode/Audio/BlipGoldenFixtureTests.cs` in `Blip.Tests.EditMode.asmdef` (Stage 1.4 asmdef — no new asmdef). One `[Test]` per `BlipId`: parse fixture JSON, re-render via `BlipVoice.Render`, assert `sumAbsHash` within 1e-6 + `zeroCrossings` within ±2 + `patchHash` equality (stale-fixture guard).
- `ia/specs/audio-blip.md` exists — structure matches `ia/specs/*.md` conventions. `docs/blip-procedural-sfx-exploration.md` has "Superseded by `ia/specs/audio-blip.md`" banner.
- `ia/specs/glossary.md` — new rows: **Blip variant**, **Blip cooldown**, **Bake-to-clip**, **Patch flatten**. All existing blip rows updated to cross-ref `ia/specs/audio-blip.md`.
- `npm run validate:all` green.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T12.1 | Fixtures dir + bake script | **TECH-227** | Done (archived) | Create `tools/fixtures/blip/` dir + author `tools/scripts/blip-bake-fixtures.ts` — pure TypeScript port of `BlipVoice.Render` scalar loop (oscillator bank + AHDSR + one-pole LP; float32 math matching C# kernel) or Node shim invoking Unity batchmode. Bakes variant 0 for each of 10 MVP patch param sets (hardcoded from exploration §9 recipes). Writes `tools/fixtures/blip/{id}-v0.json` per id. Run once: `npx ts-node tools/scripts/blip-bake-fixtures.ts`; verify 10 JSON files produced. |
| T12.2 | Golden fixture regression test | **TECH-228** | Done (archived) | `Assets/Tests/EditMode/Audio/BlipGoldenFixtureTests.cs` in existing `Blip.Tests.EditMode.asmdef` (Stage 1.4 — no new asmdef; namespace `Territory.Tests.EditMode.Audio`). Parameterized `[TestCase(BlipId.*)]` × 10: parse `tools/fixtures/blip/{id}-v0.json` via `JsonUtility.FromJson<BlipFixtureDto>`, load SO via `AssetDatabase.LoadAssetAtPath<BlipPatch>("Assets/Audio/Blip/Patches/BlipPatch_{id}.asset")`, re-render via existing `BlipTestFixtures.RenderPatch(in flat, sampleRate=48000, seconds=sampleCount/sampleRate, variant)`, assert `SumAbsHash` within 1e-6 + zero-crossing count within ±2 + `patch.PatchHash == fx.patchHash` (fails if fixture stale — msg points at TECH-227 bake script). Kickoff 2026-04-16: aligned spec sample-rate 44100→48000, namespace `Blip.*`→`Territory.*`, helper class `BlipTestHelpers`→`BlipTestFixtures`, asset path `Assets/Audio/BlipPatches/`→`Assets/Audio/Blip/Patches/`, `RenderPatch` 3rd arg sampleCount→seconds. |
| T12.3 | Exploration → spec promotion | **TECH-229** | Done (archived) | Promote `docs/blip-procedural-sfx-exploration.md` → `ia/specs/audio-blip.md`. Restructure to match `ia/specs/*.md` conventions (section numbering, header format). Add "Superseded by `ia/specs/audio-blip.md`" banner at top of exploration doc. `npm run validate:all` — checks dead spec refs + frontmatter. |
| T12.4 | Glossary rows + cross-refs | **TECH-230** | Done (archived) | `ia/specs/glossary.md` — add rows: **Blip variant** (per-patch randomized sound selection index 0..variantCount-1), **Blip cooldown** (minimum ms between same-id plays; enforced by `BlipCooldownRegistry`), **Bake-to-clip** (on-demand render of `BlipPatchFlat` to `AudioClip` via `BlipBaker.BakeOrGet`), **Patch flatten** (`BlipPatch` SO → `BlipPatchFlat` blittable mirror in `BlipCatalog.Awake`). Rewrite Spec col on 5 existing Audio rows from `ia/projects/blip-master-plan.md` Stage 1.x → `ia/specs/audio-blip.md §N` per kickoff §5.2 mapping. Refresh Index row line 32 to list all 9 Audio terms. `npm run validate:all` green. Kickoff 2026-04-16: corrected over-claim (spec listed 13 existing rows; only 5 exist) + glossary 3-col format (was 4-col) + bundled Index refresh. |

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


**Status:** Done (Stage 4.1 closed 2026-04-16 — TECH-235..TECH-238 all archived; Stage 4.2 closed 2026-04-16 — TECH-243..TECH-246 all archived)

**Backlog state (Step 4):** 8 archived (Stage 4.1 — TECH-235, TECH-236, TECH-237, TECH-238; Stage 4.2 — TECH-243, TECH-244, TECH-245, TECH-246)

**Objectives:** Surface SFX volume + mute to player. MVP binds `BlipSfxVolumeDb` headless via `PlayerPrefs` (Stage 1.1 T1.1.2) — no visible control today. Options-menu slider (normalized 0..1) + mute toggle write `PlayerPrefs` + `AudioMixer.SetFloat("SfxVolume")` live; persist across runs. Small isolated Step — ships independent of Steps 5–7.

**Exit criteria:**

- `MainMenu.unity` Options panel exposes SFX volume slider + mute toggle — mounted inside existing `OptionsPanel` surface.
- Slider domain 0..1 normalized; internal dB conversion `20 * Log10(v)` w/ `-80 dB` floor at `v == 0`; mute toggle hard-clamps to `-80 dB`.
- Slider callback writes `PlayerPrefs.SetFloat("BlipSfxVolumeDb", db)` + `BlipMixer.SetFloat("SfxVolume", db)` on change. Mixer ref cached in `Awake` (invariant #3).
- Mute persists as `PlayerPrefs.GetInt("BlipSfxMuted", 0)`; read at `BlipBootstrap.Awake` ahead of volume apply.
- No new MonoBehaviour singletons (invariant #4); settings controller mounts on `OptionsPanel` GameObject.
- `npm run unity:compile-check` green; `npm run validate:all` green.
- Glossary row updated — **Blip bootstrap** notes visible-volume-UI path alongside headless PlayerPrefs binding.
- Phase 1 — Fixture infrastructure: bake script + fixture JSON files.
- Phase 2 — Fixture regression test + spec promotion + glossary.

**Art:** None — reuses existing UI design system.

**Relevant surfaces (load when step opens):**
- Step 3 outputs on disk: `Assets/Scripts/Audio/Blip/BlipBootstrap.cs` (lines 29–32: `SfxVolumeDbKey`, `SfxVolumeParam`, `SfxVolumeDbDefault` constants; line 52: `PlayerPrefs.GetFloat` in `Awake` — mute key not yet present).
- `Assets/Scripts/Managers/GameManagers/MainMenuController.cs` — `CreateOptionsPanel` at line 308 (Title + Back button only; `sizeDelta = (300, 200)` at line 323); `OnOptionsClicked` at line 511.
- `ia/specs/audio-blip.md §5.1`, `§5.2` — component map + init lifecycle.
- `ia/rules/invariants.md` #3 (mixer ref cached in `Awake`, not per-frame), #4 (no new singletons — controller on OptionsPanel, not static).
- New file: `Assets/Scripts/Audio/Blip/BlipVolumeController.cs` (new).
