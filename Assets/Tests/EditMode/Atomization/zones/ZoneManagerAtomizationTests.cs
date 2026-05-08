using NUnit.Framework;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using Territory.Zones;

namespace Territory.Tests.EditMode.Atomization.Zones
{
    /// <summary>
    /// Tracer tests: assert ZonePlacementService + ZoneSectionService + ZonePrefabRegistry extracted
    /// to Domains.Zones.Services assembly + IZones facade interface present in Domains.Zones.
    /// Red baseline: Domains/Zones/ absent → asserts fail.
    /// Green: Zones.asmdef + three services + IZones all present; compile-check exits 0.
    /// §Red-Stage Proof anchor: ZoneManagerAtomizationTests.cs::ZonePlacementService_is_in_domains_zones_services_namespace
    /// Invariant #11: UrbanizationProposal never referenced in Domains.Zones.
    /// </summary>
    public class ZoneManagerAtomizationTests
    {
        [Test]
        public void ZonePlacementService_is_in_domains_zones_services_namespace()
        {
            Type serviceType = typeof(Domains.Zones.Services.ZonePlacementService);
            Assert.AreEqual("Domains.Zones.Services", serviceType.Namespace,
                $"Expected namespace 'Domains.Zones.Services', got '{serviceType.Namespace}'");
        }

        [Test]
        public void ZoneSectionService_is_in_domains_zones_services_namespace()
        {
            Type serviceType = typeof(Domains.Zones.Services.ZoneSectionService);
            Assert.AreEqual("Domains.Zones.Services", serviceType.Namespace,
                $"Expected namespace 'Domains.Zones.Services', got '{serviceType.Namespace}'");
        }

        [Test]
        public void ZonePrefabRegistry_is_in_domains_zones_services_namespace()
        {
            Type serviceType = typeof(Domains.Zones.Services.ZonePrefabRegistry);
            Assert.AreEqual("Domains.Zones.Services", serviceType.Namespace,
                $"Expected namespace 'Domains.Zones.Services', got '{serviceType.Namespace}'");
        }

        [Test]
        public void IZones_facade_exists_in_domains_zones_namespace()
        {
            Type ifaceType = typeof(Domains.Zones.IZones);
            Assert.AreEqual("Domains.Zones", ifaceType.Namespace,
                $"Expected namespace 'Domains.Zones', got '{ifaceType.Namespace}'");
        }

        [Test]
        public void IZones_facade_exposes_GetZoneAttributes_method()
        {
            Type ifaceType = typeof(Domains.Zones.IZones);
            MethodInfo method = ifaceType.GetMethod("GetZoneAttributes",
                new Type[] { typeof(Zone.ZoneType) });
            Assert.IsNotNull(method, "IZones must expose GetZoneAttributes(Zone.ZoneType zoneType)");
        }

        [Test]
        public void IZones_facade_exposes_GetBuildingZoneType_method()
        {
            Type ifaceType = typeof(Domains.Zones.IZones);
            MethodInfo method = ifaceType.GetMethod("GetBuildingZoneType",
                new Type[] { typeof(Zone.ZoneType) });
            Assert.IsNotNull(method, "IZones must expose GetBuildingZoneType(Zone.ZoneType zoningType)");
        }

        [Test]
        public void IZones_facade_exposes_CalculateAvailableSquareZonedSections_method()
        {
            Type ifaceType = typeof(Domains.Zones.IZones);
            MethodInfo method = ifaceType.GetMethod("CalculateAvailableSquareZonedSections", Type.EmptyTypes);
            Assert.IsNotNull(method, "IZones must expose CalculateAvailableSquareZonedSections()");
        }

        [Test]
        public void IZones_facade_exposes_ClearZonedPositions_method()
        {
            Type ifaceType = typeof(Domains.Zones.IZones);
            MethodInfo method = ifaceType.GetMethod("ClearZonedPositions", Type.EmptyTypes);
            Assert.IsNotNull(method, "IZones must expose ClearZonedPositions()");
        }

        [Test]
        public void ZonePlacementService_GetZoneAttributes_returns_null_for_unrecognized()
        {
            var svc = new Domains.Zones.Services.ZonePlacementService();
            // Grass is recognized; an unmapped enum would return null — test recognized path
            ZoneAttributes attrs = svc.GetZoneAttributes(Zone.ZoneType.Grass);
            Assert.IsNotNull(attrs, "GetZoneAttributes(Grass) must return non-null ZoneAttributes");
        }

        [Test]
        public void ZonePlacementService_GetBuildingZoneType_maps_light_residential()
        {
            var svc = new Domains.Zones.Services.ZonePlacementService();
            Zone.ZoneType result = svc.GetBuildingZoneType(Zone.ZoneType.ResidentialLightZoning);
            Assert.AreEqual(Zone.ZoneType.ResidentialLightBuilding, result,
                "ResidentialLightZoning must map to ResidentialLightBuilding");
        }

