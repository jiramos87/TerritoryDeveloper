using NUnit.Framework;
using System.IO;

namespace Territory.Tests.EditMode.Atomization.Meta
{
    /// <summary>
    /// Asserts large-file-atomization-componentization-strategy.md exists and carries all required sections.
    /// Red baseline: doc absent → File.Exists fails. Green: doc lands with all 5 headings.
    /// </summary>
    public class StrategyDocPresenceTests
    {
        private static readonly string DocPath = Path.Combine(
            Directory.GetCurrentDirectory(), "..", "..", "..", "..",
            "docs", "large-file-atomization-componentization-strategy.md");

        [Test]
        public void strategy_doc_exists_and_has_required_sections()
        {
            string resolvedPath = Path.GetFullPath(DocPath);
            Assert.IsTrue(File.Exists(resolvedPath), $"Strategy doc not found at: {resolvedPath}");

            string content = File.ReadAllText(resolvedPath);
            Assert.IsTrue(content.Contains("## §Strategy γ"), "Missing §Strategy γ section");
            Assert.IsTrue(content.Contains("## §Folder shape"), "Missing §Folder shape section");
            Assert.IsTrue(content.Contains("## §Naming rules"), "Missing §Naming rules section");
            Assert.IsTrue(content.Contains("## §Asmdef boundary rule"), "Missing §Asmdef boundary rule section");
            Assert.IsTrue(content.Contains("## §Anti-patterns"), "Missing §Anti-patterns section");
            Assert.IsTrue(content.Contains("long-method-allowed:"), "Missing escape-hatch comment grammar");
        }
    }
}
