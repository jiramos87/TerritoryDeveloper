using UnityEngine;

public class WaterMap
{
    private bool[,] waterCells;
    private int width;
    private int height;

    public WaterMap(int width, int height)
    {
        this.width = width;
        this.height = height;
        waterCells = new bool[width, height];
    }

    private void InitializeWaterCells()
    {
        // Initialize all water cells to false (no water)
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                waterCells[x, y] = false;
            }
        }
    }

    public bool IsWater(int x, int y)
    {
        if (!IsValidPosition(x, y))
        {
            Debug.LogError($"Invalid position: ({x}, {y})");
            return false;
        }
        return waterCells[x, y];
    }

    public void SetWater(int x, int y, bool isWater)
    {
        if (!IsValidPosition(x, y))
        {
            return;
        }
        Debug.Log($"Setting water at ({x}, {y}) to {isWater}");
        waterCells[x, y] = isWater;
    }

    public bool IsValidPosition(int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y < height;
    }

    // Define water at or below sea level
    public void InitializeWaterBodiesBasedOnHeight(HeightMap heightMap, int seaLevel)
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (heightMap.GetHeight(x, y) <= seaLevel)
                {
                    waterCells[x, y] = true;
                }
            }
        }
    }

    public WaterMapData GetSerializableData()
    {
        WaterMapData data = new WaterMapData();
        data.width = width;
        data.height = height;
        data.waterCells = new bool[width * height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                data.waterCells[x + y * width] = waterCells[x, y];
            }
        }

        return data;
    }

    public void LoadFromSerializableData(WaterMapData data)
    {
        if (data.width != width || data.height != height)
        {
            Debug.LogError("WaterMap dimensions mismatch!");
            return;
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                waterCells[x, y] = data.waterCells[x + y * width];
            }
        }
    }
}

[System.Serializable]
public class WaterMapData
{
    public int width;
    public int height;
    public bool[] waterCells; // Flattened 2D array for serialization
}
