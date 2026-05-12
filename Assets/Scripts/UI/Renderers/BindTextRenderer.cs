using System;
using System.Globalization;
using Territory.UI.Registry;
using TMPro;
using UnityEngine;

namespace Territory.UI.Renderers
{
    /// <summary>
    /// Generic bind-to-TMP text renderer. Subscribes to a bindId on enable and writes a formatted
    /// value to the target TMP_Text. Used by IlluminatedButton bake when params_json.bind is set
    /// (e.g. hud-bar-budget-button binding economyManager.totalBudget to a $N caption).
    ///
    /// format slugs:
    ///   "currency"        → "$N" with thousands separator
    ///   "currency-delta"  → "+$N" / "-$N" with sign
    ///   "integer"         → "N"
    ///   "text" / null     → val.ToString()
    /// </summary>
    public class BindTextRenderer : MonoBehaviour
    {
        [SerializeField] private UiBindRegistry _bindRegistry;
        [SerializeField] private string _bindId;
        [SerializeField] private string _format;
        [SerializeField] private TMP_Text _target;

        private IDisposable _sub;

        public string BindId => _bindId;

        public void Initialize(UiBindRegistry registry, string bindId, string format, TMP_Text target)
        {
            _bindRegistry = registry;
            _bindId = bindId;
            _format = format;
            _target = target;
        }

        private void OnEnable()
        {
            if (_bindRegistry == null) _bindRegistry = FindObjectOfType<UiBindRegistry>();
            if (_target == null) _target = GetComponent<TMP_Text>();
            if (_bindRegistry == null || string.IsNullOrEmpty(_bindId)) return;
            _sub = _bindRegistry.Subscribe<int>(_bindId, OnInt);
        }

        private void OnDisable()
        {
            _sub?.Dispose();
            _sub = null;
        }

        private void OnInt(int v)
        {
            if (_target == null) return;
            _target.text = Format(v);
        }

        private string Format(int v)
        {
            switch (_format)
            {
                case "currency":
                    return "$" + v.ToString("N0", CultureInfo.InvariantCulture);
                case "currency-delta":
                    string sign = v >= 0 ? "+" : "-";
                    return sign + "$" + Math.Abs(v).ToString("N0", CultureInfo.InvariantCulture);
                case "integer":
                    return v.ToString(CultureInfo.InvariantCulture);
                default:
                    return v.ToString(CultureInfo.InvariantCulture);
            }
        }
    }
}
