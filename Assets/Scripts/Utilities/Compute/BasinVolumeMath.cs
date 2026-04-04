using System;

namespace Territory.Utilities.Compute
{
    /// <summary>
    /// Placeholders for FEAT-48 basin / volume ↔ surface height <c>S</c> math. No gameplay behavior until that feature ships.
    /// </summary>
    public static class BasinVolumeMath
    {
        /// <summary>Reserved for FEAT-48 — estimate water volume from body surface and bed heights.</summary>
        /// <exception cref="NotImplementedException">Always until FEAT-48.</exception>
        public static float EstimateVolumeFromSurfaceHeight(int surfaceS, int bedHeightMin)
        {
            throw new NotImplementedException("FEAT-48: basin volume vs surface S — not implemented.");
        }

        /// <summary>Reserved for FEAT-48 — solve surface <c>S</c> given target volume and bathymetry.</summary>
        /// <exception cref="NotImplementedException">Always until FEAT-48.</exception>
        public static int EstimateSurfaceHeightFromVolume(float volumeUnits, int bedHeightMin)
        {
            throw new NotImplementedException("FEAT-48: basin volume vs surface S — not implemented.");
        }
    }
}
