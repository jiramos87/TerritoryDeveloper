using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Manages the city statistics UI panel using UI Toolkit
/// Integrates with the existing CityStats system from the city builder
/// </summary>
public class CityStatsUIController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private UIDocument uiDocument;
    
    [Header("Game System References")]
    [SerializeField] private CityStats cityStats;
    [SerializeField] private EconomyManager economyManager;
    
    // Auto-find system references if not manually assigned
    void Awake()
    {
        // Try to find systems automatically if not assigned
        if (cityStats == null)
            cityStats = FindObjectOfType<CityStats>();
        if (economyManager == null)
            economyManager = FindObjectOfType<EconomyManager>();
            
        if (cityStats == null)
            Debug.LogWarning("CityStats not found! Please assign manually or ensure CityStats exists in scene.");
        if (economyManager == null)
            Debug.LogWarning("EconomyManager not found! Please assign manually or ensure EconomyManager exists in scene.");
    }
    
    // UI Element references
    private Label populationLabel;
    private Label happinessLabel;
    private Label treasuryLabel;
    private Label unemploymentLabel;
    private VisualElement statsContainer;
    private Button toggleStatsButton;

    private bool isStatsVisible = true;

    void Start()
    {
        InitializeUI();
        SetupEventHandlers();
    }

    void Update()
    {
        // Update UI every frame (you might want to optimize this)
        UpdateStatisticsDisplay();
    }

    /// <summary>
    /// Initialize the UI elements programmatically
    /// </summary>
    private void InitializeUI()
    {
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();

        var root = uiDocument.rootVisualElement;

        // Create main stats container
        statsContainer = new VisualElement();
        statsContainer.name = "stats-container";
        SetupStatsContainerStyle(statsContainer);

        // Create title
        var titleLabel = new Label("City Statistics");
        SetupTitleStyle(titleLabel);

        // Create individual stat labels
        populationLabel = CreateStatLabel("Population", "0");
        happinessLabel = CreateStatLabel("Happiness", "0%");
        treasuryLabel = CreateStatLabel("Treasury", "$0");
        unemploymentLabel = CreateStatLabel("Unemployment", "0%");

        // Create toggle button
        toggleStatsButton = new Button(ToggleStatsVisibility);
        toggleStatsButton.text = "Hide Stats";
        SetupToggleButtonStyle(toggleStatsButton);

        // Build hierarchy
        statsContainer.Add(titleLabel);
        statsContainer.Add(populationLabel);
        statsContainer.Add(happinessLabel);
        statsContainer.Add(treasuryLabel);
        statsContainer.Add(unemploymentLabel);
        
        root.Add(statsContainer);
        root.Add(toggleStatsButton);
    }

    /// <summary>
    /// Create a formatted stat label with title and value
    /// </summary>
    private Label CreateStatLabel(string statName, string initialValue)
    {
        var label = new Label($"{statName}: {initialValue}");
        label.name = $"{statName.ToLower()}-label";
        
        // Style the label
        label.style.fontSize = 16;
        label.style.color = Color.white;
        label.style.marginBottom = 5;
        label.style.paddingLeft = 10;
        label.style.paddingRight = 10;
        
        return label;
    }

    /// <summary>
    /// Setup styling for the stats container
    /// </summary>
    private void SetupStatsContainerStyle(VisualElement container)
    {
        container.style.position = Position.Absolute;
        container.style.top = 20;
        container.style.left = 20;
        container.style.width = 250;
        container.style.backgroundColor = new Color(0, 0, 0, 0.8f);
        container.style.borderTopWidth = 2;
        container.style.borderBottomWidth = 2;
        container.style.borderLeftWidth = 2;
        container.style.borderRightWidth = 2;
        container.style.borderTopColor = Color.cyan;
        container.style.borderBottomColor = Color.cyan;
        container.style.borderLeftColor = Color.cyan;
        container.style.borderRightColor = Color.cyan;
        container.style.borderTopLeftRadius = 5;
        container.style.borderTopRightRadius = 5;
        container.style.borderBottomLeftRadius = 5;
        container.style.borderBottomRightRadius = 5;
        container.style.paddingTop = 10;
        container.style.paddingBottom = 10;
        container.style.paddingLeft = 5;
        container.style.paddingRight = 5;
    }

    /// <summary>
    /// Setup styling for the title label
    /// </summary>
    private void SetupTitleStyle(Label titleLabel)
    {
        titleLabel.style.fontSize = 20;
        titleLabel.style.color = Color.cyan;
        titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        titleLabel.style.alignSelf = Align.Center;
        titleLabel.style.marginBottom = 10;
    }

    /// <summary>
    /// Setup styling for the toggle button
    /// </summary>
    private void SetupToggleButtonStyle(Button button)
    {
        button.style.position = Position.Absolute;
        button.style.top = 20;
        button.style.right = 20;
        button.style.width = 100;
        button.style.height = 30;
        button.style.backgroundColor = new Color(0.2f, 0.5f, 0.8f);
        button.style.color = Color.white;
        button.style.borderTopLeftRadius = 5;
        button.style.borderTopRightRadius = 5;
        button.style.borderBottomLeftRadius = 5;
        button.style.borderBottomRightRadius = 5;
    }

    /// <summary>
    /// Setup event handlers for UI interactions
    /// </summary>
    private void SetupEventHandlers()
    {
        // Add hover effects to stats container
        statsContainer.RegisterCallback<MouseEnterEvent>(OnStatsMouseEnter);
        statsContainer.RegisterCallback<MouseLeaveEvent>(OnStatsMouseLeave);
    }

    /// <summary>
    /// Update the statistics display with current game data
    /// </summary>
    private void UpdateStatisticsDisplay()
    {
        if (cityStats == null || economyManager == null) return;

        try
        {
            // Update population - adjust method name based on your CityStats implementation
            int population = GetPopulation();
            populationLabel.text = $"Population: {population:N0}";

            // Update happiness - adjust based on your implementation
            float happiness = GetHappiness();
            happinessLabel.style.color = GetHappinessColor(happiness);
            happinessLabel.text = $"Happiness: {happiness:F1}%";

            // Update treasury - adjust based on your EconomyManager implementation
            int treasury = GetTreasury();
            treasuryLabel.text = $"Treasury: ${treasury:N0}";
            treasuryLabel.style.color = treasury >= 0 ? Color.green : Color.red;

            // Update unemployment - adjust based on your implementation
            float unemploymentRate = GetUnemploymentRate();
            unemploymentLabel.text = $"Unemployment: {unemploymentRate:F1}%";
            unemploymentLabel.style.color = GetUnemploymentColor(unemploymentRate);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Error updating UI stats: {e.Message}");
        }
    }

    /// <summary>
    /// Get population from CityStats - modify this method to match your actual implementation
    /// </summary>
    private int GetPopulation()
    {
        // Try different common method names - uncomment the one that matches your implementation
        
        // Option 1: If you have a direct population field/property
        // return cityStats.totalPopulation;
        
        // Option 2: If you have a GetTotalPopulation method
        // return cityStats.GetTotalPopulation();
        
        // Option 3: If you calculate from residential buildings
        // return cityStats.CalculatePopulation();
        
        // Placeholder - replace with your actual implementation
        return 0;
    }

    /// <summary>
    /// Get happiness from CityStats - modify this method to match your actual implementation
    /// </summary>
    private float GetHappiness()
    {
        // Try different common method names - uncomment the one that matches your implementation
        
        // Option 1: If you have a happiness field/property
        // return cityStats.averageHappiness;
        
        // Option 2: If you have a GetAverageHappiness method
        // return cityStats.GetAverageHappiness();
        
        // Option 3: If you calculate happiness differently
        // return cityStats.CalculateHappiness();
        
        // Placeholder - replace with your actual implementation
        return 50.0f;
    }

    /// <summary>
    /// Get treasury from EconomyManager - modify this method to match your actual implementation
    /// </summary>
    private int GetTreasury()
    {
        // Try different common method names - uncomment the one that matches your implementation
        
        // Option 1: If you have a treasury field/property
        // return economyManager.treasury;
        
        // Option 2: If you have a GetTreasury method
        // return economyManager.GetTreasury();
        
        // Option 3: If it's named differently
        // return economyManager.GetCurrentMoney();
        // return economyManager.money;
        
        // Placeholder - replace with your actual implementation
        return 10000;
    }

    /// <summary>
    /// Get unemployment rate - modify this method to match your actual implementation
    /// </summary>
    private float GetUnemploymentRate()
    {
        // Try different approaches - uncomment the one that matches your implementation
        
        // Option 1: If you have a direct unemployment rate method
        // return cityStats.GetUnemploymentRate();
        
        // Option 2: If you calculate from jobs and population
        // int totalJobs = cityStats.GetTotalJobs();
        // int population = cityStats.GetWorkingPopulation();
        // return population > 0 ? ((population - totalJobs) / (float)population) * 100f : 0f;
        
        // Placeholder - replace with your actual implementation
        return 5.0f;
    }

    /// <summary>
    /// Get color based on happiness level
    /// </summary>
    private Color GetHappinessColor(float happiness)
    {
        if (happiness >= 80f) return Color.green;
        if (happiness >= 60f) return Color.yellow;
        if (happiness >= 40f) return new Color(1f, 0.5f, 0f); // Orange
        return Color.red;
    }

    /// <summary>
    /// Get color based on unemployment rate
    /// </summary>
    private Color GetUnemploymentColor(float unemploymentRate)
    {
        if (unemploymentRate <= 3f) return Color.green;
        if (unemploymentRate <= 6f) return Color.yellow;
        if (unemploymentRate <= 10f) return new Color(1f, 0.5f, 0f); // Orange
        return Color.red;
    }

    /// <summary>
    /// Toggle the visibility of the stats panel
    /// </summary>
    private void ToggleStatsVisibility()
    {
        isStatsVisible = !isStatsVisible;
        statsContainer.style.display = isStatsVisible ? DisplayStyle.Flex : DisplayStyle.None;
        toggleStatsButton.text = isStatsVisible ? "Hide Stats" : "Show Stats";
    }

    /// <summary>
    /// Handle mouse entering the stats area (visual feedback)
    /// </summary>
    private void OnStatsMouseEnter(MouseEnterEvent evt)
    {
        statsContainer.style.backgroundColor = new Color(0, 0, 0, 0.9f);
    }

    /// <summary>
    /// Handle mouse leaving the stats area (visual feedback)
    /// </summary>
    private void OnStatsMouseLeave(MouseLeaveEvent evt)
    {
        statsContainer.style.backgroundColor = new Color(0, 0, 0, 0.8f);
    }

    /// <summary>
    /// Add a new custom stat to the display
    /// </summary>
    public void AddCustomStat(string statName, string value, Color textColor = default)
    {
        var customLabel = CreateStatLabel(statName, value);
        if (textColor != default(Color))
            customLabel.style.color = textColor;
        
        statsContainer.Add(customLabel);
    }

    /// <summary>
    /// Remove a custom stat from the display
    /// </summary>
    public void RemoveCustomStat(string statName)
    {
        var labelToRemove = statsContainer.Q<Label>($"{statName.ToLower()}-label");
        if (labelToRemove != null)
        {
            statsContainer.Remove(labelToRemove);
        }
    }
}
