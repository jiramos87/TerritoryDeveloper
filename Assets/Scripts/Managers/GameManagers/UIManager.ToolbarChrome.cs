using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Territory.Buildings;
using Territory.Forests;
using Territory.Zones;

namespace Territory.UI
{
    /// <summary>
    /// Toolbar strip: cache direct child <see cref="Button"/> graphics under <see cref="ControlPanel"/>, tint active tool via <see cref="UiTheme.SurfaceElevated"/> (partial of <see cref="UIManager"/>).
    /// </summary>
    public partial class UIManager
    {
        private struct CachedToolbarToolGraphic
        {
            public Image Image;
            public Color IdleColor;
            public string RowName;
        }

        private CachedToolbarToolGraphic[] cachedToolbarToolGraphics;
        private bool toolbarChromeCacheBuilt;
        private bool toolbarChromeDirty;

        /// <summary>
        /// Mark toolbar chrome for refresh next <see cref="LateUpdate"/> → selection state settles after <see cref="ClearCurrentTool"/> + tool setup in same click frame.
        /// </summary>
        private void RequestToolbarChromeRefresh()
        {
            toolbarChromeDirty = true;
        }

        private void BuildToolbarChromeCacheIfNeeded()
        {
            if (toolbarChromeCacheBuilt || hudUiTheme == null || controlPanelBackgroundImage == null)
                return;

            Transform root = controlPanelBackgroundImage.transform;
            var list = new List<CachedToolbarToolGraphic>();
            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                var button = child.GetComponent<Button>();
                if (button == null)
                    continue;
                var graphic = button.targetGraphic as Image;
                if (graphic == null)
                    continue;
                list.Add(new CachedToolbarToolGraphic
                {
                    Image = graphic,
                    IdleColor = graphic.color,
                    RowName = child.name
                });
            }

            cachedToolbarToolGraphics = list.ToArray();
            toolbarChromeCacheBuilt = true;
        }

        private void RefreshToolbarToolChrome()
        {
            BuildToolbarChromeCacheIfNeeded();
            if (!toolbarChromeCacheBuilt || cachedToolbarToolGraphics == null || hudUiTheme == null)
                return;

            string activeRow = GetActiveControlPanelDirectChildName();
            Color elevated = hudUiTheme.SurfaceElevated;
            for (int i = 0; i < cachedToolbarToolGraphics.Length; i++)
            {
                CachedToolbarToolGraphic g = cachedToolbarToolGraphics[i];
                if (g.Image == null)
                    continue;
                bool on = activeRow != null && g.RowName == activeRow;
                g.Image.color = on ? elevated : g.IdleColor;
            }
        }

        /// <summary>
        /// Map current tool state → direct child name under <see cref="ControlPanel"/> (MainScene hierarchy).
        /// </summary>
        private string GetActiveControlPanelDirectChildName()
        {
            if (bulldozeMode)
                return "BulldozerButton";
            if (detailsMode)
                return null;
            if (selectedZoneType == Zone.ZoneType.Road)
                return "RoadsSelectorButton";
            if (selectedZoneType == Zone.ZoneType.Water)
                return "WaterBuildingSelectorButton";
            if (selectedBuilding != null)
            {
                if (selectedBuilding is PowerPlant)
                    return "PowerBuildingSelectorButton";
                if (selectedBuilding is WaterPlant)
                    return "WaterBuildingSelectorButton";
            }

            if (selectedForestData.forestType != Forest.ForestType.None)
                return "EnviromentalSelectorButton";

            if (IsResidentialZoning(selectedZoneType))
                return "ResidentialZoningSelectorButton";
            if (IsCommercialZoning(selectedZoneType))
                return "CommercialZoningSelectorButton";
            if (IsIndustrialZoning(selectedZoneType))
                return "IndustrialZoningSelectorButton";

            return null;
        }

        private static bool IsResidentialZoning(Zone.ZoneType z)
        {
            return z == Zone.ZoneType.ResidentialLightZoning ||
                   z == Zone.ZoneType.ResidentialMediumZoning ||
                   z == Zone.ZoneType.ResidentialHeavyZoning;
        }

        private static bool IsCommercialZoning(Zone.ZoneType z)
        {
            return z == Zone.ZoneType.CommercialLightZoning ||
                   z == Zone.ZoneType.CommercialMediumZoning ||
                   z == Zone.ZoneType.CommercialHeavyZoning;
        }

        private static bool IsIndustrialZoning(Zone.ZoneType z)
        {
            return z == Zone.ZoneType.IndustrialLightZoning ||
                   z == Zone.ZoneType.IndustrialMediumZoning ||
                   z == Zone.ZoneType.IndustrialHeavyZoning;
        }
    }
}
