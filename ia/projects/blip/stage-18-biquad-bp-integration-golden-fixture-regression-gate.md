### Stage 18 — Patches + integration + golden fixtures + promotion / Biquad BP + integration + golden-fixture regression gate


**Status:** In Progress — 4 tasks filed 2026-04-18 (TECH-434..TECH-437)

**Objectives:** `BlipFilterKind.BandPass` 2nd-order biquad selectable via `resonanceQ`. Integration smoke: all 10 MVP golden fixture hashes pass (passthrough invariant with empty FX + zero LFOs). All 6 Step 5 glossary rows landed and spec updated.

**Exit:**

- `BlipFilterKind.BandPass = 2` in `BlipPatchTypes.cs`; `BlipFilter` + `BlipFilterFlat` gain `float resonanceQ` (clamped 0.1..20 in `OnValidate`).
- `BlipVoiceState` gains `float biquadZ1, biquadZ2` (DF-II transposed delay elements, blittable).
- Biquad BP coefficients pre-computed once per `Render` invocation (1 `Math.Sin` + 1 `Math.Cos`; zero per-sample trig): `w0=2π*cutoffHz/sr; α=sin(w0)/(2Q); b0n=sin(w0)/2/(1+α); a1n=-2cos(w0)/(1+α); a2n=(1-α)/(1+α)`.
- `BlipVoice.Render` BandPass per-sample: DF-II transposed `v=x-a1n*z1-a2n*z2; y=b0n*v-b0n*z2; z2=z1; z1=v` (b1n=0 for bandpass). LP + None unchanged.
- All 10 MVP golden fixture hashes pass (`BlipGoldenFixtureTests` green — empty FX + zero LFOs + LowPass/None = passthrough bit-exact vs Step 3 baselines).
- `BlipNoAllocTests` gains `Render_WithBiquadBP_ZeroManagedAlloc`; assert delta/call ≤ 0.
- 6 glossary rows: **Blip FX chain**, **Blip LFO**, **Biquad band-pass**, **Param smoothing**, **Blip delay pool**, **Blip LUT pool** to `ia/specs/glossary.md` + cross-refs to `ia/specs/audio-blip.md`. `ia/specs/audio-blip.md §4.2` filter section updated: BandPass enum value + `resonanceQ` noted.
- `npm run unity:compile-check` + `npm run validate:all` green.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T18.1 | Biquad data model + BlipVoiceState delay elements | **TECH-434** | Draft | `BlipFilterKind.BandPass = 2` in `BlipPatchTypes.cs`. `BlipFilter` gains `public float resonanceQ` (clamped 0.1..20 in `BlipPatch.OnValidate`). `BlipFilterFlat` gains `public readonly float resonanceQ`; `BlipFilterFlat(BlipFilter src)` ctor copies it. `BlipVoiceState` gains `float biquadZ1, biquadZ2`. `BlipPatchFlat(BlipPatch so, …)` ctor copies `resonanceQ` through the new `BlipFilterFlat` field. |
| T18.2 | Biquad coefficient pre-compute block | **TECH-435** | Draft | Biquad BP pre-compute in `BlipVoice.Render` (outside sample loop, alongside existing `alpha` LP block at lines 59–71 of `BlipVoice.cs`): `double w0=TwoPi*cutoffHz/sampleRate; float sinW=(float)Math.Sin(w0); float cosW=(float)Math.Cos(w0); float alp=sinW/(2f*Q); float b0n=sinW*0.5f/(1f+alp); float a1n=-2f*cosW/(1f+alp); float a2n=(1f-alp)/(1f+alp)`. Computed only when `filter.kind == BandPass`; LP/None branches unchanged. |
| T18.3 | Biquad kernel in Render + NoAlloc BP test | **TECH-436** | Draft | `BlipVoice.Render` per-sample BandPass dispatch: `float v=x-a1n*state.biquadZ1-a2n*state.biquadZ2; float y=b0n*v-b0n*state.biquadZ2; state.biquadZ2=state.biquadZ1; state.biquadZ1=v; sample=y`. `BlipNoAllocTests.Render_WithBiquadBP_ZeroManagedAlloc`: BP patch (cutoffHz=1000, Q=2, deterministic) — 3 warm-up + 10 measured renders; assert delta/call ≤ 0. |
| T18.4 | Golden fixture regression + spec + all 6 glossary rows | **TECH-437** | Draft | Confirm `BlipGoldenFixtureTests` all 10 MVP hashes pass (empty FX chain + zero LFOs + None/LowPass filter = passthrough). 6 glossary rows to `ia/specs/glossary.md`: **Blip FX chain** (`BlipFxChain.ProcessFx` ordered per-patch FX processors), **Blip LFO** (`BlipLfo`/`BlipLfoFlat` per-sample modulator), **Biquad band-pass** (`BlipFilterKind.BandPass` DF-II transposed 2nd-order BP), **Param smoothing** (`BlipVoice.SmoothOnePole` 20 ms 1-pole), **Blip delay pool** (`BlipDelayPool` float[] lease service), **Blip LUT pool** (`BlipLutPool` stub). `ia/specs/audio-blip.md §4.2`: BandPass enum value + `resonanceQ`. `npm run validate:all` green. |

