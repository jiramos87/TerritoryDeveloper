using NUnit.Framework;
using System;
using System.IO;
using UnityEngine;

namespace Territory.Tests.EditMode.Atomization.Bridge
{
    /// <summary>
    /// Tracer tests: assert MutationDispatchService + ConformanceService extracted to
    /// Domains.Bridge.Services namespace + IBridge facade present in Domains.Bridge.
    /// Red baseline: Domains/Bridge/ absent → asserts fail.
    /// Green: Bridge.Editor.asmdef + MutationDispatchService + ConformanceService + IBridge all present.
    /// §Red-Stage Proof anchor: AgentBridgeCommandRunnerAtomizationTests.cs::MutationDispatchService_is_in_domains_bridge_services_namespace
    /// </summary>
    public class AgentBridgeCommandRunnerAtomizationTests
    {
        [Test]
        public void MutationDispatchService_is_in_domains_bridge_services_namespace()
        {
            Type serviceType = typeof(Domains.Bridge.Services.MutationDispatchService);
            Assert.AreEqual("Domains.Bridge.Services", serviceType.Namespace,
                $"Expected namespace 'Domains.Bridge.Services', got '{serviceType.Namespace}'");
        }

        [Test]
        public void ConformanceService_is_in_domains_bridge_services_namespace()
        {
            Type serviceType = typeof(Domains.Bridge.Services.ConformanceService);
            Assert.AreEqual("Domains.Bridge.Services", serviceType.Namespace,
                $"Expected namespace 'Domains.Bridge.Services', got '{serviceType.Namespace}'");
        }

        [Test]
        public void IBridge_facade_exists_in_domains_bridge_namespace()
        {
            Type ifaceType = typeof(Domains.Bridge.IBridge);
            Assert.AreEqual("Domains.Bridge", ifaceType.Namespace,
                $"Expected namespace 'Domains.Bridge', got '{ifaceType.Namespace}'");
        }

        [Test]
        public void IBridge_facade_exposes_TryDispatchMutation_method()
        {
            Type ifaceType = typeof(Domains.Bridge.IBridge);
            var method = ifaceType.GetMethod("TryDispatchMutation");
            Assert.IsNotNull(method, "IBridge must expose TryDispatchMutation(kind, repoRoot, commandId, requestJson)");
        }

        [Test]
        public void IBridge_facade_exposes_RunConformance_method()
        {
            Type ifaceType = typeof(Domains.Bridge.IBridge);
            var method = ifaceType.GetMethod("RunConformance");
            Assert.IsNotNull(method, "IBridge must expose RunConformance(repoRoot, commandId, requestJson)");
        }

        [Test]
        public void ConformanceService_SupportedCheckKindCount_is_eight()
        {
            var svc = new Domains.Bridge.Services.ConformanceService();
            Assert.AreEqual(8, svc.SupportedCheckKindCount,
                $"ConformanceService.SupportedCheckKindCount parity: expected 8, got {svc.SupportedCheckKindCount}");
        }

        [Test]
        public void ConformanceService_SupportedCheckKinds_contains_palette_ramp()
        {
            var kinds = Domains.Bridge.Services.ConformanceService.SupportedCheckKinds;
            Assert.IsNotNull(kinds, "SupportedCheckKinds must not be null");
            Assert.IsTrue(System.Array.IndexOf(kinds, "palette_ramp") >= 0,
                "SupportedCheckKinds must contain 'palette_ramp'");
        }

        [Test]
        public void ConformanceService_SupportedCheckKinds_contains_contrast_ratio()
        {
            var kinds = Domains.Bridge.Services.ConformanceService.SupportedCheckKinds;
            Assert.IsTrue(System.Array.IndexOf(kinds, "contrast_ratio") >= 0,
                "SupportedCheckKinds must contain 'contrast_ratio'");
        }

        [Test]
        public void MutationDispatchService_TryDispatch_method_exists()
        {
            var svcType = typeof(Domains.Bridge.Services.MutationDispatchService);
            var method = svcType.GetMethod("TryDispatch");
            Assert.IsNotNull(method, "MutationDispatchService must expose TryDispatch(kind, repoRoot, commandId, requestJson)");
        }

        [Test]
        public void bridge_editor_asmdef_exists()
        {
            string path = Path.Combine(Application.dataPath, "Scripts", "Domains", "Bridge", "Bridge.Editor.asmdef");
            Assert.IsTrue(File.Exists(path), $"Bridge.Editor.asmdef not found at: {path}");
        }
    }
}
