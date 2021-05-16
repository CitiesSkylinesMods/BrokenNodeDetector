using System;
using ColossalFramework;
using ColossalFramework.UI;
using ICities;
using UnityEngine;


namespace BrokenNodeDetector.UI {
    /// <summary>
    /// Settings UI, code borrowed from TM:PE mod
    /// </summary>
    public class SettingsUI {
        private const float ROW_WIDTH = 744f - 15f;
        private const float ROW_HEIGHT = 34f;
        
        private SavedInputKey _currentlyEditingBinding;
        
        public void BuildUI(UIHelper helper) {
            UIHelperBase group = helper.AddGroup("Shortcuts:");
            UIPanel panel = CreateRowPanel((UIPanel) ((UIHelper) group).self);

            CreateLabel(panel, "Open Mod Menu", 0.6f);
            CreateKeybindButton(panel, ModSettings.instance.MainKey, 0.3f);
            UIHelperBase group2 = helper.AddGroup("Other");
            UIPanel panel2 = CreateRowPanel((UIPanel) ((UIHelper) group2).self);
            CreateResetMenuPosition(panel2);
            
        }

        public void CreateResetMenuPosition(UIPanel parent) {
            var btn = parent.AddUIComponent<UIButton>();
            btn.size = new Vector2(ROW_WIDTH * 0.3f, ROW_HEIGHT);
            btn.text = "Reset Menu Position";
            btn.hoveredTextColor = new Color32(128, 128, 255, 255);
            btn.pressedTextColor = new Color32(192, 192, 255, 255);
            btn.normalBgSprite = "ButtonMenu";
            btn.eventClick += OnResetClicked;
        }

        private void OnResetClicked(UIComponent component, UIMouseEventParameter eventparam) {
            ModSettings.instance.ResetMenuPosition();
        }

        public UIPanel CreateRowPanel(UIComponent currentGroup) {
            var rowPanel = currentGroup.AddUIComponent<UIPanel>();
            rowPanel.size = new Vector2(ROW_WIDTH, ROW_HEIGHT);
            rowPanel.autoLayoutStart = LayoutStart.TopLeft;
            rowPanel.autoLayoutDirection = LayoutDirection.Horizontal;
            rowPanel.autoLayout = true;

            return rowPanel;
        }

        public UILabel CreateLabel(UIPanel parent, string text, float widthFraction) {
            var label = parent.AddUIComponent<UILabel>();
            label.wordWrap = true;
            label.autoSize = false;
            label.size = new Vector2(ROW_WIDTH * widthFraction, ROW_HEIGHT);
            label.text = text;
            label.verticalAlignment = UIVerticalAlignment.Middle;
            label.textAlignment = UIHorizontalAlignment.Left;
            return label;
        }

        public void CreateKeybindButton(
            UIPanel parent,
            SavedInputKey editKey,
            float widthFraction) {
            var btn = parent.AddUIComponent<UIButton>();
            btn.size = new Vector2(ROW_WIDTH * widthFraction, ROW_HEIGHT);
            btn.text = ToLocalizedString(editKey);
            btn.hoveredTextColor = new Color32(128, 128, 255, 255);
            btn.pressedTextColor = new Color32(192, 192, 255, 255);
            btn.normalBgSprite = "ButtonMenu";

            btn.eventKeyDown += OnBindingKeyDown;
            btn.eventMouseDown += OnBindingMouseDown;
            btn.objectUserData = editKey;

            AddXButton(parent, editKey, btn);
        }

        private void OnBindingMouseDown(UIComponent component, UIMouseEventParameter evParam) {
            var editable = (SavedInputKey) evParam.source.objectUserData;
            var keybindButton = evParam.source as UIButton;

            // This will only work if the user is not in the process of changing the shortcut
            if (_currentlyEditingBinding == null) {
                evParam.Use();
                StartKeybindEditMode(editable, keybindButton);
            } else if (!IsUnbindableMouseButton(evParam.buttons)) {
                // This will work if the user clicks while the shortcut change is in progress
                evParam.Use();
                var editedBinding = _currentlyEditingBinding; // will be nulled by closing modal
                UIView.PopModal();

                var inputKey = SavedInputKey.Encode(ButtonToKeycode(evParam.buttons),
                                                    IsControlDown(),
                                                    IsShiftDown(),
                                                    IsAltDown());
                editedBinding.value = inputKey;

                keybindButton.buttonsMask = UIMouseButton.Left;
                keybindButton.text = ToLocalizedString(editedBinding);
                _currentlyEditingBinding = null;
                ModService.Instance.FinishKeybindEdit();
            }
        }

