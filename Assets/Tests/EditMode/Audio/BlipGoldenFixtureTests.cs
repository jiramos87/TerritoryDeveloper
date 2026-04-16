using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Territory.Audio;

namespace Territory.Tests.EditMode.Audio
{
    /// <summary>
    /// Per-id golden fixture regression gate.
    /// Parses <c>tools/fixtures/blip/{id}-v0.json</c>, re-renders via
    /// <see cref="BlipTestFixtures.RenderPatch"/>, and asserts DSP fingerprints
    /// match within tolerance.  Stale-fixture guard fires when the SO
    /// <c>patchHash</c> diverges from the fixture JSON — instructs reviewer to
    /// re-run the TECH-227 bake script.
    /// </summary>
    public class BlipGoldenFixtureTests
    {
        [System.Serializable]
        private class BlipFixtureDto
        {
            public string id;
            public int variant;
            public int patchHash;
            public int sampleRate;
            public int sampleCount;
            public double sumAbsHash;
            public int zeroCrossings;
        }

        [TestCase(BlipId.UiButtonHover)]
        [TestCase(BlipId.UiButtonClick)]
        [TestCase(BlipId.ToolRoadTick)]
        [TestCase(BlipId.ToolRoadComplete)]
        [TestCase(BlipId.ToolBuildingPlace)]
        [TestCase(BlipId.ToolBuildingDenied)]
        [TestCase(BlipId.WorldCellSelected)]
        [TestCase(BlipId.EcoMoneyEarned)]
        [TestCase(BlipId.EcoMoneySpent)]
        [TestCase(BlipId.SysSaveGame)]
        public void RenderMatchesFixture(BlipId id)
        {
            BlipFixtureDto fx = LoadFixture(id);

            var patch = AssetDatabase.LoadAssetAtPath<BlipPatch>(
                $"Assets/Audio/Blip/Patches/BlipPatch_{id}.asset");
            Assert.IsNotNull(patch, $"Missing SO for {id}");

            Assert.AreEqual(fx.patchHash, patch.PatchHash,
                $"Stale fixture for {id} — rerun `npx ts-node tools/scripts/blip-bake-fixtures.ts` (TECH-227)");

            Assert.AreEqual(0, fx.sampleCount % fx.sampleRate,
                $"{id} fixture sampleCount not whole-second multiple");
            int seconds = fx.sampleCount / fx.sampleRate;

            var flat = BlipPatchFlat.FromSO(patch);
            float[] buffer = BlipTestFixtures.RenderPatch(in flat, fx.sampleRate, seconds, fx.variant);

            double actualSum = BlipTestFixtures.SumAbsHash(buffer);
            int actualZc = BlipTestFixtures.CountZeroCrossings(buffer);

            Assert.That(actualSum, Is.EqualTo(fx.sumAbsHash).Within(1e-6),
                $"{id} sumAbsHash drift expected={fx.sumAbsHash} actual={actualSum}");
            Assert.That(actualZc, Is.EqualTo(fx.zeroCrossings).Within(2),
                $"{id} zeroCrossings drift expected={fx.zeroCrossings} actual={actualZc}");
        }

        private static BlipFixtureDto LoadFixture(BlipId id)
        {
            string path = Path.Combine(Application.dataPath, "..",
                "tools", "fixtures", "blip", $"{id}-v0.json");
            return JsonUtility.FromJson<BlipFixtureDto>(File.ReadAllText(path));
        }
    }
}
