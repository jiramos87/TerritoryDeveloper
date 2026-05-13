// long-file-allowed: legacy hub — scope outside current atomization plan; deferred to future sweep
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;
using Territory.Economy;

namespace Territory.UI
{
/// <summary>
/// Manage city stats UI panel via UI Toolkit. Integrates with existing <see cref="CityStats"/> system.
/// </summary>
public class CityStatsUIController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private UIDocument uiDocument;

    [Header("Game System References")]
    [SerializeField] private CityStats cityStats;
    [SerializeField] private EconomyManager economyManager;
    [SerializeField] private ZoneSubTypeRegistry zoneSubTypeRegistry;

    // Auto-find system references if not manually assigned
    void Awake()
    {
        // Try to find systems automatically if not assigned
        if (cityStats == null)
            cityStats = FindObjectOfType<CityStats>();
        if (economyManager == null)
            economyManager = FindObjectOfType<EconomyManager>();
        if (zoneSubTypeRegistry == null)
            zoneSubTypeRegistry = FindObjectOfType<ZoneSubTypeRegistry>();
    }

    // UI Element references
    private Label populationLabel;
    private Label happinessLabel;
    private Label treasuryLabel;
    private Label unemploymentLabel;
    private Label envelopeCapLabel;
    private Label envelopeRemainingLabel;
    private VisualElement statsContainer;
    private Button toggleStatsButton;

    private bool isStatsVisible = true;

    void Start()
    {
        InitializeUI();
        SetupEventHandlers();
    }

    void OnDestroy()
    {
        if (statsContainer != null)
        {
            statsContainer.UnregisterCallback<MouseEnterEvent>(OnStatsMouseEnter);
            statsContainer.UnregisterCallback<MouseLeaveEvent>(OnStatsMouseLeave);
        }
    }

    void Update()
    {
        // Update UI every frame (you might want to optimize this)
        UpdateStatisticsDisplay();
    }

    /// <summary>Init UI elements programmatically.</summary>
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
        envelopeCapLabel = CreateStatLabel("S envelope cap", "$0");
        envelopeRemainingLabel = new Label("S envelope remaining (monthly):\n—");
        envelopeRemainingLabel.name = "envelope-remaining-label";
        envelopeRemainingLabel.style.fontSize = 14;
        envelopeRemainingLabel.style.color = Color.white;
        envelopeRemainingLabel.style.marginBottom = 5;
        envelopeRemainingLabel.style.paddingLeft = 10;
        envelopeRemainingLabel.style.paddingRight = 10;
        envelopeRemainingLabel.style.whiteSpace = WhiteSpace.Normal;

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
        statsContainer.Add(envelopeCapLabel);
        statsContainer.Add(envelopeRemainingLabel);

