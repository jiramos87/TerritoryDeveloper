using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    public CityStats cityStats;
    public CursorManager cursorManager;
    public GridManager gridManager;

    public TimeManager timeManager;

    public EconomyManager economyManager;

    public Text populationText;
    public Text moneyText;
    public Text happinessText;
    public Text gridCoordinatesText;
    public Text cityPowerOutputText;
    public Text cityPowerConsumptionText;

    public Text dateText;

    public Text residentialTaxText;
    public Text commercialTaxText;
    public Text industrialTaxText;

    public Text buttonMoneyText;
    public Text detailsNameText;
    public Text detailsOccupancyText;
    public Text detailsHappinessText;
    public Text detailsPowerOutputText;
    public Text detailsPowerConsumptionText;
    public Text detailsDateBuiltText;
    public Text detailsBuildingTypeText;
    public Text detailsSortingOrderText;
    public Image detailsImage;

    [Header("Selected types")]
    private Zone.ZoneType selectedZoneType;
    private IBuilding selectedBuilding;
    private IForest selectedForest;

    public GameObject powerPlantAPrefab;
    public DetailsPopupController detailsPopupController;

    public bool bulldozeMode;
    public bool detailsMode;

    public GameManager gameManager;

    public string saveName;

    public GameObject loadGameMenu;
    public Transform savedGamesListContainer;
    public GameObject savedGameButtonPrefab;

    private string saveFolderPath;

    public Text GameSavedText;

    [Header("Employment UI")]
   public Text unemploymentRateText;
   public Text totalJobsText;
   public Text demandResidentialText;
   public Text demandCommercialText;
   public Text demandIndustrialText;

   [Header("Demand UI")]
    public Text demandFeedbackText; // Shows demand status for selected zone
    public GameObject demandWarningPanel; // Warning when placing zones with low demand

    [Header("Enhanced Employment UI")]
    public Text totalJobsCreatedText; // Total jobs created by buildings
    public Text availableJobsText; // Jobs available (not taken by residents)
    public Text jobsTakenText; // Jobs taken by residents

    public Text cityWaterOutputText;
    public Text cityWaterConsumptionText;

    public GameObject waterPumpPrefab;

    [Header("Insufficient Funds Warning")]
    public GameObject insufficientFundsPanel;
    public Text insufficientFundsText;
    public float tooltipDisplayTime = 3f;
    private Coroutine hideTooltipCoroutine;

    public GameObject denseForestPrefab;
    public GameObject mediumForestPrefab;
    public GameObject sparseForestPrefab;

    private ForestSelectionData selectedForestData;

    void Start()
    {
        if (cityStats == null)
        {
            Debug.LogError("CityStats component not found.");
        }

        selectedZoneType = Zone.ZoneType.Grass;
        bulldozeMode = false;

        saveFolderPath = Application.persistentDataPath;
    }

    void Update()
    {
        if (cityStats != null)
        {
            UpdateUI();
        }

        // Check if the Escape key is pressed
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // Check if the load game panel is currently active
            if (loadGameMenu.activeSelf)
            {
                CloseLoadGameMenu();
            }
        }
    }

    public void UpdateUI()
    {
        populationText.text = cityStats.population.ToString();
        moneyText.text = cityStats.money.ToString();
        buttonMoneyText.text = cityStats.money.ToString();
        happinessText.text = cityStats.happiness.ToString();

        cityPowerOutputText.text = cityStats.cityPowerOutput.ToString() + " MW";
        cityPowerConsumptionText.text = cityStats.cityPowerConsumption.ToString() + " MW";
        cityWaterOutputText.text = cityStats.cityWaterOutput.ToString() + " kL";
        cityWaterConsumptionText.text = cityStats.cityWaterConsumption.ToString() + " kL";
        
        dateText.text = timeManager.GetCurrentDate().Date.ToString();
        residentialTaxText.text = "Residential Tax: " + economyManager.GetResidentialTax() + "%";
        commercialTaxText.text = "Commercial Tax: " + economyManager.GetCommercialTax() + "%";
        industrialTaxText.text = "Industrial Tax: " + economyManager.GetIndustrialTax() + "%";
        gridCoordinatesText.text = "x: " + gridManager.mouseGridPosition.x + ", y: " + gridManager.mouseGridPosition.y;

        EmploymentManager employment = FindObjectOfType<EmploymentManager>();
        DemandManager demand = FindObjectOfType<DemandManager>();
        StatisticsManager stats = FindObjectOfType<StatisticsManager>();
        
        if (employment != null)
        {
            unemploymentRateText.text = employment.unemploymentRate.ToString("F1") + "%";
            totalJobsText.text = employment.GetAvailableJobs().ToString();
            
            if (totalJobsCreatedText != null)
                totalJobsCreatedText.text = employment.GetTotalJobs().ToString();
            if (availableJobsText != null)
                availableJobsText.text = employment.GetAvailableJobs().ToString();
            if (jobsTakenText != null)
                jobsTakenText.text = employment.GetJobsTakenByResidents().ToString();
        }
        
        if (demand != null)
        {
            demandResidentialText.text = demand.GetResidentialDemand().demandStatus + 
                " (" + demand.GetResidentialDemand().demandLevel.ToString("F0") + ")";
            demandCommercialText.text = demand.GetCommercialDemand().demandStatus + 
                " (" + demand.GetCommercialDemand().demandLevel.ToString("F0") + ")";
            demandIndustrialText.text = demand.GetIndustrialDemand().demandStatus + 
                " (" + demand.GetIndustrialDemand().demandLevel.ToString("F0") + ")";
        }
        
        // Update demand feedback for selected zone type
        UpdateDemandFeedback();
    }

    private void UpdateDemandFeedback()
    {
        if (demandFeedbackText == null || gridManager == null) return;
        
        Zone.ZoneType selectedZone = GetSelectedZoneType();
        if (selectedZone == Zone.ZoneType.Grass || selectedZone == Zone.ZoneType.Road)
        {
            demandFeedbackText.text = "";
            return;
        }
        
        string feedback = gridManager.GetDemandFeedback(selectedZone);
        demandFeedbackText.text = feedback;
        
        // Enhanced color coding for demand levels
        if (feedback.Contains("✓"))
        {
            demandFeedbackText.color = Color.green;
        }
        else if (feedback.Contains("No Jobs Available"))
        {
            demandFeedbackText.color = Color.red; // Critical error - no jobs for residents
        }
        else if (feedback.Contains("Need Residents"))
        {
            demandFeedbackText.color = Color.yellow; // Warning color for residential requirement
        }
        else if (feedback.Contains("✗"))
        {
            demandFeedbackText.color = Color.red;
        }
        else
        {
            demandFeedbackText.color = Color.white;
        }
    }

    public void ShowDemandWarning(Zone.ZoneType zoneType, float demandLevel)
    {
        if (demandWarningPanel != null)
        {
            demandWarningPanel.SetActive(true);
            
            Text warningText = demandWarningPanel.GetComponentInChildren<Text>();
            if (warningText != null)
            {
                string message = "";
                
                // Check if it's residential that needs jobs
                Zone.ZoneType buildingType = GetBuildingTypeFromZoning(zoneType);
                if (IsResidential(buildingType) && 
                    gridManager.demandManager != null && 
                    !gridManager.demandManager.CanPlaceResidentialBuilding())
                {
                    message = $"Cannot place {zoneType}\nNo jobs available for residents!\nBuild commercial/industrial buildings first.";
                }
                // Check if it's a commercial/industrial that needs residents
                else if (IsCommercialOrIndustrial(buildingType) && 
                    gridManager.demandManager != null && 
                    !gridManager.demandManager.CanPlaceCommercialOrIndustrialBuilding(buildingType))
                {
                    message = $"Cannot place {zoneType}\nNeed residential buildings first!\nCommercial/Industrial requires residents to operate.";
                }
                else if (demandLevel < 0)
                {
                    message = $"Warning: Low demand for {zoneType}\nDemand Level: {demandLevel:F0}%\nBuildings may not develop quickly.";
                }
                else
                {
                    message = $"Placing {zoneType}\nDemand Level: {demandLevel:F0}%";
                }
                
                warningText.text = message;
            }
            
            // Auto-hide warning after 4 seconds (longer for important messages)
            Invoke("HideDemandWarning", 4f);
        }
    }

    public void HideDemandWarning()
    {
        if (demandWarningPanel != null)
        {
            demandWarningPanel.SetActive(false);
        }
    }

    private Zone.ZoneType GetBuildingTypeFromZoning(Zone.ZoneType zoneType)
    {
        switch (zoneType)
        {
            case Zone.ZoneType.ResidentialLightZoning: return Zone.ZoneType.ResidentialLightBuilding;
            case Zone.ZoneType.ResidentialMediumZoning: return Zone.ZoneType.ResidentialMediumBuilding;
            case Zone.ZoneType.ResidentialHeavyZoning: return Zone.ZoneType.ResidentialHeavyBuilding;
            case Zone.ZoneType.CommercialLightZoning: return Zone.ZoneType.CommercialLightBuilding;
            case Zone.ZoneType.CommercialMediumZoning: return Zone.ZoneType.CommercialMediumBuilding;
            case Zone.ZoneType.CommercialHeavyZoning: return Zone.ZoneType.CommercialHeavyBuilding;
            case Zone.ZoneType.IndustrialLightZoning: return Zone.ZoneType.IndustrialLightBuilding;
            case Zone.ZoneType.IndustrialMediumZoning: return Zone.ZoneType.IndustrialMediumBuilding;
            case Zone.ZoneType.IndustrialHeavyZoning: return Zone.ZoneType.IndustrialHeavyBuilding;
            default: return zoneType;
        }
    }

    private bool IsResidential(Zone.ZoneType zoneType)
    {
        return zoneType == Zone.ZoneType.ResidentialLightBuilding ||
               zoneType == Zone.ZoneType.ResidentialMediumBuilding ||
               zoneType == Zone.ZoneType.ResidentialHeavyBuilding;
    }
    
    private bool IsCommercialOrIndustrial(Zone.ZoneType zoneType)
    {
        return zoneType == Zone.ZoneType.CommercialLightBuilding ||
               zoneType == Zone.ZoneType.CommercialMediumBuilding ||
               zoneType == Zone.ZoneType.CommercialHeavyBuilding ||
               zoneType == Zone.ZoneType.IndustrialLightBuilding ||
               zoneType == Zone.ZoneType.IndustrialMediumBuilding ||
               zoneType == Zone.ZoneType.IndustrialHeavyBuilding;
    }

    public void OnLightResidentialButtonClicked()
    {
        selectedZoneType = Zone.ZoneType.ResidentialLightZoning;
        cursorManager.SetDefaultCursor();
        bulldozeMode = false;
        ClearSelectedBuilding();
        ClearSelectedForest();
        CheckAndShowDemandFeedback(selectedZoneType);
    }

    public void OnMediumResidentialButtonClicked()
    {
        selectedZoneType = Zone.ZoneType.ResidentialMediumZoning;
        cursorManager.SetDefaultCursor();
        bulldozeMode = false;
        ClearSelectedBuilding();
        ClearSelectedForest();
        CheckAndShowDemandFeedback(selectedZoneType);
    }

    public void OnHeavyResidentialButtonClicked()
    {
        selectedZoneType = Zone.ZoneType.ResidentialHeavyZoning;
        cursorManager.SetDefaultCursor();
        bulldozeMode = false;
        ClearSelectedBuilding();
        ClearSelectedForest();
        CheckAndShowDemandFeedback(selectedZoneType);
    }

    public void OnLightCommercialButtonClicked()
    {
        selectedZoneType = Zone.ZoneType.CommercialLightZoning;
        cursorManager.SetDefaultCursor();
        bulldozeMode = false;
        ClearSelectedBuilding();
        ClearSelectedForest();
        CheckAndShowDemandFeedback(selectedZoneType);
    }

    public void OnMediumCommercialButtonClicked()
    {
        selectedZoneType = Zone.ZoneType.CommercialMediumZoning;
        cursorManager.SetDefaultCursor();
        bulldozeMode = false;
        ClearSelectedBuilding();
        ClearSelectedForest();
        CheckAndShowDemandFeedback(selectedZoneType);
    }

    public void OnHeavyCommercialButtonClicked()
    {
        selectedZoneType = Zone.ZoneType.CommercialHeavyZoning;
        cursorManager.SetDefaultCursor();
        bulldozeMode = false;
        ClearSelectedBuilding();
        ClearSelectedForest();
        CheckAndShowDemandFeedback(selectedZoneType);
    }

    public void OnLightIndustrialButtonClicked()
    {
        Debug.Log("Light Industrial Button Clicked");
        selectedZoneType = Zone.ZoneType.IndustrialLightZoning;
        cursorManager.SetDefaultCursor();
        bulldozeMode = false;
        ClearSelectedBuilding();
        ClearSelectedForest();
        CheckAndShowDemandFeedback(selectedZoneType);
    }

    public void OnMediumIndustrialButtonClicked()
    {
        selectedZoneType = Zone.ZoneType.IndustrialMediumZoning;
        cursorManager.SetDefaultCursor();
        bulldozeMode = false;
        ClearSelectedBuilding();
        ClearSelectedForest();
        CheckAndShowDemandFeedback(selectedZoneType);
    }

    public void OnHeavyIndustrialButtonClicked()
    {
        selectedZoneType = Zone.ZoneType.IndustrialHeavyZoning;
        cursorManager.SetDefaultCursor();
        bulldozeMode = false;
        ClearSelectedBuilding();
        ClearSelectedForest();
        CheckAndShowDemandFeedback(selectedZoneType);
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

    public void OnTwoWayRoadButtonClicked()
    {
        selectedZoneType = Zone.ZoneType.Road;
        // cursorManager.SetRoadCursor();

        ClearSelectedBuilding();
        ClearSelectedForest();
        cursorManager.SetDefaultCursor();
        bulldozeMode = false;
    }

    public void OnGrassButtonClicked()
    {
        selectedZoneType = Zone.ZoneType.Grass;
        cursorManager.SetDefaultCursor();
        bulldozeMode = false;
        ClearSelectedBuilding();
        ClearSelectedForest();
    }

    public void OnNuclearPowerPlantButtonClicked()
    {
        ClearSelectedZoneType();
        ClearSelectedForest();
        
        GameObject powerPlantObject = Instantiate(powerPlantAPrefab);
        PowerPlant powerPlant = powerPlantObject.AddComponent<PowerPlant>();
        
        powerPlant.Initialize("Power Plant A", 10000, 100, 50, 25, 3, 10000, powerPlantAPrefab);
        
        selectedBuilding = powerPlant;
        
        cursorManager.SetDefaultCursor();
        
        cursorManager.ShowBuildingPreview(powerPlantAPrefab, 3);
        
        bulldozeMode = false;
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

    public void OnBulldozeButtonClicked()
    {
        ClearSelectedBuilding();
        ClearSelectedZoneType();
        ClearSelectedForest();
        cursorManager.SetBullDozerCursor();
        bulldozeMode = true;
    }

    public bool isBulldozeMode()
    {
      return bulldozeMode;
    }

    public void OnDetailsButtonClicked()
    {
        cursorManager.SetDetailsCursor();
        detailsMode = !detailsMode;
    }

    public void OnRaiseResidentialTaxButtonClicked()
    {
        economyManager.RaiseResidentialTax();
    }

    public void OnLowerResidentialTaxButtonClicked()
    {
        economyManager.LowerResidentialTax();
    }

    public void OnRaiseCommercialTaxButtonClicked()
    {
        economyManager.RaiseCommercialTax();
    }

    public void OnLowerCommercialTaxButtonClicked()
    {
        economyManager.LowerCommercialTax();
    }

    public void OnRaiseIndustrialTaxButtonClicked()
    {
        economyManager.RaiseIndustrialTax();
    }

    public void OnLowerIndustrialTaxButtonClicked()
    {
        economyManager.LowerIndustrialTax();
    }

    public void ShowTileDetails(Cell cell)
    {
        detailsPopupController.ShowDetails();
        detailsNameText.text = cell.GetBuildingName();
        detailsOccupancyText.text = "Occupancy: " + cell.GetPopulation();
        detailsHappinessText.text = "Happiness: " + cell.GetHappiness();
        detailsPowerOutputText.text = "Power Output: " + cell.GetPowerOutput() + " MW";
        detailsPowerConsumptionText.text = "Power Consumption: " + cell.GetPowerConsumption() + " MW";
        
        // Add water consumption information
        if (detailsPopupController.waterConsumptionText != null)
            detailsPopupController.waterConsumptionText.text = "Water Consumption: " + cell.GetWaterConsumption() + " kL";
        
        // Add water output information for water plants
        if (cell.waterPlant != null && detailsPopupController.waterOutputText != null)
            detailsPopupController.waterOutputText.text = "Water Output: " + cell.waterPlant.WaterOutput + " kL";
        
        // detailsDateBuiltText.text = "Date Built: " + timeManager.GetCurrentDate();
        detailsBuildingTypeText.text = "Building Type: " + cell.GetBuildingType();
        detailsImage.sprite = cell.GetCellPrefab().GetComponent<SpriteRenderer>().sprite;
        detailsSortingOrderText.text = "Sorting Order: " + cell.GetSortingOrder();
    }

    public bool IsDetailsMode()
    {
        return detailsMode;
    }

    public void OnSaveGameButtonClicked()
    {
        gameManager.SaveGame(saveName);
        // Show the game saved text for 3 seconds

        GameSavedText.gameObject.SetActive(true);

        Invoke("HideGameSavedText", 3f);
    }

    public void HideGameSavedText()
    {
        GameSavedText.gameObject.SetActive(false);
    }

    public void OnLoadButtonClicked()
    {
        loadGameMenu.SetActive(true);

        foreach (Transform child in savedGamesListContainer)
        {
            Destroy(child.gameObject);
        }

        string[] saveFiles = Directory.GetFiles(saveFolderPath, "*.json");

        foreach (string saveFile in saveFiles)
        {
            GameObject newButton = Instantiate(savedGameButtonPrefab, savedGamesListContainer);
            newButton.GetComponentInChildren<Text>().text = Path.GetFileNameWithoutExtension(saveFile);

            newButton.GetComponent<Button>().onClick.AddListener(() => OnSavedGameSelected(saveFile));
        }
    }

    // Called when a saved game is selected
    public void OnSavedGameSelected(string saveFilePath)
    {
        loadGameMenu.SetActive(false); // Close the load game menu
        OnLoadGameButtonClicked(saveFilePath); // Load the selected game
    }

    public void OnLoadGameButtonClicked(string saveFilePath)
    {
        gameManager.LoadGame(saveFilePath); // Call the game manager to load the game
    }

    public void CloseLoadGameMenu()
    {
        loadGameMenu.SetActive(false);
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
        try {
            ClearSelectedZoneType();
            
            if (waterPumpPrefab == null) {
                return;
            }

            GameObject waterPlantObject = Instantiate(waterPumpPrefab);
            WaterPlant waterPlant = waterPlantObject.GetComponent<WaterPlant>();
            if (waterPlant == null) {
                waterPlant = waterPlantObject.AddComponent<WaterPlant>();
            }
            
            waterPlant.Initialize("Water Pump", 8000, 80, 30, 20, 2, 8000, waterPumpPrefab);

            selectedBuilding = waterPlant;
        
            cursorManager.SetDefaultCursor();
            cursorManager.ShowBuildingPreview(waterPumpPrefab, 2);
            bulldozeMode = false;
        }
        catch (System.Exception ex) {
            Debug.LogError($"Error in OnWaterPumpPlantButtonClicked: {ex.Message}\n{ex.StackTrace}");
        }
    }

    public void OnPlaceWaterButtonClicked()
    {
        selectedZoneType = Zone.ZoneType.Water;
        cursorManager.SetDefaultCursor();
        bulldozeMode = false;
        ClearSelectedBuilding();
        ClearSelectedForest();
    }

    public void ShowInsufficientFundsTooltip(string itemType, int cost)
    {
        if (insufficientFundsPanel == null || insufficientFundsText == null) return;
        
        insufficientFundsText.text = $"Cannot afford {itemType}!\nCost: ${cost}\nAvailable: ${cityStats.money}";
        insufficientFundsPanel.SetActive(true);
        
        // Cancel any existing hide coroutine
        if (hideTooltipCoroutine != null)
            StopCoroutine(hideTooltipCoroutine);
        
        // Start a new hide coroutine
        hideTooltipCoroutine = StartCoroutine(HideTooltipAfterDelay());
    }

    private System.Collections.IEnumerator HideTooltipAfterDelay()
    {
        yield return new WaitForSeconds(tooltipDisplayTime);
        insufficientFundsPanel.SetActive(false);
        hideTooltipCoroutine = null;
    }

    // Use this to hide the tooltip manually if needed
    public void HideInsufficientFundsTooltip()
    {
        if (insufficientFundsPanel != null)
            insufficientFundsPanel.SetActive(false);
        
        if (hideTooltipCoroutine != null)
        {
            StopCoroutine(hideTooltipCoroutine);
            hideTooltipCoroutine = null;
        }
    }

    public void OnForestButtonClicked(Forest.ForestType forestType)
    {
        ClearSelectedZoneType();
        ClearSelectedBuilding();
        
        // Don't instantiate yet - just prepare the forest data
        ForestSelectionData forestData = new ForestSelectionData
        {
            forestType = forestType,
            prefab = GetForestPrefabForType(forestType)
        };
        
        selectedForestData = forestData; // Store selection data instead of instance
        selectedForest = null; // Clear any existing instance

        cursorManager.SetDefaultCursor();
        cursorManager.ShowBuildingPreview(forestData.prefab, 0);
        bulldozeMode = false;
    }

    /// <summary>
    /// Get the appropriate prefab for forest type
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
    /// Create forest instance only when actually placing
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
    /// Specific methods for backward compatibility
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
    /// Get currently selected forest, creating instance if needed
    /// </summary>
    public IForest GetSelectedForest()
    {
        if (selectedForest == null && selectedForestData.forestType != Forest.ForestType.None)
        {
            selectedForest = CreateForestInstance(selectedForestData.forestType);
        }
        return selectedForest;
    }
}

[System.Serializable]
public struct ForestSelectionData
{
    public Forest.ForestType forestType;
    public GameObject prefab;
}