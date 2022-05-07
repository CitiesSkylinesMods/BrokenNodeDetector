using BrokenNodeDetector.UI.Tools;
using ColossalFramework.UI;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BrokenNodeDetector.UI {
    public class ResultsPanel : UIPanel {
        public UILabel _label;

        private UIComponent _detectorResultsView;

        public override void Awake() {
            base.Awake();
            width = 400;
            height = 150;
            _label = AddUIComponent<UILabel>();
            _label.text = string.Empty;
            _label.relativePosition = new Vector3(15, 5);
            _label.textScale = 1.1f;
        }
        
        public void UseDetector(IDetector detector) {
            _label.text = $"{detector.Name}";
            if (_detectorResultsView) {
                Destroy(_detectorResultsView.gameObject);
                _detectorResultsView = null;
            }
            
            if (detector.UITemplateResults) {
                _detectorResultsView = Object.Instantiate(detector.UITemplateResults);
                AttachUIComponent(_detectorResultsView.gameObject);
                _detectorResultsView.relativePosition = new Vector3(0, 30);
                detector.InitResultsView(_detectorResultsView);
                height = _detectorResultsView.height + 40f;
                _detectorResultsView.isVisible = true;
            }
        }

        public override void OnDestroy() {
            base.OnDestroy();
            _label = null;
            if (_detectorResultsView) {
                Destroy(_detectorResultsView.gameObject);
                _detectorResultsView = null;
            }
        }
    }
}