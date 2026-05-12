using NUnit.Framework;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using Domains.UI.Services;

namespace Territory.Tests.EditMode.Atomization.Stage5_6
{
    /// <summary>
    /// §Red-Stage Proof anchor: MiniMapControllerThinSpec.cs::mini_map_controller_is_thin
    /// Stage 5.6: MiniMapController Tier-C NO-PORT — hub collapses to ≤200 LOC;
    /// color/classifier logic delegated to MiniMapService.
    /// Green: MiniMapController.cs ≤200 LOC AND MiniMapService.cs exists.
    /// </summary>
    public class MiniMapControllerThinSpec
    {
        private const string HubPath =
            "Assets/Scripts/Controllers/GameControllers/MiniMapController.cs";

        private const string SvcPath =
            "Assets/Scripts/Domains/UI/Services/MiniMapService.cs";

        [Test]
        public void mini_map_controller_is_thin()
        {
            string repoRoot = GetRepoRoot();

            // Assert 1: MiniMapController.cs ≤200 LOC
            string hub = Path.Combine(repoRoot, HubPath);
            Assert.IsTrue(File.Exists(hub), $"MiniMapController.cs not found at {hub}");
            int lineCount = File.ReadAllLines(hub).Length;
            Assert.LessOrEqual(lineCount, 200,
                $"MiniMapController.cs must be ≤200 LOC after THIN. Got {lineCount} lines.");

            // Assert 2: MiniMapService.cs exists under Domains/UI/Services/
            string svc = Path.Combine(repoRoot, SvcPath);
            Assert.IsTrue(File.Exists(svc), $"MiniMapService.cs must exist at {svc}.");
        }

        [Test]
        public void mini_map_service_is_in_correct_namespace()
        {
            Type t = typeof(Domains.UI.Services.MiniMapService);
            Assert.AreEqual("Domains.UI.Services", t.Namespace,
                $"MiniMapService namespace mismatch: {t.Namespace}");
        }

        [Test]
        public void mini_map_service_exposes_get_cell_color()
        {
            Type t = typeof(Domains.UI.Services.MiniMapService);
            MethodInfo m = t.GetMethod("GetCellColor");
            Assert.IsNotNull(m, "MiniMapService must expose GetCellColor()");
        }

        [Test]
        public void mini_map_service_exposes_build_road_set()
        {
            Type t = typeof(Domains.UI.Services.MiniMapService);
            MethodInfo m = t.GetMethod("BuildRoadSet");
            Assert.IsNotNull(m, "MiniMapService must expose BuildRoadSet()");
        }

        [Test]
        public void mini_map_service_exposes_build_interstate_set()
        {
            Type t = typeof(Domains.UI.Services.MiniMapService);
            MethodInfo m = t.GetMethod("BuildInterstateSet");
            Assert.IsNotNull(m, "MiniMapService must expose BuildInterstateSet()");
        }

        [Test]
        public void mini_map_service_exposes_compute_desirability_range()
        {
            Type t = typeof(Domains.UI.Services.MiniMapService);
            MethodInfo m = t.GetMethod("ComputeDesirabilityRange");
            Assert.IsNotNull(m, "MiniMapService must expose ComputeDesirabilityRange()");
        }

        [Test]
        public void hub_path_unchanged()
        {
            string repoRoot = GetRepoRoot();
            string hub = Path.Combine(repoRoot, HubPath);
            Assert.IsTrue(File.Exists(hub),
                $"MiniMapController.cs must remain at locked path {HubPath}");
        }

        [Test]
        public void hub_delegates_to_mini_map_service()
        {
            string repoRoot = GetRepoRoot();
            string hub = Path.Combine(repoRoot, HubPath);
            string hubSource = File.ReadAllText(hub);
            Assert.IsTrue(hubSource.Contains("MiniMapService"),
                "MiniMapController hub must reference MiniMapService.");
            Assert.IsTrue(hubSource.Contains("_svc"),
                "MiniMapController hub must hold a _svc delegate field.");
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
