using UnityEngine;
using UnityEngine.UIElements;
using Territory.Core;
using Territory.Terrain;
using Territory.Zones;
using Territory.Forests;

namespace Territory.UI.Hosts
{
    /// <summary>
    /// Effort 4 (post iter-27) — per-frame hover-info card.
    /// Reads GridManager.mouseGridPosition + GetCell(x,y); shows cell coords / type /
    /// zone state / height / building name. Anchored bottom-right.
    /// Self-bootstraps via RuntimeInitializeOnLoadMethod when CityScene loads — attaches
    /// a programmatic VisualElement card to the notifications-toast UIDocument root
    /// (already proven full-viewport cream overlay; no scene .unity edit needed).
    /// Hides when cursor leaves valid grid or hovers over a UI Toolkit picking-enabled element.
    /// </summary>
    public sealed class HoverInfoHost : MonoBehaviour
    {
        VisualElement _root;
        Label _rowCell;
        Label _rowType;
        Label _rowZone;
        Label _rowHeight;
        Label _rowName;

        GridManager _grid;
        WaterManager _water;
        UIDocument _anchorDoc;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            // Only run in CityScene — MainMenu has no grid.
            var sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (sceneName != "CityScene") return;
            if (FindObjectOfType<HoverInfoHost>() != null) return;
            var go = new GameObject("HoverInfoHost");
            go.AddComponent<HoverInfoHost>();
        }

        void Start() => TryAttach();

        void TryAttach()
        {
            if (_root != null) return;
            _grid = FindObjectOfType<GridManager>();
            _water = FindObjectOfType<WaterManager>();
            // Pick the first UIDocument whose root is mounted; toast doc is preferred.
            var toast = FindObjectOfType<NotificationsToastHost>();
            if (toast != null)
            {
                var doc = toast.GetComponent<UIDocument>();
                if (doc != null && doc.rootVisualElement != null) _anchorDoc = doc;
            }
            if (_anchorDoc == null)
            {
                foreach (var d in FindObjectsOfType<UIDocument>())
                    if (d != null && d.rootVisualElement != null) { _anchorDoc = d; break; }
            }
            if (_anchorDoc == null || _anchorDoc.rootVisualElement == null) return;

            _root = new VisualElement { name = "hover-info" };
            _root.pickingMode = PickingMode.Ignore;
            ApplyCardStyle(_root);

            _rowCell   = MakeRow("hover-info__row--coord");
            _rowType   = MakeRow("hover-info__row--type");
            _rowZone   = MakeRow(null);
            _rowHeight = MakeRow(null);
            _rowName   = MakeRow("hover-info__row--name");

            _root.Add(_rowCell);
            _root.Add(_rowType);
            _root.Add(_rowZone);
            _root.Add(_rowHeight);
            _root.Add(_rowName);
            _root.style.display = DisplayStyle.None;

            _anchorDoc.rootVisualElement.Add(_root);
        }

        Label MakeRow(string variant)
        {
            var l = new Label("");
            // Inline literal hex (plan-scope rule). Tan/cream palette mirror §20 toast.
            l.style.color = Hex("#3a2f1c");
            l.style.fontSize = 11f;
            l.style.marginBottom = 2f;
            l.style.whiteSpace = WhiteSpace.Normal;
            if (variant == "hover-info__row--coord") { l.style.color = Hex("#6b5a3d"); l.style.fontSize = 10f; }
            else if (variant == "hover-info__row--type") { l.style.unityFontStyleAndWeight = FontStyle.Bold; l.style.fontSize = 12f; }
            else if (variant == "hover-info__row--name") { l.style.color = Hex("#5b7fa8"); }
            return l;
        }

        void ApplyCardStyle(VisualElement v)
        {
            v.style.position = Position.Absolute;
            v.style.bottom = 24f;
            v.style.right = 24f;
            v.style.width = 220f;
            v.style.backgroundColor = Hex("#f5e6c8");
            var tan = Hex("#b89b5e");
            v.style.borderTopColor = tan; v.style.borderBottomColor = tan;
            v.style.borderLeftColor = tan; v.style.borderRightColor = tan;
            v.style.borderTopWidth = 1f; v.style.borderBottomWidth = 1f;
            v.style.borderLeftWidth = 1f; v.style.borderRightWidth = 1f;
            v.style.borderTopLeftRadius = 4f; v.style.borderTopRightRadius = 4f;
            v.style.borderBottomLeftRadius = 4f; v.style.borderBottomRightRadius = 4f;
            v.style.paddingTop = 8f; v.style.paddingBottom = 8f;
            v.style.paddingLeft = 10f; v.style.paddingRight = 10f;
            v.style.flexDirection = FlexDirection.Column;
        }

        static Color Hex(string h)
        {
            ColorUtility.TryParseHtmlString(h, out var c);
            return c;
        }

