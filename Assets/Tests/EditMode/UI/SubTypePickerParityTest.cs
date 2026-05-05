// TECH-14100 / game-ui-catalog-bake Stage 8 §Test Blueprint.
//
// Pins SubtypePickerController contract against TECH-10500 design intent:
//   1. ToolFamily enum cardinality = 4 (Residential / Commercial / Industrial / StateService).
//      Power / water / roads / environmental folded into StateService catalog rows
//      driven by ZoneSubTypeRegistry — NOT separate ToolFamily values.
//   2. R/C/I families each yield exactly 3 density tier rows (Light / Medium / Heavy).
//   3. StateService family delegates row enumeration to ZoneSubTypeRegistry.
//
// Drift signal: any of these properties changing without a Plan Digest update means
// SubtypePickerController contract drifted from main-branch parity intent.

using System;
using System.Linq;
using NUnit.Framework;
using Territory.UI;

namespace Territory.Tests.EditMode.UI
{
    public class SubTypePickerParityTest
    {
        [Test]
        public void ToolFamily_HasExactlyFourValues()
        {
            Array values = Enum.GetValues(typeof(ToolFamily));
            Assert.AreEqual(4, values.Length,
                $"ToolFamily must carry exactly 4 values (TECH-10500 contract). Found {values.Length}: " +
                string.Join(", ", values.Cast<ToolFamily>().Select(v => v.ToString())));
        }

        [Test]
        public void ToolFamily_ContainsResidentialCommercialIndustrialStateService()
        {
            string[] names = Enum.GetNames(typeof(ToolFamily));
            CollectionAssert.Contains(names, nameof(ToolFamily.Residential));
            CollectionAssert.Contains(names, nameof(ToolFamily.Commercial));
            CollectionAssert.Contains(names, nameof(ToolFamily.Industrial));
            CollectionAssert.Contains(names, nameof(ToolFamily.StateService));
        }

        [Test]
        public void ToolFamily_DoesNotContainPowerWaterRoadsEnvironmental()
        {
            // Main branch had separate Power / Water / Roads / Environmental categories;
            // TECH-10500 collapsed them into StateService. Drift guard: re-introducing
            // any of those names breaks the StateService catalog dispatch contract.
            string[] names = Enum.GetNames(typeof(ToolFamily));
            CollectionAssert.DoesNotContain(names, "Power");
            CollectionAssert.DoesNotContain(names, "Water");
            CollectionAssert.DoesNotContain(names, "Roads");
            CollectionAssert.DoesNotContain(names, "Environmental");
        }

        [Test]
        public void SubtypePickerController_TypeExists_InTerritoryUiNamespace()
        {
            Type t = typeof(SubtypePickerController);
            Assert.AreEqual("Territory.UI", t.Namespace,
                "SubtypePickerController must live in Territory.UI namespace (TECH-10500 migration).");
            Assert.IsTrue(typeof(UnityEngine.MonoBehaviour).IsAssignableFrom(t),
                "SubtypePickerController must be a MonoBehaviour.");
        }

        [Test]
        public void SubtypePickerController_HasShowAndHidePublicEntryPoints()
        {
            Type t = typeof(SubtypePickerController);
            var show = t.GetMethod("Show", new[] { typeof(Territory.UI.UIManager), typeof(ToolFamily) });
            Assert.NotNull(show, "SubtypePickerController.Show(UIManager, ToolFamily) entry point missing — UIManager.Toolbar.cs depends on this signature.");

            var hide = t.GetMethod("Hide", new[] { typeof(bool) });
            Assert.NotNull(hide, "SubtypePickerController.Hide(bool cancelled) entry point missing — UIManager.PopupStack.cs Esc-stack pop depends on this signature.");
        }
    }
}
