using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Territory.Tests.EditMode.Atomization.Terrain
{
    /// <summary>
    /// Tracer tests: assert TerraformingService extracted to Domains.Terrain.Services assembly
    /// composed under Terrain facade.
    /// Red baseline: Domains/Terrain/Services/TerraformingService.cs absent → asserts fail.
    /// Green: class present; ExpandDiagonalStepsToCardinal static + ComputePathBaseHeight instance accessible; compile-check exits 0.
    /// §Red-Stage Proof anchor: TerraformingServiceAtomizationTests.cs::TerraformingService_is_in_domains_terrain_services_namespace
    /// </summary>
    public class TerraformingServiceAtomizationTests
    {
        [Test]
        public void TerraformingService_is_in_domains_terrain_services_namespace()
        {
            Type serviceType = typeof(Domains.Terrain.Services.TerraformingService);
            Assert.AreEqual("Domains.Terrain.Services", serviceType.Namespace,
                $"Expected namespace 'Domains.Terrain.Services', got '{serviceType.Namespace}'");
        }

        [Test]
        public void TerraformingService_ExpandDiagonalStepsToCardinal_is_static_and_public()
        {
            MethodInfo method = typeof(Domains.Terrain.Services.TerraformingService)
                .GetMethod("ExpandDiagonalStepsToCardinal",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    new Type[] { typeof(IList<Vector2>) },
                    null);
            Assert.IsNotNull(method, "ExpandDiagonalStepsToCardinal(IList<Vector2>) must be public static on TerraformingService");
        }

        [Test]
        public void TerraformingService_ExpandDiagonalStepsToCardinal_null_returns_empty_list()
        {
            List<Vector2> result = Domains.Terrain.Services.TerraformingService.ExpandDiagonalStepsToCardinal(null);
            Assert.IsNotNull(result, "null input must return non-null empty list");
            Assert.AreEqual(0, result.Count, "null input must return empty list");
        }

        [Test]
        public void TerraformingService_ExpandDiagonalStepsToCardinal_single_cell_passthrough()
        {
            var path = new List<Vector2> { new Vector2(3, 4) };
            List<Vector2> result = Domains.Terrain.Services.TerraformingService.ExpandDiagonalStepsToCardinal(path);
            Assert.AreEqual(1, result.Count, "Single-cell path must pass through unchanged");
            Assert.AreEqual(new Vector2(3, 4), result[0]);
        }

        [Test]
        public void TerraformingService_ExpandDiagonalStepsToCardinal_diagonal_step_expands_to_two_cardinal()
        {
            // Diagonal step (0,0)->(1,1) must expand to (0,0)->(1,0)->(1,1) or (0,0)->(0,1)->(1,1)
            var path = new List<Vector2> { new Vector2(0, 0), new Vector2(1, 1) };
            List<Vector2> result = Domains.Terrain.Services.TerraformingService.ExpandDiagonalStepsToCardinal(path);
            Assert.AreEqual(3, result.Count, "Diagonal step (0,0)->(1,1) must expand to 3 cells (two cardinal steps)");
            Assert.AreEqual(new Vector2(0, 0), result[0], "First cell must be origin");
            Assert.AreEqual(new Vector2(1, 1), result[2], "Last cell must be destination");
            // Intermediate must differ from origin and destination by exactly one axis
            Vector2 mid = result[1];
            bool validMid = (mid == new Vector2(1, 0)) || (mid == new Vector2(0, 1));
            Assert.IsTrue(validMid, $"Intermediate cell {mid} must be cardinal neighbor of origin");
        }

        [Test]
        public void TerraformingService_ExpandDiagonalStepsToCardinal_orthogonal_step_unchanged()
        {
            var path = new List<Vector2> { new Vector2(0, 0), new Vector2(1, 0), new Vector2(2, 0) };
            List<Vector2> result = Domains.Terrain.Services.TerraformingService.ExpandDiagonalStepsToCardinal(path);
            Assert.AreEqual(3, result.Count, "Orthogonal path must not expand");
        }

        [Test]
        public void TerraformingService_ComputePathBaseHeight_method_exists()
        {
            MethodInfo method = typeof(Domains.Terrain.Services.TerraformingService)
                .GetMethod("ComputePathBaseHeight",
                    BindingFlags.Public | BindingFlags.Instance,
                    null,
                    new Type[] { typeof(IList<Vector2>) },
                    null);
            Assert.IsNotNull(method, "ComputePathBaseHeight(IList<Vector2>) must be public instance on TerraformingService");
        }

        [Test]
        public void TerraformingService_ComputePathPlan_method_exists()
        {
            MethodInfo method = typeof(Domains.Terrain.Services.TerraformingService)
                .GetMethod("ComputePathPlan",
                    BindingFlags.Public | BindingFlags.Instance);
            Assert.IsNotNull(method, "ComputePathPlan must be public instance on TerraformingService");
        }

        [Test]
        public void TerraformingService_constructor_accepts_delegate_params()
        {
            // Verify constructor signature accepts Func delegates (dependency injection pattern)
            var ctors = typeof(Domains.Terrain.Services.TerraformingService).GetConstructors();
            Assert.IsTrue(ctors.Length > 0, "TerraformingService must have at least one public constructor");
            // Primary constructor must accept at least 7 parameters (7 delegate injections)
            bool hasLargeConstructor = false;
            foreach (var ctor in ctors)
            {
                if (ctor.GetParameters().Length >= 7)
                {
                    hasLargeConstructor = true;
                    break;
                }
            }
            Assert.IsTrue(hasLargeConstructor, "TerraformingService must have a constructor with >= 7 parameters for delegate injection");
        }
    }
}
