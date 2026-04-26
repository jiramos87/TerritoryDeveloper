using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Territory.Persistence;
using Territory.Simulation.Signals;

namespace Territory.Tests.EditMode.Simulation.Signals
{
    /// <summary>EditMode coverage for <see cref="DistrictMap"/> save/load round-trip + schema-4→5 migration via <see cref="GameSaveData"/>.</summary>
    [TestFixture]
    public class DistrictMapSaveRoundTripTests
    {
        [Test]
        public void DistrictMap_RoundTrip_4x4_AllRingIds()
        {
            DistrictMap source = new DistrictMap(4, 4);
            // Spread ids 0..3 across the 4x4 grid (concentric quadrants).
            for (int x = 0; x < 4; x++)
            {
                for (int y = 0; y < 4; y++)
                {
                    int id = ((x < 2) ? 0 : 2) + ((y < 2) ? 0 : 1); // 0,1,2,3 quadrants.
                    source.SetDistrictId(x, y, id);
                }
            }

            DistrictMapData payload = source.GetSerializableData();
            string json = JsonUtility.ToJson(payload);
            DistrictMapData restored = JsonUtility.FromJson<DistrictMapData>(json);

            DistrictMap target = new DistrictMap(4, 4);
            target.RestoreFromSerializableData(restored);

            for (int x = 0; x < 4; x++)
            {
                for (int y = 0; y < 4; y++)
                {
                    Assert.AreEqual(
                        source.GetDistrictId(x, y),
                        target.GetDistrictId(x, y),
                        $"District id parity mismatch at ({x},{y})");
                }
            }
        }

        [Test]
        public void DistrictMap_LegacySchema4_MigrationLeavesDistrictMapNull()
        {
            GameSaveData data = NewMinimalSchema4Payload();
            data.districtMap = null;

            InvokeMigrate(data);

            Assert.AreEqual(GameSaveData.CurrentSchemaVersion, data.schemaVersion, "schemaVersion not bumped to current");
            Assert.IsNull(data.districtMap, "schema-4 → 5 migration must leave null districtMap untouched (LoadGame falls back to dm.Rebuild())");
        }

        [Test]
        public void DistrictMap_ForwardCompat_NonNullPreserved()
        {
            GameSaveData data = NewMinimalSchema4Payload();
            DistrictMapData payload = new DistrictMapData
            {
                width = 2,
                height = 2,
                districtIdsFlat = new[] { 0, 1, 2, 3 },
            };
            data.districtMap = payload;

            InvokeMigrate(data);

            Assert.IsNotNull(data.districtMap, "Non-null districtMap dropped during migration");
            Assert.AreEqual(2, data.districtMap.width);
            Assert.AreEqual(2, data.districtMap.height);
            Assert.AreEqual(new[] { 0, 1, 2, 3 }, data.districtMap.districtIdsFlat);
        }

        private static GameSaveData NewMinimalSchema4Payload()
        {
            return new GameSaveData
            {
                schemaVersion = 4,
                regionId = System.Guid.NewGuid().ToString(),
                countryId = System.Guid.NewGuid().ToString(),
            };
        }

        private static void InvokeMigrate(GameSaveData data)
        {
            MethodInfo migrate = typeof(GameSaveManager).GetMethod(
                "MigrateLoadedSaveData",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(migrate, "GameSaveManager.MigrateLoadedSaveData missing");
            migrate.Invoke(null, new object[] { data });
        }
    }
}
