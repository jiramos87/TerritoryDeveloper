using System;
using UnityEngine;

namespace Domains.Bridge.Services
{
    /// <summary>
    /// Response-builder POCO for bridge command kinds (Editor-only).
    /// Stage 6.1 extraction from AgentBridgeCommandRunner stem.
    /// Owns BuildObservabilityResponseJson, BuildAgentContextResponseJson,
    /// BuildPlayModeBridgeResponseJson helpers so the stem partial stays ≤200 LOC.
    /// Lives in the implicit editor assembly — reaches AgentBridgeResponseFileDto,
    /// AgentBridgeLogLineDto without asmdef boundary.
    /// </summary>
    public static class BridgeCommandService
    {
        /// <summary>Build a generic observability response JSON string.</summary>
        public static string BuildObservabilityResponseJson(
            string commandId,
            bool ok,
            string storage,
            bool postgresOnly,
            string[] artifactPaths,
            string error,
            AgentBridgeLogLineDto[] logLines)
        {
            var resp = new AgentBridgeResponseFileDto
            {
                schema_version   = 1,
                artifact         = "unity_agent_bridge_response",
                command_id       = commandId,
                ok               = ok,
                completed_at_utc = DateTime.UtcNow.ToString("o"),
                storage          = storage,
                postgres_only    = postgresOnly,
                error            = ok ? string.Empty : (error ?? string.Empty),
                artifact_paths   = artifactPaths ?? Array.Empty<string>(),
                log_lines        = logLines ?? Array.Empty<AgentBridgeLogLineDto>(),
                play_mode_state  = string.Empty,
                ready            = false,
                already_playing  = false,
                already_stopped  = false,
                has_grid_dimensions = false,
                grid_width       = 0,
                grid_height      = 0,
            };
            return JsonUtility.ToJson(resp, true);
        }

        /// <summary>Build an agent-context response JSON string from an outcome.</summary>
        public static string BuildAgentContextResponseJson(
            string commandId,
            AgentBridgeAgentContextOutcome outcome)
        {
            var resp = new AgentBridgeResponseFileDto
            {
                schema_version   = 1,
                artifact         = "unity_agent_bridge_response",
                command_id       = commandId,
                ok               = outcome.Success,
                completed_at_utc = DateTime.UtcNow.ToString("o"),
                storage          = outcome.Storage,
                postgres_only    = outcome.PostgresOnly,
                error            = outcome.Success ? string.Empty : outcome.ErrorMessage,
                log_lines        = Array.Empty<AgentBridgeLogLineDto>(),
                play_mode_state  = string.Empty,
                ready            = false,
                already_playing  = false,
                already_stopped  = false,
                has_grid_dimensions = false,
                grid_width       = 0,
                grid_height      = 0,
            };
            resp.artifact_paths = outcome.Success && !string.IsNullOrEmpty(outcome.ArtifactPathRepoRelative)
                ? new[] { outcome.ArtifactPathRepoRelative }
                : Array.Empty<string>();
            return JsonUtility.ToJson(resp, true);
        }

        /// <summary>Build a play-mode bridge response JSON string.</summary>
        public static string BuildPlayModeBridgeResponseJson(
            string commandId,
            bool ok,
            string storage,
            string error,
            string playModeState,
            bool ready,
            bool alreadyPlaying,
            bool alreadyStopped,
            bool hasGridDimensions,
            int gridWidth,
            int gridHeight)
        {
            var resp = new AgentBridgeResponseFileDto
            {
                schema_version   = 1,
                artifact         = "unity_agent_bridge_response",
                command_id       = commandId,
                ok               = ok,
                completed_at_utc = DateTime.UtcNow.ToString("o"),
                storage          = storage,
                postgres_only    = false,
                error            = ok ? string.Empty : (error ?? string.Empty),
                artifact_paths   = Array.Empty<string>(),
                log_lines        = Array.Empty<AgentBridgeLogLineDto>(),
                play_mode_state  = playModeState ?? string.Empty,
                ready            = ready,
                already_playing  = alreadyPlaying,
                already_stopped  = alreadyStopped,
                has_grid_dimensions = hasGridDimensions,
                grid_width       = gridWidth,
                grid_height      = gridHeight,
            };
            return JsonUtility.ToJson(resp, true);
        }
    }
}
