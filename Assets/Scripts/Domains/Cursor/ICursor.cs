using UnityEngine;
using Territory.Zones;
using Territory.Buildings;
using Territory.Core;
using System;

namespace Domains.Cursor
{
    /// <summary>Public facade for cursor + placement-preview concerns. Consumers bind here only.</summary>
    public interface ICursor
    {
        // ── Cursor texture ──────────────────────────────────────────────────────
        void SetBullDozerCursor();
        void SetDefaultCursor();
        void SetDetailsCursor();

        // ── Preview ─────────────────────────────────────────────────────────────
        void ShowBuildingPreview(GameObject buildingPrefab, int buildingSize = 1);
        void RemovePreview();

        // ── Events ──────────────────────────────────────────────────────────────
        event Action<PlacementResult> PlacementResultChanged;
        event Action<PlacementFailReason> PlacementReasonChanged;
    }
}
