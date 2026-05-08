using NUnit.Framework;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace Territory.Tests.EditMode.Atomization.Roads
{
    /// <summary>
    /// Tracer tests: assert StrokeService extracted to Domains.Roads.Services assembly
    /// + IRoads facade interface present in Domains.Roads.
    /// Red baseline: Domains/Roads/ absent → asserts fail.
    /// Green: Roads.asmdef + StrokeService + IRoads all present; compile-check exits 0.
    /// §Red-Stage Proof anchor: RoadManagerAtomizationTests.cs::StrokeService_is_in_domains_roads_services_namespace
    /// </summary>
    public class RoadManagerAtomizationTests
    {
        [Test]
        public void StrokeService_is_in_domains_roads_services_namespace()
        {
            Type serviceType = typeof(Domains.Roads.Services.StrokeService);
            Assert.AreEqual("Domains.Roads.Services", serviceType.Namespace,
                $"Expected namespace 'Domains.Roads.Services', got '{serviceType.Namespace}'");
        }

        [Test]
        public void IRoads_facade_exists_in_domains_roads_namespace()
        {
            Type ifaceType = typeof(Domains.Roads.IRoads);
            Assert.AreEqual("Domains.Roads", ifaceType.Namespace,
                $"Expected namespace 'Domains.Roads', got '{ifaceType.Namespace}'");
        }

        [Test]
        public void IRoads_facade_exposes_TryCommitStreetStrokeForScenarioBuild_method()
        {
            Type ifaceType = typeof(Domains.Roads.IRoads);
            MethodInfo method = ifaceType.GetMethod("TryCommitStreetStrokeForScenarioBuild");
            Assert.IsNotNull(method, "IRoads must expose TryCommitStreetStrokeForScenarioBuild()");
        }

        [Test]
        public void IRoads_facade_exposes_CanPlaceRoadAt_method()
        {
            Type ifaceType = typeof(Domains.Roads.IRoads);
            MethodInfo method = ifaceType.GetMethod("CanPlaceRoadAt",
                new Type[] { typeof(UnityEngine.Vector2) });
            Assert.IsNotNull(method, "IRoads must expose CanPlaceRoadAt(Vector2 gridPos)");
        }

        [Test]
        public void StrokeService_MinStrokeCellCount_is_two()
        {
            // Behavior parity: minimum valid stroke = 2 cells (start + end)
            var field = typeof(Domains.Roads.Services.StrokeService)
                .GetField("MinStrokeCellCount", BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(field, "MinStrokeCellCount field must exist on StrokeService");
            int value = (int)field.GetValue(null);
            Assert.AreEqual(2, value, $"MinStrokeCellCount behavior parity: expected 2, got {value}");
        }

        [Test]
        public void StrokeService_ValidateStrokePath_rejects_null_path()
        {
            var svc = new Domains.Roads.Services.StrokeService();
            bool result = svc.ValidateStrokePath(null, out string error);
            Assert.IsFalse(result, "ValidateStrokePath(null) must return false");
            Assert.IsNotNull(error, "error must be set on null input");
        }

        [Test]
        public void StrokeService_ValidateStrokePath_rejects_single_cell()
        {
            var svc = new Domains.Roads.Services.StrokeService();
            var path = new System.Collections.Generic.List<UnityEngine.Vector2>
            {
                new UnityEngine.Vector2(1, 1)
            };
            bool result = svc.ValidateStrokePath(path, out string error);
            Assert.IsFalse(result, "ValidateStrokePath with 1 cell must return false");
            Assert.IsNotNull(error, "error must be set on under-minimum path");
        }

        [Test]
        public void StrokeService_ValidateStrokePath_accepts_two_cell_path()
        {
            var svc = new Domains.Roads.Services.StrokeService();
            var path = new System.Collections.Generic.List<UnityEngine.Vector2>
            {
                new UnityEngine.Vector2(1, 1),
                new UnityEngine.Vector2(2, 1)
            };
            bool result = svc.ValidateStrokePath(path, out string error);
            Assert.IsTrue(result, "ValidateStrokePath with 2 cells must return true");
            Assert.IsNull(error, "error must be null on valid path");
        }

        [Test]
        public void StrokeService_IsCardinalAdjacent_accepts_horizontal_stroke()
        {
            var svc = new Domains.Roads.Services.StrokeService();
            var path = new System.Collections.Generic.List<UnityEngine.Vector2>
            {
                new UnityEngine.Vector2(0, 0),
                new UnityEngine.Vector2(1, 0),
                new UnityEngine.Vector2(2, 0)
            };
            Assert.IsTrue(svc.IsCardinalAdjacent(path), "Horizontal stroke must be cardinal-adjacent");
        }

        [Test]
        public void StrokeService_IsCardinalAdjacent_rejects_diagonal_stroke()
        {
            var svc = new Domains.Roads.Services.StrokeService();
            var path = new System.Collections.Generic.List<UnityEngine.Vector2>
            {
                new UnityEngine.Vector2(0, 0),
                new UnityEngine.Vector2(1, 1)
            };
            Assert.IsFalse(svc.IsCardinalAdjacent(path), "Diagonal stroke must not be cardinal-adjacent");
        }

        [Test]
        public void StrokeService_HasDuplicateCells_detects_loop()
        {
            var svc = new Domains.Roads.Services.StrokeService();
            var path = new System.Collections.Generic.List<UnityEngine.Vector2>
            {
                new UnityEngine.Vector2(0, 0),
                new UnityEngine.Vector2(1, 0),
                new UnityEngine.Vector2(0, 0)
            };
            Assert.IsTrue(svc.HasDuplicateCells(path), "Looping stroke must have duplicate cells");
        }

        [Test]
        public void roads_asmdef_exists()
        {
            string path = Path.Combine(Application.dataPath, "Scripts", "Domains", "Roads", "Roads.asmdef");
            Assert.IsTrue(File.Exists(path), $"Roads.asmdef not found at: {path}");
        }

        [Test]
        public void roads_editor_asmdef_exists()
        {
            string path = Path.Combine(Application.dataPath, "Scripts", "Domains", "Roads", "Editor", "Roads.Editor.asmdef");
            Assert.IsTrue(File.Exists(path), $"Roads.Editor.asmdef not found at: {path}");
        }

        [Test]
        public void roads_asmdef_references_territory_developer_game()
        {
            string path = Path.Combine(Application.dataPath, "Scripts", "Domains", "Roads", "Roads.asmdef");
            Assert.IsTrue(File.Exists(path), "Roads.asmdef absent");
            string content = File.ReadAllText(path);
            Assert.IsTrue(content.Contains("7d8f9e2a1b4c5d6e7f8a9b0c1d2e3f4a"),
                "Roads.asmdef must reference TerritoryDeveloper.Game (GUID 7d8f9e2a...)");
        }
    }
}
