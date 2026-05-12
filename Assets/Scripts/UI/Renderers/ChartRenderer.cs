using System;
using Territory.UI.Registry;
using UnityEngine;
using UnityEngine.UI;

namespace Territory.UI.Renderers
{
    /// <summary>
    /// Stage 10 stats/budget panel — chart-stub companion. Owns a 256×96 RGBA32 Texture2D
    /// drawn on every <see cref="UiBindRegistry.Subscribe{T}"/> push of a float[] series.
    /// Two modes: <see cref="ChartMode.Line"/> draws a polyline, <see cref="ChartMode.StackedBar"/>
    /// draws vertical bars whose height encodes value magnitude.
    /// </summary>
    [RequireComponent(typeof(RawImage))]
    public class ChartRenderer : MonoBehaviour
    {
        public enum ChartMode { Line, StackedBar }

        private const int TextureWidth = 256;
        private const int TextureHeight = 96;

        [SerializeField] private UiBindRegistry _bindRegistry;
        [SerializeField] private string _bindId;
        [SerializeField] private ChartMode _mode = ChartMode.Line;
        [SerializeField] private Color _lineColor = new Color(0.29f, 0.62f, 1f, 1f);
        [SerializeField] private Color _axisColor = new Color(0.3f, 0.3f, 0.3f, 1f);

        private RawImage _rawImage;
        private Texture2D _texture;
        private IDisposable _sub;
        private readonly Color[] _clearBuffer = new Color[TextureWidth * TextureHeight];

        private void OnEnable()
        {
            if (_rawImage == null) _rawImage = GetComponent<RawImage>();
            if (_texture == null)
            {
                _texture = new Texture2D(TextureWidth, TextureHeight, TextureFormat.RGBA32, mipChain: false);
                _texture.filterMode = FilterMode.Bilinear;
                _texture.wrapMode = TextureWrapMode.Clamp;
                _rawImage.texture = _texture;
            }

            if (_bindRegistry == null) _bindRegistry = FindObjectOfType<UiBindRegistry>();
            if (_bindRegistry == null || string.IsNullOrEmpty(_bindId))
            {
                Debug.LogWarning($"[ChartRenderer][LOG] OnEnable on {gameObject.name} — SKIPPED bindRegistry={(_bindRegistry != null ? "OK" : "NULL")} bindId='{_bindId}'");
                return;
            }

            _sub = _bindRegistry.Subscribe<float[]>(_bindId, OnSeriesChanged);
            Debug.Log($"[ChartRenderer][LOG] OnEnable on {gameObject.name} — subscribed bindId='{_bindId}' mode={_mode}");
        }

        private void OnDisable()
        {
            _sub?.Dispose();
            _sub = null;
        }

        private void OnDestroy()
        {
            if (_texture != null)
            {
                Destroy(_texture);
                _texture = null;
            }
        }

        private void OnSeriesChanged(float[] series)
        {
            Debug.Log($"[ChartRenderer][LOG] OnSeriesChanged on {gameObject.name} bindId='{_bindId}' — series.Length={(series != null ? series.Length : -1)} first={(series != null && series.Length > 0 ? series[0].ToString("F2") : "-")}");
            if (_texture == null) return;

            for (int i = 0; i < _clearBuffer.Length; i++) _clearBuffer[i] = Color.clear;
            _texture.SetPixels(_clearBuffer);

            // Axis baseline.
            for (int x = 0; x < TextureWidth; x++) _texture.SetPixel(x, 0, _axisColor);

            if (series != null && series.Length > 0)
            {
                float min = float.MaxValue;
                float max = float.MinValue;
                for (int i = 0; i < series.Length; i++)
                {
                    if (series[i] < min) min = series[i];
                    if (series[i] > max) max = series[i];
                }
                if (Mathf.Approximately(min, max)) max = min + 1f;
                float range = max - min;

                if (_mode == ChartMode.Line)
                {
                    int prevX = 0;
                    int prevY = SampleY(series[0], min, range);
                    for (int i = 1; i < series.Length; i++)
                    {
                        int x = Mathf.RoundToInt((float)i / (series.Length - 1) * (TextureWidth - 1));
                        int y = SampleY(series[i], min, range);
                        DrawLine(prevX, prevY, x, y, _lineColor);
                        prevX = x;
                        prevY = y;
                    }
                }
                else
                {
                    int barCount = series.Length;
                    int barWidth = Mathf.Max(1, TextureWidth / barCount);
                    for (int i = 0; i < barCount; i++)
                    {
                        int barX = i * barWidth;
                        int barH = SampleY(series[i], min, range);
                        for (int x = barX; x < Mathf.Min(barX + barWidth - 1, TextureWidth); x++)
                            for (int y = 1; y <= barH; y++)
                                _texture.SetPixel(x, y, _lineColor);
                    }
                }
            }

            _texture.Apply(updateMipmaps: false);
        }

        private static int SampleY(float v, float min, float range)
        {
            float normalized = (v - min) / range;
            return Mathf.Clamp(Mathf.RoundToInt(normalized * (TextureHeight - 4)) + 2, 1, TextureHeight - 1);
        }

        private void DrawLine(int x0, int y0, int x1, int y1, Color color)
        {
            int dx = Mathf.Abs(x1 - x0);
            int dy = Mathf.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;
            while (true)
            {
                if (x0 >= 0 && x0 < TextureWidth && y0 >= 0 && y0 < TextureHeight)
                    _texture.SetPixel(x0, y0, color);
                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * err;
                if (e2 > -dy) { err -= dy; x0 += sx; }
                if (e2 < dx)  { err += dx; y0 += sy; }
            }
        }
    }
}
