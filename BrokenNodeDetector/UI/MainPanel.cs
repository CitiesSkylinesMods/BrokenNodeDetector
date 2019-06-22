using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ColossalFramework;
using ColossalFramework.UI;
using ICities;
using UnityEngine;

namespace BrokenNodeDetector {
    public class MainPanel : UIPanel {
        private UILabel _title;
        private UILabel _brokenNodesLabel;
        private UIButton _closeButton;
        private UIButton _startButton;
        private UIButton _moveNextButton;
        private UIPanel _mainPanel;
        private UIProgressBar _progressBar;

        private List<ushort> _invalidNodes;

        private List<ushort>.Enumerator _invalidNodesEnumerator;

        private readonly List<ushort> _markedForRemoval = new List<ushort>();

        public void Initailize() {
            if (_mainPanel != null) {
                _mainPanel.OnDestroy();
            }

            isVisible = false;
            _mainPanel = AddUIComponent<UIPanel>();
            _mainPanel.backgroundSprite = "UnlockingPanel2";
            _mainPanel.color = new Color32(75, 75, 135, 255);
            width = 400;
            height = 130;
            _mainPanel.width = 400;
            _mainPanel.height = 130;

            relativePosition = new Vector3(250, 20);
            _mainPanel.relativePosition = Vector3.zero;

            _title = _mainPanel.AddUIComponent<UILabel>();
            _title.autoSize = true;
            _title.padding = new RectOffset(10, 10, 15, 15);
            _title.relativePosition = new Vector2(100, 12);
            _title.text = "Broken node detector";

            _closeButton = _mainPanel.AddUIComponent<UIButton>();
            _closeButton.eventClick += CloseButtonClick;
            _closeButton.relativePosition = new Vector3(width - _closeButton.width - 45, 15f);
            _closeButton.normalBgSprite = "buttonclose";
            _closeButton.hoveredBgSprite = "buttonclosehover";
            _closeButton.pressedBgSprite = "buttonclosepressed";


            _startButton = _mainPanel.AddUIComponent<UIButton>();
            _startButton.eventClick += StartButtonClick;
            _startButton.relativePosition = new Vector3(width / 2 - 64, 60f);
            _startButton.width = 128;
            _startButton.height = 48;
            _startButton.normalBgSprite = "ButtonMenu";
            _startButton.hoveredBgSprite = "ButtonMenu";
            _startButton.pressedBgSprite = "ButtonMenu";
            _startButton.text = "Run detector";

            _progressBar = _mainPanel.AddUIComponent<UIProgressBar>();
            _progressBar.relativePosition = new Vector3(50, 75);
            _progressBar.width = 300;
            _progressBar.height = 25;
            _progressBar.fillMode = UIFillMode.Fill;
            _progressBar.progressSprite = "ProgressBarFill";
            _progressBar.isVisible = false;

            _brokenNodesLabel = _mainPanel.AddUIComponent<UILabel>();
            _brokenNodesLabel.autoSize = true;
            _brokenNodesLabel.padding = new RectOffset(10, 10, 15, 15);
            _brokenNodesLabel.relativePosition = new Vector2(95, 120);

            _moveNextButton = _mainPanel.AddUIComponent<UIButton>();
            _moveNextButton.eventClick += MoveNextBrokeNodeButtonClick;
            _moveNextButton.relativePosition = new Vector3(75, 240f);
            _moveNextButton.width = 250;
            _moveNextButton.height = 48;
            _moveNextButton.normalBgSprite = "ButtonMenu";
            _moveNextButton.hoveredBgSprite = "ButtonMenu";
            _moveNextButton.pressedBgSprite = "ButtonMenu";
            _moveNextButton.text = "Move to next broken node";
            _moveNextButton.Hide();
        }

        private void CloseButtonClick(UIComponent component, UIMouseEventParameter eventparam) {
            eventparam.Use();
            OnClose();
        }

