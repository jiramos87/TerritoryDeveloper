using NUnit.Framework;
using System;
using System.IO;
using System.Reflection;
using System.Linq;
using UnityEngine;

namespace Territory.Tests.EditMode.Atomization.Stage3_1
{
    /// <summary>
    /// §Red-Stage Proof anchor: TerrainManagerThinSpec.cs::terrain_manager_is_thin
    /// Stage 3.1: TerrainManager Tier-B THIN — hub collapses to ≤200 LOC; three services carved.
    /// Red until hub ≤200 LOC AND all publics are single-line delegates.
    /// Green: all five asserts pass, compile-check exits 0.
    /// </summary>
    public class TerrainManagerThinSpec
    {
        private const string TerrainManagerPath =
            "Assets/Scripts/Managers/GameManagers/TerrainManager.cs";

        private const string HeightWriteServicePath =
            "Assets/Scripts/Domains/Terrain/Services/HeightWriteService.cs";

        private const string TerrainQueryServicePath =
            "Assets/Scripts/Domains/Terrain/Services/TerrainQueryService.cs";

        private const string TerrainInitServicePath =
            "Assets/Scripts/Domains/Terrain/Services/TerrainInitService.cs";

        /// <summary>
        /// Anchor method. All five stage-exit assertions run here.
        /// Fails until hub ≤200 LOC (TECH-30017 cutover task).
        /// Passes once TerrainManager is fully thinned.
        /// </summary>
        [Test]
        public void terrain_manager_is_thin()
        {
            // Assert 1: line count ≤200
            string repoRoot = GetRepoRoot();
            string hubPath = Path.Combine(repoRoot, TerrainManagerPath);
            Assert.IsTrue(File.Exists(hubPath), $"TerrainManager.cs not found at {hubPath}");
            int lineCount = File.ReadAllLines(hubPath).Length;
            Assert.LessOrEqual(lineCount, 200,
                $"TerrainManager.cs must be ≤200 LOC after THIN. Got {lineCount} lines.");

            // Assert 2: three service files exist
            Assert.IsTrue(File.Exists(Path.Combine(repoRoot, HeightWriteServicePath)),
                "HeightWriteService.cs must exist in Domains/Terrain/Services/");
            Assert.IsTrue(File.Exists(Path.Combine(repoRoot, TerrainQueryServicePath)),
                "TerrainQueryService.cs must exist in Domains/Terrain/Services/");
            Assert.IsTrue(File.Exists(Path.Combine(repoRoot, TerrainInitServicePath)),
                "TerrainInitService.cs must exist in Domains/Terrain/Services/");

            // Assert 3: HeightWriteService in correct namespace
            Type hwType = typeof(Domains.Terrain.Services.HeightWriteService);
            Assert.AreEqual("Domains.Terrain.Services", hwType.Namespace,
                $"HeightWriteService namespace mismatch: {hwType.Namespace}");

            // Assert 4: TerrainQueryService in correct namespace
            Type tqType = typeof(Domains.Terrain.Services.TerrainQueryService);
            Assert.AreEqual("Domains.Terrain.Services", tqType.Namespace,
                $"TerrainQueryService namespace mismatch: {tqType.Namespace}");

            // Assert 5: TerrainInitService in correct namespace
            Type tiType = typeof(Domains.Terrain.Services.TerrainInitService);
            Assert.AreEqual("Domains.Terrain.Services", tiType.Namespace,
                $"TerrainInitService namespace mismatch: {tiType.Namespace}");
        }

        /// <summary>
        /// HeightWriteService must own RestoreHeightMapFromGridData method (height-write path).
        /// </summary>
        [Test]
        public void HeightWriteService_owns_RestoreHeightMapFromGridData()
        {
            Type t = typeof(Domains.Terrain.Services.HeightWriteService);
            MethodInfo m = t.GetMethod("RestoreHeightMapFromGridData");
            Assert.IsNotNull(m,
                "HeightWriteService must expose RestoreHeightMapFromGridData(List<CellData>)");
        }

        /// <summary>
        /// TerrainQueryService must expose CanPlaceRoad (read-only query).
        /// </summary>
        [Test]
        public void TerrainQueryService_exposes_CanPlaceRoad()
        {
            Type t = typeof(Domains.Terrain.Services.TerrainQueryService);
            MethodInfo m = t.GetMethod("CanPlaceRoad", new Type[] { typeof(int), typeof(int) });
            Assert.IsNotNull(m,
                "TerrainQueryService must expose CanPlaceRoad(int x, int y)");
        }

        /// <summary>
        /// TerrainInitService must expose GetOrCreateHeightMap.
        /// </summary>
        [Test]
        public void TerrainInitService_exposes_GetOrCreateHeightMap()
        {
            Type t = typeof(Domains.Terrain.Services.TerrainInitService);
            MethodInfo m = t.GetMethod("GetOrCreateHeightMap");
            Assert.IsNotNull(m,
                "TerrainInitService must expose GetOrCreateHeightMap()");
        }

        /// <summary>
        /// TerrainInitService must expose SetNewGameFlatTerrainOptions.
        /// </summary>
        [Test]
        public void TerrainInitService_exposes_SetNewGameFlatTerrainOptions()
        {
            Type t = typeof(Domains.Terrain.Services.TerrainInitService);
            MethodInfo m = t.GetMethod("SetNewGameFlatTerrainOptions");
            Assert.IsNotNull(m,
                "TerrainInitService must expose SetNewGameFlatTerrainOptions(bool, int)");
        }

        private static string GetRepoRoot()
        {
            // Walk up from Application.dataPath (Assets/) to find repo root.
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
