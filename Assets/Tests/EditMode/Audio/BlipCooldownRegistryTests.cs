using NUnit.Framework;
using Territory.Audio;

namespace Territory.Tests.EditMode.Audio
{
    /// <summary>
    /// EditMode unit tests for <see cref="BlipCooldownRegistry"/>.
    /// All tests run without a Play Mode harness — registry is clock-agnostic.
    /// </summary>
    [TestFixture]
    public class BlipCooldownRegistryTests
    {
        private BlipCooldownRegistry _registry;

        [SetUp]
        public void SetUp()
        {
            _registry = new BlipCooldownRegistry();
        }

        /// <summary>
        /// First call for an unseen id must return <c>true</c> and record the
        /// timestamp, so a subsequent call within the window returns <c>false</c>.
        /// </summary>
        [Test]
        public void FirstCall_Unseen_Returns_True_And_Records()
        {
            const double t1         = 1.0;
            const double cooldownMs = 100.0;

            bool first  = _registry.TryConsume(BlipId.UiButtonClick, t1, cooldownMs);
            bool second = _registry.TryConsume(BlipId.UiButtonClick, t1 + 0.05, cooldownMs); // 50 ms < 100 ms

            Assert.IsTrue(first,   "First call for unseen id must return true.");
            Assert.IsFalse(second, "Second call within cooldown window must return false.");
        }

        /// <summary>
        /// A blocked attempt must NOT update the anchor timestamp.
        /// Window is anchored to the first accepted time, so a third call that
        /// clears the original window (but not the rejected attempt's time) passes.
        /// </summary>
        [Test]
        public void WithinWindow_Returns_False_And_Does_Not_Update()
        {
            const double t1         = 1.0;
            const double cooldownMs = 100.0;

            // First accepted call — anchor = t1
            _registry.TryConsume(BlipId.UiButtonClick, t1, cooldownMs);

            // Rejected call at t1 + 50 ms — anchor must stay at t1
            bool rejected = _registry.TryConsume(BlipId.UiButtonClick, t1 + 0.05, cooldownMs);

            // Third call at t1 + 50 + 60 = t1 + 110 ms — past window anchored at t1
            bool third = _registry.TryConsume(BlipId.UiButtonClick, t1 + 0.05 + 0.06, cooldownMs);

            Assert.IsFalse(rejected, "Call within window must return false.");
            Assert.IsTrue(third,     "Call past original anchor window must return true.");
        }

        /// <summary>
        /// A call past the cooldown window must return <c>true</c> and update the anchor.
        /// </summary>
        [Test]
        public void PastWindow_Returns_True_And_Updates()
        {
            const double t1         = 1.0;
            const double cooldownMs = 100.0;

            // Anchor at t1
            _registry.TryConsume(BlipId.UiButtonClick, t1, cooldownMs);

            // 200 ms later — past the 100 ms window
            bool result = _registry.TryConsume(BlipId.UiButtonClick, t1 + 0.2, cooldownMs);

            Assert.IsTrue(result, "Call past cooldown window must return true.");

            // Verify anchor updated: immediate follow-up within new window must be blocked
            bool blocked = _registry.TryConsume(BlipId.UiButtonClick, t1 + 0.2 + 0.01, cooldownMs);
            Assert.IsFalse(blocked, "Follow-up within updated window must be blocked.");
        }

        /// <summary>
        /// <c>cooldownMs == 0</c> degenerates to always-pass: every call returns <c>true</c>.
        /// </summary>
        [Test]
        public void ZeroCooldown_Always_Returns_True()
        {
            const double t1 = 1.0;

            bool first  = _registry.TryConsume(BlipId.UiButtonClick, t1, cooldownMs: 0.0);
            bool second = _registry.TryConsume(BlipId.UiButtonClick, t1, cooldownMs: 0.0); // same time

            Assert.IsTrue(first,  "Zero-cooldown first call must return true.");
            Assert.IsTrue(second, "Zero-cooldown second call (same time) must return true.");
        }

        /// <summary>
        /// Distinct <see cref="BlipId"/> values use independent tracking buckets.
        /// A cooldown on one id must not affect another.
        /// </summary>
        [Test]
        public void DistinctIds_Track_Independently()
        {
            const double t1         = 1.0;
            const double cooldownMs = 100.0;

            // Anchor UiButtonClick at t1
            bool firstA = _registry.TryConsume(BlipId.UiButtonClick, t1, cooldownMs);

            // ToolRoadTick never seen — must pass regardless of UiButtonClick window
            bool firstB = _registry.TryConsume(BlipId.ToolRoadTick, t1 + 0.01, cooldownMs);

            // UiButtonClick within window — must still be blocked
            bool secondA = _registry.TryConsume(BlipId.UiButtonClick, t1 + 0.05, cooldownMs);

            Assert.IsTrue(firstA,   "UiButtonClick first call must return true.");
            Assert.IsTrue(firstB,   "ToolRoadTick first call must return true (independent bucket).");
            Assert.IsFalse(secondA, "UiButtonClick within window must return false.");
        }
    }
}
