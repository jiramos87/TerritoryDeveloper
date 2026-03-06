using System.Collections.Generic;
using Territory.Utilities;

namespace Territory.Geography
{
/// <summary>
/// Data model for a single territory in the regional map.
/// Each territory contains one city (or is uninhabited).
/// Serializable for save/load via JsonUtility.
/// Border convention: 0=South, 1=North, 2=West, 3=East (same as InterstateManager).
/// </summary>
[System.Serializable]
public class TerritoryData
{
    public int gridX;
    public int gridY;
    public string cityName;
    public bool isPlayerTerritory;
    public int population;
    public CityCategory category;
    public float economicStrength;

    public bool hasNorthConnection;
    public bool hasSouthConnection;
    public bool hasEastConnection;
    public bool hasWestConnection;

    public enum CityCategory
    {
        Uninhabited = 0,
        Village = 1,
        Town = 2,
        City = 3,
        Metropolis = 4
    }

    public bool GetConnection(int border)
    {
        switch (border)
        {
            case 0: return hasSouthConnection;
            case 1: return hasNorthConnection;
            case 2: return hasWestConnection;
            case 3: return hasEastConnection;
            default: return false;
        }
    }

    public void SetConnection(int border, bool value)
    {
        switch (border)
        {
            case 0: hasSouthConnection = value; break;
            case 1: hasNorthConnection = value; break;
            case 2: hasWestConnection = value; break;
            case 3: hasEastConnection = value; break;
        }
    }

    public static int OppositeBorder(int border)
    {
        switch (border)
        {
            case 0: return 1;
            case 1: return 0;
            case 2: return 3;
            case 3: return 2;
            default: return -1;
        }
    }

    public int ConnectionCount()
    {
        int count = 0;
        if (hasNorthConnection) count++;
        if (hasSouthConnection) count++;
        if (hasEastConnection) count++;
        if (hasWestConnection) count++;
        return count;
    }

    public List<int> GetConnectedBorders()
    {
        var borders = new List<int>();
        if (hasSouthConnection) borders.Add(0);
        if (hasNorthConnection) borders.Add(1);
        if (hasWestConnection) borders.Add(2);
        if (hasEastConnection) borders.Add(3);
        return borders;
    }

    public string GetCategoryDisplayName()
    {
        switch (category)
        {
            case CityCategory.Uninhabited: return "Wilderness";
            case CityCategory.Village: return "Village";
            case CityCategory.Town: return "Town";
            case CityCategory.City: return "City";
            case CityCategory.Metropolis: return "Metropolis";
            default: return "Unknown";
        }
    }

    public string GetPopulationDisplay()
    {
        if (population >= 1000000) return (population / 1000000f).ToString("F1") + "M";
        if (population >= 1000) return (population / 1000f).ToString("F0") + "K";
        return population.ToString();
    }

    public static TerritoryData GenerateRandom(
        int gx, int gy,
        System.Random rng,
        HashSet<string> usedNames)
    {
        var data = new TerritoryData
        {
            gridX = gx,
            gridY = gy,
            isPlayerTerritory = false,
            economicStrength = (float)rng.NextDouble()
        };

        if (rng.NextDouble() < 0.15)
        {
            data.category = CityCategory.Uninhabited;
            data.cityName = "";
            data.population = 0;
            data.economicStrength = 0f;
            return data;
        }

        data.cityName = CityNameGenerator.GenerateUnique(rng, usedNames);

        double roll = rng.NextDouble();
        if (roll < 0.30)
        {
            data.category = CityCategory.Village;
            data.population = rng.Next(500, 5001);
        }
        else if (roll < 0.65)
        {
            data.category = CityCategory.Town;
            data.population = rng.Next(5001, 50001);
        }
        else if (roll < 0.90)
        {
            data.category = CityCategory.City;
            data.population = rng.Next(50001, 500001);
        }
        else
        {
            data.category = CityCategory.Metropolis;
            data.population = rng.Next(500001, 2000001);
        }

        return data;
    }
}
}
