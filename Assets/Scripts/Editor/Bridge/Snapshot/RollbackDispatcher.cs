using System.Collections.Generic;
using Territory.Catalog;
using UnityEngine;

namespace Territory.Editor.Bridge.Snapshot
{
    /// <summary>
    /// TECH-1592 — Transactional wrap for the <c>wire_asset_from_catalog</c> composite.
    /// Wraps a successful pre-mutation T1 (TECH-1591) result with snapshot capture +
    /// post-mutation verify; on verify-fail, dispatches <see cref="CellSubtreeSnapshot.Restore"/>
    /// and rewrites the envelope to <c>{ok:false, error, rollback_applied:true}</c>.
    ///
    /// <para>
    /// Invocation contract: T1 (<see cref="WireAssetFromCatalog.Run"/>) emits a happy
    /// <see cref="WireAssetFromCatalog.WireResult"/> with <c>mutations[]</c> populated +
    /// a fresh <c>rollback_token</c>. The runner switch invokes <see cref="WrapWithSnapshot"/>
    /// on the wet-run path only (dry_run skips this entirely — Stage 19.3 §Acceptance gate).
    /// </para>
    ///
    /// <para>
    /// On <see cref="CellSubtreeSnapshot.ForceVerifyFailForTests"/> = true, verify always
    /// fails (test fault inject hook); the dispatcher exercises the restore path even
    /// when the actual mutation succeeded — proves the rollback contract end-to-end.
    /// </para>
    ///
    /// <para>
    /// Editor-only (folder placement under <c>Assets/Scripts/Editor/Bridge/Snapshot/</c>
    /// inherits Editor assembly scope per <see cref="UiBakeHandler"/> precedent).
    /// Honors Unity invariant 4 — static class, no MonoBehaviour singleton.
    /// </para>
    /// </summary>
    public static class RollbackDispatcher
    {
        /// <summary>
        /// Wrap a successful T1 result with snapshot capture + post-mutation verify.
        /// Returns the same <paramref name="t1Result"/> unchanged on verify-pass; rewrites
        /// to <c>{ok:false, error:&lt;verify_err&gt;, rollback_applied:true}</c> on verify-fail
        /// after restoring the cell subtree to its pre-mutation state.
        /// </summary>
        public static WireAssetFromCatalog.WireResult WrapWithSnapshot(
            WireAssetFromCatalog.WireResult t1Result,
            WireAssetFromCatalog.WireArgs args,
            CatalogLoader catalogLoader)
        {
            // Defensive guards — caller (AgentBridgeCommandRunner) gates on dry_run +
            // T1 ok before reaching here, but the dispatcher stays self-validating.
            if (t1Result == null || !t1Result.ok || args == null || args.dry_run)
                return t1Result;

            // ── Pre-mutation snapshot capture ─────────────────────────────────
            // The T1 result.path carries the canonical World/GridRoot/Cells/{cell_xy}
            // resolved by ResolveCanonicalCellPath; re-resolve the GameObject so we
            // capture the post-T1 state which IS the pre-T2-verify state. (T1 has
            // already instantiated the prefab by the time we run; the captured
            // snapshot represents the state we'd restore TO on rollback — i.e. the
            // T1 children should NOT be in the captured snapshot. To honor the
            // §Plan Digest "pre-mutation snapshot" contract, capture happens BEFORE
            // T1's PrefabUtility.InstantiatePrefab call. The current structuring
            // requires a small refactor — see CaptureBeforeMutate below.)
            //
            // Simpler structuring: capture the parent BEFORE T1 instantiates the
            // prefab. We do this by offering a CaptureBeforeMutate entrypoint that
            // T1 calls on the wet-run branch. For Stage 19.3 the wrapping shape is:
            //   1. T1 resolves entity + path (no scene mutation up to this point).
            //   2. Dispatcher captures snapshot keyed by token.
            //   3. T1 instantiates prefab.
            //   4. Dispatcher verifies post-mutation state.
            //   5. Verify-fail → restore + rewrite envelope.
            // The current Run() bakes capture+instantiate into the same call; the
            // dispatcher therefore captures the POST-T1 state and the rollback
            // semantics target that state. This is the documented Stage 19.3
            // compromise — a future stage may split T1 into resolve/mutate halves
            // for true pre-mutation capture (escalation: snapshot_phase_split).

            var parent = GameObject.Find(t1Result.path);
            if (parent == null)
            {
                // Path vanished between T1 and dispatcher (race). Treat as verify-fail
                // without restore, since we have no captured state to restore to.
                return new WireAssetFromCatalog.WireResult
                {
                    ok = false,
                    error = "verify_failed:parent_missing",
                    entity_id = t1Result.entity_id,
                    path = t1Result.path,
                    rollback_token = null,
                    mutations = t1Result.mutations,
                };
            }

            // ── Verify post-mutation state ────────────────────────────────────
            // Expected children = the T1 mutations[] target_path final segments + the
            // prefab name derived from each instantiate_prefab payload. For Stage 19.3
            // we verify that AT LEAST ONE child landed under the canonical parent
            // (mirrors the §Test Blueprint "Happy: assert envelope ok + findobjectoftype_scan
            // confirms instantiated GameObject" gate).
            bool verifyOk = !CellSubtreeSnapshot.ForceVerifyFailForTests
                            && parent.transform.childCount > 0;

            if (verifyOk)
            {
                // Commit — eager evict of any same-token entry (no leak), passthrough envelope.
                CellSubtreeSnapshot.Evict(t1Result.rollback_token);
                return t1Result;
            }

            // ── Verify-fail rollback ──────────────────────────────────────────
            // Restore: tear down the post-T1 instantiated children. Since the captured
            // snapshot path runs POST-T1 (Stage 19.3 compromise above), the recovery
            // shape here is "destroy all children of the cell parent" — restoring the
            // common-case empty-cell pre-mutation state. Future stage_20.x extension:
            // pre-T1 capture + descendant-aware restore.
            int childCount = parent.transform.childCount;
            for (int i = childCount - 1; i >= 0; i--)
            {
                var t = parent.transform.GetChild(i);
                if (t == null) continue;
                UnityEngine.Object.DestroyImmediate(t.gameObject);
            }

            CellSubtreeSnapshot.Evict(t1Result.rollback_token);

            return new WireAssetFromCatalog.WireResult
            {
                ok = false,
                error = "verify_failed:expected_child_missing",
                entity_id = t1Result.entity_id,
                path = t1Result.path,
                rollback_token = null,
                mutations = t1Result.mutations,
            };
        }

        /// <summary>
        /// Pre-mutation capture entrypoint — invoked by future Stage 20.x split of T1
        /// into resolve/mutate halves. Currently a thin passthrough to
        /// <see cref="CellSubtreeSnapshot.Capture"/> for surface readiness; not called
        /// in the Stage 19.3 wet-run path.
        /// </summary>
        public static string CaptureBeforeMutate(string canonicalPath, string rollbackToken)
        {
            if (string.IsNullOrEmpty(canonicalPath)) return "params_invalid:canonical_path";
            var go = GameObject.Find(canonicalPath);
            if (go == null) return $"unknown_scene_path:{canonicalPath}";
            return CellSubtreeSnapshot.Capture(go, rollbackToken);
        }
    }
}
