using System.Collections;
using Territory.UI.Themed;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Territory.UI.Juice
{
    /// <summary>
    /// Drop-shadow surface decorator for <see cref="ThemedPanel"/>. Idempotent <c>Awake</c>
    /// instantiates a child <see cref="Image"/> named <c>ShadowDepth_Shadow</c> once;
    /// pointer-enter / pointer-exit drive an offset + alpha tween via the configured
    /// <c>motion_curve</c>. Re-baking the host prefab does NOT duplicate the child.
    /// </summary>
    [RequireComponent(typeof(ThemedPanel))]
    public class ShadowDepth : JuiceBase, IPointerEnterHandler, IPointerExitHandler
    {
        /// <summary>Child shadow object name; idempotency anchor for re-bake.</summary>
        public const string ShadowChildName = "ShadowDepth_Shadow";

        [SerializeField] private Vector2 restOffset = new Vector2(0f, -2f);
        [SerializeField] private Vector2 hoverOffset = new Vector2(0f, -8f);
        [SerializeField, Range(0f, 1f)] private float restAlpha = 0.25f;
        [SerializeField, Range(0f, 1f)] private float hoverAlpha = 0.55f;
        [SerializeField] private string shadowPaletteSlug = "shadow";
        [SerializeField] private float fallbackDurationMs = 180f;

        private Image _shadow;
        private RectTransform _shadowRect;
        private Coroutine _activeTween;

        /// <summary>Read-only child shadow image (used by smoke tests).</summary>
        public Image Shadow => _shadow;

        protected override void Awake()
        {
            base.Awake();
            EnsureShadowChild();
            ApplyShadowColor();
            if (_shadowRect != null) _shadowRect.anchoredPosition = restOffset;
            if (_shadow != null) _shadow.color = WithAlpha(_shadow.color, restAlpha);
        }

        private void EnsureShadowChild()
        {
            var existing = transform.Find(ShadowChildName);
            if (existing != null)
            {
                _shadow = existing.GetComponent<Image>();
                _shadowRect = existing as RectTransform;
                return;
            }

            var go = new GameObject(ShadowChildName, typeof(RectTransform), typeof(Image));
            _shadowRect = go.GetComponent<RectTransform>();
            _shadow = go.GetComponent<Image>();
            _shadowRect.SetParent(transform, false);
            _shadowRect.SetAsFirstSibling();
            _shadowRect.anchorMin = Vector2.zero;
            _shadowRect.anchorMax = Vector2.one;
            _shadowRect.offsetMin = Vector2.zero;
            _shadowRect.offsetMax = Vector2.zero;
        }

        private void ApplyShadowColor()
        {
            if (_shadow == null) return;
            // Default neutral shadow tint; palette dict lookup deferred to Stage 6 render layer when
            // the palette → Color resolver is wired. Stage 5 sources alpha + offset only.
            _shadow.color = new Color(0f, 0f, 0f, restAlpha);
        }

        /// <inheritdoc />
        public void OnPointerEnter(PointerEventData eventData)
        {
            StartTween(hoverOffset, hoverAlpha);
        }

        /// <inheritdoc />
        public void OnPointerExit(PointerEventData eventData)
        {
            StartTween(restOffset, restAlpha);
        }

        private void StartTween(Vector2 targetOffset, float targetAlpha)
        {
            if (_shadow == null) return;
            if (_activeTween != null) StopCoroutine(_activeTween);
            _activeTween = StartCoroutine(TweenRoutine(targetOffset, targetAlpha));
        }

        private IEnumerator TweenRoutine(Vector2 targetOffset, float targetAlpha)
        {
            float durationMs = fallbackDurationMs;
            if (TryEvaluateCurve(out var spec) && spec.durationMs > 0f)
            {
                durationMs = spec.durationMs;
            }
            float duration = Mathf.Max(0.001f, durationMs / 1000f);
            float elapsed = 0f;
            Vector2 startOffset = _shadowRect != null ? _shadowRect.anchoredPosition : Vector2.zero;
            float startAlpha = _shadow.color.a;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                if (_shadowRect != null)
                {
                    _shadowRect.anchoredPosition = Vector2.Lerp(startOffset, targetOffset, t);
                }
                _shadow.color = WithAlpha(_shadow.color, Mathf.Lerp(startAlpha, targetAlpha, t));
                yield return null;
            }
            _shadow.color = WithAlpha(_shadow.color, targetAlpha);
            _activeTween = null;
        }

        private static Color WithAlpha(Color c, float a)
        {
            c.a = Mathf.Clamp01(a);
            return c;
        }
    }
}
