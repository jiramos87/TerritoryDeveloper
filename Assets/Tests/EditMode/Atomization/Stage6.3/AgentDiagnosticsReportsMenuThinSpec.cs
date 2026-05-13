using NUnit.Framework;
using System.IO;

namespace Territory.Tests.EditMode.Atomization.Stage6_3
{
    /// <summary>
    /// §Red-Stage Proof anchor: AgentDiagnosticsReportsMenuThinSpec.cs::agent_diagnostics_reports_menu_thin
    /// Stage 6.3: AgentDiagnosticsReportsMenu Tier-D consolidation.
    /// Green: AgentDiagnosticsReportsMenu.cs ≤200 LOC; DiagnosticsReportsService.cs exists.
    /// </summary>
    public class AgentDiagnosticsReportsMenuThinSpec
    {
        private const string MenuPath   = "Assets/Scripts/Editor/AgentDiagnosticsReportsMenu.cs";
        private const string ServicePath = "Assets/Scripts/Editor/Bridge/Services/DiagnosticsReportsService.cs";

        // ── §Red-Stage Proof anchor method ────────────────────────────────────────

        [Test]
        public void agent_diagnostics_reports_menu_thin()
        {
            string repoRoot = GetRepoRoot();

            // Assert 1: AgentDiagnosticsReportsMenu.cs ≤200 LOC
            string menuAbs = Path.Combine(repoRoot, MenuPath);
            Assert.IsTrue(File.Exists(menuAbs), $"AgentDiagnosticsReportsMenu.cs not found at {menuAbs}");
            int lineCount = File.ReadAllLines(menuAbs).Length;
            Assert.LessOrEqual(lineCount, 200,
                $"AgentDiagnosticsReportsMenu.cs must be THIN (≤200 LOC). Current: {lineCount}");

            // Assert 2: DiagnosticsReportsService.cs exists
            string svcAbs = Path.Combine(repoRoot, ServicePath);
            Assert.IsTrue(File.Exists(svcAbs), $"DiagnosticsReportsService.cs must exist at {svcAbs}");
        }

        [Test]
        public void diagnostics_report_service_exists()
        {
            string repoRoot = GetRepoRoot();
            string absPath = Path.Combine(repoRoot, ServicePath);
            Assert.IsTrue(File.Exists(absPath), $"DiagnosticsReportsService.cs must exist at {absPath}");
        }

        [Test]
        public void menu_delegates_to_diagnostics_reports_service()
        {
            string repoRoot = GetRepoRoot();
            string src = File.ReadAllText(Path.Combine(repoRoot, MenuPath));
            Assert.IsTrue(src.Contains("DiagnosticsReportsService"),
                "AgentDiagnosticsReportsMenu.cs must delegate to DiagnosticsReportsService.");
        }

        [Test]
        public void service_contains_build_agent_context_method()
        {
            string repoRoot = GetRepoRoot();
            string src = File.ReadAllText(Path.Combine(repoRoot, ServicePath));
            Assert.IsTrue(src.Contains("BuildAgentContextJsonString"),
                "DiagnosticsReportsService.cs must contain BuildAgentContextJsonString.");
        }

        [Test]
        public void service_contains_build_sorting_debug_method()
        {
            string repoRoot = GetRepoRoot();
            string src = File.ReadAllText(Path.Combine(repoRoot, ServicePath));
            Assert.IsTrue(src.Contains("BuildSortingDebugMarkdownString"),
                "DiagnosticsReportsService.cs must contain BuildSortingDebugMarkdownString.");
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private static string GetRepoRoot()
        {
            string path = UnityEngine.Application.dataPath;
            return Path.GetDirectoryName(path);
        }
    }
}
