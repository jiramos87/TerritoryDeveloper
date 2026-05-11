using NUnit.Framework;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace Territory.Tests.EditMode.Atomization.Stage4_0
{
    /// <summary>
    /// §Red-Stage Proof anchor: RoadManagerThinSpec.cs::road_manager_is_thin
    /// Stage 4.0: RoadManager Tier-B THIN — hub collapses to ≤200 LOC; RoadPlacementService carved.
    /// Green: RoadManager.cs ≤200 LOC AND RoadPlacementService.cs exists AND namespace correct.
    /// </summary>
    public class RoadManagerThinSpec
    {
        private const string RoadManagerPath =
            "Assets/Scripts/Managers/GameManagers/RoadManager.cs";

        private const string RoadPlacementServicePath =
            "Assets/Scripts/Managers/GameManagers/RoadPlacementService.cs";

        [Test]
        public void road_manager_is_thin()
        {
            string repoRoot = GetRepoRoot();

            // Assert 1: RoadManager.cs ≤200 LOC
            string hubPath = Path.Combine(repoRoot, RoadManagerPath);
            Assert.IsTrue(File.Exists(hubPath), $"RoadManager.cs not found at {hubPath}");
            int lineCount = File.ReadAllLines(hubPath).Length;
            Assert.LessOrEqual(lineCount, 200,
                $"RoadManager.cs must be ≤200 LOC after THIN. Got {lineCount} lines.");

            // Assert 2: RoadPlacementService.cs exists
            Assert.IsTrue(File.Exists(Path.Combine(repoRoot, RoadPlacementServicePath)),
                "RoadPlacementService.cs must exist in Domains/Roads/Services/");

            // Assert 3: RoadPlacementService in correct namespace
            Type t = typeof(Domains.Roads.Services.RoadPlacementService);
            Assert.AreEqual("Domains.Roads.Services", t.Namespace,
                $"RoadPlacementService namespace mismatch: {t.Namespace}");
        }

        [Test]
        public void RoadPlacementService_exposes_HandleRoadDrawing()
        {
            Type t = typeof(Domains.Roads.Services.RoadPlacementService);
            MethodInfo m = t.GetMethod("HandleRoadDrawing");
            Assert.IsNotNull(m, "RoadPlacementService must expose HandleRoadDrawing(Vector2, UIManager)");
        }

        [Test]
        public void RoadPlacementService_exposes_TryCommitStreetStrokeForScenarioBuild()
        {
            Type t = typeof(Domains.Roads.Services.RoadPlacementService);
            MethodInfo m = t.GetMethod("TryCommitStreetStrokeForScenarioBuild");
            Assert.IsNotNull(m, "RoadPlacementService must expose TryCommitStreetStrokeForScenarioBuild(List<Vector2>, out string)");
        }

        [Test]
        public void RoadPlacementService_exposes_PlaceInterstateFromPath()
        {
            Type t = typeof(Domains.Roads.Services.RoadPlacementService);
            MethodInfo m = t.GetMethod("PlaceInterstateFromPath");
            Assert.IsNotNull(m, "RoadPlacementService must expose PlaceInterstateFromPath(List<Vector2Int>, GameSaveManager, InterstateManager)");
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
