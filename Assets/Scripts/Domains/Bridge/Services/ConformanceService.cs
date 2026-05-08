namespace Domains.Bridge.Services
{
    /// <summary>
    /// Thin facade for bridge conformance kinds (Editor-only).
    /// Stage 7 tracer slice — wraps AgentBridgeCommandRunner conformance dispatch.
    /// Full extraction (inline walk + check methods) deferred to Stage 8+.
    /// </summary>
    public class ConformanceService
    {
        /// <summary>
        /// Returns the canonical conformance check kinds supported by this service.
        /// Eight kinds: palette_ramp, font_face, frame_style, panel_kind,
        /// caption, contrast_ratio, frame_sprite_bound, button_state_block.
        /// </summary>
        public static readonly string[] SupportedCheckKinds = new[]
        {
            "palette_ramp",
            "font_face",
            "frame_style",
            "panel_kind",
            "caption",
            "contrast_ratio",
            "frame_sprite_bound",
            "button_state_block",
        };

        /// <summary>
        /// Returns the number of supported conformance check kinds.
        /// Tracer-test anchor: ConformanceService.SupportedCheckKindCount == 8.
        /// </summary>
        public int SupportedCheckKindCount => SupportedCheckKinds.Length;
    }
}
