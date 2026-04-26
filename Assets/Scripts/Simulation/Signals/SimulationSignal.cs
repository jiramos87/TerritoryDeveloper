namespace Territory.Simulation.Signals
{
    /// <summary>City-sim depth signal inventory — locked 12-entry contract per <c>ia/specs/simulation-signals.md</c>.</summary>
    public enum SimulationSignal
    {
        PollutionAir = 0,
        PollutionLand = 1,
        PollutionWater = 2,
        Crime = 3,
        ServicePolice = 4,
        ServiceFire = 5,
        ServiceEducation = 6,
        ServiceHealth = 7,
        ServiceParks = 8,
        TrafficLevel = 9,
        WastePressure = 10,
        LandValue = 11,
    }
}
