using System;
using ColossalFramework.UI;
using UnityEngine;

namespace BrokenNodeDetector.UI.Tools {
    public static class UIHelpers {
        
        public static UIButton CreateButton(UIComponent parent, string text, Rect relativePosSize, MouseEventHandler clickHandler) {
            UIButton button = parent.AddUIComponent<UIButton>();
            button.eventClick += clickHandler;
            button.relativePosition = relativePosSize.position;
            button.width = relativePosSize.width;
            button.height = relativePosSize.height;
            button.text = text;
            AssignButtonSprites(button);
            return button;
        }
        
        private static void AssignButtonSprites(UIButton button) {
            button.normalBgSprite = "ButtonMenu";
            button.hoveredBgSprite = "ButtonMenuHovered";
            button.pressedBgSprite = "ButtonMenuPressed";
            button.disabledBgSprite = "ButtonMenuDisabled";
        }
        
        public static void ShowConfirmDialog(string title, string text, Action onConfirm) {
            ShowConfirmDialog(title, text, onConfirm, () => { });
        }

        public static void ShowConfirmDialog(string title, string text, Action onConfirm, Action onCancel) {
            ConfirmPanel.ShowModal(title, text, (_, result) => {
                if (result != 1) {
                    onCancel();
                } else {
                    onConfirm();
                }
            });
        }
    }
}