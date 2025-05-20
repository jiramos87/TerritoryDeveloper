using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class JobData
{
    public int totalJobsCreated; // Total jobs created by buildings
    public int jobsTakenByResidents; // Jobs taken by new residents
    public int availableJobs; // totalJobsCreated - jobsTakenByResidents
    public int filledJobs; // Currently filled jobs (for UI display)
    
    public float GetFilledPercentage()
    {
        return totalJobsCreated > 0 ? (float)filledJobs / totalJobsCreated : 0f;
    }
    
    public void AddJobs(int newJobs)
    {
        totalJobsCreated += newJobs;
        RecalculateAvailableJobs();
    }
    
    public void AddResidents(int newResidents, float jobsPerResident = 0.7f)
    {
        // New residents take jobs at a certain rate (70% of residents work)
        int newJobsTaken = Mathf.RoundToInt(newResidents * jobsPerResident);
        jobsTakenByResidents += newJobsTaken;
        RecalculateAvailableJobs();
    }
    
    private void RecalculateAvailableJobs()
    {
        availableJobs = Mathf.Max(0, totalJobsCreated - jobsTakenByResidents);
    }
}

public class EmploymentManager : MonoBehaviour
{
    [Header("Employment Statistics")]
    public JobData residentialJobs; // Internal jobs (should be minimal)
    public JobData commercialJobs;
    public JobData industrialJobs;
    
    [Header("Population & Employment")]
    public int totalPopulation;
    public int workingAgePopulation;
    public int totalJobSeekers;
    public int totalEmployedCitizens;
    public float unemploymentRate;
    
    [Header("Employment Configuration")]
    [Range(0.6f, 0.8f)]
    public float workingAgeRatio = 0.7f; // Percentage of population that can work
    [Range(0.8f, 0.95f)]
    public float jobSeekingRatio = 0.9f; // Percentage of working age that seeks jobs
    
    [Header("Job Consumption Tracking")]
    public float jobsPerResidentialBuilding = 0.8f; // How many jobs each residential building consumes
    
    public CityStats cityStats;
    public DemandManager demandManager;
    
    // Track previous building counts to detect new buildings
    private int previousResidentialBuildings = 0;
    private int previousCommercialBuildings = 0;
    private int previousIndustrialBuildings = 0;
    private int previousPopulation = 0;
    
    void Start()
    {
        // Initialize job data
        residentialJobs = new JobData();
        commercialJobs = new JobData();
        industrialJobs = new JobData();
    }
    
    public void UpdateEmployment()
    {
        CalculatePopulationMetrics();
        TrackNewBuildingsAndJobs();
        CalculateCurrentEmployment();
        UpdateDemand();
    }
    
    private void CalculatePopulationMetrics()
    {
        totalPopulation = cityStats.population;
        workingAgePopulation = Mathf.RoundToInt(totalPopulation * workingAgeRatio);
        totalJobSeekers = Mathf.RoundToInt(workingAgePopulation * jobSeekingRatio);
    }
    
    private void TrackNewBuildingsAndJobs()
    {
        // Get current building counts
        int currentResidentialBuildings = GetTotalResidentialBuildings();
        int currentCommercialBuildings = GetTotalCommercialBuildings();
        int currentIndustrialBuildings = GetTotalIndustrialBuildings();
        int currentPopulation = totalPopulation;
        
        // Track new buildings and their job impact
        int newResidentialBuildings = currentResidentialBuildings - previousResidentialBuildings;
        int newCommercialBuildings = currentCommercialBuildings - previousCommercialBuildings;
        int newIndustrialBuildings = currentIndustrialBuildings - previousIndustrialBuildings;
        int newPopulation = currentPopulation - previousPopulation;
        
        // New commercial/industrial buildings create jobs
        if (newCommercialBuildings > 0)
        {
            int newCommercialJobs = CalculateJobsFromNewBuildings(Zone.ZoneType.CommercialLightBuilding, newCommercialBuildings);
            commercialJobs.AddJobs(newCommercialJobs);
            Debug.Log($"Added {newCommercialJobs} commercial jobs from {newCommercialBuildings} new buildings");
        }
        
        if (newIndustrialBuildings > 0)
        {
            int newIndustrialJobs = CalculateJobsFromNewBuildings(Zone.ZoneType.IndustrialLightBuilding, newIndustrialBuildings);
            industrialJobs.AddJobs(newIndustrialJobs);
            Debug.Log($"Added {newIndustrialJobs} industrial jobs from {newIndustrialBuildings} new buildings");
        }
        
        // New residents consume jobs
        if (newPopulation > 0)
        {
            // Distribute job consumption across commercial and industrial
            float commercialRatio = GetJobTypeRatio(commercialJobs.availableJobs);
            float industrialRatio = GetJobTypeRatio(industrialJobs.availableJobs);
            float totalRatio = commercialRatio + industrialRatio;
            
            if (totalRatio > 0)
            {
                int commercialJobsTaken = Mathf.RoundToInt((commercialRatio / totalRatio) * newPopulation * jobsPerResidentialBuilding);
                int industrialJobsTaken = Mathf.RoundToInt((industrialRatio / totalRatio) * newPopulation * jobsPerResidentialBuilding);
                
                commercialJobs.AddResidents(commercialJobsTaken, 1.0f); // 1.0f means all considered as jobs
                industrialJobs.AddResidents(industrialJobsTaken, 1.0f);
                
                Debug.Log($"New residents took {commercialJobsTaken} commercial and {industrialJobsTaken} industrial jobs");
            }
        }
        
        // Update previous counts
        previousResidentialBuildings = currentResidentialBuildings;
        previousCommercialBuildings = currentCommercialBuildings;
        previousIndustrialBuildings = currentIndustrialBuildings;
        previousPopulation = currentPopulation;
    }
    
