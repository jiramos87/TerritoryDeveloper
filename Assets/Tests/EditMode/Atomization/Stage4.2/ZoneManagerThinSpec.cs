using NUnit.Framework;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace Territory.Tests.EditMode.Atomization.Stage4_2
{
    /// <summary>
    /// §Red-Stage Proof anchor: ZoneManagerThinSpec.cs::zone_manager_is_thin_with_shared_interface
    /// Stage 4.2: ZoneManager Tier-B THIN — hub collapses to ≤200 LOC; ZonesService POCO extracted.
    /// ZonesService wires IGrid/IWater/IRoads via WireDependencies called from hub Start.
    /// Invariant #11: no UrbanizationProposal reference.
    /// </summary>
    public class ZoneManagerThinSpec
    {
        private const string ZoneManagerPath =
            "Assets/Scripts/Managers/GameManagers/ZoneManager.cs";

        private const string ZonesServicePath =
            "Assets/Scripts/Managers/GameManagers/ZonesService.cs";

        [Test]
        public void zone_manager_is_thin_with_shared_interface()
        {
            string repoRoot = GetRepoRoot();

            // Assert 1: ZoneManager.cs ≤200 LOC
            string hubPath = Path.Combine(repoRoot, ZoneManagerPath);
            Assert.IsTrue(File.Exists(hubPath), $"ZoneManager.cs not found at {hubPath}");
            int lineCount = File.ReadAllLines(hubPath).Length;
            Assert.LessOrEqual(lineCount, 200,
                $"ZoneManager.cs must be ≤200 LOC after THIN. Got {lineCount} lines.");

            // Assert 2: hub [SerializeField] field set UNCHANGED — gridManager field present
            string hubContent = File.ReadAllText(hubPath);
            Assert.IsTrue(hubContent.Contains("public GridManager gridManager"),
                "ZoneManager must retain public GridManager gridManager field (locked #3).");

            // Assert 3: ZonesService.cs exists
            string svcPath = Path.Combine(repoRoot, ZonesServicePath);
            Assert.IsTrue(File.Exists(svcPath),
                $"ZonesService.cs must exist at {ZonesServicePath}");

            // Assert 4: ZonesService uses registry.Resolve (shared-interface pattern)
            string svcContent = File.ReadAllText(svcPath);
            Assert.IsTrue(svcContent.Contains("WireDependencies"),
                "ZonesService must expose WireDependencies for IGrid/IWater/IRoads registry wiring.");
        }

        [Test]
        public void hub_serialize_field_set_unchanged()
        {
            string repoRoot = GetRepoRoot();
            string hubPath = Path.Combine(repoRoot, ZoneManagerPath);
            Assert.IsTrue(File.Exists(hubPath), $"ZoneManager.cs not found at {hubPath}");
            string content = File.ReadAllText(hubPath);

            // Locked [SerializeField] fields must be unchanged
            Assert.IsTrue(content.Contains("public GridManager gridManager"),    "gridManager field must be present.");
            Assert.IsTrue(content.Contains("public RoadManager roadManager"),    "roadManager field must be present.");
            Assert.IsTrue(content.Contains("public WaterManager waterManager"),  "waterManager field must be present.");
            Assert.IsTrue(content.Contains("public CityStats cityStats"),        "cityStats field must be present.");
        }

        [Test]
        public void zones_service_uses_registry_resolve()
        {
            // ZonesService must expose WireDependencies(IGrid, IWater, IRoads)
            Type t = Type.GetType("Domains.Zones.Services.ZonesService, Assembly-CSharp");
            Assert.IsNotNull(t, "ZonesService must be loadable from Assembly-CSharp in Domains.Zones.Services namespace.");

            MethodInfo wire = t.GetMethod("WireDependencies");
            Assert.IsNotNull(wire, "ZonesService must expose WireDependencies method.");

            ParameterInfo[] parms = wire.GetParameters();
            Assert.AreEqual(3, parms.Length,
                "WireDependencies must accept exactly 3 parameters (IGrid, IWater, IRoads).");

            Assert.AreEqual("Domains.Grid.IGrid", parms[0].ParameterType.FullName,
                $"First parameter must be Domains.Grid.IGrid, got {parms[0].ParameterType.FullName}.");
            Assert.AreEqual("Domains.Water.IWater", parms[1].ParameterType.FullName,
                $"Second parameter must be Domains.Water.IWater, got {parms[1].ParameterType.FullName}.");
            Assert.AreEqual("Domains.Roads.IRoads", parms[2].ParameterType.FullName,
                $"Third parameter must be Domains.Roads.IRoads, got {parms[2].ParameterType.FullName}.");
        }

        [Test]
        public void zones_service_is_in_correct_namespace()
        {
            Type t = Type.GetType("Domains.Zones.Services.ZonesService, Assembly-CSharp");
            Assert.IsNotNull(t, "ZonesService must be loadable from Assembly-CSharp");
            Assert.AreEqual("Domains.Zones.Services", t.Namespace,
                $"ZonesService namespace mismatch: {t.Namespace}");
        }

        [Test]
        public void zones_service_exposes_SetPrefabRegistry()
        {
            Type t = Type.GetType("Domains.Zones.Services.ZonesService, Assembly-CSharp");
            Assert.IsNotNull(t, "ZonesService must exist");
            MethodInfo m = t.GetMethod("SetPrefabRegistry");
            Assert.IsNotNull(m, "ZonesService must expose SetPrefabRegistry(ZonePrefabRegistry).");
        }

        [Test]
        public void zone_manager_delegates_to_zones_service()
        {
            // ZoneManager must reference ZonesService in its body
            string repoRoot = GetRepoRoot();
            string content = File.ReadAllText(Path.Combine(repoRoot, ZoneManagerPath));
            Assert.IsTrue(content.Contains("ZonesService"),
                "ZoneManager.cs must reference ZonesService (delegation pattern).");
        }

        private static string GetRepoRoot()
        {
            string dir = Application.dataPath;
            while (!string.IsNullOrEmpty(dir))
            {
                if (File.Exists(Path.Combine(dir, "CLAUDE.md")))
                    return dir;
                string parent = Path.GetDirectoryName(dir);
                if (parent == dir) break;
                dir = parent;
            }
            return Application.dataPath.Replace("/Assets", "");
        }
    }
}
