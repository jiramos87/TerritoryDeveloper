### Stage 7 — Settings sliders + Credits + first-run toast + resume polish / First-run toast + resume polish

**Status:** _pending_

**Objectives:** First-run toast fires exactly once on first gameplay scene load via `GameNotificationManager.Instance.PostNotification(...)`. Immediate `MusicFirstRunDone = 1` flip (fire-and-forget — no dismissal callback per doc §6.2). Resume track-id smoke coverage — verify fallback warn + shuffle-fresh on missing id (already coded Stage 2.1; this stage adds explicit test path + closes exploration doc Example 4). Lock correct `PostNotification` API signature — `ShowMessage` does not exist; name drift in exploration doc §6.2 + P6 noted in master plan Step 3 header.

**Exit:**

- First-run toast logic lives on `MusicBootstrap` (per exploration doc Design Expansion Components — `MusicFirstRunToast` not a new MB). Method `TryShowFirstRunToast()` called from `MusicBootstrap.Start` (not `Awake` — `GameNotificationManager.Instance` may not be alive yet in Awake ordering; `Start` runs after all Awakes).
- Toast uses `GameNotificationManager.Instance.PostNotification("Jazz playing — toggle via top-right", GameNotificationManager.NotificationType.Info, 5f)` — real API signature (3-arg overload at L188 duration overload). NOT `ShowMessage` (does not exist).
- Duration override contingency — if 3-arg `PostNotification(msg, type, duration)` routes `duration` through but `GameNotificationManager.notificationDuration` field overrides at queue-pop time (coroutine internals unverified), fall-back acceptance = 3s default + doc note in exploration doc revision. Stage 3.3 task intent locks verification step.
- `PlayerPrefs.SetInt(MusicBootstrap.MusicFirstRunDoneKey, 1)` immediately after call (before toast dismissal — fire-and-forget per doc §6.2).
- Resume track-id smoke — editor script or manual PlayerPrefs poke sets `MusicLastTrackId = "t-does-not-exist"`; reload MainMenu; verify warn log + shuffle-fresh start (`BootstrapAutoplay` Stage 2.1 T2.1.10 already wires this — Stage 3.3 adds explicit manual test).
- `npm run unity:compile-check` green.
- Exploration doc §6.2 + P6 "ShowMessage" naming drift noted in MEMORY.md or exploration-doc revision (optional follow-up; non-blocker for Music MVP).
- Phase 1 — `TryShowFirstRunToast` on `MusicBootstrap.Start` + PlayerPrefs flag flip + correct API.
- Phase 2 — Resume track-id smoke + duration contingency verification.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T7.1 | TryShowFirstRunToast on Start | _pending_ | _pending_ | Add `private void Start()` + `private void TryShowFirstRunToast()` on `MusicBootstrap`. `Start` body: `TryShowFirstRunToast();`. Method body: `if (PlayerPrefs.GetInt(MusicFirstRunDoneKey, 0) != 0) return; var gnm = GameNotificationManager.Instance; if (gnm == null) { Debug.LogWarning("[Music] GameNotificationManager not alive — first-run toast skipped"); PlayerPrefs.SetInt(MusicFirstRunDoneKey, 1); return; } gnm.PostNotification("Jazz playing — toggle via top-right", GameNotificationManager.NotificationType.Info, 5f); PlayerPrefs.SetInt(MusicFirstRunDoneKey, 1);`. Reason `Start` not `Awake`: `GameNotificationManager.Instance` set in its own `Awake`; execution order uncertain → `Start` is safe. |
| T7.2 | Duration contingency verify | _pending_ | _pending_ | Verify at authoring time — does `PostNotification(msg, type, duration)` (L188 3-arg overload) actually apply per-call `duration` OR does `DisplayNotificationCoroutine` use the serialized `notificationDuration` field? Read `GameNotificationManager.cs` L200-260 during implementation; if duration arg is ignored, either (a) patch `GameNotificationManager` to use arg (out-of-scope Music MVP — split to separate TECH id) OR (b) accept default 3s + update exploration doc §6.2 to state "toast ~3s default". Log outcome in task Verification block + MEMORY.md. |
| T7.3 | Resume missing-id smoke | _pending_ | _pending_ | Manual smoke — create `EditorTools/ForceMissingMusicTrackId.cs` editor menu (Territory Developer → Music → Force missing last-track id). Menu body: `PlayerPrefs.SetString("MusicLastTrackId", "t-does-not-exist"); PlayerPrefs.Save(); Debug.Log("[Music] Forced missing last-track id — reload MainMenu to verify fallback");`. Operator runs menu → reloads MainMenu → observes warn log `"[Music] last track 't-does-not-exist' not in playlist — starting shuffle-fresh"` + music autoplays fresh track. Closes exploration doc §Examples "Missing track on resume". |
| T7.4 | Second-launch no-toast smoke | _pending_ | _pending_ | Manual smoke — `Window → Unity Registry → clear PlayerPrefs` (or editor menu `Edit → Clear All PlayerPrefs`) → launch MainMenu → toast shows (~5s OR 3s per T3.3.2 outcome) → stop play → launch MainMenu again → toast does NOT show (flag `MusicFirstRunDone = 1` persisted). `npm run unity:compile-check` green. Stage 3.3 closes Step 3 — all 6 exploration Implementation Points landed (P1 + P2 Step 1, P3 + P4 Step 2, P5 + P6 Step 3). |

---
