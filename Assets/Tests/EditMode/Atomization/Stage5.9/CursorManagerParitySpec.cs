using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Territory.Tests.EditMode.Atomization.Stage5_9
{
    /// <summary>
    /// §Red-Stage Proof anchor: CursorManagerParitySpec.cs::cursor_manager_parity_verified
    /// Stage 5.9: CursorManager parity verification post-Tier-A scaffolds.
    /// Re-validates Stage 1.0 tracer invariants after all Tier-A domain scaffolds landed.
    /// Green: CursorManager ≤200 LOC AND ICursor resolves via typeof AND CursorService exists.
    /// </summary>
    public class CursorManagerParitySpec
    {
        private const string CursorManagerPath = "Assets/Scripts/Managers/GameManagers/CursorManager.cs";
        private const string CursorServicePath = "Assets/Scripts/Domains/Cursor/Services/CursorService.cs";

        // ── §Red-Stage Proof anchor method ────────────────────────────────────────

        [Test]
        public void cursor_manager_parity_verified()
        {
            string repoRoot = GetRepoRoot();

            // Assert 1: CursorManager.cs ≤200 LOC
            string hubPath = Path.Combine(repoRoot, CursorManagerPath);
            Assert.IsTrue(File.Exists(hubPath), $"CursorManager.cs not found at {hubPath}");
            int lineCount = File.ReadAllLines(hubPath).Length;
            Assert.LessOrEqual(lineCount, 200,
                $"CursorManager.cs must be THIN (≤200 LOC). Current: {lineCount} lines.");

            // Assert 2: ICursor resolves via reflection (Domains.Cursor.ICursor)
            Type iface = typeof(Domains.Cursor.ICursor);
            Assert.IsNotNull(iface, "Domains.Cursor.ICursor must be resolvable.");
            Assert.AreEqual("Domains.Cursor", iface.Namespace,
                $"ICursor namespace mismatch: {iface.Namespace}");

            // Assert 3: CursorService.cs exists at canonical path
            string svcPath = Path.Combine(repoRoot, CursorServicePath);
            Assert.IsTrue(File.Exists(svcPath),
                $"CursorService.cs must exist at {svcPath}.");
        }

        // ── Tier-A scaffold regression checks ────────────────────────────────────

        [Test]
        public void cursor_manager_hub_path_unchanged_post_tier_a()
        {
            string repoRoot = GetRepoRoot();
            string absPath = Path.Combine(repoRoot, CursorManagerPath);
            Assert.IsTrue(File.Exists(absPath),
                $"CursorManager.cs must remain at canonical path after Tier-A scaffolds: {CursorManagerPath}");
        }

        [Test]
        public void cursor_manager_implements_ICursor_post_tier_a()
        {
            Type hubType = FindCursorManagerType();
            Assert.IsNotNull(hubType, "CursorManager type must be resolvable via reflection");
            Type iface = typeof(Domains.Cursor.ICursor);
            Assert.IsTrue(iface.IsAssignableFrom(hubType),
                "CursorManager must still implement Domains.Cursor.ICursor after Tier-A scaffolds.");
        }

        [Test]
        public void cursor_manager_namespace_unchanged_post_tier_a()
        {
            Type t = FindCursorManagerType();
            Assert.IsNotNull(t, "CursorManager type must be resolvable");
            Assert.AreEqual("Territory.UI", t.Namespace,
                $"CursorManager namespace must remain Territory.UI after Tier-A scaffolds. Got: {t.Namespace}");
        }

        [Test]
        public void cursor_service_in_correct_namespace_post_tier_a()
        {
            Type t = typeof(Domains.Cursor.Services.CursorService);
            Assert.AreEqual("Domains.Cursor.Services", t.Namespace,
                $"CursorService namespace mismatch post Tier-A scaffolds: {t.Namespace}");
        }

        [Test]
        public void hub_registers_ICursor_in_source_post_tier_a()
        {
            string repoRoot = GetRepoRoot();
            string absPath = Path.Combine(repoRoot, CursorManagerPath);
            Assert.IsTrue(File.Exists(absPath));
            string src = File.ReadAllText(absPath);
            Assert.IsTrue(src.Contains("Register<ICursor>(this)"),
                "CursorManager.Awake must call _registry.Register<ICursor>(this) post Tier-A scaffolds.");
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private static Type FindCursorManagerType()
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = asm.GetType("Territory.UI.CursorManager");
                if (t != null) return t;
            }
            return null;
        }

        private static string GetRepoRoot()
        {
            string path = UnityEngine.Application.dataPath;
            return Path.GetDirectoryName(path);
        }
    }
}