        root.Add(statsContainer);
        root.Add(toggleStatsButton);
    }

    /// <summary>Create formatted stat label (title + value).</summary>
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

    /// <summary>Style stats container.</summary>
    private void SetupStatsContainerStyle(VisualElement container)
    {
        container.style.position = Position.Absolute;
        container.style.top = 20;
        container.style.left = 20;
        container.style.width = 300;
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

    /// <summary>Style title label.</summary>
    private void SetupTitleStyle(Label titleLabel)
    {
        titleLabel.style.fontSize = 20;
        titleLabel.style.color = Color.cyan;
        titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        titleLabel.style.alignSelf = Align.Center;
        titleLabel.style.marginBottom = 10;
    }

    /// <summary>Style toggle button.</summary>
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

    /// <summary>Wire event handlers for UI interactions.</summary>
    private void SetupEventHandlers()
    {
        // Add hover effects to stats container
        statsContainer.RegisterCallback<MouseEnterEvent>(OnStatsMouseEnter);
        statsContainer.RegisterCallback<MouseLeaveEvent>(OnStatsMouseLeave);
    }

    /// <summary>Update stats display with current game data.</summary>
    private void UpdateStatisticsDisplay()
    {
        if (cityStats == null || economyManager == null) return;

        try
        {
            // Update population - adjust method name based on your CityStats implementation
            int population = GetPopulation();
            populationLabel.text = $"Population: {population:N0}";

            // Update happiness (0–100 normalized score)
            float happiness = GetHappiness();
            happinessLabel.style.color = GetHappinessColor(happiness);
            happinessLabel.text = $"Happiness: {happiness:F0}/100";

            // Update treasury - adjust based on your EconomyManager implementation
            int treasury = GetTreasury();
            int delta = economyManager != null ? economyManager.GetMonthlyIncomeDelta() : 0;
            string deltaStr = delta >= 0 ? $"(+${delta:N0})" : $"(-${Mathf.Abs(delta):N0})";
            treasuryLabel.text = $"Treasury: ${treasury:N0} {deltaStr}";
            treasuryLabel.style.color = treasury >= 0 ? Color.green : Color.red;

            // Update unemployment - adjust based on your implementation
            float unemploymentRate = GetUnemploymentRate();
            unemploymentLabel.text = $"Unemployment: {unemploymentRate:F1}%";
            unemploymentLabel.style.color = GetUnemploymentColor(unemploymentRate);

            if (envelopeCapLabel != null)
                envelopeCapLabel.text = $"S envelope cap: ${cityStats.totalEnvelopeCap:N0}";
            if (envelopeRemainingLabel != null)
            {
                int[] rem = cityStats.envelopeRemainingPerSubType;
                if (rem == null || rem.Length == 0)
                {
                    envelopeRemainingLabel.text = "S envelope remaining (monthly):\n—";
                }
                else
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("S envelope remaining (monthly):");
                    int n = Mathf.Min(7, rem.Length);
                    for (int i = 0; i < n; i++)
                    {
                        string name = $"Subtype {i}";
                        if (zoneSubTypeRegistry != null)
                        {
                            var ent = zoneSubTypeRegistry.GetById(i);
                            if (ent != null && !string.IsNullOrEmpty(ent.displayName))
                                name = ent.displayName;
                        }
                        sb.AppendLine($"  • {name}: ${rem[i]:N0}");
                    }
                    envelopeRemainingLabel.text = sb.ToString().TrimEnd();
                }
            }
        }
        catch (System.Exception)
        {
        }
    }

    /// <summary>Get population from <see cref="CityStats"/> — modify to match actual implementation.</summary>
    private int GetPopulation()
    {
        // Try different common method names - uncomment the one that matches your implementation

        // Option 1: If you have a direct population field/property
        // return cityStats.totalPopulation;

        // Option 2: If you have a GetTotalPopulation method
        // return cityStats.GetTotalPopulation();

        // Option 3: If you calculate from residential buildings
        // return cityStats.CalculatePopulation();

        return cityStats.population;
    }

    /// <summary>Current city happiness score from <see cref="CityStats"/>.</summary>
    private float GetHappiness()
    {
        return cityStats.happiness;
    }

    /// <summary>Treasury from <see cref="EconomyManager"/>.</summary>
    private int GetTreasury()
    {
        return economyManager != null ? economyManager.GetCurrentMoney() : 0;
    }

    /// <summary>Unemployment rate — modify to match actual implementation.</summary>
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

    /// <summary>Color by happiness level.</summary>
    private Color GetHappinessColor(float happiness)
    {
        if (happiness >= 80f) return Color.green;
        if (happiness >= 60f) return Color.yellow;
        if (happiness >= 40f) return new Color(1f, 0.5f, 0f); // Orange
        return Color.red;
    }

    /// <summary>Color by unemployment rate.</summary>
    private Color GetUnemploymentColor(float unemploymentRate)
    {
        if (unemploymentRate <= 3f) return Color.green;
        if (unemploymentRate <= 6f) return Color.yellow;
        if (unemploymentRate <= 10f) return new Color(1f, 0.5f, 0f); // Orange
        return Color.red;
    }

    /// <summary>Toggle stats panel visibility.</summary>
    private void ToggleStatsVisibility()
    {
        isStatsVisible = !isStatsVisible;
        statsContainer.style.display = isStatsVisible ? DisplayStyle.Flex : DisplayStyle.None;
        toggleStatsButton.text = isStatsVisible ? "Hide Stats" : "Show Stats";
    }

    /// <summary>Mouse enter stats area (visual feedback).</summary>
    private void OnStatsMouseEnter(MouseEnterEvent evt)
    {
        statsContainer.style.backgroundColor = new Color(0, 0, 0, 0.9f);
    }

    /// <summary>Mouse leave stats area (visual feedback).</summary>
    private void OnStatsMouseLeave(MouseLeaveEvent evt)
    {
        statsContainer.style.backgroundColor = new Color(0, 0, 0, 0.8f);
    }

    /// <summary>Add new custom stat to display.</summary>
    public void AddCustomStat(string statName, string value, Color textColor = default)
    {
        var customLabel = CreateStatLabel(statName, value);
        if (textColor != default(Color))
            customLabel.style.color = textColor;

        statsContainer.Add(customLabel);
    }

    /// <summary>Remove custom stat from display.</summary>
    public void RemoveCustomStat(string statName)
    {
        var labelToRemove = statsContainer.Q<Label>($"{statName.ToLower()}-label");
        if (labelToRemove != null)
        {
            statsContainer.Remove(labelToRemove);
        }
    }
}
}
