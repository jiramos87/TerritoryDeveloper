using System.IO;
using NUnit.Framework;
using UnityEngine;
using Territory.Economy;

namespace Territory.Tests.EditMode.Economy
{
    /// <summary>TECH-687 — catalog-backed display + cent costs vs committed snapshot fragment.</summary>
    public class ZoneSubTypeRegistryCatalogBackedTests
    {
        private static string LoadFragmentJson()
        {
            string path = Path.Combine(Application.dataPath, "Tests/EditMode/Economy/Fixtures/zone_s_catalog_fragment.json");
            return File.ReadAllText(path);
        }

        [Test]
        public void SubTypeIds_CatalogBacked_DisplayAndCents_MatchFixture()
        {
            Assert.IsTrue(GridAssetCatalog.TryParseSnapshotJson(LoadFragmentJson(), out GridAssetSnapshotRoot root, out string perr),
                perr);

            var catalogGo = new GameObject("CatalogTech687");
            var catalog = catalogGo.AddComponent<GridAssetCatalog>();
            catalog.RebuildIndexes(root);

            var registryGo = new GameObject("RegistryTech687");
            var registry = registryGo.AddComponent<ZoneSubTypeRegistry>();
            try
            {
                Assert.AreEqual(7, registry.Entries.Count,
                    "Registry must load seven JSON entries when scene catalog is present.");

                for (int id = 0; id <= 6; id++)
                {
                    Assert.IsTrue(registry.TryGetAssetIdForSubType(id, out int assetId), $"asset map {id}");
                    Assert.AreEqual(id, assetId);

                    Assert.IsTrue(catalog.TryGetAsset(assetId, out CatalogAssetRowDto assetRow), $"asset row {id}");
                    Assert.IsTrue(catalog.TryGetEconomyForAsset(assetId, out CatalogEconomyRowDto econRow), $"econ {id}");

                    Assert.IsTrue(registry.TryGetPickerLabelForSubType(id, out string line, out int costCents), $"label {id}");
                    Assert.AreEqual(econRow.base_cost_cents, costCents);
                    int simUnits = econRow.base_cost_cents / 100;
                    Assert.AreEqual($"{assetRow.display_name} (${simUnits})", line);

                    Assert.IsTrue(registry.TryGetStateServiceBuildCostSimUnits(id, out int simCost), $"build cost {id}");
                    Assert.AreEqual(simUnits, simCost);
                }
            }
            finally
            {
                Object.DestroyImmediate(registryGo);
                Object.DestroyImmediate(catalogGo);
            }
        }
    }
}
