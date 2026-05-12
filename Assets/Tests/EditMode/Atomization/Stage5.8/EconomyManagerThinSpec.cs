using NUnit.Framework;
using System;
using System.IO;
using System.Reflection;

namespace Territory.Tests.EditMode.Atomization.Stage5_8
{
    /// <summary>
    /// §Red-Stage Proof anchor: EconomyManagerThinSpec.cs::economy_manager_is_thin
    /// Stage 5.8: EconomyManager Tier-C NO-PORT — hub collapses to ≤200 LOC; logic delegated to EconomyService.
    /// Green: EconomyManager.cs ≤200 LOC AND EconomyService.cs exists AND hub delegates via _svc.
    /// </summary>
    public class EconomyManagerThinSpec
    {
        private const string HubPath =
            "Assets/Scripts/Managers/GameManagers/EconomyManager.cs";

        private const string SvcPath =
            "Assets/Scripts/Domains/Economy/Services/EconomyService.cs";

        [Test]
        public void economy_manager_is_thin()
        {
            string repoRoot = GetRepoRoot();

            // Assert 1: EconomyManager.cs ≤200 LOC
            string hub = Path.Combine(repoRoot, HubPath);
            Assert.IsTrue(File.Exists(hub), $"EconomyManager.cs not found at {hub}");
            int lineCount = File.ReadAllLines(hub).Length;
            Assert.LessOrEqual(lineCount, 200,
                $"EconomyManager.cs must be ≤200 LOC after THIN. Got {lineCount} lines.");

            // Assert 2: EconomyService.cs exists under Domains/Economy/Services/
            string svc = Path.Combine(repoRoot, SvcPath);
            Assert.IsTrue(File.Exists(svc), $"EconomyService.cs must exist at {svc}.");

            // Assert 3: hub delegates to EconomyService (_svc field present)
            string hubSource = File.ReadAllText(hub);
            Assert.IsTrue(hubSource.Contains("EconomyService"),
                "EconomyManager hub must reference EconomyService.");
            Assert.IsTrue(hubSource.Contains("_svc"),
                "EconomyManager hub must hold a _svc delegate field.");

            // Assert 4: hub uses WireDependencies pattern
            Assert.IsTrue(hubSource.Contains("WireDependencies"),
                "EconomyManager hub must call _svc.WireDependencies(...).");
        }

        [Test]
        public void economy_service_is_in_correct_namespace()
        {
            Type t = typeof(Domains.Economy.Services.EconomyService);
            Assert.AreEqual("Domains.Economy.Services", t.Namespace,
                $"EconomyService namespace mismatch: {t.Namespace}");
        }

        [Test]
        public void economy_service_exposes_spend_money()
        {
            Type t = typeof(Domains.Economy.Services.EconomyService);
            MethodInfo m = t.GetMethod("SpendMoney");
            Assert.IsNotNull(m, "EconomyService must expose SpendMoney()");
        }

        [Test]
        public void economy_service_exposes_get_projected_monthly_income()
        {
            Type t = typeof(Domains.Economy.Services.EconomyService);
            MethodInfo m = t.GetMethod("GetProjectedMonthlyIncome");
            Assert.IsNotNull(m, "EconomyService must expose GetProjectedMonthlyIncome()");
        }

        private static string GetRepoRoot()
        {
            string path = UnityEngine.Application.dataPath;
            return Path.GetDirectoryName(path);
        }
    }
}
