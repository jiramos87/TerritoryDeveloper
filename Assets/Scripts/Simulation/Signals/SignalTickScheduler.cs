using System;
using System.Collections.Generic;
using UnityEngine;

namespace Territory.Simulation.Signals
{
    /// <summary>Per-tick orchestrator for the city-sim depth signal phase: producers → diffusion → rollup-stub → consumers. Inspector-resolved deps; no <c>FindObjectOfType</c> in <see cref="Tick"/>. See <c>simulation-signals.md</c> §Interface contract.</summary>
    public class SignalTickScheduler : MonoBehaviour
    {
        [SerializeField] private SignalFieldRegistry registry;
        [SerializeField] private SignalMetadataRegistry metadata;
        [SerializeField] private List<MonoBehaviour> producerList = new List<MonoBehaviour>();
        [SerializeField] private List<MonoBehaviour> consumerList = new List<MonoBehaviour>();

        private List<ISignalProducer> _producers = new List<ISignalProducer>();
        private List<ISignalConsumer> _consumers = new List<ISignalConsumer>();
        private static readonly int SignalCount = Enum.GetValues(typeof(SimulationSignal)).Length;

        private void Awake()
        {
            if (registry == null)
            {
                registry = FindObjectOfType<SignalFieldRegistry>();
            }

            if (metadata == null)
            {
                Debug.LogError("SignalTickScheduler.metadata not assigned — must be Inspector-set (ScriptableObject asset). Aborting setup.");
                return;
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

        /// <summary>Run one signal-phase tick: producers emit, diffusion runs over every signal field, rollup deferred to Stage 3, consumers read post-diffusion state.</summary>
        public void Tick(float deltaSeconds)
        {
            if (registry == null || metadata == null)
            {
                return;
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

            // Phase 3 — DistrictAggregator (DistrictSignalCache aggregator lands in Stage 3 — no-op until then).

            // Phase 4 — consumers (cache stays null until Stage 3).
            for (int i = 0; i < _consumers.Count; i++)
            {
                _consumers[i].ConsumeSignals(registry, null);
            }
        }
    }
}
