using Domains.UI.Data;
using Domains.UI.Editor.UiBake.Services;
using UnityEngine;

namespace Territory.Editor.Bridge
{
    // Stage 1 tracer (TECH-31977/31978): BakeContext + ButtonBaker POCOs established.
    // Delegate stub wires hub → ButtonBaker entry. Full body migration: Stage 3.
    public static partial class UiBakeHandler
    {
        /// <summary>Delegate stub — Stage 1 tracer. Full body lives in ButtonBaker (wired Stage 3).
        /// Hub callers keep the UiBakeHandler.BakeButton signature unchanged.</summary>
        static GameObject BakeButton(IrInteractive row, BakeContext ctx) =>
            new ButtonBaker(ctx).Bake(row);
    }
}
