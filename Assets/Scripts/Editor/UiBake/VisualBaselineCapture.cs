using System;
using System.IO;
using UnityEngine;

namespace Territory.Editor.UiBake
{
    /// <summary>
    /// Layer 5 visual diff harness (TECH-28374).
    /// Captures per-panel screenshots and computes SSIM tolerance vs stored baselines.
    /// Baselines live under Assets/Resources/UI/Generated/VisualBaselines/{panel}.png.
    /// </summary>
    public static class VisualBaselineCapture
    {
        private const string BaselineDir = "Assets/Resources/UI/Generated/VisualBaselines";
        private const float DefaultTolerance = 0.95f;

        /// <summary>Compute SSIM (simplified luma-channel) between two textures. Returns [0,1].</summary>
        public static float ComputeSSIM(Texture2D a, Texture2D b)
        {
            if (a == null) throw new ArgumentNullException(nameof(a));
            if (b == null) throw new ArgumentNullException(nameof(b));

            int w = Mathf.Min(a.width,  b.width);
            int h = Mathf.Min(a.height, b.height);

            if (w == 0 || h == 0) return 0f;

            double sumLumaA = 0, sumLumaB = 0;
            var pixA = a.GetPixels(0, 0, w, h);
            var pixB = b.GetPixels(0, 0, w, h);
            int n = pixA.Length;

            for (int i = 0; i < n; i++)
            {
                sumLumaA += Luma(pixA[i]);
                sumLumaB += Luma(pixB[i]);
            }
            double meanA = sumLumaA / n;
            double meanB = sumLumaB / n;

            double varA = 0, varB = 0, covar = 0;
            for (int i = 0; i < n; i++)
            {
                double da = Luma(pixA[i]) - meanA;
                double db = Luma(pixB[i]) - meanB;
                varA  += da * da;
                varB  += db * db;
                covar += da * db;
            }
            varA  /= n;
            varB  /= n;
            covar /= n;

            // SSIM constants (standard k1/k2 with L=1 luma range).
            const double c1 = 0.0001, c2 = 0.0009;
            double ssim = ((2 * meanA * meanB + c1) * (2 * covar + c2))
                        / ((meanA * meanA + meanB * meanB + c1) * (varA + varB + c2));

            return (float)Math.Max(0.0, Math.Min(1.0, ssim));
        }

        /// <summary>Assert captured vs baseline SSIM >= tolerance. Throws on fail.</summary>
        public static void AssertSSIM(Texture2D captured, Texture2D baseline,
            float tolerance = DefaultTolerance, string panelSlug = "")
        {
            if (captured == null) throw new ArgumentNullException(nameof(captured));
            if (baseline == null) throw new ArgumentNullException(nameof(baseline));

            float score = ComputeSSIM(captured, baseline);
            if (score < tolerance)
                throw new Exception(
                    $"Visual diff FAIL for '{panelSlug}': SSIM={score:F4} < tolerance={tolerance:F4}");
        }

        /// <summary>
        /// Load a PNG baseline for <paramref name="panelSlug"/> from Resources.
        /// Returns null when not found (first-author path: baseline must be committed).
        /// </summary>
        public static Texture2D LoadBaseline(string panelSlug)
        {
            if (string.IsNullOrEmpty(panelSlug)) throw new ArgumentNullException(nameof(panelSlug));
            // Resources.Load strips Assets/Resources prefix; texture lives at sub-path.
            return Resources.Load<Texture2D>($"UI/Generated/VisualBaselines/{panelSlug}");
        }

        /// <summary>
        /// Save <paramref name="texture"/> as PNG baseline for <paramref name="panelSlug"/>.
        /// Editor-only — call from snapshot tool or test setup.
        /// </summary>
        public static void SaveBaseline(string panelSlug, Texture2D texture)
        {
            if (string.IsNullOrEmpty(panelSlug)) throw new ArgumentNullException(nameof(panelSlug));
            if (texture == null) throw new ArgumentNullException(nameof(texture));

            Directory.CreateDirectory(BaselineDir);
            string path  = $"{BaselineDir}/{panelSlug}.png";
            byte[] bytes = texture.EncodeToPNG();
            if (bytes == null || bytes.Length == 0)
                throw new Exception($"EncodeToPNG returned empty for panel '{panelSlug}'.");
            File.WriteAllBytes(path, bytes);
        }

        /// <summary>
        /// Inject a 1-pixel jitter into a copy of <paramref name="source"/> for test purposes.
        /// Returns a new Texture2D with pixel (0,0) flipped.
        /// </summary>
        public static Texture2D InjectOnePixelJitter(Texture2D source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            var copy = new Texture2D(source.width, source.height, source.format, false);
            copy.SetPixels(source.GetPixels());
            // Flip a single pixel by a tiny amount — within SSIM tolerance of 0.95 for typical sizes.
            if (source.width > 0 && source.height > 0)
            {
                var px = copy.GetPixel(0, 0);
                copy.SetPixel(0, 0, new Color(
                    Mathf.Clamp01(px.r + 0.01f),
                    px.g, px.b, px.a));
            }
            copy.Apply();
            return copy;
        }

        /// <summary>
        /// Inject a large-scale distortion (every pixel flipped) — forces SSIM below tolerance.
        /// Used by deliberate-fail companion test.
        /// </summary>
        public static Texture2D InjectLargeDistortion(Texture2D source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            var copy = new Texture2D(source.width, source.height, source.format, false);
            var pixels = source.GetPixels();
            for (int i = 0; i < pixels.Length; i++)
            {
                var p = pixels[i];
                pixels[i] = new Color(1f - p.r, 1f - p.g, 1f - p.b, p.a);
            }
            copy.SetPixels(pixels);
            copy.Apply();
            return copy;
        }

        // ── Helpers ──────────────────────────────────────────────────────────────

        private static double Luma(Color c)
            => 0.2126 * c.r + 0.7152 * c.g + 0.0722 * c.b;
    }
}
