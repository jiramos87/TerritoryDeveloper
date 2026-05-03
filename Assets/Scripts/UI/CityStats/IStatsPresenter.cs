using System;
using System.Collections.Generic;

namespace Territory.UI.CityStatsHandoff
{
    /// <summary>
    /// Tick-driven read-model surface for city-stats UI consumers. Implementations
    /// expose a flat <c>key → live-value</c> binding dictionary refreshed on the
    /// simulation tick edge (see <see cref="OnRefreshed"/>) so renderers stay
    /// event-driven and never poll producers in <c>Update</c> (invariant #3).
    /// </summary>
    /// <remarks>
    /// Stage 13.5 (TECH-9868) — extracted to decouple the bake-baked
    /// <c>city-stats-handoff.prefab</c> rows from concrete <see cref="Territory.Economy.CityStats"/>
    /// + <see cref="Territory.Economy.CityStatsFacade"/> wiring. Adapters consume
    /// <see cref="Bindings"/> entries by key when <see cref="IsReady"/> is true and
    /// re-paint on every <see cref="OnRefreshed"/> fire.
    /// </remarks>
    public interface IStatsPresenter
    {
        /// <summary>Flat binding registry. Renderers cast the boxed value to the expected type.</summary>
        IReadOnlyDictionary<string, Func<object>> Bindings { get; }

        /// <summary>Fires once per simulation tick after producers settle (and on every <see cref="RequestRefresh"/>).</summary>
        event Action OnRefreshed;

        /// <summary>True when producers are wired AND <see cref="Bindings"/> is populated. Adapters MUST gate render on this.</summary>
        bool IsReady { get; }

        /// <summary>Force a synchronous out-of-band <see cref="OnRefreshed"/> fire. Idempotent — does not re-subscribe producers.</summary>
        void RequestRefresh();
    }
}
