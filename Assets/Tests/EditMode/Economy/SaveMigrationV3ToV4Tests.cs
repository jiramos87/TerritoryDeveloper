using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Territory.Economy;
using Territory.Persistence;

namespace Territory.Tests.EditMode.Economy
{
    /// <summary>
    /// EditMode tests for GameSaveManager schema v3 → v4 migration.
    /// Verifies that legacy saves (no budgetAllocation, no stateServiceZones) are
    /// correctly seeded with a valid BudgetAllocationData default and an empty
    /// StateServiceZone list while pre-existing v3 fields remain intact.
    /// Uses inline payload construction — no on-disk JSON fixture file.
    /// </summary>
    public class SaveMigrationV3ToV4Tests
    {
        /// <summary>
        /// Legacy v3 save payload seeds valid budgetAllocation + empty stateServiceZones;
        /// schemaVersion bumped to CurrentSchemaVersion (4); pre-existing fields preserved.
        /// </summary>
        [Test]
        public void MigrateV3ToV4_Seeds_BudgetAllocationAndEmptyStateServiceZones()
        {
            // Build a v3-shaped payload: no budgetAllocation, no stateServiceZones.
            // regionId + countryId populated (schema < 1 branch would overwrite them).
            var data = new GameSaveData
            {
                schemaVersion = 3,
                regionId = "test-region-guid",
                countryId = "test-country-guid",
            };
            // neighborStubs + neighborCityBindings initialized to new List<> by field
            // declarations in GameSaveData — already non-null; schema 2/3 paths are no-ops.

            // Null-out schema-4 fields to simulate a true v3 on-disk payload.
            data.budgetAllocation = null;
            data.stateServiceZones = null;

            // Preserve a sample pre-existing v3 field.
            data.cityStats = new CityStatsData { money = 42_000 };

            // Invoke private static MigrateLoadedSaveData via reflection.
            MethodInfo migrateMethod = typeof(GameSaveManager)
                .GetMethod("MigrateLoadedSaveData", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(migrateMethod,
                "Reflection: MigrateLoadedSaveData not found on GameSaveManager (method renamed?)");
            migrateMethod.Invoke(null, new object[] { data });

            // Schema version bumped.
            Assert.AreEqual(GameSaveData.CurrentSchemaVersion, data.schemaVersion,
                "schemaVersion must equal CurrentSchemaVersion (4) after migration");

            // budgetAllocation seeded and valid.
            Assert.IsNotNull(data.budgetAllocation,
                "budgetAllocation must be non-null after v3→v4 migration");
            float sum = 0f;
            foreach (float p in data.budgetAllocation.envelopePct)
                sum += p;
            Assert.AreEqual(1f, sum, 1e-6f,
                "budgetAllocation.envelopePct must sum to 1.0 (within 1e-6) after default seeding");

            // stateServiceZones seeded as empty list.
            Assert.IsNotNull(data.stateServiceZones,
                "stateServiceZones must be non-null after v3→v4 migration");
            Assert.AreEqual(0, data.stateServiceZones.Count,
                "stateServiceZones must be an empty list on migration from v3 (no placements yet)");

            // Pre-existing v3 field preserved.
            Assert.AreEqual(42_000, data.cityStats.money,
                "Pre-existing v3 cityStats.money must be intact after migration");
        }
    }
}
