using Territory.Buildings;

namespace Territory.Economy
{
/// <summary>
/// Contract for city-wide stats: money, population, happiness, resource tracking.
/// Read this interface to understand <see cref="CityStats"/> public API without full impl.
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
