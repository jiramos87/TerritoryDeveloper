# Music Player — Master Plan (MVP)

> **Last updated:** 2026-04-16
>
> **Status:** In Progress — Step 1 pending (Stage 1.1 ready for `/stage-file`); Steps 2 + 3 fully decomposed (tasks `_pending_`)
>
> **Scope:** Authored-jazz music subsystem. `.ogg` streaming from `Assets/Resources/Music/`, shuffle no-repeat-until-exhausted, single `AudioSource` + coroutine advance, C# event-driven `NowPlayingWidget` HUD row, Settings Master + Music sliders + Credits sub-screen, first-run toast, resume-by-track-id. Separate from Blip (procedural SFX) — single overlap = shared `Assets/Audio/BlipMixer.mixer` asset (new `Blip-Music` group + `MusicVolume` + `MasterVolume` params). No scope-boundary doc — `docs/music-player-jazz-exploration.md` §2.2 + §10 carry non-scope lock.
>
> **Exploration source:** `docs/music-player-jazz-exploration.md` (§10 locked decisions — 27 items; Design Expansion §Chosen Approach, §Components, §Data flow, §Interfaces, §Non-scope, §Architecture mermaid, §Subsystem impact table, §Implementation points P1–P6, §Examples, §Review Notes — 3 blocking resolved inline).
>
> **Hierarchy rules:** `ia/rules/project-hierarchy.md` (step > stage > phase > task). `ia/rules/orchestrator-vs-spec.md` (this doc = orchestrator, never closeable via `/closeout`).
>
> **Sibling orchestrator in flight:**
> - `ia/projects/blip-master-plan.md` — Blip (procedural SFX). Shares `Assets/Audio/BlipMixer.mixer` asset. Music Stage 1.1 mixer edit must land between Blip stages, not during — confirm Blip boundary state before authoring mixer param additions. `SfxVolume` param + `BlipSfxVolumeDb` PlayerPrefs key stay Blip-owned — Music does not re-declare.
> - **Parallel-work rule:** do NOT run `/stage-file` or `/closeout` against two sibling orchestrators concurrently — glossary + MCP index regens must sequence on a single branch.
>
> **Read first if landing cold:**
> - `docs/music-player-jazz-exploration.md` — full design + 5 worked examples + 3 resolved blocking items. §10 (27 locked decisions) is ground truth.
> - `ia/rules/project-hierarchy.md` + `ia/rules/orchestrator-vs-spec.md` — doc semantics + phase/task cardinality rule (≥2 ≤6 tasks per phase).
> - `ia/rules/invariants.md` — **#3** (no `FindObjectOfType` in `Update` / per-frame — widget caches `MusicPlayer` ref in `Awake`) and **#4** (no new singletons — `MusicBootstrap.Instance` is Inspector-placed MB, not `new`). Both gate every Step.
> - `ia/specs/audio-blip.md` §5.1 + §5.4 — BlipBootstrap pattern + existing mixer group layout. `Assets/Scripts/Audio/Blip/BlipBootstrap.cs` is the template mirror. Note: spec present on disk but not indexed by MCP `list_specs` — read directly.
> - `ia/specs/unity-development-context.md` §3 + §6 — `[SerializeField] private` + `FindObjectOfType` fallback pattern in `Awake`; Script Execution Order + initialization races.
> - `ia/specs/ui-design-system.md` §1.3.1 (HUD / uGUI hygiene — no full-stretch anchors on small strips, corner anchors = (1,1)), §3.1 (HUD density — `Canvas/DataPanelButtons` as-built cluster), §5.2 (`UiTheme` script + theme asset paths).
> - `ia/specs/managers-reference.md` §Game notifications — `GameNotificationManager.Instance` singleton; actual API = `PostNotification(message, type, duration)` (exploration doc §6.2 + P6 use informal "ShowMessage" — real method name = `PostNotification`; Step 3 task intent locks the correct signature).
> - MCP: `backlog_issue {id}` per referenced id once tasks file; never full `BACKLOG.md` read.

---

## Stages

> **Tracking legend:** Step / Stage `Status:` uses enum `Draft | In Review | In Progress — {active child} | Final` (per `ia/rules/project-hierarchy.md`). Phase bullets use `- [ ]` / `- [x]`. Task tables carry a **Status** column: `_pending_` (not filed) → `Draft` → `In Review` → `In Progress` → `Done (archived)`. Markers flipped by lifecycle skills: `stage-file` → task rows gain `Issue` id + `Draft` status; `/author` (`plan-author`) → `In Review`; `/implement` → `In Progress`; the Stage-scoped `/closeout` pair (`stage-closeout-plan` → `plan-applier` Mode `stage-closeout`) → task rows `Done (archived)` + stage `Final` + stage-level rollup.

