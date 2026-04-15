---
purpose: "TECH-218 — GameSaveManager save-complete Blip call site."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-218 — GameSaveManager save-complete Blip call site

> **Issue:** [TECH-218](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-15
> **Last updated:** 2026-04-15

## 1. Summary

Wire `BlipEngine.Play(BlipId.SysSaveGame)` after successful save-file write in `GameSaveManager.SaveGame` + `TryWriteGameSaveToPath`. Patch SO carries 2 s cooldown via `BlipCooldownRegistry` — no additional guard needed. Completes Blip master plan Stage 3.2 Exit bullet 3 (Sys lane). Orchestrator: [`ia/projects/blip-master-plan.md`](blip-master-plan.md) Stage 3.2.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Successful save write → `SysSaveGame` SFX fires once.
2. 2 s cooldown prevents audible burst if user hammers save hotkey.
3. Failed save write (exception path) → silent (no SFX on failure).

### 2.2 Non-Goals

1. Autosave wiring — no autosave in MVP; post-MVP per orchestrator.
2. Load-complete SFX — not in MVP BlipId list.
3. Save-failed error SFX — no `SysSaveFailed` patch authored.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Player | Save game, hear confirm tone | `SaveGame` success fires `SysSaveGame` |
| 2 | Developer | Rapid save-hotkey mash stays clean | `BlipCooldownRegistry` gates at patch `cooldownMs = 2000` |

## 4. Current State

### 4.1 Domain behavior

`SaveGame` line ~64 + `TryWriteGameSaveToPath` line ~86 write save JSON via `File.WriteAllText` without audio feedback. Patch authored (Stage 3.1 TECH-209) w/ 2000 ms cooldown per orchestrator.

### 4.2 Systems map

- `Assets/Scripts/Managers/GameManagers/GameSaveManager.cs` — `SaveGame` line ~64 (default save path), `TryWriteGameSaveToPath(path)` line ~86 (explicit path).
- `System.IO.File.WriteAllText(path, json)` — success predicate = no exception thrown.
- `BlipId.SysSaveGame` enum (Stage 1.2).
- `BlipCooldownRegistry` (Stage 2.2 TECH-172) — per-id cooldown in dsp-time.

## 5. Proposed Design

### 5.1 Target behavior

Successful `File.WriteAllText` → `BlipEngine.Play(BlipId.SysSaveGame)` as next statement. Exception during write → no Blip fire (call sits after write, skipped on throw).

### 5.2 Architecture

Single-line insert immediately after each `File.WriteAllText` call inside the happy-path (before any `Debug.Log` / return). Both call sites (`SaveGame` + `TryWriteGameSaveToPath`) get the same insert. No guard needed — cooldown registry handles rapid-save burst; exception-propagation handles failure silence.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-15 | Cooldown gating via patch SO only | Registry already enforces 2 s; duplicate guard = tech debt | Local `bool _saveBlipInFlight` (rejected — redundant with registry) |
| 2026-04-15 | No SFX on save failure | MVP lacks `SysSaveFailed` patch; exception-skip gives silent fail | Add failure patch (deferred post-MVP) |

## 7. Implementation Plan

### Phase 1 — Save-complete wiring

- [ ] `SaveGame` line ~64 — add `BlipEngine.Play(BlipId.SysSaveGame)` after `File.WriteAllText`.
- [ ] `TryWriteGameSaveToPath` line ~86 — add same call after its `File.WriteAllText`.
- [ ] Verify no try/catch swallows exception between write + Blip call (exception → silent fail).
- [ ] `npm run unity:compile-check` green.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Compile | Unity compile | `npm run unity:compile-check` | |
| Save SFX audible + cooldown gated | Manual | Play Mode; save once → hear; mash save → one blip per 2 s | |
| IA validation | Node | `npm run validate:all` | |

## 8. Acceptance Criteria

- [ ] Both `File.WriteAllText` call sites followed by `BlipEngine.Play(BlipId.SysSaveGame)`.
- [ ] Failure path (exception) silent — Blip call not reached.
- [ ] No additional cooldown guard added (registry owns gating).
- [ ] `npm run unity:compile-check` + `npm run validate:all` green.

## Open Questions

1. None — game rule clear from orchestrator (save-success fires; failure silent MVP).
