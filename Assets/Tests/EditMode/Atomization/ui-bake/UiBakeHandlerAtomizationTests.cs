using NUnit.Framework;
using System;
using System.IO;
using UnityEngine;

namespace Territory.Tests.EditMode.Atomization.UiBake
{
    /// <summary>
    /// Tracer tests: assert UiBakeService + IUiBake facade present in Domains.UI.Editor.UiBake namespace.
    /// Red baseline: Domains/UI/Editor/UiBake/ absent → asserts fail.
    /// Green: UI.Editor.asmdef + UiBakeService + IUiBake all present.
    /// §Red-Stage Proof anchor: UiBakeHandlerAtomizationTests.cs::UiBakeService_is_in_domains_ui_editor_uibake_services_namespace
    /// </summary>
    public class UiBakeHandlerAtomizationTests
    {
        [Test]
        public void UiBakeService_is_in_domains_ui_editor_uibake_services_namespace()
        {
            Type serviceType = typeof(Domains.UI.Editor.UiBake.Services.UiBakeService);
            Assert.AreEqual("Domains.UI.Editor.UiBake.Services", serviceType.Namespace,
                $"Expected namespace 'Domains.UI.Editor.UiBake.Services', got '{serviceType.Namespace}'");
        }

        [Test]
        public void IUiBake_facade_exists_in_domains_ui_editor_uibake_namespace()
        {
            Type ifaceType = typeof(Domains.UI.Editor.UiBake.IUiBake);
            Assert.AreEqual("Domains.UI.Editor.UiBake", ifaceType.Namespace,
                $"Expected namespace 'Domains.UI.Editor.UiBake', got '{ifaceType.Namespace}'");
        }

        [Test]
        public void IUiBake_facade_exposes_BakeFromSnapshot_method()
        {
            Type ifaceType = typeof(Domains.UI.Editor.UiBake.IUiBake);
            var method = ifaceType.GetMethod("BakeFromSnapshot");
            Assert.IsNotNull(method, "IUiBake must expose BakeFromSnapshot(panelsPath, outDir, themeSoPath)");
        }

        [Test]
        public void IUiBake_facade_exposes_ParseSnapshot_method()
        {
            Type ifaceType = typeof(Domains.UI.Editor.UiBake.IUiBake);
            var method = ifaceType.GetMethod("ParseSnapshot");
            Assert.IsNotNull(method, "IUiBake must expose ParseSnapshot(snapshotJson)");
        }

        [Test]
        public void UiBakeService_BakeFromSnapshot_method_exists()
        {
            var svcType = typeof(Domains.UI.Editor.UiBake.Services.UiBakeService);
            var method = svcType.GetMethod("BakeFromSnapshot");
            Assert.IsNotNull(method, "UiBakeService must expose BakeFromSnapshot(panelsPath, outDir, themeSoPath)");
        }

        [Test]
        public void UiBakeService_ParseSnapshot_method_exists()
        {
            var svcType = typeof(Domains.UI.Editor.UiBake.Services.UiBakeService);
            var method = svcType.GetMethod("ParseSnapshot");
            Assert.IsNotNull(method, "UiBakeService must expose ParseSnapshot(snapshotJson)");
        }

        [Test]
        public void ui_editor_asmdef_exists()
        {
            string path = Path.Combine(Application.dataPath, "Scripts", "Domains", "UI", "Editor", "UiBake", "UI.Editor.asmdef");
            Assert.IsTrue(File.Exists(path), $"UI.Editor.asmdef not found at: {path}");
        }
    }
}
