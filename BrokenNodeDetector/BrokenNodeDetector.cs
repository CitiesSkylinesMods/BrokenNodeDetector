using ICities;
using BrokenNodeDetector.UI;
using CitiesHarmony.API;
using ColossalFramework.UI;
using JetBrains.Annotations;
using UnityEngine;

namespace BrokenNodeDetector
{
    public class BrokenNodeDetector : IUserMod {
        public static readonly string Version = "0.7.2";

        public string Name => "Broken Node Detector " + Version;

        public string Description => "Search for broken nodes when TM:PE vehicles despawn and more.";
#if TEST_UI
        private MainUI _testUI;
#endif

        [UsedImplicitly]
        public void OnEnabled() {
            Debug.Log($"[BND] Broken Node Detector enabled. Version {Version}");
            HarmonyHelper.EnsureHarmonyInstalled();
            ModSettings.Ensure();
            
#if TEST_UI
            if (UIView.GetAView()) {
                TestUI();
            } else {
                LoadingManager.instance.m_introLoaded += TestUI;
            }
#endif
#if DEBUG
            LoadingExtension.MainUi = (MainUI)UIView.GetAView().AddUIComponent(typeof(MainUI));
            LoadingExtension.Patcher.PatchAll();
#endif
        }

        [UsedImplicitly]
        public void OnDisabled() {
            Debug.Log("[BND] Broken Node Detector disabled.");
            if (LoadingExtension.MainUi) {
                Object.Destroy(LoadingExtension.MainUi.gameObject);
            }

            if (ModSettings.exists && ModSettings.instance) {
                Object.Destroy(ModSettings.instance.gameObject);
            }
#if TEST_UI
            if (_testUI) {
                Object.Destroy(_testUI.gameObject);
                _testUI = null;
            }
#endif
#if DEBUG
            LoadingManager.instance.m_introLoaded -= TestUI;
            LoadingExtension.Patcher.UnpatchAll();
#endif
        }

        public void OnSettingsUI(UIHelper helper) {
            new SettingsUI().BuildUI(helper);
        }
        
#if TEST_UI
        private void TestUI() {
            _testUI = (MainUI) UIView.GetAView().AddUIComponent(typeof(MainUI));
        }
#endif
    }
}
