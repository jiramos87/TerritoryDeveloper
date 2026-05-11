using Territory.Core;
using Territory.Forests;
using Territory.Utilities;

namespace Territory.Geography
{
/// <summary>
/// Reset and clear operations extracted from GeographyManager.
/// Handles geography reset, clear-all-forests, and clear-forests-by-type.
/// </summary>
public class GeographyClearService
{
    private readonly GeographyManager _hub;

    public GeographyClearService(GeographyManager hub)
    {
        _hub = hub;
    }

    public void ResetGeography()
    {
        if (_hub.forestManager != null && _hub.forestManager.GetForestMap() != null)
            ClearAllForests();

        if (_hub.waterManager != null && _hub.waterManager.GetWaterMap() != null)
            DebugHelper.Log("GeographyManager: Water reset not implemented yet");

        _hub.currentGeographyData = new GeographyData();
        DebugHelper.Log("GeographyManager: Geography reset complete!");
    }

    public void ClearAllForests()
    {
        if (_hub.forestManager == null || _hub.gridManager == null) return;
        ForestMap forestMap = _hub.forestManager.GetForestMap();
        if (forestMap == null) return;

        var allForests = forestMap.GetAllForests();
        foreach (var pos in allForests)
            _hub.forestManager.RemoveForestFromCell(pos.x, pos.y, false);
    }

    public void ClearForestsOfType(Forest.ForestType forestType)
    {
        if (_hub.forestManager == null || _hub.gridManager == null) return;
        ForestMap forestMap = _hub.forestManager.GetForestMap();
        if (forestMap == null) return;

        var forests = forestMap.GetAllForestsOfType(forestType);
        foreach (var pos in forests)
            _hub.forestManager.RemoveForestFromCell(pos.x, pos.y, false);
    }
}
}
