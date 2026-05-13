using System;
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
    /// <remarks>TECH-32929 Stage 6.0 — Quarantined; UIDocument/UXML panel replaces.</remarks>
    [Obsolete("ThemedPanel quarantined (TECH-32929). Use UIDocument / UXML panel host. Deletion deferred to uGUI purge plan.")]
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
                // Pilot rim (2026-05-12) — force amber color-border-accent (#ffb020) on the 4
                // border strips AND bump thickness 3 → 6 px so always-on UI surfaces (toolbar,
                // themed panels) read coherent with stats-panel rim. Was ramp[4] = #34393f on
                // chassis-graphite (dark grey, invisible) + 3 px thin hairline.
                Color borderColor = new Color(1f, 0.690f, 0.125f, 1f);
                const float pilotBorderThickness = 6f;
                if (_borderTop != null)
                {
                    _borderTop.color = borderColor;
                    var rt = _borderTop.rectTransform; if (rt != null) rt.sizeDelta = new Vector2(0f, pilotBorderThickness);
                }
                if (_borderBottom != null)
                {
                    _borderBottom.color = borderColor;
                    var rt = _borderBottom.rectTransform; if (rt != null) rt.sizeDelta = new Vector2(0f, pilotBorderThickness);
                }
                if (_borderLeft != null)
                {
                    _borderLeft.color = borderColor;
                    var rt = _borderLeft.rectTransform; if (rt != null) rt.sizeDelta = new Vector2(pilotBorderThickness, 0f);
                }
                if (_borderRight != null)
                {
                    _borderRight.color = borderColor;
                    var rt = _borderRight.rectTransform; if (rt != null) rt.sizeDelta = new Vector2(pilotBorderThickness, 0f);
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
