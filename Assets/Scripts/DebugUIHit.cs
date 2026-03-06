using UnityEngine;
using UnityEngine.EventSystems;

namespace Territory.Core
{
/// <summary>
/// Temporary debug script: logs which UI elements receive the raycast on left click.
/// Attach to Canvas (or any active GameObject) and check Console when clicking.
/// Remove or disable when done debugging.
/// </summary>
public class DebugUIHit : MonoBehaviour
{
    // void Update()
    // {
    //     if (!Input.GetMouseButtonDown(0))
    //         return;

    //     var es = EventSystem.current;
    //     if (es == null)
    //     {
    //         Debug.Log("[DebugUIHit] No EventSystem in scene.");
    //         return;
    //     }

    //     var ped = new PointerEventData(es) { position = Input.mousePosition };
    //     var results = new System.Collections.Generic.List<RaycastResult>();
    //     es.RaycastAll(ped, results);

    //     if (results.Count == 0)
    //     {
    //         Debug.Log("[DebugUIHit] Click did not hit any UI. Position: " + Input.mousePosition);
    //         return;
    //     }

    //     for (int i = 0; i < results.Count; i++)
    //     {
    //         var r = results[i];
    //         Debug.Log($"[DebugUIHit] Hit #{i + 1}: {r.gameObject.name} (layer={r.gameObject.layer})", r.gameObject);
    //     }
    // }
}
}
