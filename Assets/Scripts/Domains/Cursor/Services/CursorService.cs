using UnityEngine;
using Territory.Core;
using Territory.Zones;
using Domains.Grid;

namespace Domains.Cursor.Services
{
    /// <summary>POCO service — absorbs cursor + preview body logic from CursorManager hub.</summary>
    public class CursorService
    {
        private readonly ICursorHub _hub;
        private IGrid _grid;

        // ── Preview state ────────────────────────────────────────────────────────
        private GameObject _previewInstance;
        private GameObject _currentRoadGhostPrefab;
        private int _lastCellX = int.MinValue;
        private int _lastCellY = int.MinValue;
        private int _currentAssetId;
        private int _currentRotation;
        private Zone.ZoneType _currentZoneType = Zone.ZoneType.None;

        private enum PreviewTintState { None, Valid, Invalid }
        private PreviewTintState _lastTintState = PreviewTintState.None;
        private Color _originalPreviewColor = new Color(1f, 1f, 1f, 0.5f);
        private static readonly Color PreviewTintGreen = new Color(0.4f, 1f, 0.4f, 0.5f);
        private static readonly Color PreviewTintRed = new Color(1f, 0.4f, 0.4f, 0.5f);

        // ── Cursor texture state ─────────────────────────────────────────────────
        private Texture2D _activeCursorTexture;
        private Vector2 _activeCursorHotSpot;
        private bool _isOverUI;

        // ── Texture scaling cache ────────────────────────────────────────────────
        private Texture2D _scaledBulldozerTexture;

        /// <summary>Construct CursorService with hub reference.</summary>
        public CursorService(ICursorHub hub)
        {
            _hub = hub;
        }

        /// <summary>Wire IGrid resolved from registry in Start (not Awake).</summary>
        public void WireDependencies(IGrid grid)
        {
            _grid = grid;
        }

        // ── Cursor texture ────────────────────────────────────────────────────────

        /// <summary>Switch cursor to bulldozer texture (scaled half-size).</summary>
        public void SetBullDozerCursor()
        {
            var tex = GetScaledBulldozerTexture();
            if (tex == null) return;
            _activeCursorTexture = tex;
            _activeCursorHotSpot = new Vector2(0, tex.height);
            if (!_hub.IsPointerOverUI())
                UnityEngine.Cursor.SetCursor(_activeCursorTexture, _activeCursorHotSpot, CursorMode.Auto);
        }

        /// <summary>Reset cursor to default hub texture.</summary>
        public void SetDefaultCursor()
        {
            _activeCursorTexture = null;
            _activeCursorHotSpot = Vector2.zero;
            UnityEngine.Cursor.SetCursor(_hub.CursorTexture, Vector2.zero, CursorMode.Auto);
        }

        /// <summary>Switch cursor to details inspection texture.</summary>
        public void SetDetailsCursor()
        {
            _activeCursorTexture = _hub.DetailsTexture;
            _activeCursorHotSpot = Vector2.zero;
            if (!_hub.IsPointerOverUI())
                UnityEngine.Cursor.SetCursor(_activeCursorTexture, _activeCursorHotSpot, CursorMode.Auto);
        }

        // ── Preview ────────────────────────────────────────────────────────────────

