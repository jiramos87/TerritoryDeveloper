using UnityEngine;

namespace Territory.Tests.PlayMode.UI.HUD
{
    /// <summary>
    /// Stage 6 HUD migration parity fixture — captures baseline values read from sim
    /// producers (<see cref="Territory.UI.HUD.HudBarDataAdapter"/> channels) before legacy
    /// HUD-bar surface decommission. The Stage 6.4 PlayMode parity test loads this asset,
    /// boots the bake-from-IR scene, drives a fixed sim tick budget, then asserts each
    /// consumer surface (money readout digits, population readout digits, happiness needle
    /// target value, illuminated speed-button index) within tolerance against the captured
    /// expected values. Mirrors the fixture pattern used by Stage 5 JuiceLayer tests.
    /// </summary>
    [CreateAssetMenu(menuName = "Territory/Tests/HudParityFixture", fileName = "HudParityFixture")]
    public sealed class HudParityFixture : ScriptableObject
    {
        [Header("Captured baseline (pre-decommission)")]
        public int expectedMoney;
        public int expectedPopulation;
        [Range(0f, 1f)] public float expectedHappiness;
        public int expectedSpeedIndex;

        [Header("Tolerances")]
        [Tooltip("Absolute tolerance for happiness needle (0..1 range).")]
        public float happinessTolerance = 0.05f;

        [Tooltip("Sim ticks driven before parity assert.")]
        public int simTickBudget = 30;
    }
}
