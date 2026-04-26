using System;

namespace Territory.Simulation.Signals
{
    /// <summary>Per-signal float grid with clamp-floor-0 invariant on every write. Backing store sized at construction.</summary>
    public class SignalField
    {
        private readonly float[,] values;

        public int Width { get; }
        public int Height { get; }

        public SignalField(int width, int height)
        {
            if (width < 0 || height < 0)
            {
                throw new ArgumentOutOfRangeException("width/height", "SignalField dims must be non-negative.");
            }
            Width = width;
            Height = height;
            values = new float[width, height];
        }

        /// <summary>Read cell value. Always >= 0 (boundary clamp).</summary>
        public float Get(int x, int y)
        {
            return values[x, y];
        }

        /// <summary>Overwrite cell value, clamped floor-0.</summary>
        public void Set(int x, int y, float v)
        {
            values[x, y] = v < 0f ? 0f : v;
        }

        /// <summary>Add to cell value, clamped floor-0 after sum (negative sources may cancel positives but stored value never goes negative).</summary>
        public void Add(int x, int y, float v)
        {
            float next = values[x, y] + v;
            values[x, y] = next < 0f ? 0f : next;
        }

        /// <summary>Return an independent <c>float[,]</c> copy of the backing store; mutating the copy never affects this field.</summary>
        public float[,] Snapshot()
        {
            float[,] copy = new float[Width, Height];
            Array.Copy(values, copy, values.Length);
            return copy;
        }
    }
}
