using System;
using System.IO;
using UnityEngine;

namespace Territory.Integration
{
    /// <summary>
    /// Resolves optional Postgres connection and Node executable for runtime-spawned
    /// <c>tools/postgres-ia</c> scripts (no EditorPrefs; suitable for Play Mode and batch).
    /// </summary>
    public static class RuntimePostgresEnv
    {
        /// <summary>
        /// Returns a non-empty database URL from <c>DATABASE_URL</c> or repo-root <c>.env.local</c>, or null.
        /// </summary>
        public static string TryGetDatabaseUrl(string repoRoot)
        {
            if (string.IsNullOrEmpty(repoRoot))
                return null;

            string env = Environment.GetEnvironmentVariable("DATABASE_URL");
            if (!string.IsNullOrWhiteSpace(env))
                return env.Trim();

            return TryParseDatabaseUrlFromEnvLocal(repoRoot);
        }

        /// <summary>
        /// Resolves <c>node</c> for subprocess spawn (mirrors Editor defaults where possible).
        /// </summary>
        public static string ResolveNodeExecutablePath()
        {
            string envNode = Environment.GetEnvironmentVariable("NODE_BINARY");
            if (!string.IsNullOrWhiteSpace(envNode))
            {
                envNode = envNode.Trim();
                if (File.Exists(envNode))
                    return envNode;
            }

            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (!string.IsNullOrEmpty(home))
            {
                string voltaName = Application.platform == RuntimePlatform.WindowsPlayer ? "node.exe" : "node";
                string volta = Path.Combine(home, ".volta", "bin", voltaName);
                if (File.Exists(volta))
                    return volta;
            }

            if (Application.platform == RuntimePlatform.OSXPlayer || Application.platform == RuntimePlatform.OSXEditor)
            {
                if (File.Exists("/opt/homebrew/bin/node"))
                    return "/opt/homebrew/bin/node";
                if (File.Exists("/usr/local/bin/node"))
                    return "/usr/local/bin/node";
            }

            if (Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor)
            {
                string pf = Environment.GetEnvironmentVariable("ProgramFiles");
                if (!string.IsNullOrEmpty(pf))
                {
                    string winNode = Path.Combine(pf, "nodejs", "node.exe");
                    if (File.Exists(winNode))
                        return winNode;
                }
            }

            return "node";
        }

        static string TryParseDatabaseUrlFromEnvLocal(string repoRoot)
        {
            string p = Path.Combine(repoRoot, ".env.local");
            if (!File.Exists(p))
                return null;
            try
            {
                foreach (string line in File.ReadAllLines(p))
                {
                    string t = line.Trim();
                    if (t.Length == 0 || t.StartsWith("#", StringComparison.Ordinal))
                        continue;
                    const string prefix = "DATABASE_URL=";
                    if (!t.StartsWith(prefix, StringComparison.Ordinal))
                        continue;
                    string v = t.Substring(prefix.Length).Trim();
                    if (v.Length >= 2 &&
                        ((v[0] == '"' && v[^1] == '"') || (v[0] == '\'' && v[^1] == '\'')))
                    {
                        v = v.Substring(1, v.Length - 2);
                    }

                    return string.IsNullOrWhiteSpace(v) ? null : v.Trim();
                }
            }
            catch
            {
                // ignored
            }

            return null;
        }
    }
}
