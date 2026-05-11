using NUnit.Framework;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace Territory.Tests.EditMode.Atomization.Stage4_2
{
    /// <summary>
    /// §Red-Stage Proof anchor: ZoneManagerThinSpec.cs::zone_manager_is_thin_with_shared_interface
    /// Stage 4.2: ZoneManager Tier-B THIN — hub collapses to ≤200 LOC; shared-interface answer applied.
    /// Green: ZoneManager.cs ≤200 LOC AND ZonesService.cs exists AND hub delegates via registry-resolved IGrid/IWater/IRoads.
    /// </summary>
    public class ZoneManagerThinSpec
    {
        private const string ZoneManagerPath =
            "Assets/Scripts/Managers/GameManagers/ZoneManager.cs";

        private const string ZonesServicePath =
            "Assets/Scripts/Domains/Zones/Services/ZonesService.cs";

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

            // Assert 2: hub [SerializeField] fields unchanged — gridManager, waterManager, roadManager present
            string hubSource = File.ReadAllText(hubPath);
            Assert.IsTrue(hubSource.Contains("public GridManager gridManager"),
                "ZoneManager hub must retain public GridManager gridManager field (locked #3).");
            Assert.IsTrue(hubSource.Contains("public WaterManager waterManager"),
                "ZoneManager hub must retain public WaterManager waterManager field (locked #3).");
            Assert.IsTrue(hubSource.Contains("public RoadManager roadManager"),
                "ZoneManager hub must retain public RoadManager roadManager field (locked #3).");

            // Assert 3: ZonesService.cs exists under Domains/Zones/Services/
            string svcPath = Path.Combine(repoRoot, ZonesServicePath);
            Assert.IsTrue(File.Exists(svcPath),
                $"ZonesService.cs must exist at {svcPath} (not clobbering ZoneSService.cs at Managers/GameManagers/).");

            // Assert 4: ZonesService references registry-resolved interfaces (IGrid, IWater, IRoads)
            string svcSource = File.ReadAllText(svcPath);
            Assert.IsTrue(svcSource.Contains("IGrid"), "ZonesService must reference IGrid.");
            Assert.IsTrue(svcSource.Contains("IWater"), "ZonesService must reference IWater.");
            Assert.IsTrue(svcSource.Contains("IRoads"), "ZonesService must reference IRoads.");
            Assert.IsTrue(svcSource.Contains("WireDependencies"),
                "ZonesService must expose WireDependencies(IGrid, IWater, IRoads).");

            // Assert 5: hub calls WireDependencies in Start
            Assert.IsTrue(hubSource.Contains("WireDependencies"),
                "ZoneManager hub must call _zones.WireDependencies(...) in Start.");
            Assert.IsTrue(hubSource.Contains("Resolve<Domains.Grid.IGrid>"),
                "ZoneManager hub must resolve IGrid from registry.");
            Assert.IsTrue(hubSource.Contains("Resolve<Domains.Water.IWater>"),
                "ZoneManager hub must resolve IWater from registry.");
            Assert.IsTrue(hubSource.Contains("Resolve<Domains.Roads.IRoads>"),
                "ZoneManager hub must resolve IRoads from registry.");
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
        public void zones_service_exposes_wire_dependencies()
        {
            Type t = Type.GetType("Domains.Zones.Services.ZonesService, Assembly-CSharp");
            Assert.IsNotNull(t, "ZonesService must exist");
            MethodInfo m = t.GetMethod("WireDependencies");
            Assert.IsNotNull(m, "ZonesService must expose WireDependencies()");
        }

        [Test]
        public void zones_service_exposes_position_tracking()
        {
            Type t = Type.GetType("Domains.Zones.Services.ZonesService, Assembly-CSharp");
            Assert.IsNotNull(t, "ZonesService must exist");
            Assert.IsNotNull(t.GetMethod("AddPosition"), "ZonesService must expose AddPosition()");
            Assert.IsNotNull(t.GetMethod("RemovePosition"), "ZonesService must expose RemovePosition()");
            Assert.IsNotNull(t.GetMethod("ClearAll"), "ZonesService must expose ClearAll()");
            Assert.IsNotNull(t.GetMethod("GetZonedPositions"), "ZonesService must expose GetZonedPositions()");
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
