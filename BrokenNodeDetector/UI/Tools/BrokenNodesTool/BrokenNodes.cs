using System.Collections.Generic;
using System.Linq;
using ColossalFramework.UI;
using UnityEngine;

namespace BrokenNodeDetector.UI.Tools.BrokenNodesTool {
    public class BrokenNodes : Detector {
        private readonly List<ushort> _markedForRemoval = new List<ushort>();
        private List<ushort> _invalidNodes;
        private List<ushort>.Enumerator _invalidNodesEnumerator;

        public BrokenNodes() {
            CustomYieldInstruction = new WaitForSeconds(0.5f);
            BuildTemplate();
        }

        public override string Name => "Find broken nodes";

        public override string Tooltip => "Detects broken pedestrian/bike path nodes\nThey may slow simulation\n" +
                                          "Simulation must be running for correct detection result!";

        public override IEnumerable<float> Process() {
            IsProcessing = true;
            ProgressMessage = "Searching for broken nodes... Do not pause simulation!";
            _markedForRemoval.Clear();
            ModService.Instance.StartDetector();
            for (int i = 0; i < 10; i++) {
                yield return i * 0.1f;
            }

            ModService.Instance.StopDetector();
            yield return 1f;

            ProgressMessage = "Done";
            IsProcessing = false;
        }

        public override void InitResultsView(UIComponent component) {
            if (component is UIPanel panel) {
                AttachCallbacks(component);
                UILabel label = panel.Find<UILabel>("UILabel");
                if (ModService.Instance.Results.Count > 0) {
                    label.text = $"Found {ModService.Instance.Results.Count} possibly broken nodes\n" +
                                 " 1. Click on 'Move next' to show node location\n" +
                                 " 2. Move node or rebuild path segment\n" +
                                 " 3. Repeat 1-2 until nothing new found\n" +
                                 "Run detector again if you want :)";
                    _invalidNodes = ModService.Instance.Results;
                    _invalidNodesEnumerator = _invalidNodes.GetEnumerator();
                    Debug.Log($"[BND] Found {_invalidNodes.Count} nodes. ({string.Join(",", _invalidNodes.Select(i => i.ToString()).ToArray())})");
                    component.height = 170;
                    UIButton moveNext = panel.Find<UIButton>("MoveNext");
                    moveNext.Show();
                } else {
                    component.height = 50;
                    label.textScale = 1.2f;
                    label.text = "Great! Nothing found :-)";
                    Debug.Log("[BND] Nothing found :-)");
                }
            }
        }

        private void BuildTemplate() {
            _template = new GameObject("BrokenNodesTemplate").AddComponent<UIPanel>();
            _template.transform.SetParent(_defaultGameObject.transform, true);
            _template.width = 400;
            _template.height = 50;
            UILabel label = _template.AddUIComponent<UILabel>();
            label.autoSize = true;
            label.padding = new RectOffset(15, 10, 15, 15);
            label.relativePosition = new Vector3(0, 0);
            UIButton moveNextButton = UIHelpers.CreateButton(
                _template,
                "Move to next broken node",
                new Rect(new Vector2(75, 120f),
                    new Vector2(250, 32)),
                MoveNextBrokeNodeButtonClick);
            moveNextButton.name = "MoveNext";
            moveNextButton.Hide();
        }

        private void AttachCallbacks(UIComponent templateInstance) {
            templateInstance.Find<UIButton>("MoveNext").eventClick += MoveNextBrokeNodeButtonClick;
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
                bool unlimitedCamera = ToolsModifierControl.cameraController.m_unlimitedCamera;
                ToolsModifierControl.cameraController.m_unlimitedCamera = true;
                ToolsModifierControl.cameraController.SetTarget(instanceId, ToolsModifierControl.cameraController.transform.position, true);
                BndResultHighlightManager.instance.Highlight(new HighlightData { NodeID = nextNodeId, Type = HighlightType.Node });
                ToolsModifierControl.cameraController.m_unlimitedCamera = unlimitedCamera;
                ToolsModifierControl.cameraController.ClearTarget();
            } else {
                _markedForRemoval.Add(nextNodeId);
                component.SimulateClick();
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
    }
}