# Blip — Master Plan (MVP)

> **Last updated:** 2026-04-16
>
> **Status:** In Progress — Step 1 Done; Step 2 Final (closed 2026-04-15); Step 3 Final (fully shipped 2026-04-16); Step 4 Final (closed 2026-04-16); Step 5 decomposed (tasks _pending_, ready to file when Step 4 Final confirmed); Steps 6–7 skeleton (stages named, tasks _pending_)
>
> **Scope:** Procedural SFX synthesis subsystem. Ten baked sounds, parameter-only patches, zero `.wav` / `.ogg` assets under `Assets/Audio/Sfx/`. Post-MVP extensions (Live DSP, FX chain, LFOs, editor window, 10 more sounds) → `docs/blip-post-mvp-extensions.md`.
>
> **Exploration source:** `docs/blip-procedural-sfx-exploration.md` (§7 architecture, §11 names registry, §13 locked decisions, §14 MVP scope).
>
> **Hierarchy rules:** `ia/rules/project-hierarchy.md` (step > stage > phase > task). `ia/rules/orchestrator-vs-spec.md` (this doc = orchestrator, never closeable).
>
> **Sibling orchestrators in flight (shared `feature/multi-scale-plan` branch):**
> - `ia/projects/multi-scale-master-plan.md` — mutates `GridManager.cs` + `GameSaveManager.cs` + save schema. Blip Step 3.3 World lane kickoff must re-read `GridManager` selection surface now that multi-scale Stage 1.3 is archived. `WorldCellSelected` stays scale-agnostic in MVP; per-scale variants tracked in `docs/blip-post-mvp-extensions.md` §4.
> - `ia/projects/sprite-gen-master-plan.md` — Python tool + new `Assets/Sprites/Generated/` output. Disjoint C# surface; no blip collision on runtime code.
> - **Parallel-work rule:** do NOT run `/stage-file` or `/closeout` against two sibling orchestrators concurrently — glossary + MCP index regens must sequence on a single branch.
>
> **Read first if landing cold:**
> - `docs/blip-procedural-sfx-exploration.md` — full design + pseudo-code + 20 concrete examples. §13 (locked decisions) + §14 (MVP scope) are ground truth.
> - `docs/blip-post-mvp-extensions.md` — scope boundary (what's OUT of MVP).
> - `ia/rules/project-hierarchy.md` + `ia/rules/orchestrator-vs-spec.md` — doc semantics + phase/task cardinality rule (≥2 tasks per phase).
> - `ia/rules/invariants.md` — #3 (no `FindObjectOfType` in hot loops), #4 (no new singletons).
> - MCP: `backlog_issue {id}` per referenced id once tasks file; never full `BACKLOG.md` read.

---

## Stages

> **Tracking legend:** Step / Stage `Status:` uses enum `Draft | In Review | In Progress — {active child} | Final` (per `ia/rules/project-hierarchy.md`). Phase bullets use `- [ ]` / `- [x]`. Task tables carry a **Status** column: `_pending_` (not filed) → `Draft` → `In Review` → `In Progress` → `Done (archived)`. Markers flipped by lifecycle skills: `stage-file` → task rows gain `Issue` id + `Draft` status; `/author` (`plan-author`) → `In Review`; `/implement` → `In Progress`; the Stage-scoped `/closeout` pair (`stage-closeout-plan` → `plan-applier` Mode `stage-closeout`) → task rows `Done (archived)` + stage `Final` + stage-level rollup.

**Status:** Final

### Stage index

- [Stage 1 — DSP foundations + audio infra / Audio infrastructure + persistent bootstrap](stage-1-audio-infrastructure-persistent-bootstrap.md) — _Final_
- [Stage 2 — DSP foundations + audio infra / Patch data model](stage-2-patch-data-model.md) — _Done — TECH-111..TECH-115 Done_
- [Stage 3 — DSP foundations + audio infra / Voice DSP kernel](stage-3-voice-dsp-kernel.md) — _Final — all tasks complete (TECH-116..120 Done, TECH-135 Done; TECH-121 + TECH-122 compressed into TECH-135)_
- [Stage 4 — DSP foundations + audio infra / EditMode DSP tests](stage-4-editmode-dsp-tests.md) — _Done (closed 2026-04-15 — all 5 tasks archived)_
- [Stage 5 — Bake + facade + PlayMode smoke / Bake-to-clip pipeline](stage-5-bake-to-clip-pipeline.md) — _Done — TECH-159 / TECH-160 / TECH-161 / TECH-162 Done (archived) 2026-04-15_
- [Stage 6 — Bake + facade + PlayMode smoke / Catalog + mixer router + cooldown registry + player pool](stage-6-catalog-mixer-router-cooldown-registry-player-pool.md) — _Done (6 tasks archived 2026-04-15 — **TECH-169**..**TECH-174**)_
- [Stage 7 — Bake + facade + PlayMode smoke / BlipEngine facade + main-thread gate](stage-7-blipengine-facade-main-thread-gate.md) — _Done — 4 tasks archived (TECH-188..TECH-191) 2026-04-15_
- [Stage 8 — Bake + facade + PlayMode smoke / PlayMode smoke test](stage-8-playmode-smoke-test.md) — _Done — TECH-196..TECH-199 all archived 2026-04-15._
- [Stage 9 — Patches + integration + golden fixtures + promotion / Patch authoring + catalog wiring](stage-9-patch-authoring-catalog-wiring.md) — _Done (all tasks archived 2026-04-15 — TECH-209..TECH-212)_
- [Stage 10 — Patches + integration + golden fixtures + promotion / UI + Eco + Sys call sites](stage-10-ui-eco-sys-call-sites.md) — _Done — all tasks archived 2026-04-15 (TECH-215..TECH-218)_
- [Stage 11 — Patches + integration + golden fixtures + promotion / World lane call sites](stage-11-world-lane-call-sites.md) — _Done — all 4 tasks archived (TECH-219 + TECH-220 archived 2026-04-15, TECH-221 + TECH-222 archived 2026-04-16)_
- [Stage 12 — Patches + integration + golden fixtures + promotion / Golden fixtures + spec promotion + glossary](stage-12-golden-fixtures-spec-promotion-glossary.md) — _Done (closed 2026-04-16) — all tasks archived (TECH-227..TECH-230)_
- [Stage 13 — Patches + integration + golden fixtures + promotion / Options panel UI (slider + mute toggle + controller stub)](stage-13-options-panel-ui-slider-mute-toggle-controller-stub.md) — _Final (4 tasks filed 2026-04-16 — TECH-235..TECH-238 all archived; closed 2026-04-16)_
- [Stage 14 — Patches + integration + golden fixtures + promotion / Settings controller + persistence + mute semantics](stage-14-settings-controller-persistence-mute-semantics.md) — _Done (TECH-243..TECH-246 all archived 2026-04-16)_
- [Stage 15 — Patches + integration + golden fixtures + promotion / FX data model + memoryless cores](stage-15-fx-data-model-memoryless-cores.md) — _Done (all 5 tasks TECH-256..TECH-260 archived)_
- [Stage 16 — Patches + integration + golden fixtures + promotion / Delay-line FX + BlipDelayPool](stage-16-delay-line-fx-blipdelaypool.md) — _Done (closed 2026-04-17 — TECH-270..TECH-275 all archived)_
- [Stage 17 — Patches + integration + golden fixtures + promotion / LFOs + routing matrix + param smoothing](stage-17-lfos-routing-matrix-param-smoothing.md) — _Final_
- [Stage 18 — Patches + integration + golden fixtures + promotion / Biquad BP + integration + golden-fixture regression gate](stage-18-biquad-bp-integration-golden-fixture-regression-gate.md) — _In Progress — 4 tasks filed 2026-04-18 (TECH-434..TECH-437)_

## Deferred decomposition

- **Step 2 — Bake + facade + PlayMode smoke:** decomposed 2026-04-15. Stages: Bake-to-clip pipeline, Catalog + mixer router + cooldown registry + player pool, BlipEngine facade + main-thread gate, PlayMode smoke test.
- **Step 3 — Patches + integration + golden fixtures + promotion:** decomposed 2026-04-15. Stages: Patch authoring + catalog wiring, UI + Eco + Sys call sites, World lane call sites, Golden fixtures + spec promotion + glossary.
- **Step 4 — Settings UI + volume controls:** decomposed 2026-04-16. Stages: Options panel UI (slider + mute toggle + controller stub), Settings controller + persistence + mute semantics.
- **Step 5 — DSP kernel v2 — FX chain + LFOs + biquad BP + param smoothing:** decomposed 2026-04-16. Stages: FX data model + memoryless cores, Delay-line FX + BlipDelayPool, LFOs + routing matrix + param smoothing, Biquad BP + integration + golden-fixture regression gate.
- **Step 6 — 10 post-MVP sound patches + call sites:** skeleton only (2026-04-16). Stages named (UI lane; Tool lane; World lane; Sys lane + golden-fixture + catalog + glossary closeout); decompose via `/stage-decompose` when Step → `In Progress` AND Step 5 closed.
- **Step 7 — BlipPatchEditorWindow:** skeleton only (2026-04-16). Stages named (Editor asmdef + window shell + preview + auto-rebake; Waveform + spectrum + LUFS; A/B compare + polish); decompose via `/stage-decompose` when Step → `In Progress` AND Step 6 closed.

Do NOT pre-file Step 3–7 BACKLOG rows. Candidate-issue pointers live inline on each step's **Relevant surfaces** line; new-feature-row candidates surface during that step's decomposition pass, filed under `§ Audio / Blip lane` in `BACKLOG.md`.

Step 1 + Step 2 stages decomposed above w/ phases + tasks. Steps 4–7 carry stage names only — phases + tasks decompose lazily. Use `stage-file` skill to create BACKLOG rows + project spec stubs when a given stage → `In Progress`.

---

## Orchestration guardrails

**Do:**

- Propose edits to step / stage skeletons when a phase exposes missing load-bearing item (e.g. Stage 1.3 reveals need for extra voice-state field → edit stage objectives + add task).
- Push MVP-scope-creep into `docs/blip-post-mvp-extensions.md`. Edits to that doc are cheap; edits to MVP stages require explicit re-decision against exploration §13.
- Create Stage 2.x / Stage 3.x orchestrator content lazily when parent step → `In Progress`.
- Keep task rows `_pending_` until `stage-file` runs for that stage. Never hand-author BACKLOG rows ahead of stage open.

**Do not:**

- Resurrect Live DSP path (`BlipLiveHost`, `OnAudioFilterRead`, `BlipEventQueue`, `PlayLoop`, `BlipHandle`) inside MVP stages. Entire surface deferred to post-MVP per exploration §13 + §15.
- Resurrect FX chain, LFOs, biquad BP filter, param smoothing, LUT oscillators, voice-steal crossfade, cache pre-warm, `BlipLutPool` / `BlipDelayPool` inside MVP. All post-MVP.
- Add spatialization (`BlipEngine.PlayAt`) to MVP API surface. Flat stereo only.
- Add sounds beyond the 10 MVP list (exploration §14). 11th sound → post-MVP extensions list first.
- Introduce custom `EditorWindow` w/ waveform preview / spectrum / LUFS / A/B compare inside MVP. Inspector-only authoring per exploration §13.
- Rely on byte-equality cross-platform determinism in MVP golden fixtures. Use sum-of-abs tolerance hash. LUT-osc bit-exact path lands post-MVP.
- Bypass `BlipEngine` main-thread assert. Background-thread `Play` = bug. Enforced at facade entry.
- Violate invariant #3 — `BlipEngine` caches `BlipCatalog` / `BlipPlayer` refs after first lookup; no `FindObjectOfType` in per-frame paths.
- Violate invariant #4 — `BlipEngine` is a static facade (stateless dispatch); all state lives on MonoBehaviour hosts under `BlipBootstrap`. Not a singleton pattern.
- File BACKLOG rows for future-step Blip FEAT ideas outside an open stage. Use `docs/blip-post-mvp-extensions.md` as the holding pen.
- Give time estimates on steps / stages / phases / tasks.
- Close this orchestrator via `/closeout` — orchestrators are permanent per `ia/rules/orchestrator-vs-spec.md`. Individual task specs close normally; stages close via the `/closeout` pair (`stage-closeout-plan` → `plan-applier` Mode `stage-closeout`); the umbrella orchestrator never deletes.

---

## Decision Log

> **Pattern:** append rows as stages close (via the `/closeout` pair) or when orchestrator-level pivots surface in task authoring. Format: `{YYYY-MM-DD} — {short title}. {1–3 sentence rationale}. Source: {task id | stage id | author | review}.`

- `2026-04-13 — MVP drops BlipMode enum.` Single implicit baked path for MVP. `BlipMode` enum re-lands post-MVP when `BlipLiveHost` + `OnAudioFilterRead` Live DSP path ships. Source: pre-implementation review of this orchestrator.
- `2026-04-13 — BlipMixerRouter parallel to BlipCatalog.` `BlipPatchFlat` must stay blittable (no managed refs → no `AudioMixerGroup` in flat struct). `BlipMixerRouter` holds `BlipId → AudioMixerGroup` map built at `BlipCatalog.Awake` from authoring-only `BlipPatch.mixerGroup` ref. Source: pre-implementation review.
- `2026-04-13 — BlipCooldownRegistry lives on BlipCatalog.` Instance field on MonoBehaviour host (plain class, owned by catalog) — not static — to honor invariant #4 (no new singletons). `BlipEngine.Play` queries via cached catalog ref. Source: pre-implementation review.
- `2026-04-13 — MVP Settings UI deferred; headless PlayerPrefs binding.` `BlipBootstrap.Awake` reads `BlipSfxVolumeDb` from `PlayerPrefs` + calls `AudioMixer.SetFloat("SfxVolume", db)`. Visible slider + mute toggle post-MVP per `docs/blip-post-mvp-extensions.md` §4. Source: repo audit (no existing Settings surface found).
- `2026-04-13 — Boot scene = MainMenu.unity (build index 0).` `BlipBootstrap` prefab placed at root of `MainMenu.unity`; survives load via `DontDestroyOnLoad(transform.root.gameObject)` per `GameNotificationManager.cs` pattern. Source: `MainMenuController.cs` reads `SceneManager.LoadScene(MainSceneBuildIndex)`.
- `2026-04-13 — Determinism test uses sum-of-abs tolerance + first-256 byte gate.` Byte-equality on full buffer brittle against JIT / `Math.Sin` LSB drift. Sum-of-abs hash within 1e-6 epsilon + first-256-samples byte-equal (cheap early-signal gate) gives deterministic regression signal without platform-brittleness. Bit-exact path post-MVP w/ LUT oscillators per `docs/blip-post-mvp-extensions.md` §1. Source: pre-implementation research.
- `2026-04-13 — IAudioGenerator not available (Unity 2022.3.62f3).` Unity 6.3 LTS introduces `IAudioGenerator` cleanup for live DSP; current project on Unity 2022.3 so bake-to-clip stays MVP path + `OnAudioFilterRead` remains post-MVP Live DSP path. Revisit on engine upgrade. Source: `ProjectSettings/ProjectVersion.txt` + Unity 6 research.
- `2026-04-13 — AHDSR per-stage shape enum (Linear | Exponential).` Exponential shape (`1 - exp(-t/τ)` on attack; τ = stageDuration/4) reads perceptually linear per ear's log loudness response. Keeps scope tight (no curves) while giving natural-sounding envelopes. Source: audio perception literature.
- `2026-04-13 — OnValidate clamps attack/decay/release ≥ 1 ms.` Prevents snap-onset click at default 48 kHz mix rate (≈48-sample ramp floor). Source: DSP best practice for step-free transitions.
- `2026-04-14 — Stage 1.3 closed; TECH-121 + TECH-122 compressed into TECH-135.` Render-driver and per-invocation jitter were originally two separate tasks (T1.3.6 / T1.3.7); merged into single TECH-135 during implementation because jitter is computed inside the same per-sample loop — splitting produced no useful parallel track. Compression approved during stage execution; spec updated in-place. Source: Stage 1.3 project-stage-close.
- `2026-04-16 — Step 4 (Settings UI + volume controls) selected.` Smallest-blast-radius post-MVP win — replaces today's headless `PlayerPrefs` binding w/ player-visible slider + mute toggle. Independent of Steps 5–7, ships anytime. Source: post-MVP expansion review (handoff 1).
- `2026-04-16 — Step 5 (DSP kernel v2) selected; must precede Step 6.` FX chain + LFOs + biquad BP + param smoothing unlock cliff bit-crush, terrain ring-mod, tooltip LFO tremolo in Step 6 patches. Gate held (not merged into Step 6) to avoid double-fixture churn when kernel change lands alongside 10 new patches. Source: post-MVP expansion review.
- `2026-04-16 — Step 6 (10 post-MVP patches + call sites) selected.` Highest player-audible impact — doubles catalog 10 → 20 + fills MVP gaps (demolish, tooltip, terrain, cliff, multi-select, load). Depends on Step 5 (FX surfaces) + Stage 3.4 (spec promotion). Source: post-MVP expansion review.
- `2026-04-16 — Step 7 (BlipPatchEditorWindow) selected; gated on 20-patch pain.` Overrides exploration §13 "Inspector only" lock once 20 patches × FX chain × LFO routing × biquad params make Inspector tuning untenable. Gate explicit via Step 6 closed + Step 5 closed deps. Source: post-MVP expansion review.
- `2026-04-16 — Live DSP path (candidate #4 — BlipLiveHost / OnAudioFilterRead) rejected for post-MVP pick.` No MVP or Step 6 sound requires live voice modulation post-trigger. Unity 6.3 `IAudioGenerator` revisit-on-upgrade per Decision Log 2026-04-13. Deferred to future orchestrator pass. Source: post-MVP expansion review.
- `2026-04-16 — LUT osc + voice-steal crossfade + cache pre-warm (candidate #5) rejected.` Internal quality polish — not user-facing. Sum-of-abs golden fixture (Decision Log 2026-04-13) covers regression w/o bit-exact LUT path. Deferred. Source: post-MVP expansion review.
- `2026-04-16 — Multi-scale WorldCellSelected variants + SysScaleTransition (candidate #7) deferred.` Sibling `multi-scale-master-plan.md` Step 3 still Draft (decomposition deferred until Step 2 → Final). Couple via future Step when multi-scale Step 3/4 → `In Progress`; not this pass. Source: post-MVP expansion review + `multi-scale-master-plan.md` Step 3 status.
- `2026-04-16 — "CI headless bake integration tests" rejected as standalone Step.` Hard constraint per handoff — existing test infra (`Assets/Tests/EditMode/Audio/BlipGoldenFixtureTests.cs` from Stage 3.4 + `tools/scripts/blip-bake-fixtures.ts`) already covers bake determinism. Folds into Stage 6.4 task-level asks when catalog grows to 20. Source: handoff 1 hard constraint.
- `2026-04-16 — Stage 3.4 T3.4.3: exploration doc promoted to ia/specs/audio-blip.md.` Canonical DSP kernel + architecture + invariants now under `ia/specs/`; exploration doc retains §9 recipe tables + §10–§12 live-DSP sketches + §13 locked decisions + §15 post-MVP extensions as historical / implementer reference. `docs/blip-procedural-sfx-exploration.md` gains "Superseded by" banner. Source: TECH-229 closeout.

## Lessons Learned

> **Pattern:** append rows as stages close, migrate actionable ones to canonical IA (`ia/specs/`, `ia/rules/`, glossary) via the `/closeout` pair. Keep the lesson here if it's orchestrator-local (applies only inside Blip MVP); promote if it generalizes. Format: `{YYYY-MM-DD} — {short title}. {1–3 sentence summary}. {Action: where promoted, or "orchestrator-local"}.`

- `2026-04-14 — Compress co-located tasks before filing.` When two pending tasks share the same implementation surface (same file, same loop), merge them into one TECH issue at stage-file time rather than filing both then closing one early. Avoids orphan issues + simplifies history. Action: orchestrator-local (Blip MVP).
- `2026-04-14 — BlipVoiceState carries all per-voice mutable DSP state.` `phaseA..D`, `envLevel`, `envStage`, `filterZ1`, `rngState`, `samplesElapsed` all live in a single blittable struct passed by ref — no statics, no heap alloc inside `Render`. Pattern validated by Stage 1.3; reuse for any future voice-type addition (e.g. `BlipLiveHost` post-MVP). Action: promoted to `ia/specs/audio-blip.md` §3 DSP kernel (TECH-229).
- `2026-04-14 — Exponential τ = stageDuration/4 gives ≈98 % settled at stage end.` Validated analytically (`exp(-4) ≈ 0.018`). No tuning pass required for MVP; perceptual loudness log curve satisfied. Action: orchestrator-local (Blip MVP).
