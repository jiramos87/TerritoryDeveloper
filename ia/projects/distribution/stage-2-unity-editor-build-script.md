### Stage 2 — Unity build pipeline + versioning manifest / Unity editor build script

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Land `ReleaseBuilder.cs` under `Assets/Editor/` — the Unity batch-mode entry point that reads env vars, stamps `BuildInfo.asset`, updates `PlayerSettings.bundleVersion`, and runs `BuildPipeline.BuildPlayer` for each target. Editor-only code, no runtime impact.

**Exit:**

- `Assets/Editor/ReleaseBuilder.cs` exposes `public static void BuildMac()` + `public static void BuildWindows()`.
- Reads `BUILD_VERSION` / `BUILD_SHA` / `BUILD_TIMESTAMP` via `System.Environment.GetEnvironmentVariable`; fails with non-zero Unity exit on missing vars.
- `UpdateBuildInfoAsset(version, sha, timestamp)` helper loads `Assets/Resources/BuildInfo.asset` via `AssetDatabase.LoadAssetAtPath`, calls `WriteFields`, `EditorUtility.SetDirty`, `AssetDatabase.SaveAssets`.
- Writes `PlayerSettings.bundleVersion = version` before BuildPlayer invocation.
- Calls `BuildPipeline.BuildPlayer` with `BuildTarget.StandaloneOSX` (BuildMac) / `BuildTarget.StandaloneWindows64` (BuildWindows), output paths `Builds/mac/Territory.app` / `Builds/win/Territory.exe`.
- Local dry-run (manual invocation from Unity editor on a test semver) produces a `BuildInfo.asset` with correct fields + a built binary in `Builds/`.
- Phase 1 — Author ReleaseBuilder skeleton + env var reader + BuildInfo writer helper.
- Phase 2 — Wire platform-specific BuildPipeline invocations.
- Phase 3 — Local dry-run validation on both targets (mac in-repo; win documented).

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T2.1 | ReleaseBuilder skeleton + env reader | _pending_ | _pending_ | Author `Assets/Editor/ReleaseBuilder.cs` with `ReadEnv(string key)` helper that throws a descriptive exception when var missing, and top-level try/catch that `EditorApplication.Exit(1)` on any error so the shell script in Stage 1.3 propagates failure. |
| T2.2 | UpdateBuildInfoAsset helper | _pending_ | _pending_ | Add `UpdateBuildInfoAsset(string version, string sha, string timestamp)` in `ReleaseBuilder.cs` — `AssetDatabase.LoadAssetAtPath<BuildInfo>("Assets/Resources/BuildInfo.asset")`, call editor-gated `WriteFields`, `EditorUtility.SetDirty(asset)`, `AssetDatabase.SaveAssets()`, `AssetDatabase.Refresh()`. Fail loudly if asset missing (points the user at T1.1.2). |
| T2.3 | BuildMac + BuildWindows entry methods | _pending_ | _pending_ | Implement `public static void BuildMac()` + `public static void BuildWindows()` in `ReleaseBuilder.cs` — read env vars, call `UpdateBuildInfoAsset`, set `PlayerSettings.bundleVersion`, invoke `BuildPipeline.BuildPlayer` with the right `BuildPlayerOptions` (target, locationPathName `Builds/mac/Territory.app` / `Builds/win/Territory.exe`, `BuildOptions.None`, explicit scene list from `EditorBuildSettings.scenes`). Check `BuildReport.summary.result` and exit non-zero on failure. |
| T2.4 | Local dry-run validation | _pending_ | _pending_ | Run `BuildMac` once from the Unity editor menu with hand-set env vars (`BUILD_VERSION=0.0.0-dev-test`, `BUILD_SHA=abc1234`, `BUILD_TIMESTAMP=...`); confirm `Assets/Resources/BuildInfo.asset` updated + `Builds/mac/Territory.app` produced. Capture command + output in a scratch note (eventually lands in Stage 2.3 trainable skill). Document the Windows machine invocation (cannot run locally) as a placeholder for Stage 2.1. |
