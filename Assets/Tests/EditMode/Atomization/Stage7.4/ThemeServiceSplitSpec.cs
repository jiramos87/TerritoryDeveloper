using NUnit.Framework;
using System.IO;

namespace Territory.Tests.EditMode.Atomization.Stage7_4
{
    /// <summary>
    /// §Red-Stage Proof anchor: ThemeServiceSplitSpec.cs::theme_service_split
    /// Stage 7.4: Tier-E — ThemeService split into 3 sub-services.
    /// Green: ThemeService.cs + ThemeTokenResolveService.cs + ThemeStyleApplyService.cs + ThemeCacheService.cs each ≤500 LOC.
    /// Facade contract (ITheme) unchanged.
    /// </summary>
    public class ThemeServiceSplitSpec
    {
        private const string ServicesDir = "Assets/Scripts/Domains/UI/Services";
        private const string OrchestratorPath = "Assets/Scripts/Domains/UI/Services/ThemeService.cs";
        private const string TokenResolvePath = "Assets/Scripts/Domains/UI/Services/ThemeTokenResolveService.cs";
        private const string StyleApplyPath = "Assets/Scripts/Domains/UI/Services/ThemeStyleApplyService.cs";
        private const string CachePath = "Assets/Scripts/Domains/UI/Services/ThemeCacheService.cs";

        // ── §Red-Stage Proof anchor method ────────────────────────────────────────

        [Test]
        public void theme_service_split()
        {
            string root = GetRepoRoot();

            // Assert 1: ThemeService.cs ≤500 LOC
            string orchAbs = Path.Combine(root, OrchestratorPath);
            Assert.IsTrue(File.Exists(orchAbs), $"ThemeService.cs not found at {orchAbs}");
            int orchLines = File.ReadAllLines(orchAbs).Length;
            Assert.LessOrEqual(orchLines, 500,
                $"ThemeService.cs (orchestrator) must be ≤500 LOC. Current: {orchLines}");

            // Assert 2: ThemeTokenResolveService.cs exists and ≤500 LOC
            string tokenAbs = Path.Combine(root, TokenResolvePath);
            Assert.IsTrue(File.Exists(tokenAbs), $"ThemeTokenResolveService.cs not found at {tokenAbs}");
            int tokenLines = File.ReadAllLines(tokenAbs).Length;
            Assert.LessOrEqual(tokenLines, 500,
                $"ThemeTokenResolveService.cs must be ≤500 LOC. Current: {tokenLines}");

            // Assert 3: ThemeStyleApplyService.cs exists and ≤500 LOC
            string styleAbs = Path.Combine(root, StyleApplyPath);
            Assert.IsTrue(File.Exists(styleAbs), $"ThemeStyleApplyService.cs not found at {styleAbs}");
            int styleLines = File.ReadAllLines(styleAbs).Length;
            Assert.LessOrEqual(styleLines, 500,
                $"ThemeStyleApplyService.cs must be ≤500 LOC. Current: {styleLines}");

            // Assert 4: ThemeCacheService.cs exists and ≤500 LOC
            string cacheAbs = Path.Combine(root, CachePath);
            Assert.IsTrue(File.Exists(cacheAbs), $"ThemeCacheService.cs not found at {cacheAbs}");
            int cacheLines = File.ReadAllLines(cacheAbs).Length;
            Assert.LessOrEqual(cacheLines, 500,
                $"ThemeCacheService.cs must be ≤500 LOC. Current: {cacheLines}");

            // Assert 5: facade resolve unchanged — ITheme in ThemeService
            string orchSrc = File.ReadAllText(orchAbs);
            Assert.IsTrue(orchSrc.Contains("ITheme"),
                "ThemeService must implement ITheme facade — contract unchanged.");
        }

        [Test]
        public void token_resolve_service_exists()
        {
            string abs = Path.Combine(GetRepoRoot(), TokenResolvePath);
            Assert.IsTrue(File.Exists(abs), $"ThemeTokenResolveService.cs must exist at {abs}");
        }

        [Test]
        public void style_apply_service_exists()
        {
            string abs = Path.Combine(GetRepoRoot(), StyleApplyPath);
            Assert.IsTrue(File.Exists(abs), $"ThemeStyleApplyService.cs must exist at {abs}");
        }

        [Test]
        public void cache_service_exists()
        {
            string abs = Path.Combine(GetRepoRoot(), CachePath);
            Assert.IsTrue(File.Exists(abs), $"ThemeCacheService.cs must exist at {abs}");
        }

        [Test]
        public void orchestrator_delegates_to_token_resolve()
        {
            string src = File.ReadAllText(Path.Combine(GetRepoRoot(), OrchestratorPath));
            Assert.IsTrue(src.Contains("ThemeTokenResolveService"),
                "ThemeService orchestrator must reference ThemeTokenResolveService.");
        }

        [Test]
        public void orchestrator_delegates_to_style_apply()
        {
            string src = File.ReadAllText(Path.Combine(GetRepoRoot(), OrchestratorPath));
            Assert.IsTrue(src.Contains("ThemeStyleApplyService"),
                "ThemeService orchestrator must reference ThemeStyleApplyService.");
        }

        [Test]
        public void orchestrator_delegates_to_cache()
        {
            string src = File.ReadAllText(Path.Combine(GetRepoRoot(), OrchestratorPath));
            Assert.IsTrue(src.Contains("ThemeCacheService"),
                "ThemeService orchestrator must reference ThemeCacheService.");
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
