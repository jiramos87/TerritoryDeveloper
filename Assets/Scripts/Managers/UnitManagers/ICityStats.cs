using Territory.Buildings;

namespace Territory.Economy
{
/// <summary>
/// Contract for city-wide statistics: money, population, happiness, and resource tracking.
/// Read this interface to understand CityStats's public API without reading its full implementation.
/// </summary>
public interface ICityStats
{
    bool CanAfford(int cost);
    void AddMoney(int value);
    void RemoveMoney(int value);
    void AddPopulation(int value);
    int GetTotalPowerOutput();
    int GetTotalPowerConsumption();
    int GetTotalWaterOutput();
    int GetTotalWaterConsumption();
    void RegisterPowerPlant(PowerPlant powerPlant);
    void UnregisterPowerPlant(PowerPlant powerPlant);
}
}
