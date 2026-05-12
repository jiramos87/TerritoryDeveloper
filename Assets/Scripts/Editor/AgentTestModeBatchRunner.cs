#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Domains.Testing.Dto;
using Domains.Testing.Services;
using Territory.Core;
using Territory.Economy;
using Territory.Persistence;
using Territory.Simulation;
using Territory.Terrain;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Territory.Testing
{
    /// <summary>
    /// Editor -batchmode entry. Stage 6.2 thin hub: Game-type Play Mode pump only.
    /// Parse + report helpers → <see cref="TestModeBatchService"/>.
    /// State IO → <see cref="BatchStateService"/>. Golden compare → <see cref="GoldenCompareService"/>.
    /// </summary>
    public static class AgentTestModeBatchRunner
    {
        public const string ExecuteMethodName       = "Territory.Testing.AgentTestModeBatchRunner.Run";
        public const string ArgSimulationTicks      = "-testSimulationTicks";
        public const string ArgGoldenPath           = "-testGoldenPath";
        public const string ArgNewGame              = "-testNewGame";
        public const string ArgTestSeed             = "-testSeed";
        public const int    ExitCodeGoldenMismatch      = 8;
        public const int    ExitCodeHeightIntegrityFail = 9;
        public const string CityScenePath           = "Assets/Scenes/CityScene.unity";

        const double GridWaitMaxSeconds     = 120.0;
        const double ExitPlayWaitMaxSeconds = 90.0;
        enum BatchPhase { WaitGrid = 1, WaitStopped = 2 }

        static string Root => ScenarioPathResolver.GetRepositoryRoot();

        [InitializeOnLoadMethod]
        static void RegisterAfterDomainReload()
        {
            if (!File.Exists(BatchStateService.GetStateFilePath(Root))) return;
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.update += OnEditorUpdate;
        }

        public static void Run()
        {
            if (!TestModeSecurity.IsTestModeEntryAllowed) { FailImmediate(4, "Test mode not allowed."); return; }
            string[] args    = Environment.GetCommandLineArgs();
            bool newGameMode = TestModeBatchService.ParseNewGameFlag(args);
            int testSeed     = TestModeBatchService.ParseTestSeed(args);

            if (newGameMode)
            {
                TestModeCommandLineBootstrap.TryParse(args, out string ngId, out _);
                string gpNg = TestModeBatchService.ParseTestGoldenPath(args);
                if (!string.IsNullOrEmpty(gpNg)) { gpNg = Path.GetFullPath(gpNg); if (!File.Exists(gpNg)) { FailImmediate(4, $"Golden not found: {gpNg}"); return; } }
                try { EditorSceneManager.OpenScene(CityScenePath, OpenSceneMode.Single); } catch (Exception ex) { FailImmediate(4, $"Scene open failed: {ex.Message}"); return; }
                var ng = new AgentTestModeBatchStateDto { phase = (int)BatchPhase.WaitGrid, scenario_id = ngId ?? "", golden_path = gpNg ?? "", ticks_requested = TestModeBatchService.ParseSimulationTicks(args), started_utc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture), new_game_mode = true, test_seed = testSeed };
                BatchStateService.WriteState(Root, ng);
                EditorApplication.EnterPlaymode();
                return;
            }

            if (!TestModeCommandLineBootstrap.TryParse(args, out string sid, out string sp)) { FailImmediate(4, "Missing -testScenarioId/-testScenarioPath."); return; }
            if (string.IsNullOrEmpty(sp) || !File.Exists(sp)) { FailImmediate(4, $"Save not found: {sp}"); return; }
            string gp = TestModeBatchService.ParseTestGoldenPath(args);
            if (!string.IsNullOrEmpty(gp)) { gp = Path.GetFullPath(gp); if (!File.Exists(gp)) { FailImmediate(4, $"Golden not found: {gp}"); return; } }
            try { EditorSceneManager.OpenScene(CityScenePath, OpenSceneMode.Single); } catch (Exception ex) { FailImmediate(4, $"Scene open failed: {ex.Message}"); return; }
            var st = new AgentTestModeBatchStateDto { phase = (int)BatchPhase.WaitGrid, save_path = sp, scenario_id = sid ?? "", golden_path = gp ?? "", ticks_requested = TestModeBatchService.ParseSimulationTicks(args), started_utc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture) };
            BatchStateService.WriteState(Root, st);
            EditorApplication.EnterPlaymode();
        }

        static void OnEditorUpdate()
        {
            try
            {
                if (!BatchStateService.TryReadState(Root, out var s)) { EditorApplication.update -= OnEditorUpdate; return; }
                if (s.phase == (int)BatchPhase.WaitGrid)    PumpWaitGrid(s);
                else if (s.phase == (int)BatchPhase.WaitStopped) PumpWaitStopped(s);
            }
            catch (Exception ex) { Debug.LogError($"[AgentTestModeBatch] {ex.Message}"); BeginExitSequence(6, ex.Message); }
        }

        static void PumpWaitGrid(AgentTestModeBatchStateDto s)
        {
            if (!EditorApplication.isPlaying) { BeginExitSequence(6, "Play Mode not active."); return; }
            if (!BatchStateService.TryParseStartedUtc(s.started_utc, out DateTime t0)) t0 = DateTime.UtcNow;
            if ((DateTime.UtcNow - t0).TotalSeconds > GridWaitMaxSeconds) { BeginExitSequence(6, "GridManager init timeout."); return; }
            var grid = UnityEngine.Object.FindObjectOfType<GridManager>();
            if (grid == null || !grid.isInitialized) return;
            try
            {
                var saveMgr = UnityEngine.Object.FindObjectOfType<GameSaveManager>();
                if (saveMgr == null) { BeginExitSequence(6, "GameSaveManager not found."); return; }
                if (s.new_game_mode) NeighborStubSmokeDriver.RunNewGameSmoke(saveMgr, grid, s.test_seed);
                else saveMgr.LoadGame(s.save_path);
                HeightMap hm = grid.terrainManager?.GetOrCreateHeightMap();
                var swL = hm != null ? HeightIntegritySweep(grid, hm) : null;
                int applied = 0;
                var simMgr = UnityEngine.Object.FindObjectOfType<SimulationManager>();
                if (simMgr != null && s.ticks_requested > 0) for (int i = 0; i < s.ticks_requested; i++) { simMgr.ProcessSimulationTick(); applied++; }
                var swT = (applied > 0 && hm != null) ? HeightIntegritySweep(grid, hm) : null;
                s.height_integrity_json     = JsonUtility.ToJson(new HeightIntegrityDto { post_load = swL ?? new HeightIntegritySweepResultDto(), post_tick = swT ?? new HeightIntegritySweepResultDto() }, false);
                s.ticks_applied             = applied;
                var snap    = BuildCitySnapshot(UnityEngine.Object.FindObjectOfType<CityStats>(), applied, grid);
                var nSnap   = BuildNeighborStubSnapshot(saveMgr);
                if (snap  != null) s.city_stats_snapshot_json     = JsonUtility.ToJson(snap, false);
                if (nSnap != null) s.neighbor_stubs_snapshot_json  = JsonUtility.ToJson(nSnap, false);
                if (s.new_game_mode) { var smoke = NeighborStubSmokeDriver.RunSmokeAssertions(saveMgr, grid); s.neighbor_stub_smoke_json = JsonUtility.ToJson(smoke, false); if (!smoke.assertions_passed && s.exit_code == 0) { s.exit_code = ExitCodeGoldenMismatch; s.error = $"Smoke: {smoke.failure_detail}"; } }
                ApplyGoldenCheck(s, snap, nSnap, swL, swT);
            }
            catch (Exception ex) { Debug.LogError($"[AgentTestModeBatch] {ex}"); s.exit_code = 6; s.error = ex.Message; BatchStateService.WriteState(Root, s); TestModeBatchService.TryWriteReportFromState(Root, s, false, ex.Message); }
            s.phase = (int)BatchPhase.WaitStopped;
            s.started_utc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
            BatchStateService.WriteState(Root, s);
            EditorApplication.ExitPlaymode();
        }

        static void ApplyGoldenCheck(AgentTestModeBatchStateDto s, AgentTestModeBatchCitySnapshotDto snap, NeighborStubRoundtripGoldenDto nSnap, HeightIntegritySweepResultDto swL, HeightIntegritySweepResultDto swT)
        {
            s.golden_checked = !string.IsNullOrEmpty(s.golden_path); s.golden_matched = true; s.golden_diff = string.Empty;
            if (s.golden_checked)
            {
                bool ok; string diff;
                if (GoldenCompareService.IsNeighborStubGolden(s.golden_path)) { if (nSnap == null) { ok = false; diff = "GameSaveManager not found."; } else ok = GoldenCompareService.TryCompareNeighborStubGolden(s.golden_path, nSnap, out diff); }
                else { if (snap == null) { ok = false; diff = "CityStats not found."; } else ok = GoldenCompareService.TryCompareGolden(s.golden_path, snap, s.ticks_requested, out diff); }
                if (!ok) { s.golden_matched = false; s.golden_diff = diff ?? "mismatch"; s.exit_code = ExitCodeGoldenMismatch; s.error = diff; BatchStateService.WriteState(Root, s); TestModeBatchService.TryWriteReportFromState(Root, s, false, diff); return; }
            }
            bool hiViol = (swL?.violations > 0) || (swT?.violations > 0);
            if (hiViol) { s.exit_code = ExitCodeHeightIntegrityFail; s.error = "HeightMap integrity violation."; }
            else { s.exit_code = 0; s.error = string.Empty; }
            BatchStateService.WriteState(Root, s);
            TestModeBatchService.TryWriteReportFromState(Root, s, !hiViol, hiViol ? s.error : null);
        }

        static void PumpWaitStopped(AgentTestModeBatchStateDto s)
        {
            if (EditorApplication.isPlaying)
            {
                if (!BatchStateService.TryParseStartedUtc(s.started_utc, out DateTime t)) t = DateTime.UtcNow;
                if ((DateTime.UtcNow - t).TotalSeconds > ExitPlayWaitMaxSeconds) { Debug.LogError("[AgentTestModeBatch] Timeout waiting for stop."); int c = s.exit_code; FinishAndExitEditor(c != 0 ? c : 7); }
                return;
            }
            FinishAndExitEditor(s.exit_code);
        }

        static void BeginExitSequence(int code, string error)
        {
            Debug.LogError($"[AgentTestModeBatch] {error}");
            if (BatchStateService.TryReadState(Root, out var s)) { s.exit_code = code; s.error = error ?? string.Empty; BatchStateService.WriteState(Root, s); TestModeBatchService.TryWriteReportFromState(Root, s, false, error); }
            else TestModeBatchService.TryWriteReportImmediate(Root, false, error, code);
            if (EditorApplication.isPlaying) { if (BatchStateService.TryReadState(Root, out var st)) { st.phase = (int)BatchPhase.WaitStopped; st.started_utc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture); BatchStateService.WriteState(Root, st); } EditorApplication.ExitPlaymode(); }
            else FinishAndExitEditor(code);
        }

        static void FinishAndExitEditor(int code) { BatchStateService.DeleteStateFile(Root); EditorApplication.update -= OnEditorUpdate; EditorApplication.Exit(code); }
        static void FailImmediate(int code, string error) { Debug.LogError($"[AgentTestModeBatch] {error}"); TestModeBatchService.TryWriteReportImmediate(Root, false, error, code); EditorApplication.Exit(code); }

        // ── Game-type helpers (require TerritoryDeveloper.Game assembly; cannot move to Testing.Editor) ──

        static AgentTestModeBatchCitySnapshotDto BuildCitySnapshot(CityStats cs, int applied, GridManager grid)
        {
            if (cs == null) return null;
            return new AgentTestModeBatchCitySnapshotDto { schema_version = 2, simulation_ticks = applied, population = cs.population, money = cs.money, roadCount = cs.roadCount, grassCount = cs.grassCount, residentialZoneCount = cs.residentialZoneCount, commercialZoneCount = cs.commercialZoneCount, industrialZoneCount = cs.industrialZoneCount, residentialBuildingCount = cs.residentialBuildingCount, commercialBuildingCount = cs.commercialBuildingCount, industrialBuildingCount = cs.industrialBuildingCount, forestCellCount = cs.forestCellCount, regionId = grid?.ParentRegionId ?? "", countryId = grid?.ParentCountryId ?? "" };
        }

        static NeighborStubRoundtripGoldenDto BuildNeighborStubSnapshot(GameSaveManager mgr)
        {
            if (mgr == null) return null;
            var stubs = new List<NeighborStubGoldenEntry>(); foreach (var s in mgr.NeighborStubs) stubs.Add(new NeighborStubGoldenEntry { id = s.id ?? "", displayName = s.displayName ?? "", borderSide = s.borderSide.ToString() }); stubs.Sort((a, b) => string.Compare(a.id, b.id, StringComparison.Ordinal));
            var bindings = new List<NeighborBindingGoldenEntry>(); if (mgr.neighborCityBindings != null) foreach (var b in mgr.neighborCityBindings) { string bs = ""; foreach (var s in mgr.NeighborStubs) if (s.id == b.stubId) { bs = s.borderSide.ToString(); break; } bindings.Add(new NeighborBindingGoldenEntry { stubId = b.stubId ?? "", exitCellX = b.exitCellX, exitCellY = b.exitCellY, borderSide = bs }); }
            bindings.Sort((a, b) => { int c = string.Compare(a.stubId, b.stubId, StringComparison.Ordinal); if (c != 0) return c; c = a.exitCellX.CompareTo(b.exitCellX); return c != 0 ? c : a.exitCellY.CompareTo(b.exitCellY); });
            return new NeighborStubRoundtripGoldenDto { schema_version = 1, neighborStubs = stubs.ToArray(), neighborCityBindings = bindings.ToArray() };
        }

        static HeightIntegritySweepResultDto HeightIntegritySweep(GridManager grid, HeightMap hm)
        {
            const int Max = 10; int w = grid.width, h = grid.height, checked_ = 0, viol = 0; var off = new List<HeightIntegrityViolationDto>(Max);
            for (int x = 0; x < w; x++) for (int y = 0; y < h; y++) { var cell = grid.GetCell<CityCell>(x, y); if (cell == null) continue; checked_++; int hmH = hm.GetHeight(x, y); if (hmH != cell.height) { viol++; if (off.Count < Max) off.Add(new HeightIntegrityViolationDto { x = x, y = y, heightMap = hmH, cell = cell.height }); } }
            return new HeightIntegritySweepResultDto { checked_cells = checked_, violations = viol, first_offenders = off.ToArray() };
        }
    }
}
#endif
