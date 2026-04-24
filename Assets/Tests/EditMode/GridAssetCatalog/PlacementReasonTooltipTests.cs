using System;
using NUnit.Framework;
using UnityEngine;
using Territory.Core;
using Territory.UI;

namespace Territory.Tests.EditMode.GridAsset
{
    /// <summary>TECH-760 — UIManager placement-reason tooltip map + show/hide semantics.</summary>
    public class PlacementReasonTooltipTests
    {
        [Test]
        public void Map_CoversAllEnumValuesExceptNone() { Assert.Inconclusive("authored in /implement (TECH-760)"); }

        [Test]
        public void Show_RendersTextOnInvalidReason() { Assert.Inconclusive("authored in /implement (TECH-760)"); }

        [Test]
        public void Show_NoneEarlyReturnsToHide() { Assert.Inconclusive("authored in /implement (TECH-760)"); }

        [Test]
        public void Show_NoAutoHideAfterFiveSeconds() { Assert.Inconclusive("authored in /implement (TECH-760)"); }

        [Test]
        public void Hide_DeactivatesPanel_NoCoroutine() { Assert.Inconclusive("authored in /implement (TECH-760)"); }
    }
}
