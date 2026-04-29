using System.Collections;
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
// Load game, forests, demolition VFX, and misc UI helpers (partial of UIManager).
public partial class UIManager
{
    #region Utility Methods
    /// <summary>
    /// Stage 12 (game-ui-design-system): legacy popup chrome + per-row text writes retired.
    /// Path: assemble 5-tuple → fire <c>OnCellInfoShown</c> via DetailsPopupController →
    /// open themed info-panel via <c>UIManager.Instance.OpenPopup(PopupType.InfoPanel)</c>.
    /// <c>InfoPanelDataAdapter</c> binds tuple → ThemedLabel slots; tab activation included.
    /// </summary>
    public void ShowTileDetails(CityCell cell)
    {
        if (detailsPopupController == null) return;

        string cellType = cell.GetBuildingType();
        string zoneType = cell.GetBuildingName();
        string population = "Occupancy: " + cell.GetPopulation();
        string landValue = "Desirability: " + cell.desirability.ToString("F1");
        string pollution = "Happiness: " + cell.GetHappiness();

        detailsPopupController.ShowCellDetails(cellType, zoneType, population, landValue, pollution);
    }

    public bool IsDetailsMode()
    {
        return detailsMode;
    }

    public void OnSaveGameButtonClicked()
    {
        gameManager.SaveGame(saveName);
        if (GameSavedText != null)
        {
            GameSavedText.gameObject.SetActive(true);
            Invoke("HideGameSavedText", 3f);
        }
    }

    public void HideGameSavedText()
    {
        if (GameSavedText != null)
            GameSavedText.gameObject.SetActive(false);
    }

    public void OnLoadButtonClicked()
    {
        if (loadGameMenu == null)
            return;

        foreach (Transform child in savedGamesListContainer)
        {
            Destroy(child.gameObject);
        }

        string[] saveFiles = Directory.GetFiles(saveFolderPath, "*.json");
        var entries = new List<(string path, string displayName, System.DateTime sortDate)>();

        foreach (string path in saveFiles)
        {
            var meta = GameSaveManager.GetSaveMetadata(path);
            entries.Add((path, meta.displayName, meta.sortDate));
        }

        entries.Sort((a, b) => b.sortDate.CompareTo(a.sortDate));

        foreach (var entry in entries)
        {
            GameObject newButton = Instantiate(savedGameButtonPrefab, savedGamesListContainer);
            newButton.GetComponentInChildren<Text>().text = entry.displayName;
            newButton.GetComponent<Button>().onClick.AddListener(() => OnSavedGameSelected(entry.path));
        }

        if (loadMenuFadeRoutine != null)
            StopCoroutine(loadMenuFadeRoutine);
        RegisterPopupOpened(PopupType.LoadGame);
        loadMenuFadeRoutine = StartCoroutine(OpenLoadGameMenuFadeRoutine());
    }

    private IEnumerator OpenLoadGameMenuFadeRoutine()
    {
        CanvasGroup cg = UiCanvasGroupUtility.EnsureCanvasGroup(loadGameMenu);
        cg.blocksRaycasts = true;
        cg.interactable = false;
        cg.alpha = 0f;
        loadGameMenu.SetActive(true);
        yield return UiCanvasGroupUtility.FadeUnscaled(cg, 0f, 1f, PopupFadeDurationSeconds);
        cg.interactable = true;
        loadMenuFadeRoutine = null;
    }

    // Called when a saved game is selected
    public void OnSavedGameSelected(string saveFilePath)
    {
        CloseLoadGameMenu();
        OnLoadGameButtonClicked(saveFilePath);
    }

    public void OnLoadGameButtonClicked(string saveFilePath)
    {
        gameManager.LoadGame(saveFilePath); // Call the game manager to load the game
    }

    public void CloseLoadGameMenu()
    {
        if (loadGameMenu == null || !loadGameMenu.activeSelf)
            return;
        if (loadMenuFadeRoutine != null)
            StopCoroutine(loadMenuFadeRoutine);
        loadMenuFadeRoutine = StartCoroutine(CloseLoadGameMenuFadeRoutine());
    }

