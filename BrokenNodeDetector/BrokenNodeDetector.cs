using ICities;
using BrokenNodeDetector.UI;
using CitiesHarmony.API;
using UnityEngine;

namespace BrokenNodeDetector
{
    public class BrokenNodeDetector : IUserMod {
        public static readonly string Version = "0.5";

        public string Name => "Broken Node Detector " + Version;

        public string Description => "Search for broken nodes when TM:PE vehicles despawn.";

        public void OnEnabled() {
            Debug.Log($"[BND] Broken Node Detector enabled. Version {Version}");
            HarmonyHelper.EnsureHarmonyInstalled();
            Keybinds.Ensure();
#if DEBUG
            LoadingExtension.Patcher.PatchAll();
#endif
        }

        public void OnDisabled() {
            Debug.Log("[BND] Broken Node Detector disabled.");
#if DEBUG
            LoadingExtension.Patcher.UnpatchAll();
#endif
        }

        public void OnSettingsUI(UIHelper helper) {
            new SettingsUI().BuildUI(helper);
        }
    }
}
