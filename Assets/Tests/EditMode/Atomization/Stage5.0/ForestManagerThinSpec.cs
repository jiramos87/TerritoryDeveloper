using NUnit.Framework;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace Territory.Tests.EditMode.Atomization.Stage5_0
{
    /// <summary>
    /// §Red-Stage Proof anchor: ForestManagerThinSpec.cs::forest_manager_is_thin
    /// Stage 5.0: ForestManager Tier-C NO-PORT — hub collapses to ≤200 LOC; logic delegated to ForestService.
    /// Green: ForestManager.cs ≤200 LOC AND ForestService.cs exists at Domains/Forests/Services/ AND hub delegates via _svc.
    /// </summary>
    public class ForestManagerThinSpec
    {
        private const string ForestManagerPath =
            "Assets/Scripts/Managers/GameManagers/ForestManager.cs";

        private const string ForestServicePath =
            "Assets/Scripts/Domains/Forests/Services/ForestService.cs";

        [Test]
        public void forest_manager_is_thin()
        {
            string repoRoot = GetRepoRoot();

            // Assert 1: ForestManager.cs ≤200 LOC
            string hubPath = Path.Combine(repoRoot, ForestManagerPath);
            Assert.IsTrue(File.Exists(hubPath), $"ForestManager.cs not found at {hubPath}");
            int lineCount = File.ReadAllLines(hubPath).Length;
            Assert.LessOrEqual(lineCount, 200,
                $"ForestManager.cs must be ≤200 LOC after THIN. Got {lineCount} lines.");

            // Assert 2: ForestService.cs exists under Domains/Forests/Services/
            string svcPath = Path.Combine(repoRoot, ForestServicePath);
            Assert.IsTrue(File.Exists(svcPath),
                $"ForestService.cs must exist at {svcPath}.");

            // Assert 3: hub delegates to ForestService (_svc field present)
            string hubSource = File.ReadAllText(hubPath);
            Assert.IsTrue(hubSource.Contains("ForestService"),
                "ForestManager hub must reference ForestService.");
            Assert.IsTrue(hubSource.Contains("_svc"),
                "ForestManager hub must hold a _svc delegate field.");

            // Assert 4: locked SerializeField fields still present (invariant #3)
            Assert.IsTrue(hubSource.Contains("public GridManager gridManager"),
                "ForestManager hub must retain public GridManager gridManager field (locked #3).");
            Assert.IsTrue(hubSource.Contains("public WaterManager waterManager"),
                "ForestManager hub must retain public WaterManager waterManager field (locked #3).");
            Assert.IsTrue(hubSource.Contains("public TerrainManager terrainManager"),
                "ForestManager hub must retain public TerrainManager terrainManager field (locked #3).");

            // Assert 5: hub uses WireDependencies pattern
            Assert.IsTrue(hubSource.Contains("WireDependencies"),
                "ForestManager hub must call _svc.WireDependencies(...).");
        }

        [Test]
        public void forest_service_is_in_correct_namespace()
        {
            Type t = typeof(Domains.Forests.Services.ForestService);
            Assert.AreEqual("Domains.Forests.Services", t.Namespace,
                $"ForestService namespace mismatch: {t.Namespace}");
        }

        [Test]
        public void forest_service_exposes_wire_dependencies()
        {
            Type t = typeof(Domains.Forests.Services.ForestService);
            Assert.IsNotNull(t, "ForestService must exist");
            MethodInfo m = t.GetMethod("WireDependencies");
            Assert.IsNotNull(m, "ForestService must expose WireDependencies()");
        }

        [Test]
        public void forest_service_exposes_can_place_forest()
        {
            Type t = typeof(Domains.Forests.Services.ForestService);
            Assert.IsNotNull(t, "ForestService must exist");
            Assert.IsNotNull(t.GetMethod("CanPlaceForestAt"), "ForestService must expose CanPlaceForestAt()");
        }

        [Test]
        public void forest_service_exposes_forest_statistics()
        {
            Type t = typeof(Domains.Forests.Services.ForestService);
            Assert.IsNotNull(t, "ForestService must exist");
            Assert.IsNotNull(t.GetMethod("GetForestStatistics"), "ForestService must expose GetForestStatistics()");
        }

        private static string GetRepoRoot()
        {
            string dir = Application.dataPath;
            while (!string.IsNullOrEmpty(dir))
            {
                if (File.Exists(Path.Combine(dir, "CLAUDE.md"))) return dir;
                string parent = Path.GetDirectoryName(dir);
                if (parent == dir) break;
                dir = parent;
            }
            return Application.dataPath.Replace("/Assets", "");
        }
    }
}
