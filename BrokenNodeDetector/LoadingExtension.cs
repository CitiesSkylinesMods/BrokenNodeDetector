using System;
using BrokenNodeDetector.Patch._NetNode;
using BrokenNodeDetector.UI;
using CitiesHarmony.API;
using ColossalFramework;
using ColossalFramework.UI;
using HarmonyLib;
using ICities;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BrokenNodeDetector {
    public class LoadingExtension : LoadingExtensionBase {
        public bool DetourInited { get; private set; }
        public static MainUI MainUi { get; private set; }

        private bool _created;

        public override void OnCreated(ILoading loading) {
            base.OnCreated(loading);
            if (LoadingManager.instance.m_loadingComplete &&
                loading.currentMode == AppMode.Game &&
                !_created) {
                InitDetours();

                if (!MainUi) {
                    MainUi = (MainUI)UIView.GetAView().AddUIComponent(typeof(MainUI));
                }
            }

            _created = true;
        }

        public override void OnReleased() {
            base.OnReleased();
            RevertDetours();
            if (MainUi) {
                Object.Destroy(MainUi);
                MainUi = null;
            }

            _created = false;
        }

        public override void OnLevelLoaded(LoadMode mode) {
            base.OnLevelLoaded(mode);

            if (mode == LoadMode.NewGame
                || mode == LoadMode.LoadGame
                || mode == LoadMode.NewGameFromScenario) {
                InitDetours();
            }

            if (!MainUi) {
                MainUi = (MainUI)UIView.GetAView().AddUIComponent(typeof(MainUI));
            }
        }

        public override void OnLevelUnloading() {
            base.OnLevelUnloading();

            RevertDetours();
            if (MainUi) {
                Object.Destroy(MainUi);
                MainUi = null;
            }
        }

        private void InitDetours() {
            if (DetourInited || !HarmonyHelper.IsHarmonyInstalled) return;

            bool detourFailed = false;
            try {
                Debug.Log("[BND] Deploying manual detours");
                Patcher.PatchAll();
            } catch (Exception e) {
                Debug.LogError("[BND] Could not deploy manual detours for Broken Nodes Detector");
                Debug.Log(e.ToString());
                Debug.Log(e.StackTrace);
                detourFailed = true;
            }

            if (detourFailed) {
                Singleton<SimulationManager>.instance.m_ThreadingWrapper.QueueMainThread(() => {
                    UIView.library.ShowModal<ExceptionPanel>("ExceptionPanel").SetMessage("Broken Nodes Detector failed to load", "Broken Nodes Detector failed to load. You can continue playing but the mod may not work properly.", true);
                });
            } else {
                Debug.Log("[BND] Detours successful");
            }

            DetourInited = true;
        }

        private void RevertDetours() {
            if (!DetourInited) return;

            Patcher.UnpatchAll();
            DetourInited = false;
        }

        internal static class Patcher {
            private const string HarmonyId = "krzychu124.broken-node-detector";
            private static bool patched = false;

            public static void PatchAll() {
                if (patched) return;

                patched = true;
                var harmony = new Harmony(HarmonyId);
                harmony.Patch(typeof(NetNode).GetMethod(nameof(NetNode.UpdateLaneConnection)),
                    postfix: new HarmonyMethod(typeof(CustomNetNode), nameof(CustomNetNode.Postfix)));
            }

            public static void UnpatchAll() {
                if (!patched) return;

                var harmony = new Harmony(HarmonyId);
                harmony.UnpatchAll(HarmonyId);
                patched = false;
            }
        }
    }
}