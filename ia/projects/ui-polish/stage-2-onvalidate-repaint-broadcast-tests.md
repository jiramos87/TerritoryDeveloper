### Stage 2 — Token ring extension / OnValidate repaint broadcast + tests

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Wire `OnValidate` broadcast in `UiTheme.cs` (`#if UNITY_EDITOR` gated). Provide a minimal EditMode test fixture that confirms token edits fire broadcast in Editor. No runtime subscribers yet (primitives land in Step 2) — this stage proves the broadcast hook exists + is inert in Player builds.

**Exit:**

- `UiTheme.OnValidate` calls `ThemeBroadcaster.BroadcastAll()` **only** under `#if UNITY_EDITOR` guard (never compiled into Player).
- Stub `ThemeBroadcaster` MonoBehaviour in scene with `BroadcastAll()` method that discovers `IThemed` via `FindObjectsOfType<MonoBehaviour>()` — `Awake`-cached scan list, not per-frame.
- EditMode test confirms `OnValidate` calls broadcaster exactly once per edit; never under Play mode hot loop.
- `#if !UNITY_EDITOR` compile path clean — player build ships without broadcaster references from `OnValidate`.
- Phase 1 — Broadcaster stub + Editor-gated broadcast.
- Phase 2 — EditMode fixture + compile-check.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T2.1 | ThemeBroadcaster stub MonoBehaviour | _pending_ | _pending_ | Create `Assets/Scripts/UI/Theme/ThemeBroadcaster.cs` — scene MonoBehaviour with `[SerializeField] UiTheme theme` + `BroadcastAll()` method. Discovery via `FindObjectsOfType<MonoBehaviour>()` in `Awake`, filter `IThemed`, cache list. No per-frame scan. `IThemed` interface stub lands here too (empty `void ApplyTheme(UiTheme)` — real impls in Step 2). |
| T2.2 | UiTheme.OnValidate Editor gate | _pending_ | _pending_ | Add `OnValidate` to `UiTheme.cs` under `#if UNITY_EDITOR` guard. Find active `ThemeBroadcaster` via `UnityEngine.Object.FindObjectsOfType<ThemeBroadcaster>()` (Editor-only — no Player impact). Call `BroadcastAll()` on each. No-op if none found. Wrap every broadcast ref in `#if UNITY_EDITOR` so Player build is untouched. |
| T2.3 | EditMode test — OnValidate fires broadcast | _pending_ | _pending_ | New `Assets/Tests/EditMode/UI/UiTheme_OnValidateTests.cs`: load test `UiTheme` + scene `ThemeBroadcaster` + one mock `IThemed`. Assert `ApplyTheme` called once after `EditorUtility.SetDirty` + `OnValidate` invocation. Covers Review Note D (Editor gate). |
| T2.4 | Compile-check + Player path | _pending_ | _pending_ | Run `npm run unity:compile-check` for Editor. Verify `#if !UNITY_EDITOR` compile path (Player build) has zero references to `ThemeBroadcaster` from `UiTheme.OnValidate`. Add a trivial `UNITY_EDITOR == false` conditional compile test in a smoke fixture if needed. Update `ia/specs/ui-design-system.md` §1.5 note that Editor-only broadcast exists. |

---
