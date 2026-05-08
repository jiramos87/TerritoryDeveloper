using System;
using System.Collections.Generic;
using UnityEngine;
using Territory.Zones;

namespace Domains.Zones.Services
{
    /// <summary>
    /// Pure service: computes contiguous square zone sections (1x1, 2x2, 3x3) from tracked zoned positions.
    /// Extracted from ZoneManager section-calculation logic.
    /// No MonoBehaviour dependency. Desirability weighting lives inside via delegate to avoid engine coupling.
    /// Invariant #11 preserved: no UrbanizationProposal reference.
    /// </summary>
    public class ZoneSectionService
    {
        private static readonly Zone.ZoneType[] ValidZoneTypes =
        {
            Zone.ZoneType.ResidentialLightZoning, Zone.ZoneType.ResidentialMediumZoning, Zone.ZoneType.ResidentialHeavyZoning,
            Zone.ZoneType.CommercialLightZoning, Zone.ZoneType.CommercialMediumZoning, Zone.ZoneType.CommercialHeavyZoning,
            Zone.ZoneType.IndustrialLightZoning, Zone.ZoneType.IndustrialMediumZoning, Zone.ZoneType.IndustrialHeavyZoning,
            Zone.ZoneType.Grass, Zone.ZoneType.Water
        };

        /// <summary>Returns the valid zone types for section calculation.</summary>
        public static IReadOnlyList<Zone.ZoneType> GetValidZoneTypes() => ValidZoneTypes;

        /// <summary>
        /// Computes all available square sections (1x1, 2x2, 3x3) for the given zone type's positions.
        /// Returns list of sections; each section is a Vector2[] of grid positions.
        /// </summary>
        public List<List<Vector2>> CalculateSections(IReadOnlyList<Vector2> zonedPositions)
        {
            var sections = new List<List<Vector2>>();
            for (int size = 1; size <= 3; size++)
            {
                if (zonedPositions.Count == 0) continue;
                sections.AddRange(CalculateSectionsForSize(zonedPositions, size));
            }
            return sections;
        }

        private List<List<Vector2>> CalculateSectionsForSize(IReadOnlyList<Vector2> zonedPositions, int size)
        {
            var sections = new List<List<Vector2>>();
            var usedPositions = new HashSet<Vector2>();
            var zonedSet = new HashSet<Vector2>(zonedPositions);

            for (int i = zonedPositions.Count - 1; i >= 0; i--)
            {
                Vector2 start = zonedPositions[i];
                if (usedPositions.Contains(start)) continue;

                List<Vector2> section = GetSquareSection(start, size, zonedSet, usedPositions);
                if (section.Count == size * size)
                {
                    sections.Add(section);
                    foreach (var pos in section)
                        usedPositions.Add(pos);
                }
            }
            return sections;
        }

        private List<Vector2> GetSquareSection(Vector2 start, int size, HashSet<Vector2> available, HashSet<Vector2> excluded)
        {
            var section = new List<Vector2>();
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    Vector2 pos = new Vector2(start.x + x, start.y + y);
                    if (available.Contains(pos) && !excluded.Contains(pos))
                        section.Add(pos);
                }
            }
            return section;
        }

        /// <summary>
        /// Returns a weighted random section from candidates.
        /// isIndustrial: inverts desirability weight (industry prefers low-desirability cells).
        /// cellDesirability: delegate mapping (x,y) → float desirability score.
        /// </summary>
        public (int size, List<Vector2> section) GetWeightedSection(
            Zone.ZoneType zoneType,
            List<(int size, List<Vector2> section)> candidates,
            Func<int, int, float> cellDesirability,
            float baseSpawnWeight,
            float minDesirabilityThreshold,
            float lowDesirabilityPenalty)
        {
            if (candidates == null || candidates.Count == 0)
                return (0, null);

            bool isIndustrial = zoneType == Zone.ZoneType.IndustrialLightZoning
                || zoneType == Zone.ZoneType.IndustrialMediumZoning
                || zoneType == Zone.ZoneType.IndustrialHeavyZoning;

            float[] weights = new float[candidates.Count];
            float maxDesir = 0f;

            for (int i = 0; i < candidates.Count; i++)
            {
                float avg = AverageSectionDesirability(candidates[i].section, cellDesirability);
                if (avg > maxDesir) maxDesir = avg;
            }

            for (int i = 0; i < candidates.Count; i++)
            {
                float avgDesir = AverageSectionDesirability(candidates[i].section, cellDesirability);
                if (isIndustrial)
                {
                    weights[i] = (maxDesir - avgDesir) + baseSpawnWeight;
                }
                else
                {
                    weights[i] = avgDesir + baseSpawnWeight;
                    if (avgDesir < minDesirabilityThreshold)
                        weights[i] *= lowDesirabilityPenalty;
                }
                if (weights[i] < 0) weights[i] = 0;
            }

            float totalWeight = 0;
            for (int i = 0; i < weights.Length; i++)
                totalWeight += weights[i];

            if (totalWeight <= 0)
            {
                int idx = UnityEngine.Random.Range(0, candidates.Count);
                return candidates[idx];
            }

            float roll = UnityEngine.Random.value * totalWeight;
            float cumulative = 0;
            for (int i = 0; i < weights.Length; i++)
            {
                cumulative += weights[i];
                if (roll <= cumulative)
                    return candidates[i];
            }
            return candidates[candidates.Count - 1];
        }

        private float AverageSectionDesirability(List<Vector2> section, Func<int, int, float> cellDesirability)
        {
            if (section == null || section.Count == 0 || cellDesirability == null) return 0f;
            float total = 0f;
            for (int i = 0; i < section.Count; i++)
                total += cellDesirability((int)section[i].x, (int)section[i].y);
            return total / section.Count;
        }
    }
}