        private void StartButtonClick(UIComponent component, UIMouseEventParameter eventparam) {
            eventparam.Use();
            OnBeforeStart();

            ModService.Instance.StartDetector();
            StartCoroutine(Countdown(5, () => {
                ModService.Instance.StopDetector();
                OnAfterStop();
                ShowResults();
            }));
        }

        private void MoveNextBrokeNodeButtonClick(UIComponent component, UIMouseEventParameter eventparam) {
            
            if (_invalidNodes == null || _invalidNodes.Count == 0) return;
            
            InstanceID instanceId = default;
            ushort nextNodeId = 0;
            if (_invalidNodesEnumerator.MoveNext()) {
                nextNodeId = _invalidNodesEnumerator.Current;
            }
            
            if (nextNodeId == 0) return;
            
            instanceId.NetNode = nextNodeId;
            if (InstanceManager.IsValid(instanceId)) {
                ToolsModifierControl.cameraController.SetTarget(instanceId, ToolsModifierControl.cameraController.transform.position, true);
            } else {
                _markedForRemoval.Add(nextNodeId);
                _moveNextButton.SimulateClick();
            }

            //reset cycle
            if (_invalidNodes.IndexOf(nextNodeId) == _invalidNodes.Count - 1) {
                for (int i = 0; i < _markedForRemoval.Count; i++) {
                    _invalidNodes.Remove(_markedForRemoval[i]);
                }

                _markedForRemoval.Clear();
                if (_invalidNodes.Count == 0) return;
                
                _invalidNodesEnumerator = _invalidNodes.GetEnumerator();
            }
        }

        private void OnBeforeStart() {
            _markedForRemoval.Clear();
            _brokenNodesLabel.relativePosition = new Vector2(95, 120);
            _brokenNodesLabel.Hide();

            _mainPanel.height = 130;
            _startButton.Hide();
            _progressBar.value = 0;
            _progressBar.Show();

            _moveNextButton.Hide();
        }

        private void OnAfterStop() {
            _startButton.Show();
            _brokenNodesLabel.Show();

            _progressBar.Hide();
        }

        private void OnClose() {
            _mainPanel.height = 130;
            _brokenNodesLabel.relativePosition = new Vector2(95, 120);
            _brokenNodesLabel.text = "";
            _brokenNodesLabel.Hide();
            _moveNextButton.Hide();
            Hide();
        }

        IEnumerator Countdown(int seconds, Action action) {
            int counter = seconds;
            _progressBar.maxValue = seconds;
            while (counter > 0) {
                UpdateProgressBar(counter);
                yield return new WaitForSeconds(1);
                counter--;
            }

            action();
        }

        private void UpdateProgressBar(int value) {
            float currentValue = _progressBar.maxValue - value + 1;
            _progressBar.value = currentValue;
        }

        private void ShowResults() {
            if (ModService.Instance.Results.Count == 0) {
                _brokenNodesLabel.text = "Great! Nothing found :-)";
                _mainPanel.height = 160;
                Debug.Log("BrokenNodeDetector - nothing found");
            } else {
                _brokenNodesLabel.relativePosition = new Vector2(10, 120);
                _brokenNodesLabel.text = $"Found {ModService.Instance.Results.Count} possibly broken nodes\n" +
                                         "1. Click on 'Move next' to show node location\n" +
                                         "2. Move node or rebuild path segment\n" +
                                         "3. Repeat 1-2 until nothing new found\n" +
                                         "Run detector again if you want :)";
                _invalidNodes = ModService.Instance.Results;
                Debug.Log($"BrokenNodeDetector found {_invalidNodes.Count} nodes. ({string.Join(",", _invalidNodes.Select(i => i.ToString()).ToArray())})");
                _invalidNodesEnumerator = _invalidNodes.GetEnumerator();
                _moveNextButton.Show();
                _mainPanel.height = 300;
            }
        }
    }
}