using System;
using Territory.Core;
using UnityEngine;

namespace Territory.Simulation.Signals
{
    /// <summary>Per-scene owner of one <see cref="SignalField"/> per <see cref="SimulationSignal"/>. Allocated in <c>Awake</c> from <see cref="GridManager"/> dims; reallocated via <see cref="ResizeForMap"/> on map reload. No per-frame work.</summary>
    public class SignalFieldRegistry : MonoBehaviour
    {
        [SerializeField] private GridManager grid;

        private SignalField[] fields;

        private void Awake()
        {
            if (grid == null)
            {
                grid = FindObjectOfType<GridManager>();
            }

            int signalCount = Enum.GetValues(typeof(SimulationSignal)).Length;
            fields = new SignalField[signalCount];

            int width = grid != null ? grid.width : 0;
            int height = grid != null ? grid.height : 0;

            for (int i = 0; i < signalCount; i++)
            {
                fields[i] = new SignalField(width, height);
            }
        }

        /// <summary>O(1) accessor; <paramref name="signal"/> ordinal indexes the underlying array.</summary>
        public SignalField GetField(SimulationSignal signal)
        {
            return fields[(int)signal];
        }

        /// <summary>Reallocate all fields at the new grid dims; signal data is NOT preserved (locked decision — recompute via warmup pass).</summary>
        public void ResizeForMap(int width, int height)
        {
            int signalCount = fields != null ? fields.Length : Enum.GetValues(typeof(SimulationSignal)).Length;
            fields = new SignalField[signalCount];
            for (int i = 0; i < signalCount; i++)
            {
                fields[i] = new SignalField(width, height);
            }
        }
    }
}
