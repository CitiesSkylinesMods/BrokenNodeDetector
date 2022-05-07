using System;
using System.Reflection;
using ColossalFramework;
using UnityEngine;

namespace BrokenNodeDetector {
    public class ModSettings: Singleton<ModSettings> {
        private const int DEFAULT_MENU_POS_X = 250; 
        private const int DEFAULT_MENU_POS_Y = 20; 
        private static string FILE_NAME { get; } = "BrokenNodeDetector";
        public SavedInputKey MainKey = new SavedInputKey("BrokeNodeDetectorMenu", FILE_NAME, KeyCode.Alpha0, true, false, false, true);

        public SavedInt MenuPosX = new SavedInt("BrokenNodeDetectorMenuPosX", FILE_NAME, DEFAULT_MENU_POS_X, true);
        public SavedInt MenuPosY = new SavedInt("BrokenNodeDetectorMenuPosY", FILE_NAME, DEFAULT_MENU_POS_Y, true);

        private static SettingsFile _settingsFile;
        static ModSettings() {
            TryCreateModConfig();
        }

        internal void ResetMenuPosition() {
            MenuPosX.value = DEFAULT_MENU_POS_X;
            MenuPosY.value = DEFAULT_MENU_POS_Y;

            if (LoadingExtension.MainUi && LoadingExtension.MainUi.MainPanel) {
                LoadingExtension.MainUi.MainPanel.ForceUpdateMenuPosition();
            }
        }

        private static void TryCreateModConfig() {
            if (GameSettings.FindSettingsFileByName(FILE_NAME) == null)
                GameSettings.AddSettingsFile(new SettingsFile {fileName = FILE_NAME});

            _settingsFile = GameSettings.FindSettingsFileByName(FILE_NAME);
        }

        private void OnDestroy() {
            GetType().GetField("sInstance", BindingFlags.NonPublic | BindingFlags.Static)
                ?.SetValue(null, null);
        }
    }
}