using NUnit.Framework;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace Territory.Tests.EditMode.Atomization.Stage5_4
{
    /// <summary>
    /// §Red-Stage Proof anchor: DemandManagerThinSpec.cs::demand_manager_is_thin_with_invariant_11
    /// Stage 5.4: DemandManager Tier-C NO-PORT — hub collapses to ≤200 LOC; RCI logic delegated to DemandService.
    /// Green: DemandManager.cs ≤200 LOC AND DemandService.cs exists AND hub delegates via _svc AND invariant #11 preserved.
    /// </summary>
    public class DemandManagerThinSpec
    {
        private const string DemandManagerPath =
            "Assets/Scripts/Managers/GameManagers/DemandManager.cs";

        private const string DemandServicePath =
            "Assets/Scripts/Domains/Demand/Services/DemandService.cs";

        [Test]
        public void demand_manager_is_thin_with_invariant_11()
        {
            string repoRoot = GetRepoRoot();

            // Assert 1: DemandManager.cs ≤200 LOC
            string hubPath = Path.Combine(repoRoot, DemandManagerPath);
            Assert.IsTrue(File.Exists(hubPath), $"DemandManager.cs not found at {hubPath}");
            int lineCount = File.ReadAllLines(hubPath).Length;
            Assert.LessOrEqual(lineCount, 200,
                $"DemandManager.cs must be ≤200 LOC after THIN. Got {lineCount} lines.");

            // Assert 2: DemandService.cs exists under Domains/Demand/Services/
            string svcPath = Path.Combine(repoRoot, DemandServicePath);
            Assert.IsTrue(File.Exists(svcPath),
                $"DemandService.cs must exist at {svcPath}.");

            // Assert 3 (urbanization_proposal_call_sites_flagged_not_deleted): invariant #11
            // DemandManager must NOT contain UrbanizationProposal runtime references (pre-scan flagged none).
            string hubSource = File.ReadAllText(hubPath);
            Assert.IsFalse(hubSource.Contains("UrbanizationProposalManager") && !hubSource.Contains("// [INVARIANT-11]"),
                "DemandManager must not add UrbanizationProposal references without invariant #11 flag comment.");

            // Assert 4: hub delegates to DemandService (_svc field present)
            Assert.IsTrue(hubSource.Contains("DemandService"),
                "DemandManager hub must reference DemandService.");
            Assert.IsTrue(hubSource.Contains("_svc"),
                "DemandManager hub must hold a _svc delegate field.");

            // Assert 5: hub uses WireDependencies pattern
            Assert.IsTrue(hubSource.Contains("WireDependencies"),
                "DemandManager hub must call _svc.WireDependencies(...).");
        }

        [Test]
        public void demand_service_is_in_correct_namespace()
        {
            Type t = typeof(Domains.Demand.Services.DemandService);
            Assert.AreEqual("Domains.Demand.Services", t.Namespace,
                $"DemandService namespace mismatch: {t.Namespace}");
        }

        [Test]
        public void demand_service_exposes_wire_dependencies()
        {
            Type t = typeof(Domains.Demand.Services.DemandService);
            Assert.IsNotNull(t, "DemandService must exist");
            MethodInfo m = t.GetMethod("WireDependencies");
            Assert.IsNotNull(m, "DemandService must expose WireDependencies()");
        }

        [Test]
        public void demand_service_exposes_update_rci_demand()
        {
            Type t = typeof(Domains.Demand.Services.DemandService);
            Assert.IsNotNull(t, "DemandService must exist");
            MethodInfo m = t.GetMethod("UpdateRCIDemand");
            Assert.IsNotNull(m, "DemandService must expose UpdateRCIDemand()");
        }

        [Test]
        public void demand_service_exposes_get_demand_level()
        {
            Type t = typeof(Domains.Demand.Services.DemandService);
            Assert.IsNotNull(t, "DemandService must exist");
            MethodInfo m = t.GetMethod("GetDemandLevel");
            Assert.IsNotNull(m, "DemandService must expose GetDemandLevel()");
        }

        private static string GetRepoRoot()
        {
            string dir = Application.dataPath;
            while (!string.IsNullOrEmpty(dir))
            {
                if (File.Exists(Path.Combine(dir, "CLAUDE.md"))) return dir;
                string parent = Path.GetDirectoryName(dir);
                if (parent == dir) break;
                dir = parent;
            }
            return Application.dataPath.Replace("/Assets", "");
        }
    }
}
