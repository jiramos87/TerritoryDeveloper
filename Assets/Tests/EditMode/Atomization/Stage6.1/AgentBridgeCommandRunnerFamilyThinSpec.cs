using NUnit.Framework;
using System.IO;

namespace Territory.Tests.EditMode.Atomization.Stage6_1
{
    /// <summary>
    /// §Red-Stage Proof anchor: AgentBridgeCommandRunnerFamilyThinSpec.cs::agent_bridge_command_runner_family_thin
    /// Stage 6.1: AgentBridgeCommandRunner family Tier-D partial-class consolidation.
    /// Green: AgentBridgeCommandRunner.cs + .Conformance.cs each ≤200 LOC;
    /// mutation kinds declared in .Mutations.cs; BridgeCommandService + BridgeConformanceService POCOs exist.
    /// </summary>
    public class AgentBridgeCommandRunnerFamilyThinSpec
    {
        private const string RunnerPath    = "Assets/Scripts/Editor/AgentBridgeCommandRunner.cs";
        private const string ConformPath   = "Assets/Scripts/Editor/AgentBridgeCommandRunner.Conformance.cs";
        private const string MutationsPath = "Assets/Scripts/Editor/AgentBridgeCommandRunner.Mutations.cs";
        private const string BridgeCmdSvc  = "Assets/Scripts/Domains/Bridge/Services/BridgeCommandService.cs";
        private const string BridgeConfSvc = "Assets/Scripts/Domains/Bridge/Services/BridgeConformanceService.cs";

        // ── §Red-Stage Proof anchor method ────────────────────────────────────────

        [Test]
        public void agent_bridge_command_runner_family_thin()
        {
            string repoRoot = GetRepoRoot();

            // Assert 1: AgentBridgeCommandRunner.cs ≤200 LOC
            string runnerAbs = Path.Combine(repoRoot, RunnerPath);
            Assert.IsTrue(File.Exists(runnerAbs), $"AgentBridgeCommandRunner.cs not found at {runnerAbs}");
            int runnerLines = File.ReadAllLines(runnerAbs).Length;
            Assert.LessOrEqual(runnerLines, 200,
                $"AgentBridgeCommandRunner.cs must be THIN (≤200 LOC). Current: {runnerLines}");

            // Assert 2: AgentBridgeCommandRunner.Conformance.cs ≤200 LOC
            string conformAbs = Path.Combine(repoRoot, ConformPath);
            Assert.IsTrue(File.Exists(conformAbs), $"AgentBridgeCommandRunner.Conformance.cs not found at {conformAbs}");
            int conformLines = File.ReadAllLines(conformAbs).Length;
            Assert.LessOrEqual(conformLines, 200,
                $"AgentBridgeCommandRunner.Conformance.cs must be THIN (≤200 LOC). Current: {conformLines}");

            // Assert 3: mutation kinds declared in .Mutations.cs (guardrail #13)
            string mutationsAbs = Path.Combine(repoRoot, MutationsPath);
            Assert.IsTrue(File.Exists(mutationsAbs), $"AgentBridgeCommandRunner.Mutations.cs not found at {mutationsAbs}");
            string mutationsSrc = File.ReadAllText(mutationsAbs);
            Assert.IsTrue(mutationsSrc.Contains("TryDispatchMutationKind"),
                "Mutations.cs must declare TryDispatchMutationKind (guardrail #13 mutation-kind declaration).");
        }

        [Test]
        public void bridge_command_service_exists()
        {
            string repoRoot = GetRepoRoot();
            string absPath = Path.Combine(repoRoot, BridgeCmdSvc);
            Assert.IsTrue(File.Exists(absPath), $"BridgeCommandService.cs must exist at {absPath}");
        }

        [Test]
        public void bridge_conformance_service_exists()
        {
            string repoRoot = GetRepoRoot();
            string absPath = Path.Combine(repoRoot, BridgeConfSvc);
            Assert.IsTrue(File.Exists(absPath), $"BridgeConformanceService.cs must exist at {absPath}");
        }

        [Test]
        public void runner_stem_declares_initialize_on_load_method()
        {
            string repoRoot = GetRepoRoot();
            string src = File.ReadAllText(Path.Combine(repoRoot, RunnerPath));
            Assert.IsTrue(src.Contains("[InitializeOnLoadMethod]"),
                "AgentBridgeCommandRunner.cs stem must retain [InitializeOnLoadMethod] bootstrap.");
        }

        [Test]
        public void conformance_partial_delegates_to_bridge_conformance_service()
        {
            string repoRoot = GetRepoRoot();
            string src = File.ReadAllText(Path.Combine(repoRoot, ConformPath));
            Assert.IsTrue(src.Contains("BridgeConformanceService"),
                "AgentBridgeCommandRunner.Conformance.cs must delegate to BridgeConformanceService.");
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private static string GetRepoRoot()
        {
            string path = UnityEngine.Application.dataPath;
            return Path.GetDirectoryName(path);
        }
    }
}
