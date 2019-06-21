using System;
using System.Collections.Generic;
using System.Reflection;
using ColossalFramework;
using ColossalFramework.UI;
using ICities;
using OtherCSMods.RedirectionFramework;
using RedirectionFramework;
using UnityEngine;

namespace BrokenNodeDetector {
    public class LoadingExtension : LoadingExtensionBase {
        public bool DetourInited { get; private set; }
        public static MainUI MainUi { get; private set; }
        public static IDictionary<MethodInfo, RedirectCallsState> DetouredMethodStates { get; private set; } = new Dictionary<MethodInfo, RedirectCallsState>();
        
        public override void OnCreated(ILoading loading) {
            base.OnCreated(loading);
            Debug.Log("OnCreated");
        }

        public override void OnLevelLoaded(LoadMode mode) {
            Debug.Log("OnLevelLoaded Mode: " + mode);
            base.OnLevelLoaded(mode);

            if (mode == LoadMode.NewGame
                || mode == LoadMode.LoadGame
                || mode == LoadMode.NewGameFromScenario) {
                InitDetours();
            }

            if (MainUi == null) {
                MainUi = (MainUI) UIView.GetAView().AddUIComponent(typeof(MainUI));
            }
        }

        public override void OnLevelUnloading() {
            Debug.Log("OnLevelUnloading");
            base.OnLevelUnloading();

            RevertDetours();
        }

        private void InitDetours() {
            if (DetourInited) return;

            bool detourFailed = false;
            try {
                Debug.Log("BND: Deploying manual detours");

                DetouredMethodStates = AssemblyRedirector.Deploy();
            } catch (Exception e) {
                Debug.LogError("Could not deploy manual detours for Broken Nodes Detector");
                Debug.Log(e.ToString());
                Debug.Log(e.StackTrace);
                detourFailed = true;
            }

            if (detourFailed) {
                Singleton<SimulationManager>.instance.m_ThreadingWrapper.QueueMainThread(() => {
                    UIView.library.ShowModal<ExceptionPanel>("ExceptionPanel").SetMessage("Broken Nodes Detector failed to load", "Broken Nodes Detector failed to load. You can continue playing but the mod may not work properly.", true);
                });
            } else {
                Debug.Log("Detours successful");
            }

            DetourInited = true;
        }

        private void RevertDetours() {
            if (!DetourInited) return;

            AssemblyRedirector.Revert();

            DetourInited = false;
        }

    }
}