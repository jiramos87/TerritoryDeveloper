using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Territory.Editor.Bridge.Snapshot
{
    /// <summary>
    /// TECH-1592 — Pre-mutation cell subtree snapshot manager. Serializes the GameObject
    /// hierarchy under <c>World/GridRoot/Cells/{cell_xy}</c> (parent + descendants) into
    /// an in-memory record keyed by <c>rollback_token</c> (UUID). Restore tears down the
    /// post-mutation subtree and re-instantiates the captured snapshot to recover the
    /// pre-mutation state.
    ///
    /// <para>
    /// Scope contract: cell subtree only. Whole-scene capture explicitly rejected by
    /// Stage 19.3 §Implementer Latitude (verify_scope_drift escalation). Snapshot
    /// captures children GameObject names + transforms (sufficient for the verify-fail
    /// rollback contract); component-level state restoration is deferred to Stage 20.x
    /// since the cell subtree under a fresh wire is empty pre-mutation in the common
    /// case (Stage 19.3 acceptance: empty cell → wire prefab → fail → restore empty).
    /// </para>
    ///
    /// <para>
    /// Storage: static internal <c>Dictionary&lt;string, CellSnapshot&gt;</c> keyed by
    /// rollback_token. Honors Unity invariant 4 — NOT a MonoBehaviour singleton; static
    /// class with private dict accessed through <see cref="Capture"/> / <see cref="Restore"/>
    /// / <see cref="Evict"/>. Eviction policy = eager: <see cref="Evict"/> drops the entry
    /// on commit OR rollback completion; stale-token reuse → <see cref="TokenUnknown"/>.
    /// </para>
    ///
    /// <para>
    /// Editor-only — folder placement under <c>Assets/Scripts/Editor/Bridge/Snapshot/</c>
    /// inherits the Editor assembly scope per <see cref="UiBakeHandler"/> precedent;
    /// no new <c>.asmdef</c> needed.
    /// </para>
    /// </summary>
    public static class CellSubtreeSnapshot
    {
        /// <summary>Sentinel error returned by <see cref="Restore"/> when the token is unknown / evicted.</summary>
        public const string TokenUnknown = "token_unknown";

        /// <summary>Test-only fault inject hook — when <c>true</c>, <see cref="Verify"/> always returns false.</summary>
        internal static bool ForceVerifyFailForTests;

        // ── Snapshot record ──────────────────────────────────────────────────────

        /// <summary>One captured entry — root path + child descriptors at capture time.</summary>
        public class CellSnapshot
        {
            public string parentPath;
            public List<ChildRecord> children;
            public DateTime capturedAt;
        }

        /// <summary>One immediate-child descriptor; recursive children are summarized as count for verify parity.</summary>
        public class ChildRecord
        {
            public string name;
            public Vector3 localPosition;
            public Quaternion localRotation;
            public Vector3 localScale;
            public int descendantCount;
        }

        // ── Storage ──────────────────────────────────────────────────────────────

        static readonly Dictionary<string, CellSnapshot> _store =
            new Dictionary<string, CellSnapshot>(StringComparer.Ordinal);

        // ── Capture ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Capture the cell subtree at <paramref name="cellRoot"/> under <paramref name="rollbackToken"/>.
        /// Returns null on success; populated error string on missing root or token collision.
        /// </summary>
        public static string Capture(GameObject cellRoot, string rollbackToken)
        {
            if (cellRoot == null)
                return "params_invalid:cell_root (null)";
            if (string.IsNullOrEmpty(rollbackToken))
                return "params_invalid:rollback_token (empty)";
            if (_store.ContainsKey(rollbackToken))
                return $"token_collision:{rollbackToken}";

            var snap = new CellSnapshot
            {
                parentPath = GetGameObjectPath(cellRoot),
                children = new List<ChildRecord>(),
                capturedAt = DateTime.UtcNow,
            };

            for (int i = 0; i < cellRoot.transform.childCount; i++)
            {
                var t = cellRoot.transform.GetChild(i);
                if (t == null) continue;
                snap.children.Add(new ChildRecord
                {
                    name = t.gameObject.name,
                    localPosition = t.localPosition,
                    localRotation = t.localRotation,
                    localScale = t.localScale,
                    descendantCount = CountDescendants(t),
                });
            }

            _store[rollbackToken] = snap;
            return null;
        }

        // ── Verify ───────────────────────────────────────────────────────────────

        /// <summary>
        /// Post-mutation verification — returns true when expected mutations[] target paths
        /// resolve to live GameObjects under the canonical cell parent. Mirrors the
        /// <c>findobjectoftype_scan</c> semantics described in unity-invariants §Bridge tooling
        /// for the narrow per-cell scope.
        /// </summary>
        public static bool Verify(string parentPath, IReadOnlyList<string> expectedChildNames)
        {
            if (ForceVerifyFailForTests)
                return false;
            if (string.IsNullOrEmpty(parentPath)) return false;

            var parent = GameObject.Find(parentPath);
            if (parent == null) return false;

            if (expectedChildNames == null || expectedChildNames.Count == 0)
                return true;

            for (int i = 0; i < expectedChildNames.Count; i++)
            {
                string expected = expectedChildNames[i];
                if (string.IsNullOrEmpty(expected)) continue;
                var t = parent.transform.Find(expected);
                if (t == null) return false;
            }
            return true;
        }

        // ── Restore ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Restore the cell subtree to its captured state. Tears down the current children
        /// of the cell parent + the current children that did not exist at capture time;
        /// re-instantiates by name for any captured child that no longer exists. Returns
        /// null on success; populated error string on miss.
        /// </summary>
        public static string Restore(string rollbackToken)
        {
            if (string.IsNullOrEmpty(rollbackToken))
                return "params_invalid:rollback_token (empty)";
            if (!_store.TryGetValue(rollbackToken, out var snap))
                return TokenUnknown;

            var parent = GameObject.Find(snap.parentPath);
            if (parent == null)
            {
                // Parent vanished post-capture — cannot restore subtree shape; evict + report.
                _store.Remove(rollbackToken);
                return $"parent_missing:{snap.parentPath}";
            }

            // Build a set of captured-child names for fast lookup.
            var capturedNames = new HashSet<string>(StringComparer.Ordinal);
            if (snap.children != null)
            {
                for (int i = 0; i < snap.children.Count; i++)
                {
                    var rec = snap.children[i];
                    if (rec != null && !string.IsNullOrEmpty(rec.name))
                        capturedNames.Add(rec.name);
                }
            }

            // Tear down any current children whose names did not exist at capture time
            // (i.e. the post-mutation additions). We iterate from end → start to keep
            // sibling indices stable across DestroyImmediate calls.
            int childCount = parent.transform.childCount;
            for (int i = childCount - 1; i >= 0; i--)
            {
                var t = parent.transform.GetChild(i);
                if (t == null) continue;
                if (!capturedNames.Contains(t.gameObject.name))
                {
                    UnityEngine.Object.DestroyImmediate(t.gameObject);
                }
            }

            // For captured children that no longer exist, re-create empty placeholders
            // (sufficient for Stage 19.3 acceptance — the common path is empty cell pre-mutation,
            // so restore-to-empty is the predominant case). Stage 20.x can extend this to a
            // full prefab-aware re-instantiate when richer per-cell state lands.
            if (snap.children != null)
            {
                for (int i = 0; i < snap.children.Count; i++)
                {
                    var rec = snap.children[i];
                    if (rec == null || string.IsNullOrEmpty(rec.name)) continue;
                    if (parent.transform.Find(rec.name) != null) continue;

                    var go = new GameObject(rec.name);
                    go.transform.SetParent(parent.transform, false);
                    go.transform.localPosition = rec.localPosition;
                    go.transform.localRotation = rec.localRotation;
                    go.transform.localScale = rec.localScale;
                }
            }

            EditorSceneManager.MarkSceneDirty(parent.scene);
            _store.Remove(rollbackToken);
            return null;
        }

        // ── Eviction ─────────────────────────────────────────────────────────────

        /// <summary>Eager eviction on commit — drops the dict entry; safe to call on unknown token.</summary>
        public static void Evict(string rollbackToken)
        {
            if (string.IsNullOrEmpty(rollbackToken)) return;
            _store.Remove(rollbackToken);
        }

        /// <summary>Test-only — current dictionary size; consumed by EditMode tests for leak detection.</summary>
        internal static int StoreSizeForTests => _store.Count;

        /// <summary>Test-only — full reset; consumed between tests to isolate fixtures.</summary>
        internal static void ResetForTests()
        {
            _store.Clear();
            ForceVerifyFailForTests = false;
        }

        // ── Helpers ──────────────────────────────────────────────────────────────

        static int CountDescendants(Transform t)
        {
            if (t == null) return 0;
            int count = 0;
            for (int i = 0; i < t.childCount; i++)
            {
                count += 1 + CountDescendants(t.GetChild(i));
            }
            return count;
        }

        static string GetGameObjectPath(GameObject go)
        {
            if (go == null) return string.Empty;
            var t = go.transform;
            var path = t.name;
            while (t.parent != null)
            {
                t = t.parent;
                path = t.name + "/" + path;
            }
            return path;
        }
    }
}
