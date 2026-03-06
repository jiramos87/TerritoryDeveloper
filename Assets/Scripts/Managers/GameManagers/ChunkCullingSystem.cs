using UnityEngine;

namespace Territory.Core
{
    /// <summary>
    /// Chunk-based camera culling system. Divides the grid into rectangular chunks and
    /// toggles their visibility based on the camera viewport, avoiding draw calls for
    /// off-screen tiles. Extracted from GridManager to reduce its responsibilities.
    /// </summary>
    public class ChunkCullingSystem
    {
        private readonly GridManager grid;
        private readonly int chunkSize;

        public GameObject[,] chunkObjects;
        public bool[,] chunkActiveState;
        public int chunksX;
        public int chunksY;
        private Camera cachedCamera;
        private Vector3 lastCameraPosition;
        private float lastOrthoSize;

        public ChunkCullingSystem(GridManager grid, int chunkSize, Camera initialCamera)
        {
            this.grid = grid;
            this.chunkSize = chunkSize;
            this.cachedCamera = initialCamera;

            chunksX = Mathf.CeilToInt((float)grid.width / chunkSize);
            chunksY = Mathf.CeilToInt((float)grid.height / chunkSize);
            chunkObjects = new GameObject[chunksX, chunksY];
            chunkActiveState = new bool[chunksX, chunksY];
        }

        /// <summary>
        /// Checks whether the camera has moved and, if so, activates/deactivates chunks
        /// based on the visible grid range. Call from LateUpdate.
        /// </summary>
        public void UpdateVisibility()
        {
            if (cachedCamera == null) cachedCamera = Camera.main;
            if (cachedCamera == null) return;

            Vector3 camPos = cachedCamera.transform.position;
            float orthoSize = cachedCamera.orthographicSize;
            if (camPos == lastCameraPosition && Mathf.Approximately(orthoSize, lastOrthoSize))
                return;
            lastCameraPosition = camPos;
            lastOrthoSize = orthoSize;

            float halfH = orthoSize;
            float halfW = orthoSize * cachedCamera.aspect;
            Vector2 bl = new Vector2(camPos.x - halfW, camPos.y - halfH);
            Vector2 br = new Vector2(camPos.x + halfW, camPos.y - halfH);
            Vector2 tl = new Vector2(camPos.x - halfW, camPos.y + halfH);
            Vector2 tr = new Vector2(camPos.x + halfW, camPos.y + halfH);

            int minGridX = Mathf.Min((int)grid.GetGridPosition(bl).x, (int)grid.GetGridPosition(br).x, (int)grid.GetGridPosition(tl).x, (int)grid.GetGridPosition(tr).x);
            int maxGridX = Mathf.Max((int)grid.GetGridPosition(bl).x, (int)grid.GetGridPosition(br).x, (int)grid.GetGridPosition(tl).x, (int)grid.GetGridPosition(tr).x);
            int minGridY = Mathf.Min((int)grid.GetGridPosition(bl).y, (int)grid.GetGridPosition(br).y, (int)grid.GetGridPosition(tl).y, (int)grid.GetGridPosition(tr).y);
            int maxGridY = Mathf.Max((int)grid.GetGridPosition(bl).y, (int)grid.GetGridPosition(br).y, (int)grid.GetGridPosition(tl).y, (int)grid.GetGridPosition(tr).y);

            int minCX = Mathf.Max(0, minGridX / chunkSize - 1);
            int maxCX = Mathf.Min(chunksX - 1, maxGridX / chunkSize + 1);
            int minCY = Mathf.Max(0, minGridY / chunkSize - 1);
            int maxCY = Mathf.Min(chunksY - 1, maxGridY / chunkSize + 1);

            for (int cx = 0; cx < chunksX; cx++)
            {
                for (int cy = 0; cy < chunksY; cy++)
                {
                    bool shouldBeActive = cx >= minCX && cx <= maxCX && cy >= minCY && cy <= maxCY;
                    if (chunkActiveState[cx, cy] != shouldBeActive)
                    {
                        chunkActiveState[cx, cy] = shouldBeActive;
                        chunkObjects[cx, cy].SetActive(shouldBeActive);
                    }
                }
            }
        }
    }
}
