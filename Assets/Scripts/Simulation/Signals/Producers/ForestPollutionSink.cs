using Territory.Core;
using Territory.Forests;
using UnityEngine;

namespace Territory.Simulation.Signals.Producers
{
    /// <summary>
    /// Stage 4 sink emitting negative <see cref="SimulationSignal.PollutionAir"/> at forested cells.
    /// <see cref="SignalField.Add"/> floor-clamps the sum to 0, so net field stays non-negative
    /// (per <c>simulation-signals.md</c> §Diffusion physics contract). Absorption rate matches
    /// <c>CityStats.FOREST_ABSORPTION_RATE</c> (line 885).
    /// </summary>
    public class ForestPollutionSink : MonoBehaviour, ISignalProducer
    {
        [SerializeField] private ForestManager forestManager;
        [SerializeField] private GridManager gridManager;

        private const float FOREST_ABSORPTION_RATE = 0.3f;

        private void Awake()
        {
            if (forestManager == null)
            {
                forestManager = FindObjectOfType<ForestManager>();
            }
            if (gridManager == null)
            {
                gridManager = FindObjectOfType<GridManager>();
            }
        }

        /// <summary>Iterate grid; subtract <see cref="FOREST_ABSORPTION_RATE"/> at each forested cell.</summary>
        public void EmitSignals(SignalFieldRegistry registry)
        {
            if (registry == null || gridManager == null || forestManager == null)
            {
                return;
            }
            SignalField field = registry.GetField(SimulationSignal.PollutionAir);
            if (field == null)
            {
                return;
            }

            int width = gridManager.width;
            int height = gridManager.height;
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (forestManager.IsForestAt(x, y))
                    {
                        field.Add(x, y, -FOREST_ABSORPTION_RATE);
                    }
                }
            }
        }
    }
}
