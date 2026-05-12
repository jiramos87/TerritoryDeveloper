using System.Collections.Generic;
using UnityEngine;
using Territory.Core;
using Territory.Zones;
using Territory.Terrain;
using Territory.Forests;
using Territory.Roads;

namespace Domains.UI.Services
{
    /// <summary>
    /// POCO service extracted from MiniMapController (Stage 5.6 Tier-C NO-PORT).
    /// Owns: cell-color logic, color constants, road/interstate set construction,
    /// desirability range computation. No MonoBehaviour. No Unity lifecycle.
    /// Invariants #3 (no per-frame FindObjectOfType), #4 (no new singletons).
    /// autoReferenced:false — Services/ inherits UI.Runtime assembly coverage.
    /// </summary>
    public class MiniMapService
    {
        // ── Color constants ──────────────────────────────────────────────────────────

        public static readonly Color ColorGrass          = new Color(0.176f, 0.353f, 0.118f);
        public static readonly Color ColorResidential    = new Color(0.298f, 0.686f, 0.314f);
        public static readonly Color ColorCommercial     = new Color(0.129f, 0.588f, 0.953f);
        public static readonly Color ColorIndustrial     = new Color(1f, 0.757f, 0.027f);
        public static readonly Color ColorBuilding       = new Color(0.102f, 0.102f, 0.102f);
        public static readonly Color ColorRoad           = Color.white;
        public static readonly Color ColorInterstate     = new Color(0.5f, 0.5f, 0.5f);
        public static readonly Color ColorWater          = new Color(0.082f, 0.396f, 0.753f);
        public static readonly Color ColorDesirabilityLow  = new Color(0.9f, 0.2f, 0.2f);
        public static readonly Color ColorDesirabilityHigh = new Color(0.2f, 0.8f, 0.2f);
        public static readonly Color ColorForestSparse   = new Color(0.4f, 0.7f, 0.35f);
        public static readonly Color ColorForestMedium   = new Color(0.25f, 0.55f, 0.25f);
        public static readonly Color ColorForestDense    = new Color(0.15f, 0.4f, 0.15f);
        public static readonly Color ColorCentroid       = new Color(1f, 0f, 1f);
        public static readonly Color ColorRingBoundary   = new Color(0.6f, 0.9f, 1f);
        public static readonly Color ColorStateService   = new Color(0.55f, 0.25f, 0.75f);

        // ── State ────────────────────────────────────────────────────────────────────

        private HashSet<Vector2Int> _interstateSet;
        private HashSet<Vector2Int> _roadSet;
        private float _desirabilityMin;
        private float _desirabilityMax;

        // ── Road / interstate set builders ──────────────────────────────────────────

        /// <summary>Build interstate cell set from InterstateManager positions.</summary>
        public void BuildInterstateSet(InterstateManager interstateManager)
        {
            _interstateSet = new HashSet<Vector2Int>();
            if (interstateManager != null && interstateManager.InterstatePositions != null)
            {
                foreach (var pos in interstateManager.InterstatePositions)
                    _interstateSet.Add(pos);
            }
        }

