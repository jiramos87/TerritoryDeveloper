using System;
using UnityEngine;

namespace Territory.Simulation
{
    /// <summary>
    /// Discrete grid pole for urban centroid / connurbation blending (FEAT-47). Complements continuous <see cref="UrbanCentroidService.GetCentroid"/>.
    /// </summary>
    [Serializable]
    public struct UrbanCentroidPole
    {
        public int gridX;
        public int gridY;
        public float weight;

        public UrbanCentroidPole(int gridX, int gridY, float weight = 1f)
        {
            this.gridX = gridX;
            this.gridY = gridY;
            this.weight = weight;
        }

        /// <summary>Rounded grid cell from a continuous centroid (e.g. center of mass).</summary>
        public static UrbanCentroidPole FromContinuous(Vector2 continuousCentroid, float weight = 1f)
        {
            return new UrbanCentroidPole(
                Mathf.RoundToInt(continuousCentroid.x),
                Mathf.RoundToInt(continuousCentroid.y),
                weight);
        }
    }
}
