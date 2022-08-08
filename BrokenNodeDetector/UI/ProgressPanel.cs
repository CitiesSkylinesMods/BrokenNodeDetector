using System;
using System.Collections;
using BrokenNodeDetector.UI.Tools;
using BrokenNodeDetector.UI.Tools.Utils;
using ColossalFramework.UI;
using UnityEngine;

namespace BrokenNodeDetector.UI {
    public class ProgressPanel : UIPanel {
        private UIPanel _mainPanel;
        private UIProgressBar _progressBar;
        private UILabel _progressLabel;
        private UILabel _titleLabel;
        private bool _processing;

        public event Action<IDetector> OnProcessFinished;

        public override void Awake() {
            base.Awake();
            autoLayout = false;
            width = 400;
            height = 100;
            _mainPanel = AddUIComponent<UIPanel>();
            _mainPanel.autoLayout = false;
            _mainPanel.relativePosition = Vector3.zero;
            _mainPanel.size = new Vector2(400, 100);

            _titleLabel = _mainPanel.AddUIComponent<UILabel>();
            _titleLabel.padding = new RectOffset(15, 10, 5, 10);
            _titleLabel.relativePosition = new Vector2(0, 0);
            _titleLabel.text = string.Empty;

            _progressBar = _mainPanel.AddUIComponent<UIProgressBar>();
            _progressBar.relativePosition = new Vector3(15, 35);
            _progressBar.width = 370;
            _progressBar.height = 25;
            _progressBar.fillMode = UIFillMode.Fill;
            _progressBar.progressSprite = "ProgressBarFill";
            _progressBar.isVisible = true;

            _progressLabel = _mainPanel.AddUIComponent<UILabel>();
            _progressLabel.textScale = 0.8f;
            _progressLabel.padding = new RectOffset(15, 10, 10, 15);
            _progressLabel.relativePosition = new Vector2(0, 55);
            _progressLabel.text = "Processing...";
        }

        public override void OnDestroy() {
            base.OnDestroy();
            OnProcessFinished = null;
            _mainPanel = null;
            _progressBar = null;
            _progressLabel = null;
            _titleLabel = null;
        }

        public void UseDetector(IDetector detector) {
            StartProcessing(detector);
        }

        public void StartProcessing(IDetector detector) {
            if (_processing)
                return;
            _processing = true;
            _titleLabel.text = detector.Name;
            this.StartExceptionHandledIterator(ProcessingImpl(detector), UnityExtensions.DefaultExceptionHandler);
        }

        private IEnumerator ProcessingImpl(IDetector detector) {
            _progressBar.minValue = 0f;
            _progressBar.maxValue = 1.0f;
            string message = string.Empty;
            _progressLabel.text = "Processing...";
            foreach (var progress in detector.Process()) {
                _progressBar.value = progress;
                if (!message.Equals(detector.ProgressMessage))
                    _progressLabel.text = detector.ProgressMessage;

                yield return detector.CustomYieldInstruction;
            }

            _progressLabel.text = "Processing done";
            _progressBar.value = 1.0f;
            _processing = false;
            yield return new WaitForSeconds(1);
            OnProcessFinished?.Invoke(detector);
        }
    }
}