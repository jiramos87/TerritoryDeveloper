using UnityEngine;
using Territory.Core;

namespace Domains.Cursor
{
    /// <summary>Internal seam — lets CursorService call back to the hub MonoBehaviour without MonoBehaviour dep.</summary>
    internal interface ICursorHub
    {
        UnityEngine.Texture2D CursorTexture { get; }
        UnityEngine.Texture2D BulldozerTexture { get; }
        UnityEngine.Texture2D DetailsTexture { get; }
        bool IsPointerOverUI();
        void FirePlacementResultChanged(PlacementResult result);
        void FirePlacementReasonChanged(PlacementFailReason reason);
    }
}
