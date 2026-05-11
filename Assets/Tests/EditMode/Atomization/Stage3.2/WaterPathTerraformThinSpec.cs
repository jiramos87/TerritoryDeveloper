using NUnit.Framework;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace Territory.Tests.EditMode.Atomization.Stage3_2
{
    /// <summary>
    /// §Red-Stage Proof anchor: WaterPathTerraformThinSpec.cs::water_and_path_terraform_are_thin
    /// Stage 3.2: WaterMap Tier-B THIN + PathTerraformPlan trim.
    /// Green: WaterMap.cs ≤500 LOC, PathTerraformPlan.cs ≤500 LOC, partial files exist, service files exist.
    /// </summary>
    public class WaterPathTerraformThinSpec
    {
        private const string WaterMapPath = "Assets/Scripts/Core/Terrain/WaterMap.cs";
        private const string WaterMapJunctionMergePath = "Assets/Scripts/Core/Terrain/WaterMap.JunctionMerge.cs";
        private const string WaterMapLakeGenPath = "Assets/Scripts/Core/Terrain/WaterMap.LakeGen.cs";
        private const string WaterMapSerializationPath = "Assets/Scripts/Core/Terrain/WaterMap.Serialization.cs";
        private const string PathTerraformPlanPath = "Assets/Scripts/Core/Terrain/PathTerraformPlan.cs";
        private const string PathTerraformPlanValidationPath = "Assets/Scripts/Core/Terrain/PathTerraformPlan.Validation.cs";
        private const string PathTerraformServicePath = "Assets/Scripts/Domains/Terrain/Services/PathTerraformService.cs";

        [Test]
        public void water_and_path_terraform_are_thin()
        {
            string repoRoot = GetRepoRoot();

            // Assert 1: WaterMap.cs ≤500 LOC
            string waterMapFull = Path.Combine(repoRoot, WaterMapPath);
            Assert.IsTrue(File.Exists(waterMapFull), $"WaterMap.cs not found at {waterMapFull}");
            int waterMapLoc = File.ReadAllLines(waterMapFull).Length;
            Assert.LessOrEqual(waterMapLoc, 500,
                $"WaterMap.cs must be ≤500 LOC. Got {waterMapLoc} lines.");

            // Assert 2: WaterMap partial files exist
            Assert.IsTrue(File.Exists(Path.Combine(repoRoot, WaterMapJunctionMergePath)),
                "WaterMap.JunctionMerge.cs must exist");
            Assert.IsTrue(File.Exists(Path.Combine(repoRoot, WaterMapLakeGenPath)),
                "WaterMap.LakeGen.cs must exist");
            Assert.IsTrue(File.Exists(Path.Combine(repoRoot, WaterMapSerializationPath)),
                "WaterMap.Serialization.cs must exist");

            // Assert 3: PathTerraformPlan.cs ≤500 LOC
            string planFull = Path.Combine(repoRoot, PathTerraformPlanPath);
            Assert.IsTrue(File.Exists(planFull), $"PathTerraformPlan.cs not found at {planFull}");
            int planLoc = File.ReadAllLines(planFull).Length;
            Assert.LessOrEqual(planLoc, 500,
                $"PathTerraformPlan.cs must be ≤500 LOC. Got {planLoc} lines.");

            // Assert 4: PathTerraformPlan.Validation.cs partial exists
            Assert.IsTrue(File.Exists(Path.Combine(repoRoot, PathTerraformPlanValidationPath)),
                "PathTerraformPlan.Validation.cs must exist");

            // Assert 5: PathTerraformService in correct namespace
            Type ptType = typeof(Domains.Terrain.Services.PathTerraformService);
            Assert.AreEqual("Domains.Terrain.Services", ptType.Namespace,
                $"PathTerraformService namespace mismatch: {ptType.Namespace}");
        }

        [Test]
        public void PathTerraformService_exposes_Apply()
        {
            Type t = typeof(Domains.Terrain.Services.PathTerraformService);
            MethodInfo m = t.GetMethod("Apply");
            Assert.IsNotNull(m, "PathTerraformService must expose Apply(PathTerraformPlan, HeightMap, ITerrainManager)");
        }

        [Test]
        public void PathTerraformService_exposes_TryValidatePhase1Heights()
        {
            Type t = typeof(Domains.Terrain.Services.PathTerraformService);
            MethodInfo m = t.GetMethod("TryValidatePhase1Heights");
            Assert.IsNotNull(m, "PathTerraformService must expose TryValidatePhase1Heights(PathTerraformPlan, HeightMap, ITerrainManager)");
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
