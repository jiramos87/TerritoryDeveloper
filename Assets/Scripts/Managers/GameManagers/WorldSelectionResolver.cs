using System.Collections.Generic;
using UnityEngine;
using Territory.Zones;
using Territory.Economy;

namespace Territory.Core
{
/// <summary>
/// Resolves Alt+Click world selection to a <see cref="SelectionInfo"/> with type + field array.
/// Replaces DetailsPopupController + OnCellInfoShown event for info-panel binding (T9.0.3).
/// </summary>
public class WorldSelectionResolver : MonoBehaviour
{
    private GridManager _gridManager;
    private CityStats _cityStats;

    void Awake()
    {
        if (_gridManager == null) _gridManager = FindObjectOfType<GridManager>();
        if (_cityStats == null) _cityStats = FindObjectOfType<CityStats>();
    }

    /// <summary>Resolve grid coord to selection info. Returns null if cell not found.</summary>
    public SelectionInfo Resolve(Vector2Int gridCoord)
    {
        if (_gridManager == null || !_gridManager.isInitialized) return null;
        int gx = gridCoord.x;
        int gy = gridCoord.y;
        if (gx < 0 || gx >= _gridManager.width || gy < 0 || gy >= _gridManager.height) return null;

        CityCell cell = _gridManager.GetCell(gx, gy);
        if (cell == null) return null;

        Zone.ZoneType zt = cell.GetZoneType();
        string typeName = ResolveTypeName(zt);
        List<SelectionField> fields = BuildFields(typeName, cell, gridCoord);

        return new SelectionInfo
        {
            type = typeName,
            gridCoord = gridCoord,
            fields = fields
        };
    }

    private static string ResolveTypeName(Zone.ZoneType zt)
    {
        if (zt == Zone.ZoneType.Road) return "road";
        if (zt == Zone.ZoneType.Water) return "utility";
        if (zt == Zone.ZoneType.Grass) return "empty";
        if (IsResidential(zt)) return "zone-residential";
        if (IsCommercial(zt)) return "zone-commercial";
        if (IsIndustrial(zt)) return "zone-industrial";
        return "empty";
    }

    private List<SelectionField> BuildFields(string typeName, CityCell cell, Vector2Int coord)
    {
        var fields = new List<SelectionField>();
        fields.Add(new SelectionField { key = "Type", value = typeName });
        fields.Add(new SelectionField { key = "Grid", value = $"({coord.x},{coord.y})" });
        fields.Add(new SelectionField { key = "Elevation", value = cell.height.ToString() });

        switch (typeName)
        {
            case "zone-residential":
            case "zone-commercial":
            case "zone-industrial":
                fields.Add(new SelectionField { key = "Zone", value = cell.GetZoneType().ToString() });
                fields.Add(new SelectionField { key = "Desirability", value = cell.desirability.ToString("F1") });
                break;
            case "road":
                fields.Add(new SelectionField { key = "Road", value = "Infrastructure" });
                break;
            case "utility":
                fields.Add(new SelectionField { key = "Utility", value = "Water body" });
                break;
            default:
                fields.Add(new SelectionField { key = "Status", value = "Undeveloped" });
                break;
        }

        return fields;
    }

    private static bool IsResidential(Zone.ZoneType zt)
    {
        return zt == Zone.ZoneType.ResidentialLightBuilding || zt == Zone.ZoneType.ResidentialMediumBuilding ||
               zt == Zone.ZoneType.ResidentialHeavyBuilding || zt == Zone.ZoneType.ResidentialLightZoning ||
               zt == Zone.ZoneType.ResidentialMediumZoning || zt == Zone.ZoneType.ResidentialHeavyZoning;
    }

    private static bool IsCommercial(Zone.ZoneType zt)
    {
        return zt == Zone.ZoneType.CommercialLightBuilding || zt == Zone.ZoneType.CommercialMediumBuilding ||
               zt == Zone.ZoneType.CommercialHeavyBuilding || zt == Zone.ZoneType.CommercialLightZoning ||
               zt == Zone.ZoneType.CommercialMediumZoning || zt == Zone.ZoneType.CommercialHeavyZoning;
    }

    private static bool IsIndustrial(Zone.ZoneType zt)
    {
        return zt == Zone.ZoneType.IndustrialLightBuilding || zt == Zone.ZoneType.IndustrialMediumBuilding ||
               zt == Zone.ZoneType.IndustrialHeavyBuilding || zt == Zone.ZoneType.IndustrialLightZoning ||
               zt == Zone.ZoneType.IndustrialMediumZoning || zt == Zone.ZoneType.IndustrialHeavyZoning;
    }
}

/// <summary>Selection result from <see cref="WorldSelectionResolver.Resolve"/>.</summary>
public class SelectionInfo
{
    public string type;
    public Vector2Int gridCoord;
    public List<SelectionField> fields;
}

/// <summary>Key-value pair for info-panel field-list binding.</summary>
public class SelectionField
{
    public string key;
    public string value;
}
}
