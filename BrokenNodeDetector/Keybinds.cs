using ColossalFramework;
using UnityEngine;

namespace BrokenNodeDetector {
    public class Keybinds: Singleton<Keybinds> {
        private static string FILE_NAME { get; } = "BrokenNodeDetector";
        public SavedInputKey MainKey = new SavedInputKey("BrokeNodeDetectorMenu", FILE_NAME, KeyCode.Alpha0, true, false, false, true);

        static Keybinds() {
            TryCreateModConfig();
        }

        private static void TryCreateModConfig() {
            if (GameSettings.FindSettingsFileByName(FILE_NAME) == null)
                GameSettings.AddSettingsFile(new SettingsFile {fileName = FILE_NAME});
        }
    }
}