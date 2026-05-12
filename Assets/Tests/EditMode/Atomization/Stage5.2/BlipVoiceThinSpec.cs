using NUnit.Framework;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace Territory.Tests.EditMode.Atomization.Stage5_2
{
    /// <summary>
    /// §Red-Stage Proof anchor: BlipVoiceThinSpec.cs::blip_voice_is_thin
    /// Stage 5.2: BlipVoice Tier-C NO-PORT — hub collapses to ≤200 LOC; DSP delegated to BlipVoiceService.
    /// Green: BlipVoice.cs ≤200 LOC AND BlipVoiceService.cs exists at Domains/Audio/Services/ AND hub delegates.
    /// </summary>
    public class BlipVoiceThinSpec
    {
        private const string HubPath =
            "Assets/Scripts/Audio/Blip/BlipVoice.cs";

        private const string SvcPath =
            "Assets/Scripts/Domains/Audio/Services/BlipVoiceService.cs";

        [Test]
        public void blip_voice_is_thin()
        {
            string repoRoot = GetRepoRoot();

            // Assert 1: BlipVoice.cs ≤200 LOC
            string hub = Path.Combine(repoRoot, HubPath);
            Assert.IsTrue(File.Exists(hub), $"BlipVoice.cs not found at {hub}");
            int lineCount = File.ReadAllLines(hub).Length;
            Assert.LessOrEqual(lineCount, 200,
                $"BlipVoice.cs must be ≤200 LOC after THIN. Got {lineCount} lines.");

            // Assert 2: BlipVoiceService.cs exists under Domains/Audio/Services/
            string svc = Path.Combine(repoRoot, SvcPath);
            Assert.IsTrue(File.Exists(svc), $"BlipVoiceService.cs must exist at {svc}.");

            // Assert 3: hub delegates to BlipVoiceService
            string hubSource = File.ReadAllText(hub);
            Assert.IsTrue(hubSource.Contains("BlipVoiceService"),
                "BlipVoice hub must reference BlipVoiceService.");
            Assert.IsTrue(hubSource.Contains("BlipVoiceService.Render"),
                "BlipVoice hub must delegate Render to BlipVoiceService.Render.");
        }

        [Test]
        public void blip_voice_service_is_in_correct_namespace()
        {
            Type t = typeof(Territory.Audio.BlipVoiceService);
            Assert.AreEqual("Territory.Audio", t.Namespace,
                $"BlipVoiceService namespace mismatch: {t.Namespace}");
        }

        [Test]
        public void blip_voice_service_exposes_render()
        {
            Type t = typeof(Territory.Audio.BlipVoiceService);
            Assert.IsNotNull(t, "BlipVoiceService must exist");
            // Check for at least one Render overload
            MethodInfo[] methods = t.GetMethods(BindingFlags.Public | BindingFlags.Static);
            bool hasRender = false;
            foreach (var m in methods)
                if (m.Name == "Render") { hasRender = true; break; }
            Assert.IsTrue(hasRender, "BlipVoiceService must expose static Render()");
        }

        [Test]
        public void blip_voice_service_exposes_smooth_one_pole()
        {
            Type t = typeof(Territory.Audio.BlipVoiceService);
            Assert.IsNotNull(t, "BlipVoiceService must exist");
            MethodInfo m = t.GetMethod("SmoothOnePole", BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(m, "BlipVoiceService must expose static SmoothOnePole()");
        }

        [Test]
        public void blip_voice_hub_path_unchanged()
        {
            string repoRoot = GetRepoRoot();
            string hub = Path.Combine(repoRoot, HubPath);
            Assert.IsTrue(File.Exists(hub),
                $"BlipVoice.cs must remain at locked path {HubPath}");
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
