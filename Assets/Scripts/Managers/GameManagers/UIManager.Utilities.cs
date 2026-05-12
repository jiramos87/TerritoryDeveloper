using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using Territory.Buildings;
using Territory.Core;
using Territory.Persistence;
using Territory.Forests;
using Territory.Utilities;

namespace Territory.UI
{
// Tier-B THIN partial — Tier-B ported (Stage 4.5). Pure logic delegates to UIManagerUtilitiesService.
public partial class UIManager
{
    private Domains.UI.Services.UIManagerUtilitiesService _utilitiesService = new Domains.UI.Services.UIManagerUtilitiesService();

    // ─── Cell details ─────────────────────────────────────────────────────────
    public void ShowTileDetails(CityCell cell)
    {
        if (detailsPopupController == null) return;
        var (ct, zt, pop, lv, poll) = Domains.UI.Services.UIManagerUtilitiesService.BuildCellDetailsTuple(cell);
        detailsPopupController.ShowCellDetails(ct, zt, pop, lv, poll);
    }
    public bool IsDetailsMode() => detailsMode;

    // ─── Save / Load ──────────────────────────────────────────────────────────
    public void OnSaveGameButtonClicked()
    {
        gameManager.SaveGame(saveName);
        if (GameSavedText != null) { GameSavedText.gameObject.SetActive(true); Invoke("HideGameSavedText", 3f); }
    }
    public void HideGameSavedText()              { if (GameSavedText != null) GameSavedText.gameObject.SetActive(false); }
    public void OnSavedGameSelected(string path) { CloseLoadGameMenu(); OnLoadGameButtonClicked(path); }
    public void OnLoadGameButtonClicked(string path) => gameManager.LoadGame(path);
    public void OnNewGameButtonClicked()         => gameManager.CreateNewGame();

    public void OnLoadButtonClicked()
    {
        if (loadGameMenu == null) return;
        foreach (Transform child in savedGamesListContainer) Destroy(child.gameObject);
        var entries = new List<(string path, string displayName, System.DateTime sortDate)>();
        foreach (string p in Directory.GetFiles(saveFolderPath, "*.json"))
        { var m = GameSaveManager.GetSaveMetadata(p); entries.Add((p, m.displayName, m.sortDate)); }
        entries.Sort((a, b) => b.sortDate.CompareTo(a.sortDate));
        foreach (var e in entries)
        {
            var btn = Instantiate(savedGameButtonPrefab, savedGamesListContainer);
            btn.GetComponentInChildren<Text>().text = e.displayName;
            btn.GetComponent<Button>().onClick.AddListener(() => OnSavedGameSelected(e.path));
        }
        if (loadMenuFadeRoutine != null) StopCoroutine(loadMenuFadeRoutine);
        RegisterPopupOpened(PopupType.LoadGame);
        loadMenuFadeRoutine = StartCoroutine(OpenLoadGameMenuFadeRoutine());
    }

    public void CloseLoadGameMenu()
    {
        if (loadGameMenu == null || !loadGameMenu.activeSelf) return;
        if (loadMenuFadeRoutine != null) StopCoroutine(loadMenuFadeRoutine);
        loadMenuFadeRoutine = StartCoroutine(CloseLoadGameMenuFadeRoutine());
    }

    private IEnumerator OpenLoadGameMenuFadeRoutine()  { var cg = UiCanvasGroupUtility.EnsureCanvasGroup(loadGameMenu); cg.blocksRaycasts = true; cg.interactable = false; cg.alpha = 0f; loadGameMenu.SetActive(true); yield return UiCanvasGroupUtility.FadeUnscaled(cg, 0f, 1f, PopupFadeDurationSeconds); cg.interactable = true; loadMenuFadeRoutine = null; }
    private IEnumerator CloseLoadGameMenuFadeRoutine() { var cg = loadGameMenu.GetComponent<CanvasGroup>(); if (cg != null) yield return UiCanvasGroupUtility.FadeUnscaled(cg, cg.alpha, 0f, PopupFadeDurationSeconds); loadGameMenu.SetActive(false); loadMenuFadeRoutine = null; }

    // ─── Cursor / Water ───────────────────────────────────────────────────────
    public void RestoreMouseCursor()    { cursorManager.SetDefaultCursor(); cursorManager.RemovePreview(); }
    public void OnPlaceWaterButtonClicked() { ClearCurrentTool(); selectedZoneType = Territory.Zones.Zone.ZoneType.Water; }

