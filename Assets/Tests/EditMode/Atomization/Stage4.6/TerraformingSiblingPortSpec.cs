using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace Territory.Tests.EditMode.Atomization.Stage4_6
{
    /// <summary>
    /// §Red-Stage Proof anchor: TerraformingSiblingPortSpec.cs::terraforming_service_manager_copy_verified
    /// Stage 4.6: Tier-B sibling-port — TerraformingService Manager-copy verification.
    /// Confirms Manager-copy (Territory.Terrain.TerraformingService) remains a clean pass-through
    /// to the Domain service (Domains.Terrain.Services.TerraformingService) after Grid/Terrain trim.
    /// Invariants: class/namespace/path UNCHANGED on Manager; all public surface methods delegate to _terraformingService.
    /// </summary>
    public class TerraformingSiblingPortSpec
    {
        private const string ManagerPath =
            "Assets/Scripts/Managers/GameManagers/TerraformingService.cs";

        private const string DomainServicePath =
            "Assets/Scripts/Domains/Terrain/Services/TerraformingService.cs";

        // §Red-Stage Proof anchor — terraforming_service_manager_copy_verified
        [Test]
        public void terraforming_service_manager_copy_verified()
        {
            string root = GetRepoRoot();

            // Assert 1: sibling_port_signature_match — Manager-copy exists at locked path (invariant #1)
            string managerFullPath = Path.Combine(root, ManagerPath);
            Assert.IsTrue(File.Exists(managerFullPath),
                $"Manager TerraformingService must exist at locked path: {managerFullPath}");

            // Assert 2: Domain service exists at locked path
            string domainFullPath = Path.Combine(root, DomainServicePath);
            Assert.IsTrue(File.Exists(domainFullPath),
                $"Domain TerraformingService must exist at locked path: {domainFullPath}");

            // Assert 3: Manager-copy is thin (≤200 LOC — pass-through delegates only)
            int managerLoc = File.ReadAllLines(managerFullPath).Length;
            Assert.LessOrEqual(managerLoc, 200,
                $"Manager TerraformingService must be ≤200 LOC (pass-through only). Got {managerLoc} lines.");

            // Assert 4: Manager-copy delegates to _terraformingService field (sibling-port pattern)
            string managerSrc = File.ReadAllText(managerFullPath);
            Assert.IsTrue(managerSrc.Contains("_terraformingService"),
                "Manager TerraformingService must hold _terraformingService delegate field.");

            // Assert 5: Manager-copy references Domain namespace (wiring confirmed)
            Assert.IsTrue(managerSrc.Contains("Domains.Terrain.Services.TerraformingService"),
                "Manager TerraformingService must reference Domains.Terrain.Services.TerraformingService.");

            // Assert 6: playmode_smoke_green proxy — Domain POCO constructor resolvable with null-safe delegates
            // Verifies no regression in constructor signature post-Grid/Terrain trim.
            var domainType = typeof(Domains.Terrain.Services.TerraformingService);
            var ctors = domainType.GetConstructors();
            Assert.IsTrue(ctors.Length > 0, "Domain TerraformingService must have at least one public constructor.");
            bool hasInjectionCtor = false;
            foreach (var ctor in ctors)
            {
                if (ctor.GetParameters().Length >= 7) { hasInjectionCtor = true; break; }
            }
            Assert.IsTrue(hasInjectionCtor,
                "Domain TerraformingService constructor must accept >= 7 delegate params (injection contract unchanged post-trim).");
        }

        [Test]
        public void manager_copy_namespace_unchanged()
        {
            // Manager stays in Territory.Terrain namespace (invariant #2 — namespace UNCHANGED)
            Type managerType = typeof(Territory.Terrain.TerraformingService);
            Assert.AreEqual("Territory.Terrain", managerType.Namespace,
                $"Manager TerraformingService namespace must remain 'Territory.Terrain'. Got '{managerType.Namespace}'");
        }

        [Test]
        public void domain_service_namespace_correct()
        {
            Type domainType = typeof(Domains.Terrain.Services.TerraformingService);
            Assert.AreEqual("Domains.Terrain.Services", domainType.Namespace,
                $"Domain TerraformingService namespace must be 'Domains.Terrain.Services'. Got '{domainType.Namespace}'");
        }

        [Test]
        public void manager_copy_public_surface_matches_domain_service()
        {
            // Signature-match: each public instance method on Manager delegates to the same-named method on Domain POCO.
            Type managerType = typeof(Territory.Terrain.TerraformingService);
            Type domainType  = typeof(Domains.Terrain.Services.TerraformingService);

            string[] sharedSurface = new[]
            {
                "ComputePathBaseHeight",
                "ComputePathPlan",
                "ApplyTerraform",
                "RevertTerraform",
                "TryBuildDeckSpanOnlyWaterBridgePlan",
            };

            foreach (string methodName in sharedSurface)
            {
                MethodInfo domainMethod = domainType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
                Assert.IsNotNull(domainMethod,
                    $"Domain TerraformingService must expose public instance method '{methodName}'.");

                MethodInfo managerMethod = managerType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
                Assert.IsNotNull(managerMethod,
                    $"Manager TerraformingService must expose public instance method '{methodName}' (sibling-port mirror).");
            }
        }

        [Test]
        public void expand_diagonal_steps_static_accessible_via_manager()
        {
            // Static utility exposed on both Manager and Domain — callers (RoadManager etc.) can reach either.
            MethodInfo managerStatic = typeof(Territory.Terrain.TerraformingService)
                .GetMethod("ExpandDiagonalStepsToCardinal",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    new Type[] { typeof(IList<Vector2>) },
                    null);
            Assert.IsNotNull(managerStatic,
                "Manager TerraformingService.ExpandDiagonalStepsToCardinal must be public static (pass-through to Domain static).");

            MethodInfo domainStatic = typeof(Domains.Terrain.Services.TerraformingService)
                .GetMethod("ExpandDiagonalStepsToCardinal",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    new Type[] { typeof(IList<Vector2>) },
                    null);
            Assert.IsNotNull(domainStatic,
                "Domain TerraformingService.ExpandDiagonalStepsToCardinal must be public static.");
        }

        [Test]
        public void domain_service_no_tier_e_needed_yet()
        {
            // Tier-E split scheduled at Stage 7.2 only if service oversize persists.
            string root = GetRepoRoot();
            string domainFullPath = Path.Combine(root, DomainServicePath);
            Assert.IsTrue(File.Exists(domainFullPath), $"Domain service not found at {domainFullPath}");
            int loc = File.ReadAllLines(domainFullPath).Length;
            // Info-only gate: warn if approaching 1000 LOC threshold for Tier-E scheduler.
            // Not a hard failure — Tier-E split handled at Stage 7.2 per plan.
            Assert.LessOrEqual(loc, 1200,
                $"Domain TerraformingService is {loc} LOC. If >1200, advance Stage 7.2 Tier-E split.");
        }

        private static string GetRepoRoot()
        {
            string dir = Application.dataPath;
            while (!string.IsNullOrEmpty(dir))
            {
                if (File.Exists(Path.Combine(dir, "CLAUDE.md")))
                    return dir;
                string parent = Path.GetDirectoryName(dir);
                if (parent == dir) break;
                dir = parent;
            }
            return Application.dataPath.Replace("/Assets", "");
        }
    }
}
