using System;
using System.Collections.Generic;
using UnityEngine;

namespace Territory.Simulation.Signals
{
    /// <summary>Per-tick orchestrator for the city-sim depth signal phase: district rebuild → producers → diffusion → district rollup → consumers. Inspector-resolved deps; no <c>FindObjectOfType</c> in <see cref="Tick"/>. See <c>simulation-signals.md</c> §Interface contract.</summary>
    public class SignalTickScheduler : MonoBehaviour
    {
        [SerializeField] private SignalFieldRegistry registry;
        [SerializeField] private SignalMetadataRegistry metadata;
        [SerializeField] private DistrictManager districtManager;
        [SerializeField] private List<MonoBehaviour> producerList = new List<MonoBehaviour>();
        [SerializeField] private List<MonoBehaviour> consumerList = new List<MonoBehaviour>();

        private List<ISignalProducer> _producers = new List<ISignalProducer>();
        private List<ISignalConsumer> _consumers = new List<ISignalConsumer>();
        private DistrictSignalCache _cache;
        private static readonly int SignalCount = Enum.GetValues(typeof(SimulationSignal)).Length;

        private void Awake()
        {
            if (registry == null)
            {
                registry = FindObjectOfType<SignalFieldRegistry>();
            }

            if (districtManager == null)
            {
                districtManager = FindObjectOfType<DistrictManager>();
            }

            if (metadata == null)
            {
                Debug.LogError("SignalTickScheduler.metadata not assigned — must be Inspector-set (ScriptableObject asset). Aborting setup.");
                return;
            }

            if (_cache == null)
            {
                _cache = new DistrictSignalCache();
            }

            _producers.Clear();
            for (int i = 0; i < producerList.Count; i++)
            {
                MonoBehaviour mb = producerList[i];
                if (mb is ISignalProducer prod)
                {
                    _producers.Add(prod);
                }
                else if (mb != null)
                {
                    Debug.LogWarning($"SignalTickScheduler.producerList[{i}] = '{mb.GetType().Name}' does not implement ISignalProducer — skipping.");
                }
            }

            _consumers.Clear();
            for (int i = 0; i < consumerList.Count; i++)
            {
                MonoBehaviour mb = consumerList[i];
                if (mb is ISignalConsumer cons)
                {
                    _consumers.Add(cons);
                }
                else if (mb != null)
                {
                    Debug.LogWarning($"SignalTickScheduler.consumerList[{i}] = '{mb.GetType().Name}' does not implement ISignalConsumer — skipping.");
                }
            }
        }

        /// <summary>Run one signal-phase tick: district rebuild → producers emit → diffusion over every signal field → <see cref="DistrictAggregator"/> rollup → consumers read post-rollup state.</summary>
        public void Tick(float deltaSeconds)
        {
            if (registry == null || metadata == null)
            {
                return;
            }

            // Phase 0 — DistrictMap rebuild (refresh per-cell district ids before producers run).
            if (districtManager != null)
            {
                districtManager.Rebuild();
            }

            // Phase 1 — producers.
            for (int i = 0; i < _producers.Count; i++)
            {
                _producers[i].EmitSignals(registry);
            }

            // Phase 2 — diffusion over every signal field.
            for (int i = 0; i < SignalCount; i++)
            {
                SimulationSignal signal = (SimulationSignal)i;
                SignalField field = registry.GetField(signal);
                SignalMetadataRegistry.Entry meta = metadata.GetMetadata(signal);
                DiffusionKernel.Apply(field, meta);
            }

            // Phase 3 — DistrictAggregator: roll signal fields into per-district mean / P90 cache.
            if (districtManager != null && districtManager.Map != null)
            {
                DistrictAggregator.Aggregate(registry, districtManager.Map, metadata, _cache);
            }

            // Phase 4 — consumers (cache populated when district manager wired; empty Clear-state otherwise).
            for (int i = 0; i < _consumers.Count; i++)
            {
                _consumers[i].ConsumeSignals(registry, _cache);
            }
        }
    }
}
