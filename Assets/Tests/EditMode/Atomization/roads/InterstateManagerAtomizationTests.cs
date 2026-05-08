using NUnit.Framework;
using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;

namespace Territory.Tests.EditMode.Atomization.Roads
{
    /// <summary>
    /// Tracer tests: assert InterstateService extracted to Domains.Roads.Services assembly
    /// + IInterstate facade interface present in Domains.Roads.
    /// Red baseline: Domains/Roads/Services/InterstateService.cs + IInterstate.cs absent → asserts fail.
    /// Green: InterstateService + IInterstate present; compile-check exits 0.
    /// §Red-Stage Proof anchor: InterstateManagerAtomizationTests.cs::InterstateService_is_in_domains_roads_services_namespace
    /// </summary>
    public class InterstateManagerAtomizationTests
    {
        [Test]
        public void InterstateService_is_in_domains_roads_services_namespace()
        {
            Type serviceType = typeof(Domains.Roads.Services.InterstateService);
            Assert.AreEqual("Domains.Roads.Services", serviceType.Namespace,
                $"Expected namespace 'Domains.Roads.Services', got '{serviceType.Namespace}'");
        }

        [Test]
        public void IInterstate_facade_exists_in_domains_roads_namespace()
        {
            Type ifaceType = typeof(Domains.Roads.IInterstate);
            Assert.AreEqual("Domains.Roads", ifaceType.Namespace,
                $"Expected namespace 'Domains.Roads', got '{ifaceType.Namespace}'");
        }

        [Test]
        public void IInterstate_facade_exposes_IsConnectedToInterstate_property()
        {
            Type ifaceType = typeof(Domains.Roads.IInterstate);
            PropertyInfo prop = ifaceType.GetProperty("IsConnectedToInterstate");
            Assert.IsNotNull(prop, "IInterstate must expose IsConnectedToInterstate property");
        }

        [Test]
        public void IInterstate_facade_exposes_IsInterstateAt_method()
        {
            Type ifaceType = typeof(Domains.Roads.IInterstate);
            MethodInfo method = ifaceType.GetMethod("IsInterstateAt", new Type[] { typeof(int), typeof(int) });
            Assert.IsNotNull(method, "IInterstate must expose IsInterstateAt(int x, int y)");
        }

        [Test]
        public void IInterstate_facade_exposes_CanPlaceStreetFrom_method()
        {
            Type ifaceType = typeof(Domains.Roads.IInterstate);
            MethodInfo method = ifaceType.GetMethod("CanPlaceStreetFrom", new Type[] { typeof(int), typeof(int) });
            Assert.IsNotNull(method, "IInterstate must expose CanPlaceStreetFrom(int x, int y)");
        }

        [Test]
        public void IInterstate_facade_exposes_CheckInterstateConnectivity_method()
        {
            Type ifaceType = typeof(Domains.Roads.IInterstate);
            MethodInfo method = ifaceType.GetMethod("CheckInterstateConnectivity");
            Assert.IsNotNull(method, "IInterstate must expose CheckInterstateConnectivity()");
        }

        [Test]
        public void IInterstate_facade_exposes_RebuildFromGrid_method()
        {
            Type ifaceType = typeof(Domains.Roads.IInterstate);
            MethodInfo method = ifaceType.GetMethod("RebuildFromGrid");
            Assert.IsNotNull(method, "IInterstate must expose RebuildFromGrid()");
        }

        [Test]
        public void InterstateService_BorderSouth_is_zero()
        {
            Assert.AreEqual(0, Domains.Roads.Services.InterstateService.BorderSouth,
                "BorderSouth must be 0 (south edge = y==0)");
        }

        [Test]
        public void InterstateService_BorderNorth_is_one()
        {
            Assert.AreEqual(1, Domains.Roads.Services.InterstateService.BorderNorth,
                "BorderNorth must be 1 (north edge = y==h-1)");
        }

        [Test]
        public void InterstateService_GetBorderIndex_south_cell()
        {
            var svc = new Domains.Roads.Services.InterstateService();
            int idx = svc.GetBorderIndex(new Vector2Int(3, 0), 10, 10);
            Assert.AreEqual(0, idx, "Cell at y==0 must return BorderSouth (0)");
        }

