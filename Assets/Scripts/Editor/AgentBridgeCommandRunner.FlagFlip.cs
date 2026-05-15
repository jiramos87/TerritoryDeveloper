using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Territory.Core;
using Domains.Bridge.Services;

// Wave D (vibe-coding-safety stage-5-0 TECH-36137) — flag_flip bridge kind.
// Invalidates FeatureFlags cache + re-hydrates from latest interchange snapshot.

public static partial class AgentBridgeCommandRunner
{
    /// <summary>
    /// <c>flag_flip</c> — invalidate <see cref="FeatureFlags"/> cache and re-hydrate
    /// from <c>tools/interchange/feature-flags-snapshot.json</c>.
    /// Params (optional): <c>slug</c> — if supplied, logs which flag was toggled upstream
    /// (Unity side always re-reads the full snapshot file).
    /// </summary>
    static void RunFlagFlip(string repoRoot, string commandId, string requestJson)
    {
        string slug = string.Empty;
        if (TryParseRequestEnvelope(requestJson, out AgentBridgeRequestEnvelopeDto env, out _))
            slug = env?.bridge_params?.slug ?? string.Empty;

        string snapshotPath = Path.GetFullPath(
            Path.Combine(repoRoot, "tools", "interchange", "feature-flags-snapshot.json"));

        FeatureFlags.InvalidateCache();
        FeatureFlags.HydrateFromJson(snapshotPath);

        bool snapshotExists = File.Exists(snapshotPath);
        string msg = string.IsNullOrEmpty(slug)
            ? $"flag_flip: cache invalidated + re-hydrated from '{snapshotPath}' (exists={snapshotExists})."
            : $"flag_flip: '{slug}' toggled upstream; cache invalidated + re-hydrated from '{snapshotPath}' (exists={snapshotExists}).";

        Debug.Log($"[AgentBridge] {msg}");

        // Build a minimal JSON response (ok + message) without importing a full response builder.
        string escapedMsg = msg.Replace("\\", "\\\\").Replace("\"", "\\\"");
        string responseJson = $"{{\"command_id\":\"{commandId}\",\"ok\":true,\"message\":\"{escapedMsg}\"}}";
        CompleteOrFail(repoRoot, commandId, responseJson);
    }
}
