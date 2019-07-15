using ColossalFramework;
using ColossalFramework.UI;
using UnityEngine;

namespace BrokenNodeDetector {
    public class MainUI : UIComponent {
        public static string FILE_NAME { get; } = "BrokenNodeDetector";

        public MainPanel MainPanel { get; private set; }
        private SavedInputKey MainKey { get; set; }

        public MainUI() {
            var uiView = UIView.GetAView();

            MainPanel = (MainPanel) uiView.AddUIComponent(typeof(MainPanel));
            MainPanel.Initailize();
            MainKey = new SavedInputKey("BrokeNodeDetectorMenu", FILE_NAME, KeyCode.Alpha0, true, false, false, true);
        }

        public override void Update() {
            if (!MainPanel.isVisible && MainKey.IsPressed()) {
                MainPanel.Show();
            }
        }

        private void OnGUI() {
            if (!MainPanel.isVisible && MainKey.IsPressed()) {
                MainPanel.Show();
            }
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