**Dependencies:** Step 1 Done. Ships BEFORE Step 6 (patches depend on FX / LFO / biquad surfaces).

#### §Stage File Plan

> Opus `stage-file-plan` writes structured `{operation, target_path, target_anchor, payload}` tuples here per pending Task. Sonnet `stage-file-apply` reads tuples and materializes BACKLOG rows + spec stubs. Contract: `ia/rules/plan-apply-pair-contract.md`.

_pending — populated by `/stage-file` planner pass._

#### §Plan Fix

> Opus `plan-review` writes targeted fix tuples here when a Stage's Task specs need tightening before first `/implement`. Sonnet `plan-applier` Mode plan-fix reads tuples and applies edits. Contract: `ia/rules/plan-apply-pair-contract.md`.

_pending — populated by `/plan-review` when fixes are needed._

#### §Stage Audit

> Opus `opus-audit` writes one `§Audit` paragraph per Task row here (Stage-scoped bulk, non-pair) once every Task reaches Done post-verify. Feeds `§Stage Closeout Plan` migration tuples downstream. Contract: `ia/rules/plan-apply-pair-contract.md` Stage-scoped non-pair row.

_pending — populated by `/audit {{this-doc}} Stage {{N.M}}` once all Tasks reach Done post-verify._

#### §Stage Closeout Plan

> Opus `stage-closeout-plan` writes unified tuple list here ONCE per Stage when all Task rows reach `Done` post-verify. Sonnet `stage-closeout-apply` reads tuples and applies verbatim. Contract: `ia/rules/plan-apply-pair-contract.md` seam #4.

_pending — populated by `/closeout {{this-doc}} Stage {{N.M}}` planner pass when all Tasks reach `Done`._

---


**Status:** Draft (tasks _pending_ — not yet filed).

**Objectives:** Author 10 additional patches + enum rows + catalog entries + call-site wiring. Catalog 10 → 20 sounds. Covers MVP gaps (tab switch, tooltip appear, demolish, road erase, water paint, terrain raise/lower, cliff created, multi-select step, load game). Leans on Step 5 kernel v2 (cliff thud needs bit-crush; terrain scrape needs ring-mod; tooltip needs LFO tremolo).

**Exit criteria:**

- `BlipId` enum gains 10 rows: `UiTabSwitch`, `UiTooltipAppear`, `ToolRoadErase`, `ToolDemolish`, `ToolWaterPaint`, `ToolTerrainRaise`, `ToolTerrainLower`, `WorldCliffCreated`, `WorldMultiSelectStep`, `SysLoadGame`.
- `Assets/Audio/Blip/Patches/` gains 10 SO assets authored per `docs/blip-post-mvp-extensions.md` §3 recipes.
- `BlipCatalog.entries[]` → 20 rows; mixer-group assignments match `docs/blip-post-mvp-extensions.md` §3 table.
- Call sites fire `BlipEngine.Play(id)` at respective tool / UI hosts (tab switcher, tooltip controller, demolish tool, road-erase tool, water-paint tool, terrain up/down tools, cliff generator, multi-select controller, `GameSaveManager.LoadGame`).
- Cliff thud debounced per terrain-refresh batch (one play per batch, not per cliff cell).
- Multi-select rate-limited via `BlipCooldownRegistry` 125 ms (8 Hz cap).
- Golden fixtures extended to 20 ids; `tools/scripts/blip-bake-fixtures.ts` regenerated; `BlipGoldenFixtureTests` parameterized over full set.
- `npm run unity:compile-check` + `npm run validate:all` green.
- Glossary: new rows for any Step-6-introduced terms (e.g. **Cliff thud debounce**).

