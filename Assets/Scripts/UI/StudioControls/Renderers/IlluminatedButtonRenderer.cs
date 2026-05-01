using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Territory.UI.StudioControls.Renderers
{
    /// <summary>Render-layer companion for <see cref="IlluminatedButton"/>; lerps main <see cref="Image"/> alpha from <see cref="IlluminatedButton.IlluminationAlpha"/> on each <see cref="StudioControlBase.ApplyDetail"/> + halo flash coroutine on <see cref="IlluminatedButton.OnClick"/>.</summary>
    [RequireComponent(typeof(IlluminatedButton))]
    public class IlluminatedButtonRenderer : StudioControlRendererBase,
        IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        private const float DefaultHaloDurationSeconds = 0.25f;

        // Step 16.4 — hover/press state visuals (P8=a locked).
        private const float HaloIdleAlpha = 0f;
        private const float HaloHoverAlpha = 0.35f;
        private const float HaloPressAlpha = 0.6f;
        private const float BodyPressDimFactor = 0.7f;

        private IlluminatedButton _button;
        private Image _mainImage;
        private Image _haloImage;
        private Coroutine _haloCoroutine;
        private bool _isHover;
        private bool _isPressed;

        protected override void Awake()
        {
            base.Awake();
            _button = GetComponent<IlluminatedButton>();
            ResolveImages();
            if (_button != null)
            {
                _button.OnClick.AddListener(OnClicked);
            }
            ApplyHaloState();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (_button != null)
            {
                _button.OnClick.RemoveListener(OnClicked);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _isHover = true;
            ApplyHaloState();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHover = false;
            _isPressed = false;
            ApplyHaloState();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _isPressed = true;
            ApplyHaloState();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _isPressed = false;
            ApplyHaloState();
        }

        private void ApplyHaloState()
        {
            if (_haloCoroutine != null) return; // active click pulse owns halo until done
            if (_haloImage != null)
            {
                float target = _isPressed ? HaloPressAlpha : (_isHover ? HaloHoverAlpha : HaloIdleAlpha);
                var hc = _haloImage.color;
                hc.a = target;
                _haloImage.color = hc;
            }
            if (_mainImage != null && _button != null)
            {
                float baseAlpha = Mathf.Clamp01(_button.IlluminationAlpha);
                float bodyAlpha = _isPressed ? baseAlpha * BodyPressDimFactor : baseAlpha;
                var bc = _mainImage.color;
                bc.a = bodyAlpha;
                _mainImage.color = bc;
            }
        }

        private void ResolveImages()
        {
            var images = GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                var img = images[i];
                if (img == null) continue;
                // Skip Image attached to root GameObject (the StudioControl itself); we want children only.
                if (img.gameObject == gameObject) continue;
                if (img.gameObject.name == "halo")
                {
                    _haloImage = img;
                    continue;
                }
                if (_mainImage == null)
                {
                    _mainImage = img;
                }
            }
        }

        protected override void OnStateApplied()
        {
            if (_button == null || _mainImage == null) return;
            float baseAlpha = Mathf.Clamp01(_button.IlluminationAlpha);
            float bodyAlpha = _isPressed ? baseAlpha * BodyPressDimFactor : baseAlpha;
            var color = _mainImage.color;
            color.a = bodyAlpha;
            _mainImage.color = color;
        }

        private void OnClicked()
        {
            if (_haloImage == null) return;
            if (_haloCoroutine != null)
            {
                StopCoroutine(_haloCoroutine);
            }
            _haloCoroutine = StartCoroutine(HaloPulse());
        }

        private IEnumerator HaloPulse()
        {
            if (_haloImage == null) yield break;
            float duration = DefaultHaloDurationSeconds;
            float elapsed = 0f;
            while (elapsed < duration && _haloImage != null)
            {
                float t = elapsed / duration;
                var color = _haloImage.color;
                color.a = Mathf.Lerp(1f, 0f, t);
                _haloImage.color = color;
                elapsed += Time.deltaTime;
                yield return null;
            }
            _haloCoroutine = null;
            // Step 16.4 — restore hover/press steady state after click pulse ends.
            ApplyHaloState();
        }
    }
}
