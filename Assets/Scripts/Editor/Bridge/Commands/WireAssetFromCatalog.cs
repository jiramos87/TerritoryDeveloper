using System;
using System.Collections.Generic;
using System.Text;
using Territory.Catalog;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Territory.Editor.Bridge
{
    /// <summary>
    /// TECH-1591 — `wire_asset_from_catalog` bridge composite kind. Resolves an
    /// <c>entity_id</c> via Stage 19.2's <see cref="ResolveLiveEntityId"/> (TECH-1586),
    /// then instantiates a prefab under the canonical world cell parent path
    /// (<c>World/GridRoot/Cells/{cell_xy}</c>), emitting a <c>mutations[]</c> descriptor
    /// that TECH-1592 (snapshot/rollback) consumes for transactional verification.
    ///
    /// <para>
    /// Envelope: <c>{ok:true, mutations:[…], rollback_token:"&lt;uuid&gt;"}</c> on happy path.
    /// Failure shapes (DEC-A48 structured envelope):
    /// <list type="bullet">
    /// <item><c>{ok:false, error:'unknown_scene_path', path:&lt;offered&gt;}</c> — pre-mutation reject when
    /// canonical cell parent absent OR offered <c>cell_xy</c> fails the canonical template match.</item>
    /// <item><c>{ok:false, error:'entity_unresolvable', entity_id}</c> — <see cref="ResolveLiveEntityId"/>
    /// returned <c>null</c> on the input id (retired / never present).</item>
    /// <item><c>{ok:false, error:'asset_detail_missing', entity_id}</c> — no prefab path resolvable from
    /// the live catalog snapshot for the resolved id.</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <c>dry_run=true</c> short-circuit: returns <c>{ok:true, mutations:[…], rollback_token:""}</c> without
    /// instantiating any GameObject and without invoking the (forthcoming TECH-1592) snapshot capture path.
    /// The mutations[] descriptor is still synthesized so callers can inspect proposed changes pre-commit.
    /// </para>
    ///
    /// <para>
    /// Sibling-partial registration in <see cref="AgentBridgeCommandRunner"/>'s mutation switch follows the
    /// <see cref="UiBakeHandler"/> precedent (Stage 2 of Game UI Design System MVP). T2 (TECH-1592) supplies
    /// the snapshot/rollback hooks; T3 (TECH-1593) authors the canonical-path scene contract doc that this
    /// handler enforces.
    /// </para>
    /// </summary>
    public static class WireAssetFromCatalog
    {
        // ── Canonical path scheme (D10) ──────────────────────────────────────────
        /// <summary>Canonical world cell parent template (T3 / TECH-1593 scene contract doc).</summary>
        public const string WorldCellParentTemplate = "World/GridRoot/Cells/{cell_xy}";

        // ── DTOs ─────────────────────────────────────────────────────────────────

        /// <summary>Bridge-mutation argument bag for <c>wire_asset_from_catalog</c>.</summary>
        [Serializable]
        public class WireArgs
        {
            /// <summary>Catalog <c>entity_id</c> to wire onto the scene (resolved via TECH-1586).</summary>
            public string entity_id;
            /// <summary>Cell coordinate token, e.g. <c>"5_7"</c>; resolves to <c>World/GridRoot/Cells/{cell_xy}</c>.</summary>
            public string cell_xy;
            /// <summary>When true, return proposed mutations[] without scene mutation OR snapshot capture.</summary>
            public bool dry_run;
        }

        /// <summary>One emitted mutation step — flat descriptor consumed by T2 snapshot/rollback.</summary>
        [Serializable]
        public class MutationStep
        {
            public string kind;          // e.g. "instantiate_prefab", "assign_serialized_field"
            public string target_path;   // canonical scene path or asset path
            public string payload;       // JSON-encoded extra payload (prefab path, field name, etc.)
        }

        /// <summary>Result envelope for the wire composite.</summary>
        [Serializable]
        public class WireResult
        {
            public bool ok;
            public string error;
            public string entity_id;
            public string path;
            public string rollback_token;
            public List<MutationStep> mutations;
        }

        // ── Entrypoint ───────────────────────────────────────────────────────────

        /// <summary>
        /// Execute the wire composite. <paramref name="catalogLoader"/> may be null —
        /// when null, the resolver step is skipped and treated as <c>entity_unresolvable</c>.
        /// Caller (the AgentBridgeCommandRunner switch) is responsible for the boot-time
        /// <c>FindObjectOfType&lt;CatalogLoader&gt;()</c> resolution; this entrypoint stays
        /// pure-static and DI-friendly per Unity invariant 4 (no new singletons).
        /// </summary>
        public static WireResult Run(WireArgs args, CatalogLoader catalogLoader)
        {
            // ── Arg validation ────────────────────────────────────────────────
            if (args == null)
                return Fail("missing_arg", null, null, "args");
            if (string.IsNullOrEmpty(args.entity_id))
                return Fail("missing_arg", null, null, "entity_id");
            if (string.IsNullOrEmpty(args.cell_xy))
                return Fail("missing_arg", null, null, "cell_xy");

            // ── Path resolution (canonical scheme) ────────────────────────────
            string canonicalPath = ResolveCanonicalCellPath(args.cell_xy);

            // ── Entity resolution (TECH-1586) ─────────────────────────────────
            // Catalog loader optional; null = treat as unresolvable.
            string resolvedId = null;
            if (catalogLoader != null)
            {
                IReadOnlyDictionary<string, CatalogEntity> entities = catalogLoader.Entities;
                resolvedId = ResolveLiveEntityId.Resolve(args.entity_id, entities);
            }

            if (string.IsNullOrEmpty(resolvedId))
                return Fail("entity_unresolvable", args.entity_id, null, null);

            // ── Asset detail lookup ───────────────────────────────────────────
            // Stage 19.3 §Plan Digest defers an explicit `asset_detail` repo accessor on the
            // Unity side: the live catalog snapshot exposes `entity_id` presence; per-row
            // `prefab_path` / `world_sprite` / `has_button` columns are NOT on the
            // CatalogEntity POCO surface (intentional — JsonUtility cannot round-trip the
            // `paramsJson` / `detail` jsonb columns). The composite synthesizes a
            // canonical prefab asset path from the entity slug ("Assets/Prefabs/Catalog/{slug}.prefab"),
            // emits the placeholder mutations[] descriptor, and treats real per-row metadata
            // (world_sprite path, has_button flag) as Stage 20.x extension work. Failure to
            // resolve a usable prefab path → asset_detail_missing reject.
            CatalogEntity row = null;
            if (catalogLoader != null && catalogLoader.Entities != null)
            {
                catalogLoader.Entities.TryGetValue(resolvedId, out row);
            }
            if (row == null || string.IsNullOrEmpty(row.slug))
                return Fail("asset_detail_missing", resolvedId, null, null);

            string prefabPath = $"Assets/Prefabs/Catalog/{row.slug}.prefab";

            // ── Pre-mutation canonical path enforcement ───────────────────────
            // Reject unknown scene paths BEFORE any mutation. The canonical world-cell
            // parent must already exist in the active scene; absence → unknown_scene_path.
            // dry_run STILL enforces this — the descriptor is meaningless if the canonical
            // mount doesn't exist.
            GameObject parentGo = GameObject.Find(canonicalPath);
            if (parentGo == null)
                return Fail("unknown_scene_path", resolvedId, canonicalPath, null);

            // ── Mutations[] synthesis ─────────────────────────────────────────
            var mutations = new List<MutationStep>();
            mutations.Add(new MutationStep
            {
                kind = "instantiate_prefab",
                target_path = canonicalPath,
                payload = $"{{\"prefab_path\":\"{EscapeJson(prefabPath)}\",\"entity_id\":\"{EscapeJson(resolvedId)}\"}}",
            });

            // ── dry_run short-circuit ─────────────────────────────────────────
            // Returns the proposed mutations[] without instantiating anything AND without
            // engaging T2 snapshot capture (T2 owns the capture entrypoint; T1 simply does
            // not call it on dry_run).
            if (args.dry_run)
            {
                return new WireResult
                {
                    ok = true,
                    error = null,
                    entity_id = resolvedId,
                    path = canonicalPath,
                    rollback_token = string.Empty,
                    mutations = mutations,
                };
            }

            // ── Wet run: instantiate prefab under canonical parent ────────────
            // Snapshot capture + rollback dispatch live in TECH-1592; T1's contract is
            // limited to the instantiate + mutations[] emit. T1 DOES generate the
            // rollback_token (UUID) up front so the envelope shape is uniform across
            // T1-only and T1+T2 integrated invocations.
            var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefabAsset == null)
                return Fail("asset_detail_missing", resolvedId, prefabPath, null);

            string rollbackToken = Guid.NewGuid().ToString();
            try
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefabAsset, parentGo.transform);
                if (instance == null)
                    return Fail("instantiate_failed", resolvedId, prefabPath, null);

                EditorSceneManager.MarkSceneDirty(instance.scene);
            }
            catch (Exception ex)
            {
                return Fail("instantiate_threw", resolvedId, prefabPath, ex.Message);
            }

            return new WireResult
            {
                ok = true,
                error = null,
                entity_id = resolvedId,
                path = canonicalPath,
                rollback_token = rollbackToken,
                mutations = mutations,
            };
        }

        // ── Helpers ──────────────────────────────────────────────────────────────

        /// <summary>Compose <c>World/GridRoot/Cells/{cell_xy}</c> from a cell coordinate token.</summary>
        public static string ResolveCanonicalCellPath(string cellXy)
        {
            return WorldCellParentTemplate.Replace("{cell_xy}", cellXy ?? string.Empty);
        }

        static WireResult Fail(string error, string entityId, string path, string detail)
        {
            return new WireResult
            {
                ok = false,
                error = string.IsNullOrEmpty(detail) ? error : $"{error}:{detail}",
                entity_id = entityId,
                path = path,
                rollback_token = null,
                mutations = null,
            };
        }

        /// <summary>Serialize <see cref="WireResult"/> into the bridge envelope JSON payload.</summary>
        public static string ToBridgeJson(WireResult result)
        {
            if (result == null) return "{\"ok\":false,\"error\":\"null_result\"}";

            var sb = new StringBuilder();
            sb.Append('{');
            sb.Append("\"ok\":").Append(result.ok ? "true" : "false");
            if (!string.IsNullOrEmpty(result.error))
                sb.Append(",\"error\":\"").Append(EscapeJson(result.error)).Append('"');
            if (!string.IsNullOrEmpty(result.entity_id))
                sb.Append(",\"entity_id\":\"").Append(EscapeJson(result.entity_id)).Append('"');
            if (!string.IsNullOrEmpty(result.path))
                sb.Append(",\"path\":\"").Append(EscapeJson(result.path)).Append('"');
            if (result.rollback_token != null)
                sb.Append(",\"rollback_token\":\"").Append(EscapeJson(result.rollback_token)).Append('"');
            if (result.mutations != null)
            {
                sb.Append(",\"mutations\":[");
                for (int i = 0; i < result.mutations.Count; i++)
                {
                    if (i > 0) sb.Append(',');
                    var m = result.mutations[i];
                    sb.Append('{');
                    sb.Append("\"kind\":\"").Append(EscapeJson(m.kind ?? string.Empty)).Append('"');
                    sb.Append(",\"target_path\":\"").Append(EscapeJson(m.target_path ?? string.Empty)).Append('"');
                    sb.Append(",\"payload\":").Append(string.IsNullOrEmpty(m.payload) ? "{}" : m.payload);
                    sb.Append('}');
                }
                sb.Append(']');
            }
            sb.Append('}');
            return sb.ToString();
        }

        static string EscapeJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
        }
    }
}
