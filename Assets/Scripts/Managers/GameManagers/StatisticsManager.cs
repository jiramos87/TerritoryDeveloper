// ===== STATISTICS MANAGER FOR TRENDS =====
using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class StatisticTrend
{
    public Queue<float> values = new Queue<float>();
    public int maxValues = 30; // Keep last 30 data points
    public float currentValue;
    public float previousValue;
    public float changeRate;
    public TrendDirection trend;
    
    public void AddValue(float value)
    {
        if (values.Count >= maxValues)
        {
            values.Dequeue();
        }
        
        previousValue = currentValue;
        currentValue = value;
        values.Enqueue(value);
        
        UpdateTrend();
    }
    
    private void UpdateTrend()
    {
        if (values.Count < 2) return;
        
        changeRate = currentValue - previousValue;
        
        if (Mathf.Abs(changeRate) < 0.1f)
            trend = TrendDirection.Stable;
        else if (changeRate > 0)
            trend = TrendDirection.Increasing;
        else
            trend = TrendDirection.Decreasing;
    }
    
    public float GetAverage()
    {
        if (values.Count == 0) return 0f;
        
        float sum = 0f;
        foreach (float value in values)
        {
            sum += value;
        }
        return sum / values.Count;
    }
}

public enum TrendDirection
{
    Decreasing,
    Stable,
    Increasing
}

public class StatisticsManager : MonoBehaviour
{
    [Header("Employment Trends")]
    public StatisticTrend populationTrend;
    public StatisticTrend unemploymentTrend;
    public StatisticTrend jobsTrend;
    
    [Header("Demand Trends")]
    public StatisticTrend residentialDemandTrend;
    public StatisticTrend commercialDemandTrend;
    public StatisticTrend industrialDemandTrend;
    
    [Header("Economic Trends")]
    public StatisticTrend incomeTrend;
    public StatisticTrend happinessTrend;
    
    private EmploymentManager employmentManager;
    private DemandManager demandManager;
    private EconomyManager economyManager;
    private CityStats cityStats;
    
    void Start()
    {
        InitializeTrends();
        
        employmentManager = FindObjectOfType<EmploymentManager>();
        demandManager = FindObjectOfType<DemandManager>();
        economyManager = FindObjectOfType<EconomyManager>();
        cityStats = FindObjectOfType<CityStats>();
    }
    
    private void InitializeTrends()
    {
        populationTrend = new StatisticTrend();
        unemploymentTrend = new StatisticTrend();
        jobsTrend = new StatisticTrend();
        residentialDemandTrend = new StatisticTrend();
        commercialDemandTrend = new StatisticTrend();
        industrialDemandTrend = new StatisticTrend();
        incomeTrend = new StatisticTrend();
        happinessTrend = new StatisticTrend();
    }
    
    public void UpdateStatistics()
    {
        if (employmentManager != null)
        {
            populationTrend.AddValue(employmentManager.totalPopulation);
            unemploymentTrend.AddValue(employmentManager.unemploymentRate);
            jobsTrend.AddValue(employmentManager.GetTotalJobs());
        }
        
        if (demandManager != null)
        {
            residentialDemandTrend.AddValue(demandManager.GetResidentialDemand().demandLevel);
            commercialDemandTrend.AddValue(demandManager.GetCommercialDemand().demandLevel);
            industrialDemandTrend.AddValue(demandManager.GetIndustrialDemand().demandLevel);
        }
        
        if (cityStats != null)
        {
            incomeTrend.AddValue(cityStats.money);
            happinessTrend.AddValue(cityStats.happiness);
        }
    }
    
    // Public getters for UI
    public string GetPopulationTrendString() => GetTrendString(populationTrend.trend);
    public string GetUnemploymentTrendString() => GetTrendString(unemploymentTrend.trend);
    public string GetJobsTrendString() => GetTrendString(jobsTrend.trend);
    
    private string GetTrendString(TrendDirection trend)
    {
        switch (trend)
        {
            case TrendDirection.Increasing: return "▲";
            case TrendDirection.Decreasing: return "▼";
            case TrendDirection.Stable: return "►";
            default: return "►";
        }
    }
}
