using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Territory.Economy;
using Territory.Simulation;
using Territory.Simulation.Signals;

namespace Territory.Tests.EditMode.Simulation.Signals
{
    /// <summary>EditMode smoke covering <see cref="SimulationManager.ProcessSimulationTick"/> → <see cref="SignalTickScheduler.Tick"/> → producer emit + diffusion neighbor spread.</summary>
    [TestFixture]
    public class SignalTickSchedulerSmokeTest
    {
        private GameObject cityStatsGO;
        private GameObject simManagerGO;
        private GameObject schedulerGO;
        private GameObject registryGO;
        private GameObject producerGO;
        private SignalMetadataRegistry metadataAsset;

        private CityStats cityStats;
        private SimulationManager simulationManager;
        private SignalTickScheduler scheduler;
        private SignalFieldRegistry registry;
        private StubProducer producer;

        [SetUp]
        public void SetUp()
        {
            cityStatsGO = new GameObject("SmokeCityStats");
            cityStats = cityStatsGO.AddComponent<CityStats>();
            cityStats.simulateGrowth = true;

            registryGO = new GameObject("SmokeSignalFieldRegistry");
            registry = registryGO.AddComponent<SignalFieldRegistry>();
            // Awake ran with no grid; allocate fields at smoke-test dims.
            registry.ResizeForMap(20, 20);

            metadataAsset = ScriptableObject.CreateInstance<SignalMetadataRegistry>();
            FieldInfo entriesField = typeof(SignalMetadataRegistry).GetField(
                "entries", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(entriesField, "SignalMetadataRegistry.entries field missing");
            SignalMetadataRegistry.Entry[] entries = new SignalMetadataRegistry.Entry[12];
            for (int i = 0; i < entries.Length; i++)
            {
                entries[i] = new SignalMetadataRegistry.Entry
                {
                    diffusionRadius = 2f,
                    decayPerStep = 0.05f,
                    anisotropy = new Vector2(1f, 1f),
                    rollup = RollupRule.Mean,
                };
            }
            entriesField.SetValue(metadataAsset, entries);

            producerGO = new GameObject("SmokeStubProducer");
            producer = producerGO.AddComponent<StubProducer>();

            schedulerGO = new GameObject("SmokeSignalTickScheduler");
            scheduler = schedulerGO.AddComponent<SignalTickScheduler>();

            FieldInfo registryField = typeof(SignalTickScheduler).GetField(
                "registry", BindingFlags.NonPublic | BindingFlags.Instance);
            registryField.SetValue(scheduler, registry);

            FieldInfo metadataField = typeof(SignalTickScheduler).GetField(
                "metadata", BindingFlags.NonPublic | BindingFlags.Instance);
            metadataField.SetValue(scheduler, metadataAsset);

            FieldInfo producerListField = typeof(SignalTickScheduler).GetField(
                "producerList", BindingFlags.NonPublic | BindingFlags.Instance);
            producerListField.SetValue(scheduler, new List<MonoBehaviour> { producer });

            FieldInfo consumerListField = typeof(SignalTickScheduler).GetField(
                "consumerList", BindingFlags.NonPublic | BindingFlags.Instance);
            consumerListField.SetValue(scheduler, new List<MonoBehaviour>());

            // Re-trigger SignalTickScheduler.Awake so cached typed lists pick up the producer.
            MethodInfo awake = typeof(SignalTickScheduler).GetMethod(
                "Awake", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(awake, "SignalTickScheduler.Awake method missing");
            awake.Invoke(scheduler, null);

            simManagerGO = new GameObject("SmokeSimulationManager");
            simulationManager = simManagerGO.AddComponent<SimulationManager>();
            simulationManager.cityStats = cityStats;

            FieldInfo schedulerField = typeof(SimulationManager).GetField(
                "signalTickScheduler", BindingFlags.NonPublic | BindingFlags.Instance);
            schedulerField.SetValue(simulationManager, scheduler);
        }

        [TearDown]
        public void TearDown()
        {
            if (simManagerGO != null) Object.DestroyImmediate(simManagerGO);
            if (schedulerGO != null) Object.DestroyImmediate(schedulerGO);
            if (producerGO != null) Object.DestroyImmediate(producerGO);
            if (registryGO != null) Object.DestroyImmediate(registryGO);
            if (cityStatsGO != null) Object.DestroyImmediate(cityStatsGO);
            if (metadataAsset != null) Object.DestroyImmediate(metadataAsset);
        }

        [Test]
        public void Tick_InvokesSchedulerExactlyOnce()
        {
            simulationManager.ProcessSimulationTick();

            Assert.AreEqual(1, producer.EmitCount, "Producer should be emitted exactly once per tick");

            float neighborValue = registry.GetField(SimulationSignal.PollutionAir).Get(6, 5);
            Assert.Greater(neighborValue, 0f, "Neighbor cell (6,5) should receive diffused mass after Tick");
        }

        private class StubProducer : MonoBehaviour, ISignalProducer
        {
            public int EmitCount { get; private set; }

            public void EmitSignals(SignalFieldRegistry registry)
            {
                EmitCount++;
                if (registry == null) return;
                SignalField field = registry.GetField(SimulationSignal.PollutionAir);
                if (field == null) return;
                field.Add(5, 5, 4f);
            }
        }
    }
}