        private void OnBindingKeyDown(UIComponent component, UIKeyEventParameter evParam) {
            try {
                if (IsModifierKey(evParam.keycode)) {
                    return;
                }

                evParam.Use(); 
                var editedBinding = _currentlyEditingBinding;
                UIView.PopModal();

                var keybindButton = evParam.source as UIButton;
                var inputKey = SavedInputKey.Encode(evParam.keycode, evParam.control, evParam.shift, evParam.alt);

                if (evParam.keycode != KeyCode.Escape) {
                    editedBinding.value = inputKey;
                }

                keybindButton.text = ToLocalizedString(editedBinding);
                _currentlyEditingBinding = null;
                ModService.Instance.FinishKeybindEdit();
            } catch (Exception e) {
                Debug.LogError($"{e}");
            }
        }

        private static void AddXButton(UIPanel parent,
            SavedInputKey editKey,
            UIButton alignTo) {
            UIButton btnX = parent.AddUIComponent<UIButton>();
            btnX.autoSize = false;
            btnX.size = new Vector2(ROW_HEIGHT, ROW_HEIGHT);
            btnX.normalBgSprite = "buttonclose";
            btnX.hoveredBgSprite = "buttonclosehover";
            btnX.pressedBgSprite = "buttonclosepressed";
            btnX.eventClicked += (component, eventParam) => {
                editKey.value = SavedInputKey.Empty;
                alignTo.text = ToLocalizedString(editKey);
            };
        }

        private void StartKeybindEditMode(SavedInputKey editable, UIButton keybindButton) {
            _currentlyEditingBinding = editable;
            ModService.Instance.StartKeybindEdit();

            keybindButton.buttonsMask =
                UIMouseButton.Left | UIMouseButton.Right | UIMouseButton.Middle |
                UIMouseButton.Special0 | UIMouseButton.Special1 | UIMouseButton.Special2 |
                UIMouseButton.Special3;
            keybindButton.text = "Press key (or Esc)";
            keybindButton.Focus();
            UIView.PushModal(keybindButton, OnKeybindModalPopped);
        }

        private void OnKeybindModalPopped(UIComponent component) {
            var keybindButton = component as UIButton;
            if (keybindButton != null && _currentlyEditingBinding != null) {
                keybindButton.text = ToLocalizedString(_currentlyEditingBinding);
                _currentlyEditingBinding = null;
                ModService.Instance.FinishKeybindEdit();
            }
        }

        public static string ToLocalizedString(SavedInputKey k) {
            if (k.value == SavedInputKey.Empty) {
                return "None";
            }

            return k.ToLocalizedString("KEYNAME");
        }

        public static bool IsModifierKey(KeyCode code) {
            return code == KeyCode.LeftControl || code == KeyCode.RightControl ||
                   code == KeyCode.LeftShift || code == KeyCode.RightShift ||
                   code == KeyCode.LeftAlt || code == KeyCode.RightAlt;
        }

        public static bool IsControlDown() {
            return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        }

        public static bool IsShiftDown() {
            return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        }

        public static bool IsAltDown() {
            return Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
        }

        public static bool IsUnbindableMouseButton(UIMouseButton code) {
            return code == UIMouseButton.Left || code == UIMouseButton.Right;
        }

        public static KeyCode ButtonToKeycode(UIMouseButton button) {
            if (button == UIMouseButton.Left) {
                return KeyCode.Mouse0;
            }

            if (button == UIMouseButton.Right) {
                return KeyCode.Mouse1;
            }

            if (button == UIMouseButton.Middle) {
                return KeyCode.Mouse2;
            }

            if (button == UIMouseButton.Special0) {
                return KeyCode.Mouse3;
            }

            if (button == UIMouseButton.Special1) {
                return KeyCode.Mouse4;
            }

            if (button == UIMouseButton.Special2) {
                return KeyCode.Mouse5;
            }

            if (button == UIMouseButton.Special3) {
                return KeyCode.Mouse6;
            }

            return KeyCode.None;
        }
    }
}