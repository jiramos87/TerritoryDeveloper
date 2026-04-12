using System.Collections.Generic;

namespace Territory.Utilities
{
/// <summary>
/// Generate unique procedural city names from combinable components.
/// Stateless utility — all methods static. Uses <c>System.Random</c> for deterministic seed-based generation.
/// </summary>
public static class CityNameGenerator
{
    private static readonly string[] Prefixes = {
        "San", "New", "Port", "Fort", "Lake", "West", "East",
        "North", "South", "Old", "Grand", "Mount", "Royal",
        "Silver", "Golden", "Cedar", "Pine", "Saint"
    };

    private static readonly string[] Roots = {
        "Chester", "Valley", "Haven", "Ridge", "Brook", "Field",
        "Wood", "Springs", "Creek", "Hill", "Crest", "Dale",
        "Glen", "Meadow", "Stone", "Bridge", "Ford", "Wick",
        "Bury", "Ville", "Milton", "Brighton", "Ashford",
        "Fairview", "Oakdale", "Riverside", "Greenfield",
        "Clearwater", "Maplewood", "Ironside", "Whitmore",
        "Blackwood", "Thornton", "Redmond", "Weston",
        "Stratford", "Lancaster", "Hartford", "Portland"
    };

    private static readonly string[] StandaloneNames = {
        "Aurora", "Concord", "Liberty", "Prosperity", "Harmony",
        "Summit", "Cascade", "Evergreen", "Horizon", "Pinnacle",
        "Sterling", "Avalon", "Meridian", "Solace", "Crescent",
        "Tranquil", "Providence", "Dominion", "Paramount", "Valor"
    };

    /// <summary>
    /// Generate a single name (may collide — use GenerateUnique for safety).
    /// </summary>
    public static string Generate(System.Random rng)
    {
        double roll = rng.NextDouble();

        if (roll < 0.40)
            return Prefixes[rng.Next(Prefixes.Length)] + " "
                 + Roots[rng.Next(Roots.Length)];
        if (roll < 0.70)
            return Roots[rng.Next(Roots.Length)];
        return StandaloneNames[rng.Next(StandaloneNames.Length)];
    }

    /// <summary>
    /// Generate name guaranteed unique within provided set.
    /// Adds name to usedNames before returning.
    /// </summary>
    public static string GenerateUnique(System.Random rng, HashSet<string> usedNames)
    {
        for (int attempt = 0; attempt < 200; attempt++)
        {
            string name = Generate(rng);
            if (!usedNames.Contains(name))
            {
                usedNames.Add(name);
                return name;
            }
        }
        string fallback = "Territory " + (usedNames.Count + 1);
        usedNames.Add(fallback);
        return fallback;
    }
}
}
