using NUnit.Framework;
using System.IO;

namespace Territory.Tests.EditMode.Atomization.Stage7_1
{
    /// <summary>
    /// §Red-Stage Proof anchor: InterstateServiceSplitSpec.cs::interstate_service_split
    /// Stage 7.1: Tier-E — InterstateService split into 3 sub-services.
    /// Green: InterstateService.cs + InterstateGenService.cs + InterstateConformanceService.cs + InterstateFlowTrackerService.cs each ≤500 LOC.
    /// Facade contract (IRoads / InterstateService public API) unchanged.
    /// </summary>
    public class InterstateServiceSplitSpec
    {
        private const string ServicesDir = "Assets/Scripts/Domains/Roads/Services";
        private const string OrchestratorPath = "Assets/Scripts/Domains/Roads/Services/InterstateService.cs";
        private const string GenPath = "Assets/Scripts/Domains/Roads/Services/InterstateGenService.cs";
        private const string ConformancePath = "Assets/Scripts/Domains/Roads/Services/InterstateConformanceService.cs";
        private const string FlowTrackerPath = "Assets/Scripts/Domains/Roads/Services/InterstateFlowTrackerService.cs";

        // ── §Red-Stage Proof anchor method ────────────────────────────────────────

        [Test]
        public void interstate_service_split()
        {
            string root = GetRepoRoot();

            // Assert 1: InterstateService.cs ≤500 LOC
            string orchAbs = Path.Combine(root, OrchestratorPath);
            Assert.IsTrue(File.Exists(orchAbs), $"InterstateService.cs not found at {orchAbs}");
            int orchLines = File.ReadAllLines(orchAbs).Length;
            Assert.LessOrEqual(orchLines, 500,
                $"InterstateService.cs (orchestrator) must be ≤500 LOC. Current: {orchLines}");

            // Assert 2: InterstateGenService.cs exists and ≤500 LOC
            string genAbs = Path.Combine(root, GenPath);
            Assert.IsTrue(File.Exists(genAbs), $"InterstateGenService.cs not found at {genAbs}");
            int genLines = File.ReadAllLines(genAbs).Length;
            Assert.LessOrEqual(genLines, 500,
                $"InterstateGenService.cs must be ≤500 LOC. Current: {genLines}");

            // Assert 3: InterstateConformanceService.cs exists and ≤500 LOC
            string confAbs = Path.Combine(root, ConformancePath);
            Assert.IsTrue(File.Exists(confAbs), $"InterstateConformanceService.cs not found at {confAbs}");
            int confLines = File.ReadAllLines(confAbs).Length;
            Assert.LessOrEqual(confLines, 500,
                $"InterstateConformanceService.cs must be ≤500 LOC. Current: {confLines}");

            // Assert 4: InterstateFlowTrackerService.cs exists and ≤500 LOC
            string flowAbs = Path.Combine(root, FlowTrackerPath);
            Assert.IsTrue(File.Exists(flowAbs), $"InterstateFlowTrackerService.cs not found at {flowAbs}");
            int flowLines = File.ReadAllLines(flowAbs).Length;
            Assert.LessOrEqual(flowLines, 500,
                $"InterstateFlowTrackerService.cs must be ≤500 LOC. Current: {flowLines}");

            // Assert 5: facade contract — InterstateService still has GenerateAndPlaceInterstate
            string orchSrc = File.ReadAllText(orchAbs);
            Assert.IsTrue(orchSrc.Contains("public bool GenerateAndPlaceInterstate"),
                "InterstateService must retain public GenerateAndPlaceInterstate() — facade contract unchanged.");
        }

        [Test]
        public void gen_service_exists()
        {
            string abs = Path.Combine(GetRepoRoot(), GenPath);
            Assert.IsTrue(File.Exists(abs), $"InterstateGenService.cs must exist at {abs}");
        }

        [Test]
        public void conformance_service_exists()
        {
            string abs = Path.Combine(GetRepoRoot(), ConformancePath);
            Assert.IsTrue(File.Exists(abs), $"InterstateConformanceService.cs must exist at {abs}");
        }

        [Test]
        public void flow_tracker_service_exists()
        {
            string abs = Path.Combine(GetRepoRoot(), FlowTrackerPath);
            Assert.IsTrue(File.Exists(abs), $"InterstateFlowTrackerService.cs must exist at {abs}");
        }

        [Test]
        public void orchestrator_delegates_to_gen()
        {
            string src = File.ReadAllText(Path.Combine(GetRepoRoot(), OrchestratorPath));
            Assert.IsTrue(src.Contains("InterstateGenService"),
                "InterstateService orchestrator must reference InterstateGenService.");
        }

        [Test]
        public void orchestrator_delegates_to_conformance()
        {
            string src = File.ReadAllText(Path.Combine(GetRepoRoot(), OrchestratorPath));
            Assert.IsTrue(src.Contains("InterstateConformanceService"),
                "InterstateService orchestrator must reference InterstateConformanceService.");
        }

        [Test]
        public void orchestrator_delegates_to_flow_tracker()
        {
            string src = File.ReadAllText(Path.Combine(GetRepoRoot(), OrchestratorPath));
            Assert.IsTrue(src.Contains("InterstateFlowTrackerService"),
                "InterstateService orchestrator must reference InterstateFlowTrackerService.");
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
