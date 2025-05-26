using UnityEngine;

public class HeightMap
{
    private int[,] heights;
    private int width;
    private int height;

    public HeightMap(int width, int height)
    {
        this.width = width;
        this.height = height;
        heights = new int[width, height];
        InitializeHeights();
    }

    private void InitializeHeights()
    {
        // Initialize all heights to 1 (or whatever base height you prefer)
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                heights[x, y] = 1;
            }
        }
    }

    public void SetHeights(int[,] newHeights)
    {
        if (newHeights.GetLength(0) != width || newHeights.GetLength(1) != height)
        {
            Debug.LogError("Height map dimensions do not match!");
            return;
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                heights[x, y] = Mathf.Clamp(newHeights[x, y], 
                    TerrainManager.MIN_HEIGHT, 
                    TerrainManager.MAX_HEIGHT);
            }
        }
    }

    public int GetHeight(int x, int y)
    {
        if (!IsValidPosition(x, y))
        {
            return TerrainManager.MIN_HEIGHT; // Return minimum height for invalid positions
        }
        return heights[x, y];
    }

    public void SetHeight(int x, int y, int newHeight)
    {
        if (!IsValidPosition(x, y))
        {
            return;
        }

        heights[x, y] = Mathf.Clamp(newHeight, 
            TerrainManager.MIN_HEIGHT, 
            TerrainManager.MAX_HEIGHT);
    }

    public bool IsValidPosition(int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y < height;
    }

    public bool HasHeightDifference(int x, int y)
    {
        int currentHeight = GetHeight(x, y);

        // Check all 8 surrounding tiles
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;

                int nx = x + dx;
                int ny = y + dy;

                if (IsValidPosition(nx, ny))
                {
                    if (Mathf.Abs(GetHeight(nx, ny) - currentHeight) > 0)
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    public bool IsFlat(int startX, int startY, int size)
    {
        int baseHeight = GetHeight(startX, startY);

        for (int x = startX; x < startX + size; x++)
        {
            for (int y = startY; y < startY + size; y++)
            {
                if (!IsValidPosition(x, y) || GetHeight(x, y) != baseHeight)
                {
                    return false;
                }
            }
        }
        return true;
    }

    public HeightMapData GetSerializableData()
    {
        HeightMapData data = new HeightMapData();
        data.width = width;
        data.height = height;
        data.heights = new int[width * height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                data.heights[x + y * width] = heights[x, y];
            }
        }

        return data;
    }

    public void LoadFromSerializableData(HeightMapData data)
    {
        if (data.width != width || data.height != height)
        {
            Debug.LogError("HeightMap dimensions mismatch!");
            return;
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                heights[x, y] = data.heights[x + y * width];
            }
        }
    }

    // Helper methods for specific height checks
    public bool IsHigherThan(int x, int y, int compareX, int compareY)
    {
        if (!IsValidPosition(x, y) || !IsValidPosition(compareX, compareY))
        {
            return false;
        }
        return GetHeight(x, y) > GetHeight(compareX, compareY);
    }

    public bool HasUniformSlope(int x, int y)
    {
        int currentHeight = GetHeight(x, y);
        int higherCount = 0;
        int lowerCount = 0;

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;

                int nx = x + dx;
                int ny = y + dy;

                if (IsValidPosition(nx, ny))
                {
                    int neighborHeight = GetHeight(nx, ny);
                    if (neighborHeight > currentHeight) higherCount++;
                    if (neighborHeight < currentHeight) lowerCount++;
                }
            }
        }

        // Return true if the tile is either consistently sloping up or down
        return (higherCount > 0 && lowerCount == 0) || (lowerCount > 0 && higherCount == 0);
    }
}

[System.Serializable]
public class HeightMapData
{
    public int width;
    public int height;
    public int[] heights; // Flattened 2D array for serialization
}
