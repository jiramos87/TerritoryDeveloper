using NUnit.Framework;
using System.IO;

namespace Territory.Tests.EditMode.Atomization.Stage7_2
{
    /// <summary>
    /// §Red-Stage Proof anchor: TerraformingServiceSplitSpec.cs::terraforming_service_split
    /// Stage 7.2: Tier-E — TerraformingService split into 3 sub-services.
    /// Green: TerraformingService.cs + TerraformPlanService.cs + TerraformApplyService.cs + TerraformSmoothService.cs each ≤500 LOC.
    /// Facade contract (ITerrain / TerraformingService public API) unchanged.
    /// </summary>
    public class TerraformingServiceSplitSpec
    {
        private const string ServicesDir = "Assets/Scripts/Domains/Terrain/Services";
        private const string OrchestratorPath = "Assets/Scripts/Domains/Terrain/Services/TerraformingService.cs";
        private const string PlanPath = "Assets/Scripts/Domains/Terrain/Services/TerraformPlanService.cs";
        private const string ApplyPath = "Assets/Scripts/Domains/Terrain/Services/TerraformApplyService.cs";
        private const string SmoothPath = "Assets/Scripts/Domains/Terrain/Services/TerraformSmoothService.cs";

        // ── §Red-Stage Proof anchor method ────────────────────────────────────────

        [Test]
        public void terraforming_service_split()
        {
            string root = GetRepoRoot();

            // Assert 1: TerraformingService.cs ≤500 LOC
            string orchAbs = Path.Combine(root, OrchestratorPath);
            Assert.IsTrue(File.Exists(orchAbs), $"TerraformingService.cs not found at {orchAbs}");
            int orchLines = File.ReadAllLines(orchAbs).Length;
            Assert.LessOrEqual(orchLines, 500,
                $"TerraformingService.cs (orchestrator) must be ≤500 LOC. Current: {orchLines}");

            // Assert 2: TerraformPlanService.cs exists and ≤500 LOC
            string planAbs = Path.Combine(root, PlanPath);
            Assert.IsTrue(File.Exists(planAbs), $"TerraformPlanService.cs not found at {planAbs}");
            int planLines = File.ReadAllLines(planAbs).Length;
            Assert.LessOrEqual(planLines, 500,
                $"TerraformPlanService.cs must be ≤500 LOC. Current: {planLines}");

            // Assert 3: TerraformApplyService.cs exists and ≤500 LOC
            string applyAbs = Path.Combine(root, ApplyPath);
            Assert.IsTrue(File.Exists(applyAbs), $"TerraformApplyService.cs not found at {applyAbs}");
            int applyLines = File.ReadAllLines(applyAbs).Length;
            Assert.LessOrEqual(applyLines, 500,
                $"TerraformApplyService.cs must be ≤500 LOC. Current: {applyLines}");

            // Assert 4: TerraformSmoothService.cs exists and ≤500 LOC
            string smoothAbs = Path.Combine(root, SmoothPath);
            Assert.IsTrue(File.Exists(smoothAbs), $"TerraformSmoothService.cs not found at {smoothAbs}");
            int smoothLines = File.ReadAllLines(smoothAbs).Length;
            Assert.LessOrEqual(smoothLines, 500,
                $"TerraformSmoothService.cs must be ≤500 LOC. Current: {smoothLines}");

            // Assert 5: facade contract — TerraformingService still exposes ComputePathPlan
            string orchSrc = File.ReadAllText(orchAbs);
            Assert.IsTrue(orchSrc.Contains("public Territory.Terrain.PathTerraformPlan ComputePathPlan") ||
                          orchSrc.Contains("public PathTerraformPlan ComputePathPlan"),
                "TerraformingService must retain public ComputePathPlan() — facade contract unchanged.");
        }

        [Test]
        public void plan_service_exists()
        {
            string abs = Path.Combine(GetRepoRoot(), PlanPath);
            Assert.IsTrue(File.Exists(abs), $"TerraformPlanService.cs must exist at {abs}");
        }

        [Test]
        public void apply_service_exists()
        {
            string abs = Path.Combine(GetRepoRoot(), ApplyPath);
            Assert.IsTrue(File.Exists(abs), $"TerraformApplyService.cs must exist at {abs}");
        }

        [Test]
        public void smooth_service_exists()
        {
            string abs = Path.Combine(GetRepoRoot(), SmoothPath);
            Assert.IsTrue(File.Exists(abs), $"TerraformSmoothService.cs must exist at {abs}");
        }

        [Test]
        public void orchestrator_delegates_to_plan()
        {
            string src = File.ReadAllText(Path.Combine(GetRepoRoot(), OrchestratorPath));
            Assert.IsTrue(src.Contains("TerraformPlanService"),
                "TerraformingService orchestrator must reference TerraformPlanService.");
        }

        [Test]
        public void orchestrator_delegates_to_apply()
        {
            string src = File.ReadAllText(Path.Combine(GetRepoRoot(), OrchestratorPath));
            Assert.IsTrue(src.Contains("TerraformApplyService"),
                "TerraformingService orchestrator must reference TerraformApplyService.");
        }

        [Test]
        public void orchestrator_delegates_to_smooth()
        {
            string src = File.ReadAllText(Path.Combine(GetRepoRoot(), OrchestratorPath));
            Assert.IsTrue(src.Contains("TerraformSmoothService"),
                "TerraformingService orchestrator must reference TerraformSmoothService.");
        }

        private static string GetRepoRoot()
        {
            string dir = Directory.GetCurrentDirectory();
            while (!string.IsNullOrEmpty(dir))
            {
                if (File.Exists(Path.Combine(dir, "CLAUDE.md")))
                    return dir;
                dir = Directory.GetParent(dir)?.FullName;
            }
            return Directory.GetCurrentDirectory();
        }
    }
}
