using UnityEngine;
using UnityEngine.UI;

namespace Territory.UI.Themed
{
    /// <summary>Layout-kind taxonomy — Stage 12 Step 11. Mirrors IR `panel.kind`.</summary>
    public enum PanelKind
    {
        Modal = 0,
        Screen = 1,
        Hud = 2,
        Toolbar = 3,
    }

    /// <summary>Themed panel root + runtime slot graph composer; baked by <c>UiBakeHandler</c> from IR <c>panels[]</c>.</summary>
    public class ThemedPanel : ThemedPrimitiveBase
    {
        [SerializeField] private string _paletteSlug;
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private SlotSpec[] _slots;
        [SerializeField] private GameObject[] _children;
        [SerializeField] private PanelKind _kind = PanelKind.Modal;

        /// <summary>Bake-time SlotSpec[] (read-only — runtime accessor used by EditMode smoke).</summary>
        public SlotSpec[] Slots => _slots;

        /// <summary>Bake-time child candidate refs (read-only — runtime accessor used by EditMode smoke).</summary>
        public GameObject[] Children => _children;

        /// <summary>Bake-time PanelKind (read-only — runtime accessor used by EditMode smoke).</summary>
        public PanelKind Kind => _kind;

        private void OnEnable()
        {
            // Step 11.2 — runtime kind enforcement (overrides any scene PrefabInstance overrides).
            ApplyKindLayout();
            // Step 10.4 — modal stacking: ensure panel draws above its canvas siblings.
            transform.SetAsLastSibling();
        }

        private void ApplyKindLayout()
        {
            var rt = transform as RectTransform;
            if (rt == null) return;

            switch (_kind)
            {
                case PanelKind.Modal:
                    rt.anchorMin = new Vector2(0.5f, 0.5f);
                    rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.sizeDelta = new Vector2(600f, 800f);
                    rt.anchoredPosition = Vector2.zero;
                    EnsureLayoutGroup<VerticalLayoutGroup>();
                    break;
                case PanelKind.Screen:
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.sizeDelta = Vector2.zero;
                    rt.anchoredPosition = Vector2.zero;
                    RemoveLayoutGroups();
                    break;
                case PanelKind.Hud:
                    rt.anchorMin = new Vector2(0f, 1f);
                    rt.anchorMax = new Vector2(1f, 1f);
                    rt.pivot = new Vector2(0.5f, 1f);
                    rt.sizeDelta = new Vector2(0f, 100f);
                    rt.anchoredPosition = Vector2.zero;
                    EnsureLayoutGroup<HorizontalLayoutGroup>();
                    break;
                case PanelKind.Toolbar:
                    rt.anchorMin = new Vector2(0f, 0f);
                    rt.anchorMax = new Vector2(0f, 1f);
                    rt.pivot = new Vector2(0f, 0.5f);
                    rt.sizeDelta = new Vector2(200f, 0f);
                    rt.anchoredPosition = Vector2.zero;
                    EnsureLayoutGroup<VerticalLayoutGroup>();
                    break;
            }
        }

        private void EnsureLayoutGroup<T>() where T : LayoutGroup
        {
            // Strip any other LayoutGroup variants first to avoid double-attach (Step 10.2 cascade fix).
            var existing = GetComponents<LayoutGroup>();
            for (int i = 0; i < existing.Length; i++)
            {
                if (!(existing[i] is T))
                {
                    DestroyImmediate(existing[i]);
                }
            }
            if (GetComponent<T>() == null)
            {
                gameObject.AddComponent<T>();
            }
        }

        private void RemoveLayoutGroups()
        {
            var existing = GetComponents<LayoutGroup>();
            for (int i = 0; i < existing.Length; i++)
            {
                DestroyImmediate(existing[i]);
            }
        }

        public override void ApplyTheme(UiTheme theme)
        {
            // Slot graph composer: walk slots, match each accept-rule against children, parent matched child.
            // Bake-time-vs-runtime contract: bake handler populates _slots + _children deterministically;
            // runtime composer is reparent-only — no PlayMode-only API (no Instantiate of scene refs).
            if (theme != null
                && _backgroundImage != null
                && theme.TryGetPalette(_paletteSlug, out var ramp)
                && ramp.ramp != null
                && ramp.ramp.Length > 0)
            {
                // Step 10.3 — ramp[0] is the darkest shade (near-black); ramp[1] is the panel-fill shade.
                int idx = ramp.ramp.Length >= 2 ? 1 : 0;
                if (ColorUtility.TryParseHtmlString(ramp.ramp[idx], out var bg))
                {
                    _backgroundImage.color = bg;
                }
            }

            if (_slots == null || _children == null) return;

            for (int i = 0; i < _slots.Length; i++)
            {
                var slot = _slots[i];
                if (slot.accepts == null || slot.accepts.Length == 0) continue;

                GameObject matched = null;
                for (int c = 0; c < _children.Length; c++)
                {
                    var child = _children[c];
                    if (child == null) continue;
                    if (ChildMatches(child, slot.accepts))
                    {
                        matched = child;
                        break;
                    }
                }

                if (matched == null)
                {
                    Debug.LogWarning(
                        $"[ThemedPanel] slot {slot.slug} unbound — no child matched accepts[{string.Join(",", slot.accepts)}]");
                    continue;
                }

                if (matched.transform.parent != transform)
                {
                    matched.transform.SetParent(transform, false);
                }
            }
        }

        private static bool ChildMatches(GameObject child, string[] accepts)
        {
            for (int i = 0; i < accepts.Length; i++)
            {
                var token = accepts[i];
                if (string.IsNullOrEmpty(token)) continue;
                if (child.name == token) return true;
            }
            return false;
        }
    }
}
