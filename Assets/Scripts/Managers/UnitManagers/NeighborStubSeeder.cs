using System.Collections.Generic;
using UnityEngine;
using Territory.Roads;

namespace Territory.Core
{
    /// <summary>
    /// Static helper — seeds the initial <see cref="NeighborCityStub"/> list in a
    /// <see cref="GameSaveData"/> during new-game init.
    /// Places exactly one <b>parent-scale stub</b> (see glossary) at a <see cref="BorderSide"/>
    /// drawn from the active interstate endpoints.  Deterministic from
    /// <see cref="GameManagers.MapGenerationSeed.MasterSeed"/> — same master seed → same side.
    /// GUID id is non-deterministic by design (cross-save uniqueness over reproducibility).
    /// </summary>
    public static class NeighborStubSeeder
    {
        /// <summary>
        /// Populate <paramref name="saveData"/><c>.neighborStubs</c> with exactly one
        /// <see cref="NeighborCityStub"/> whose <c>borderSide</c> is drawn from the
        /// interstate entry/exit border indices.
        /// </summary>
        /// <param name="saveData">In-memory new-game save container; <c>neighborStubs</c> must be non-null.</param>
        /// <param name="interstate">Active <see cref="InterstateManager"/> (may be null → falls back to all four sides).</param>
        /// <param name="masterSeed">Deterministic seed — use <see cref="GameManagers.MapGenerationSeed.MasterSeed"/>.</param>
        public static void SeedInitial(
            GameSaveData saveData,
            InterstateManager interstate,
            int masterSeed)
        {
            if (saveData == null)
            {
                Debug.LogError("[NeighborStubSeeder] saveData is null — cannot seed stubs.");
                return;
            }
            if (saveData.neighborStubs == null)
            {
                Debug.LogWarning("[NeighborStubSeeder] neighborStubs list was null — initializing.");
                saveData.neighborStubs = new System.Collections.Generic.List<NeighborCityStub>();
            }

            List<BorderSide> candidates = BuildCandidates(interstate);

            // Deterministic pick: process-local Random seeded by master seed; no shared RNG side-effects.
            System.Random rng = new System.Random(masterSeed);
            BorderSide picked = candidates[rng.Next(candidates.Count)];

            string id = System.Guid.NewGuid().ToString();
            NeighborCityStub stub = new NeighborCityStub
            {
                id          = id,
                displayName = $"Neighbor-{id.Substring(0, 8)}",
                borderSide  = picked,
            };

            saveData.neighborStubs.Add(stub);
        }

        // ── private helpers ──────────────────────────────────────────────────────

        /// <summary>
        /// Build the candidate <see cref="BorderSide"/> list from interstate endpoints.
        /// Falls back to all four sides when both endpoints are unset (-1).
        /// Border index convention (per TerritoryData.OppositeBorder): 0=South, 1=North, 2=West, 3=East.
        /// </summary>
        private static List<BorderSide> BuildCandidates(InterstateManager interstate)
        {
            var candidates = new List<BorderSide>();

            if (interstate != null)
            {
                if (interstate.EntryBorder >= 0)
                    candidates.Add(BorderIndexToSide(interstate.EntryBorder));
                if (interstate.ExitBorder >= 0)
                    candidates.Add(BorderIndexToSide(interstate.ExitBorder));
            }

            if (candidates.Count == 0)
            {
                // Degenerate map — no interstate endpoints; fall back to all four sides.
                Debug.LogWarning("[NeighborStubSeeder] No interstate endpoints found — falling back to all four border sides.");
                candidates.Add(BorderSide.South);
                candidates.Add(BorderSide.North);
                candidates.Add(BorderSide.West);
                candidates.Add(BorderSide.East);
            }

            return candidates;
        }

        /// <summary>
        /// Map <paramref name="borderIndex"/> to <see cref="BorderSide"/>.
        /// Convention (TerritoryData.OppositeBorder): 0=South, 1=North, 2=West, 3=East.
        /// </summary>
        private static BorderSide BorderIndexToSide(int borderIndex)
        {
            switch (borderIndex)
            {
                case 0: return BorderSide.South;
                case 1: return BorderSide.North;
                case 2: return BorderSide.West;
                case 3: return BorderSide.East;
                default:
                    Debug.LogWarning($"[NeighborStubSeeder] Unknown border index {borderIndex} — defaulting to South.");
                    return BorderSide.South;
            }
        }
    }
}
