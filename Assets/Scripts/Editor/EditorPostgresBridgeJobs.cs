using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// Runs <c>tools/postgres-ia/agent-bridge-dequeue.mjs</c> and <c>agent-bridge-complete.mjs</c> with the same
/// <see cref="EditorPostgresExportRegistrar.ResolveNodeExecutablePath"/> / <c>DATABASE_URL</c> policy as the export registrar.
/// </summary>
public static class EditorPostgresBridgeJobs
{
    const int ScriptTimeoutMs = 120_000;

    /// <summary>Stdout JSON shape from <c>agent-bridge-dequeue.mjs</c> (Unity <see cref="JsonUtility"/>).</summary>
    [Serializable]
    public class DequeueStdoutDto
    {
        public bool ok;
        public bool empty;
        public string command_id;
        public string kind;
        public string request_json;
    }

    /// <summary>
    /// Claims one pending bridge job or returns <paramref name="dto"/> with <c>empty == true</c>. Returns <c>false</c> if the script failed or stdout was not valid JSON.
    /// </summary>
    public static bool TryDequeue(string repoRoot, out DequeueStdoutDto dto, out string logDetail)
    {
        dto = null;
        logDetail = "";
        string script = Path.Combine(repoRoot, "tools", "postgres-ia", "agent-bridge-dequeue.mjs");
        if (!File.Exists(script))
        {
            logDetail = $"Missing script: {script}";
            return false;
        }

        if (!TryRunNodeScript(repoRoot, $"\"{script}\"", out string stdout, out string stderr, out int exitCode))
        {
            logDetail = $"dequeue spawn failed. stderr: {stderr}";
            return false;
        }

        logDetail = $"exit={exitCode} stderr={stderr} stdout={stdout}";
        if (exitCode != 0)
            return false;

        string trimmed = (stdout ?? "").Trim();
        if (string.IsNullOrEmpty(trimmed))
            return false;

        try
        {
            dto = JsonUtility.FromJson<DequeueStdoutDto>(trimmed);
        }
        catch
        {
            return false;
        }

        return dto != null && dto.ok;
    }

    /// <summary>Writes <paramref name="responseJsonUtf8"/> to a temp file and runs <c>agent-bridge-complete.mjs --response-file</c>.</summary>
    public static bool TryCompleteSuccess(string repoRoot, string commandId, string responseJsonUtf8, out string logDetail)
    {
        logDetail = "";
        string tmp = Path.Combine(Path.GetTempPath(), $"agent-bridge-resp-{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(tmp, responseJsonUtf8, new UTF8Encoding(false));
            string p = tmp.Replace('\\', '/');
            string script = Path.Combine(repoRoot, "tools", "postgres-ia", "agent-bridge-complete.mjs");
            string args = $"\"{script}\" --command-id {commandId} --response-file \"{p}\"";
            if (!TryRunNodeScript(repoRoot, args, out string stdout, out string stderr, out int exitCode))
            {
                logDetail = $"complete spawn failed. stderr: {stderr}";
                return false;
            }

            logDetail = $"exit={exitCode} stderr={stderr} stdout={stdout}";
            return exitCode == 0;
        }
        finally
        {
            try
            {
                File.Delete(tmp);
            }
            catch
            {
                // ignored
            }
        }
    }

    /// <summary>Marks the job <c>failed</c> with an English error (quotes/newlines sanitized for process arguments).</summary>
    public static bool TryCompleteFailed(string repoRoot, string commandId, string englishError, out string logDetail)
    {
        string safe = SanitizeErrorForArgv(englishError ?? "Unknown error.");
        string script = Path.Combine(repoRoot, "tools", "postgres-ia", "agent-bridge-complete.mjs");
        string args = $"\"{script}\" --command-id {commandId} --failed --error \"{safe}\"";
        if (!TryRunNodeScript(repoRoot, args, out string stdout, out string stderr, out int exitCode))
        {
            logDetail = $"complete-failed spawn failed. stderr: {stderr}";
            return false;
        }

        logDetail = $"exit={exitCode} stderr={stderr} stdout={stdout}";
        return exitCode == 0;
    }

    static string SanitizeErrorForArgv(string s)
    {
        if (string.IsNullOrEmpty(s))
            return "Unknown error.";
        s = s.Replace('\r', ' ').Replace('\n', ' ').Replace('"', '\'');
        if (s.Length > 800)
            s = s.Substring(0, 800) + "…";
        return s;
    }

    static bool TryRunNodeScript(string repoRoot, string argumentsAfterNode, out string stdout, out string stderr, out int exitCode)
    {
        stdout = "";
        stderr = "";
        exitCode = -1;
        string dbUrl = EditorPostgresExportRegistrar.ResolveEffectiveDatabaseUrl(repoRoot);
        if (string.IsNullOrWhiteSpace(dbUrl))
            return false;

        string nodeExe = EditorPostgresExportRegistrar.ResolveNodeExecutablePath();
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = nodeExe,
                Arguments = argumentsAfterNode,
                WorkingDirectory = repoRoot,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            psi.EnvironmentVariables["DATABASE_URL"] = dbUrl;

            using var proc = Process.Start(psi);
            if (proc == null)
                return false;

            stdout = proc.StandardOutput.ReadToEnd();
            stderr = proc.StandardError.ReadToEnd();
            if (!proc.WaitForExit(ScriptTimeoutMs))
            {
                try
                {
                    proc.Kill();
                }
                catch
                {
                    // ignored
                }

                stderr = (stderr ?? "") + " [timeout]";
                return false;
            }

            exitCode = proc.ExitCode;
            return true;
        }
        catch (Exception ex)
        {
            stderr = ex.Message;
            return false;
        }
    }
}
