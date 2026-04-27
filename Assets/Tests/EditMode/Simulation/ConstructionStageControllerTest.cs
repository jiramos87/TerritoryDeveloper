using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Text.RegularExpressions;
using Territory.Core;
using Territory.Simulation;
using Territory.Simulation.Signals;
using Territory.Zones;

namespace Territory.Tests.EditMode.Simulation
{
    /// <summary>
    /// TECH-2412 — Stage 10 (city-sim-depth) parity coverage for
    /// <see cref="ConstructionStageController"/>. Locks five invariants over a fixed
    /// 10×10 fixture:
    /// (1) parity: R-medium at desirability=0.6 reaches stage 3 in 19±1 ticks
    ///     (formula = <c>Mathf.CeilToInt(20 / (0.5 + 0.6)) = 19</c>),
    /// (2) edge-low: desirability=0 → effectiveTime=40 days; reach within 40±1,
    /// (3) edge-high: desirability=1 → effectiveTime≈13.33 days; reach within 13±1,
    /// (4) sprite-fallback: no NRE + warning logged when no construction art present,
    /// (5) multi-cell: only pivot tracks stage; non-pivot cells stay at stage 0.
    /// Direct formula authority per T10.1 §Pending Decisions LOCKED (per-stage <c>/4</c>).
    /// </summary>
    [TestFixture]
    public class ConstructionStageControllerTest
    {
        private const int GRID = 10;

        private GameObject gridGO;
        private GameObject districtGO;
        private GameObject centroidGO;
        private GameObject composerGO;
        private GameObject controllerGO;

        private GridManager grid;
        private DesirabilityComposer composer;
        private ConstructionStageController controller;
        private ConstructionCurveTable curveTable;
        private SignalTuningWeightsAsset weightsAsset;

