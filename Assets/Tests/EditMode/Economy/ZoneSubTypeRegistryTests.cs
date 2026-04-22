using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Territory.Zones;
using Territory.Economy;

namespace Territory.Tests.EditMode.Economy
{
    /// <summary>
    /// EditMode regression tests for ZoneSubTypeRegistry, EconomyManager.IsStateServiceZone,
    /// and Zone.SubTypeId serialization. Locks Stage 1.1 scaffolding (TECH-278/279/280/281).
    /// </summary>
    public class ZoneSubTypeRegistryTests
    {
        private GameObject catalogHost;
        private ZoneSubTypeRegistry registry;
        private EconomyManager economy;

        [SetUp]
        public void SetUp()
        {
            catalogHost = new GameObject("TestGridAssetCatalog");
            catalogHost.AddComponent<GridAssetCatalog>();
            registry = new GameObject("TestRegistry").AddComponent<ZoneSubTypeRegistry>();
            // Awake fires immediately in EditMode — loads Resources/Economy/zone-sub-types.json.
            Assume.That(registry.Entries.Count, Is.EqualTo(7),
                "zone-sub-types.json must have exactly 7 entries (TECH-281 seed table drift guard)");

            economy = new GameObject("TestEconomy").AddComponent<EconomyManager>();
            // EconomyManager has no Awake; Start has FindObjectOfType but does not fire in EditMode.
        }

        [TearDown]
        public void TearDown()
        {
            if (registry != null) UnityEngine.Object.DestroyImmediate(registry.gameObject);
            if (catalogHost != null) UnityEngine.Object.DestroyImmediate(catalogHost);
            if (economy != null) UnityEngine.Object.DestroyImmediate(economy.gameObject);
        }

        /// <summary>GetById(0..6) returns non-null entry with matching id.</summary>
        [Test]
        public void GetById_ValidIds_ReturnsEntry()
        {
            for (int id = 0; id <= 6; id++)
            {
                ZoneSubTypeRegistry.ZoneSubTypeEntry entry = registry.GetById(id);
                Assert.IsNotNull(entry, $"GetById({id}) returned null — expected seeded entry");
                Assert.AreEqual(id, entry.id, $"entry.id mismatch for id={id}");
            }
        }

        /// <summary>GetById(-1) returns null (Zone.subTypeId sentinel value).</summary>
        [Test]
        public void GetById_MinusOne_ReturnsNull()
        {
            ZoneSubTypeRegistry.ZoneSubTypeEntry entry = registry.GetById(-1);
            Assert.IsNull(entry, "GetById(-1) must return null (sentinel for 'no sub-type')");
        }

        /// <summary>IsStateServiceZone returns true for all 6 Zone S enum values.</summary>
        [Test]
        public void IsStateServiceZone_NewEnumValues_TrueForAllSix()
        {
            Zone.ZoneType[] stateServiceTypes = {
                Zone.ZoneType.StateServiceLightBuilding,
                Zone.ZoneType.StateServiceMediumBuilding,
                Zone.ZoneType.StateServiceHeavyBuilding,
                Zone.ZoneType.StateServiceLightZoning,
                Zone.ZoneType.StateServiceMediumZoning,
                Zone.ZoneType.StateServiceHeavyZoning,
            };

            foreach (var zoneType in stateServiceTypes)
            {
                Assert.IsTrue(economy.IsStateServiceZone(zoneType),
                    $"IsStateServiceZone should be true for {zoneType}");
            }
        }

        /// <summary>
        /// IsStateServiceZone returns false for all non-State-Service ZoneType values.
        /// Uses Enum.GetValues as a drift guard — any new non-S value added to the enum
        /// is covered automatically.
        /// </summary>
        [Test]
        public void IsStateServiceZone_NonStateServiceValues_False()
        {
            var stateServiceSet = new HashSet<Zone.ZoneType>
            {
                Zone.ZoneType.StateServiceLightBuilding,
                Zone.ZoneType.StateServiceMediumBuilding,
                Zone.ZoneType.StateServiceHeavyBuilding,
                Zone.ZoneType.StateServiceLightZoning,
                Zone.ZoneType.StateServiceMediumZoning,
                Zone.ZoneType.StateServiceHeavyZoning,
            };

            foreach (Zone.ZoneType zoneType in Enum.GetValues(typeof(Zone.ZoneType)))
            {
                if (stateServiceSet.Contains(zoneType)) continue;
                Assert.IsFalse(economy.IsStateServiceZone(zoneType),
                    $"IsStateServiceZone should be false for {zoneType}");
            }
        }

        /// <summary>Zone.SubTypeId defaults to -1 on a freshly added component.</summary>
        [Test]
        public void Zone_SubTypeId_DefaultsToMinusOne()
        {
            GameObject go = new GameObject("TestZone");
            Zone zone = go.AddComponent<Zone>();
            try
            {
                Assert.AreEqual(-1, zone.SubTypeId,
                    "Zone.SubTypeId must default to -1 (no sub-type sentinel)");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        /// <summary>Zone.subTypeId survives a JsonUtility serialize → deserialize round-trip.</summary>
        [Test]
        public void Zone_SubTypeId_SerializationRoundTrip()
        {
            GameObject go = new GameObject("TestZoneSerial");
            Zone zone = go.AddComponent<Zone>();
            try
            {
                zone.SubTypeId = 3;
                string json = JsonUtility.ToJson(zone);
                // Reset to default sentinel, then overwrite from json.
                zone.SubTypeId = -1;
                JsonUtility.FromJsonOverwrite(json, zone);
                Assert.AreEqual(3, zone.SubTypeId,
                    "Zone.SubTypeId must survive JsonUtility round-trip (save-format drift guard)");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }
    }
}
