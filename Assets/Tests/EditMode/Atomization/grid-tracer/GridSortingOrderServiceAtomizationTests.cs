using NUnit.Framework;
using System;
using System.Reflection;
using UnityEngine;

namespace Territory.Tests.EditMode.Atomization.GridTracer
{
    /// <summary>
    /// Tracer test: asserts GridSortingOrderService extracted to Domains.Grid.Services assembly.
    /// Red baseline: file still at Territory.Core + no IGrid.cs + Grid.asmdef empty → asserts fail.
    /// Green: assembly name = Grid; IGrid facade has GetRoadSortingOrderForCell method; namespace correct.
    /// </summary>
    public class GridSortingOrderServiceAtomizationTests
    {
        [Test]
        public void GridSortingOrderService_is_in_domains_grid_services_namespace()
        {
            Type serviceType = typeof(Domains.Grid.Services.GridSortingOrderService);
            Assert.AreEqual("Domains.Grid.Services", serviceType.Namespace,
                $"Expected namespace 'Domains.Grid.Services', got '{serviceType.Namespace}'");
        }

        [Test]
        public void IGrid_facade_exposes_GetRoadSortingOrderForCell_method()
        {
            Type ifaceType = typeof(Domains.Grid.IGrid);
            MethodInfo method = ifaceType.GetMethod("GetRoadSortingOrderForCell",
                new Type[] { typeof(int), typeof(int), typeof(int) });
            Assert.IsNotNull(method,
                "IGrid must expose GetRoadSortingOrderForCell(int x, int y, int height)");
        }

        [Test]
        public void IGrid_facade_exposes_SetRoadSortingOrder_method()
        {
            Type ifaceType = typeof(Domains.Grid.IGrid);
            MethodInfo method = ifaceType.GetMethod("SetRoadSortingOrder",
                new Type[] { typeof(GameObject), typeof(int), typeof(int) });
            Assert.IsNotNull(method,
                "IGrid must expose SetRoadSortingOrder(GameObject tile, int x, int y)");
        }

        [Test]
        public void GridSortingOrderService_ROAD_SORTING_OFFSET_preserved()
        {
            // Behavior parity check: constant value must remain 106 after move
            var field = typeof(Domains.Grid.Services.GridSortingOrderService)
                .GetField("ROAD_SORTING_OFFSET", BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(field, "ROAD_SORTING_OFFSET field must exist on GridSortingOrderService");
            int value = (int)field.GetValue(null);
            Assert.AreEqual(106, value,
                $"ROAD_SORTING_OFFSET behavior parity: expected 106, got {value}");
        }
    }
}