    public void OnMediumWaterPumpPlantButtonClicked()
    {
        try
        {
            ClearCurrentTool();
            if (waterPumpPrefab == null) return;
            var obj = Instantiate(waterPumpPrefab);
            var wp  = obj.GetComponent<WaterPlant>() ?? obj.AddComponent<WaterPlant>();
            wp.Initialize("Water Pump", 4000, 80, 30, 20, 2, 16000, waterPumpPrefab);
            selectedBuilding = wp;
            cursorManager.ShowBuildingPreview(waterPumpPrefab, 2);
        }
        catch (System.Exception ex) { Debug.LogError($"Error in OnWaterPumpPlantButtonClicked: {ex.Message}\n{ex.StackTrace}"); }
    }

    // ─── Placement tooltips ───────────────────────────────────────────────────
    public void ShowInsufficientFundsTooltip(string itemType, int cost)
    {
        if (insufficientFundsPanel == null || insufficientFundsText == null) return;
        insufficientFundsText.text = Domains.UI.Services.UIManagerUtilitiesService.BuildInsufficientFundsMessage(itemType, cost, cityStats.money);
        if (hideTooltipCoroutine != null) StopCoroutine(hideTooltipCoroutine);
        hideTooltipCoroutine = StartCoroutine(ShowInsufficientFundsFadeInThenAutoHide());
    }
    public void HideInsufficientFundsTooltip()
    {
        if (hideTooltipCoroutine != null) { StopCoroutine(hideTooltipCoroutine); hideTooltipCoroutine = null; }
        if (insufficientFundsPanel == null || !insufficientFundsPanel.activeSelf) return;
        StartCoroutine(HideInsufficientFundsFadeRoutine());
    }
    private IEnumerator ShowInsufficientFundsFadeInThenAutoHide()  { var cg = UiCanvasGroupUtility.EnsureCanvasGroup(insufficientFundsPanel); cg.blocksRaycasts = true; cg.interactable = true; cg.alpha = 0f; insufficientFundsPanel.SetActive(true); yield return UiCanvasGroupUtility.FadeUnscaled(cg, 0f, 1f, PopupFadeDurationSeconds); hideTooltipCoroutine = null; hideTooltipCoroutine = StartCoroutine(HideTooltipAfterDelay()); }
    private IEnumerator HideTooltipAfterDelay()               { yield return new WaitForSecondsRealtime(tooltipDisplayTime); if (insufficientFundsPanel != null && insufficientFundsPanel.activeSelf) { var cg = insufficientFundsPanel.GetComponent<CanvasGroup>(); if (cg != null) yield return UiCanvasGroupUtility.FadeUnscaled(cg, cg.alpha, 0f, PopupFadeDurationSeconds); insufficientFundsPanel.SetActive(false); } hideTooltipCoroutine = null; }
    private IEnumerator HideInsufficientFundsFadeRoutine()    { var cg = insufficientFundsPanel.GetComponent<CanvasGroup>(); if (cg != null) yield return UiCanvasGroupUtility.FadeUnscaled(cg, cg.alpha, 0f, PopupFadeDurationSeconds); insufficientFundsPanel.SetActive(false); }

    public void ShowPlacementReasonTooltip(PlacementFailReason reason)
    {
        if (reason == PlacementFailReason.None) { HidePlacementReasonTooltip(); return; }
        if (insufficientFundsPanel == null || insufficientFundsText == null) return;
        if (!_utilitiesService.TryGetPlacementMessage(reason, out string msg))
        { Debug.LogWarning($"PlacementReasonStringMap missing entry for {reason}; tooltip suppressed."); HidePlacementReasonTooltip(); return; }
        if (hideTooltipCoroutine != null) { StopCoroutine(hideTooltipCoroutine); hideTooltipCoroutine = null; }
        insufficientFundsText.text = msg;
        var cg = UiCanvasGroupUtility.EnsureCanvasGroup(insufficientFundsPanel);
        cg.blocksRaycasts = false; cg.interactable = false; cg.alpha = 1f;
        insufficientFundsPanel.SetActive(true);
    }
    public void HidePlacementReasonTooltip()
    {
        if (hideTooltipCoroutine != null) { StopCoroutine(hideTooltipCoroutine); hideTooltipCoroutine = null; }
        if (insufficientFundsPanel == null || !insufficientFundsPanel.activeSelf) return;
        insufficientFundsPanel.SetActive(false);
    }

