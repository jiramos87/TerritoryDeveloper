using NUnit.Framework;
using Territory.Core;

namespace Territory.Tests.EditMode.Economy
{
    /// <summary>EditMode tests for TECH-689 placement result shape (no Play Mode).</summary>
    public class PlacementValidatorEditModeTests
    {
        [Test]
        public void PlacementResult_Allowed_IsAllowed()
        {
            var r = PlacementResult.Allowed();
            Assert.IsTrue(r.IsAllowed);
            Assert.AreEqual(PlacementFailReason.None, r.Reason);
        }

        [Test]
        public void PlacementResult_Fail_Zoning_NotAllowed()
        {
            var r = PlacementResult.Fail(PlacementFailReason.Zoning, "channel");
            Assert.IsFalse(r.IsAllowed);
            Assert.AreEqual(PlacementFailReason.Zoning, r.Reason);
            Assert.AreEqual("channel", r.Detail);
        }

        [Test]
        public void PlacementResult_Fail_Unaffordable_NotAllowed()
        {
            var r = PlacementResult.Fail(PlacementFailReason.Unaffordable);
            Assert.IsFalse(r.IsAllowed);
            Assert.AreEqual(PlacementFailReason.Unaffordable, r.Reason);
        }
    }
}
