using Territory.UI.StudioControls;
using UnityEngine;

namespace Territory.UI.Juice
{
    /// <summary>
    /// Ring-buffer sweep driver for <see cref="Oscilloscope"/>. Pushes one sample per
    /// <c>1 / sweepRateHz</c> seconds (Time.deltaTime accumulator); buffer is fixed length
    /// (256 samples) for MVP. Sibling control + theme cached in <c>Awake</c> per invariant #3.
    /// </summary>
    [RequireComponent(typeof(Oscilloscope))]
    public class OscilloscopeSweep : JuiceBase
    {
        /// <summary>Fixed ring-buffer length for MVP (T5.2 §Pending Decisions).</summary>
        public const int BufferLength = 256;

        [SerializeField] private bool useSyntheticSamples = true;
        [SerializeField] private float syntheticFrequencyHz = 1f;

        private Oscilloscope _scope;
        private float[] _buffer;
        private int _writeCursor;
        private int _samplesPushed;
        private float _accumulator;
        private float _sweepRateHz;

        /// <summary>Read-only sample buffer view (used by render layer + tests).</summary>
        public float[] Buffer => _buffer;

        /// <summary>Read-only ring-buffer write cursor (latest sample index + 1 mod length).</summary>
        public int WriteCursor => _writeCursor;

        /// <summary>Cumulative samples pushed since component enable; used by smoke tests.</summary>
        public int SamplesPushed => _samplesPushed;

        /// <summary>Optional injected sample source override (T5.6 fixture entry point).</summary>
        public System.Func<float, float> SampleSourceOverride { get; set; }

        protected override void Awake()
        {
            base.Awake();
            _scope = GetComponent<Oscilloscope>();
            _buffer = new float[BufferLength];
            _sweepRateHz = _scope != null ? _scope.SweepRateHz : 0f;
            if (_sweepRateHz <= 0f) _sweepRateHz = 60f;
        }

        private void Update()
        {
            if (_buffer == null) return;
            float interval = 1f / _sweepRateHz;
            _accumulator += Time.deltaTime;
            while (_accumulator >= interval)
            {
                _accumulator -= interval;
                PushSample();
            }
        }

        private void PushSample()
        {
            float sample;
            if (SampleSourceOverride != null)
            {
                sample = SampleSourceOverride(Time.time);
            }
            else if (useSyntheticSamples)
            {
                sample = Mathf.Sin(Time.time * Mathf.PI * 2f * syntheticFrequencyHz);
            }
            else
            {
                sample = 0f;
            }

            _buffer[_writeCursor] = sample;
            _writeCursor = (_writeCursor + 1) % BufferLength;
            _samplesPushed++;
        }

        /// <summary>Test hook — clear buffer + counters.</summary>
        public void ResetBuffer()
        {
            if (_buffer != null) System.Array.Clear(_buffer, 0, _buffer.Length);
            _writeCursor = 0;
            _samplesPushed = 0;
            _accumulator = 0f;
        }
    }
}
