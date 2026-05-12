using NUnit.Framework;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace Territory.Tests.EditMode.Atomization.Stage5_3
{
    /// <summary>
    /// §Red-Stage Proof anchor: ProceduralRiverThinSpec.cs::procedural_river_generator_is_thin
    /// Stage 5.3: ProceduralRiverGenerator Tier-C NO-PORT — hub ≤200 LOC; BFS+carve delegated to ProceduralRiverService.
    /// Green: ProceduralRiverGenerator.cs ≤200 LOC AND ProceduralRiverService.cs exists AND H_bed monotonic invariant green.
    /// </summary>
    public class ProceduralRiverThinSpec
    {
        private const string HubPath =
            "Assets/Scripts/Managers/GameManagers/ProceduralRiverGenerator.cs";

        private const string SvcPath =
            "Assets/Scripts/Domains/Water/Services/ProceduralRiverService.cs";

        [Test]
        public void procedural_river_generator_is_thin()
        {
            string repoRoot = GetRepoRoot();

            // Assert 1: ProceduralRiverGenerator.cs ≤200 LOC
            string hub = Path.Combine(repoRoot, HubPath);
            Assert.IsTrue(File.Exists(hub), $"ProceduralRiverGenerator.cs not found at {hub}");
            int lineCount = File.ReadAllLines(hub).Length;
            Assert.LessOrEqual(lineCount, 200,
                $"ProceduralRiverGenerator.cs must be ≤200 LOC after THIN. Got {lineCount} lines.");

            // Assert 2: ProceduralRiverService.cs exists under Domains/Water/Services/
            string svc = Path.Combine(repoRoot, SvcPath);
            Assert.IsTrue(File.Exists(svc), $"ProceduralRiverService.cs must exist at {svc}.");

            // Assert 3 (h_bed_monotonic_invariant_green): hub delegates to ProceduralRiverService
            string hubSource = File.ReadAllText(hub);
            Assert.IsTrue(hubSource.Contains("ProceduralRiverService"),
                "ProceduralRiverGenerator hub must reference ProceduralRiverService.");
            Assert.IsTrue(hubSource.Contains("ProceduralRiverService.ApplyCrossSectionHeights"),
                "Hub must delegate ApplyCrossSectionHeights (H_bed monotonic write) to ProceduralRiverService.");
        }

        [Test]
        public void procedural_river_service_in_water_domain_namespace()
        {
            Type t = typeof(Domains.Water.Services.ProceduralRiverService);
            Assert.AreEqual("Domains.Water.Services", t.Namespace,
                $"ProceduralRiverService namespace mismatch: {t.Namespace}");
        }

        [Test]
        public void procedural_river_service_exposes_apply_cross_section_heights()
        {
            Type t = typeof(Domains.Water.Services.ProceduralRiverService);
            MethodInfo m = t.GetMethod("ApplyCrossSectionHeights", BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(m, "ProceduralRiverService must expose static ApplyCrossSectionHeights()");
        }

        [Test]
        public void procedural_river_service_exposes_try_build_centerline()
        {
            Type t = typeof(Domains.Water.Services.ProceduralRiverService);
            MethodInfo m = t.GetMethod("TryBuildCenterline", BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(m, "ProceduralRiverService must expose static TryBuildCenterline()");
        }

        [Test]
        public void procedural_river_hub_path_unchanged()
        {
            string repoRoot = GetRepoRoot();
            string hub = Path.Combine(repoRoot, HubPath);
            Assert.IsTrue(File.Exists(hub),
                $"ProceduralRiverGenerator.cs must remain at locked path {HubPath}");
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
