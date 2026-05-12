using NUnit.Framework;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace Territory.Tests.EditMode.Atomization.Stage4_5
{
    /// <summary>
    /// §Red-Stage Proof anchor: UIManagerUtilitiesThinSpec.cs::ui_manager_utilities_is_thin
    /// Stage 4.5: UIManager.Utilities Tier-B THIN — UIManager.Utilities.cs ≤200 LOC;
    /// pure logic delegated to UIManagerUtilitiesService; stem partial declares MonoBehaviour.
    /// </summary>
    public class UIManagerUtilitiesThinSpec
    {
        private const string UtilitiesPath =
            "Assets/Scripts/Managers/GameManagers/UIManager.Utilities.cs";

        private const string StemPath =
            "Assets/Scripts/Managers/GameManagers/UIManager.cs";

        private const string ServicePath =
            "Assets/Scripts/Domains/UI/Services/UIManagerUtilitiesService.cs";

        [Test]
        public void ui_manager_utilities_is_thin()
        {
            string root = GetRepoRoot();

            // Assert 1: UIManager.Utilities.cs ≤200 LOC
            string utilPath = Path.Combine(root, UtilitiesPath);
            Assert.IsTrue(File.Exists(utilPath), $"UIManager.Utilities.cs not found at {utilPath}");
            int lineCount = File.ReadAllLines(utilPath).Length;
            Assert.LessOrEqual(lineCount, 200,
                $"UIManager.Utilities.cs must be ≤200 LOC after THIN. Got {lineCount} lines.");

            // Assert 2: stem file declares MonoBehaviour (guardrail #7)
            string stemPath = Path.Combine(root, StemPath);
            Assert.IsTrue(File.Exists(stemPath), $"UIManager.cs stem not found at {stemPath}");
            string stemSource = File.ReadAllText(stemPath);
            Assert.IsTrue(stemSource.Contains("partial class UIManager : MonoBehaviour"),
                "UIManager.cs stem must declare 'partial class UIManager : MonoBehaviour' (guardrail #7).");

            // Assert 3: Utilities partial has no MonoBehaviour base (thin partial — no base)
            string utilSource = File.ReadAllText(utilPath);
            Assert.IsFalse(utilSource.Contains(": MonoBehaviour"),
                "UIManager.Utilities.cs must NOT redeclare MonoBehaviour base (stem owns it).");

            // Assert 4: partial delegates to UIManagerUtilitiesService
            Assert.IsTrue(utilSource.Contains("UIManagerUtilitiesService"),
                "UIManager.Utilities.cs must reference UIManagerUtilitiesService.");
            Assert.IsTrue(utilSource.Contains("_utilitiesService"),
                "UIManager.Utilities.cs must hold _utilitiesService delegate field.");

            // Assert 5: UIManagerUtilitiesService.cs exists
            string svcPath = Path.Combine(root, ServicePath);
            Assert.IsTrue(File.Exists(svcPath),
                $"UIManagerUtilitiesService.cs must exist at {svcPath}.");
        }

        [Test]
        public void ui_manager_stem_file_declares_monobehaviour()
        {
            string root     = GetRepoRoot();
            string stemPath = Path.Combine(root, StemPath);
            Assert.IsTrue(File.Exists(stemPath), $"UIManager.cs not found at {stemPath}");
            string src = File.ReadAllText(stemPath);
            Assert.IsTrue(src.Contains("partial class UIManager : MonoBehaviour"),
                "UIManager.cs stem must declare MonoBehaviour (guardrail #7).");
        }

        [Test]
        public void utilities_service_exists_in_correct_namespace()
        {
            Type t = Type.GetType("Domains.UI.Services.UIManagerUtilitiesService, Assembly-CSharp");
            Assert.IsNotNull(t, "UIManagerUtilitiesService must be loadable from Assembly-CSharp");
            Assert.AreEqual("Domains.UI.Services", t.Namespace,
                $"UIManagerUtilitiesService namespace mismatch: {t.Namespace}");
        }

        [Test]
        public void utilities_service_exposes_placement_message_lookup()
        {
            Type t = Type.GetType("Domains.UI.Services.UIManagerUtilitiesService, Assembly-CSharp");
            Assert.IsNotNull(t, "UIManagerUtilitiesService must exist");
            Assert.IsNotNull(t.GetMethod("TryGetPlacementMessage"),
                "UIManagerUtilitiesService must expose TryGetPlacementMessage");
        }

        [Test]
        public void utilities_service_placement_message_none_returns_false()
        {
            var svc = new Domains.UI.Services.UIManagerUtilitiesService();
            bool result = svc.TryGetPlacementMessage(Territory.Core.PlacementFailReason.None, out string msg);
            Assert.IsFalse(result, "TryGetPlacementMessage(None) must return false.");
            Assert.IsNull(msg, "message must be null for None reason.");
        }

        [Test]
        public void utilities_service_placement_message_unaffordable_resolves()
        {
            var svc = new Domains.UI.Services.UIManagerUtilitiesService();
            bool result = svc.TryGetPlacementMessage(Territory.Core.PlacementFailReason.Unaffordable, out string msg);
            Assert.IsTrue(result, "TryGetPlacementMessage(Unaffordable) must return true.");
            Assert.IsNotNull(msg, "message must not be null for Unaffordable.");
        }

        [Test]
        public void utilities_service_builds_insufficient_funds_message()
        {
            string msg = Domains.UI.Services.UIManagerUtilitiesService.BuildInsufficientFundsMessage("Road", 500, 200);
            Assert.IsTrue(msg.Contains("Road"), "Message must include item type.");
            Assert.IsTrue(msg.Contains("500"),  "Message must include cost.");
            Assert.IsTrue(msg.Contains("200"),  "Message must include available funds.");
        }

        [Test]
        public void utilities_service_forest_type_to_index()
        {
            Assert.AreEqual(0, Domains.UI.Services.UIManagerUtilitiesService.ForestTypeToIndex(Territory.Forests.Forest.ForestType.Sparse));
            Assert.AreEqual(1, Domains.UI.Services.UIManagerUtilitiesService.ForestTypeToIndex(Territory.Forests.Forest.ForestType.Medium));
            Assert.AreEqual(2, Domains.UI.Services.UIManagerUtilitiesService.ForestTypeToIndex(Territory.Forests.Forest.ForestType.Dense));
        }

        [Test]
        public void utilities_service_no_tier_e_needed()
        {
            string root    = GetRepoRoot();
            string svcPath = Path.Combine(root, ServicePath);
            Assert.IsTrue(File.Exists(svcPath), $"UIManagerUtilitiesService.cs not found at {svcPath}");
            int lineCount = File.ReadAllLines(svcPath).Length;
            Assert.LessOrEqual(lineCount, 500,
                $"UIManagerUtilitiesService.cs is {lineCount} LOC — if >500 a Tier-E sub-split must be scheduled.");
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
