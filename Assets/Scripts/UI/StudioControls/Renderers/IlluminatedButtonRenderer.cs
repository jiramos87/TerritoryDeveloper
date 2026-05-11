using System.Collections;
using Territory.Audio;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Territory.UI.StudioControls.Renderers
{
    /// <summary>Render-layer companion for <see cref="IlluminatedButton"/>; lerps main <see cref="Image"/> alpha from <see cref="IlluminatedButton.IlluminationAlpha"/> on each <see cref="StudioControlBase.ApplyDetail"/> + halo flash coroutine on <see cref="IlluminatedButton.OnClick"/>.</summary>
    [RequireComponent(typeof(IlluminatedButton))]
    public class IlluminatedButtonRenderer : StudioControlRendererBase,
        IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        private const float DefaultHaloDurationSeconds = 0.25f;

        // Step 16.4 — hover/press state visuals (P8=a locked).
        private const float HaloIdleAlpha = 0f;
        private const float HaloHoverAlpha = 0.35f;
        private const float HaloPressAlpha = 0.6f;
        private const float BodyPressDimFactor = 0.7f;

        private IlluminatedButton _button;
        // Step 16 D3.1 — bake handler writes these refs at authoring time so hover/press wiring
        // is deterministic (no runtime GetComponentsInChildren scan + name-match coupling).
        // ResolveImages() stays as a fallback for legacy prefabs that pre-date the bake-time wire.
        [SerializeField] private Image _mainImage;
        [SerializeField] private Image _haloImage;
        private Coroutine _haloCoroutine;
        private bool _isHover;
        private bool _isPressed;

        protected override void Awake()
        {
            base.Awake();
            _button = GetComponent<IlluminatedButton>();
            // Step 16 D3.1 — only fall back to runtime image discovery when bake-time refs are absent.
            if (_mainImage == null || _haloImage == null) ResolveImages();
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

        // Stage 13 hotfix — pause-menu Resume "yellow stays" bug.
        // OnClicked → HaloPulse coroutine fades halo 1→0 over 0.25s, then on click
        // the panel SetActive(false) suspends the coroutine mid-lerp leaving halo
        // alpha=1 (yellow). On reopen the coroutine resumes but the panel paints
        // yellow until it completes. Force-reset on disable.
        private void OnDisable()
        {
            if (_haloCoroutine != null)
            {
                StopCoroutine(_haloCoroutine);
                _haloCoroutine = null;
            }
            _isHover = false;
            _isPressed = false;
            ApplyHaloState();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _isHover = true;
            ApplyHaloState();
            // Parity with ThemedButton hover blip — main-menu buttons (illuminated-button bake
            // path) also need UiButtonHover. Without this, the panel renders correct visuals
            // but ships silent.
            BlipEngine.Play(BlipId.UiButtonHover);
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

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_button == null) return;
            if (eventData != null && eventData.button != PointerEventData.InputButton.Left) return;
            _button.OnClick.Invoke();
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
            // Parity with ThemedButton click blip — fires on every confirmed left-click via
            // _button.OnClick.Invoke (driven from OnPointerClick).
            BlipEngine.Play(BlipId.UiButtonClick);
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
                // Stage 13 hotfix — use unscaled delta so modal pause (timeScale=0)
                // doesn't deadlock the pulse coroutine, leaving halo stuck at full alpha.
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            _haloCoroutine = null;
            // Step 16.4 — restore hover/press steady state after click pulse ends.
            ApplyHaloState();
        }
    }
}
