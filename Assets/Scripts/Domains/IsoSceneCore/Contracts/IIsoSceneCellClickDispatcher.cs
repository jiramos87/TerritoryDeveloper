using UnityEngine;

namespace Territory.IsoSceneCore.Contracts
{
    /// <summary>Left/right click dispatch contract shared between RegionScene (Stage 3.0) and tool layer (Stage 5.0). Subscribe handlers in Start, not Awake (invariant #12).</summary>
    public interface IIsoSceneCellClickDispatcher
    {
        /// <summary>Subscribe a handler to receive left/right click events with cell coords.</summary>
        void Subscribe(IIsoSceneCellClickHandler handler);

        /// <summary>Unsubscribe a previously registered handler.</summary>
        void Unsubscribe(IIsoSceneCellClickHandler handler);
    }

    /// <summary>Implemented by per-scene click handlers (RegionCellClickHandler, future tool handlers).</summary>
    public interface IIsoSceneCellClickHandler
    {
        void OnLeftClick(Vector2Int cell);
        void OnRightClick(Vector2Int cell);
    }
}
