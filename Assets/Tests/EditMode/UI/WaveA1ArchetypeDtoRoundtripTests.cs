using NUnit.Framework;
using UnityEngine;
using Territory.Editor.Bridge;

namespace Territory.Tests.EditMode.UI
{
    /// <summary>
    /// Wave A1 (TECH-27064) — per-archetype IR DTO JsonUtility round-trip tests.
    /// view-slot + confirm-button archetypes.
    /// </summary>
    public class WaveA1ArchetypeDtoRoundtripTests
    {
        // ── view-slot ────────────────────────────────────────────────────────────

        [Test]
        public void ViewSlotDetail_RoundTrip_BindEnum()
        {
            var original = new UiBakeHandler.ViewSlotDetail { bind_enum = "mainmenu.contentScreen" };
            string json = JsonUtility.ToJson(original);
            var restored = JsonUtility.FromJson<UiBakeHandler.ViewSlotDetail>(json);
            Assert.AreEqual("mainmenu.contentScreen", restored.bind_enum);
        }

        [Test]
        public void ViewSlotDetail_RoundTrip_Empty_BindEnum()
        {
            var original = new UiBakeHandler.ViewSlotDetail { bind_enum = string.Empty };
            string json = JsonUtility.ToJson(original);
            var restored = JsonUtility.FromJson<UiBakeHandler.ViewSlotDetail>(json);
            Assert.IsNotNull(restored);
            Assert.AreEqual(string.Empty, restored.bind_enum);
        }

        // ── confirm-button ───────────────────────────────────────────────────────

        [Test]
        public void ConfirmButtonDetail_RoundTrip_AllFields()
        {
            var original = new UiBakeHandler.ConfirmButtonDetail
            {
                action          = "mainmenu.quit",
                confirm_action  = "mainmenu.quit-confirmed",
                confirm_seconds = 3,
            };
            string json = JsonUtility.ToJson(original);
            var restored = JsonUtility.FromJson<UiBakeHandler.ConfirmButtonDetail>(json);
            Assert.AreEqual("mainmenu.quit",           restored.action);
            Assert.AreEqual("mainmenu.quit-confirmed", restored.confirm_action);
            Assert.AreEqual(3,                         restored.confirm_seconds);
        }

        [Test]
        public void ConfirmButtonDetail_DefaultSeconds_IsThree()
        {
            var dto = new UiBakeHandler.ConfirmButtonDetail();
            Assert.AreEqual(3, dto.confirm_seconds,
                "confirm_seconds default must be 3 — required by spec");
        }

        [Test]
        public void ConfirmButtonDetail_RoundTrip_CustomSeconds()
        {
            var original = new UiBakeHandler.ConfirmButtonDetail
            {
                action          = "logout",
                confirm_action  = "logout-confirmed",
                confirm_seconds = 5,
            };
            string json = JsonUtility.ToJson(original);
            var restored = JsonUtility.FromJson<UiBakeHandler.ConfirmButtonDetail>(json);
            Assert.AreEqual(5, restored.confirm_seconds);
        }
    }
}
