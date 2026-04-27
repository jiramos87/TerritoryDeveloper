using System;
using System.Collections.Generic;
using UnityEngine;
using Territory.Zones;

namespace Territory.Simulation
{
    /// <summary>
    /// Stage 10 (city-sim-depth) construction curve table — per-zone-type baseTime
    /// (in-game days) source-of-truth feeding <see cref="ConstructionStageController"/>.
    /// 12 default rows: R/C/I/S × Light/Medium/Heavy. Effective construction time
    /// derived per cell: <c>effectiveTime = baseTime / (0.5 + Mathf.Clamp01(desirability))</c>
    /// per master-plan plan-digest Example 3 + <c>ia/specs/managers-reference.md</c>
    /// §Zone density. Per-stage advance at <c>effectiveTime / 4f</c> days
    /// (4 stages × per-stage = effectiveTime total; T10.1 §Pending Decisions LOCKED).
    /// </summary>
    [CreateAssetMenu(menuName = "Territory/Construction/ConstructionCurveTable", fileName = "ConstructionCurveTable")]
    public class ConstructionCurveTable : ScriptableObject
    {
        [Serializable]
        public struct CurveRow
        {
            public Zone.ZoneType zoneType;
            public float baseTime;
        }

        [SerializeField] private List<CurveRow> rows = new List<CurveRow>();

        // Once-per-key warning dedup — prevents log spam on repeated misses for the same zoneType.
        // Cleared on domain reload via [InitializeOnLoadMethod] reset in #if UNITY_EDITOR block.
        private static readonly HashSet<Zone.ZoneType> _warnedMissingKeys = new HashSet<Zone.ZoneType>();

        /// <summary>
        /// Looks up <paramref name="zoneType"/> baseTime in the configured rows. Returns the
        /// matched <c>baseTime</c> on hit, or the default <c>30f</c> on miss with a once-per-key
        /// <see cref="Debug.LogWarning"/> dedup.
        /// </summary>
        public float GetBaseTime(Zone.ZoneType zoneType)
        {
            for (int i = 0; i < rows.Count; i++)
            {
                if (rows[i].zoneType == zoneType)
                {
                    return rows[i].baseTime;
                }
            }
            if (_warnedMissingKeys.Add(zoneType))
            {
                Debug.LogWarning($"ConstructionCurveTable: missing baseTime for {zoneType}, using 30f default");
            }
            return 30f;
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        private static void ResetWarningDedupOnDomainReload()
        {
            _warnedMissingKeys.Clear();
        }
#endif
    }
}
