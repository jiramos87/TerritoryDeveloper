using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.IO;
using System.Collections.Generic;
using Territory.Core;
using Territory.Roads;
using Territory.Zones;
using Territory.Economy;
using Territory.Terrain;
using Territory.Timing;
using Territory.Persistence;
using Territory.Forests;
using Territory.Buildings;
using Territory.Utilities;

namespace Territory.UI
{
// Toolbar tool selection and ghost preview (partial of UIManager).
public partial class UIManager
{
    #region Toolbar and Selection
    public void OnLightResidentialButtonClicked()
    {
        ClearCurrentTool();
        selectedZoneType = Zone.ZoneType.ResidentialLightZoning;
        SetGhostPreview(zoneManager.GetRandomZonePrefab(selectedZoneType, 1), 1);
        CheckAndShowDemandFeedback(selectedZoneType);
        RequestToolbarChromeRefresh();
    }

    public void OnMediumResidentialButtonClicked()
    {
        ClearCurrentTool();
        selectedZoneType = Zone.ZoneType.ResidentialMediumZoning;
        SetGhostPreview(zoneManager.GetRandomZonePrefab(selectedZoneType, 1), 1);
        CheckAndShowDemandFeedback(selectedZoneType);
        RequestToolbarChromeRefresh();
    }

    public void OnHeavyResidentialButtonClicked()
    {
        ClearCurrentTool();
        selectedZoneType = Zone.ZoneType.ResidentialHeavyZoning;
        SetGhostPreview(zoneManager.GetRandomZonePrefab(selectedZoneType, 1), 1);
        CheckAndShowDemandFeedback(selectedZoneType);
        RequestToolbarChromeRefresh();
    }

    public void OnLightCommercialButtonClicked()
    {
        ClearCurrentTool();
        selectedZoneType = Zone.ZoneType.CommercialLightZoning;
        SetGhostPreview(zoneManager.GetRandomZonePrefab(selectedZoneType, 1), 1);
        CheckAndShowDemandFeedback(selectedZoneType);
        RequestToolbarChromeRefresh();
    }

    public void OnMediumCommercialButtonClicked()
    {
        ClearCurrentTool();
        selectedZoneType = Zone.ZoneType.CommercialMediumZoning;
        SetGhostPreview(zoneManager.GetRandomZonePrefab(selectedZoneType, 1), 1);
        CheckAndShowDemandFeedback(selectedZoneType);
        RequestToolbarChromeRefresh();
    }

    public void OnHeavyCommercialButtonClicked()
    {
        ClearCurrentTool();
        selectedZoneType = Zone.ZoneType.CommercialHeavyZoning;
        SetGhostPreview(zoneManager.GetRandomZonePrefab(selectedZoneType, 1), 1);
        CheckAndShowDemandFeedback(selectedZoneType);
        RequestToolbarChromeRefresh();
    }

    public void OnLightIndustrialButtonClicked()
    {
        ClearCurrentTool();
        selectedZoneType = Zone.ZoneType.IndustrialLightZoning;
        SetGhostPreview(zoneManager.GetRandomZonePrefab(selectedZoneType, 1), 1);
        CheckAndShowDemandFeedback(selectedZoneType);
        RequestToolbarChromeRefresh();
    }

    public void OnMediumIndustrialButtonClicked()
    {
        ClearCurrentTool();
        selectedZoneType = Zone.ZoneType.IndustrialMediumZoning;
        SetGhostPreview(zoneManager.GetRandomZonePrefab(selectedZoneType, 1), 1);
        CheckAndShowDemandFeedback(selectedZoneType);
        RequestToolbarChromeRefresh();
    }

    public void OnHeavyIndustrialButtonClicked()
    {
        ClearCurrentTool();
        selectedZoneType = Zone.ZoneType.IndustrialHeavyZoning;
        SetGhostPreview(zoneManager.GetRandomZonePrefab(selectedZoneType, 1), 1);
        CheckAndShowDemandFeedback(selectedZoneType);
        RequestToolbarChromeRefresh();
    }

    private void CheckAndShowDemandFeedback(Zone.ZoneType zoneType)
    {
        if (gridManager != null && gridManager.demandManager != null)
        {
            float demandLevel = gridManager.demandManager.GetDemandLevel(zoneType);
            bool canGrow = gridManager.demandManager.CanZoneTypeGrow(zoneType);

            // Check residential requirements for commercial/industrial
            Zone.ZoneType buildingType = GetBuildingTypeFromZoning(zoneType);
            bool needsResidential = IsCommercialOrIndustrial(buildingType);
            bool hasResidentialSupport = !needsResidential ||
                gridManager.demandManager.CanPlaceCommercialOrIndustrialBuilding(buildingType);

            // Check job requirements for residential
            bool needsJobs = IsResidential(buildingType);
            bool hasJobsAvailable = !needsJobs ||
                gridManager.demandManager.CanPlaceResidentialBuilding();

            // Show warning for various conditions
            if (!hasJobsAvailable)
            {
                ShowDemandWarning(zoneType, demandLevel);
            }
            else if (!hasResidentialSupport)
            {
                ShowDemandWarning(zoneType, demandLevel);
            }
            else if (!canGrow && demandLevel < -10f)
            {
                ShowDemandWarning(zoneType, demandLevel);
            }
        }
    }

