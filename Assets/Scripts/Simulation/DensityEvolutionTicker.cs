using UnityEngine;

namespace Territory.Simulation
{
    /// <summary>
    /// Step 4 shell — density-evolution ticker. Per-tick logic fills in Step 4
    /// (TECH-17xx). Stage 5 only stands up the MonoBehaviour with pre-wired
    /// Inspector ref to <see cref="DesirabilityComposer"/>.
    /// </summary>
    public class DensityEvolutionTicker : MonoBehaviour
    {
        [SerializeField] private DesirabilityComposer desirabilityComposer;

        private void Awake()
        {
            if (desirabilityComposer == null)
            {
                desirabilityComposer = FindObjectOfType<DesirabilityComposer>();
            }
        }

        /// <summary>Step 4 fills density-evolution tick logic.</summary>
        public void SetDesirabilitySource(DesirabilityComposer composer)
        {
            desirabilityComposer = composer;
        }

        private void Update()
        {
            // Step 4: per-tick density evolution.
        }
    }
}
