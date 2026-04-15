namespace Territory.Audio
{
    /// <summary>
    /// Facade for the Blip audio subsystem.
    /// <para>
    /// <b>Stage 2.3 T2.3.2</b> fills <see cref="Bind"/> and <see cref="Unbind"/>
    /// with cached resolution, mixer routing, and player-pool wiring.
    /// Until then both methods are intentional no-ops so that
    /// <see cref="BlipCatalog"/> can compile and ship without forward-ref errors.
    /// </para>
    /// </summary>
    public static class BlipEngine
    {
        /// <summary>
        /// Called by <see cref="BlipCatalog.Awake"/> immediately before the
        /// catalog raises its ready flag.  Full impl: Stage 2.3 T2.3.2.
        /// </summary>
        public static void Bind(BlipCatalog c) { /* Stage 2.3 T2.3.2 fills */ }

        /// <summary>
        /// Called by <see cref="BlipCatalog.OnDestroy"/>.
        /// Full impl: Stage 2.3 T2.3.2.
        /// </summary>
        public static void Unbind(BlipCatalog c) { /* Stage 2.3 T2.3.2 fills */ }

        /// <summary>
        /// Called by <see cref="BlipPlayer.Awake"/> after the pool is built.
        /// Full impl: Stage 2.3 T2.3.2 — wires the player reference into the engine facade.
        /// Until then this is an intentional no-op so that <see cref="BlipPlayer"/> can
        /// compile and ship without forward-ref errors.
        /// </summary>
        public static void Bind(BlipPlayer p) { /* Stage 2.3 T2.3.2 fills */ }

        /// <summary>
        /// Called by <see cref="BlipPlayer.OnDestroy"/>.
        /// Full impl: Stage 2.3 T2.3.2.
        /// </summary>
        public static void Unbind(BlipPlayer p) { /* Stage 2.3 T2.3.2 fills */ }
    }
}
