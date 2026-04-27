using Territory.Core;
using Territory.Simulation.Signals;
using UnityEngine;

namespace Territory.Simulation
{
    /// <summary>
    /// Stage 5 desirability consumer over the signal layer. Computes a per-cell
    /// desirability scalar in <c>[0,1]</c> from <see cref="SimulationSignal.LandValue"/>
    /// rollup plus a <see cref="SimulationSignal.ServiceParks"/> bonus minus a
    /// <see cref="SimulationSignal.PollutionAir"/> penalty, normalized by
    /// <see cref="NORMALIZATION_CAP"/> and clamped at the composer boundary per
    /// <c>ia/specs/simulation-signals.md</c> diffusion physics contract Example 3.
    /// FEAT-43 toggle wires this composer into <c>ZoneManager.AverageSectionDesirability</c>
    /// via <c>AutoZoningManager.useSignalDesirability</c>.
    /// </summary>
    public class DesirabilityComposer : MonoBehaviour, ISignalConsumer
    {
        [SerializeField] private GridManager gridManager;
        [SerializeField] private DistrictManager districtManager;

        // Composer formula constants — fixed per Stage 5 §Plan Digest.
        private const float PARKS_BONUS = 0.3f;
        private const float POLLUTION_PENALTY = 0.5f;
        private const float NORMALIZATION_CAP = 100f;

        private float[] _cellDesirability;
        private int _width;
        private int _height;

        private void Awake()
        {
            if (gridManager == null)
            {
                gridManager = FindObjectOfType<GridManager>();
            }
            if (districtManager == null)
            {
                districtManager = FindObjectOfType<DistrictManager>();
            }
        }

        private void Start()
        {
            _width = gridManager != null ? gridManager.width : 0;
            _height = gridManager != null ? gridManager.height : 0;
            _cellDesirability = new float[_width * _height];
        }

        /// <summary>Per-cell composer body. Reads <see cref="SimulationSignal.LandValue"/> + <see cref="SimulationSignal.ServiceParks"/> + <see cref="SimulationSignal.PollutionAir"/> from the registry; writes <c>Mathf.Clamp01((land + parks * PARKS_BONUS - air * POLLUTION_PENALTY) / NORMALIZATION_CAP)</c> per cell with NaN-guard.</summary>
        public void ConsumeSignals(SignalFieldRegistry registry, DistrictSignalCache cache)
        {
            if (registry == null || _cellDesirability == null)
            {
                return;
            }

            SignalField land = registry.GetField(SimulationSignal.LandValue);
            SignalField parks = registry.GetField(SimulationSignal.ServiceParks);
            SignalField air = registry.GetField(SimulationSignal.PollutionAir);

            if (land == null || parks == null || air == null)
            {
                return;
            }

            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    float landRaw = land.Get(x, y);
                    float parksRaw = parks.Get(x, y);
                    float airRaw = air.Get(x, y);

                    if (float.IsNaN(landRaw) || float.IsNaN(parksRaw) || float.IsNaN(airRaw))
                    {
                        _cellDesirability[y * _width + x] = 0f;
                        continue;
                    }

                    float raw = (landRaw + parksRaw * PARKS_BONUS - airRaw * POLLUTION_PENALTY) / NORMALIZATION_CAP;
                    if (float.IsNaN(raw) || float.IsInfinity(raw))
                    {
                        _cellDesirability[y * _width + x] = 0f;
                        continue;
                    }

                    _cellDesirability[y * _width + x] = Mathf.Clamp01(raw);
                }
            }
        }

        /// <summary>Read per-cell desirability scalar in <c>[0,1]</c>. Returns <c>0f</c> for out-of-bounds (x,y) or pre-Start access (no exception).</summary>
        public float CellValue(int x, int y)
        {
            if (_cellDesirability == null)
            {
                return 0f;
            }
            if (x < 0 || x >= _width || y < 0 || y >= _height)
            {
                return 0f;
            }
            return _cellDesirability[y * _width + x];
        }
    }
}
