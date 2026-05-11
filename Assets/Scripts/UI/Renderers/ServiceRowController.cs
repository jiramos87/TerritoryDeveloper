using System;
using Territory.UI.Registry;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Territory.UI.Renderers
{
    /// <summary>
    /// Stage 10 stats/budget panel — list-row companion. Subscribes to a float bind id and
    /// writes the formatted value into <see cref="_secondaryValueText"/>. Icon is set at bake
    /// time; when missing the bake disables the host GameObject and bumps the primary label
    /// flex so captions stay readable.
    /// </summary>
    public class ServiceRowController : MonoBehaviour
    {
        [SerializeField] private UiBindRegistry _bindRegistry;
        [SerializeField] private TextMeshProUGUI _secondaryValueText;
        [SerializeField] private Image _iconImage;
        [SerializeField] private string _bindId;
        [SerializeField] private string _format = "0";

        private IDisposable _sub;

        private void OnEnable()
        {
            if (_bindRegistry == null) _bindRegistry = FindObjectOfType<UiBindRegistry>();
            if (_bindRegistry == null || string.IsNullOrEmpty(_bindId)) return;

            _sub = _bindRegistry.Subscribe<float>(_bindId, OnValueChanged);
        }

        private void OnDisable()
        {
            _sub?.Dispose();
            _sub = null;
        }

        private void OnValueChanged(float v)
        {
            if (_secondaryValueText == null) return;
            _secondaryValueText.text = v.ToString(_format);
        }
    }
}
