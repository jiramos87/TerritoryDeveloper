using UnityEngine;
using System.Collections.Generic;
using Territory.Zones;

namespace Territory.Simulation
{
public enum ProposalStatus { Pending, Accepted, Rejected }

[System.Serializable]
public struct ProposedCell
{
    public int offsetX;
    public int offsetY;
    public int zoneTypeInt;

    public Vector2Int Offset
    {
        get => new Vector2Int(offsetX, offsetY);
        set { offsetX = value.x; offsetY = value.y; }
    }

    public Zone.ZoneType ZoneType
    {
        get => (Zone.ZoneType)zoneTypeInt;
        set => zoneTypeInt = (int)value;
    }
}

/// <summary>
/// Data class representing an urbanization expansion proposal with road paths, zone positions, status, and cost estimation.
/// </summary>
[System.Serializable]
public class UrbanizationProposal
{
    public string proposalName;
    public int anchorX;
    public int anchorY;
    public List<ProposedCell> cells = new List<ProposedCell>();
    public int totalCost;
    public int statusInt;

    public Vector2Int AnchorPosition
    {
        get => new Vector2Int(anchorX, anchorY);
        set { anchorX = value.x; anchorY = value.y; }
    }

    public ProposalStatus Status
    {
        get => (ProposalStatus)statusInt;
        set => statusInt = (int)value;
    }
}
}
