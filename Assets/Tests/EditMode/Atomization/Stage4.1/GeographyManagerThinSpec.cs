using NUnit.Framework;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace Territory.Tests.EditMode.Atomization.Stage4_1
{
    /// <summary>
    /// §Red-Stage Proof anchor: GeographyManagerThinSpec.cs::geography_manager_is_thin
    /// Stage 4.1: GeographyManager Tier-B THIN — hub collapses to ≤200 LOC; init/load/clear/report delegated.
    /// Green: GeographyManager.cs ≤200 LOC AND GeographyInitService.cs exists AND hub delegates all bodies.
    /// </summary>
    public class GeographyManagerThinSpec
    {
        private const string GeographyManagerPath =
            "Assets/Scripts/Managers/GameManagers/GeographyManager.cs";

        private const string GeographyInitServicePath =
            "Assets/Scripts/Managers/GameManagers/GeographyInitService.cs";

        private const string GeographyQueryServicePath =
            "Assets/Scripts/Managers/GameManagers/GeographyQueryService.cs";

        private const string GeographyClearServicePath =
            "Assets/Scripts/Managers/GameManagers/GeographyClearService.cs";

        [Test]
        public void geography_manager_is_thin()
        {
            string repoRoot = GetRepoRoot();

            // Assert 1: GeographyManager.cs ≤200 LOC
            string hubPath = Path.Combine(repoRoot, GeographyManagerPath);
            Assert.IsTrue(File.Exists(hubPath), $"GeographyManager.cs not found at {hubPath}");
            int lineCount = File.ReadAllLines(hubPath).Length;
            Assert.LessOrEqual(lineCount, 200,
                $"GeographyManager.cs must be ≤200 LOC after THIN. Got {lineCount} lines.");

            // Assert 2: GeographyInitService.cs exists
            Assert.IsTrue(File.Exists(Path.Combine(repoRoot, GeographyInitServicePath)),
                "GeographyInitService.cs must exist in Managers/GameManagers/");

            // Assert 3: GeographyQueryService.cs exists
            Assert.IsTrue(File.Exists(Path.Combine(repoRoot, GeographyQueryServicePath)),
                "GeographyQueryService.cs must exist in Managers/GameManagers/");

            // Assert 4: GeographyClearService.cs exists
            Assert.IsTrue(File.Exists(Path.Combine(repoRoot, GeographyClearServicePath)),
                "GeographyClearService.cs must exist in Managers/GameManagers/");
        }

        [Test]
        public void GeographyInitService_is_in_correct_namespace()
        {
            Type t = Type.GetType("Territory.Geography.GeographyInitService, Assembly-CSharp");
            Assert.IsNotNull(t, "GeographyInitService must be loadable from Assembly-CSharp");
            Assert.AreEqual("Territory.Geography", t.Namespace,
                $"GeographyInitService namespace mismatch: {t.Namespace}");
        }

        [Test]
        public void GeographyInitService_exposes_InitializeGeography()
        {
            Type t = Type.GetType("Territory.Geography.GeographyInitService, Assembly-CSharp");
            Assert.IsNotNull(t, "GeographyInitService must exist");
            MethodInfo m = t.GetMethod("InitializeGeography");
            Assert.IsNotNull(m, "GeographyInitService must expose InitializeGeography()");
        }

        [Test]
        public void GeographyInitService_exposes_BuildGeographyInitReportJson()
        {
            Type t = Type.GetType("Territory.Geography.GeographyInitService, Assembly-CSharp");
            Assert.IsNotNull(t, "GeographyInitService must exist");
            MethodInfo m = t.GetMethod("BuildGeographyInitReportJson");
            Assert.IsNotNull(m, "GeographyInitService must expose BuildGeographyInitReportJson()");
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
