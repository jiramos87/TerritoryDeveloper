using NUnit.Framework;
using System.IO;

namespace Territory.Tests.EditMode.Atomization.Meta
{
    /// <summary>
    /// Asserts post-atomization-architecture.md exists and lists every planned domain.
    /// Red baseline: doc absent or missing any domain → test fails.
    /// Green: doc lands with full domain catalog + cross-domain refs section.
    /// </summary>
    public class ArchitectureDocPresenceTests
    {
        private static readonly string DocPath = Path.Combine(
            Directory.GetCurrentDirectory(), "..", "..", "..", "..",
            "docs", "post-atomization-architecture.md");

        private static readonly string[] RequiredDomains =
        {
            "Terrain", "Roads", "Grid", "Water", "Zones", "UI", "Bridge", "Economy", "Geography"
        };

        [Test]
        public void architecture_doc_exists_and_lists_every_planned_domain()
        {
            string resolvedPath = Path.GetFullPath(DocPath);
            Assert.IsTrue(File.Exists(resolvedPath), $"Architecture doc not found at: {resolvedPath}");

            string content = File.ReadAllText(resolvedPath);
            Assert.IsTrue(content.Contains("## §Domain catalog table"), "Missing §Domain catalog table section");
            Assert.IsTrue(content.Contains("## §Cross-domain refs"), "Missing §Cross-domain refs section");
            Assert.IsTrue(content.Contains("## §Top-level non-domain"), "Missing §Top-level non-domain section");
            Assert.IsTrue(content.Contains("## §Editor sub-asmdef rule"), "Missing §Editor sub-asmdef rule section");

            foreach (string domain in RequiredDomains)
            {
                Assert.IsTrue(content.Contains(domain), $"Architecture doc missing domain: {domain}");
            }
        }
    }
}
