using System.Collections.Generic;
using UnityEngine;
using Territory.Utilities;

namespace Territory.Geography
{
/// <summary>
/// The regional-scale map: a grid of TerritoryData where each cell represents one territory/city.
/// Our playable map is one cell in this grid. Player at center (2,2) for 5x5. Flat array for JsonUtility.
/// </summary>
[System.Serializable]
public class RegionalMap
{
    public int width;
    public int height;
    public int playerX;
    public int playerY;
    public int seed;
    public TerritoryData[] territories;

    public TerritoryData GetTerritory(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height)
            return null;
        return territories[y * width + x];
    }

    public TerritoryData GetPlayerTerritory()
    {
        return GetTerritory(playerX, playerY);
    }

    public TerritoryData GetNeighborByBorder(int fromX, int fromY, int border)
    {
        int nx = fromX, ny = fromY;
        switch (border)
        {
            case 0: ny -= 1; break;
            case 1: ny += 1; break;
            case 2: nx -= 1; break;
            case 3: nx += 1; break;
        }
        return GetTerritory(nx, ny);
    }

    public TerritoryData GetPlayerNeighbor(int border)
    {
        return GetNeighborByBorder(playerX, playerY, border);
    }

    public static RegionalMap Generate(int w, int h, int seed = -1)
    {
        if (seed < 0)
            seed = Random.Range(0, int.MaxValue);

        var map = new RegionalMap
        {
            width = w,
            height = h,
            seed = seed,
            playerX = w / 2,
            playerY = h / 2
        };
        map.territories = new TerritoryData[w * h];

        var rng = new System.Random(seed);
        var usedNames = new HashSet<string>();

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                TerritoryData territory;

                if (x == map.playerX && y == map.playerY)
                {
                    territory = new TerritoryData
                    {
                        gridX = x,
                        gridY = y,
                        isPlayerTerritory = true,
                        cityName = CityNameGenerator.GenerateUnique(rng, usedNames),
                        category = TerritoryData.CityCategory.Village,
                        population = 0,
                        economicStrength = 0.5f
                    };
                }
                else
                {
                    territory = TerritoryData.GenerateRandom(x, y, rng, usedNames);
                }

                map.territories[y * w + x] = territory;
            }
        }

        GenerateConnections(map, rng);

        return map;
    }

    private static void GenerateConnections(RegionalMap map, System.Random rng)
    {
        for (int y = 0; y < map.height; y++)
        {
            for (int x = 0; x < map.width; x++)
            {
                var territory = map.GetTerritory(x, y);
                if (territory.category == TerritoryData.CityCategory.Uninhabited)
                    continue;

                var eastNeighbor = map.GetTerritory(x + 1, y);
                if (eastNeighbor != null
                    && eastNeighbor.category != TerritoryData.CityCategory.Uninhabited
                    && !territory.hasEastConnection
                    && !eastNeighbor.hasWestConnection)
                {
                    float probability = GetConnectionProbability(territory, eastNeighbor);
                    if (rng.NextDouble() < probability)
                    {
                        territory.hasEastConnection = true;
                        eastNeighbor.hasWestConnection = true;
                    }
                }

                var northNeighbor = map.GetTerritory(x, y + 1);
                if (northNeighbor != null
                    && northNeighbor.category != TerritoryData.CityCategory.Uninhabited
                    && !territory.hasNorthConnection
                    && !northNeighbor.hasSouthConnection)
                {
                    float probability = GetConnectionProbability(territory, northNeighbor);
                    if (rng.NextDouble() < probability)
                    {
                        territory.hasNorthConnection = true;
                        northNeighbor.hasSouthConnection = true;
                    }
                }
            }
        }

        EnsurePlayerConnections(map, rng, 2);
    }

    private static float GetConnectionProbability(TerritoryData a, TerritoryData b)
    {
        int maxCategory = Mathf.Max((int)a.category, (int)b.category);
        switch (maxCategory)
        {
            case 1: return 0.3f;
            case 2: return 0.5f;
            case 3: return 0.7f;
            case 4: return 0.9f;
            default: return 0.0f;
        }
    }

    private static void EnsurePlayerConnections(RegionalMap map, System.Random rng, int minConnections)
    {
        var player = map.GetPlayerTerritory();
        if (player.ConnectionCount() >= minConnections)
            return;

        var candidateBorders = new List<int>();
        for (int border = 0; border < 4; border++)
        {
            if (player.GetConnection(border))
                continue;
            var neighbor = map.GetNeighborByBorder(map.playerX, map.playerY, border);
            if (neighbor != null && neighbor.category != TerritoryData.CityCategory.Uninhabited)
                candidateBorders.Add(border);
        }

        ShuffleList(candidateBorders, rng);
        foreach (int border in candidateBorders)
        {
            if (player.ConnectionCount() >= minConnections)
                break;
            player.SetConnection(border, true);
            var neighbor = map.GetNeighborByBorder(map.playerX, map.playerY, border);
            if (neighbor != null)
                neighbor.SetConnection(TerritoryData.OppositeBorder(border), true);
        }

        if (player.ConnectionCount() < minConnections)
        {
            var usedNames = new HashSet<string>();
            foreach (var t in map.territories)
            {
                if (t != null && !string.IsNullOrEmpty(t.cityName))
                    usedNames.Add(t.cityName);
            }

            for (int border = 0; border < 4; border++)
            {
                if (player.ConnectionCount() >= minConnections)
                    break;
                if (player.GetConnection(border))
                    continue;
                var neighbor = map.GetNeighborByBorder(map.playerX, map.playerY, border);
                if (neighbor == null)
                    continue;

                neighbor.category = TerritoryData.CityCategory.Village;
                neighbor.cityName = CityNameGenerator.GenerateUnique(rng, usedNames);
                neighbor.population = rng.Next(500, 5001);
                neighbor.economicStrength = (float)rng.NextDouble();

                player.SetConnection(border, true);
                neighbor.SetConnection(TerritoryData.OppositeBorder(border), true);
            }
        }
    }

    private static void ShuffleList<T>(List<T> list, System.Random rng)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }

    public string ToDebugString()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("RegionalMap " + width + "x" + height + ", seed=" + seed + ", player=(" + playerX + "," + playerY + ")");
        for (int y = height - 1; y >= 0; y--)
        {
            for (int x = 0; x < width; x++)
            {
                var t = GetTerritory(x, y);
                string marker = t.isPlayerTerritory ? "*" : " ";
                string name = t.category == TerritoryData.CityCategory.Uninhabited ? "---" : t.cityName;
                string conns = "";
                if (t.hasNorthConnection) conns += "N";
                if (t.hasSouthConnection) conns += "S";
                if (t.hasEastConnection) conns += "E";
                if (t.hasWestConnection) conns += "W";
                sb.Append("[" + marker + name + "(" + conns + ")] ");
            }
            sb.AppendLine();
        }
        return sb.ToString();
    }
}
}
