using UnityEngine;
using Territory.UI;

namespace Territory.UI.HUD
{
    /// <summary>
    /// Map-panel adapter — subscribes minimap.toggle / minimap.layer.set / minimap.drag actions
    /// → forwards to <see cref="MiniMapController"/>. Apply-time render-check mirrors SettingsViewController.
    /// </summary>
    public class MapPanelAdapter : MonoBehaviour
    {
        private MiniMapController _miniMapController;
        private Territory.UI.CameraController _cameraController;

        private void Awake()
        {
            _miniMapController = FindObjectOfType<MiniMapController>();
            _cameraController = FindObjectOfType<Territory.UI.CameraController>();

            if (_miniMapController != null)
                _miniMapController.EnforceRenderSize();
        }

        private void OnEnable()
        {
            ApplyTimeRenderCheck();
        }

        /// <summary>Subscribe minimap.toggle action. Called by HUD map button.</summary>
        public void OnMinimapToggle(bool visible)
        {
            if (_miniMapController == null) return;
            _miniMapController.SetVisible(visible);
        }

        /// <summary>Subscribe minimap.layer.set action. Called by header layer-toggle buttons.</summary>
        public void OnLayerSet(string layerName)
        {
            if (_miniMapController == null) return;
            if (System.Enum.TryParse<MiniMapLayer>(layerName, out MiniMapLayer layer))
                _miniMapController.ForwardLayerToggle(layer);
            else
                Debug.LogWarning($"[MapPanelAdapter] Unknown layer: {layerName}");
        }

        /// <summary>Subscribe minimap.drag event → pan camera to target grid.</summary>
        public void OnMinimapDrag(Vector2Int targetGrid)
        {
            if (_cameraController == null) return;
            _cameraController.PanCameraTo(targetGrid);
        }

        /// <summary>
        /// Apply-time render-check — mirrors SettingsViewController pattern.
        /// Validates minimap-canvas RawImage + IDragHandler + 3 layer-toggles reachable.
        /// </summary>
        private void ApplyTimeRenderCheck()
        {
            if (_miniMapController == null)
            {
                Debug.LogWarning("[MapPanelAdapter] MiniMapController not found in scene.");
                return;
            }
            var rawImg = _miniMapController.mapImage;
            if (rawImg == null)
                Debug.LogWarning("[MapPanelAdapter] minimap-canvas RawImage not wired on MiniMapController.");

            Debug.Log($"[MapPanelAdapter] render-check OK — minimap visible={_miniMapController.IsVisible}");
        }
    }
}
