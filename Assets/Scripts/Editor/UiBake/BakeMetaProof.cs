using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Territory.Editor.UiBake
{
    /// <summary>
    /// Layer 2 .meta-file write proof (TECH-28364).
    /// Post-SaveAssets verification: assert .meta file exists and contains the
    /// stable GUID derived from the panel slug. Closes F4-class defect where
    /// Unity silently produced a phantom-GUID prefab (no Import = runtime null).
    /// </summary>
    public static class BakeMetaProof
    {
        /// <summary>
        /// Assert that a .meta file exists at <paramref name="prefabPath"/> + ".meta"
        /// and that the file contains <paramref name="expectedGuid"/>.
        /// Throws <see cref="BakeException"/> with code "meta_missing_or_unstable:{path}" on fail.
        /// </summary>
        public static void AssertMetaExists(string prefabPath, string expectedGuid)
        {
            var metaPath = prefabPath + ".meta";
            if (!File.Exists(metaPath))
            {
                throw new BakeException($"meta_missing_or_unstable:{prefabPath}");
            }

            var content = File.ReadAllText(metaPath);
            if (!content.Contains(expectedGuid))
            {
                throw new BakeException($"meta_missing_or_unstable:{prefabPath}");
            }
        }

        /// <summary>
        /// Compute a stable GUID string derived deterministically from <paramref name="panelSlug"/>.
        /// Uses MD5 of slug bytes (lowercased) formatted as a Unity GUID hex string (32 chars, no dashes).
        /// Deterministic across runs — same slug always produces the same GUID string.
        /// </summary>
        public static string ComputeStableGuid(string panelSlug)
        {
            if (string.IsNullOrEmpty(panelSlug)) throw new ArgumentException("panelSlug must not be empty", nameof(panelSlug));
            var inputBytes = Encoding.UTF8.GetBytes(panelSlug.ToLowerInvariant());
            using var md5 = MD5.Create();
            var hash = md5.ComputeHash(inputBytes);
            return BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
        }

        /// <summary>
        /// Write a minimal Unity-compatible .meta file containing <paramref name="guid"/>
        /// at <paramref name="prefabPath"/> + ".meta". Used by tests and post-SaveAssets hook.
        /// </summary>
        public static void WriteMetaFile(string prefabPath, string guid)
        {
            var metaPath = prefabPath + ".meta";
            var content = $"fileFormatVersion: 2\nguid: {guid}\nPrefabImporter:\n  externalObjects: {{}}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n";
            File.WriteAllText(metaPath, content);
        }
    }
}
