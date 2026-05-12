using NUnit.Framework;
using System;
using System.IO;
using System.Reflection;
using Domains.UI.Services;

namespace Territory.Tests.EditMode.Atomization.Stage5_7
{
    /// <summary>
    /// §Red-Stage Proof anchor: ToolbarDataAdapterThinSpec.cs::toolbar_data_adapter_is_thin
    /// Stage 5.7: ToolbarDataAdapter Tier-C NO-PORT — hub collapses to ≤200 LOC;
    /// logic delegated to ToolbarAdapterService.
    /// Green: ToolbarDataAdapter.cs ≤200 LOC AND ToolbarAdapterService.cs exists.
    /// </summary>
    public class ToolbarDataAdapterThinSpec
    {
        private const string HubPath =
            "Assets/Scripts/UI/Toolbar/ToolbarDataAdapter.cs";

        private const string SvcPath =
            "Assets/Scripts/Domains/UI/Services/ToolbarAdapterService.cs";

        [Test]
        public void toolbar_data_adapter_is_thin()
        {
            string repoRoot = GetRepoRoot();

            // Assert 1: ToolbarDataAdapter.cs ≤200 LOC
            string hub = Path.Combine(repoRoot, HubPath);
            Assert.IsTrue(File.Exists(hub), $"ToolbarDataAdapter.cs not found at {hub}");
            int lineCount = File.ReadAllLines(hub).Length;
            Assert.LessOrEqual(lineCount, 200,
                $"ToolbarDataAdapter.cs must be ≤200 LOC after THIN. Got {lineCount} lines.");

            // Assert 2: ToolbarAdapterService.cs exists under Domains/UI/Services/
            string svc = Path.Combine(repoRoot, SvcPath);
            Assert.IsTrue(File.Exists(svc), $"ToolbarAdapterService.cs must exist at {svc}.");
        }

        [Test]
        public void toolbar_adapter_service_is_in_correct_namespace()
        {
            Type t = typeof(Domains.UI.Services.ToolbarAdapterService);
            Assert.AreEqual("Domains.UI.Services", t.Namespace,
                $"ToolbarAdapterService namespace mismatch: {t.Namespace}");
        }

        [Test]
        public void toolbar_adapter_service_exposes_update_illumination()
        {
            Type t = typeof(Domains.UI.Services.ToolbarAdapterService);
            MethodInfo m = t.GetMethod("UpdateIllumination");
            Assert.IsNotNull(m, "ToolbarAdapterService must expose UpdateIllumination()");
        }

        [Test]
        public void toolbar_adapter_service_exposes_rebind_buttons_by_icon_slug()
        {
            Type t = typeof(Domains.UI.Services.ToolbarAdapterService);
            MethodInfo m = t.GetMethod("RebindButtonsByIconSlug");
            Assert.IsNotNull(m, "ToolbarAdapterService must expose RebindButtonsByIconSlug(Transform)");
        }

        private static string GetRepoRoot()
        {
            // Walk up from application data path until we find Assets/ peer.
            string path = UnityEngine.Application.dataPath;
            return Path.GetDirectoryName(path);
        }
    }
}
