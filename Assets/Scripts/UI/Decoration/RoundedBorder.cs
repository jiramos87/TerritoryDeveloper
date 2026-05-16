using UnityEngine;
using UnityEngine.UI;

namespace Territory.UI.Decoration
{
    /// <summary>
    /// Stats-panel pilot — rounded rectangular fill + border drawn via OnPopulateMesh.
    /// Two modes (independent): fill (interior color, _fillEnabled) and border (outline,
    /// _borderWidth > 0). Used by UiBakeHandler when panel_detail.padding_json includes
    /// border_width / corner_radius. No sprite asset required.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public class RoundedBorder : MaskableGraphic
    {
        private const float QuadrantSweepDeg = 90f;
        private const float QuadrantStartTR = 0f;
        private const float QuadrantStartTL = 90f;
        private const float QuadrantStartBL = 180f;
        private const float QuadrantStartBR = 270f;

        [SerializeField] private float _cornerRadius = 4f;
        [SerializeField] private float _borderWidth = 2f;
        [SerializeField] private Color _borderColor = Color.white;
        [SerializeField] private int _cornerSegments = 8;
        [SerializeField] private bool _fillEnabled = false;
        [SerializeField] private Color _fillColor = new Color(0.196f, 0.196f, 0.196f, 1f);

        public float CornerRadius
        {
            get => _cornerRadius;
            set { _cornerRadius = Mathf.Max(0f, value); SetVerticesDirty(); }
        }

        public float BorderWidth
        {
            get => _borderWidth;
            set { _borderWidth = Mathf.Max(0f, value); SetVerticesDirty(); }
        }

        public Color BorderColor
        {
            get => _borderColor;
            set { _borderColor = value; SetVerticesDirty(); }
        }

        public int CornerSegments
        {
            get => _cornerSegments;
            set { _cornerSegments = Mathf.Max(1, value); SetVerticesDirty(); }
        }

        public bool FillEnabled
        {
            get => _fillEnabled;
            set { _fillEnabled = value; SetVerticesDirty(); }
        }

        public Color FillColor
        {
            get => _fillColor;
            set { _fillColor = value; SetVerticesDirty(); }
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var rect = GetPixelAdjustedRect();
            float w = rect.width;
            float h = rect.height;
            if (w <= 0f || h <= 0f) return;

            float r = Mathf.Min(_cornerRadius, w * 0.5f, h * 0.5f);
            int seg = Mathf.Max(1, _cornerSegments);

            float xMin = rect.xMin;
            float yMin = rect.yMin;
            float xMax = rect.xMax;
            float yMax = rect.yMax;

            Vector2 cTR = new Vector2(xMax - r, yMax - r);
            Vector2 cTL = new Vector2(xMin + r, yMax - r);
            Vector2 cBL = new Vector2(xMin + r, yMin + r);
            Vector2 cBR = new Vector2(xMax - r, yMin + r);

            // Outer rounded perimeter — 4 quadrants × (seg+1) points.
            int ringCount = (seg + 1) * 4;
            var outerPts = new Vector2[ringCount];
            BuildArc(outerPts, 0, seg, cTR, r, QuadrantStartTR);
            BuildArc(outerPts, 1, seg, cTL, r, QuadrantStartTL);
            BuildArc(outerPts, 2, seg, cBL, r, QuadrantStartBL);
            BuildArc(outerPts, 3, seg, cBR, r, QuadrantStartBR);

            // ── Fill pass (triangle fan from center) ────────────────────
            if (_fillEnabled && _fillColor.a > 0f)
            {
                int fanCenter = vh.currentVertCount;
                vh.AddVert(new Vector2(rect.center.x, rect.center.y), _fillColor, Vector2.zero);
                for (int i = 0; i < ringCount; i++)
                {
                    vh.AddVert(outerPts[i], _fillColor, Vector2.zero);
                }
                for (int i = 0; i < ringCount; i++)
                {
                    int a = fanCenter + 1 + i;
                    int b = fanCenter + 1 + ((i + 1) % ringCount);
                    vh.AddTriangle(fanCenter, a, b);
                }
            }

            // ── Border pass (ring of triangle strips) ───────────────────
            if (_borderWidth > 0f)
            {
                float bw = Mathf.Min(_borderWidth, w * 0.5f, h * 0.5f);
                float rInner = Mathf.Max(0f, r - bw);
                var innerPts = new Vector2[ringCount];
                BuildArc(innerPts, 0, seg, cTR, rInner, QuadrantStartTR);
                BuildArc(innerPts, 1, seg, cTL, rInner, QuadrantStartTL);
                BuildArc(innerPts, 2, seg, cBL, rInner, QuadrantStartBL);
                BuildArc(innerPts, 3, seg, cBR, rInner, QuadrantStartBR);

                // For border the inner ring radius must shrink AND the inner perimeter must follow
                // the inset rect (xMin+bw .. xMax-bw). When rInner == 0 (border thicker than radius)
                // the inner perimeter degenerates to the inset rect corners — use straight projections
                // instead of arcs to avoid all 4 quadrants collapsing onto each other.
                if (rInner <= 0f)
                {
                    float ixMin = xMin + bw;
                    float ixMax = xMax - bw;
                    float iyMin = yMin + bw;
                    float iyMax = yMax - bw;
                    Vector2 iTR = new Vector2(ixMax, iyMax);
                    Vector2 iTL = new Vector2(ixMin, iyMax);
                    Vector2 iBL = new Vector2(ixMin, iyMin);
                    Vector2 iBR = new Vector2(ixMax, iyMin);
                    for (int s = 0; s <= seg; s++)
                    {
                        innerPts[s] = iTR;
                        innerPts[(seg + 1) + s] = iTL;
                        innerPts[(seg + 1) * 2 + s] = iBL;
                        innerPts[(seg + 1) * 3 + s] = iBR;
                    }
                }

                int baseIdx = vh.currentVertCount;
                for (int i = 0; i < ringCount; i++)
                {
                    vh.AddVert(outerPts[i], _borderColor, Vector2.zero);
                    vh.AddVert(innerPts[i], _borderColor, Vector2.zero);
                }
                for (int i = 0; i < ringCount; i++)
                {
                    int next = (i + 1) % ringCount;
                    int o0 = baseIdx + i * 2;
                    int i0 = baseIdx + i * 2 + 1;
                    int o1 = baseIdx + next * 2;
                    int i1 = baseIdx + next * 2 + 1;
                    vh.AddTriangle(o0, o1, i1);
                    vh.AddTriangle(o0, i1, i0);
                }
            }
        }

        private static void BuildArc(Vector2[] outBuf, int quadrantIdx, int seg, Vector2 center, float radius, float startDeg)
        {
            int basePos = quadrantIdx * (seg + 1);
            for (int s = 0; s <= seg; s++)
            {
                float t = (float)s / seg;
                float deg = startDeg + t * QuadrantSweepDeg;
                float rad = deg * Mathf.Deg2Rad;
                outBuf[basePos + s] = new Vector2(center.x + Mathf.Cos(rad) * radius, center.y + Mathf.Sin(rad) * radius);
            }
        }
    }
}
