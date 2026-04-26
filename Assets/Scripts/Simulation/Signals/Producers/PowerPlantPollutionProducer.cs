using Territory.Buildings;
using Territory.Core;
using UnityEngine;

namespace Territory.Simulation.Signals.Producers
{
    /// <summary>
    /// Stage 4 producer emitting <see cref="SimulationSignal.PollutionAir"/> contributions
    /// at registered <see cref="PowerPlant"/> locations. Discovers plants via
    /// <c>FindObjectsOfType</c> in <c>Awake</c> only (invariant #3 — no per-tick scene scan)
    /// and re-resolves the cache on demand if the list is stale. Plant grid coord derived
    /// via <see cref="GridManager.GetGridPosition(Vector2)"/> from <c>transform.position</c>.
    /// </summary>
    public class PowerPlantPollutionProducer : MonoBehaviour, ISignalProducer
    {
        [SerializeField] private GridManager gridManager;

        // Pollution per plant — matches CityStats.POLLUTION_NUCLEAR (line 884).
        private const float POLLUTION_NUCLEAR = 2.0f;

        private PowerPlant[] cachedPlants;

        private void Awake()
        {
            if (gridManager == null)
            {
                gridManager = FindObjectOfType<GridManager>();
            }
            cachedPlants = FindObjectsOfType<PowerPlant>();
        }

        /// <summary>Iterate cached plants; emit nuclear pollution at each plant cell.</summary>
        public void EmitSignals(SignalFieldRegistry registry)
        {
            if (registry == null || gridManager == null)
            {
                return;
            }
            SignalField field = registry.GetField(SimulationSignal.PollutionAir);
            if (field == null)
            {
                return;
            }

            // Refresh cache if scene mutated post-Awake (e.g. plant placed mid-game).
            if (cachedPlants == null)
            {
                cachedPlants = FindObjectsOfType<PowerPlant>();
            }

            int width = gridManager.width;
            int height = gridManager.height;
            for (int i = 0; i < cachedPlants.Length; i++)
            {
                PowerPlant plant = cachedPlants[i];
                if (plant == null)
                {
                    continue;
                }
                if (!TryGetPlantGrid(plant, out int gx, out int gy))
                {
                    continue;
                }
                if (gx < 0 || gx >= width || gy < 0 || gy >= height)
                {
                    continue;
                }
                field.Add(gx, gy, POLLUTION_NUCLEAR);
            }
        }

        /// <summary>Refresh the cached plant list. Invokers: tests, mid-run scene mutators.</summary>
        public void RefreshCache()
        {
            cachedPlants = FindObjectsOfType<PowerPlant>();
        }

        private bool TryGetPlantGrid(PowerPlant plant, out int gridX, out int gridY)
        {
            Vector2 world = plant.transform.position;
            Vector2 grid = gridManager.GetGridPosition(world);
            gridX = Mathf.RoundToInt(grid.x);
            gridY = Mathf.RoundToInt(grid.y);
            return true;
        }
    }
}
