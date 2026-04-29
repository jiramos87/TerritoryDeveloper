using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Territory.UI.StudioControls.Renderers
{
    /// <summary>Render-layer companion for <see cref="IlluminatedButton"/>; lerps main <see cref="Image"/> alpha from <see cref="IlluminatedButton.IlluminationAlpha"/> on each <see cref="StudioControlBase.ApplyDetail"/> + halo flash coroutine on <see cref="IlluminatedButton.OnClick"/>.</summary>
    [RequireComponent(typeof(IlluminatedButton))]
    public class IlluminatedButtonRenderer : StudioControlRendererBase
    {
        private const float DefaultHaloDurationSeconds = 0.25f;

        private IlluminatedButton _button;
        private Image _mainImage;
        private Image _haloImage;
        private Coroutine _haloCoroutine;

        protected override void Awake()
        {
            base.Awake();
            _button = GetComponent<IlluminatedButton>();
            ResolveImages();
            if (_button != null)
            {
                _button.OnClick.AddListener(OnClicked);
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (_button != null)
            {
                _button.OnClick.RemoveListener(OnClicked);
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
            var color = _mainImage.color;
            color.a = Mathf.Clamp01(_button.IlluminationAlpha);
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
            if (_haloImage != null)
            {
                var color = _haloImage.color;
                color.a = 0f;
                _haloImage.color = color;
            }
            _haloCoroutine = null;
        }
    }
}
