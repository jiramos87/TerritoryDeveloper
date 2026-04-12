using UnityEngine;
using Territory.Simulation;

namespace Territory.Utilities.Compute
{
    /// <summary>
    /// Pure urban growth ring classification: distance cell→centroid vs effective radius.
    /// Mirrors <see cref="UrbanMetrics"/> thresholds for single-centroid mode; multipolar extension uses
    /// min distance to poles (see <see cref="ClassifyRingMultipolar"/>). simulation-system §Rings.
    /// </summary>
    public static class UrbanGrowthRingMath
    {
        public const float MinUrbanRadius = 20f;
        public const float RadiusScale = 1.8f;

        public const float InnerBoundaryFraction = 0.70f;
        public const float MidBoundaryFraction = 1.00f;
        public const float OuterBoundaryFraction = 1.80f;

        /// <summary>
        /// Effective urban radius from building cell count. Same formula as <see cref="UrbanMetrics.GetUrbanRadius"/>.
        /// </summary>
        public static float ComputeUrbanRadiusFromCellCount(int urbanCellCount)
        {
            float r = RadiusScale * Mathf.Sqrt(urbanCellCount / Mathf.PI);
            return Mathf.Max(MinUrbanRadius, r);
        }

        /// <summary>
        /// Ring from Euclidean distance to one centroid. Legacy single-pole behavior.
        /// </summary>
        public static UrbanRing ClassifyRing(
            float cellX,
            float cellY,
            float centroidX,
            float centroidY,
            float urbanRadius)
        {
            float dist = Vector2.Distance(new Vector2(cellX, cellY), new Vector2(centroidX, centroidY));
            return ClassifyRingFromDistance(dist, urbanRadius);
        }

        /// <summary>
        /// Multipolar-ready: classify via min Euclidean distance to any pole (equal weight).
        /// Empty pole list → <paramref name="fallbackRing"/>.
        /// </summary>
        public static UrbanRing ClassifyRingMultipolar(
            float cellX,
            float cellY,
            Vector2[] centroids,
            float urbanRadius,
            UrbanRing fallbackRing = UrbanRing.Mid)
        {
            if (centroids == null || centroids.Length == 0)
                return fallbackRing;
            float best = float.MaxValue;
            for (int i = 0; i < centroids.Length; i++)
            {
                float d = Vector2.Distance(new Vector2(cellX, cellY), centroids[i]);
                if (d < best)
                    best = d;
            }
            return ClassifyRingFromDistance(best, urbanRadius);
        }

        /// <summary>
        /// Shared threshold logic: normalized distance bands vs urban radius.
        /// </summary>
        public static UrbanRing ClassifyRingFromDistance(float distanceToPole, float urbanRadius)
        {
            if (urbanRadius <= 0f)
                return UrbanRing.Rural;
            if (distanceToPole <= urbanRadius * InnerBoundaryFraction)
                return UrbanRing.Inner;
            if (distanceToPole <= urbanRadius * MidBoundaryFraction)
                return UrbanRing.Mid;
            if (distanceToPole <= urbanRadius * OuterBoundaryFraction)
                return UrbanRing.Outer;
            return UrbanRing.Rural;
        }

        /// <summary>
        /// Ring boundary distances for visualization (70%, 100%, 180% of radius).
        /// </summary>
        public static float[] GetRingBoundaryDistances(float urbanRadius)
        {
            return new[]
            {
                urbanRadius * InnerBoundaryFraction,
                urbanRadius * MidBoundaryFraction,
                urbanRadius * OuterBoundaryFraction
            };
        }
    }
}
