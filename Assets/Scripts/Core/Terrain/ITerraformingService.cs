using System.Collections.Generic;
using UnityEngine;

namespace Territory.Terrain
{
/// <summary>
/// Contract for path-based terraform plan computation.
/// Core-leaf — Domains.Roads consumes via interface to avoid Game asmdef ref.
/// </summary>
public interface ITerraformingService
{
    PathTerraformPlan ComputePathPlan(IList<Vector2> path, bool waterBridgeTerraformRelaxation = false);
}
}
