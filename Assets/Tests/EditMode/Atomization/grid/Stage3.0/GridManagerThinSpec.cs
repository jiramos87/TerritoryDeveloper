using NUnit.Framework;
using System;
using System.IO;
using System.Reflection;
using System.Linq;
using UnityEngine;
using Territory.Core;

namespace Territory.Tests.EditMode.Atomization.Stage3
{
    /// <summary>
    /// Stage 3.0 — GridManager hub thinning spec.
    /// §Red-Stage Proof anchor: GridManagerThinSpec.cs::grid_manager_is_thin
    /// Red: GridManager &gt; 200 LOC / publics not single-line delegates.
    /// Green: GridManager ≤ 200 LOC; GridQueryService exists in Domains.Grid.Services; IGrid resolves.
    /// </summary>
    public class GridManagerThinSpec
    {
        private const string GridManagerPath =
            "Assets/Scripts/Managers/GameManagers/GridManager.cs";

        [Test]
        public void grid_manager_is_thin()
        {
            string fullPath = Path.Combine(
                Path.GetDirectoryName(Application.dataPath),
                GridManagerPath);

            Assert.IsTrue(File.Exists(fullPath),
                $"GridManager.cs not found at {fullPath}");

            int lineCount = File.ReadAllLines(fullPath).Length;
            Assert.LessOrEqual(lineCount, 200,
                $"GridManager.cs must be ≤200 LOC after hub thinning; currently {lineCount} LOC");
        }

        [Test]
        public void GridQueryService_exists_in_domains_grid_services()
        {
            Type t = typeof(Domains.Grid.Services.GridQueryService);
            Assert.AreEqual("Domains.Grid.Services", t.Namespace,
                "GridQueryService must live in Domains.Grid.Services");
        }

        [Test]
        public void GridQueryService_exposes_IsValidGridPosition()
        {
            Type t = typeof(Domains.Grid.Services.GridQueryService);
            MethodInfo m = t.GetMethod("IsValidGridPosition",
                new[] { typeof(UnityEngine.Vector2) });
            Assert.IsNotNull(m, "GridQueryService must expose IsValidGridPosition(Vector2)");
        }

        [Test]
        public void GridQueryService_exposes_IsInBounds()
        {
            Type t = typeof(Domains.Grid.Services.GridQueryService);
            MethodInfo m = t.GetMethod("IsInBounds",
                new[] { typeof(int), typeof(int) });
            Assert.IsNotNull(m, "GridQueryService must expose IsInBounds(int x, int y)");
        }

        [Test]
        public void IGrid_resolves_via_type_reference()
        {
            Type ifaceType = typeof(Domains.Grid.IGrid);
            Assert.IsNotNull(ifaceType, "IGrid interface must be resolvable");
        }
    }
}
