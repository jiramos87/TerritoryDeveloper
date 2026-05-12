using NUnit.Framework;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace Territory.Tests.EditMode.Atomization.Stage4_4
{
    /// <summary>
    /// §Red-Stage Proof anchor: WaterManagerThinSpec.cs::water_manager_is_thin
    /// Stage 4.4: WaterManager Tier-B THIN — WaterManager.cs ≤200 LOC; publics delegated to WaterService.
    /// Green: WaterManager.cs ≤200 LOC AND WaterService.cs exists AND hub delegates via _water field.
    /// </summary>
    public class WaterManagerThinSpec
    {
        private const string WaterManagerPath =
            "Assets/Scripts/Managers/GameManagers/WaterManager.cs";

        private const string WaterServicePath =
            "Assets/Scripts/Domains/Water/Services/WaterService.cs";

        [Test]
        public void water_manager_is_thin()
        {
            string repoRoot = GetRepoRoot();

            // Assert 1: WaterManager.cs ≤200 LOC
            string hubPath = Path.Combine(repoRoot, WaterManagerPath);
            Assert.IsTrue(File.Exists(hubPath), $"WaterManager.cs not found at {hubPath}");
            int lineCount = File.ReadAllLines(hubPath).Length;
            Assert.LessOrEqual(lineCount, 200,
                $"WaterManager.cs must be ≤200 LOC after THIN. Got {lineCount} lines.");

            // Assert 2: WaterService.cs exists under Domains/Water/Services/
            string svcPath = Path.Combine(repoRoot, WaterServicePath);
            Assert.IsTrue(File.Exists(svcPath),
                $"WaterService.cs must exist at {svcPath}.");

            // Assert 3: hub delegates to WaterService (_water field present)
            string hubSource = File.ReadAllText(hubPath);
            Assert.IsTrue(hubSource.Contains("WaterService"),
                "WaterManager hub must reference WaterService.");
            Assert.IsTrue(hubSource.Contains("_water"),
                "WaterManager hub must hold a _water delegate field.");

            // Assert 4: locked fields still present (invariant #3)
            Assert.IsTrue(hubSource.Contains("public GridManager gridManager"),
                "WaterManager hub must retain public GridManager gridManager field (locked #3).");
            Assert.IsTrue(hubSource.Contains("public TerrainManager terrainManager"),
                "WaterManager hub must retain public TerrainManager terrainManager field (locked #3).");
            Assert.IsTrue(hubSource.Contains("public ZoneManager zoneManager"),
                "WaterManager hub must retain public ZoneManager zoneManager field (locked #3).");
        }

        [Test]
        public void water_service_is_in_correct_namespace()
        {
            Type t = Type.GetType("Domains.Water.Services.WaterService, Assembly-CSharp");
            Assert.IsNotNull(t, "WaterService must be loadable from Assembly-CSharp");
            Assert.AreEqual("Domains.Water.Services", t.Namespace,
                $"WaterService namespace mismatch: {t.Namespace}");
        }

        [Test]
        public void water_service_exposes_production_accounting()
        {
            Type t = Type.GetType("Domains.Water.Services.WaterService, Assembly-CSharp");
            Assert.IsNotNull(t, "WaterService must exist");
            Assert.IsNotNull(t.GetMethod("RegisterWaterProduction"), "WaterService must expose RegisterWaterProduction");
            Assert.IsNotNull(t.GetMethod("UnregisterWaterProduction"), "WaterService must expose UnregisterWaterProduction");
            Assert.IsNotNull(t.GetMethod("ResetWaterOutput"), "WaterService must expose ResetWaterOutput");
            Assert.IsNotNull(t.GetMethod("GetTotalWaterOutput"), "WaterService must expose GetTotalWaterOutput");
        }

        [Test]
        public void water_service_exposes_consumption_accounting()
        {
            Type t = Type.GetType("Domains.Water.Services.WaterService, Assembly-CSharp");
            Assert.IsNotNull(t, "WaterService must exist");
            Assert.IsNotNull(t.GetMethod("AddWaterConsumption"), "WaterService must expose AddWaterConsumption");
            Assert.IsNotNull(t.GetMethod("RemoveWaterConsumption"), "WaterService must expose RemoveWaterConsumption");
            Assert.IsNotNull(t.GetMethod("GetTotalWaterConsumption"), "WaterService must expose GetTotalWaterConsumption");
            Assert.IsNotNull(t.GetMethod("GetCityWaterAvailability"), "WaterService must expose GetCityWaterAvailability");
        }

        [Test]
        public void water_service_accounting_correct()
        {
            var svc = new Domains.Water.Services.WaterService();
            svc.AddWaterConsumption(10);
            svc.AddWaterConsumption(5);
            Assert.AreEqual(15, svc.GetTotalWaterConsumption(),
                "GetTotalWaterConsumption must return 15 after two Add calls.");
            svc.RemoveWaterConsumption(3);
            Assert.AreEqual(12, svc.GetTotalWaterConsumption(),
                "GetTotalWaterConsumption must return 12 after Remove(3).");
            svc.RegisterWaterProduction(50);
            Assert.AreEqual(50, svc.GetTotalWaterOutput(),
                "GetTotalWaterOutput must return 50 after RegisterWaterProduction(50).");
            Assert.IsTrue(svc.GetCityWaterAvailability(),
                "Water available when output(50) > consumption(12).");
        }

        [Test]
        public void water_service_no_tier_e_needed()
        {
            string repoRoot = GetRepoRoot();
            string svcPath = Path.Combine(repoRoot, WaterServicePath);
            Assert.IsTrue(File.Exists(svcPath), $"WaterService.cs not found at {svcPath}");
            int lineCount = File.ReadAllLines(svcPath).Length;
            Assert.LessOrEqual(lineCount, 500,
                $"WaterService.cs is {lineCount} LOC — if >500 a Tier-E sub-split must be scheduled.");
        }

        private static string GetRepoRoot()
        {
            string dir = Application.dataPath;
            while (!string.IsNullOrEmpty(dir))
            {
                if (File.Exists(Path.Combine(dir, "CLAUDE.md")))
                    return dir;
                string parent = Path.GetDirectoryName(dir);
                if (parent == dir) break;
                dir = parent;
            }
            return Application.dataPath.Replace("/Assets", "");
        }
    }
}
