using NUnit.Framework;
using System.IO;

namespace Territory.Tests.EditMode.Atomization.Stage6_2
{
    /// <summary>
    /// §Red-Stage Proof anchor: AgentTestModeBatchRunnerThinSpec.cs::agent_test_mode_batch_runner_thin
    /// Stage 6.2: AgentTestModeBatchRunner Tier-D consolidation.
    /// Green: AgentTestModeBatchRunner.cs ≤200 LOC; Domains/Testing/Services/TestModeBatchService.cs exists.
    /// </summary>
    public class AgentTestModeBatchRunnerThinSpec
    {
        private const string RunnerPath         = "Assets/Scripts/Editor/AgentTestModeBatchRunner.cs";
        private const string TestModeSvcPath    = "Assets/Scripts/Domains/Testing/Services/TestModeBatchService.cs";

        // ── §Red-Stage Proof anchor method ────────────────────────────────────────

        [Test]
        public void agent_test_mode_batch_runner_thin()
        {
            string repoRoot = GetRepoRoot();

            // Assert 1: AgentTestModeBatchRunner.cs ≤200 LOC
            string runnerAbs = Path.Combine(repoRoot, RunnerPath);
            Assert.IsTrue(File.Exists(runnerAbs), $"AgentTestModeBatchRunner.cs not found at {runnerAbs}");
            int runnerLines = File.ReadAllLines(runnerAbs).Length;
            Assert.LessOrEqual(runnerLines, 200,
                $"AgentTestModeBatchRunner.cs must be THIN (≤200 LOC). Current: {runnerLines}");

            // Assert 2: TestModeBatchService.cs exists
            string svcAbs = Path.Combine(repoRoot, TestModeSvcPath);
            Assert.IsTrue(File.Exists(svcAbs), $"TestModeBatchService.cs must exist at {svcAbs}");
        }

        [Test]
        public void test_mode_batch_service_exists()
        {
            string repoRoot = GetRepoRoot();
            string absPath = Path.Combine(repoRoot, TestModeSvcPath);
            Assert.IsTrue(File.Exists(absPath), $"TestModeBatchService.cs must exist at {absPath}");
        }

        [Test]
        public void runner_declares_execute_method_name_const()
        {
            string repoRoot = GetRepoRoot();
            string src = File.ReadAllText(Path.Combine(repoRoot, RunnerPath));
            Assert.IsTrue(src.Contains("ExecuteMethodName"),
                "AgentTestModeBatchRunner.cs must declare ExecuteMethodName const.");
        }

        [Test]
        public void runner_delegates_report_to_test_mode_batch_service()
        {
            string repoRoot = GetRepoRoot();
            string src = File.ReadAllText(Path.Combine(repoRoot, RunnerPath));
            Assert.IsTrue(src.Contains("TestModeBatchService"),
                "AgentTestModeBatchRunner.cs must delegate to TestModeBatchService.");
        }

        [Test]
        public void runner_delegates_state_to_batch_state_service()
        {
            string repoRoot = GetRepoRoot();
            string src = File.ReadAllText(Path.Combine(repoRoot, RunnerPath));
            Assert.IsTrue(src.Contains("BatchStateService"),
                "AgentTestModeBatchRunner.cs must delegate to BatchStateService.");
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private static string GetRepoRoot()
        {
            string path = UnityEngine.Application.dataPath;
            return Path.GetDirectoryName(path);
        }
    }
}
