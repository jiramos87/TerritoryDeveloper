using NUnit.Framework;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace Territory.Tests.EditMode.Atomization.Roads
{
/// <summary>
/// Tracer tests: assert AutoBuildService extracted to Domains.Roads.Services assembly
/// and AutoRoadBuilder MonoBehaviour stays at original path as facade.
/// Red baseline: Domains/Roads/Services/AutoBuildService.cs absent → asserts fail.
/// Green: AutoBuildService present in Domains.Roads.Services; compile-check exits 0.
/// §Red-Stage Proof anchor: AutoRoadBuilderAtomizationTests.cs::AutoBuildService_is_in_domains_roads_services_namespace
/// </summary>
public class AutoRoadBuilderAtomizationTests
{
    [Test]
    public void AutoBuildService_is_in_domains_roads_services_namespace()
    {
        Type serviceType = typeof(Domains.Roads.Services.AutoBuildService);
        Assert.AreEqual("Domains.Roads.Services", serviceType.Namespace,
            $"Expected namespace 'Domains.Roads.Services', got '{serviceType.Namespace}'");
    }

    [Test]
    public void AutoBuildService_is_not_monobehaviour()
    {
        Type serviceType = typeof(Domains.Roads.Services.AutoBuildService);
        Assert.IsFalse(typeof(MonoBehaviour).IsAssignableFrom(serviceType),
            "AutoBuildService must not inherit MonoBehaviour — POCO only");
    }

    [Test]
    public void AutoBuildService_exposes_ProcessTick_method()
    {
        Type serviceType = typeof(Domains.Roads.Services.AutoBuildService);
        MethodInfo method = serviceType.GetMethod("ProcessTick", BindingFlags.Public | BindingFlags.Instance);
        Assert.IsNotNull(method, "AutoBuildService must expose public ProcessTick()");
    }

    [Test]
    public void AutoBuildService_exposes_OnSegmentCompleted_callback()
    {
        Type serviceType = typeof(Domains.Roads.Services.AutoBuildService);
        FieldInfo field = serviceType.GetField("OnSegmentCompleted", BindingFlags.Public | BindingFlags.Instance);
        Assert.IsNotNull(field, "AutoBuildService must expose OnSegmentCompleted Action field (callback-based design)");
    }

    [Test]
    public void AutoBuildService_exposes_OnTickStart_callback()
    {
        Type serviceType = typeof(Domains.Roads.Services.AutoBuildService);
        FieldInfo field = serviceType.GetField("OnTickStart", BindingFlags.Public | BindingFlags.Instance);
        Assert.IsNotNull(field, "AutoBuildService must expose OnTickStart Action field (callback-based design)");
    }

    [Test]
    public void AutoBuildService_does_not_own_segment_lists()
    {
        // Callback design: facade owns lists; service fires Action delegates.
        Type serviceType = typeof(Domains.Roads.Services.AutoBuildService);
        PropertyInfo completedProp = serviceType.GetProperty("CompletedSegmentsThisTick");
        PropertyInfo pendingProp = serviceType.GetProperty("PendingZoningSegments");
        Assert.IsNull(completedProp, "AutoBuildService must NOT own CompletedSegmentsThisTick (facade owns it)");
        Assert.IsNull(pendingProp, "AutoBuildService must NOT own PendingZoningSegments (facade owns it)");
    }

    [Test]
    public void AutoBuildService_file_exists_in_domains_roads_services()
    {
        string path = Path.Combine(Application.dataPath, "Scripts", "Domains", "Roads", "Services", "AutoBuildService.cs");
        Assert.IsTrue(File.Exists(path), $"AutoBuildService.cs not found at: {path}");
    }

    [Test]
    public void AutoBuildService_MaxBridgeWaterTiles_is_five()
    {
        var field = typeof(Domains.Roads.Services.AutoBuildService)
            .GetField("MaxBridgeWaterTiles", BindingFlags.Public | BindingFlags.Static);
        Assert.IsNotNull(field, "MaxBridgeWaterTiles const must exist on AutoBuildService");
        int value = (int)field.GetValue(null);
        Assert.AreEqual(5, value, $"MaxBridgeWaterTiles behavior parity: expected 5, got {value}");
    }
}
}
