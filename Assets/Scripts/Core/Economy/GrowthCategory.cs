namespace Territory.Simulation
{
    /// <summary>Growth budget category. Lifted from GrowthBudgetManager to Core leaf so Domains/Roads + interface surfaces can reference without crossing into Game.asmdef.</summary>
    public enum GrowthCategory { Roads, Energy, Water, Zoning }
}
