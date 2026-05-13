using NUnit.Framework;
using System.IO;

namespace Territory.Tests.EditMode.Atomization.Stage7_0
{
    /// <summary>
    /// §Red-Stage Proof anchor: AutoBuildServiceSplitSpec.cs::auto_build_service_split
    /// Stage 7.0: Tier-E — AutoBuildService split into 3 sub-services.
    /// Green: AutoBuildService.cs + AutoBuildSimRulesService.cs + AutoBuildCandidateScoringService.cs each ≤500 LOC.
    /// Facade contract (IRoads / AutoBuildService public API) unchanged.
    /// </summary>
    public class AutoBuildServiceSplitSpec
    {
        private const string ServicesDir = "Assets/Scripts/Domains/Roads/Services";
        private const string OrchestratorPath = "Assets/Scripts/Domains/Roads/Services/AutoBuildService.cs";
        private const string SimRulesPath = "Assets/Scripts/Domains/Roads/Services/AutoBuildSimRulesService.cs";
        private const string ScoringPath = "Assets/Scripts/Domains/Roads/Services/AutoBuildCandidateScoringService.cs";

        // ── §Red-Stage Proof anchor method ────────────────────────────────────────

        [Test]
        public void auto_build_service_split()
        {
            string root = GetRepoRoot();

            // Assert 1: AutoBuildService.cs ≤500 LOC
            string orchAbs = Path.Combine(root, OrchestratorPath);
            Assert.IsTrue(File.Exists(orchAbs), $"AutoBuildService.cs not found at {orchAbs}");
            int orchLines = File.ReadAllLines(orchAbs).Length;
            Assert.LessOrEqual(orchLines, 500,
                $"AutoBuildService.cs (orchestrator) must be ≤500 LOC. Current: {orchLines}");

            // Assert 2: AutoBuildSimRulesService.cs exists and ≤500 LOC
            string simAbs = Path.Combine(root, SimRulesPath);
            Assert.IsTrue(File.Exists(simAbs), $"AutoBuildSimRulesService.cs not found at {simAbs}");
            int simLines = File.ReadAllLines(simAbs).Length;
            Assert.LessOrEqual(simLines, 500,
                $"AutoBuildSimRulesService.cs must be ≤500 LOC. Current: {simLines}");

            // Assert 3: AutoBuildCandidateScoringService.cs exists and ≤500 LOC
            string scoreAbs = Path.Combine(root, ScoringPath);
            Assert.IsTrue(File.Exists(scoreAbs), $"AutoBuildCandidateScoringService.cs not found at {scoreAbs}");
            int scoreLines = File.ReadAllLines(scoreAbs).Length;
            Assert.LessOrEqual(scoreLines, 500,
                $"AutoBuildCandidateScoringService.cs must be ≤500 LOC. Current: {scoreLines}");

            // Assert 4: facade contract — AutoBuildService still has ProcessTick
            string orchSrc = File.ReadAllText(orchAbs);
            Assert.IsTrue(orchSrc.Contains("public void ProcessTick"),
                "AutoBuildService must retain public ProcessTick() — facade contract unchanged.");
        }

        [Test]
        public void sim_rules_service_exists()
        {
            string abs = Path.Combine(GetRepoRoot(), SimRulesPath);
            Assert.IsTrue(File.Exists(abs), $"AutoBuildSimRulesService.cs must exist at {abs}");
        }

        [Test]
        public void candidate_scoring_service_exists()
        {
            string abs = Path.Combine(GetRepoRoot(), ScoringPath);
            Assert.IsTrue(File.Exists(abs), $"AutoBuildCandidateScoringService.cs must exist at {abs}");
        }

        [Test]
        public void orchestrator_delegates_to_sim_rules()
        {
            string src = File.ReadAllText(Path.Combine(GetRepoRoot(), OrchestratorPath));
            Assert.IsTrue(src.Contains("AutoBuildSimRulesService"),
                "AutoBuildService orchestrator must reference AutoBuildSimRulesService.");
        }

        [Test]
        public void orchestrator_delegates_to_candidate_scoring()
        {
            string src = File.ReadAllText(Path.Combine(GetRepoRoot(), OrchestratorPath));
            Assert.IsTrue(src.Contains("AutoBuildCandidateScoringService"),
                "AutoBuildService orchestrator must reference AutoBuildCandidateScoringService.");
        }

        [Test]
        public void orchestrator_namespace_unchanged()
        {
            string src = File.ReadAllText(Path.Combine(GetRepoRoot(), OrchestratorPath));
            Assert.IsTrue(src.Contains("namespace Domains.Roads.Services"),
                "AutoBuildService namespace must remain Domains.Roads.Services.");
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private static string GetRepoRoot()
        {
            string path = UnityEngine.Application.dataPath;
            return Path.GetDirectoryName(path);
        }
    }
}
