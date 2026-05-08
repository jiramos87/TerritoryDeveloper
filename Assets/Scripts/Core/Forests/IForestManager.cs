namespace Territory.Forests
{
/// <summary>
/// Contract for forest cell ops. Core-leaf — Core.CityCell consumes via FindObjectOfType to avoid Game asmdef ref.
/// </summary>
public interface IForestManager
{
    bool RemoveForestFromCell(int x, int y, bool refundCost = false);
}
}