### Stage index

- [Stage 1 — Audio infra + playlist pipeline / Mixer extension + persistent bootstrap](stage-1-mixer-extension-persistent-bootstrap.md) — _In Progress — tasks Draft (TECH-316..TECH-321 filed 2026-04-17)_
- [Stage 2 — Audio infra + playlist pipeline / Playlist data model + loader + import postprocessor](stage-2-playlist-data-model-loader-import-postprocessor.md) — __pending__
- [Stage 3 — MusicPlayer runtime + NowPlayingWidget / MusicPlayer runtime (playback, shuffle, coroutines, events)](stage-3-musicplayer-runtime-playback-shuffle-coroutines-events.md) — __pending__
- [Stage 4 — MusicPlayer runtime + NowPlayingWidget / NowPlayingWidget HUD](stage-4-nowplayingwidget-hud.md) — __pending__
- [Stage 5 — Settings sliders + Credits + first-run toast + resume polish / Settings Master + Music sliders](stage-5-settings-master-music-sliders.md) — __pending__
- [Stage 6 — Settings sliders + Credits + first-run toast + resume polish / Music Credits sub-screen](stage-6-music-credits-sub-screen.md) — __pending__
- [Stage 7 — Settings sliders + Credits + first-run toast + resume polish / First-run toast + resume polish](stage-7-first-run-toast-resume-polish.md) — __pending__

## Orchestration guardrails

- **No BACKLOG rows until `stage-file`.** Tasks in this plan stay `_pending_` status. `/stage-file ia/projects/music-player-master-plan.md Stage 1.1` materializes BACKLOG ids + `ia/projects/{ISSUE_ID}.md` specs.
- **No orchestrator close.** This file is permanent per `ia/rules/orchestrator-vs-spec.md`. Stage close (and per-task closure folded inside it) is handled by the Stage-scoped `/closeout` pair (`stage-closeout-plan` → `plan-applier` Mode `stage-closeout`). This orchestrator never appears in BACKLOG.
- **Parallel-work rule.** Do NOT run `/stage-file` or `/closeout` concurrently against this orchestrator + `ia/projects/blip-master-plan.md` — glossary + MCP index regens must sequence on single branch.
- **Blip mixer coordination.** Stage 1.1 mixer edit lands between Blip stages (not during). Check `ia/projects/blip-master-plan.md` status before running `/stage-file` on Music 1.1 — if Blip is mid-stage on mixer work, pause + wait for Blip stage `Final`. Current state at plan landing (2026-04-16): Blip Step 1 mostly done, Step 2 Final, Step 3 Final — mixer asset stable → safe window for Music 1.1.
- **Invariants checklist (re-run at every `/author`):**
  - #3 — `MusicPlayer`/`MusicBootstrap`/`NowPlayingWidget`/`MusicCreditsPanel` cache refs in `Awake` via `[SerializeField] private` + `FindObjectOfType` fallback. No `FindObjectOfType` / `GetComponent` inside `Update` / per-frame. EQ-bars `Update` reads only cached `Image[]` + writes `sizeDelta` (struct — zero alloc).
  - #4 — `MusicBootstrap.Instance` + `MusicPlayer` MB are Inspector-placed. Zero `new MusicBootstrap()` / `new MusicPlayer()`. `DontDestroyOnLoad(transform.root.gameObject)` persists across scene loads.
- **Glossary rows** — deferred to `/stage-file` + `/implement`. Target rows per exploration doc §10 item 27: **Music track**, **Music playlist**, **Music player**, **Now-playing widget**, **Music mixer group**, **Music Credits**. Add on the stage that introduces each term (e.g. Music track + Music playlist land Stage 1.2; Music player lands Stage 2.1; Now-playing widget lands Stage 2.2; Music mixer group lands Stage 1.1; Music Credits lands Stage 3.2).
- **Naming drift noted:** exploration doc §6.2 + P6 write "ShowMessage" but `GameNotificationManager.cs` exposes `PostNotification(message, type, duration)`. Master plan task intents use real API. Optional follow-up — revise exploration doc to match OR add glossary row asserting `PostNotification` as canonical.
- **Spec indexing gap:** `ia/specs/audio-blip.md` present on disk but not indexed by MCP `list_specs` (flagged in exploration doc Review Notes §Gaps). Stage 1.1 task authoring reads spec directly. Registration follow-up = separate TECH-id, out of music-player scope.

---
