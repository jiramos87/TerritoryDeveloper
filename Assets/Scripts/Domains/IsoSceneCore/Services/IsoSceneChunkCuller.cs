using UnityEngine;
using System;

namespace Territory.IsoSceneCore
{
    /// <summary>Visible-cell windowing service. Subscribe to IsoSceneCamera deltas; emits visible-set changed events. Configure in Start (invariant #12).</summary>
    public sealed class IsoSceneChunkCuller
    {
        private Camera _cam;
        private int _gridWidth;
        private int _gridHeight;
        private float _tileWidth = 1f;
        private float _tileHeight = 0.5f;
        private int _chunkSize = 16;
        private int _chunksX;
        private int _chunksY;
        private bool[,] _chunkVisible;
        private Vector3 _lastCamPos;
        private float _lastOrthoSize;
        private bool _configured;

        /// <summary>Fired when visible-set changes. Arg = (minCX, maxCX, minCY, maxCY) visible chunk range.</summary>
        public event Action<int, int, int, int> OnVisibleSetChanged;

        /// <summary>Number of visible chunks (leaf cells count). Used by tests.</summary>
        public int VisibleCellCount
        {
            get
            {
                if (_chunkVisible == null) return 0;
                int count = 0;
                for (int cx = 0; cx < _chunksX; cx++)
                    for (int cy = 0; cy < _chunksY; cy++)
                        if (_chunkVisible[cx, cy]) count++;
                return count;
            }
        }

        /// <summary>Configure culler with grid dimensions + camera. Call from Start.</summary>
        public void Configure(Camera cam, int gridWidth, int gridHeight, int chunkSize = 16, float tileWidth = 1f, float tileHeight = 0.5f)
        {
            _cam = cam;
            _gridWidth = gridWidth;
            _gridHeight = gridHeight;
            _chunkSize = chunkSize;
            _tileWidth = tileWidth;
            _tileHeight = tileHeight;
            _chunksX = Mathf.CeilToInt((float)gridWidth / chunkSize);
            _chunksY = Mathf.CeilToInt((float)gridHeight / chunkSize);
            _chunkVisible = new bool[_chunksX, _chunksY];
            _configured = true;
        }

        /// <summary>Update visible-set if camera changed. Call from LateUpdate.</summary>
        public void UpdateVisibility()
        {
            if (!_configured || _cam == null) return;
            Vector3 camPos = _cam.transform.position;
            float orthoSize = _cam.orthographicSize;
            if (camPos == _lastCamPos && Mathf.Approximately(orthoSize, _lastOrthoSize)) return;
            _lastCamPos = camPos;
            _lastOrthoSize = orthoSize;

            float halfH = orthoSize;
            float halfW = orthoSize * _cam.aspect;

            // Convert screen corners to grid coords
            Vector2 bl = WorldToGrid(new Vector2(camPos.x - halfW, camPos.y - halfH));
            Vector2 br = WorldToGrid(new Vector2(camPos.x + halfW, camPos.y - halfH));
            Vector2 tl = WorldToGrid(new Vector2(camPos.x - halfW, camPos.y + halfH));
            Vector2 tr = WorldToGrid(new Vector2(camPos.x + halfW, camPos.y + halfH));

            int minGX = Mathf.Min((int)bl.x, (int)br.x, (int)tl.x, (int)tr.x);
            int maxGX = Mathf.Max((int)bl.x, (int)br.x, (int)tl.x, (int)tr.x);
            int minGY = Mathf.Min((int)bl.y, (int)br.y, (int)tl.y, (int)tr.y);
            int maxGY = Mathf.Max((int)bl.y, (int)br.y, (int)tl.y, (int)tr.y);

            int minCX = Mathf.Max(0, minGX / _chunkSize - 1);
            int maxCX = Mathf.Min(_chunksX - 1, maxGX / _chunkSize + 1);
            int minCY = Mathf.Max(0, minGY / _chunkSize - 1);
            int maxCY = Mathf.Min(_chunksY - 1, maxGY / _chunkSize + 1);

            bool changed = false;
            for (int cx = 0; cx < _chunksX; cx++)
            {
                for (int cy = 0; cy < _chunksY; cy++)
                {
                    bool shouldBeVisible = cx >= minCX && cx <= maxCX && cy >= minCY && cy <= maxCY;
                    if (_chunkVisible[cx, cy] != shouldBeVisible)
                    {
                        _chunkVisible[cx, cy] = shouldBeVisible;
                        changed = true;
                    }
                }
            }

            if (changed)
                OnVisibleSetChanged?.Invoke(minCX, maxCX, minCY, maxCY);
        }

        /// <summary>Isometric world → grid coord conversion (inverse of standard iso formula).</summary>
        private Vector2 WorldToGrid(Vector2 world)
        {
            // Standard iso: wx = (gx - gy) * tileWidth/2, wy = (gx + gy) * tileHeight/2
            // Inverse: gx = wx/tileWidth + wy/tileHeight, gy = wy/tileHeight - wx/tileWidth
            float hw = _tileWidth * 0.5f;
            float hh = _tileHeight * 0.5f;
            float gx = world.x / (2f * hw) + world.y / (2f * hh);
            float gy = world.y / (2f * hh) - world.x / (2f * hw);
            return new Vector2(
                Mathf.Clamp(gx, 0, _gridWidth - 1),
                Mathf.Clamp(gy, 0, _gridHeight - 1));
        }
    }
}
