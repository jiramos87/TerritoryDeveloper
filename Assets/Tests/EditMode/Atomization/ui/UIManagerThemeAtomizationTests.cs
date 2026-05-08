using NUnit.Framework;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace Territory.Tests.EditMode.Atomization.UI
{
    /// <summary>
    /// Tracer tests: assert ThemeService extracted to Domains.UI.Services + ITheme facade present in Domains.UI.
    /// Red baseline: Domains/UI/Services/ThemeService.cs absent → asserts fail.
    /// Green: UI.Runtime.asmdef + ThemeService + ITheme all present; compile-check exits 0.
    /// §Red-Stage Proof anchor: UIManagerThemeAtomizationTests.cs::ThemeService_is_in_domains_ui_services_namespace
    /// </summary>
    public class UIManagerThemeAtomizationTests
    {
        [Test]
        public void ThemeService_is_in_domains_ui_services_namespace()
        {
            Type serviceType = typeof(Domains.UI.Services.ThemeService);
            Assert.AreEqual("Domains.UI.Services", serviceType.Namespace,
                $"Expected namespace 'Domains.UI.Services', got '{serviceType.Namespace}'");
        }

        [Test]
        public void ITheme_facade_exists_in_domains_ui_namespace()
        {
            Type ifaceType = typeof(Domains.UI.ITheme);
            Assert.AreEqual("Domains.UI", ifaceType.Namespace,
                $"Expected namespace 'Domains.UI', got '{ifaceType.Namespace}'");
        }

        [Test]
        public void ITheme_facade_exposes_StyleSiblingLabelTexts_method()
        {
            Type ifaceType = typeof(Domains.UI.ITheme);
            MethodInfo method = ifaceType.GetMethod("StyleSiblingLabelTexts");
            Assert.IsNotNull(method, "ITheme must expose StyleSiblingLabelTexts(Transform, int, Color)");
        }

        [Test]
        public void ITheme_facade_exposes_FindNamedAncestor_method()
        {
            Type ifaceType = typeof(Domains.UI.ITheme);
            MethodInfo method = ifaceType.GetMethod("FindNamedAncestor");
            Assert.IsNotNull(method, "ITheme must expose FindNamedAncestor(Transform, string)");
        }

        [Test]
        public void ThemeService_StyleSiblingLabelTexts_is_static()
        {
            Type svcType = typeof(Domains.UI.Services.ThemeService);
            MethodInfo method = svcType.GetMethod("StyleSiblingLabelTexts",
                BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(method, "ThemeService.StyleSiblingLabelTexts must be a public static method");
        }

        [Test]
        public void ThemeService_FindNamedAncestor_is_static()
        {
            Type svcType = typeof(Domains.UI.Services.ThemeService);
            MethodInfo method = svcType.GetMethod("FindNamedAncestor",
                BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(method, "ThemeService.FindNamedAncestor must be a public static method");
        }

        [Test]
        public void ThemeService_TryGetRectBoundsInParent_is_static()
        {
            Type svcType = typeof(Domains.UI.Services.ThemeService);
            MethodInfo method = svcType.GetMethod("TryGetRectBoundsInParent",
                BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(method, "ThemeService.TryGetRectBoundsInParent must be a public static method");
        }

        [Test]
        public void ThemeService_CreateDividerStripe_is_static()
        {
            Type svcType = typeof(Domains.UI.Services.ThemeService);
            MethodInfo method = svcType.GetMethod("CreateDividerStripe",
                BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(method, "ThemeService.CreateDividerStripe must be a public static method");
        }

        [Test]
        public void ui_runtime_asmdef_exists()
        {
            string path = Path.Combine(Application.dataPath, "Scripts", "Domains", "UI", "UI.Runtime.asmdef");
            Assert.IsTrue(File.Exists(path), $"UI.Runtime.asmdef not found at: {path}");
        }
    }
}
