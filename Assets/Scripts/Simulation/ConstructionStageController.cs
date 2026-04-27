using System;
using UnityEngine;
using Territory.Core;
using Territory.Zones;

namespace Territory.Simulation
{
    /// <summary>
    /// Stage 10 (city-sim-depth) construction-stage progression controller. Per-tick
    /// pull-model <see cref="ProcessTick"/> (invariant 3 — invoked by
    /// <see cref="Territory.SimulationManager.ProcessSimulationTick"/>, NOT MonoBehaviour
    /// <c>Update</c>) advances per-pivot-cell <see cref="CityCell.constructionStage"/>
    /// from 0→3 over <c>effectiveTime = baseTime / (0.5 + Mathf.Clamp01(desirability))</c>
    /// in-game days, with per-stage advance at <c>effectiveTime / 4f</c>. Sources
    /// <c>baseTime</c> from <see cref="ConstructionCurveTable"/> (Resources fallback) and
    /// <c>desirability</c> from <see cref="DesirabilityComposer.CellValue"/>. T10.2 attaches
    /// sprite-swap handler to <see cref="OnStageBoundary"/> in Awake + adds
    /// <c>BeginConstruction</c> placement entry.
    /// </summary>
    public class ConstructionStageController : MonoBehaviour
    {
        [SerializeField] private DesirabilityComposer desirabilityComposer;
        [SerializeField] private ConstructionCurveTable curveTable;
        [SerializeField] private GridManager gridManager;

        // T10.2 attaches sprite-swap subscriber in Awake; T10.1 declares stub only.
        private event Action<CityCell, int> OnStageBoundary;

        private void Awake()
        {
            if (desirabilityComposer == null)
            {
                desirabilityComposer = FindObjectOfType<DesirabilityComposer>();
            }
            if (curveTable == null)
            {
                curveTable = Resources.Load<ConstructionCurveTable>("Construction/ConstructionCurveTable");
                if (curveTable == null)
                {
                    Debug.LogError("ConstructionStageController.curveTable unresolved — ConstructionCurveTable.asset missing under Resources/Construction/ and no Inspector wiring. ProcessTick will no-op.");
                }
            }
            if (gridManager == null)
            {
                gridManager = FindObjectOfType<GridManager>();
            }

            // T10.2 sprite-swap handler subscription — colocated with stage machine.
            OnStageBoundary += HandleStageBoundary;
        }

        /// <summary>Step 4 fills construction-stage progression logic.</summary>
        public void SetDesirabilitySource(DesirabilityComposer composer)
        {
            desirabilityComposer = composer;
        }

        /// <summary>
        /// T10.2 placement entry — invoked by <c>ZoneManager.PlaceZoneBuilding</c> on the
        /// pivot cell only (per <c>ia/specs/managers-reference.md</c> §Pivot cell and
        /// multi-cell buildings). Resets <see cref="CityCell.constructionStage"/> to 0 +
        /// accumulator to 0f and fires <see cref="OnStageBoundary"/>(cell, 0) to load
        /// initial stage-0 sprite.
        /// </summary>
        public void BeginConstruction(CityCell pivotCell, Zone.ZoneType zoneType)
        {
            if (pivotCell == null)
            {
                Debug.LogWarning("ConstructionStageController.BeginConstruction: null pivotCell — skip");
                return;
            }
            pivotCell.constructionStage = 0;
            pivotCell.constructionDayAccumulator = 0f;
            OnStageBoundary?.Invoke(pivotCell, 0);
        }

