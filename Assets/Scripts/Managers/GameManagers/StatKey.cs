namespace Territory.Economy
{
    /// <summary>
    /// Canonical metric identifiers for <see cref="IStatsReadModel"/> and columnar ring-buffer store (TECH-304).
    /// One member per public scalar/list-backed field in <see cref="CityStats"/> City Data region (plus envelope slots),
    /// plus multi-scale stubs. Non-numeric fields map to scalar encodings (length, count, epoch seconds, 0/1 bool).
    /// Composed by columnar ring-buffer store (TECH-304).
    /// </summary>
    public enum StatKey
    {
        // --- Core ---
        /// <summary>Game calendar date encoded as Unix epoch seconds (UTC) when sampled.</summary>
        CurrentDateEpochSeconds,
        Population,
        Money,
        Happiness,
        Pollution,

        // --- Top-level zone / building ---
        ResidentialZoneCount,
        ResidentialBuildingCount,
        CommercialZoneCount,
        CommercialBuildingCount,
        IndustrialZoneCount,
        IndustrialBuildingCount,

        // --- Residential tier ---
        ResidentialLightBuildingCount,
        ResidentialLightZoningCount,
        ResidentialMediumBuildingCount,
        ResidentialMediumZoningCount,
        ResidentialHeavyBuildingCount,
        ResidentialHeavyZoningCount,

        // --- Commercial tier ---
        CommercialLightBuildingCount,
        CommercialLightZoningCount,
        CommercialMediumBuildingCount,
        CommercialMediumZoningCount,
        CommercialHeavyBuildingCount,
        CommercialHeavyZoningCount,

        // --- Industrial tier ---
        IndustrialLightBuildingCount,
        IndustrialLightZoningCount,
        IndustrialMediumBuildingCount,
        IndustrialMediumZoningCount,
        IndustrialHeavyBuildingCount,
        IndustrialHeavyZoningCount,

        RoadCount,
        GrassCount,

        CityPowerConsumption,
        CityPowerOutput,

        /// <summary>String length of <see cref="CityStats.cityName"/> when encoded as scalar.</summary>
        CityNameLength,

        CityWaterConsumption,
        CityWaterOutput,

        ForestCellCount,
        ForestCoveragePercentage,

        /// <summary>1f when <see cref="CityStats.simulateGrowth"/> is true, else 0f.</summary>
        SimulateGrowth01,

        /// <summary><see cref="CityStats.communes"/> count.</summary>
        CommuneCount,

        TotalEnvelopeCap,

        EnvelopeRemainingSubType0,
        EnvelopeRemainingSubType1,
        EnvelopeRemainingSubType2,
        EnvelopeRemainingSubType3,
        EnvelopeRemainingSubType4,
        EnvelopeRemainingSubType5,
        EnvelopeRemainingSubType6,

        ActiveBondDebt,
        MonthlyBondRepayment,

        // --- Multi-scale stubs (filled in Stage 3+) ---
        /// <summary>Region aggregate population — stub until region facade.</summary>
        RegionPopulation,
        /// <summary>Country aggregate population — stub until country facade.</summary>
        CountryPopulation,
    }
}