        [SetUp]
        public void SetUp()
        {
            // 1. GridManager — 10×10 default-zoned CityCells.
            gridGO = new GameObject("CSCT_GridManager");
            grid = gridGO.AddComponent<GridManager>();
            grid.width = GRID;
            grid.height = GRID;
            grid.tileWidth = 1f;
            grid.tileHeight = 0.5f;
            grid.cellArray = new CityCell[GRID, GRID];
            for (int x = 0; x < GRID; x++)
            {
                for (int y = 0; y < GRID; y++)
                {
                    GameObject cellGO = new GameObject($"Cell_{x}_{y}");
                    cellGO.transform.parent = gridGO.transform;
                    CityCell cell = cellGO.AddComponent<CityCell>();
                    cell.x = x;
                    cell.y = y;
                    cell.zoneType = Zone.ZoneType.None;
                    grid.cellArray[x, y] = cell;
                }
            }
            grid.isInitialized = true;

            // 2. UrbanCentroidService + DistrictManager (composer dependency).
            centroidGO = new GameObject("CSCT_UrbanCentroidService");
            UrbanCentroidService centroid = centroidGO.AddComponent<UrbanCentroidService>();
            centroid.gridManager = grid;
            centroid.RecalculateFromGrid();

            districtGO = new GameObject("CSCT_DistrictManager");
            DistrictManager districtManager = districtGO.AddComponent<DistrictManager>();
            FieldInfo dmCentroidField = typeof(DistrictManager).GetField(
                "centroid", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo dmGridField = typeof(DistrictManager).GetField(
                "grid", BindingFlags.NonPublic | BindingFlags.Instance);
            dmCentroidField.SetValue(districtManager, centroid);
            dmGridField.SetValue(districtManager, grid);
            MethodInfo dmAwake = typeof(DistrictManager).GetMethod(
                "Awake", BindingFlags.NonPublic | BindingFlags.Instance);
            dmAwake.Invoke(districtManager, null);

            // 3. DesirabilityComposer — real instance with reflective Start to alloc cell array.
            weightsAsset = ScriptableObject.CreateInstance<SignalTuningWeightsAsset>();
            composerGO = new GameObject("CSCT_DesirabilityComposer");
            composer = composerGO.AddComponent<DesirabilityComposer>();
            InjectField(composer, "gridManager", grid);
            InjectField(composer, "districtManager", districtManager);
            InjectField(composer, "weights", weightsAsset);
            MethodInfo composerStart = typeof(DesirabilityComposer).GetMethod(
                "Start", BindingFlags.NonPublic | BindingFlags.Instance);
            composerStart.Invoke(composer, null);

            // 4. ConstructionCurveTable — 12 default rows matching Resources/Construction/ConstructionCurveTable.asset.
            curveTable = ScriptableObject.CreateInstance<ConstructionCurveTable>();
            SeedCurveTable(curveTable);

            // 5. ConstructionStageController.
            controllerGO = new GameObject("CSCT_ConstructionStageController");
            controller = controllerGO.AddComponent<ConstructionStageController>();
            InjectField(controller, "desirabilityComposer", composer);
            InjectField(controller, "curveTable", curveTable);
            InjectField(controller, "gridManager", grid);
            // Force Awake to subscribe sprite-swap handler.
            MethodInfo controllerAwake = typeof(ConstructionStageController).GetMethod(
                "Awake", BindingFlags.NonPublic | BindingFlags.Instance);
            controllerAwake.Invoke(controller, null);
        }

        [TearDown]
        public void TearDown()
        {
            if (controllerGO != null) Object.DestroyImmediate(controllerGO);
            if (composerGO != null) Object.DestroyImmediate(composerGO);
            if (districtGO != null) Object.DestroyImmediate(districtGO);
            if (centroidGO != null) Object.DestroyImmediate(centroidGO);
            if (gridGO != null) Object.DestroyImmediate(gridGO);
            if (curveTable != null) Object.DestroyImmediate(curveTable);
            if (weightsAsset != null) Object.DestroyImmediate(weightsAsset);
        }

        [Test]
        public void BeginConstruction_RMediumDesirability06_ReachesStage3WithinBound()
        {
            // R-medium baseTime=20; desirability=0.6 → effectiveTime = 20 / (0.5+0.6) = 18.18;
            // per-stage threshold = 4.545; ticks-per-stage = ceil(4.545) = 5; total = 5+5+5+5 = 20 ticks max.
            // formula = Mathf.CeilToInt(20 / 1.1) = 19 ticks ± 1 tolerance.
            CityCell pivot = grid.GetCell(5, 5);
            pivot.isPivot = true;
            pivot.zoneType = Zone.ZoneType.ResidentialMediumBuilding;
            ForceDesirability(0.6f);

            controller.BeginConstruction(pivot, Zone.ZoneType.ResidentialMediumBuilding);

            int stage3Tick = TickUntilStage3(maxTicks: 25);
            Assert.AreEqual(3, pivot.constructionStage,
                $"Pivot did not reach stage 3 within 25 ticks. Final stage={pivot.constructionStage}, accumulator={pivot.constructionDayAccumulator}");
            Assert.That(stage3Tick, Is.InRange(18, 21),
                $"R-medium d=0.6 stage-3 reach tick {stage3Tick} outside expected [18,21] (formula center 19±1+rounding tolerance).");
        }

        [Test]
        public void BeginConstruction_DesirabilityZero_ReachesStage3WithinBound()
        {
            // R-medium baseTime=20; desirability=0 → effectiveTime = 20 / 0.5 = 40;
            // per-stage threshold = 10; total = 40 ticks. Tolerance ±2 for accumulator rounding.
            CityCell pivot = grid.GetCell(5, 5);
            pivot.isPivot = true;
            pivot.zoneType = Zone.ZoneType.ResidentialMediumBuilding;
            ForceDesirability(0f);

            controller.BeginConstruction(pivot, Zone.ZoneType.ResidentialMediumBuilding);

            int stage3Tick = TickUntilStage3(maxTicks: 50);
            Assert.AreEqual(3, pivot.constructionStage,
                $"Pivot did not reach stage 3 within 50 ticks. Final stage={pivot.constructionStage}");
            Assert.That(stage3Tick, Is.InRange(38, 42),
                $"R-medium d=0 stage-3 reach tick {stage3Tick} outside [38,42] (formula center 40±2).");
        }

        [Test]
        public void BeginConstruction_DesirabilityOne_ReachesStage3WithinBound()
        {
            // R-medium baseTime=20; desirability=1 → effectiveTime = 20 / 1.5 = 13.33;
            // per-stage threshold = 3.33; ticks-per-stage = ceil(3.33) = 4; total = 4+4+4+4 = 16 ticks max
            // (or earlier when accumulator >= 3.33 at tick 4). formula bound: 13±2 tolerance.
            CityCell pivot = grid.GetCell(5, 5);
            pivot.isPivot = true;
            pivot.zoneType = Zone.ZoneType.ResidentialMediumBuilding;
            ForceDesirability(1f);

            controller.BeginConstruction(pivot, Zone.ZoneType.ResidentialMediumBuilding);

            int stage3Tick = TickUntilStage3(maxTicks: 25);
            Assert.AreEqual(3, pivot.constructionStage,
                $"Pivot did not reach stage 3 within 25 ticks. Final stage={pivot.constructionStage}");
            Assert.That(stage3Tick, Is.InRange(13, 17),
                $"R-medium d=1 stage-3 reach tick {stage3Tick} outside [13,17] (formula center 13.33±2).");
        }

        [Test]
        public void BeginConstruction_NoConstructionArt_GracefulSpriteFallback()
        {
            // No Resources/Buildings/Construction art present → expect once-per-key warning,
            // no NRE. BeginConstruction fires OnStageBoundary(cell, 0) which calls
            // HandleStageBoundary; both Resources.Load calls return null; warning logged;
            // graceful skip when occupiedBuilding is null (which it is in EditMode fixture).
            CityCell pivot = grid.GetCell(5, 5);
            pivot.isPivot = true;
            pivot.zoneType = Zone.ZoneType.ResidentialMediumBuilding;
            ForceDesirability(0.6f);

            // Reset dedup so the warning fires for this test run (other tests may have run).
            ResetSpriteWarningDedup();

            LogAssert.Expect(LogType.Warning, new Regex("missing sprite for ResidentialMediumBuilding stage 0"));
            Assert.DoesNotThrow(() => controller.BeginConstruction(pivot, Zone.ZoneType.ResidentialMediumBuilding),
                "BeginConstruction threw on missing construction art — should be graceful skip");
        }

        [Test]
        public void BeginConstruction_MultiCellBuilding_OnlyPivotTracksStage()
        {
            // 2×2 R-heavy at (5,5) pivot; non-pivot cells at (5,6), (6,5), (6,6) keep stage 0
            // after 30 ticks. Pivot reaches stage 3 within bound for d=0.6 baseTime=30
            // (effectiveTime = 30/1.1 ≈ 27.27, per-stage = 6.81, total ≈ 28 ticks).
            CityCell pivot = grid.GetCell(5, 5);
            pivot.isPivot = true;
            pivot.zoneType = Zone.ZoneType.ResidentialHeavyBuilding;
            // Non-pivot cells of the 2×2 building share zoneType but isPivot=false.
            CityCell np1 = grid.GetCell(5, 6); np1.isPivot = false; np1.zoneType = Zone.ZoneType.ResidentialHeavyBuilding;
            CityCell np2 = grid.GetCell(6, 5); np2.isPivot = false; np2.zoneType = Zone.ZoneType.ResidentialHeavyBuilding;
            CityCell np3 = grid.GetCell(6, 6); np3.isPivot = false; np3.zoneType = Zone.ZoneType.ResidentialHeavyBuilding;
            ForceDesirability(0.6f);

            controller.BeginConstruction(pivot, Zone.ZoneType.ResidentialHeavyBuilding);

            // Tick 30 days — covers full progression for R-heavy d=0.6.
            for (int i = 0; i < 30; i++)
            {
                controller.ProcessTick();
            }

            Assert.AreEqual(0, np1.constructionStage,
                $"Non-pivot (5,6) drifted from stage 0 — actual={np1.constructionStage}");
            Assert.AreEqual(0, np2.constructionStage,
                $"Non-pivot (6,5) drifted from stage 0 — actual={np2.constructionStage}");
            Assert.AreEqual(0, np3.constructionStage,
                $"Non-pivot (6,6) drifted from stage 0 — actual={np3.constructionStage}");
            Assert.AreEqual(0f, np1.constructionDayAccumulator,
                $"Non-pivot (5,6) accumulator drifted from 0 — actual={np1.constructionDayAccumulator}");
        }

        // --- helpers ---

        private static void InjectField(MonoBehaviour target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(
                name, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, $"{target.GetType().Name}.{name} field missing");
            field.SetValue(target, value);
        }

        /// <summary>
        /// Force-write the composer's private <c>_cellDesirability</c> array to a uniform
        /// scalar so tests are independent of producer/scheduler wiring.
        /// </summary>
        private void ForceDesirability(float value)
        {
            FieldInfo field = typeof(DesirabilityComposer).GetField(
                "_cellDesirability", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, "DesirabilityComposer._cellDesirability field missing");
            float[] arr = (float[])field.GetValue(composer);
            for (int i = 0; i < arr.Length; i++)
            {
                arr[i] = value;
            }
        }

        /// <summary>Tick controller until pivot at (5,5) reaches stage 3, return tick count.</summary>
        private int TickUntilStage3(int maxTicks)
        {
            CityCell pivot = grid.GetCell(5, 5);
            for (int t = 1; t <= maxTicks; t++)
            {
                controller.ProcessTick();
                if (pivot.constructionStage >= 3)
                {
                    return t;
                }
            }
            return maxTicks + 1;
        }

        private void SeedCurveTable(ConstructionCurveTable table)
        {
            FieldInfo rowsField = typeof(ConstructionCurveTable).GetField(
                "rows", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(rowsField, "ConstructionCurveTable.rows field missing");
            System.Collections.Generic.List<ConstructionCurveTable.CurveRow> rows
                = new System.Collections.Generic.List<ConstructionCurveTable.CurveRow>
            {
                new ConstructionCurveTable.CurveRow { zoneType = Zone.ZoneType.ResidentialLightBuilding, baseTime = 15f },
                new ConstructionCurveTable.CurveRow { zoneType = Zone.ZoneType.ResidentialMediumBuilding, baseTime = 20f },
                new ConstructionCurveTable.CurveRow { zoneType = Zone.ZoneType.ResidentialHeavyBuilding, baseTime = 30f },
                new ConstructionCurveTable.CurveRow { zoneType = Zone.ZoneType.CommercialLightBuilding, baseTime = 15f },
                new ConstructionCurveTable.CurveRow { zoneType = Zone.ZoneType.CommercialMediumBuilding, baseTime = 22f },
                new ConstructionCurveTable.CurveRow { zoneType = Zone.ZoneType.CommercialHeavyBuilding, baseTime = 32f },
                new ConstructionCurveTable.CurveRow { zoneType = Zone.ZoneType.IndustrialLightBuilding, baseTime = 18f },
                new ConstructionCurveTable.CurveRow { zoneType = Zone.ZoneType.IndustrialMediumBuilding, baseTime = 25f },
                new ConstructionCurveTable.CurveRow { zoneType = Zone.ZoneType.IndustrialHeavyBuilding, baseTime = 35f },
                new ConstructionCurveTable.CurveRow { zoneType = Zone.ZoneType.StateServiceLightBuilding, baseTime = 18f },
                new ConstructionCurveTable.CurveRow { zoneType = Zone.ZoneType.StateServiceMediumBuilding, baseTime = 25f },
                new ConstructionCurveTable.CurveRow { zoneType = Zone.ZoneType.StateServiceHeavyBuilding, baseTime = 35f },
            };
            rowsField.SetValue(table, rows);
        }

        private static void ResetSpriteWarningDedup()
        {
            FieldInfo field = typeof(ConstructionStageController).GetField(
                "_warnedMissingSpriteKeys", BindingFlags.NonPublic | BindingFlags.Static);
            if (field != null)
            {
                System.Collections.Generic.HashSet<string> set
                    = (System.Collections.Generic.HashSet<string>)field.GetValue(null);
                set?.Clear();
            }
        }
    }
}
