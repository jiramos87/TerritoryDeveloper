using System.Collections;
using UnityEngine;

namespace Territory.Utilities
{
    /// <summary>
    /// Shared <see cref="CanvasGroup"/> helpers for popup fade-in/out. Unscaled time → pause-safe UI.
    /// </summary>
    public static class UiCanvasGroupUtility
    {
        /// <summary>
        /// Ensure <see cref="CanvasGroup"/> on <paramref name="root"/> for alpha-driven show/hide.
        /// </summary>
        public static CanvasGroup EnsureCanvasGroup(GameObject root)
        {
            if (root == null)
                return null;
            CanvasGroup cg = root.GetComponent<CanvasGroup>();
            if (cg == null)
                cg = root.AddComponent<CanvasGroup>();
            return cg;
        }

        /// <summary>
        /// Lerp <see cref="CanvasGroup.alpha"/> <paramref name="from"/> → <paramref name="to"/> via <see cref="Time.unscaledDeltaTime"/>.
        /// </summary>
        public static IEnumerator FadeUnscaled(CanvasGroup cg, float from, float to, float duration)
        {
            if (cg == null)
                yield break;
            duration = Mathf.Max(0.0001f, duration);
            cg.alpha = from;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / duration);
                cg.alpha = Mathf.Lerp(from, to, u);
                yield return null;
            }

            cg.alpha = to;
        }
    }
}
