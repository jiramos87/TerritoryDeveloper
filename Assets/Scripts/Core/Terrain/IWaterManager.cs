namespace Territory.Terrain
{
/// <summary>
/// Contract for water-cell predicates. Core-leaf — Domains.* + Core.* may consume; Game.asmdef provides impl via concrete WaterManager.
/// </summary>
public interface IWaterManager
{
    bool IsWaterAt(int x, int y);
}
}
