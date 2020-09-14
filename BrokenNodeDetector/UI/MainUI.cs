using ColossalFramework;
using ColossalFramework.UI;
using UnityEngine;

namespace BrokenNodeDetector.UI {
    public class MainUI : UIComponent {
        public MainPanel MainPanel { get; private set; }
        private SavedInputKey MainKey { get; set; }

        private float _lastKeybindHitTime = 0;
        private float _hitInterval = 0.25f;

        public override void Awake() {
            base.Awake();
            var uiView = UIView.GetAView();
            MainPanel = (MainPanel) uiView.AddUIComponent(typeof(MainPanel));
            MainPanel.Initialize();
            MainKey = Keybinds.instance.MainKey;
        }

        public override void Update() {
            if (!MainKey.IsPressed() || ModService.Instance.KeybindEditInProgress || Time.time - _lastKeybindHitTime < _hitInterval) return;
            
            if (!MainPanel.isVisible)
                MainPanel.Show();
            else
                MainPanel.Hide();
            _lastKeybindHitTime = Time.time;
        }

        public override void OnDestroy() {
            base.OnDestroy();
            if (MainPanel != null) {
                Destroy(MainPanel);
                MainPanel = null;
                MainKey = null;
            }
        }
    }
}