    /// <summary>
    /// Zone S toolbar button — enters S placement mode and opens sub-type picker.
    /// </summary>
    public void OnStateServiceZoningButtonClicked()
    {
        ClearCurrentTool();
        selectedZoneType = Zone.ZoneType.StateServiceLightZoning;
        currentSubTypeId = -1;
        OpenSubTypePicker();
        RequestToolbarChromeRefresh();
    }

    public void OnTwoWayRoadButtonClicked()
    {
        ClearCurrentTool();
        selectedZoneType = Zone.ZoneType.Road;
        // cursorManager.SetRoadCursor();
        if (gridManager != null && gridManager.roadManager != null)
        {
            SetGhostPreview(gridManager.roadManager.roadTilePrefab1, 1);
        }
        RequestToolbarChromeRefresh();
    }

    public void OnGrassButtonClicked()
    {
        ClearCurrentTool();
        selectedZoneType = Zone.ZoneType.Grass;
        RequestToolbarChromeRefresh();
    }

    public void OnNuclearPowerPlantButtonClicked()
    {
        ClearCurrentTool();

        GameObject powerPlantObject = Instantiate(powerPlantAPrefab);
        PowerPlant powerPlant = powerPlantObject.AddComponent<PowerPlant>();

        powerPlant.Initialize("Power Plant A", 5000, 100, 50, 25, 3, 20000, powerPlantAPrefab);

        selectedBuilding = powerPlant;

        cursorManager.ShowBuildingPreview(powerPlantAPrefab, 3);
        RequestToolbarChromeRefresh();
    }

    public Zone.ZoneType GetSelectedZoneType()
    {
        return selectedZoneType;
    }

    public IBuilding GetSelectedBuilding()
    {
        return selectedBuilding;
    }

    void ClearSelectedBuilding()
    {
        selectedBuilding = null;
    }

    void ClearSelectedForest()
    {
        selectedForest = null;
        selectedForestData = new ForestSelectionData
        {
            forestType = Forest.ForestType.None,
            prefab = null
        };
    }


    void ClearSelectedZoneType()
    {
        selectedZoneType = Zone.ZoneType.Grass;
    }

    private void ClearCurrentTool()
    {
        bulldozeMode = false;
        detailsMode = false;
        selectedBuilding = null;
        selectedForest = null;
        selectedForestData = new ForestSelectionData
        {
            forestType = Forest.ForestType.None,
            prefab = null
        };
        selectedZoneType = Zone.ZoneType.Grass;
        currentSubTypeId = -1;
        ghostPreviewPrefab = null;
        ghostPreviewSize = 1;
        cursorManager.SetDefaultCursor();
        cursorManager.RemovePreview();
    }

    private void SetGhostPreview(GameObject prefab, int size)
    {
        if (prefab == null)
        {
            ghostPreviewPrefab = null;
            ghostPreviewSize = 1;
            cursorManager.RemovePreview();
            return;
        }

        ghostPreviewPrefab = prefab;
        ghostPreviewSize = size;
        cursorManager.ShowBuildingPreview(prefab, size);
    }

    public void HideGhostPreview()
    {
        cursorManager.RemovePreview();
    }

    public void RestoreGhostPreview()
    {
        if (ghostPreviewPrefab == null)
        {
            return;
        }

        cursorManager.ShowBuildingPreview(ghostPreviewPrefab, ghostPreviewSize);
    }

    public void OnBulldozeButtonClicked()
    {
        ClearCurrentTool();
        cursorManager.SetBullDozerCursor();
        bulldozeMode = true;
        RequestToolbarChromeRefresh();
    }

    public bool isBulldozeMode()
    {
        return bulldozeMode;
    }

    public void OnDetailsButtonClicked()
    {
        bool wasDetailsMode = detailsMode;
        ClearCurrentTool();
        if (wasDetailsMode)
        {
            RequestToolbarChromeRefresh();
            return;
        }
        detailsMode = true;
        cursorManager.SetDetailsCursor();
        RequestToolbarChromeRefresh();
    }

    public void OnRaiseResidentialTaxButtonClicked()
    {
        economyManager.RaiseResidentialTax();
        UpdateUI();
    }

    public void OnLowerResidentialTaxButtonClicked()
    {
        economyManager.LowerResidentialTax();
        UpdateUI();
    }

    public void OnRaiseCommercialTaxButtonClicked()
    {
        economyManager.RaiseCommercialTax();
        UpdateUI();
    }

    public void OnLowerCommercialTaxButtonClicked()
    {
        economyManager.LowerCommercialTax();
        UpdateUI();
    }

    public void OnRaiseIndustrialTaxButtonClicked()
    {
        economyManager.RaiseIndustrialTax();
        UpdateUI();
    }

    public void OnLowerIndustrialTaxButtonClicked()
    {
        economyManager.LowerIndustrialTax();
        UpdateUI();
    }
    #endregion
}
}
