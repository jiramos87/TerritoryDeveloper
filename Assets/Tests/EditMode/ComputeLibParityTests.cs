using System.IO;
using NUnit.Framework;
using UnityEngine;
using Territory.Simulation;
using Territory.Utilities.Compute;

namespace Territory.Tests.EditMode
{
    /// <summary>
    /// Edit Mode parity tests for pure compute helpers. Golden JSON + ring metrics.
    /// </summary>
    public class ComputeLibParityTests
    {
        private static string FixturePath =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "tools", "compute-lib", "test", "fixtures", "world-to-grid.json"));

        [System.Serializable]
        private class WorldToGridFixtureDto
        {
            public string description;
            public int tolerance;
            public WorldToGridCaseDto[] cases;
        }

        [System.Serializable]
        private class WorldToGridCaseDto
        {
            public float world_x;
            public float world_y;
            public float tile_width;
            public float tile_height;
            public float origin_x;
            public float origin_y;
            public int cell_x;
            public int cell_y;
        }

        [Test]
        public void WorldToGridPlanar_MatchesComputeLibGoldenFixture()
        {
            Assert.That(File.Exists(FixturePath), Is.True, "Missing golden fixture at " + FixturePath);

            string json = File.ReadAllText(FixturePath);
            var dto = JsonUtility.FromJson<WorldToGridFixtureDto>(json);
            Assert.That(dto?.cases, Is.Not.Null);
            Assert.That(dto.cases.Length, Is.GreaterThan(0));

            foreach (var c in dto.cases)
            {
                var world = new Vector2(c.world_x, c.world_y);
                Vector2Int cell = IsometricGridMath.WorldToGridPlanar(
                    world,
                    c.tile_width,
                    c.tile_height,
                    new Vector2(c.origin_x, c.origin_y));

                Assert.AreEqual(c.cell_x, cell.x, $"cell_x case world=({c.world_x},{c.world_y})");
                Assert.AreEqual(c.cell_y, cell.y, $"cell_y case world=({c.world_x},{c.world_y})");

                Vector2 back = IsometricGridMath.GridToWorldPlanar(
                    cell.x,
                    cell.y,
                    c.tile_width,
                    c.tile_height,
                    1,
                    new Vector2(c.origin_x, c.origin_y));

                Vector2Int roundTrip = IsometricGridMath.WorldToGridPlanar(
                    back,
                    c.tile_width,
                    c.tile_height,
                    new Vector2(c.origin_x, c.origin_y));
                Assert.AreEqual(cell.x, roundTrip.x, "round-trip grid X");
                Assert.AreEqual(cell.y, roundTrip.y, "round-trip grid Y");
            }
        }

        [Test]
        public void UrbanGrowthRingMath_MatchesPriorUrbanMetricsThresholds()
        {
            float r = UrbanGrowthRingMath.ComputeUrbanRadiusFromCellCount(500);
            Assert.Greater(r, 0f);

            Assert.AreEqual(UrbanRing.Inner, UrbanGrowthRingMath.ClassifyRing(0f, 0f, 0f, 0f, r));
            Assert.AreEqual(UrbanRing.Mid, UrbanGrowthRingMath.ClassifyRingFromDistance(r * 0.85f, r));
            Assert.AreEqual(UrbanRing.Outer, UrbanGrowthRingMath.ClassifyRingFromDistance(r * 1.4f, r));
            Assert.AreEqual(UrbanRing.Rural, UrbanGrowthRingMath.ClassifyRingFromDistance(r * 2f, r));

            var poles = new[] { new Vector2(0f, 0f), new Vector2(20f, 0f) };
            Assert.AreEqual(UrbanRing.Inner, UrbanGrowthRingMath.ClassifyRingMultipolar(0f, 0f, poles, r));
            Assert.AreEqual(UrbanRing.Inner, UrbanGrowthRingMath.ClassifyRingMultipolar(20f, 0f, poles, r));
        }

        [Test]
        public void GridDistanceMath_ChebyshevAndManhattan()
        {
            Assert.AreEqual(1, GridDistanceMath.Chebyshev(0, 0, 1, 1));
            Assert.AreEqual(2, GridDistanceMath.Manhattan(0, 0, 1, 1));
            Assert.AreEqual(5, GridDistanceMath.Chebyshev(0, 0, -5, 2));
        }
    }
}
