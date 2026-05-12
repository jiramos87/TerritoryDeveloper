using NUnit.Framework;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace Territory.Tests.EditMode.Atomization.Stage5_5
{
    /// <summary>
    /// §Red-Stage Proof anchor: GameNotificationManagerThinSpec.cs::game_notification_manager_is_thin_with_guardrail_8
    /// Stage 5.5: GameNotificationManager Tier-C NO-PORT — hub collapses to ≤200 LOC; queue logic delegated to NotificationService.
    /// Green: GameNotificationManager.cs ≤200 LOC AND NotificationService.cs exists AND lazy-init pattern preserved.
    /// Guardrail #8: lazy-init notification panel + EditMode fixture omission preserved.
    /// </summary>
    public class GameNotificationManagerThinSpec
    {
        private const string HubPath =
            "Assets/Scripts/Managers/GameManagers/GameNotificationManager.cs";

        private const string SvcPath =
            "Assets/Scripts/Domains/Notifications/Services/NotificationService.cs";

        [Test]
        public void game_notification_manager_is_thin_with_guardrail_8()
        {
            string repoRoot = GetRepoRoot();

            // Assert 1: GameNotificationManager.cs ≤200 LOC
            string hub = Path.Combine(repoRoot, HubPath);
            Assert.IsTrue(File.Exists(hub), $"GameNotificationManager.cs not found at {hub}");
            int lineCount = File.ReadAllLines(hub).Length;
            Assert.LessOrEqual(lineCount, 200,
                $"GameNotificationManager.cs must be ≤200 LOC after THIN. Got {lineCount} lines.");

            // Assert 2: NotificationService.cs exists under Domains/Notifications/Services/
            string svc = Path.Combine(repoRoot, SvcPath);
            Assert.IsTrue(File.Exists(svc), $"NotificationService.cs must exist at {svc}.");

            // Assert 3 (lazy_init_pattern_preserved) — guardrail #8
            // Hub must retain LazyCreateNotificationUi (panel lazy-init stays in hub, not service).
            string hubSource = File.ReadAllText(hub);
            Assert.IsTrue(hubSource.Contains("LazyCreateNotificationUi"),
                "GameNotificationManager hub must retain LazyCreateNotificationUi — guardrail #8.");

            // Assert 4 (editmode_fixture_omission_preserved) — guardrail #8
            // No [SetUp]/[TearDown] fixture in hub (runtime MonoBehaviour, not a test class).
            Assert.IsFalse(hubSource.Contains("[SetUp]") || hubSource.Contains("[TearDown]"),
                "GameNotificationManager must not carry NUnit fixture attributes — guardrail #8.");

            // Assert 5: hub delegates to NotificationService (_svc field present)
            Assert.IsTrue(hubSource.Contains("NotificationService"),
                "GameNotificationManager hub must reference NotificationService.");
            Assert.IsTrue(hubSource.Contains("_svc"),
                "GameNotificationManager hub must hold a _svc delegate field.");
        }

        [Test]
        public void notification_service_is_in_correct_namespace()
        {
            Type t = typeof(Domains.Notifications.Services.NotificationService);
            Assert.AreEqual("Domains.Notifications.Services", t.Namespace,
                $"NotificationService namespace mismatch: {t.Namespace}");
        }

        [Test]
        public void notification_service_exposes_wire_config()
        {
            Type t = typeof(Domains.Notifications.Services.NotificationService);
            Assert.IsNotNull(t, "NotificationService must exist");
            MethodInfo m = t.GetMethod("WireConfig");
            Assert.IsNotNull(m, "NotificationService must expose WireConfig()");
        }

        [Test]
        public void notification_service_exposes_enqueue()
        {
            Type t = typeof(Domains.Notifications.Services.NotificationService);
            Assert.IsNotNull(t, "NotificationService must exist");
            MethodInfo m = t.GetMethod("Enqueue");
            Assert.IsNotNull(m, "NotificationService must expose Enqueue()");
        }

        [Test]
        public void notification_service_exposes_try_dequeue_next()
        {
            Type t = typeof(Domains.Notifications.Services.NotificationService);
            Assert.IsNotNull(t, "NotificationService must exist");
            MethodInfo m = t.GetMethod("TryDequeueNext");
            Assert.IsNotNull(m, "NotificationService must expose TryDequeueNext()");
        }

        [Test]
        public void notification_service_exposes_clear()
        {
            Type t = typeof(Domains.Notifications.Services.NotificationService);
            Assert.IsNotNull(t, "NotificationService must exist");
            MethodInfo m = t.GetMethod("Clear");
            Assert.IsNotNull(m, "NotificationService must expose Clear()");
        }

        [Test]
        public void hub_path_unchanged()
        {
            string repoRoot = GetRepoRoot();
            string hub = Path.Combine(repoRoot, HubPath);
            Assert.IsTrue(File.Exists(hub),
                $"GameNotificationManager.cs must remain at locked path {HubPath}");
        }

        private static string GetRepoRoot()
        {
            string dir = Application.dataPath;
            while (!string.IsNullOrEmpty(dir))
            {
                if (File.Exists(Path.Combine(dir, "CLAUDE.md"))) return dir;
                string parent = Path.GetDirectoryName(dir);
                if (parent == dir) break;
                dir = parent;
            }
            return Application.dataPath.Replace("/Assets", "");
        }
    }
}
