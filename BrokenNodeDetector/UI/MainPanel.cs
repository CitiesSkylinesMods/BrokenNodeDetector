using System;
using BrokenNodeDetector.UI.Tools;
using ColossalFramework;
using ColossalFramework.UI;
using UnityEngine;

namespace BrokenNodeDetector.UI {
    public class MainPanel : UIPanel {
        private const int PANEL_WIDTH = 400;
        private const int PANEL_HEIGHT = 320;
        private UIButton _closeButton;
        private DetectorFactory _detectorFactory;
        private UIPanel _detectorsPanel;
        private UIDragHandle _dragHandle;
        private ProgressPanel _progressPanel;
        private ResultsPanel _resultsPanel;
        private UIButton _returnButton;
        private UILabel _title;

        public override void Awake() {
            base.Awake();
            autoLayout = false;
            width = PANEL_WIDTH;
            height = PANEL_HEIGHT;
            backgroundSprite = "UnlockingPanel2";
            color = new Color32(67, 67, 158, 255);
            _detectorFactory = new DetectorFactory();
            CreateTitle();
            CreateDragHandle();
            CreateReturnButton();
            CreateCloseButton();
            CreateDetectorsPanel();
            CreateProgressPanel();
            CreateResultsPanel();
            AddDetectors();
            isVisible = false;
            eventVisibilityChanged += OnChangedVisibility;
        }

        public override void OnDestroy() {
            base.OnDestroy();
            _detectorFactory.Dispose();
            _detectorFactory = null;
            eventVisibilityChanged -= OnChangedVisibility;
            OnClose(false);
        }

        private void OnChangedVisibility(UIComponent component, bool value) {
            RunFadeInOrOutAnimation(value);
        }

        protected override void OnPositionChanged() {
            base.OnPositionChanged();

            bool posChanged = ModSettings.instance.MenuPosX != (int)absolutePosition.x
                              || ModSettings.instance.MenuPosY != (int)absolutePosition.y;
            if (posChanged) {
                ModSettings.instance.MenuPosX.value = (int)absolutePosition.x;
                ModSettings.instance.MenuPosY.value = (int)absolutePosition.y;
            }
        }
        
        internal void ForceUpdateMenuPosition() {
            absolutePosition = new Vector3(ModSettings.instance.MenuPosX, ModSettings.instance.MenuPosY);
        }
        
        public void Initialize() {
            absolutePosition = new Vector3(ModSettings.instance.MenuPosX, ModSettings.instance.MenuPosY);
        }

        private void CreateDragHandle() {
            _dragHandle = AddUIComponent<UIDragHandle>();
            _dragHandle.area = new Vector4(0, 0, 360, 45);
        }

        private void CreateTitle() {
            _title = AddUIComponent<UILabel>();
            _title.autoSize = true;
            _title.textScale = 1.2f;
            _title.padding = new RectOffset(0, 10, 5, 15);
            _title.relativePosition = new Vector2(60, 8);
            _title.text = $"Broken node detector v{BrokenNodeDetector.Version}";
        }

        private void CreateCloseButton() {
            _closeButton = AddUIComponent<UIButton>();
            _closeButton.eventClick += CloseButtonClick;
            _closeButton.relativePosition = new Vector3(width - _closeButton.width - 35, 5f);
            _closeButton.normalBgSprite = "buttonclose";
            _closeButton.hoveredBgSprite = "buttonclosehover";
            _closeButton.pressedBgSprite = "buttonclosepressed";
        }

        private void CreateReturnButton() {
            _returnButton = AddUIComponent<UIButton>();
            _returnButton.eventClick += ReturnButtonClick;
            _returnButton.relativePosition = new Vector3(5, 8f);
            _returnButton.normalBgSprite = "ArrowLeft";
            _returnButton.hoveredBgSprite = "ArrowLeftHover";
            _returnButton.pressedBgSprite = "ArrowLeftPressed";
            _returnButton.size = new Vector2(30, 30);
            _returnButton.Hide();
        }

        private void CreateDetectorsPanel() {
            _detectorsPanel = AddUIComponent<UIPanel>();
            _detectorsPanel.autoLayout = false;
            _detectorsPanel.relativePosition = new Vector3(0, _title.height + 10);
            _detectorsPanel.width = 400;
            _detectorsPanel.height = 255;
        }

        private void CreateProgressPanel() {
            _progressPanel = AddUIComponent<ProgressPanel>();
            _progressPanel.relativePosition = new Vector3(0, _title.height + 10);
            _progressPanel.Hide();
            _progressPanel.OnProcessFinished += OnDetectionFinished;
        }

