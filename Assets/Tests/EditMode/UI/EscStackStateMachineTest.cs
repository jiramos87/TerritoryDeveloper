// TECH-14102 / game-ui-catalog-bake Stage 8 §Red-Stage Proof.
//
// Drives UIManager.HandleEscapePress() against the permutation matrix
// defined in §Acceptance — confirms iterative escape state machine
// pops one frame per press in priority order:
//   SubTypePicker > ToolSelected > {modals} > {menus} > PauseMenu (fallback).

using NUnit.Framework;
using UnityEngine;
using Territory.UI;

namespace Territory.Tests.EditMode.UI
{
    public class EscStackStateMachineTest
    {
        private UIManager BuildHarness()
        {
            var go = new GameObject("UIManagerTestHarness");
            UIManager ui = go.AddComponent<UIManager>();
            return ui;
        }

        private void TearDown(UIManager ui)
        {
            if (ui != null) Object.DestroyImmediate(ui.gameObject);
        }

        [Test]
        public void Stack_StartsEmpty()
        {
            UIManager ui = BuildHarness();
            try { Assert.AreEqual(0, ui.PopupStackCount); }
            finally { TearDown(ui); }
        }

        [Test]
        public void RegisterToolSelected_Idempotent()
        {
            UIManager ui = BuildHarness();
            try
            {
                ui.RegisterToolSelected();
                ui.RegisterToolSelected();
                ui.RegisterToolSelected();
                Assert.AreEqual(1, ui.PopupStackCount, "ToolSelected push must be idempotent");
                Assert.AreEqual(PopupType.ToolSelected, ui.PopupStackPeek());
            }
            finally { TearDown(ui); }
        }

        [Test]
        public void ToolSelectedOnly_PopsToolFrame()
        {
            UIManager ui = BuildHarness();
            try
            {
                ui.RegisterToolSelected();
                Assert.AreEqual(1, ui.PopupStackCount);
                ui.HandleEscapePress();
                Assert.AreEqual(0, ui.PopupStackCount, "Esc on tool-only stack must clear ToolSelected frame");
            }
            finally { TearDown(ui); }
        }

        [Test]
        public void ModalThenTool_TopFramePoppedFirst()
        {
            // Push order: ToolSelected first, then InfoPanel modal — Esc closes modal first.
            UIManager ui = BuildHarness();
            try
            {
                ui.RegisterToolSelected();
                ui.RegisterPopupOpened(PopupType.InfoPanel);
                Assert.AreEqual(2, ui.PopupStackCount);
                Assert.AreEqual(PopupType.InfoPanel, ui.PopupStackPeek());
                ui.HandleEscapePress();
                Assert.AreEqual(PopupType.ToolSelected, ui.PopupStackPeek(), "ToolSelected must remain after modal pops");
                Assert.AreEqual(1, ui.PopupStackCount);
            }
            finally { TearDown(ui); }
        }

        [Test]
        public void ModalThenTool_SecondEscDeselectsTool()
        {
            UIManager ui = BuildHarness();
            try
            {
                ui.RegisterToolSelected();
                ui.RegisterPopupOpened(PopupType.InfoPanel);
                ui.HandleEscapePress(); // pops modal
                ui.HandleEscapePress(); // pops tool
                Assert.AreEqual(0, ui.PopupStackCount, "Second Esc must clear ToolSelected frame");
            }
            finally { TearDown(ui); }
        }

        [Test]
        public void SubtypePickerOnTop_PopsPickerOnly()
        {
            UIManager ui = BuildHarness();
            try
            {
                ui.RegisterToolSelected();
                ui.RegisterPopupOpened(PopupType.SubTypePicker);
                ui.HandleEscapePress();
                Assert.AreEqual(PopupType.ToolSelected, ui.PopupStackPeek(), "Subtype-picker pop must leave tool frame intact");
            }
            finally { TearDown(ui); }
        }

        [Test]
        public void MenuFrame_PopsMenu()
        {
            UIManager ui = BuildHarness();
            try
            {
                ui.RegisterPopupOpened(PopupType.LoadGame);
                Assert.AreEqual(1, ui.PopupStackCount);
                ui.HandleEscapePress();
                Assert.AreEqual(0, ui.PopupStackCount, "Esc on menu-only stack pops the menu");
            }
            finally { TearDown(ui); }
        }

        [Test]
        public void RemoveFrameFromStack_PreservesOrder()
        {
            UIManager ui = BuildHarness();
            try
            {
                ui.RegisterToolSelected();
                ui.RegisterPopupOpened(PopupType.InfoPanel);
                ui.RegisterPopupOpened(PopupType.SubTypePicker);
                Assert.AreEqual(3, ui.PopupStackCount);

                ui.RemoveFrameFromStack(PopupType.InfoPanel);
                Assert.AreEqual(2, ui.PopupStackCount);
                Assert.AreEqual(PopupType.SubTypePicker, ui.PopupStackPeek(), "Top frame must remain after middle removed");

                ui.HandleEscapePress(); // pops SubTypePicker
                Assert.AreEqual(PopupType.ToolSelected, ui.PopupStackPeek(), "ToolSelected must surface after picker pops");
            }
            finally { TearDown(ui); }
        }
    }
}
