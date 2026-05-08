namespace Territory.Terrain
{
    /// <summary>Action to perform when terraforming cell. Lifted from TerraformingService nested enum to Core leaf so Roads can reference without crossing into Game.asmdef.</summary>
    public enum TerraformAction
    {
        None,
        Flatten,
        DiagonalToOrthogonal
    }
}