        [Test]
        public void InterstateService_GetBorderIndex_interior_cell_returns_minus_one()
        {
            var svc = new Domains.Roads.Services.InterstateService();
            int idx = svc.GetBorderIndex(new Vector2Int(3, 3), 10, 10);
            Assert.AreEqual(-1, idx, "Interior cell must return -1 (not on border)");
        }

        [Test]
        public void InterstateService_IsOnMapBorder_corner_cell()
        {
            var svc = new Domains.Roads.Services.InterstateService();
            Assert.IsTrue(svc.IsOnMapBorder(new Vector2Int(0, 0), 10, 10), "Corner (0,0) must be on border");
        }

        [Test]
        public void InterstateService_IsOnMapBorder_interior_cell_false()
        {
            var svc = new Domains.Roads.Services.InterstateService();
            Assert.IsFalse(svc.IsOnMapBorder(new Vector2Int(5, 5), 10, 10), "Interior cell must not be on border");
        }

        [Test]
        public void InterstateService_IsCardinalStep_horizontal_true()
        {
            var svc = new Domains.Roads.Services.InterstateService();
            Assert.IsTrue(svc.IsCardinalStep(new Vector2Int(0, 0), new Vector2Int(1, 0)), "Horizontal step must be cardinal");
        }

        [Test]
        public void InterstateService_IsCardinalStep_diagonal_false()
        {
            var svc = new Domains.Roads.Services.InterstateService();
            Assert.IsFalse(svc.IsCardinalStep(new Vector2Int(0, 0), new Vector2Int(1, 1)), "Diagonal step must not be cardinal");
        }

        [Test]
        public void InterstateService_IsCardinalPath_two_cell_horizontal()
        {
            var svc = new Domains.Roads.Services.InterstateService();
            var path = new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0) };
            Assert.IsTrue(svc.IsCardinalPath(path), "Horizontal path must be cardinal");
        }

        [Test]
        public void InterstateService_IsCardinalPath_null_returns_false()
        {
            var svc = new Domains.Roads.Services.InterstateService();
            Assert.IsFalse(svc.IsCardinalPath(null), "Null path must return false");
        }

        [Test]
        public void InterstateService_ManhattanDistance_correct()
        {
            var svc = new Domains.Roads.Services.InterstateService();
            int dist = svc.ManhattanDistance(new Vector2Int(0, 0), new Vector2Int(3, 4));
            Assert.AreEqual(7, dist, "Manhattan distance (0,0)→(3,4) must be 7");
        }

        [Test]
        public void InterstateService_GetFirstStepDirectionFromBorder_south_returns_north()
        {
            var svc = new Domains.Roads.Services.InterstateService();
            Vector2Int? dir = svc.GetFirstStepDirectionFromBorder(new Vector2Int(3, 0), 10, 10);
            Assert.IsNotNull(dir, "South border cell must have a first step direction");
            Assert.AreEqual(new Vector2Int(0, 1), dir.Value, "South border → first step must be north (0,1)");
        }

        [Test]
        public void InterstateService_GetFirstStepDirectionFromBorder_interior_returns_null()
        {
            var svc = new Domains.Roads.Services.InterstateService();
            Vector2Int? dir = svc.GetFirstStepDirectionFromBorder(new Vector2Int(5, 5), 10, 10);
            Assert.IsNull(dir, "Interior cell must return null first step direction");
        }

        [Test]
        public void InterstateManager_implements_IInterstate_interface()
        {
            Type managerType = typeof(Territory.Roads.InterstateManager);
            Type ifaceType = typeof(Domains.Roads.IInterstate);
            Assert.IsTrue(ifaceType.IsAssignableFrom(managerType),
                "InterstateManager must implement IInterstate");
        }

        [Test]
        public void roads_asmdef_exists_for_interstate_service()
        {
            string path = System.IO.Path.Combine(Application.dataPath, "Scripts", "Domains", "Roads", "Roads.asmdef");
            Assert.IsTrue(File.Exists(path), $"Roads.asmdef not found at: {path}");
        }
    }
}
