using UnityEngine;

public class ZoneAttributes
{
    public int ConstructionCost { get; set; }
    public int Population { get; set; }
    public int Happiness { get; set; }
    public int PowerConsumption { get; set; }
    public int JobsProvided { get; set; } // Jobs this zone provides to the city economy

    public ZoneAttributes(int constructionCost, int population, int happiness, int powerConsumption, int jobsProvided = 0)
    {
        ConstructionCost = constructionCost;
        Population = population;
        Happiness = happiness;
        PowerConsumption = powerConsumption;
        JobsProvided = jobsProvided;
    }

    // ===========================================
    // ECONOMIC LOGIC CLARIFICATION:
    // ===========================================
    // ZONING: Placeholder zones, no economic impact yet
    // RESIDENTIAL: Provides population (job seekers), minimal jobs (internal services)
    // COMMERCIAL: Provides service/retail jobs, no population
    // INDUSTRIAL: Provides manufacturing jobs, no population
    // ===========================================
    
    // Zoning (doesn't provide jobs or population yet)
    public static readonly ZoneAttributes ResidentialLightZoning = new ZoneAttributes(2, 0, 0, 0, 0);
    public static readonly ZoneAttributes ResidentialMediumZoning = new ZoneAttributes(4, 0, 0, 0, 0);
    public static readonly ZoneAttributes ResidentialHeavyZoning = new ZoneAttributes(6, 0, 0, 0, 0);
    
    public static readonly ZoneAttributes CommercialLightZoning = new ZoneAttributes(2, 0, 0, 0, 0);
    public static readonly ZoneAttributes CommercialMediumZoning = new ZoneAttributes(4, 0, 0, 0, 0);
    public static readonly ZoneAttributes CommercialHeavyZoning = new ZoneAttributes(7, 0, 0, 0, 0);
    
    public static readonly ZoneAttributes IndustrialLightZoning = new ZoneAttributes(2, 0, 0, 0, 0);
    public static readonly ZoneAttributes IndustrialMediumZoning = new ZoneAttributes(5, 0, 0, 0, 0);
    public static readonly ZoneAttributes IndustrialHeavyZoning = new ZoneAttributes(8, 0, 0, 0, 0);

    // RESIDENTIAL BUILDINGS: Provide population (job seekers)
    // Note: The small job values represent internal services (building maintenance, etc.)
    // These should NOT count towards city-wide employment in the demand calculation
    public static readonly ZoneAttributes ResidentialLightBuilding = new ZoneAttributes(0, 5, 100, 10, 0); // CHANGED: 0 jobs (internal only)
    public static readonly ZoneAttributes ResidentialMediumBuilding = new ZoneAttributes(0, 20, 100, 40, 0); // CHANGED: 0 jobs (internal only)
    public static readonly ZoneAttributes ResidentialHeavyBuilding = new ZoneAttributes(0, 50, 100, 80, 0); // CHANGED: 0 jobs (internal only)

    // COMMERCIAL BUILDINGS: Provide jobs (retail, services, office work)
    public static readonly ZoneAttributes CommercialLightBuilding = new ZoneAttributes(0, 0, 100, 40, 8);
    public static readonly ZoneAttributes CommercialMediumBuilding = new ZoneAttributes(0, 0, 100, 80, 15);
    public static readonly ZoneAttributes CommercialHeavyBuilding = new ZoneAttributes(0, 0, 100, 160, 25);

    // INDUSTRIAL BUILDINGS: Provide jobs (manufacturing, production)
    public static readonly ZoneAttributes IndustrialLightBuilding = new ZoneAttributes(0, 0, 5, 60, 12);
    public static readonly ZoneAttributes IndustrialMediumBuilding = new ZoneAttributes(0, 0, 100, 120, 25);
    public static readonly ZoneAttributes IndustrialHeavyBuilding = new ZoneAttributes(0, 0, 100, 240, 40);

    // Infrastructure
    public static readonly ZoneAttributes Road = new ZoneAttributes(50, 0, 0, 100, 0);
    public static readonly ZoneAttributes Grass = new ZoneAttributes(0, 0, 0, 0, 0);
}
