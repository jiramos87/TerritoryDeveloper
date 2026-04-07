using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// TECH-55b: persists <b>Territory Developer → Reports</b> exports to <b>Postgres</b> (full <b>JSONB</b> document) via <c>register-editor-export.mjs</c>.
/// **Postgres-only:** no <c>tools/reports/</c> fallback. Success path is quiet unless verbose prefs are on.
/// </summary>
public static class EditorPostgresExportRegistrar
{
    /// <summary>EditorPrefs key for optional <c>BACKLOG.md</c> issue id (metadata for SQL filters).</summary>
    public const string BacklogIssueIdPrefsKey = "TerritoryDeveloper.EditorExportRegistry.BacklogIssueId";

    /// <summary>Optional Postgres connection URI (local only; never commit). Overrides process env when non-empty.</summary>
    public const string DatabaseUrlPrefsKey = "TerritoryDeveloper.EditorExportRegistry.DatabaseUrl";

    /// <summary>When true, log a short message after a successful registry insert.</summary>
    public const string VerboseLoggingPrefsKey = "TerritoryDeveloper.EditorExportRegistry.VerboseLogging";

    /// <summary>Optional absolute path to the <c>node</c> binary. Unity GUI often lacks shell <c>PATH</c> (Volta, nvm, fnm).</summary>
    public const string NodeExecutablePrefsKey = "TerritoryDeveloper.EditorExportRegistry.NodeExecutablePath";

    /// <summary><c>--kind agent_context</c> — <c>Export Agent Context</c> JSON.</summary>
    public const string KindAgentContext = "agent_context";

    /// <summary><c>--kind sorting_debug</c> — <c>Export Sorting Debug</c> Markdown.</summary>
    public const string KindSortingDebug = "sorting_debug";

    /// <summary><c>--kind terrain_cell_chunk</c> — cell chunk interchange JSON.</summary>
    public const string KindTerrainCellChunk = "terrain_cell_chunk";

    /// <summary><c>--kind world_snapshot_dev</c> — dev world snapshot interchange JSON.</summary>
    public const string KindWorldSnapshotDev = "world_snapshot_dev";

    /// <summary><c>--kind ui_inventory</c> — <c>Export UI Inventory (JSON)</c> multi-scene <b>uGUI</b> snapshot (<b>UI design system</b> baseline).</summary>
    public const string KindUiInventory = "ui_inventory";

    const string MenuRoot = "Territory Developer/Reports/";

    [MenuItem(MenuRoot + "Postgres registry — settings…", priority = 95)]
    static void OpenSettings()
    {
        EditorPostgresExportRegistrySettingsWindow.ShowWindow();
    }

    /// <summary>
    /// Resolves <c>DATABASE_URL</c>: <see cref="EditorPrefs"/> (non-empty), then process environment, then repo <c>.env.local</c>.
    /// </summary>
    public static string ResolveEffectiveDatabaseUrl(string repoRoot)
    {
        string fromPrefs = EditorPrefs.GetString(DatabaseUrlPrefsKey, "").Trim();
        if (!string.IsNullOrEmpty(fromPrefs))
            return fromPrefs;

        string env = Environment.GetEnvironmentVariable("DATABASE_URL");
        if (!string.IsNullOrWhiteSpace(env))
            return env.Trim();

        return TryParseDatabaseUrlFromEnvLocal(repoRoot);
    }

    /// <summary>
    /// Resolves the <c>node</c> executable for subprocess spawn. GUI-launched Unity often has no Volta/nvm <c>PATH</c>.
    /// </summary>
    public static string ResolveNodeExecutablePath()
    {
        string fromPrefs = EditorPrefs.GetString(NodeExecutablePrefsKey, "").Trim();
        if (!string.IsNullOrEmpty(fromPrefs))
        {
            if (File.Exists(fromPrefs))
                return fromPrefs;
            Debug.LogWarning(
                $"[EditorPostgresExportRegistrar] Node executable not found at EditorPrefs path: {fromPrefs}. Trying defaults.");
        }

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
            string voltaName = Application.platform == RuntimePlatform.WindowsEditor ? "node.exe" : "node";
            string volta = Path.Combine(home, ".volta", "bin", voltaName);
            if (File.Exists(volta))
                return volta;
        }

        if (Application.platform == RuntimePlatform.OSXEditor)
        {
            if (File.Exists("/opt/homebrew/bin/node"))
                return "/opt/homebrew/bin/node";
            if (File.Exists("/usr/local/bin/node"))
                return "/usr/local/bin/node";
        }

        if (Application.platform == RuntimePlatform.WindowsEditor)
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

