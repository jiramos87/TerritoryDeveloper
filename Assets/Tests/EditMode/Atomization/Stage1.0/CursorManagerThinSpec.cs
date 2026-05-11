using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Territory.Tests.EditMode.Atomization.Stage1_0
{
    /// <summary>
    /// §Red-Stage Proof anchor: CursorManagerThinSpec.cs::cursor_manager_is_thin_via_registry
    /// Tracer fixture for Stage 1.0 of large-file-atomization-hub-thinning-sweep.
    /// Verifies CursorManager hub-thin pattern: ≤200 LOC, path/class unchanged,
    /// all publics single-line delegate, ICursor resolves via ServiceRegistry post-Awake.
    /// </summary>
    public class CursorManagerThinSpec
    {
        private const string CursorManagerPath = "Assets/Scripts/Managers/GameManagers/CursorManager.cs";
        private const int MaxLines = 200;

        // ── (1) Line-count gate ────────────────────────────────────────────────────

        [Test]
        public void cursor_manager_is_thin_via_registry()
        {
            string absPath = Path.Combine(
                System.Environment.CurrentDirectory, CursorManagerPath.Replace('/', Path.DirectorySeparatorChar));
            Assert.IsTrue(File.Exists(absPath), $"CursorManager.cs not found at {absPath}");
            int lineCount = File.ReadAllLines(absPath).Length;
            Assert.LessOrEqual(lineCount, MaxLines,
                $"CursorManager.cs must be THIN (≤{MaxLines} LOC). Current: {lineCount} lines.");
        }

        // ── (2) Hub path unchanged ─────────────────────────────────────────────────

        [Test]
        public void hub_path_is_unchanged()
        {
            string absPath = Path.Combine(
                System.Environment.CurrentDirectory, CursorManagerPath.Replace('/', Path.DirectorySeparatorChar));
            Assert.IsTrue(File.Exists(absPath),
                $"CursorManager.cs must remain at canonical path: {CursorManagerPath}");
        }

        // ── (3) Hub class name unchanged ───────────────────────────────────────────

        [Test]
        public void hub_class_name_is_unchanged()
        {
            Type t = FindCursorManagerType();
            Assert.IsNotNull(t, "CursorManager type must be resolvable via reflection");
            Assert.AreEqual("CursorManager", t.Name, "CursorManager class name must be unchanged");
        }

        // ── (4) Hub namespace unchanged ────────────────────────────────────────────

        [Test]
        public void hub_namespace_is_territory_ui()
        {
            Type t = FindCursorManagerType();
            Assert.IsNotNull(t, "CursorManager type must be resolvable via reflection");
            Assert.AreEqual("Territory.UI", t.Namespace, "CursorManager namespace must remain Territory.UI");
        }

        // ── (5) [SerializeField] set unchanged ────────────────────────────────────

        [Test]
        public void hub_serialize_field_set_unchanged()
        {
            Type t = FindCursorManagerType();
            Assert.IsNotNull(t, "CursorManager type must be resolvable");

            // placementValidator must remain a [SerializeField] private field
            FieldInfo placementValidatorField = t.GetField("placementValidator",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(placementValidatorField,
                "CursorManager must retain private field 'placementValidator'");
            bool hasSerializeField = placementValidatorField
                .GetCustomAttributes(typeof(SerializeField), false).Length > 0;
            Assert.IsTrue(hasSerializeField,
                "placementValidator must retain [SerializeField] attribute");

            // Public texture fields must remain
            string[] requiredPublicFields = { "cursorTexture", "bulldozerTexture", "detailsTexture", "hotSpot", "gridManager" };
            foreach (string name in requiredPublicFields)
            {
                FieldInfo f = t.GetField(name, BindingFlags.Public | BindingFlags.Instance);
                Assert.IsNotNull(f, $"CursorManager must retain public field '{name}'");
            }
        }

        // ── (6) ICursor interface implemented ─────────────────────────────────────

        [Test]
        public void cursor_manager_implements_ICursor()
        {
            Type t = FindCursorManagerType();
            Assert.IsNotNull(t, "CursorManager type must be resolvable");
            Type iface = typeof(Domains.Cursor.ICursor);
            Assert.IsTrue(iface.IsAssignableFrom(t),
                "CursorManager must implement Domains.Cursor.ICursor");
        }

        // ── (7) ICursor namespace correct ─────────────────────────────────────────

        [Test]
        public void ICursor_is_in_domains_cursor_namespace()
        {
            Type t = typeof(Domains.Cursor.ICursor);
            Assert.AreEqual("Domains.Cursor", t.Namespace,
                $"ICursor must be in Domains.Cursor namespace, got '{t.Namespace}'");
        }

        // ── (8) CursorService is in Domains.Cursor.Services ───────────────────────

        [Test]
        public void CursorService_is_in_domains_cursor_services_namespace()
        {
            Type t = typeof(Domains.Cursor.Services.CursorService);
            Assert.AreEqual("Domains.Cursor.Services", t.Namespace,
                $"CursorService must be in Domains.Cursor.Services, got '{t.Namespace}'");
        }

        // ── (9) Every public method on CursorManager is on ICursor ────────────────

        [Test]
        public void all_public_instance_methods_are_delegated_via_ICursor()
        {
            Type hubType = FindCursorManagerType();
            Assert.IsNotNull(hubType);
            Type ifaceType = typeof(Domains.Cursor.ICursor);

            // Collect interface method names
            var ifaceMethods = new System.Collections.Generic.HashSet<string>(
                ifaceType.GetMethods().Select(m => m.Name));
            // Add event accessors we allow implicitly
            var eventNames = new System.Collections.Generic.HashSet<string>(
                ifaceType.GetEvents().Select(e => e.Name));

            // Public instance methods declared on CursorManager itself (exclude inherited MonoBehaviour surface)
            var declaredPublics = hubType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => !m.IsSpecialName) // exclude property get/set + event add/remove
                .ToArray();

            // Unity lifecycle callbacks allowed even though not on ICursor
            var lifecycleAllowlist = new System.Collections.Generic.HashSet<string>
            {
                "Awake", "Start", "Update", "OnDestroy", "FixedUpdate", "LateUpdate",
                "OnEnable", "OnDisable", "OnApplicationQuit", "OnGUI", "OnValidate",
            };

            foreach (var m in declaredPublics)
            {
                bool onIface = ifaceMethods.Contains(m.Name);
                bool isLifecycle = lifecycleAllowlist.Contains(m.Name);
                Assert.IsTrue(onIface || isLifecycle,
                    $"Public method '{m.Name}' on CursorManager must either appear on ICursor or be a Unity lifecycle callback. If it's new surface, add to ICursor first.");
            }
        }

        // ── (10) ServiceRegistry has ICursor registered (structural, not runtime) ─

        [Test]
        public void CursorManager_Awake_registers_ICursor_in_source()
        {
            string absPath = Path.Combine(
                System.Environment.CurrentDirectory, CursorManagerPath.Replace('/', Path.DirectorySeparatorChar));
            Assert.IsTrue(File.Exists(absPath));
            string src = File.ReadAllText(absPath);
            Assert.IsTrue(src.Contains("Register<ICursor>(this)"),
                "CursorManager.Awake must call _registry.Register<ICursor>(this)");
        }

        // ── Helper ─────────────────────────────────────────────────────────────────

        private static Type FindCursorManagerType()
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = asm.GetType("Territory.UI.CursorManager");
                if (t != null) return t;
            }
            return null;
        }
    }
}
