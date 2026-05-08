namespace Domains.Bridge
{
    /// <summary>
    /// Public facade interface for the Bridge domain (Editor-only).
    /// Consumers bind to this interface — never to AgentBridgeCommandRunner directly.
    /// Stage 7 surface: MutationDispatchService + ConformanceService extracted in tracer slice.
    /// Guardrail #14 mutation dispatch shape preserved.
    /// </summary>
    public interface IBridge
    {
        /// <summary>Dispatch a mutation bridge kind. Returns true if handled.</summary>
        bool TryDispatchMutation(string kind, string repoRoot, string commandId, string requestJson);

        /// <summary>Run design conformance check. Returns JSON result string.</summary>
        string RunConformance(string repoRoot, string commandId, string requestJson);
    }
}