    private IEnumerator CloseLoadGameMenuFadeRoutine()
    {
        CanvasGroup cg = loadGameMenu.GetComponent<CanvasGroup>();
        if (cg != null)
            yield return UiCanvasGroupUtility.FadeUnscaled(cg, cg.alpha, 0f, PopupFadeDurationSeconds);
        loadGameMenu.SetActive(false);
        loadMenuFadeRoutine = null;
    }

    public void OnNewGameButtonClicked()
    {
        gameManager.CreateNewGame();
    }

    public void RestoreMouseCursor()
    {
        cursorManager.SetDefaultCursor();
        cursorManager.RemovePreview();
    }

    public void OnMediumWaterPumpPlantButtonClicked()
    {
        try
        {
            ClearCurrentTool();

            if (waterPumpPrefab == null)
            {
                RequestToolbarChromeRefresh();
                return;
            }

            GameObject waterPlantObject = Instantiate(waterPumpPrefab);
            WaterPlant waterPlant = waterPlantObject.GetComponent<WaterPlant>();
            if (waterPlant == null)
            {
                waterPlant = waterPlantObject.AddComponent<WaterPlant>();
            }

            waterPlant.Initialize("Water Pump", 4000, 80, 30, 20, 2, 16000, waterPumpPrefab);

            selectedBuilding = waterPlant;

            cursorManager.ShowBuildingPreview(waterPumpPrefab, 2);
            RequestToolbarChromeRefresh();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error in OnWaterPumpPlantButtonClicked: {ex.Message}\n{ex.StackTrace}");
        }
    }

    public void OnPlaceWaterButtonClicked()
    {
        ClearCurrentTool();
        selectedZoneType = Zone.ZoneType.Water;
        RequestToolbarChromeRefresh();
    }

    private static readonly System.Collections.Generic.Dictionary<PlacementFailReason, string> PlacementReasonStringMap =
        new System.Collections.Generic.Dictionary<PlacementFailReason, string>
        {
            { PlacementFailReason.Footprint, "Out of bounds or unsupported footprint." }, // TODO: localize
            { PlacementFailReason.Zoning, "Wrong zone for this asset." }, // TODO: localize
            { PlacementFailReason.Locked, "Asset locked — research required." }, // TODO: localize
            { PlacementFailReason.Unaffordable, "Insufficient funds." }, // TODO: localize
            { PlacementFailReason.Occupied, "Cell already occupied." }, // TODO: localize
        };

    public void ShowInsufficientFundsTooltip(string itemType, int cost)
    {
        if (insufficientFundsPanel == null || insufficientFundsText == null)
            return;

        insufficientFundsText.text = $"Cannot afford {itemType}!\nCost: ${cost}\nAvailable: ${cityStats.money}";

        if (hideTooltipCoroutine != null)
            StopCoroutine(hideTooltipCoroutine);
        hideTooltipCoroutine = StartCoroutine(ShowInsufficientFundsFadeInThenAutoHide());
    }

    private IEnumerator ShowInsufficientFundsFadeInThenAutoHide()
    {
        CanvasGroup cg = UiCanvasGroupUtility.EnsureCanvasGroup(insufficientFundsPanel);
        cg.blocksRaycasts = true;
        cg.interactable = true;
        cg.alpha = 0f;
        insufficientFundsPanel.SetActive(true);
        yield return UiCanvasGroupUtility.FadeUnscaled(cg, 0f, 1f, PopupFadeDurationSeconds);
        hideTooltipCoroutine = null;
        hideTooltipCoroutine = StartCoroutine(HideTooltipAfterDelay());
    }

    private IEnumerator HideTooltipAfterDelay()
    {
        yield return new WaitForSecondsRealtime(tooltipDisplayTime);
        if (insufficientFundsPanel != null && insufficientFundsPanel.activeSelf)
        {
            CanvasGroup cg = insufficientFundsPanel.GetComponent<CanvasGroup>();
            if (cg != null)
                yield return UiCanvasGroupUtility.FadeUnscaled(cg, cg.alpha, 0f, PopupFadeDurationSeconds);
            insufficientFundsPanel.SetActive(false);
        }

        hideTooltipCoroutine = null;
    }