        /// <summary>Spawn ghost preview for building prefab; tint + strip colliders.</summary>
        public void ShowBuildingPreview(GameObject buildingPrefab, int buildingSize = 1)
        {
            try
            {
                _currentRoadGhostPrefab = null;
                if (_previewInstance != null) Object.Destroy(_previewInstance);

                _previewInstance = Object.Instantiate(buildingPrefab);
                SpriteRenderer sr = _previewInstance.GetComponent<SpriteRenderer>()
                    ?? _previewInstance.GetComponentInChildren<SpriteRenderer>();
                if (sr != null)
                {
                    _originalPreviewColor = new Color(1f, 1f, 1f, 0.5f);
                    sr.color = _originalPreviewColor;
                    sr.sortingOrder = 10000;
                    _lastTintState = PreviewTintState.None;
                }
                else
                {
                    Debug.LogError("No SpriteRenderer found on building prefab or its children!");
                }

                foreach (var col in _previewInstance.GetComponentsInChildren<Collider2D>())
                    Object.Destroy(col);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error in ShowBuildingPreview: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>Destroy current preview ghost + clear cached cell.</summary>
        public void RemovePreview()
        {
            _currentRoadGhostPrefab = null;
            if (_previewInstance != null)
            {
                Object.Destroy(_previewInstance);
                _previewInstance = null;
            }
            _lastCellX = int.MinValue;
            _lastCellY = int.MinValue;
        }

        // ── Update tick ───────────────────────────────────────────────────────────

        /// <summary>Per-frame: move preview to mouse cell, swap road ghost on shape change, fire placement result.</summary>
        public void UpdatePreview(Camera cam)
        {
            if (_previewInstance == null)
            {
                UpdateCursorForUIHover();
                return;
            }

            Vector2 mousePos = _hub.ScreenToWorldOnGrid(cam, Input.mousePosition);
            CityCell mouseCell = _hub.GetMouseCell(mousePos);
            if (mouseCell == null)
            {
                _previewInstance.SetActive(false);
                _lastCellX = int.MinValue;
                _lastCellY = int.MinValue;
                UpdateCursorForUIHover();
                return;
            }

            Vector2 gridPosition = new Vector2(mouseCell.x, mouseCell.y);
            _previewInstance.SetActive(true);

            if (_hub.GetSelectedZoneType() == Zone.ZoneType.Road && _hub.HasRoadManager)
            {
                _hub.GetRoadGhostPreview(gridPosition, out GameObject roadPrefab, out Vector2 worldPos, out int sortingOrder);
                if (roadPrefab != _currentRoadGhostPrefab)
                {
                    _currentRoadGhostPrefab = roadPrefab;
                    Object.Destroy(_previewInstance);
                    _previewInstance = Object.Instantiate(roadPrefab);
                    SpriteRenderer sr = _previewInstance.GetComponent<SpriteRenderer>()
                        ?? _previewInstance.GetComponentInChildren<SpriteRenderer>();
                    if (sr != null)
                    {
                        _originalPreviewColor = new Color(1f, 1f, 1f, 0.5f);
                        sr.color = _originalPreviewColor;
                    }
                    _lastTintState = PreviewTintState.None;
                    foreach (var col in _previewInstance.GetComponentsInChildren<Collider2D>())
                        Object.Destroy(col);
                }
                _previewInstance.transform.position = new Vector3(worldPos.x, worldPos.y, 0f);
                foreach (SpriteRenderer sr in _previewInstance.GetComponentsInChildren<SpriteRenderer>())
                    if (sr != null) sr.sortingOrder = sortingOrder;
            }
            else
            {
                _currentRoadGhostPrefab = null;
                int buildingSize = _hub.GetSelectedBuildingSize();
                CityCell cell = _hub.GetCell((int)gridPosition.x, (int)gridPosition.y);
                if (cell == null)
                {
                    _previewInstance.SetActive(false);
                    _lastCellX = int.MinValue;
                    _lastCellY = int.MinValue;
                }
                else
                {
                    Vector2 newWorldPos = _hub.GetBuildingPlacementPos(gridPosition, buildingSize);
                    if (_hub.IsSelectedBuildingWaterPlant())
                        newWorldPos.y += _hub.TileHeight / 4f;
                    _previewInstance.transform.position = newWorldPos;

                    if (cell.x != _lastCellX || cell.y != _lastCellY)
                    {
                        _lastCellX = cell.x;
                        _lastCellY = cell.y;
                        PlacementResult result = _hub.CanPlace(_currentAssetId, cell.x, cell.y, _currentRotation, _currentZoneType);
                        _hub.FirePlacementResultChanged(result);
                    }
                }
            }

            UpdateCursorForUIHover();
        }

        // ── Placement tint ────────────────────────────────────────────────────────

        /// <summary>Tint preview green/red by placement result; fire reason changed event.</summary>
        public void ApplyPreviewTint(PlacementResult result)
        {
            if (_previewInstance == null) return;
            SpriteRenderer sr = _previewInstance.GetComponent<SpriteRenderer>()
                ?? _previewInstance.GetComponentInChildren<SpriteRenderer>();
            if (sr == null) return;

            if (result.IsAllowed)
            {
                if (_lastTintState != PreviewTintState.Valid)
                {
                    sr.color = PreviewTintGreen;
                    _lastTintState = PreviewTintState.Valid;
                }
            }
            else
            {
                if (_lastTintState != PreviewTintState.Invalid)
                {
                    sr.color = PreviewTintRed;
                    _lastTintState = PreviewTintState.Invalid;
                }
            }

            _hub.FirePlacementReasonChanged(result.Reason);
        }

        // ── UI hover ──────────────────────────────────────────────────────────────

        private void UpdateCursorForUIHover()
        {
            bool overUI = _hub.IsPointerOverUI();
            if (overUI == _isOverUI) return;
            _isOverUI = overUI;
            if (_isOverUI)
            {
                UnityEngine.Cursor.SetCursor(_hub.CursorTexture, Vector2.zero, CursorMode.Auto);
                return;
            }
            if (_activeCursorTexture != null)
                UnityEngine.Cursor.SetCursor(_activeCursorTexture, _activeCursorHotSpot, CursorMode.Auto);
            else
                UnityEngine.Cursor.SetCursor(_hub.CursorTexture, Vector2.zero, CursorMode.Auto);
        }

        // ── Texture scaling ───────────────────────────────────────────────────────

        private Texture2D GetScaledBulldozerTexture()
        {
            var src = _hub.BulldozerTexture;
            if (src == null) return null;
            int targetW = src.width / 2;
            int targetH = src.height / 2;
            if (targetW <= 0 || targetH <= 0) return src;
            if (_scaledBulldozerTexture != null && _scaledBulldozerTexture.width == targetW && _scaledBulldozerTexture.height == targetH)
                return _scaledBulldozerTexture;
            if (_scaledBulldozerTexture != null) Object.Destroy(_scaledBulldozerTexture);
            _scaledBulldozerTexture = ScaleTexture(src, targetW, targetH);
            return _scaledBulldozerTexture;
        }

        private static Texture2D ScaleTexture(Texture2D source, int newWidth, int newHeight)
        {
            RenderTexture rt = RenderTexture.GetTemporary(newWidth, newHeight);
            Graphics.Blit(source, rt);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = rt;
            var result = new Texture2D(newWidth, newHeight);
            result.ReadPixels(new Rect(0, 0, newWidth, newHeight), 0, 0);
            result.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);
            return result;
        }
    }
}
