using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Territory.Economy;

namespace Territory.Editor.Economy
{
    /// <summary>
    /// One-shot wire-up for the TreasuryFloorClampService helper on EconomyManager.
    /// Originally created to unblock Stage 1.2 of zone-s-economy-master-plan before
    /// scene-mutation bridge kinds were implemented. The bridge now supports
    /// attach_component / assign_serialized_field / save_scene, so agents can wire
    /// this directly; this Editor hook remains as a belt-and-suspenders safety net
    /// and for human-driven scene resets.
    ///
    /// Behaviour:
    ///   - On domain reload (InitializeOnLoadMethod), checks the active scene for an
    ///     EconomyManager. If found and TreasuryFloorClampService is not attached to the
    ///     same GameObject, attaches it and populates EconomyManager.treasuryFloorClamp.
    ///   - Subscribes to EditorSceneManager.sceneOpened so any scene opened later gets
    ///     the same treatment.
    ///   - Exposes a menu item (Bacayo → Zone-S Economy → Auto-Wire Treasury Clamp)
    ///     for manual re-runs.
    ///
    /// Safety:
    ///   - Idempotent: skips when the service is already attached and the field already
    ///     points at the same component.
    ///   - Uses SerializedObject writes so Inspector persistence follows Unity's standard
    ///     serialization flow; scene is marked dirty and saved.
    /// </summary>
    [InitializeOnLoad]
    public static class ZoneSEconomyAutoWire
    {
        private const string MenuPath = "Bacayo/Zone-S Economy/Auto-Wire Treasury Clamp";
        private const string TreasuryFieldName = "treasuryFloorClamp";

        static ZoneSEconomyAutoWire()
        {
            EditorApplication.delayCall += WireActiveScene;
            EditorSceneManager.sceneOpened += OnSceneOpened;
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            if (mode != OpenSceneMode.Single) return;
            WireScene(scene, saveIfChanged: true);
        }

        private static void WireActiveScene()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded) return;
            WireScene(scene, saveIfChanged: true);
        }

        [MenuItem(MenuPath)]
        public static void WireActiveSceneMenu()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                Debug.LogWarning("ZoneSEconomyAutoWire: no active scene loaded.");
                return;
            }

            bool changed = WireScene(scene, saveIfChanged: true);
            Debug.Log(changed
                ? $"ZoneSEconomyAutoWire: wiring applied + scene saved ({scene.path})."
                : $"ZoneSEconomyAutoWire: no change needed ({scene.path}).");
        }

        /// <summary>
        /// Find the EconomyManager in the given scene, attach TreasuryFloorClampService
        /// if missing, and populate the serialized field when empty.
        /// </summary>
        /// <returns>true if any change was applied to the scene; false otherwise.</returns>
        private static bool WireScene(Scene scene, bool saveIfChanged)
        {
            var economy = FindEconomyManagerInScene(scene);
            if (economy == null) return false;

            bool changed = false;

            var clamp = economy.GetComponent<TreasuryFloorClampService>();
            if (clamp == null)
            {
                clamp = Undo.AddComponent<TreasuryFloorClampService>(economy.gameObject);
                changed = true;
                Debug.Log($"ZoneSEconomyAutoWire: attached TreasuryFloorClampService to '{economy.gameObject.name}' in '{scene.path}'.");
            }

            var so = new SerializedObject(economy);
            var prop = so.FindProperty(TreasuryFieldName);
            if (prop != null && prop.objectReferenceValue != clamp)
            {
                prop.objectReferenceValue = clamp;
                so.ApplyModifiedPropertiesWithoutUndo();
                changed = true;
                Debug.Log($"ZoneSEconomyAutoWire: assigned EconomyManager.{TreasuryFieldName} on '{economy.gameObject.name}'.");
            }

            if (changed)
            {
                EditorUtility.SetDirty(economy);
                EditorSceneManager.MarkSceneDirty(scene);
                if (saveIfChanged && !EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    EditorSceneManager.SaveScene(scene);
                }
            }

            return changed;
        }

        private static EconomyManager FindEconomyManagerInScene(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var em = root.GetComponentInChildren<EconomyManager>(includeInactive: true);
                if (em != null) return em;
            }
            return null;
        }
    }
}
