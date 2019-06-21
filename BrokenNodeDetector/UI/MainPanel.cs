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
        private readonly SavedInputKey _escKey = new SavedInputKey("Close detector", MainUI.FILE_NAME, KeyCode.Escape, false, false, false, true);
        
        private UILabel title;
        private UILabel brokenNodesLabel;
        private UIButton closeButton;
        private UIButton startButton;
        private UIButton moveNextButton;
        private UIPanel mainPanel;
        private UIProgressBar progressBar;

        private List<ushort> invalidNodes;

        private List<ushort>.Enumerator _invalidNodesEnumerator;

        public void Initailize() {
            if (mainPanel != null) {
                mainPanel.OnDestroy();
            }

            isVisible = false;
            mainPanel = AddUIComponent<UIPanel>();
            mainPanel.backgroundSprite = "UnlockingPanel2";
            mainPanel.color = new Color32(75, 75, 135, 255);
            width = 400;
            height = 130;
            mainPanel.width = 400;
            mainPanel.height = 130;

            Vector2 resolution = UIView.GetAView().GetScreenResolution();
            relativePosition = new Vector3(resolution.x / 2 - 300, resolution.y / 5);
            mainPanel.relativePosition = Vector3.zero;

            title = mainPanel.AddUIComponent<UILabel>();
            title.autoSize = true;
            title.padding = new RectOffset(10, 10, 15, 15);
            title.relativePosition = new Vector2(100, 12);
            title.text = "Broken node detector";

            closeButton = mainPanel.AddUIComponent<UIButton>();
            closeButton.eventClick += CloseButtonClick;
            closeButton.relativePosition = new Vector3(width - closeButton.width - 45, 15f);
            closeButton.normalBgSprite = "buttonclose";
            closeButton.hoveredBgSprite = "buttonclosehover";
            closeButton.pressedBgSprite = "buttonclosepressed";


            startButton = mainPanel.AddUIComponent<UIButton>();
            startButton.eventClick += StartButtonClick;
            startButton.relativePosition = new Vector3(width / 2 - 64, 60f);
            startButton.width = 128;
            startButton.height = 48;
            startButton.normalBgSprite = "ButtonMenu";
            startButton.hoveredBgSprite = "ButtonMenu";
            startButton.pressedBgSprite = "ButtonMenu";
            startButton.text = "Run detector";

            progressBar = mainPanel.AddUIComponent<UIProgressBar>();
            progressBar.relativePosition = new Vector3(50, 75);
            progressBar.width = 300;
            progressBar.height = 25;
            progressBar.fillMode = UIFillMode.Fill;
            progressBar.progressSprite = "ProgressBarFill";
            progressBar.isVisible = false;

            brokenNodesLabel = mainPanel.AddUIComponent<UILabel>();
            brokenNodesLabel.autoSize = true;
            brokenNodesLabel.padding = new RectOffset(10, 10, 15, 15);
            brokenNodesLabel.relativePosition = new Vector2(75, 120);

            moveNextButton = mainPanel.AddUIComponent<UIButton>();
            moveNextButton.eventClick += MoveNextBrokeNodeButtonClick;
            moveNextButton.relativePosition = new Vector3(75, 240f);
            moveNextButton.width = 250;
            moveNextButton.height = 48;
            moveNextButton.normalBgSprite = "ButtonMenu";
            moveNextButton.hoveredBgSprite = "ButtonMenu";
            moveNextButton.pressedBgSprite = "ButtonMenu";
            moveNextButton.text = "Move to next broken node";
            moveNextButton.Hide();
        }

        private void OnGUI() {
            if (isVisible && _escKey.IsPressed()) {
                closeButton.SimulateClick();
            }
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
            if (invalidNodes == null || invalidNodes.Count == 0) return;
            
            InstanceID instanceId = default;
            ushort nextNodeId = 0; 
            if (_invalidNodesEnumerator.MoveNext()) {
                nextNodeId = _invalidNodesEnumerator.Current;
            }

            instanceId.NetNode = nextNodeId;
            if (InstanceManager.IsValid(instanceId)) {
                ToolsModifierControl.cameraController.SetTarget(instanceId, ToolsModifierControl.cameraController.transform.position, true);
            }
            //TODO add reset to repeat cycling from start again
        }
        private void OnBeforeStart() {            
            brokenNodesLabel.relativePosition = new Vector2(75, 120);
            brokenNodesLabel.Hide();

            mainPanel.height = 130;
            startButton.Hide();
            progressBar.value = 0;
            progressBar.Show();

            moveNextButton.Hide();
        }

        private void OnAfterStop() {
            startButton.Show();
            brokenNodesLabel.Show();

            progressBar.Hide();
        }

        private void OnClose() {
            mainPanel.height = 130;
            brokenNodesLabel.relativePosition = new Vector2(75, 120);
            brokenNodesLabel.text = "";
            brokenNodesLabel.Hide();
            moveNextButton.Hide();
            Hide();
        }

        IEnumerator Countdown(int seconds, Action action) {
            int counter = seconds;
            progressBar.maxValue = seconds;
            while (counter > 0) {
                UpdateProgressBar(counter);
                yield return new WaitForSeconds(1);
                counter--;
            }

            action();
        }

        private void UpdateProgressBar(int value) {
            float currentValue = progressBar.maxValue - value + 1;
            progressBar.value = currentValue;
        }

        private void ShowResults() {
            if (ModService.Instance.Results.Count == 0) {
                brokenNodesLabel.text = "Great! Nothing found :-)";
                mainPanel.height = 160;
                Debug.Log("BrokenNodeDetector - nothing found");
            } else {
                brokenNodesLabel.relativePosition = new Vector2(10, 120);
                brokenNodesLabel.text = $"Found {ModService.Instance.Results.Count} possibly broken nodes\n" +
                                        "1. Click on 'Move next' to show node location\n" +
                                        "2. Rebuild or remove node\n" +
                                        "3. Repeat 1-2 until nothing new found\n" +
                                        "Run detector again if you want :)";
                invalidNodes = ModService.Instance.Results;
                Debug.Log($"BrokenNodeDetector found {invalidNodes.Count} nodes. ({string.Join(",", invalidNodes.Select(i=> i.ToString()).ToArray())})");
                _invalidNodesEnumerator = invalidNodes.GetEnumerator();
                moveNextButton.Show();
                mainPanel.height = 300;
            }
        }
    }
}