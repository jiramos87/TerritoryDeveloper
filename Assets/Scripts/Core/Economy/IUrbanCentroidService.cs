using UnityEngine;
using Territory.Simulation;

namespace Territory.Economy
{
/// <summary>
/// Contract for urban-centroid ring classification + per-ring street params. Core-leaf — Domains.Roads consumes.
/// </summary>
public interface IUrbanCentroidService
{
    UrbanRing GetUrbanRing(Vector2 worldPos);
    bool CentroidShiftedRecently { get; }
    RingStreetParams GetStreetParamsForRing(UrbanRing ring);
}
}