        /// <summary>
        /// Build road set by scanning grid (cell.zoneType == Road).
        /// Bypass cache — auto-generated, manual, restored roads all show correctly.
        /// </summary>
        public void BuildRoadSet(GridManager gridManager)
        {
            _roadSet = new HashSet<Vector2Int>();
            if (gridManager == null || !gridManager.isInitialized)
                return;
            int w = gridManager.width;
            int h = gridManager.height;
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    CityCell c = gridManager.GetCell(x, y);
                    if (c != null && c.zoneType == Zone.ZoneType.Road)
                    {
                        Vector2Int pos = new Vector2Int(x, y);
                        if (_interstateSet == null || !_interstateSet.Contains(pos))
                            _roadSet.Add(pos);
                    }
                }
            }
        }

        // ── Desirability range ───────────────────────────────────────────────────────

        /// <summary>Compute min/max desirability for land cells (excludes water, roads) → dynamic color scaling.</summary>
        public void ComputeDesirabilityRange(int w, int h, GridManager gridManager, WaterManager waterManager)
        {
            _desirabilityMin = float.MaxValue;
            _desirabilityMax = float.MinValue;
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    if (waterManager != null && waterManager.IsWaterAt(x, y))
                        continue;
                    Vector2Int pos = new Vector2Int(x, y);
                    if (_interstateSet != null && _interstateSet.Contains(pos))
                        continue;
                    if (_roadSet != null && _roadSet.Contains(pos))
                        continue;
                    CityCell cell = gridManager.GetCell(x, y);
                    if (cell == null) continue;
                    if (cell.GetZoneType() == Zone.ZoneType.Water)
                        continue;
                    float d = cell.desirability;
                    if (d < _desirabilityMin) _desirabilityMin = d;
                    if (d > _desirabilityMax) _desirabilityMax = d;
                }
            }
            if (_desirabilityMin > _desirabilityMax)
            {
                _desirabilityMin = 0f;
                _desirabilityMax = 20f;
            }
        }

        // ── Cell color ───────────────────────────────────────────────────────────────

        /// <summary>Return display color for cell at (x,y) given active layer flags.</summary>
        public Color GetCellColor(int x, int y, MiniMapLayer activeLayers, GridManager gridManager, WaterManager waterManager)
        {
            Vector2Int pos = new Vector2Int(x, y);
            bool isInterstate = _interstateSet != null && _interstateSet.Contains(pos);
            bool isRoad       = _roadSet       != null && _roadSet.Contains(pos);

            // 1. Water (always visible)
            if (waterManager != null && waterManager.IsWaterAt(x, y))
                return ColorWater;

            // 2. Streets
            if ((activeLayers & MiniMapLayer.Streets) != 0)
            {
                if (isInterstate) return ColorInterstate;
                if (isRoad)       return ColorRoad;
            }

            CityCell cell = gridManager.GetCell(x, y);
            if (cell == null)
                return ColorGrass;

            if (cell.GetZoneType() == Zone.ZoneType.Water)
                return ColorWater;

            Zone.ZoneType zt = cell.GetZoneType();

            // 3. Zones
            if ((activeLayers & MiniMapLayer.Zones) != 0)
            {
                if (IsStateServiceBuilding(zt) || IsStateServiceZoning(zt)) return ColorStateService;
                if (IsBuilding(zt))           return ColorBuilding;
                if (IsResidentialZoning(zt))  return ColorResidential;
                if (IsCommercialZoning(zt))   return ColorCommercial;
                if (IsIndustrialZoning(zt))   return ColorIndustrial;
            }

            // 4. Forests
            if ((activeLayers & MiniMapLayer.Forests) != 0 && cell.HasForest())
                return GetForestColor(cell.GetForestType());

            // 5. Desirability
            if ((activeLayers & MiniMapLayer.Desirability) != 0)
            {
                if (isInterstate) return ColorInterstate;
                if (isRoad)       return ColorRoad;
                return GetDesirabilityColor(cell.desirability);
            }

            return ColorGrass;
        }

        // ── Centroid overlay ─────────────────────────────────────────────────────────

        /// <summary>
        /// Paint centroid marker + ring boundaries onto texture.
        /// Called after per-cell pass when Centroid layer is active.
        /// </summary>
        public void PaintCentroidOverlay(Texture2D tex, int w, int h,
            Vector2 centroid, float[] boundaries)
        {
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    Vector2 cellCenter = new Vector2(x + 0.5f, y + 0.5f);
                    float dist = Vector2.Distance(cellCenter, centroid);
                    foreach (float b in boundaries)
                    {
                        if (Mathf.Abs(dist - b) < 0.45f)
                        {
                            tex.SetPixel(x, y, ColorRingBoundary);
                            break;
                        }
                    }
                }
            }

            int cx = Mathf.Clamp(Mathf.RoundToInt(centroid.x), 0, w - 1);
            int cy = Mathf.Clamp(Mathf.RoundToInt(centroid.y), 0, h - 1);
            const int markerRadius = 2;
            for (int dx = -markerRadius; dx <= markerRadius; dx++)
            {
                int px = cx + dx;
                if (px >= 0 && px < w)
                    tex.SetPixel(px, cy, ColorCentroid);
            }
            for (int dy = -markerRadius; dy <= markerRadius; dy++)
            {
                int py = cy + dy;
                if (py >= 0 && py < h)
                    tex.SetPixel(cx, py, ColorCentroid);
            }
        }

        // ── Desirability color ───────────────────────────────────────────────────────

        public Color GetDesirabilityColor(float desirability)
        {
            float range = _desirabilityMax - _desirabilityMin;
            float t = range > 0.001f
                ? Mathf.Clamp01((desirability - _desirabilityMin) / range)
                : 0f;
            return Color.Lerp(ColorDesirabilityLow, ColorDesirabilityHigh, t);
        }

        // ── Forest color ─────────────────────────────────────────────────────────────

        public static Color GetForestColor(Forest.ForestType forestType)
        {
            switch (forestType)
            {
                case Forest.ForestType.Sparse: return ColorForestSparse;
                case Forest.ForestType.Medium: return ColorForestMedium;
                case Forest.ForestType.Dense:  return ColorForestDense;
                default:                       return ColorForestMedium;
            }
        }

        // ── Viewport rect helper ─────────────────────────────────────────────────────

        /// <summary>Compute normalised anchor rect for viewport overlay from camera frustum + grid.</summary>
        public static void ComputeViewportAnchors(Camera cam, GridManager gridManager,
            out Vector2 anchorMin, out Vector2 anchorMax)
        {
            Vector3 camPos  = cam.transform.position;
            float orthoSize = cam.orthographicSize;
            float halfW     = orthoSize * cam.aspect;
            float halfH     = orthoSize;

            Vector2 bl = new Vector2(camPos.x - halfW, camPos.y - halfH);
            Vector2 br = new Vector2(camPos.x + halfW, camPos.y - halfH);
            Vector2 tl = new Vector2(camPos.x - halfW, camPos.y + halfH);
            Vector2 tr = new Vector2(camPos.x + halfW, camPos.y + halfH);

            int minGX = Mathf.Min((int)gridManager.GetGridPosition(bl).x, (int)gridManager.GetGridPosition(br).x,
                (int)gridManager.GetGridPosition(tl).x, (int)gridManager.GetGridPosition(tr).x);
            int maxGX = Mathf.Max((int)gridManager.GetGridPosition(bl).x, (int)gridManager.GetGridPosition(br).x,
                (int)gridManager.GetGridPosition(tl).x, (int)gridManager.GetGridPosition(tr).x);
            int minGY = Mathf.Min((int)gridManager.GetGridPosition(bl).y, (int)gridManager.GetGridPosition(br).y,
                (int)gridManager.GetGridPosition(tl).y, (int)gridManager.GetGridPosition(tr).y);
            int maxGY = Mathf.Max((int)gridManager.GetGridPosition(bl).y, (int)gridManager.GetGridPosition(br).y,
                (int)gridManager.GetGridPosition(tl).y, (int)gridManager.GetGridPosition(tr).y);

            int w = gridManager.width;
            int h = gridManager.height;

            anchorMin = new Vector2(Mathf.Clamp01(minGX / (float)w), Mathf.Clamp01(minGY / (float)h));
            anchorMax = new Vector2(Mathf.Clamp01((maxGX + 1) / (float)w), Mathf.Clamp01((maxGY + 1) / (float)h));
        }

        // ── Click-to-navigate helper ─────────────────────────────────────────────────

        /// <summary>Convert screen-space local point on minimap rect to grid coords.</summary>
        public static Vector2Int LocalPointToGrid(Vector2 localPoint, Rect rectLocal, GridManager gridManager)
        {
            float normX = (localPoint.x - rectLocal.xMin) / rectLocal.width;
            float normY = (localPoint.y - rectLocal.yMin) / rectLocal.height;
            int w = gridManager.width;
            int h = gridManager.height;
            int gx = Mathf.Clamp(Mathf.FloorToInt(normX * w), 0, w - 1);
            int gy = Mathf.Clamp(Mathf.FloorToInt(normY * h), 0, h - 1);
            return new Vector2Int(gx, gy);
        }

        // ── Drag-pan helper ──────────────────────────────────────────────────────────

        /// <summary>Convert drag delta (normalised map coords) + current cam grid pos → target grid cell.</summary>
        public static Vector2Int DragDeltaToTargetGrid(float normDx, float normDy, Camera cam, GridManager gridManager)
        {
            Vector2 camGrid = gridManager.GetGridPosition(new Vector2(cam.transform.position.x, cam.transform.position.y));
            int w = gridManager.width;
            int h = gridManager.height;
            return new Vector2Int(
                Mathf.Clamp(Mathf.RoundToInt(camGrid.x + normDx * w), 0, w - 1),
                Mathf.Clamp(Mathf.RoundToInt(camGrid.y + normDy * h), 0, h - 1));
        }

        // ── Zone classifiers ─────────────────────────────────────────────────────────

        public static bool IsBuilding(Zone.ZoneType zt)
        {
            return zt == Zone.ZoneType.Building ||
                   zt == Zone.ZoneType.ResidentialLightBuilding || zt == Zone.ZoneType.ResidentialMediumBuilding || zt == Zone.ZoneType.ResidentialHeavyBuilding ||
                   zt == Zone.ZoneType.CommercialLightBuilding  || zt == Zone.ZoneType.CommercialMediumBuilding  || zt == Zone.ZoneType.CommercialHeavyBuilding  ||
                   zt == Zone.ZoneType.IndustrialLightBuilding  || zt == Zone.ZoneType.IndustrialMediumBuilding  || zt == Zone.ZoneType.IndustrialHeavyBuilding;
        }

        public static bool IsStateServiceBuilding(Zone.ZoneType zt)
        {
            return zt == Zone.ZoneType.StateServiceLightBuilding || zt == Zone.ZoneType.StateServiceMediumBuilding ||
                   zt == Zone.ZoneType.StateServiceHeavyBuilding;
        }

        public static bool IsStateServiceZoning(Zone.ZoneType zt)
        {
            return zt == Zone.ZoneType.StateServiceLightZoning || zt == Zone.ZoneType.StateServiceMediumZoning ||
                   zt == Zone.ZoneType.StateServiceHeavyZoning;
        }

        public static bool IsResidentialZoning(Zone.ZoneType zt)
        {
            return zt == Zone.ZoneType.ResidentialLightZoning || zt == Zone.ZoneType.ResidentialMediumZoning || zt == Zone.ZoneType.ResidentialHeavyZoning;
        }

        public static bool IsCommercialZoning(Zone.ZoneType zt)
        {
            return zt == Zone.ZoneType.CommercialLightZoning || zt == Zone.ZoneType.CommercialMediumZoning || zt == Zone.ZoneType.CommercialHeavyZoning;
        }

        public static bool IsIndustrialZoning(Zone.ZoneType zt)
        {
            return zt == Zone.ZoneType.IndustrialLightZoning || zt == Zone.ZoneType.IndustrialMediumZoning || zt == Zone.ZoneType.IndustrialHeavyZoning;
        }
    }
}