    /// <summary>
    /// Persists a report: writes a temp staging file, runs <c>register-editor-export.mjs</c>, then deletes staging.
    /// **Postgres-only:** no <c>tools/reports/</c> fallback — configure <c>DATABASE_URL</c> (env, EditorPrefs, or <c>.env.local</c>).
    /// </summary>
    /// <param name="kind">One of the <c>Kind*</c> constants.</param>
    /// <param name="utf8Body">File body (JSON text or Markdown).</param>
    /// <param name="isMarkdown">True for sorting debug export.</param>
    /// <param name="fileBaseNameWithoutExtension">Unused for persistence (call-site compatibility).</param>
    /// <param name="filesystemPathWritten">Always <c>null</c> (no workspace export files).</param>
    /// <returns>True if a Postgres row was inserted successfully.</returns>
    public static bool TryPersistReport(
        string kind,
        string utf8Body,
        bool isMarkdown,
        string fileBaseNameWithoutExtension,
        out string filesystemPathWritten)
    {
        filesystemPathWritten = null;
        _ = fileBaseNameWithoutExtension;
        if (string.IsNullOrEmpty(utf8Body))
        {
            Debug.LogError("[EditorPostgresExportRegistrar] Empty export body.");
            return false;
        }

        string repoRoot = GetRepoRoot();
        string dbUrl = ResolveEffectiveDatabaseUrl(repoRoot);
        string ext = isMarkdown ? ".md" : ".json";

        if (string.IsNullOrWhiteSpace(dbUrl))
        {
            Debug.LogError(
                "[EditorPostgresExportRegistrar] Reports export requires Postgres: set DATABASE_URL (process env, EditorPrefs TerritoryDeveloper.EditorExportRegistry.DatabaseUrl, or .env.local). See docs/postgres-ia-dev-setup.md.");
            return false;
        }

        string stagingDir = Path.Combine(Path.GetTempPath(), "TerritoryDeveloperEditorExportStaging");
        string stagingName = $"body-{Guid.NewGuid():N}{ext}";
        string stagingAbs = Path.Combine(stagingDir, stagingName);

        try
        {
            Directory.CreateDirectory(stagingDir);
            File.WriteAllText(stagingAbs, utf8Body, new UTF8Encoding(false));
        }
        catch (Exception ex)
        {
            Debug.LogError($"[EditorPostgresExportRegistrar] Staging failed: {ex.Message}");
            return false;
        }

        string scriptPath = Path.Combine(repoRoot, "tools", "postgres-ia", "register-editor-export.mjs");
        if (!File.Exists(scriptPath))
        {
            Debug.LogError($"[EditorPostgresExportRegistrar] Missing script: {scriptPath}");
            try
            {
                File.Delete(stagingAbs);
            }
            catch
            {
                // ignored
            }

            return false;
        }

        string issue = EditorPrefs.GetString(BacklogIssueIdPrefsKey, "").Trim();
        string documentFileArg = stagingAbs.Replace('\\', '/');
        string arguments = $"\"{scriptPath}\" --kind {kind} --document-file \"{documentFileArg}\"";
        if (!string.IsNullOrEmpty(issue))
            arguments += $" --issue \"{issue}\"";

        string nodeExe = ResolveNodeExecutablePath();
        bool dbOk = false;
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = nodeExe,
                Arguments = arguments,
                WorkingDirectory = repoRoot,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            psi.EnvironmentVariables["DATABASE_URL"] = dbUrl;

            using var proc = System.Diagnostics.Process.Start(psi);
            if (proc == null)
            {
                Debug.LogError("[EditorPostgresExportRegistrar] Failed to start node process.");
            }
            else
            {
                string stdout = proc.StandardOutput.ReadToEnd();
                string stderr = proc.StandardError.ReadToEnd();
                if (!proc.WaitForExit(120000))
                {
                    Debug.LogError("[EditorPostgresExportRegistrar] register-editor-export timed out.");
                    try
                    {
                        proc.Kill();
                    }
                    catch
                    {
                        // ignored
                    }
                }
                else if (proc.ExitCode != 0)
                {
                    Debug.LogError(
                        $"[EditorPostgresExportRegistrar] register-editor-export failed (exit {proc.ExitCode}): {stderr}\n{stdout}");
                }
                else
                {
                    dbOk = true;
                    if (EditorPrefs.GetBool(VerboseLoggingPrefsKey, false) && !string.IsNullOrEmpty(stdout))
                        Debug.Log($"[EditorPostgresExportRegistrar] {stdout.Trim()}");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[EditorPostgresExportRegistrar] Registry invocation failed: {ex.Message}");
        }

        try
        {
            File.Delete(stagingAbs);
        }
        catch
        {
            // ignored
        }

        if (!dbOk)
        {
            Debug.LogError(
                "[EditorPostgresExportRegistrar] Postgres insert failed; no filesystem fallback. Fix DATABASE_URL / migration / register-editor-export.mjs output (see Console above).");
        }

        return dbOk;
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
                if (v.Length >= 2 && ((v[0] == '"' && v[^1] == '"') || (v[0] == '\'' && v[^1] == '\'')))
                    v = v.Substring(1, v.Length - 2);
                if (!string.IsNullOrWhiteSpace(v))
                    return v.Trim();
            }
        }
        catch
        {
            // ignored
        }

