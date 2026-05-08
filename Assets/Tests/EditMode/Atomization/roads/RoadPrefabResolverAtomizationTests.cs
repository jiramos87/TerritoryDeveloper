using NUnit.Framework;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace Territory.Tests.EditMode.Atomization.Roads
{
/// <summary>
/// Tracer tests: assert PrefabResolverService extracted to Domains.Roads.Services assembly
/// and RoadPrefabResolver remains as thin facade in Territory.Roads.
/// Red baseline: PrefabResolverService absent → asserts fail.
/// Green: PrefabResolverService present in Domains.Roads.Services; compile-check exits 0.
/// §Red-Stage Proof anchor: RoadPrefabResolverAtomizationTests.cs::PrefabResolverService_is_in_domains_roads_services_namespace
/// </summary>
public class RoadPrefabResolverAtomizationTests
{
    [Test]
    public void PrefabResolverService_is_in_domains_roads_services_namespace()
    {
        Type serviceType = typeof(Domains.Roads.Services.PrefabResolverService);
        Assert.AreEqual("Domains.Roads.Services", serviceType.Namespace,
            $"Expected namespace 'Domains.Roads.Services', got '{serviceType.Namespace}'");
    }

    [Test]
    public void PrefabResolverService_is_not_monobehaviour()
    {
        Type serviceType = typeof(Domains.Roads.Services.PrefabResolverService);
        Assert.IsFalse(typeof(MonoBehaviour).IsAssignableFrom(serviceType),
            "PrefabResolverService must not inherit MonoBehaviour — POCO only");
    }

    [Test]
    public void PrefabResolverService_exposes_ResolveForPath_method()
    {
        Type serviceType = typeof(Domains.Roads.Services.PrefabResolverService);
        MethodInfo method = serviceType.GetMethod("ResolveForPath", BindingFlags.Public | BindingFlags.Instance);
        Assert.IsNotNull(method, "PrefabResolverService must expose public ResolveForPath()");
    }

    [Test]
    public void PrefabResolverService_exposes_ResolveForCell_method()
    {
        Type serviceType = typeof(Domains.Roads.Services.PrefabResolverService);
        MethodInfo method = serviceType.GetMethod("ResolveForCell", BindingFlags.Public | BindingFlags.Instance);
        Assert.IsNotNull(method, "PrefabResolverService must expose public ResolveForCell()");
    }

    [Test]
    public void PrefabResolverService_exposes_ResolveForGhostPreview_method()
    {
        Type serviceType = typeof(Domains.Roads.Services.PrefabResolverService);
        MethodInfo method = serviceType.GetMethod("ResolveForGhostPreview", BindingFlags.Public | BindingFlags.Instance);
        Assert.IsNotNull(method, "PrefabResolverService must expose public ResolveForGhostPreview()");
    }

    [Test]
    public void RoadPrefabResolver_facade_remains_in_territory_roads_namespace()
    {
        Type facadeType = typeof(Territory.Roads.RoadPrefabResolver);
        Assert.AreEqual("Territory.Roads", facadeType.Namespace,
            $"RoadPrefabResolver facade must remain in Territory.Roads, got '{facadeType.Namespace}'");
    }

    [Test]
    public void RoadPrefabResolver_facade_is_not_monobehaviour()
    {
        Type facadeType = typeof(Territory.Roads.RoadPrefabResolver);
        Assert.IsFalse(typeof(MonoBehaviour).IsAssignableFrom(facadeType),
            "RoadPrefabResolver facade must not inherit MonoBehaviour");
    }

    [Test]
    public void roads_asmdef_exists()
    {
        string path = System.IO.Path.Combine(Application.dataPath, "Scripts", "Domains", "Roads", "Roads.asmdef");
        Assert.IsTrue(File.Exists(path), $"Roads.asmdef not found at: {path}");
    }
}
}