    // ─── Forests ──────────────────────────────────────────────────────────────
    public void OnForestButtonClicked(Forest.ForestType type)
    {
        ClearCurrentTool();
        selectedForestData = new ForestSelectionData { forestType = type, prefab = GetForestPrefabForType(type) };
        selectedForest = null;
        SetGhostPreview(selectedForestData.prefab, 0);
    }
    public GameObject GetForestPrefabForType(Forest.ForestType type)
    { int idx = Domains.UI.Services.UIManagerUtilitiesService.ForestTypeToIndex(type); switch (idx) { case 0: return sparseForestPrefab; case 1: return mediumForestPrefab; default: return denseForestPrefab; } }

    public IForest CreateForestInstance(Forest.ForestType type)
    {
        var go = Instantiate(GetForestPrefabForType(type));
        go.transform.position = new Vector3(-1000, -1000, 0);
        IForest f = null;
        switch (type)
        {
            case Forest.ForestType.Sparse: f = go.GetComponent<SparseForest>() ?? go.AddComponent<SparseForest>(); ((SparseForest)f).Initialize(); break;
            case Forest.ForestType.Medium: f = go.GetComponent<MediumForest>() ?? go.AddComponent<MediumForest>(); ((MediumForest)f).Initialize(); break;
            case Forest.ForestType.Dense:  f = go.GetComponent<DenseForest>()  ?? go.AddComponent<DenseForest>();  ((DenseForest)f).Initialize();  break;
        }
        return f;
    }
    public void OnSparseForestButtonClicked() => OnForestButtonClicked(Forest.ForestType.Sparse);
    public void OnMediumForestButtonClicked() => OnForestButtonClicked(Forest.ForestType.Medium);
    public void OnDenseForestButtonClicked()  => OnForestButtonClicked(Forest.ForestType.Dense);
    public IForest GetSelectedForest()
    {
        if ((selectedForest as UnityEngine.Object) == null) selectedForest = null;
        if (selectedForest == null && selectedForestData.forestType != Forest.ForestType.None)
            selectedForest = CreateForestInstance(selectedForestData.forestType);
        return selectedForest;
    }

    // ─── Demolition ───────────────────────────────────────────────────────────
    public void ShowDemolitionAnimation(GameObject cell, int sortOrder)
    {
        if (demolitionExplosionPrefab == null || cell == null) return;
        Vector3 pos = cell.GetComponent<CityCell>().transformPosition; pos.y += 0.1f;
        SpawnExplosion(pos, sortOrder);
    }
    public void ShowDemolitionAnimationCentered(GameObject centerCell, int buildingSize, int sortOrder)
    {
        if (demolitionExplosionPrefab == null || centerCell == null) { ShowDemolitionAnimation(centerCell, sortOrder); return; }
        Vector3 pos = centerCell.GetComponent<CityCell>().transformPosition;
        if (buildingSize > 1) { float gs = 1.0f; pos.x += (buildingSize - 1) * gs * 0.5f; pos.z += (buildingSize - 1) * gs * 0.5f; }
        pos.y += 0.1f;
        SpawnExplosion(pos, sortOrder);
    }
    private void SpawnExplosion(Vector3 pos, int sortOrder)
    {
        var exp = Instantiate(demolitionExplosionPrefab, pos, Quaternion.identity);
        var sr = exp.GetComponent<SpriteRenderer>();
        if (sr != null) { sr.sortingLayerName = "Effects"; sr.sortingOrder = sortOrder + 1; }
        var da = exp.GetComponent<DemolitionAnimation>(); if (da != null) da.Initialize(pos);
    }

    // ─── Mode exits ───────────────────────────────────────────────────────────
    public void ExitBulldozeMode()         => ClearCurrentTool();
    public void ExitDetailsMode()           => ClearCurrentTool();
    public bool IsBuildingPlacementMode()   => selectedBuilding != null || selectedForest != null || selectedZoneType != Territory.Zones.Zone.ZoneType.Grass;
    public void ExitBuildingPlacementMode() { ClearCurrentTool(); buildingSelectorMenuController.ClosePopup(); buildingSelectorMenuController.DeselectAndUnpressAllButtons(); }
}
}
