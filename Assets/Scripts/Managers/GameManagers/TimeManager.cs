using UnityEngine;

public class TimeManager : MonoBehaviour
{
    [Header("UI References")]
    public UIManager uiManager;
    public SpeedButtonsController speedButtonsController; // Add this field

    public CityStats cityStats;
    public EconomyManager economyManager;
    public GridManager gridManager;
    public AnimatorManager animatorManager;
    public ZoneManager zoneManager;
    public float[] timeSpeeds = new float[] { 0f, 0.50f, 1.0f, 2.0f, 4.0f };
    private int currentTimeSpeedIndex = 0;

    private float timeMultiplier = 1f;
    private float timeElapsed = 0f;
    private System.DateTime currentDate;

    void Start()
    {
        currentDate = new System.DateTime(2024, 8, 27);
        currentDate = currentDate.Date;
        uiManager.UpdateUI();
    }

    void Update()
    {
        HandleOnKeyInput();

        timeElapsed += Time.deltaTime * timeMultiplier;

        if (timeElapsed >= 1f)
        {
            currentDate = currentDate.AddDays(1);
            timeElapsed = 0f;

            cityStats.PerformDailyUpdates();
            zoneManager.CalculateAvailableSquareZonedSections();
            PlaceAllZonedBuildings();

            if (currentDate.Day == 1)
            {
                cityStats.PerformMonthlyUpdates();
                economyManager.ProcessDailyEconomy();
            }
            uiManager.UpdateUI();      
        }
    }

    void PlaceAllZonedBuildings()
    {
        zoneManager.PlaceZonedBuildings(Zone.ZoneType.ResidentialLightZoning);
        zoneManager.PlaceZonedBuildings(Zone.ZoneType.ResidentialMediumZoning);
        zoneManager.PlaceZonedBuildings(Zone.ZoneType.ResidentialHeavyZoning);
        zoneManager.PlaceZonedBuildings(Zone.ZoneType.CommercialLightZoning);
        zoneManager.PlaceZonedBuildings(Zone.ZoneType.CommercialMediumZoning);
        zoneManager.PlaceZonedBuildings(Zone.ZoneType.CommercialHeavyZoning);
        zoneManager.PlaceZonedBuildings(Zone.ZoneType.IndustrialLightZoning);
        zoneManager.PlaceZonedBuildings(Zone.ZoneType.IndustrialMediumZoning);
    }

    public void HandleOnKeyInput()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SetTimeSpeedIndex(0);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SetTimeSpeedIndex(1);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SetTimeSpeedIndex(2);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            SetTimeSpeedIndex(3);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            SetTimeSpeedIndex(4);
        }
    }

    public void ChangeTimeSpeed()
    {
        currentTimeSpeedIndex = (currentTimeSpeedIndex + 1) % timeSpeeds.Length;
        timeMultiplier = timeSpeeds[currentTimeSpeedIndex];
        
        // Update button states when speed changes
        if (speedButtonsController != null)
        {
            speedButtonsController.OnSpeedChangedExternally(currentTimeSpeedIndex);
        }
    }

    public void SetTimeSpeedIndex(int index)
    {
        if (index < 0 || index >= timeSpeeds.Length)
        {
            Debug.LogWarning($"Invalid speed index: {index}");
            return;
        }
        
        currentTimeSpeedIndex = index;
        timeMultiplier = timeSpeeds[currentTimeSpeedIndex];
        animatorManager.SetAnimatorSpeed(timeMultiplier);
        
        // Update button visual states when speed changes via keyboard
        if (speedButtonsController != null)
        {
            speedButtonsController.OnSpeedChangedExternally(currentTimeSpeedIndex);
        }
        
        Debug.Log($"Time speed set to index {index} (multiplier: {timeMultiplier})");
    }

    public System.DateTime GetCurrentDate()
    {
        return currentDate;
    }

    public float GetCurrentTimeMultiplier()
    {
        return timeMultiplier;
    }

    public InGameTime GetCurrentInGameTime()
    {
        InGameTime inGameTime = new InGameTime();
        inGameTime.day = currentDate.Day;
        inGameTime.month = currentDate.Month;
        inGameTime.year = currentDate.Year;
        return inGameTime;
    }

    public void RestoreInGameTime(InGameTime inGameTime)
    {
        currentDate = new System.DateTime(inGameTime.year, inGameTime.month, inGameTime.day);
    }

    public void ResetInGameTime()
    {
        currentDate = new System.DateTime(2024, 8, 27);
    }
}

[System.Serializable]
public struct InGameTime
{
    public int day;
    public int month;
    public int year;
}