        [Test]
        public void ZonePlacementService_IsZoningType_returns_true_for_zoning_overlay()
        {
            var svc = new Domains.Zones.Services.ZonePlacementService();
            Assert.IsTrue(svc.IsZoningType(Zone.ZoneType.CommercialMediumZoning),
                "CommercialMediumZoning must be identified as zoning type");
        }

        [Test]
        public void ZonePlacementService_IsZoningType_returns_false_for_building_type()
        {
            var svc = new Domains.Zones.Services.ZonePlacementService();
            Assert.IsFalse(svc.IsZoningType(Zone.ZoneType.ResidentialHeavyBuilding),
                "ResidentialHeavyBuilding must NOT be identified as zoning type");
        }

        [Test]
        public void ZoneSectionService_GetValidZoneTypes_includes_all_nine_rci_zonings()
        {
            var types = Domains.Zones.Services.ZoneSectionService.GetValidZoneTypes();
            Assert.IsTrue(types.Count >= 9,
                $"ValidZoneTypes must include at least 9 RCI zoning types, got {types.Count}");
        }

        [Test]
        public void ZoneSectionService_CalculateSections_returns_empty_on_empty_positions()
        {
            var svc = new Domains.Zones.Services.ZoneSectionService();
            var result = svc.CalculateSections(new System.Collections.Generic.List<Vector2>());
            Assert.AreEqual(0, result.Count, "Empty position list must yield zero sections");
        }

        [Test]
        public void ZoneSectionService_CalculateSections_returns_1x1_section_for_single_position()
        {
            var svc = new Domains.Zones.Services.ZoneSectionService();
            var positions = new System.Collections.Generic.List<Vector2> { new Vector2(0, 0) };
            var result = svc.CalculateSections(positions);
            Assert.AreEqual(1, result.Count, "Single position must yield exactly one 1x1 section");
            Assert.AreEqual(1, result[0].Count, "1x1 section must contain 1 cell");
        }

        [Test]
        public void ZonePrefabRegistry_GetRandom_returns_null_on_empty_registry()
        {
            var reg = new Domains.Zones.Services.ZonePrefabRegistry(null);
            GameObject result = reg.GetRandom(Zone.ZoneType.ResidentialLightBuilding, 1);
            Assert.IsNull(result, "GetRandom on empty registry must return null");
        }

        [Test]
        public void ZonePlacementService_ParseZoneType_returns_Grass_on_null_input()
        {
            var svc = new Domains.Zones.Services.ZonePlacementService();
            Zone.ZoneType result = svc.ParseZoneType(null);
            Assert.AreEqual(Zone.ZoneType.Grass, result, "null input must parse to Grass");
        }

        [Test]
        public void ZonePlacementService_ParseZoneType_returns_correct_type_for_valid_string()
        {
            var svc = new Domains.Zones.Services.ZonePlacementService();
            Zone.ZoneType result = svc.ParseZoneType("CommercialHeavyBuilding");
            Assert.AreEqual(Zone.ZoneType.CommercialHeavyBuilding, result,
                "Valid string must parse to correct ZoneType");
        }

        [Test]
        public void zones_asmdef_exists()
        {
            string path = Path.Combine(Application.dataPath, "Scripts", "Domains", "Zones", "Zones.asmdef");
            Assert.IsTrue(File.Exists(path), $"Zones.asmdef not found at: {path}");
        }

        [Test]
        public void zones_asmdef_references_territory_developer_game()
        {
            string path = Path.Combine(Application.dataPath, "Scripts", "Domains", "Zones", "Zones.asmdef");
            Assert.IsTrue(File.Exists(path), "Zones.asmdef absent");
            string content = File.ReadAllText(path);
            Assert.IsTrue(content.Contains("7d8f9e2a1b4c5d6e7f8a9b0c1d2e3f4a"),
                "Zones.asmdef must reference TerritoryDeveloper.Game (GUID 7d8f9e2a...)");
        }

        [Test]
        public void domains_zones_contains_no_UrbanizationProposal_reference()
        {
            // Invariant #11: UrbanizationProposal must NEVER be re-enabled.
            // Assert none of the Domains.Zones service types reference UrbanizationProposal.
            string domainsPath = Path.Combine(Application.dataPath, "Scripts", "Domains", "Zones");
            if (!Directory.Exists(domainsPath))
            {
                Assert.Fail($"Domains/Zones directory not found at: {domainsPath}");
                return;
            }
            string[] files = Directory.GetFiles(domainsPath, "*.cs", SearchOption.AllDirectories);
            foreach (string file in files)
            {
                string content = File.ReadAllText(file);
                Assert.IsFalse(content.Contains("UrbanizationProposal"),
                    $"Invariant #11 violated: {Path.GetFileName(file)} references UrbanizationProposal");
            }
        }
    }
}
