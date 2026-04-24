### Stage 1 — DSP foundations + audio infra / Audio infrastructure + persistent bootstrap


**Status:** Final

**Objectives:** Mixer asset + three routing groups wired. `BlipBootstrap` prefab instantiated in `MainMenu.unity` boot scene, survives scene loads. Headless SFX volume binding via `PlayerPrefs` → `AudioMixer.SetFloat` at `BlipBootstrap.Awake` (no Settings UI in MVP — visible slider + mute toggle post-MVP per `docs/blip-post-mvp-extensions.md` §4). Scene-load suppression policy documented so Blip stays silent until `BlipCatalog.Awake` completes.

**Exit:**

- `Assets/Audio/BlipMixer.mixer` w/ three groups (`Blip-UI`, `Blip-World`, `Blip-Ambient`) + exposed master `SfxVolume` dB param (default 0 dB). Authored via Unity Editor (`Window → Audio → Audio Mixer`); committed as binary YAML asset.
- `BlipBootstrap` GameObject prefab at `MainMenu.unity` root; `DontDestroyOnLoad(transform.root.gameObject)` in `Awake` (pattern per `GameNotificationManager.cs`).
- `SfxVolume` bound headless — `BlipBootstrap.Awake` reads `PlayerPrefs.GetFloat("BlipSfxVolumeDb", 0f)` + calls `BlipMixer.SetFloat("SfxVolume", db)`. No UI touched in MVP.
- Scene-load suppression policy doc'd in glossary row + catalog comment.
- Glossary rows land for **Blip mixer group** + **Blip bootstrap**.
- Phase 1 — Mixer asset + three groups + exposed SFX volume param.
- Phase 2 — Persistent bootstrap prefab + headless volume binding + scene-load suppression policy.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T1.1 | BlipMixer asset | **TECH-98** | Done | Create `Assets/Audio/BlipMixer.mixer` via Unity Editor (`Window → Audio → Audio Mixer` — binary YAML, not hand-written). Three groups (`Blip-UI`, `Blip-World`, `Blip-Ambient`), each routed through master. Expose master `SfxVolume` dB param (`Exposed Parameters` panel, default 0 dB). |
| T1.2 | Headless volume binding | **TECH-99** | Done | Headless SFX volume binding — `BlipBootstrap.Awake` reads `PlayerPrefs.GetFloat("BlipSfxVolumeDb", 0f)` + calls `BlipMixer.SetFloat("SfxVolume", db)`. No Settings UI in MVP (visible slider + mute toggle deferred post-MVP per `docs/blip-post-mvp-extensions.md` §4). Key string constant on `BlipBootstrap`. |
| T1.3 | BlipBootstrap prefab | **TECH-100** | Done | `BlipBootstrap` GameObject prefab + `DontDestroyOnLoad(transform.root.gameObject)` in `Awake` (pattern per `GameNotificationManager.cs`). Empty Catalog / Player / MixerRouter / CooldownRegistry child slots (populated Step 2). Placed at root of `MainMenu.unity` (boot scene; build index 0 per `MainMenuController.cs`). |
| T1.4 | Scene-load suppression | **TECH-101** | Done (archived) | Scene-load suppression policy — no Blip fires until `BlipCatalog.Awake` sets ready flag. Document in glossary rows for **Blip mixer group** + **Blip bootstrap**. |

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
