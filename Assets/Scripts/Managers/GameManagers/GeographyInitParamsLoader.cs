using System;
using System.IO;
using UnityEngine;

namespace Territory.Persistence
{
    /// <summary>
    /// Loads <see cref="GeographyInitParamsDto"/> from disk (StreamingAssets interchange, TECH-41).
    /// </summary>
    public static class GeographyInitParamsLoader
    {
        public const string ExpectedArtifact = "geography_init_params";
        public const int ExpectedSchemaVersion = 1;

        /// <summary>
        /// Reads UTF-8 JSON and validates artifact / schema / required <c>map</c> bounds.
        /// </summary>
        /// <param name="absolutePath">Full path to the JSON file.</param>
        /// <param name="dto">Parsed DTO when return value is true.</param>
        /// <param name="errorMessage">Human-readable failure reason when return value is false.</param>
        public static bool TryLoadFromPath(string absolutePath, out GeographyInitParamsDto dto, out string errorMessage)
        {
            dto = null;
            errorMessage = null;

            if (string.IsNullOrEmpty(absolutePath))
            {
                errorMessage = "Path is empty.";
                return false;
            }

            if (!File.Exists(absolutePath))
            {
                errorMessage = $"File not found: {absolutePath}";
                return false;
            }

            string json;
            try
            {
                json = File.ReadAllText(absolutePath);
            }
            catch (Exception ex)
            {
                errorMessage = $"Read failed: {ex.Message}";
                return false;
            }

            if (string.IsNullOrWhiteSpace(json))
            {
                errorMessage = "File is empty.";
                return false;
            }

            GeographyInitParamsDto parsed;
            try
            {
                parsed = JsonUtility.FromJson<GeographyInitParamsDto>(json);
            }
            catch (Exception ex)
            {
                errorMessage = $"JSON parse failed: {ex.Message}";
                return false;
            }

            if (parsed == null)
            {
                errorMessage = "JSON deserialized to null.";
                return false;
            }

            if (!string.Equals(parsed.artifact, ExpectedArtifact, StringComparison.Ordinal))
            {
                errorMessage = $"Invalid artifact (expected '{ExpectedArtifact}', got '{parsed.artifact}').";
                return false;
            }

            if (parsed.schema_version != ExpectedSchemaVersion)
            {
                errorMessage = $"Unsupported schema_version (expected {ExpectedSchemaVersion}, got {parsed.schema_version}).";
                return false;
            }

            if (parsed.map == null)
            {
                errorMessage = "Missing required 'map' object.";
                return false;
            }

            if (parsed.map.width < 1 || parsed.map.height < 1)
            {
                errorMessage = $"Invalid map dimensions ({parsed.map.width}x{parsed.map.height}).";
                return false;
            }

            dto = parsed;
            return true;
        }
    }
}
