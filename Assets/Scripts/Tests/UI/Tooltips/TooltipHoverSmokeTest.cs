using System.Collections;
using NUnit.Framework;
using TMPro;
using Territory.UI.Tooltips;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Territory.Tests.PlayMode.UI.Tooltips
{
    /// <summary>
    /// Stage 9 (TECH-8548) — PlayMode smoke for the tooltip hover lifecycle.
    /// Asserts:
    ///   1. <see cref="TooltipController"/> spawns the tooltip prefab as a child of the canvas
    ///      after a synthetic <see cref="PointerEventData"/> enter on a probe carrying
    ///      <see cref="TooltipText"/>.
    ///   2. The spawned tooltip's <see cref="TMP_Text"/> body matches the probe's text.
    ///   3. Subsequent <see cref="PointerEventData"/> exit destroys the tooltip child.
    /// </summary>
    public sealed class TooltipHoverSmokeTest
    {
        private const string ScenePath = "Assets/Scenes/MainScene.unity";
        private const string ProbeText = "hover-me";

        private TooltipController _controller;
        private RectTransform _canvasRect;
        private GameObject _probe;
        private TooltipText _trigger;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            LogAssert.ignoreFailingMessages = true;

#if UNITY_EDITOR
            EditorSceneManager.LoadSceneInPlayMode(
                ScenePath,
                new LoadSceneParameters(LoadSceneMode.Single));
#endif
            // Allow scene Awake cascade.
            yield return null;
            yield return null;

            _controller = Object.FindObjectOfType<TooltipController>();
            var canvas = _controller != null ? _controller.GetComponentInParent<Canvas>() : null;
            _canvasRect = canvas != null ? canvas.GetComponent<RectTransform>() : null;

            // Build a probe element under the canvas with a TooltipText component.
            if (_canvasRect != null)
            {
                _probe = new GameObject("TooltipProbe");
                _probe.transform.SetParent(_canvasRect, false);
                _probe.AddComponent<RectTransform>();
                _probe.AddComponent<Image>();
                _trigger = _probe.AddComponent<TooltipText>();
                _trigger.SetText(ProbeText);
            }

            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_probe != null) Object.Destroy(_probe);
            yield return null;
        }

        [UnityTest]
        public IEnumerator HoverEnter_SpawnsTooltipChild_NonEmptyText()
        {
            Assert.IsNotNull(_controller, "TooltipController not found in scene — Stage 9 wiring incomplete.");
            Assert.IsNotNull(_canvasRect, "Parent canvas RectTransform not resolvable.");
            Assert.IsNotNull(_trigger, "TooltipText probe not built.");

            int before = _canvasRect.childCount;
            var enterEvent = new PointerEventData(EventSystem.current) { position = new Vector2(100f, 100f) };
            _trigger.OnPointerEnter(enterEvent);
            yield return null;

            int after = _canvasRect.childCount;
            Assert.Greater(after, before, "Canvas child count did not grow on PointerEnter — tooltip not spawned.");

            // Find the spawned tooltip child (last child added under canvas).
            Transform spawned = null;
            for (int i = _canvasRect.childCount - 1; i >= 0; i--)
            {
                var child = _canvasRect.GetChild(i);
                if (child == _probe.transform) continue;
                spawned = child;
                break;
            }
            Assert.IsNotNull(spawned, "Spawned tooltip transform not found under canvas.");

            var tmp = spawned.GetComponentInChildren<TMP_Text>(true);
            Assert.IsNotNull(tmp, "Spawned tooltip carries no TMP_Text — body label missing.");
            Assert.AreEqual(ProbeText, tmp.text, "Tooltip body text did not match probe text.");

            // Exit destroys.
            var exitEvent = new PointerEventData(EventSystem.current);
            _trigger.OnPointerExit(exitEvent);
            yield return null;
            Assert.IsTrue(spawned == null || spawned.gameObject == null,
                "Tooltip child not destroyed on PointerExit.");
        }
    }
}
