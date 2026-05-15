using System.IO;
using UnityEngine;
using Territory.Core;

namespace Territory.Persistence
{
/// <summary>
/// Lightweight boot component — call <see cref="FeatureFlags.HydrateFromJson"/> in
/// <c>Awake</c> so flags are ready before any <c>Start</c> consumer runs.
/// Attach to the scene-root boot GameObject (CityScene / MainScene) via Inspector.
/// Path resolves to <c>{Application.dataPath}/../tools/interchange/feature-flags-snapshot.json</c>
/// so it works from both Editor and built players that ship the interchange folder.
/// </summary>
public class FeatureFlagsBootstrap : MonoBehaviour
{
    [Tooltip("Override interchange snapshot path. Leave blank to use the project-default tools/interchange/ location.")]
    [SerializeField] string snapshotPathOverride;

    void Awake()
    {
        string path = string.IsNullOrWhiteSpace(snapshotPathOverride)
            ? ResolveDefaultPath()
            : snapshotPathOverride;
        FeatureFlags.HydrateFromJson(path);
    }

    static string ResolveDefaultPath()
    {
        // Application.dataPath = <repo>/Assets in Editor; <player>/Contents/Data in build.
        // One level up from Assets lands at repo root; append interchange artifact path.
        string repoRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        return Path.Combine(repoRoot, "tools", "interchange", "feature-flags-snapshot.json");
    }
}
}
