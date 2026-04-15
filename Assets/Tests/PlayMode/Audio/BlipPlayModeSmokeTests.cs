using System.Collections;
using NUnit.Framework;
using Territory.Audio;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Territory.Tests.PlayMode.Audio
{
    /// <summary>
    /// PlayMode smoke tests for the Blip audio subsystem.
    /// <para>
    /// <see cref="SetUp"/> loads <c>MainMenu</c> (build index 0) which hosts the
    /// <c>BlipBootstrap</c> persistent root.  Two yield-frames satisfy the Awake
    /// cascade (frame 1) and catalog-ready flag (frame 2).
    /// </para>
    /// <para>
    /// <see cref="TearDown"/> unloads <c>MainMenu</c> so the next <c>SetUp</c>
    /// re-enters a clean state — guards against bootstrap double-init across tests.
    /// </para>
    /// </summary>
    public sealed class BlipPlayModeSmokeTests
    {
        private BlipCatalog _catalog;
        private BlipPlayer  _player;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
            yield return null; // one extra frame for Start cascade

            _catalog = Object.FindObjectOfType<BlipCatalog>();
            _player  = Object.FindObjectOfType<BlipPlayer>();

            Assert.IsNotNull(BlipBootstrap.Instance, "Blip bootstrap missing");
            Assert.IsTrue(_catalog != null && _catalog.IsReady, "BlipCatalog.IsReady false after LoadScene");
        }

        // -----------------------------------------------------------------
        // Phase 1 — catalog readiness gate (TECH-212 Phase 1)
        // Phase 2 — 10-id resolve + mixer-group assertions (TECH-212 Phase 2)
        // [Test] (synchronous): _catalog.IsReady already asserted in [UnitySetUp].
        // -----------------------------------------------------------------
        [Test]
        public void Catalog_AllMvpIds_Resolve_WithMixerGroup()
        {
            // Phase 1 gate: readiness + router non-null.
            Assert.IsTrue(_catalog.IsReady, "BlipCatalog.IsReady false at test entry");
            Assert.IsNotNull(_catalog.MixerRouter, "BlipCatalog.MixerRouter null");

            // Phase 2: per-id resolve + hash + mixer-group.
            var mvpIds = new[]
            {
                BlipId.UiButtonHover,
                BlipId.UiButtonClick,
                BlipId.ToolRoadTick,
                BlipId.ToolRoadComplete,
                BlipId.ToolBuildingPlace,
                BlipId.ToolBuildingDenied,
                BlipId.WorldCellSelected,
                BlipId.EcoMoneyEarned,
                BlipId.EcoMoneySpent,
                BlipId.SysSaveGame,
            };

            foreach (var id in mvpIds)
            {
                string name = id.ToString();

                // Assert 1 — patch registered (Resolve throws on unknown id; DoesNotThrow = patch != null).
                Assert.DoesNotThrow(() => _catalog.Resolve(id),
                    $"Resolve threw for {name}");

                // Assert 2 — patchHash non-zero.
                Assert.AreNotEqual(0, _catalog.PatchHash(id),
                    $"patchHash == 0 for {name}");

                // Assert 3 — mixer-group non-null.
                Assert.IsNotNull(_catalog.MixerRouter.Get(id),
                    $"MixerRouter.Get null for {name}");
            }
        }

        [UnityTest]
        public IEnumerator Play_AllMvpIds_ResolvesAndRoutes()
        {
            var mvpIds = new[]
            {
                BlipId.UiButtonHover,
                BlipId.UiButtonClick,
                BlipId.ToolRoadTick,
                BlipId.ToolRoadComplete,
                BlipId.ToolBuildingPlace,
                BlipId.ToolBuildingDenied,
                BlipId.WorldCellSelected,
                BlipId.EcoMoneyEarned,
                BlipId.EcoMoneySpent,
                BlipId.SysSaveGame,
            };

            foreach (var id in mvpIds)
            {
                Assert.DoesNotThrow(() => _catalog.Resolve(id),
                    $"Resolve threw for {id}");
                Assert.AreNotEqual(0, _catalog.PatchHash(id),
                    $"PatchHash == 0 for {id}");
                Assert.IsNotNull(_catalog.MixerRouter.Get(id),
                    $"mixer group null for {id}");
                Assert.DoesNotThrow(() => BlipEngine.Play(id),
                    $"BlipEngine.Play threw for {id}");
            }

            yield return null; // drain AudioSource.Play side-effects
        }

        [UnityTest]
        public IEnumerator Play_RapidFire_ExhaustsPoolAndBlocksOnCooldown()
        {
            // ── Rapid-fire leg ────────────────────────────────────────────────
            // 16 consecutive Play calls on ToolRoadTick (cooldownMs == 0).
            // Pool wraps: cursor advances 0→1→…→15→0; final DebugCursor == 0.
            // No exception expected.
            for (int i = 0; i < 16; i++)
            {
                Assert.DoesNotThrow(() => BlipEngine.Play(BlipId.ToolRoadTick),
                    $"BlipEngine.Play threw on rapid-fire call {i}");
            }
            Assert.AreEqual(0, _player.DebugCursor,
                "DebugCursor should be 0 after 16-call pool wrap");

            // ── Cooldown block leg ────────────────────────────────────────────
            // MVP catalog patches all have cooldownMs == 0, so BlipEngine.Play
            // cannot trigger a block via the normal dispatch path.  Exercise the
            // BlipCooldownRegistry block path directly with an explicit 5 000 ms
            // window — this is the only way to reach the BlockedCount increment
            // without a catalog mutation (out-of-scope per spec §3).
            var registry = _catalog.CooldownRegistry;
            double now = UnityEngine.AudioSettings.dspTime;
            int baseline = registry.BlockedCount;

            // First consume: unseen id with large window → accepted (returns true).
            bool first = registry.TryConsume(BlipId.SysSaveGame, now, 5000.0);
            Assert.IsTrue(first, "First TryConsume should be accepted");

            // Second consume: same DSP time, still inside 5 000 ms window → blocked.
            bool second = registry.TryConsume(BlipId.SysSaveGame, now, 5000.0);
            Assert.IsFalse(second, "Second TryConsume should be blocked by cooldown");

            Assert.AreEqual(1, registry.BlockedCount - baseline,
                "BlockedCount delta should be 1 after one blocked call");

            yield return null; // clean teardown frame
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            // Cannot unload the only active scene. Clear refs only;
            // next SetUp's LoadSceneAsync(Single) replaces the scene.
            _catalog = null;
            _player  = null;
            yield return null;
        }
    }
}
