using System.Collections;
using UnityEngine;

namespace Territory.Services
{
    /// <summary>Interface for test injection.</summary>
    public interface ICrossfadeTriggerEvaluator
    {
        bool ShouldFireRegionFade(float cameraOrthoSize, Rect cityFootprintWorld, Rect regionAnchorWorld);
        void ResetForNewTransition();
    }

    /// <summary>CoreScene service — geometric crossfade trigger. Fires region CanvasGroup alpha 0→1 once per transition when city footprint world-rect ⊆ 2×2 region anchor world-rect. Invariant #3: cache refs in Awake.</summary>
    public class CrossfadeTriggerEvaluator : MonoBehaviour, ICrossfadeTriggerEvaluator
    {
        // ── Inspector ────────────────────────────────────────────────────────
        [SerializeField] private CanvasGroup regionLayerCanvasGroup;

        /// <summary>World-space size of the anchor rect used for crossfade trigger check (2×2 default).</summary>
        [SerializeField] private float anchorWorldSize = 2f;

        /// <summary>Alpha fade duration in seconds.</summary>
        [SerializeField] private float fadeDuration = 0.5f;

        // ── State ────────────────────────────────────────────────────────────
        private bool _hasFiredRegionFade;

        // ── ICrossfadeTriggerEvaluator ────────────────────────────────────────

        /// <summary>Returns true once when city footprint world-rect ⊆ anchor world-rect. Latches after first true.</summary>
        public bool ShouldFireRegionFade(float cameraOrthoSize, Rect cityFootprintWorld, Rect regionAnchorWorld)
        {
            if (_hasFiredRegionFade) return false;
            return regionAnchorWorld.Contains(new Vector2(cityFootprintWorld.xMin, cityFootprintWorld.yMin))
                && regionAnchorWorld.Contains(new Vector2(cityFootprintWorld.xMax, cityFootprintWorld.yMax));
        }

        /// <summary>Reset latch for a new transition. Called by ZoomTransitionController at TweeningOut entry.</summary>
        public void ResetForNewTransition()
        {
            _hasFiredRegionFade = false;
            if (regionLayerCanvasGroup != null)
                regionLayerCanvasGroup.alpha = 0f;
        }

        // ── Frame evaluation (called by ZoomTransitionController each frame) ──

        /// <summary>Per-frame call during TweeningOut. Derives city footprint from camera; fires region fade when trigger met.</summary>
        public void EvaluateFrame(Camera cam)
        {
            if (_hasFiredRegionFade || cam == null) return;

            float ortho = cam.orthographicSize;
            float aspect = cam.aspect;
            float halfW = ortho * aspect;

            Vector3 camPos = cam.transform.position;
            Rect cityFootprint = new Rect(camPos.x - halfW, camPos.y - ortho, halfW * 2f, ortho * 2f);

            float half = anchorWorldSize * 0.5f;
            Rect anchorRect = new Rect(camPos.x - half, camPos.y - half, anchorWorldSize, anchorWorldSize);

            if (ShouldFireRegionFade(ortho, cityFootprint, anchorRect))
            {
                FireRegionFade();
            }
        }

        // ── Private ──────────────────────────────────────────────────────────

        private void FireRegionFade()
        {
            _hasFiredRegionFade = true;
            if (regionLayerCanvasGroup != null)
                StartCoroutine(FadeAlphaCoroutine(regionLayerCanvasGroup, 1f, fadeDuration));
        }

        private IEnumerator FadeAlphaCoroutine(CanvasGroup group, float endValue, float duration)
        {
            float start   = group.alpha;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed     += Time.deltaTime;
                group.alpha  = Mathf.Lerp(start, endValue, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }
            group.alpha = endValue;
        }
    }
}
