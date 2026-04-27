using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Territory.Simulation.Signals;
using Territory.Simulation.Signals.Consumers;
using Territory.Simulation.Signals.Events;

namespace Territory.Tests.EditMode.Simulation.Signals
{
    /// <summary>
    /// Stage 8 unit + light-integration coverage for <see cref="CrimeHotspotEventEmitter"/>
    /// (TECH-1955). Cache-driven (avoids full scheduler scaffolding): seeds
    /// <see cref="DistrictSignalCache"/> directly with district P90 values; asserts
    /// emitter invokes <c>Hotspot</c> only for districts above threshold and skips NaN
    /// districts. Includes a JsonUtility round-trip parity sanity check on
    /// <see cref="CrimeHotspotEvent"/>'s <c>[System.Serializable]</c> contract.
    /// </summary>
    [TestFixture]
    public class CrimeHotspotEventEmitterTest
    {
        private GameObject emitterGO;
        private CrimeHotspotEventEmitter emitter;
        private SignalTuningWeightsAsset weightsAsset;

        [SetUp]
        public void SetUp()
        {
            weightsAsset = ScriptableObject.CreateInstance<SignalTuningWeightsAsset>();

            emitterGO = new GameObject("CrimeHotspotEmitter");
            emitter = emitterGO.AddComponent<CrimeHotspotEventEmitter>();
            FieldInfo weightsField = typeof(CrimeHotspotEventEmitter).GetField(
                "tuningWeights", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(weightsField, "tuningWeights field missing");
            weightsField.SetValue(emitter, weightsAsset);
        }

        [TearDown]
        public void TearDown()
        {
            if (emitterGO != null) Object.DestroyImmediate(emitterGO);
            if (weightsAsset != null) Object.DestroyImmediate(weightsAsset);
        }

        [Test]
        public void CrimeHotspotEventEmitter_Tick_NoPolice_EmitsAtLeastOnceAboveThreshold()
        {
            // No-police fixture surrogate: district 0 P90 Crime = 30 (well above 15 threshold).
            DistrictSignalCache cache = new DistrictSignalCache();
            cache.Set(0, SimulationSignal.Crime, 30f);
            cache.Set(1, SimulationSignal.Crime, 5f);  // below threshold
            cache.Set(2, SimulationSignal.Crime, 8f);
            cache.Set(3, SimulationSignal.Crime, 3f);

            List<CrimeHotspotEvent> captured = new List<CrimeHotspotEvent>();
            emitter.Hotspot += e => captured.Add(e);

            // 10-tick loop: emitter is idempotent on stable cache values; we count >= 1.
            for (int i = 0; i < 10; i++)
            {
                emitter.ConsumeSignals(null, cache);
            }

            Assert.GreaterOrEqual(captured.Count, 1, "no hotspot emitted for district above threshold");
            Assert.Greater(captured[0].level, weightsAsset.CrimeHotspotThreshold);
            Assert.AreEqual(0, captured[0].districtId);
        }

        [Test]
        public void CrimeHotspotEventEmitter_Tick_WithPolice_EmitsZeroForCoveredDistrict()
        {
            // Police-fixture surrogate: district 0 P90 Crime = 5 (below threshold thanks to ServicePolice).
            DistrictSignalCache cache = new DistrictSignalCache();
            cache.Set(0, SimulationSignal.Crime, 5f);
            cache.Set(1, SimulationSignal.Crime, 3f);
            cache.Set(2, SimulationSignal.Crime, 4f);
            cache.Set(3, SimulationSignal.Crime, 2f);

            List<CrimeHotspotEvent> captured = new List<CrimeHotspotEvent>();
            emitter.Hotspot += e => captured.Add(e);

            for (int i = 0; i < 10; i++)
            {
                emitter.ConsumeSignals(null, cache);
            }

            int districtZeroHits = 0;
            for (int i = 0; i < captured.Count; i++)
            {
                if (captured[i].districtId == 0)
                {
                    districtZeroHits++;
                }
            }
            Assert.AreEqual(0, districtZeroHits, "policed district emitted hotspot below threshold");
        }

        [Test]
        public void CrimeHotspotEventEmitter_SkipsNaNDistrict()
        {
            // No Set() calls → cache.Get returns NaN for every district.
            DistrictSignalCache cache = new DistrictSignalCache();

            List<CrimeHotspotEvent> captured = new List<CrimeHotspotEvent>();
            emitter.Hotspot += e => captured.Add(e);

            emitter.ConsumeSignals(null, cache);

            Assert.AreEqual(0, captured.Count, "emitter fired on NaN-rollup district");
        }

        [Test]
        public void CrimeHotspotEventEmitter_StrictThreshold_TieDoesNotEmit()
        {
            DistrictSignalCache cache = new DistrictSignalCache();
            cache.Set(0, SimulationSignal.Crime, weightsAsset.CrimeHotspotThreshold);

            List<CrimeHotspotEvent> captured = new List<CrimeHotspotEvent>();
            emitter.Hotspot += e => captured.Add(e);

            emitter.ConsumeSignals(null, cache);

            Assert.AreEqual(0, captured.Count, "tie at threshold should not emit (strict >)");
        }

        [Test]
        public void CrimeHotspotEvent_PocoSerializable()
        {
            CrimeHotspotEvent original = new CrimeHotspotEvent { districtId = 2, level = 19.5f };
            string json = JsonUtility.ToJson(original);
            CrimeHotspotEvent restored = JsonUtility.FromJson<CrimeHotspotEvent>(json);
            Assert.AreEqual(original.districtId, restored.districtId);
            Assert.AreEqual(original.level, restored.level, 1e-5f);
        }
    }
}
