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
        SideRail = 4,
    }

    /// <summary>Themed panel root + runtime slot graph composer; baked by <c>UiBakeHandler</c> from IR <c>panels[]</c>.</summary>
    [ExecuteAlways]
    public class ThemedPanel : ThemedPrimitiveBase
    {
        [SerializeField] private string _paletteSlug;
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private SlotSpec[] _slots;
        [SerializeField] private GameObject[] _children;
        [SerializeField] private PanelKind _kind = PanelKind.Modal;
        [SerializeField] private string _frameStyleSlug;
        [SerializeField] private Image _borderTop;
        [SerializeField] private Image _borderBottom;
        [SerializeField] private Image _borderLeft;
        [SerializeField] private Image _borderRight;

        /// <summary>Bake-time SlotSpec[] (read-only — runtime accessor used by EditMode smoke).</summary>
        public SlotSpec[] Slots => _slots;

        /// <summary>Bake-time child candidate refs (read-only — runtime accessor used by EditMode smoke).</summary>
        public GameObject[] Children => _children;

        /// <summary>Bake-time PanelKind (read-only — runtime accessor used by EditMode smoke).</summary>
        public PanelKind Kind => _kind;

        private void OnEnable()
        {
            // Stage 13.7 fallout — kind dispatch is now component-only (LayoutGroup
            // attach + sibling order). RectTransform is sourced from the authored
            // prefab (baked from layout-rects.json CD truth source). Runtime rect
            // override was the loss vector for Stage 13.7's anchor wipe — bake handler
            // wrote correct anchors, OnEnable clobbered them with kind-default
            // placeholder before SaveAsPrefabAsset captured the result.
            ApplyKindLayout();
            // Step 10.4 — modal stacking: ensure panel draws above its canvas siblings.
            transform.SetAsLastSibling();
        }

        private void ApplyKindLayout()
        {
            // RectTransform writes intentionally removed (Stage 13.7 fallout). Authored
            // anchors / pivot / sizeDelta on the prefab are the truth source — see
            // `Assets/Scripts/Editor/Bridge/UiBakeHandler.Frame.cs` SavePanelPrefab. This switch
            // only attaches the kind-appropriate LayoutGroup; layout itself is anchor-driven.
            switch (_kind)
            {
                case PanelKind.Modal:
                    EnsureLayoutGroup<VerticalLayoutGroup>();
                    break;
                case PanelKind.Screen:
                    RemoveLayoutGroups();
                    break;
                case PanelKind.Hud:
                    EnsureLayoutGroup<HorizontalLayoutGroup>();
                    break;
                case PanelKind.Toolbar:
                    EnsureGridLayout(columns: 2, cell: new Vector2(100f, 100f), spacing: new Vector2(8f, 8f), padding: 8);
                    break;
                case PanelKind.SideRail:
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

        private void EnsureGridLayout(int columns, Vector2 cell, Vector2 spacing, int padding)
        {
            var existing = GetComponents<LayoutGroup>();
            for (int i = 0; i < existing.Length; i++)
            {
                if (!(existing[i] is GridLayoutGroup))
                {
                    DestroyImmediate(existing[i]);
                }
            }
            var grid = GetComponent<GridLayoutGroup>();
            if (grid == null)
            {
                grid = gameObject.AddComponent<GridLayoutGroup>();
            }
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = columns;
            grid.cellSize = cell;
            grid.spacing = spacing;
            grid.padding = new RectOffset(padding, padding, padding, padding);
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
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
                // Step 16.8 — procedural border tint: pick a clearly-distinct shade from the
                // panel-fill ramp[1]. ramp[2] (Step 16.2) was only ~9 brightness units lighter and
                // rendered invisible. ramp[4] (or ramp.Length-1 fallback) gives a visible hairline.
                int borderIdx = ramp.ramp.Length >= 5 ? 4 : ramp.ramp.Length - 1;
                if (borderIdx < 0) borderIdx = idx;
                if (ColorUtility.TryParseHtmlString(ramp.ramp[borderIdx], out var borderColor))
                {
                    if (_borderTop != null) _borderTop.color = borderColor;
                    if (_borderBottom != null) _borderBottom.color = borderColor;
                    if (_borderLeft != null) _borderLeft.color = borderColor;
                    if (_borderRight != null) _borderRight.color = borderColor;
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