        return null;
    }

    /// <summary>
    /// Repository root (parent of <c>Assets</c>). Exposed for IDE agent bridge path resolution.
    /// </summary>
    public static string GetRepositoryRoot() => GetRepoRoot();

    /// <summary>
    /// Path relative to <see cref="GetRepositoryRoot"/> using forward slashes (workspace-style).
    /// </summary>
    public static string ToRepositoryRelativePath(string absolutePath) =>
        ToRepoRelativePath(GetRepoRoot(), absolutePath);

    static string GetRepoRoot()
    {
        return Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
    }

    static string ToRepoRelativePath(string repoRoot, string absolutePath)
    {
        string fullRoot = Path.GetFullPath(repoRoot)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        string fullPath = Path.GetFullPath(absolutePath);
        if (!fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
        {
            return fullPath.Replace(Path.DirectorySeparatorChar, '/')
                .Replace(Path.AltDirectorySeparatorChar, '/');
        }

        string rel = fullPath.Substring(fullRoot.Length)
            .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return rel.Replace(Path.DirectorySeparatorChar, '/').Replace(Path.AltDirectorySeparatorChar, '/');
    }
}

/// <summary>
/// Postgres export registry: optional <c>BACKLOG.md</c> issue id, optional <c>DATABASE_URL</c>, optional <c>node</c> path, verbose logging.
/// </summary>
sealed class EditorPostgresExportRegistrySettingsWindow : EditorWindow
{
    string _issueId = "";
    string _databaseUrl = "";
    string _nodeExecutable = "";
    bool _verbose;

    public static void ShowWindow()
    {
        var w = GetWindow<EditorPostgresExportRegistrySettingsWindow>(true, "Postgres export registry", true);
        w.minSize = new Vector2(480, 300);
    }

    void OnEnable()
    {
        _issueId = EditorPrefs.GetString(EditorPostgresExportRegistrar.BacklogIssueIdPrefsKey, "");
        _databaseUrl = EditorPrefs.GetString(EditorPostgresExportRegistrar.DatabaseUrlPrefsKey, "");
        _nodeExecutable = EditorPrefs.GetString(EditorPostgresExportRegistrar.NodeExecutablePrefsKey, "");
        _verbose = EditorPrefs.GetBool(EditorPostgresExportRegistrar.VerboseLoggingPrefsKey, false);
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField(
            "Optional BACKLOG issue id (e.g. BUG-37, FEAT-12). Stored in EditorPrefs and attached to Postgres rows as metadata.",
            EditorStyles.wordWrappedLabel);
        _issueId = EditorGUILayout.TextField("Issue id", _issueId);
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField(
            "Optional DATABASE_URL (local machine only). If set, overrides process environment for Unity-spawned registry scripts. Prefer repo-root .env.local (gitignored) for teams; see docs/postgres-ia-dev-setup.md.",
            EditorStyles.wordWrappedLabel);
        _databaseUrl = EditorGUILayout.TextField("DATABASE_URL", _databaseUrl);
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField(
            "Optional path to the node binary. Leave empty to auto-detect Volta (~/.volta/bin/node), Homebrew, or Windows Program Files. Unity launched from the Dock often has no shell PATH — if registry fails with \"Cannot find the specified file\", set this to the output of `which node` in Terminal.",
            EditorStyles.wordWrappedLabel);
        _nodeExecutable = EditorGUILayout.TextField("Node executable", _nodeExecutable);
        _verbose = EditorGUILayout.ToggleLeft("Verbose logging after successful Postgres insert", _verbose);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Save"))
        {
            EditorPrefs.SetString(EditorPostgresExportRegistrar.BacklogIssueIdPrefsKey, _issueId.Trim());
            EditorPrefs.SetString(EditorPostgresExportRegistrar.DatabaseUrlPrefsKey, _databaseUrl.Trim());
            EditorPrefs.SetString(EditorPostgresExportRegistrar.NodeExecutablePrefsKey, _nodeExecutable.Trim());
            EditorPrefs.SetBool(EditorPostgresExportRegistrar.VerboseLoggingPrefsKey, _verbose);
            if (_verbose)
                Debug.Log("[EditorPostgresExportRegistrar] Saved registry settings.");
            Close();
        }

        if (GUILayout.Button("Clear issue id"))
        {
            EditorPrefs.DeleteKey(EditorPostgresExportRegistrar.BacklogIssueIdPrefsKey);
            _issueId = "";
        }

        if (GUILayout.Button("Clear DATABASE_URL"))
        {
            EditorPrefs.DeleteKey(EditorPostgresExportRegistrar.DatabaseUrlPrefsKey);
            _databaseUrl = "";
        }

        if (GUILayout.Button("Clear node path"))
        {
            EditorPrefs.DeleteKey(EditorPostgresExportRegistrar.NodeExecutablePrefsKey);
            _nodeExecutable = "";
        }

        EditorGUILayout.EndHorizontal();
    }
}
