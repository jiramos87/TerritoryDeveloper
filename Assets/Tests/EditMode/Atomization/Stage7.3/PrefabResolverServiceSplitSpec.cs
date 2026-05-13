using NUnit.Framework;
using System.IO;

namespace Territory.Tests.EditMode.Atomization.Stage7_3
{
    /// <summary>
    /// §Red-Stage Proof anchor: PrefabResolverServiceSplitSpec.cs::prefab_resolver_service_split
    /// Stage 7.3: Tier-E — PrefabResolverService split into 3 sub-services.
    /// Green: PrefabResolverService.cs + PrefabLookupService.cs + PrefabVariantPickService.cs + PrefabCacheService.cs each ≤500 LOC.
    /// Facade contract (PrefabResolverService public API) unchanged.
    /// </summary>
    public class PrefabResolverServiceSplitSpec
    {
        private const string ServicesDir = "Assets/Scripts/Domains/Roads/Services";
        private const string OrchestratorPath = "Assets/Scripts/Domains/Roads/Services/PrefabResolverService.cs";
        private const string LookupPath = "Assets/Scripts/Domains/Roads/Services/PrefabLookupService.cs";
        private const string VariantPickPath = "Assets/Scripts/Domains/Roads/Services/PrefabVariantPickService.cs";
        private const string CachePath = "Assets/Scripts/Domains/Roads/Services/PrefabCacheService.cs";

        // ── §Red-Stage Proof anchor method ────────────────────────────────────────

        [Test]
        public void prefab_resolver_service_split()
        {
            string root = GetRepoRoot();

            // Assert 1: PrefabResolverService.cs ≤500 LOC
            string orchAbs = Path.Combine(root, OrchestratorPath);
            Assert.IsTrue(File.Exists(orchAbs), $"PrefabResolverService.cs not found at {orchAbs}");
            int orchLines = File.ReadAllLines(orchAbs).Length;
            Assert.LessOrEqual(orchLines, 500,
                $"PrefabResolverService.cs (orchestrator) must be ≤500 LOC. Current: {orchLines}");

            // Assert 2: PrefabLookupService.cs exists and ≤500 LOC
            string lookupAbs = Path.Combine(root, LookupPath);
            Assert.IsTrue(File.Exists(lookupAbs), $"PrefabLookupService.cs not found at {lookupAbs}");
            int lookupLines = File.ReadAllLines(lookupAbs).Length;
            Assert.LessOrEqual(lookupLines, 500,
                $"PrefabLookupService.cs must be ≤500 LOC. Current: {lookupLines}");

            // Assert 3: PrefabVariantPickService.cs exists and ≤500 LOC
            string variantAbs = Path.Combine(root, VariantPickPath);
            Assert.IsTrue(File.Exists(variantAbs), $"PrefabVariantPickService.cs not found at {variantAbs}");
            int variantLines = File.ReadAllLines(variantAbs).Length;
            Assert.LessOrEqual(variantLines, 500,
                $"PrefabVariantPickService.cs must be ≤500 LOC. Current: {variantLines}");

            // Assert 4: PrefabCacheService.cs exists and ≤500 LOC
            string cacheAbs = Path.Combine(root, CachePath);
            Assert.IsTrue(File.Exists(cacheAbs), $"PrefabCacheService.cs not found at {cacheAbs}");
            int cacheLines = File.ReadAllLines(cacheAbs).Length;
            Assert.LessOrEqual(cacheLines, 500,
                $"PrefabCacheService.cs must be ≤500 LOC. Current: {cacheLines}");

            // Assert 5: facade contract — PrefabResolverService still exposes ResolveForPath
            string orchSrc = File.ReadAllText(orchAbs);
            Assert.IsTrue(orchSrc.Contains("public") && orchSrc.Contains("ResolveForPath"),
                "PrefabResolverService must retain public ResolveForPath() — facade contract unchanged.");
        }

        [Test]
        public void lookup_service_exists()
        {
            string abs = Path.Combine(GetRepoRoot(), LookupPath);
            Assert.IsTrue(File.Exists(abs), $"PrefabLookupService.cs must exist at {abs}");
        }

        [Test]
        public void variant_pick_service_exists()
        {
            string abs = Path.Combine(GetRepoRoot(), VariantPickPath);
            Assert.IsTrue(File.Exists(abs), $"PrefabVariantPickService.cs must exist at {abs}");
        }

        [Test]
        public void cache_service_exists()
        {
            string abs = Path.Combine(GetRepoRoot(), CachePath);
            Assert.IsTrue(File.Exists(abs), $"PrefabCacheService.cs must exist at {abs}");
        }

        [Test]
        public void orchestrator_delegates_to_lookup()
        {
            string src = File.ReadAllText(Path.Combine(GetRepoRoot(), OrchestratorPath));
            Assert.IsTrue(src.Contains("PrefabLookupService"),
                "PrefabResolverService orchestrator must reference PrefabLookupService.");
        }

        [Test]
        public void orchestrator_delegates_to_variant_pick()
        {
            string src = File.ReadAllText(Path.Combine(GetRepoRoot(), OrchestratorPath));
            Assert.IsTrue(src.Contains("PrefabVariantPickService"),
                "PrefabResolverService orchestrator must reference PrefabVariantPickService.");
        }

        [Test]
        public void orchestrator_delegates_to_cache()
        {
            string src = File.ReadAllText(Path.Combine(GetRepoRoot(), OrchestratorPath));
            Assert.IsTrue(src.Contains("PrefabCacheService"),
                "PrefabResolverService orchestrator must reference PrefabCacheService.");
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
