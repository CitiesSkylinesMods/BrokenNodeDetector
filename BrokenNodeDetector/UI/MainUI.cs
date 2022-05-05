using System.Reflection;
using ColossalFramework;
using ColossalFramework.UI;
using UnityEngine;

namespace BrokenNodeDetector.UI {
    public class MainUI : UIComponent {
        public MainPanel MainPanel { get; private set; }
        private SavedInputKey MainKey { get; set; }

        private float _lastKeybindHitTime = 0;
        private float _hitInterval = 0.25f;

        private BndResultHighlightManager _highlightManager;

        public override void Awake() {
            base.Awake();
            var uiView = UIView.GetAView();
            MainPanel = (MainPanel) uiView.AddUIComponent(typeof(MainPanel));
            MainPanel.Initialize();
            MainKey = ModSettings.instance.MainKey;
            #if !DEBUG
            BndResultHighlightManager.Ensure();
            _highlightManager = BndResultHighlightManager.instance;
            RenderManager.RegisterRenderableManager(_highlightManager);
            #endif
        }

        public override void Update() {
            if (!MainKey.IsPressed() || ModService.Instance.KeybindEditInProgress || Time.time - _lastKeybindHitTime < _hitInterval) return;
            
            // if (!MainPanel.isVisible)
            //     MainPanel.Show();
            // else
            //     MainPanel.Hide();
            if (!MainPanel.isVisible)
                MainPanel.Show();
            else
                MainPanel.Hide();
            _lastKeybindHitTime = Time.time;
        }

        public override void OnDestroy() {
            base.OnDestroy();
#if !DEBUG
            RemoveFromRenderables();
#endif
            if (MainPanel != null) {
                Destroy(MainPanel.gameObject);
                MainPanel = null;
                MainKey = null;
            }
        }

        private void RemoveFromRenderables() {
            RenderManager.GetManagers(out IRenderableManager[] _, out int count);
            FieldInfo fieldInfo = typeof(RenderManager).GetField("m_renderables", BindingFlags.Static | BindingFlags.NonPublic);
            FastList<IRenderableManager> value = (FastList<IRenderableManager>)fieldInfo.GetValue(null);
            value.Remove(_highlightManager);
            RenderManager.GetManagers(out IRenderableManager[] _, out int count2);
            Destroy(_highlightManager.gameObject);
            _highlightManager = null;
        }
    }
}