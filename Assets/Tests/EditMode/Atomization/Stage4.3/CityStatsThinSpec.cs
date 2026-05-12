using NUnit.Framework;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace Territory.Tests.EditMode.Atomization.Stage4_3
{
    /// <summary>
    /// §Red-Stage Proof anchor: CityStatsThinSpec.cs::city_stats_is_thin
    /// Stage 4.3: CityStats Tier-B THIN — hub collapses to ≤200 LOC; 90 publics delegated to CityStatsService.
    /// Green: CityStats.cs ≤200 LOC AND CityStatsService.cs exists at Domains/Economy/Services/ AND hub delegates via service.
    /// </summary>
    public class CityStatsThinSpec
    {
        private const string CityStatsPath =
            "Assets/Scripts/Managers/GameManagers/CityStats.cs";

        private const string CityStatsServicePath =
            "Assets/Scripts/Domains/Economy/Services/CityStatsService.cs";

        [Test]
        public void city_stats_is_thin()
        {
            string repoRoot = GetRepoRoot();

            // Assert 1: CityStats.cs ≤200 LOC
            string hubPath = Path.Combine(repoRoot, CityStatsPath);
            Assert.IsTrue(File.Exists(hubPath), $"CityStats.cs not found at {hubPath}");
            int lineCount = File.ReadAllLines(hubPath).Length;
            Assert.LessOrEqual(lineCount, 200,
                $"CityStats.cs must be ≤200 LOC after THIN. Got {lineCount} lines.");

            // Assert 2: CityStatsService.cs exists under Domains/Economy/Services/
            string svcPath = Path.Combine(repoRoot, CityStatsServicePath);
            Assert.IsTrue(File.Exists(svcPath),
                $"CityStatsService.cs must exist at {svcPath}.");

            // Assert 3: hub delegates to CityStatsService (_svc field present)
            string hubSource = File.ReadAllText(hubPath);
            Assert.IsTrue(hubSource.Contains("CityStatsService"),
                "CityStats hub must reference CityStatsService.");
            Assert.IsTrue(hubSource.Contains("_svc"),
                "CityStats hub must hold a _svc delegate field.");

            // Assert 4: locked SerializeField fields still present (invariant #3)
            Assert.IsTrue(hubSource.Contains("public TimeManager timeManager"),
                "CityStats hub must retain public TimeManager timeManager field (locked #3).");
            Assert.IsTrue(hubSource.Contains("public WaterManager waterManager"),
                "CityStats hub must retain public WaterManager waterManager field (locked #3).");
            Assert.IsTrue(hubSource.Contains("public ForestManager forestManager"),
                "CityStats hub must retain public ForestManager forestManager field (locked #3).");
        }

        [Test]
        public void city_stats_service_is_in_correct_namespace()
        {
            Type t = typeof(Domains.Economy.Services.CityStatsService);
            Assert.AreEqual("Domains.Economy.Services", t.Namespace,
                $"CityStatsService namespace mismatch: {t.Namespace}");
        }

        [Test]
        public void city_stats_service_sub_type_zone_mutators_present()
        {
            Type t = typeof(Domains.Economy.Services.CityStatsService);
            Assert.IsNotNull(t.GetMethod("AddResidentialLightBuildingCount"), "Must expose AddResidentialLightBuildingCount");
            Assert.IsNotNull(t.GetMethod("RemoveResidentialLightBuildingCount"), "Must expose RemoveResidentialLightBuildingCount");
            Assert.IsNotNull(t.GetMethod("AddCommercialHeavyZoningCount"), "Must expose AddCommercialHeavyZoningCount");
            Assert.IsNotNull(t.GetMethod("AddIndustrialMediumBuildingCount"), "Must expose AddIndustrialMediumBuildingCount");
        }

        [Test]
        public void city_stats_service_sub_type_accessors_return_correct_values()
        {
            var svc = new Domains.Economy.Services.CityStatsService();
            svc.AddResidentialLightBuildingCount();
            svc.AddResidentialLightBuildingCount();
            Assert.AreEqual(2, svc.GetResidentialLightBuildingCount(),
                "GetResidentialLightBuildingCount must return 2 after two Add calls.");
            Assert.AreEqual(2, svc.GetResidentialBuildingCount(),
                "AddResidentialLightBuildingCount must also increment aggregate residential building count.");
        }

        [Test]
        public void city_stats_service_no_tier_e_needed()
        {
            string repoRoot = GetRepoRoot();
            string svcPath = Path.Combine(repoRoot, CityStatsServicePath);
            Assert.IsTrue(File.Exists(svcPath), $"CityStatsService.cs not found at {svcPath}");
            int lineCount = File.ReadAllLines(svcPath).Length;
            Assert.LessOrEqual(lineCount, 500,
                $"CityStatsService.cs is {lineCount} LOC — if >500 a Tier-E sub-split must be scheduled.");
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