    /// <summary>
    /// Hide insufficient-funds overlay with short fade when <see cref="CanvasGroup"/> present.
    /// </summary>
    public void HideInsufficientFundsTooltip()
    {
        if (hideTooltipCoroutine != null)
        {
            StopCoroutine(hideTooltipCoroutine);
            hideTooltipCoroutine = null;
        }

        if (insufficientFundsPanel == null || !insufficientFundsPanel.activeSelf)
            return;
        StartCoroutine(HideInsufficientFundsFadeRoutine());
    }

    private IEnumerator HideInsufficientFundsFadeRoutine()
    {
        CanvasGroup cg = insufficientFundsPanel.GetComponent<CanvasGroup>();
        if (cg != null)
            yield return UiCanvasGroupUtility.FadeUnscaled(cg, cg.alpha, 0f, PopupFadeDurationSeconds);
        insufficientFundsPanel.SetActive(false);
    }

    public void ShowPlacementReasonTooltip(PlacementFailReason reason)
    {
        if (reason == PlacementFailReason.None)
        {
            HidePlacementReasonTooltip();
            return;
        }

        if (insufficientFundsPanel == null || insufficientFundsText == null)
            return;

        if (!PlacementReasonStringMap.TryGetValue(reason, out string msg))
        {
            Debug.LogWarning($"PlacementReasonStringMap missing entry for {reason}; tooltip suppressed.");
            HidePlacementReasonTooltip();
            return;
        }

        if (hideTooltipCoroutine != null)
        {
            StopCoroutine(hideTooltipCoroutine);
            hideTooltipCoroutine = null;
        }

        insufficientFundsText.text = msg;

        CanvasGroup cg = UiCanvasGroupUtility.EnsureCanvasGroup(insufficientFundsPanel);
        cg.blocksRaycasts = false;
        cg.interactable = false;
        cg.alpha = 1f;
        insufficientFundsPanel.SetActive(true);
    }

    public void HidePlacementReasonTooltip()
    {
        if (hideTooltipCoroutine != null)
        {
            StopCoroutine(hideTooltipCoroutine);
            hideTooltipCoroutine = null;
        }

        if (insufficientFundsPanel == null || !insufficientFundsPanel.activeSelf)
            return;
        insufficientFundsPanel.SetActive(false);
    }

    public void OnForestButtonClicked(Forest.ForestType forestType)
    {
        ClearCurrentTool();

        // Don't instantiate yet - just prepare the forest data
        ForestSelectionData forestData = new ForestSelectionData
        {
            forestType = forestType,
            prefab = GetForestPrefabForType(forestType)
        };

        selectedForestData = forestData; // Store selection data instead of instance
        selectedForest = null; // Clear any existing instance

        SetGhostPreview(forestData.prefab, 0);
        RequestToolbarChromeRefresh();
    }

    /// <summary>
    /// Get prefab for forest type.
    /// </summary>
    public GameObject GetForestPrefabForType(Forest.ForestType forestType)
    {
        switch (forestType)
        {
            case Forest.ForestType.Sparse:
                return sparseForestPrefab;
            case Forest.ForestType.Medium:
                return mediumForestPrefab;
            case Forest.ForestType.Dense:
                return denseForestPrefab; // Your existing dense forest prefab
            default:
                return denseForestPrefab;
        }
    }

    /// <summary>
    /// Create forest instance only on actual placement.
    /// </summary>
    public IForest CreateForestInstance(Forest.ForestType forestType)
    {
        GameObject forestPrefab = GetForestPrefabForType(forestType);
        GameObject forestObject = Instantiate(forestPrefab);

        // Move it off-screen initially to prevent visual issues
        forestObject.transform.position = new Vector3(-1000, -1000, 0);

        IForest forest = null;

        switch (forestType)
        {
            case Forest.ForestType.Sparse:
                forest = forestObject.GetComponent<SparseForest>();
                if (forest == null)
                    forest = forestObject.AddComponent<SparseForest>();
                ((SparseForest)forest).Initialize();
                break;

            case Forest.ForestType.Medium:
                forest = forestObject.GetComponent<MediumForest>();
                if (forest == null)
                    forest = forestObject.AddComponent<MediumForest>();
                ((MediumForest)forest).Initialize();
                break;

            case Forest.ForestType.Dense:
                forest = forestObject.GetComponent<DenseForest>();
                if (forest == null)
                    forest = forestObject.AddComponent<DenseForest>();
                ((DenseForest)forest).Initialize();
                break;
        }

        return forest;
    }

