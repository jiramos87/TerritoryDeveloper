using NUnit.Framework;
using System.IO;
using UnityEngine;

namespace Territory.Tests.EditMode.Atomization.GridTracer
{
    /// <summary>
    /// Asserts Grid.asmdef bootstraps cleanly with no legacy Managers/ refs.
    /// Red baseline: asmdef absent or cyclic ref → Unity compile-check fails.
    /// Green: Grid.asmdef + Editor sub-asmdef present; compile-check exits 0.
    /// </summary>
    public class AsmdefBootstrapTests
    {
        private static readonly string GridAsmdefPath = Path.Combine(
            Application.dataPath, "Scripts", "Domains", "Grid", "Grid.asmdef");

        private static readonly string EditorAsmdefPath = Path.Combine(
            Application.dataPath, "Scripts", "Domains", "Grid", "Editor", "Grid.Editor.asmdef");

        [Test]
        public void grid_asmdef_exists()
        {
            Assert.IsTrue(File.Exists(GridAsmdefPath), $"Grid.asmdef not found at: {GridAsmdefPath}");
        }

        [Test]
        public void grid_editor_asmdef_exists()
        {
            Assert.IsTrue(File.Exists(EditorAsmdefPath), $"Grid.Editor.asmdef not found at: {EditorAsmdefPath}");
        }

        [Test]
        public void grid_asmdef_does_not_reference_legacy_managers_asmdef()
        {
            Assert.IsTrue(File.Exists(GridAsmdefPath), "Grid.asmdef absent");
            string content = File.ReadAllText(GridAsmdefPath);
            Assert.IsFalse(content.Contains("TerritoryDeveloper.Game"), "Grid.asmdef must not reference TerritoryDeveloper.Game (legacy)");
            Assert.IsFalse(content.Contains("Managers"), "Grid.asmdef must not reference any Managers asmdef");
        }

        [Test]
        public void grid_editor_asmdef_references_grid_asmdef()
        {
            Assert.IsTrue(File.Exists(EditorAsmdefPath), "Grid.Editor.asmdef absent");
            string content = File.ReadAllText(EditorAsmdefPath);
            Assert.IsTrue(content.Contains("\"Grid\""), "Grid.Editor.asmdef must reference Grid asmdef");
        }
    }
}