**Art:** None — parameter-only patches.

**Relevant surfaces:** `Assets/Scripts/Audio/Blip/BlipId.cs`, `BlipCatalog.cs`, `Assets/Audio/Blip/Patches/*.asset`, tool / UI / world call-site hosts (enumerated at stage decompose), `tools/scripts/blip-bake-fixtures.ts`, `tools/fixtures/blip/*.json`, `docs/blip-post-mvp-extensions.md` §3.

**Stages (skeleton — decompose via `/stage-decompose` when Step → `In Progress`):**

- Stage 6.1 — UI lane (tab switch, tooltip appear).
- Stage 6.2 — Tool lane (demolish, road erase, water paint, terrain raise, terrain lower).
- Stage 6.3 — World lane (cliff created w/ batch debounce; multi-select step w/ 8 Hz cap).
- Stage 6.4 — Sys lane + golden-fixture + catalog + glossary closeout (load game, bake regen, test expansion, glossary rows).

**Dependencies:** Step 5 closed. Stage 3.4 spec promotion closed (Step 6 call sites reference canonical `ia/specs/audio-blip.md`). Multi-scale `WorldCellSelected` per-scale variants stay OUT of this Step — they land via multi-scale orchestrator coupling, not here.

---


**Status:** Draft (tasks _pending_ — not yet filed).

**Objectives:** Custom `EditorWindow` replaces Inspector authoring once 20-patch catalog + FX + LFO + biquad surfaces make Inspector tuning painful. Waveform preview (1 s offline render), spectrum FFT, LUFS meter (simplified EBU R128 mono), A/B compare across two patches, auto-rebake on SO dirty, patch-hash live readout. Overrides exploration §13 "Inspector only" lock — gate documented in Decision Log.

**Exit criteria:**

- `Assets/Editor/Blip/BlipPatchEditorWindow.cs` w/ `Territory/Audio/Blip Patch Editor` menu item.
- Window panels: waveform oscilloscope, spectrum FFT (power-of-two bins), LUFS meter (momentary + integrated readouts), A/B dropdown w/ side-by-side waveform.
- Preview renders offline via `BlipVoice.Render` → `AudioClip` → hidden Editor `AudioSource` (no runtime `BlipEngine` dependency).
- Auto-rebake on `OnValidate` broadcast from `BlipPatch.OnValidate`.
- Patch-hash live readout mirrors `BlipPatchFlat.patchHash` used by golden fixture test.
- New `Assets/Editor/Blip/Blip.Editor.asmdef` (editor-only, depends on `Blip.asmdef` + `Blip.Tests.EditMode.asmdef` helpers).
- Glossary row: **Blip patch editor window**.
- `npm run unity:compile-check` green.
- Phase 1 — Biquad data model: `BlipFilterKind.BandPass` enum value + `resonanceQ` field + `BlipVoiceState` delay elements + coefficient pre-compute block.
- Phase 2 — Biquad kernel in `Render` + `BlipNoAllocTests` BP variant + golden fixture regression + spec + all 6 glossary rows.

**Art:** None — editor tooling only.

**Relevant surfaces:** `Assets/Editor/Blip/BlipPatchEditorWindow.cs` (new), `Assets/Editor/Blip/Blip.Editor.asmdef` (new), `BlipPatch.cs` `OnValidate` broadcast hook, `BlipTestFixtures.RenderPatch` (reuse), `docs/blip-post-mvp-extensions.md` §5.

**Stages (skeleton — decompose via `/stage-decompose` when Step → `In Progress`):**

- Stage 7.1 — Editor asmdef + window shell + offline preview + auto-rebake hook.
- Stage 7.2 — Waveform + spectrum + LUFS panels.
- Stage 7.3 — A/B compare + polish + glossary row.

**Dependencies:** Step 6 closed (20-patch pain threshold — Decision Log documents override of §13 "Inspector only"). Step 5 closed (FX / LFO / biquad surfaces to visualize).

---