    /// <summary>
    /// Backward-compat shims.
    /// </summary>
    public void OnSparseForestButtonClicked()
    {
        OnForestButtonClicked(Forest.ForestType.Sparse);
    }

    public void OnMediumForestButtonClicked()
    {
        OnForestButtonClicked(Forest.ForestType.Medium);
    }

    public void OnDenseForestButtonClicked()
    {
        OnForestButtonClicked(Forest.ForestType.Dense);
    }

    /// <summary>
    /// Get selected forest; create instance if needed.
    /// </summary>
    public IForest GetSelectedForest()
    {
        // Treat destroyed Unity object as null so we create a fresh instance
        if ((selectedForest as UnityEngine.Object) == null)
            selectedForest = null;

        if (selectedForest == null && selectedForestData.forestType != Forest.ForestType.None)
        {
            selectedForest = CreateForestInstance(selectedForestData.forestType);
        }
        return selectedForest;
    }

    public void ShowDemolitionAnimation(GameObject cell, int preCapturedSortingOrder)
    {
        if (demolitionExplosionPrefab == null || cell == null)
        {
            return;
        }

        CityCell centerCell = cell.GetComponent<CityCell>();
        Vector3 explosionPosition = centerCell.transformPosition;
        explosionPosition.y += 0.1f;

        // NOT parented to cell so it won't be destroyed during demolition cleanup
        GameObject explosion = Instantiate(demolitionExplosionPrefab, explosionPosition, Quaternion.identity);

        // Use the pre-captured sorting order (from before demolition reset)
        SpriteRenderer sr = explosion.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sortingLayerName = "Effects";
            sr.sortingOrder = preCapturedSortingOrder + 1;
        }

        DemolitionAnimation demolitionAnim = explosion.GetComponent<DemolitionAnimation>();
        if (demolitionAnim != null)
        {
            demolitionAnim.Initialize(explosionPosition);
        }
    }

    /// <summary>
    /// Show demolition animation for multi-tile buildings at center position.
    /// </summary>
    /// <param name="centerCell">Center cell of demolished building.</param>
    /// <param name="buildingSize">Building size → positioning.</param>
    public void ShowDemolitionAnimationCentered(GameObject centerCell, int buildingSize, int preCapturedSortingOrder)
    {
        if (demolitionExplosionPrefab == null || centerCell == null)
        {
            ShowDemolitionAnimation(centerCell, preCapturedSortingOrder);
            return;
        }
        CityCell cell = centerCell.GetComponent<CityCell>();

        Vector3 explosionPosition = cell.transformPosition;

        // Center the explosion for larger buildings
        if (buildingSize > 1)
        {
            float gridSpacing = 1.0f; // Adjust based on your grid spacing
            explosionPosition.x += (buildingSize - 1) * gridSpacing * 0.5f;
            explosionPosition.z += (buildingSize - 1) * gridSpacing * 0.5f;
        }

        explosionPosition.y += 0.1f;

        GameObject explosion = Instantiate(demolitionExplosionPrefab, explosionPosition, Quaternion.identity);

        SpriteRenderer sr = explosion.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sortingLayerName = "Effects";
            sr.sortingOrder = preCapturedSortingOrder + 1;
        }

        DemolitionAnimation demolitionAnim = explosion.GetComponent<DemolitionAnimation>();
        if (demolitionAnim != null)
        {
            demolitionAnim.Initialize(explosionPosition);
        }
    }

    public void ExitBulldozeMode()
    {
        ClearCurrentTool();
        RequestToolbarChromeRefresh();
    }

    public void ExitDetailsMode()
    {
        ClearCurrentTool();
        RequestToolbarChromeRefresh();
    }

    public bool IsBuildingPlacementMode()
    {
        return selectedBuilding != null || selectedForest != null || selectedZoneType != Zone.ZoneType.Grass;
    }

    public void ExitBuildingPlacementMode()
    {
        ClearCurrentTool();
        buildingSelectorMenuController.ClosePopup();
        buildingSelectorMenuController.DeselectAndUnpressAllButtons();
        RequestToolbarChromeRefresh();
    }
    #endregion
}
}