        private void CreateResultsPanel() {
            _resultsPanel = AddUIComponent<ResultsPanel>();
            _resultsPanel.relativePosition = new Vector3(0, _title.height + 10);
            _resultsPanel.Hide();
        }

        private void AddDetectors() {
            float buttonWidth = width - 20;
            float buttonHeight = 30;
            float buttonTopMargin = 5;

            float currentTop = 10;
            for (int i = 0; i < _detectorFactory.Detectors.Count; i++) {
                Detector detector = _detectorFactory.Detectors[i];
                CreateDetectorButton(_detectorsPanel, detector, buttonWidth, buttonHeight, currentTop);
                currentTop = currentTop + buttonHeight + buttonTopMargin;
            }
        }

        private void CreateDetectorButton(UIPanel parentPanel, Detector detector, float buttonWidth, float buttonHeight, float topPos) {
            UIButton button = parentPanel.AddUIComponent<UIButton>();
            button.objectUserData = detector;
            button.eventClick += OnDetectorButtonClick;
            button.relativePosition = new Vector3(10, topPos, 0);
            button.width = buttonWidth;
            button.height = buttonHeight;
            button.text = detector.Name;
            button.tooltip = detector.Tooltip;
            AssignButtonSprites(button);
        }

        private void OnDetectorButtonClick(UIComponent component, UIMouseEventParameter param) {
            if (component.objectUserData is IDetector detector) {
                RunFadeInOutAnimations(_detectorsPanel, _progressPanel, () => {
                    height = 145;
                    _progressPanel.UseDetector(detector);
                });
            }
        }

        private void OnDetectionFinished(IDetector detector) {
            if (detector.ShowResultsPanel) {
                _returnButton.Show();
                RunFadeInOutAnimations(_progressPanel, _resultsPanel, () => {
                    _resultsPanel.UseDetector(detector);
                    height = _resultsPanel.height + 50f /*title bar*/;
                });
            } else {
                RunFadeInOutAnimations(_progressPanel, _detectorsPanel, () => { height = PANEL_HEIGHT; });
            }
        }

        private void OnResultsClose(bool updateHeight = false) {
            _returnButton.Hide();
            RunFadeInOutAnimations(_resultsPanel, _detectorsPanel);
            BndResultHighlightManager.instance.enabled = false;
            if (updateHeight) {
                height = PANEL_HEIGHT;
            }
        }

        private void RunFadeInOutAnimations(UIPanel hidePanel, UIPanel showPanel, Action showAction = null) {
            ValueAnimator.Animate("fade_out_",
                (f) => hidePanel.opacity = f,
                new AnimatedFloat(1f, 0.0f, 0.2f, EasingType.SineEaseOut),
                () => {
                    hidePanel.Hide();
                    showPanel.opacity = 0f;
                    showAction?.Invoke();
                    showPanel.Show();
                    ValueAnimator.Animate("fade_in_",
                        (f) => showPanel.opacity = f,
                        new AnimatedFloat(0.0f, 1f, 0.25f, EasingType.SineEaseOut));
                });
        }

        private void RunFadeInOrOutAnimation(bool value, Action action = null) {
            if (!this || m_IsDisposing) return; //destroying component

            if (value) {
                _detectorsPanel.opacity = 1.0f;
                height = PANEL_HEIGHT;
            }

            ValueAnimator.Animate("fade_in_out",
                (f) => opacity = f,
                new AnimatedFloat(value ? 0f : 1f, value ? 1f : 0.0f, 0.2f, EasingType.SineEaseOut),
                () => action?.Invoke());
        }

        private static void AssignButtonSprites(UIButton button) {
            button.normalBgSprite = "ButtonMenu";
            button.hoveredBgSprite = "ButtonMenuHovered";
            button.pressedBgSprite = "ButtonMenuPressed";
            button.disabledBgSprite = "ButtonMenuDisabled";
        }

        private void CloseButtonClick(UIComponent component, UIMouseEventParameter eventparam) {
            eventparam.Use();
            OnClose();
        }

        private void ReturnButtonClick(UIComponent component, UIMouseEventParameter eventparam) {
            eventparam.Use();
            OnResultsClose(true);
        }

        private void OnClose(bool resetOnly = true) {
            _detectorsPanel.Show();
            _resultsPanel.Hide();
            _progressPanel.Hide();
            _returnButton.Hide();
            if (resetOnly) {
                RunFadeInOrOutAnimation(false, () => Hide());
            }

#if !DEBUG
            if (resetOnly) {
                BndResultHighlightManager.instance.enabled = false;
            }
#endif
        }
    }
}