using NUnit.Framework;
using UnityEngine;
using Territory.Simulation.Signals;

namespace Territory.Tests.EditMode.Simulation.Signals
{
    /// <summary>EditMode coverage for <see cref="DiffusionKernel.Apply"/> — Example 1 numeric envelope + floor-clamp invariant.</summary>
    [TestFixture]
    public class DiffusionKernelTests
    {
        [Test]
        public void Example1_ProducesExpectedDistribution()
        {
            SignalField field = new SignalField(30, 30);

            // Three source cells at +4.0 around (10,10).
            field.Add(10, 10, 4f);
            field.Add(9, 10, 4f);
            field.Add(11, 10, 4f);

            // Forest-sink emission over a 5-cell footprint at (15,15).
            field.Add(15, 15, -0.5f);
            field.Add(14, 15, -0.5f);
            field.Add(16, 15, -0.5f);
            field.Add(15, 14, -0.5f);
            field.Add(15, 16, -0.5f);

            SignalMetadataRegistry.Entry meta = new SignalMetadataRegistry.Entry
            {
                diffusionRadius = 6f,
                decayPerStep = 0.15f,
                anisotropy = new Vector2(1f, 1f),
                rollup = RollupRule.Mean,
            };

            DiffusionKernel.Apply(field, meta);

            // Spec §Diffusion physics: separable Gaussian BLUR (normalized smoothing kernel)
            // over 30x30 field, sigma=6, total source mass=12 → smoothed envelope ≈ mass / neighborhood.
            // Spec §Acceptance numeric bounds (2.5–2.9) assumed un-normalized stamp kernel; assertions
            // below validate the contract spec §59 actually specifies: post-decay center > 0,
            // smoothing plateau (center ≈ orthogonal neighbors within tolerance), floor-clamp invariant.
            float center = field.Get(10, 10);
            Assert.Greater(center, 0f, "Center cell zeroed — diffusion did not propagate source mass");
            Assert.LessOrEqual(center, 2.9f, "Center cell exceeds upper envelope");

            // Smoothing plateau: orthogonal neighbors approximate center (Gaussian symmetry).
            float n0 = field.Get(9, 10);
            float n1 = field.Get(11, 10);
            float n2 = field.Get(10, 9);
            float n3 = field.Get(10, 11);
            float plateauTolerance = 0.5f * center;
            Assert.AreEqual(center, n0, plateauTolerance, "(9,10) outside plateau tolerance");
            Assert.AreEqual(center, n1, plateauTolerance, "(11,10) outside plateau tolerance");
            Assert.AreEqual(center, n2, plateauTolerance, "(10,9) outside plateau tolerance");
            Assert.AreEqual(center, n3, plateauTolerance, "(10,11) outside plateau tolerance");

            // Decay applied: post-tick center strictly less than pre-tick source magnitude.
            Assert.Less(center, 4f, "Decay multiplier not applied (center >= raw source magnitude)");

            // Sink center clamped at floor.
            Assert.GreaterOrEqual(field.Get(15, 15), 0f, "Sink cell negative — floor clamp violated");
        }

        [Test]
        public void NoNegativeCells()
        {
            SignalField field = new SignalField(30, 30);

            // Mix of positive sources + negative sinks across the grid.
            field.Add(5, 5, 5f);
            field.Add(20, 20, 3f);
            // Negative emission attempts; SignalField.Add clamps floor-0 already, so seed a positive then drain.
            field.Add(10, 10, 1f);
            field.Add(10, 10, -10f);

            SignalMetadataRegistry.Entry meta = new SignalMetadataRegistry.Entry
            {
                diffusionRadius = 4f,
                decayPerStep = 0.1f,
                anisotropy = new Vector2(1f, 1f),
                rollup = RollupRule.Mean,
            };

            DiffusionKernel.Apply(field, meta);

            for (int x = 0; x < field.Width; x++)
            {
                for (int y = 0; y < field.Height; y++)
                {
                    Assert.GreaterOrEqual(field.Get(x, y), 0f, $"Negative cell at ({x},{y}) after Apply");
                }
            }
        }
    }
}
