using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Territory.UI.Themed
{
    /// <summary>Themed list variant — viewport palette tint; per-item token application delegated to per-item primitives.</summary>
    /// <remarks>TECH-32929 Stage 6.0 — Complex primitive quarantined; [UxmlElement] ListView or UXML repeater replaces.</remarks>
    [Obsolete("ThemedList quarantined (TECH-32929). Use UxmlElement custom-control port or UI Toolkit ListView. Deletion deferred to uGUI purge plan.")]
    public class ThemedList : ThemedPrimitiveBase
    {
        [SerializeField] private string _paletteSlug;
        [SerializeField] private ScrollRect _viewport;
        [SerializeField] private GameObject _itemTemplate;

        public void Populate(IList<string> rowLabels, System.Action<int> onRowSelected)
        {
            if (_viewport == null || _viewport.content == null) return;
            var content = _viewport.content;
            for (int i = content.childCount - 1; i >= 0; i--)
            {
                var child = content.GetChild(i).gameObject;
                if (child != _itemTemplate) Destroy(child);
            }
            if (_itemTemplate != null) _itemTemplate.SetActive(false);
            for (int i = 0; i < rowLabels.Count; i++)
            {
                var rowGo = _itemTemplate != null
                    ? Instantiate(_itemTemplate, content)
                    : new GameObject("Row", typeof(RectTransform));
                rowGo.SetActive(true);
                var tmp = rowGo.GetComponentInChildren<TMP_Text>();
                if (tmp != null) tmp.text = rowLabels[i];
                var legacy = rowGo.GetComponentInChildren<Text>();
                if (legacy != null) legacy.text = rowLabels[i];
                var btn = rowGo.GetComponent<Button>() ?? rowGo.GetComponentInChildren<Button>();
                if (btn != null)
                {
                    int captured = i;
                    btn.onClick.AddListener(() => onRowSelected?.Invoke(captured));
                }
            }
        }

        public override void ApplyTheme(UiTheme theme)
        {
            if (theme == null || _viewport == null) return;
            var background = _viewport.GetComponent<Image>();
            if (background == null) return;
            if (theme.TryGetPalette(_paletteSlug, out var ramp)
                && ramp.ramp != null
                && ramp.ramp.Length > 0
                && ColorUtility.TryParseHtmlString(ramp.ramp[0], out var c))
            {
                background.color = c;
            }
            // _itemTemplate intentionally untouched — per-item Themed* primitives apply their own tokens.
        }
    }
}
