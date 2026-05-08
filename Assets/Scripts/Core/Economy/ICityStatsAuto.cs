namespace Territory.Economy
{
/// <summary>
/// Slim Core-leaf surface of CityStats consumed by Domains.Roads.Services.AutoBuildService growth gate.
/// Separate from ICityStats (Game-asmdef-resident, refs PowerPlant/IBuilding) to keep Core ref count zero.
/// </summary>
public interface ICityStatsAuto
{
    bool simulateGrowth { get; }
    int cityPowerOutput { get; }
    bool GetCityPowerAvailability();
}
}
