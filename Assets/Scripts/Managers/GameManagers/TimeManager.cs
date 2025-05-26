using UnityEngine;

public class TimeManager : MonoBehaviour
{
    public UIManager uiManager;

    public CityStats cityStats;
    public EconomyManager economyManager;
    public GridManager gridManager;
    public AnimatorManager animatorManager;
    public float[] timeSpeeds = new float[] { 0f, 0.50f, 1.0f, 2.0f, 4.0f };
    private int currentTimeSpeedIndex = 0;

    private float timeMultiplier = 1f;
    private float timeElapsed = 0f;
    private System.DateTime currentDate;

    void Start()
    {
        currentDate = new System.DateTime(2024, 8, 27); // Starting date

        // Trim the time to only include the date, not the time

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
            gridManager.CalculateAvailableSquareZonedSections();
            PlaceAllZonedBuildings();

            if (currentDate.Day == 1)
            {
                // Trigger any daily updates, like income, expenses, power usage, etc.
                cityStats.PerformMonthlyUpdates();
                economyManager.ProcessDailyEconomy();
            }
            uiManager.UpdateUI();      
        }
    }

    void PlaceAllZonedBuildings()
    {
        gridManager.PlaceZonedBuildings(Zone.ZoneType.ResidentialLightZoning);
        gridManager.PlaceZonedBuildings(Zone.ZoneType.ResidentialMediumZoning);
        gridManager.PlaceZonedBuildings(Zone.ZoneType.ResidentialHeavyZoning);
        gridManager.PlaceZonedBuildings(Zone.ZoneType.CommercialLightZoning);
        gridManager.PlaceZonedBuildings(Zone.ZoneType.CommercialMediumZoning);
        gridManager.PlaceZonedBuildings(Zone.ZoneType.CommercialHeavyZoning);
        gridManager.PlaceZonedBuildings(Zone.ZoneType.IndustrialLightZoning);
        gridManager.PlaceZonedBuildings(Zone.ZoneType.IndustrialMediumZoning);
    }

    private void UpdateCityStatsForDay()
    {
        // Update logic for daily city stats like power consumption and income generation
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
    }

    public System.DateTime GetCurrentDate()
    {
        return currentDate;
    }

    public void SetTimeSpeedIndex(int index)
    {
        currentTimeSpeedIndex = index;
        timeMultiplier = timeSpeeds[currentTimeSpeedIndex];
        animatorManager.SetAnimatorSpeed(timeMultiplier);
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