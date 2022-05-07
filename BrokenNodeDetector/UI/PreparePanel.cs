using System;
using System.Collections;
using BrokenNodeDetector.UI.Tools;
using ColossalFramework.UI;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BrokenNodeDetector.UI {
    public class PreparePanel : UIPanel {
        private UIComponent _prepareView;
        private bool _preparing;
        private Coroutine _prepareCoroutine;

        public event Action<IDetector> OnPrepareFinished;
        public override void Awake() {
            base.Awake();
            width = 400;
            height = 150;
        }
        
        public void UseDetector(IDetector detector) {
            if (_prepareView) {
                Destroy(_prepareView.gameObject);
                _prepareView = null;
            }
            
            if (detector.UITemplatePrepare) {
                _prepareView = Object.Instantiate(detector.UITemplatePrepare);
                AttachUIComponent(_prepareView.gameObject);
                _prepareView.relativePosition = new Vector3(0, 0);
                detector.InitPrepareView(_prepareView);
                height = _prepareView.height + 40f;
                _prepareView.isVisible = true;
            }
            Prepare(detector);
        }

        public override void OnDestroy() {
            OnPrepareFinished = null;
            CancelPrepare();
            if (_prepareView) {
                Destroy(_prepareView.gameObject);
                _prepareView = null;
            }
            base.OnDestroy();
        }

        public void CancelPrepare() {
            if (_preparing && _prepareCoroutine != null) {
                StopCoroutine(_prepareCoroutine);
            }
        }
        
        private void Prepare(IDetector detector) {
            if (_preparing)
                return;
            _preparing = true;
            _prepareView.Find<UILabel>("Label").text = detector.Name;
            _prepareCoroutine = StartCoroutine(PrepareImpl(detector));
        }

        private IEnumerator PrepareImpl(IDetector detector) {
            string message = string.Empty;
            UILabel label = _prepareView.Find<UILabel>("Label");
            label.text = "Preparing...";
            foreach (var result in detector.Prepare()) {
                if (!message.Equals(detector.ProgressMessage))
                    label.text = detector.ProgressMessage;

                yield return detector.CustomYieldInstruction;
            }

            _preparing = false;
            _prepareCoroutine = null;
            yield return new WaitForSeconds(1);
            OnPrepareFinished?.Invoke(detector);
        }
    }
}