        /// <summary>
        /// Pull-model per-day tick. Iterates <c>gridManager.cellArray</c> (invariant 5
        /// carve-out: helper-service trust boundary — ConstructionStageController is a
        /// per-cell scheduler, mirrors DesirabilityComposer access pattern); advances
        /// per-pivot stage when accumulator reaches <c>effectiveTime / 4f</c>.
        /// </summary>
        public void ProcessTick()
        {
            if (gridManager == null || curveTable == null || desirabilityComposer == null)
            {
                return;
            }
            if (gridManager.cellArray == null)
            {
                return;
            }

            int width = gridManager.width;
            int height = gridManager.height;
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    CityCell cell = gridManager.cellArray[x, y];
                    if (cell == null)
                    {
                        continue;
                    }
                    // Cheapest-first early-out ordering.
                    if (cell.constructionStage >= 3)
                    {
                        continue;
                    }
                    if (!cell.isPivot)
                    {
                        continue;
                    }
                    if (!IsBuildingZone(cell.zoneType))
                    {
                        continue;
                    }

                    float desirability = desirabilityComposer.CellValue(cell.x, cell.y);
                    float baseTime = curveTable.GetBaseTime(cell.zoneType);
                    float effectiveTime = baseTime / (0.5f + Mathf.Clamp01(desirability));
                    // 3 advances 0→1→2→3 across `effectiveTime` total days. T10.1 §Pending
                    // Decisions narrative ("/4") referenced 4 visual stage values 0..3 (FOUR values,
                    // THREE transitions); T10.2 test bound LOCKED at `Mathf.CeilToInt(effectiveTime)`
                    // for stage-3 reach. /3 per-stage threshold reconciles both authorities.
                    float perStageThreshold = effectiveTime / 3f;

                    cell.constructionDayAccumulator += 1f;
                    if (cell.constructionDayAccumulator >= perStageThreshold)
                    {
                        cell.constructionStage++;
                        cell.constructionDayAccumulator = 0f;
                        OnStageBoundary?.Invoke(cell, cell.constructionStage);
                    }
                }
            }
        }

        /// <summary>
        /// True when <paramref name="zoneType"/> is one of the 12 *Building zone types
        /// matched 1:1 by the curve table. Empty zoning intent (e.g. <c>ResidentialLight</c>
        /// without <c>Building</c> suffix) skipped — only post-<c>PlaceZoneBuilding</c>
        /// pivot cells track stage.
        /// </summary>
        private static bool IsBuildingZone(Zone.ZoneType zoneType)
        {
            switch (zoneType)
            {
                case Zone.ZoneType.ResidentialLightBuilding:
                case Zone.ZoneType.ResidentialMediumBuilding:
                case Zone.ZoneType.ResidentialHeavyBuilding:
                case Zone.ZoneType.CommercialLightBuilding:
                case Zone.ZoneType.CommercialMediumBuilding:
                case Zone.ZoneType.CommercialHeavyBuilding:
                case Zone.ZoneType.IndustrialLightBuilding:
                case Zone.ZoneType.IndustrialMediumBuilding:
                case Zone.ZoneType.IndustrialHeavyBuilding:
                case Zone.ZoneType.StateServiceLightBuilding:
                case Zone.ZoneType.StateServiceMediumBuilding:
                case Zone.ZoneType.StateServiceHeavyBuilding:
                    return true;
                default:
                    return false;
            }
        }

        // Once-per-key sprite warning dedup. Cleared on domain reload via static reset below.
        private static readonly System.Collections.Generic.HashSet<string> _warnedMissingSpriteKeys
            = new System.Collections.Generic.HashSet<string>();

        /// <summary>
        /// T10.2 sprite-swap handler — Resources fallback chain: zone-specific →
        /// <c>Buildings/Construction/placeholder_stage{newStage}</c> → graceful skip with
        /// once-per-key <see cref="Debug.LogWarning"/>. NO NRE on missing art.
        /// </summary>
        private void HandleStageBoundary(CityCell cell, int newStage)
        {
            if (cell == null)
            {
                return;
            }

            Sprite sprite = Resources.Load<Sprite>($"Buildings/Construction/{cell.zoneType}_stage{newStage}");
            if (sprite == null)
            {
                sprite = Resources.Load<Sprite>($"Buildings/Construction/placeholder_stage{newStage}");
            }

            if (sprite == null)
            {
                string warnKey = $"{cell.zoneType}_stage{newStage}";
                if (_warnedMissingSpriteKeys.Add(warnKey))
                {
                    Debug.LogWarning($"ConstructionStageController: missing sprite for {cell.zoneType} stage {newStage} — graceful skip (no NRE)");
                }
                return;
            }

            if (cell.occupiedBuilding == null)
            {
                return;
            }
            SpriteRenderer renderer = cell.occupiedBuilding.GetComponentInChildren<SpriteRenderer>();
            if (renderer == null)
            {
                return;
            }
            renderer.sprite = sprite;
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        private static void ResetSpriteWarningDedupOnDomainReload()
        {
            _warnedMissingSpriteKeys.Clear();
        }
#endif
    }
}