    private float GetJobTypeRatio(int availableJobs)
    {
        return availableJobs > 0 ? 1.0f : 0.0f; // Simple: if jobs available, weight is 1
    }
    
    private int CalculateJobsFromNewBuildings(Zone.ZoneType representativeType, int newBuildings)
    {
        // Calculate average jobs per building for this type
        // This is a simplified calculation - you might want to be more precise
        var attributes = GetZoneAttributes(representativeType);
        return attributes != null ? newBuildings * attributes.JobsProvided : 0;
    }
    
    private int GetTotalResidentialBuildings()
    {
        return cityStats.residentialLightBuildingCount + 
               cityStats.residentialMediumBuildingCount + 
               cityStats.residentialHeavyBuildingCount;
    }
    
    private int GetTotalCommercialBuildings()
    {
        return cityStats.commercialLightBuildingCount + 
               cityStats.commercialMediumBuildingCount + 
               cityStats.commercialHeavyBuildingCount;
    }
    
    private int GetTotalIndustrialBuildings()
    {
        return cityStats.industrialLightBuildingCount + 
               cityStats.industrialMediumBuildingCount + 
               cityStats.industrialHeavyBuildingCount;
    }
    
    private void CalculateCurrentEmployment()
    {
        // For UI display: show how many jobs are currently filled
        int totalAvailableJobs = commercialJobs.availableJobs + industrialJobs.availableJobs;
        totalEmployedCitizens = Mathf.Min(totalJobSeekers, totalAvailableJobs);
        
        // Distribute employment across job types proportionally
        if (totalAvailableJobs > 0)
        {
            float commercialRatio = (float)commercialJobs.availableJobs / totalAvailableJobs;
            float industrialRatio = (float)industrialJobs.availableJobs / totalAvailableJobs;
            
            commercialJobs.filledJobs = Mathf.RoundToInt(totalEmployedCitizens * commercialRatio);
            industrialJobs.filledJobs = Mathf.RoundToInt(totalEmployedCitizens * industrialRatio);
        }
        else
        {
            commercialJobs.filledJobs = 0;
            industrialJobs.filledJobs = 0;
        }
        
        // Calculate unemployment
        int unemployedCitizens = totalJobSeekers - totalEmployedCitizens;
        unemploymentRate = totalJobSeekers > 0 ? (float)unemployedCitizens / totalJobSeekers * 100f : 0f;
    }
    
    private ZoneAttributes GetZoneAttributes(Zone.ZoneType zoneType)
    {
        switch (zoneType)
        {
            case Zone.ZoneType.CommercialLightBuilding: return ZoneAttributes.CommercialLightBuilding;
            case Zone.ZoneType.CommercialMediumBuilding: return ZoneAttributes.CommercialMediumBuilding;
            case Zone.ZoneType.CommercialHeavyBuilding: return ZoneAttributes.CommercialHeavyBuilding;
            case Zone.ZoneType.IndustrialLightBuilding: return ZoneAttributes.IndustrialLightBuilding;
            case Zone.ZoneType.IndustrialMediumBuilding: return ZoneAttributes.IndustrialMediumBuilding;
            case Zone.ZoneType.IndustrialHeavyBuilding: return ZoneAttributes.IndustrialHeavyBuilding;
            default: return null;
        }
    }
    
    private void UpdateDemand()
    {
        if (demandManager != null)
        {
            demandManager.UpdateRCIDemand(this);
        }
    }
    
    // Public getters for UI and other systems
    public int GetTotalJobs() => commercialJobs.totalJobsCreated + industrialJobs.totalJobsCreated;
    public int GetAvailableJobs() => commercialJobs.availableJobs + industrialJobs.availableJobs; // NEW: Available (not taken) jobs
    public int GetUnemployedCitizens() => totalJobSeekers - totalEmployedCitizens;
    public float GetEmploymentRate() => 100f - unemploymentRate;
    
    // Additional getters for detailed information
    public int GetResidentialPopulation() => totalPopulation; // All population comes from residential
    public int GetJobsTakenByResidents() => commercialJobs.jobsTakenByResidents + industrialJobs.jobsTakenByResidents;
}