        void Update()
        {
            if (_root == null) { TryAttach(); return; }
            if (_grid == null) { _grid = FindObjectOfType<GridManager>(); if (_grid == null) { Hide(); return; } }
            if (!_grid.isInitialized) { Hide(); return; }

            if (IsPointerOverPickingUi())
            {
                Hide();
                return;
            }

            int x = Mathf.RoundToInt(_grid.mouseGridPosition.x);
            int y = Mathf.RoundToInt(_grid.mouseGridPosition.y);
            var cell = _grid.GetCell(x, y);
            if (cell == null) { Hide(); return; }

            _rowCell.text   = $"Cell ({x}, {y})";
            _rowType.text   = $"Type: {DescribeType(cell)}";
            _rowZone.text   = $"Occupied: {(cell.occupiedBuilding != null || IsZoneBuilding(cell.zoneType) ? "yes" : "no")}";
            int surfaceH = ResolveSurfaceHeight(x, y, cell);
            _rowHeight.text = surfaceH != cell.height
                ? $"Ground: {cell.height}  ·  Surface: {surfaceH}"
                : $"Height: {cell.height}";
            var bname = cell.GetBuildingName();
            if (!string.IsNullOrEmpty(bname))
            {
                _rowName.text = bname;
                _rowName.style.display = DisplayStyle.Flex;
            }
            else
            {
                _rowName.style.display = DisplayStyle.None;
            }

            Show();
        }

        void Show() { if (_root.style.display != DisplayStyle.Flex) _root.style.display = DisplayStyle.Flex; }
        void Hide() { if (_root != null && _root.style.display != DisplayStyle.None) _root.style.display = DisplayStyle.None; }

        bool IsPointerOverPickingUi()
        {
            if (_anchorDoc == null || _anchorDoc.rootVisualElement == null) return false;
            // Check every UIDocument in scene; pick-test mouse position against any non-ignore element.
            var docs = FindObjectsOfType<UIDocument>();
            Vector2 mouse = Input.mousePosition;
            // UI Toolkit y-down; Input y-up. Convert.
            float invY = Screen.height - mouse.y;
            var screenPt = new Vector2(mouse.x, invY);
            foreach (var d in docs)
            {
                if (d == null || d.rootVisualElement == null) continue;
                var panel = d.rootVisualElement.panel;
                if (panel == null) continue;
                var hit = panel.Pick(screenPt);
                if (hit == null) continue;
                // Ignore the hover-info card itself.
                if (hit == _root || hit.FindCommonAncestor(_root) == _root) continue;
                // Hit must be picking-enabled (Position non-Ignore) to count as a UI overlay.
                if (hit.pickingMode == PickingMode.Ignore) continue;
                return true;
            }
            return false;
        }

        int ResolveSurfaceHeight(int x, int y, CityCell c)
        {
            if (_water == null) _water = FindObjectOfType<WaterManager>();
            var wm = _water != null ? _water.GetWaterMap() : null;
            if (wm != null)
            {
                int sh = wm.GetSurfaceHeightAt(x, y);
                if (sh > -1) return sh;
            }
            return c != null ? c.height : 0;
        }

        static string DescribeType(CityCell c)
        {
            if (c == null) return "Unknown";
            var z = c.zoneType;
            if (z == Zone.ZoneType.Road) return "Road";
            if (z == Zone.ZoneType.Water) return "Water";
            if (z == Zone.ZoneType.Forest || c.forestType != Forest.ForestType.None) return "Forest";
            if (IsZoneBuilding(z)) return $"Building ({z})";
            if (IsZoning(z)) return $"Zone ({z})";
            return "Grass";
        }

        static bool IsZoning(Zone.ZoneType z) =>
            z == Zone.ZoneType.ResidentialLightZoning || z == Zone.ZoneType.ResidentialMediumZoning || z == Zone.ZoneType.ResidentialHeavyZoning ||
            z == Zone.ZoneType.CommercialLightZoning  || z == Zone.ZoneType.CommercialMediumZoning  || z == Zone.ZoneType.CommercialHeavyZoning  ||
            z == Zone.ZoneType.IndustrialLightZoning  || z == Zone.ZoneType.IndustrialMediumZoning  || z == Zone.ZoneType.IndustrialHeavyZoning  ||
            z == Zone.ZoneType.StateServiceLightZoning|| z == Zone.ZoneType.StateServiceMediumZoning|| z == Zone.ZoneType.StateServiceHeavyZoning;

        static bool IsZoneBuilding(Zone.ZoneType z) =>
            z == Zone.ZoneType.ResidentialLightBuilding || z == Zone.ZoneType.ResidentialMediumBuilding || z == Zone.ZoneType.ResidentialHeavyBuilding ||
            z == Zone.ZoneType.CommercialLightBuilding  || z == Zone.ZoneType.CommercialMediumBuilding  || z == Zone.ZoneType.CommercialHeavyBuilding  ||
            z == Zone.ZoneType.IndustrialLightBuilding  || z == Zone.ZoneType.IndustrialMediumBuilding  || z == Zone.ZoneType.IndustrialHeavyBuilding  ||
            z == Zone.ZoneType.StateServiceLightBuilding|| z == Zone.ZoneType.StateServiceMediumBuilding|| z == Zone.ZoneType.StateServiceHeavyBuilding ||
            z == Zone.ZoneType.Building;
    }
}
