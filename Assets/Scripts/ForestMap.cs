using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages the forest data for the entire grid, tracking different forest types per cell.
/// Provides efficient querying and updating of forest information.
/// </summary>
public class ForestMap
{
    private Forest.ForestType[,] forestGrid;
    private int width;
    private int height;
    
    /// <summary>
    /// Initialize forest map with specified dimensions
    /// </summary>
    public ForestMap(int width, int height)
    {
        this.width = width;
        this.height = height;
        forestGrid = new Forest.ForestType[width, height];
        
        // Initialize all cells to None
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                forestGrid[x, y] = Forest.ForestType.None;
            }
        }
    }
    
    /// <summary>
    /// Initialize the forest map from a predefined matrix
    /// </summary>
    public void InitializeFromMatrix(Forest.ForestType[,] initialForestCells)
    {
        int matrixWidth = initialForestCells.GetLength(1);
        int matrixHeight = initialForestCells.GetLength(0);
        
        for (int y = 0; y < Mathf.Min(height, matrixHeight); y++)
        {
            for (int x = 0; x < Mathf.Min(width, matrixWidth); x++)
            {
                forestGrid[x, y] = initialForestCells[y, x]; // Note: matrix is [row, col] but grid is [x, y]
            }
        }
    }
    
    /// <summary>
    /// Check if position is within valid bounds
    /// </summary>
    public bool IsValidPosition(int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y < height;
    }
    
    /// <summary>
    /// Get the forest type at specified position
    /// </summary>
    public Forest.ForestType GetForestType(int x, int y)
    {
        if (!IsValidPosition(x, y))
            return Forest.ForestType.None;
            
        return forestGrid[x, y];
    }
    
    /// <summary>
    /// Set the forest type at specified position
    /// </summary>
    public void SetForestType(int x, int y, Forest.ForestType forestType)
    {
        if (!IsValidPosition(x, y))
            return;
            
        forestGrid[x, y] = forestType;
    }
    
    /// <summary>
    /// Check if position has any forest (not None)
    /// </summary>
    public bool IsForest(int x, int y)
    {
        return GetForestType(x, y) != Forest.ForestType.None;
    }
    
    /// <summary>
    /// Set forest state (backward compatibility method)
    /// </summary>
    public void SetForest(int x, int y, bool hasForest)
    {
        if (hasForest)
        {
            // Default to medium forest for backward compatibility
            SetForestType(x, y, Forest.ForestType.Medium);
        }
        else
        {
            SetForestType(x, y, Forest.ForestType.None);
        }
    }
    
    /// <summary>
    /// Get total count of all forest cells (excluding None)
    /// </summary>
    public int GetTotalForestCells()
    {
        int count = 0;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (forestGrid[x, y] != Forest.ForestType.None)
                    count++;
            }
        }
        return count;
    }
    
    /// <summary>
    /// Get count of each forest type
    /// </summary>
    public Dictionary<Forest.ForestType, int> GetForestTypeCounts()
    {
        var counts = new Dictionary<Forest.ForestType, int>
        {
            { Forest.ForestType.None, 0 },
            { Forest.ForestType.Sparse, 0 },
            { Forest.ForestType.Medium, 0 },
            { Forest.ForestType.Dense, 0 }
        };
        
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                counts[forestGrid[x, y]]++;
            }
        }
        
        return counts;
    }
    
    /// <summary>
    /// Get forest coverage percentage (excluding None type)
    /// </summary>
    public float GetForestCoveragePercentage()
    {
        int totalCells = width * height;
        int forestCells = GetTotalForestCells();
        
        if (totalCells == 0)
            return 0f;
            
        return (float)forestCells / totalCells * 100f;
    }
    
    /// <summary>
    /// Count adjacent forest cells (any type except None)
    /// </summary>
    public int GetAdjacentForestCount(int centerX, int centerY)
    {
        int count = 0;
        
        // Check all 8 adjacent positions
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                // Skip center position
                if (dx == 0 && dy == 0)
                    continue;
                    
                int x = centerX + dx;
                int y = centerY + dy;
                
                if (IsValidPosition(x, y) && IsForest(x, y))
                    count++;
            }
        }
        
        return count;
    }
    
    /// <summary>
    /// Get positions adjacent to any forest around a center position
    /// </summary>
    public List<Vector2Int> GetPositionsAdjacentToForest(int centerX, int centerY)
    {
        var adjacentPositions = new List<Vector2Int>();
        
        // Check all 8 adjacent positions
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                // Skip center position
                if (dx == 0 && dy == 0)
                    continue;
                    
                int x = centerX + dx;
                int y = centerY + dy;
                
                if (IsValidPosition(x, y))
                    adjacentPositions.Add(new Vector2Int(x, y));
            }
        }
        
        return adjacentPositions;
    }
    
    /// <summary>
    /// Find the nearest forest of any type to a given position
    /// </summary>
    public Vector2Int? FindNearestForest(int fromX, int fromY, int searchRadius = 5)
    {
        float minDistance = float.MaxValue;
        Vector2Int? nearestForest = null;
        
        for (int x = Mathf.Max(0, fromX - searchRadius); x < Mathf.Min(width, fromX + searchRadius + 1); x++)
        {
            for (int y = Mathf.Max(0, fromY - searchRadius); y < Mathf.Min(height, fromY + searchRadius + 1); y++)
            {
                if (IsForest(x, y))
                {
                    float distance = Vector2.Distance(new Vector2(fromX, fromY), new Vector2(x, y));
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        nearestForest = new Vector2Int(x, y);
                    }
                }
            }
        }
        
        return nearestForest;
    }
    
    /// <summary>
    /// Find the nearest forest of a specific type to a given position
    /// </summary>
    public Vector2Int? FindNearestForestOfType(int fromX, int fromY, Forest.ForestType targetType, int searchRadius = 5)
    {
        float minDistance = float.MaxValue;
        Vector2Int? nearestForest = null;
        
        for (int x = Mathf.Max(0, fromX - searchRadius); x < Mathf.Min(width, fromX + searchRadius + 1); x++)
        {
            for (int y = Mathf.Max(0, fromY - searchRadius); y < Mathf.Min(height, fromY + searchRadius + 1); y++)
            {
                if (GetForestType(x, y) == targetType)
                {
                    float distance = Vector2.Distance(new Vector2(fromX, fromY), new Vector2(x, y));
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        nearestForest = new Vector2Int(x, y);
                    }
                }
            }
        }
        
        return nearestForest;
    }
    
    /// <summary>
    /// Get all forest positions of a specific type
    /// </summary>
    public List<Vector2Int> GetAllForestsOfType(Forest.ForestType forestType)
    {
        var forests = new List<Vector2Int>();
        
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (forestGrid[x, y] == forestType)
                {
                    forests.Add(new Vector2Int(x, y));
                }
            }
        }
        
        return forests;
    }
    
    /// <summary>
    /// Get all forest positions (any type except None)
    /// </summary>
    public List<Vector2Int> GetAllForests()
    {
        var forests = new List<Vector2Int>();
        
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (forestGrid[x, y] != Forest.ForestType.None)
                {
                    forests.Add(new Vector2Int(x, y));
                }
            }
        }
        
        return forests;
    }
    
    /// <summary>
    /// Check if an area has minimum forest coverage
    /// </summary>
    public bool HasMinimumForestCoverage(int centerX, int centerY, int radius, float minimumPercentage)
    {
        int totalCells = 0;
        int forestCells = 0;
        
        for (int x = centerX - radius; x <= centerX + radius; x++)
        {
            for (int y = centerY - radius; y <= centerY + radius; y++)
            {
                if (IsValidPosition(x, y))
                {
                    totalCells++;
                    if (IsForest(x, y))
                        forestCells++;
                }
            }
        }
        
        if (totalCells == 0)
            return false;
            
        float coverage = (float)forestCells / totalCells * 100f;
        return coverage >= minimumPercentage;
    }
    
    /// <summary>
    /// Clear all forests in a rectangular area
    /// </summary>
    public void ClearArea(int startX, int startY, int endX, int endY)
    {
        for (int x = startX; x <= endX; x++)
        {
            for (int y = startY; y <= endY; y++)
            {
                if (IsValidPosition(x, y))
                {
                    SetForestType(x, y, Forest.ForestType.None);
                }
            }
        }
    }
    
    /// <summary>
    /// Clear all forests in the entire map
    /// </summary>
    public void ClearAllForest()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                forestGrid[x, y] = Forest.ForestType.None;
            }
        }
    }
    
    /// <summary>
    /// Clear all forests of a specific type
    /// </summary>
    public void ClearAllForestOfType(Forest.ForestType forestType)
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (forestGrid[x, y] == forestType)
                {
                    forestGrid[x, y] = Forest.ForestType.None;
                }
            }
        }
    }
    
    /// <summary>
    /// Fill an area with a specific forest type
    /// </summary>
    public void FillArea(int startX, int startY, int endX, int endY, Forest.ForestType forestType)
    {
        for (int x = startX; x <= endX; x++)
        {
            for (int y = startY; y <= endY; y++)
            {
                if (IsValidPosition(x, y))
                {
                    SetForestType(x, y, forestType);
                }
            }
        }
    }
    
    /// <summary>
    /// Get grid dimensions
    /// </summary>
    public Vector2Int GetDimensions()
    {
        return new Vector2Int(width, height);
    }
}
