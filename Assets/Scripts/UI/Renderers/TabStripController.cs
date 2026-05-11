using System;
using System.Collections.Generic;
using Territory.UI.Registry;
using UnityEngine;
using UnityEngine.UI;

namespace Territory.UI.Renderers
{
    /// <summary>
    /// Stage 10 stats/budget panel — tab-strip-stub companion. Reads the active caption from a
    /// string bind and recolors all pills + drives their <see cref="Toggle.isOn"/> state.
    /// Click feedback re-publishes the caption to the bind so any other subscriber
    /// (panel adapter, history recorder) sees the change.
    /// </summary>
    public class TabStripController : MonoBehaviour
    {
        [Serializable]
        public struct Pill
        {
            public Toggle toggle;
            public Image background;
            public string captionId;
        }

        [SerializeField] private UiBindRegistry _bindRegistry;
        [SerializeField] private string _bindId;
        [SerializeField] private Color _activeColor = new Color(0.29f, 0.62f, 1f, 1f);
        [SerializeField] private Color _idleColor = new Color(0.18f, 0.18f, 0.22f, 1f);
        [SerializeField] private Pill[] _pills;

        private IDisposable _sub;
        private bool _suppress;

        private void OnEnable()
        {
            if (_bindRegistry == null) _bindRegistry = FindObjectOfType<UiBindRegistry>();

            if (_pills != null)
            {
                for (int i = 0; i < _pills.Length; i++)
                {
                    var pill = _pills[i];
                    if (pill.toggle == null) continue;
                    string captionId = pill.captionId;
                    pill.toggle.onValueChanged.AddListener(isOn =>
                    {
                        if (_suppress) return;
                        if (!isOn) return;
                        if (_bindRegistry == null || string.IsNullOrEmpty(_bindId)) return;
                        _bindRegistry.Set(_bindId, captionId);
                    });
                }
            }

            if (_bindRegistry == null || string.IsNullOrEmpty(_bindId)) return;
            _sub = _bindRegistry.Subscribe<string>(_bindId, OnActiveChanged);
        }

        private void OnDisable()
        {
            _sub?.Dispose();
            _sub = null;
            if (_pills != null)
            {
                for (int i = 0; i < _pills.Length; i++)
                {
                    if (_pills[i].toggle != null) _pills[i].toggle.onValueChanged.RemoveAllListeners();
                }
            }
        }

        private void OnActiveChanged(string activeCaption)
        {
            if (_pills == null) return;
            _suppress = true;
            try
            {
                for (int i = 0; i < _pills.Length; i++)
                {
                    var pill = _pills[i];
                    bool active = pill.captionId == activeCaption;
                    if (pill.background != null) pill.background.color = active ? _activeColor : _idleColor;
                    if (pill.toggle != null) pill.toggle.isOn = active;
                }
            }
            finally
            {
                _suppress = false;
            }
        }
    }
}
