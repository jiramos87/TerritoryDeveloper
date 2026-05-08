namespace Domains.Economy
{
/// <summary>
/// Facade for city economy stats: money, population, resource tracking, zone counts.
/// Implemented by <see cref="Domains.Economy.Services.CityStatsService"/>.
/// </summary>
public interface IEconomy
{
    bool CanAfford(int cost);
    void AddMoney(int value);
    void RemoveMoney(int value);
    int GetMoney();
    void AddPopulation(int value);
    int GetPopulation();
    int GetTotalPowerOutput();
    int GetTotalPowerConsumption();
    int GetTotalWaterOutput();
    int GetTotalWaterConsumption();
    float GetHappiness();
    float GetPollution();
    int GetForestCellCount();
    float GetForestCoveragePercentage();
}
}
