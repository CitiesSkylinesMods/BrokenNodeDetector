using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using ColossalFramework.UI;
using UnityEngine;

namespace BrokenNodeDetector.UI.Tools.DisconnectedBuildingsTool {
    public class DisconnectedBuildings : Detector {
        private readonly HashSet<ushort> _buildingsVisited = new HashSet<ushort>();
        private readonly Dictionary<ushort, Vector3> _disconnectedBuildings = new Dictionary<ushort, Vector3>();
        private ushort _currentBuilding;
        private volatile float _progress;

        public DisconnectedBuildings() {
            BuildTemplate();
        }

        public override string Name => "Find disconnected buildings";

        public override string Tooltip =>
            "Detects disconnected buildings\n" +
            "They may block city service vehicles from spawning";

        public override IEnumerable<float> Process() {
            BuildingManager bm = BuildingManager.instance;
            IsProcessing = true;
            ResetState();

            _progress = 0f;
            AsyncTask<float> asyncTask = SimulationManager.instance.AddAction(CheckRoadAccess());
            while (!asyncTask.completed) {
                yield return _progress;
            }
            
            _progress = 0f;
            AsyncTask<float> asyncTask2 = SimulationManager.instance.AddAction(CollectDisconnectedBuildings());
            while (!asyncTask2.completed) {
                yield return _progress;
            }

            yield return 1.0f;
            IsProcessing = false;
        }

        public override void InitResultsView(UIComponent component) {
            if (component is UIPanel panel) {
                AttachCallbacks(component);
                UILabel label = panel.Find<UILabel>("Label"); 
                if (_disconnectedBuildings.Count == 0) {
                    label.relativePosition = Vector3.zero; 
                    label.text = "Great! Nothing found :-)";
                    Debug.Log("[BND] Disconnected buildings not detected :-)");
                } else {
                    label.relativePosition = new Vector3(10, 0);
                    label.text = $"Found {_disconnectedBuildings.Count} disconnected building(s)\n";
                    label.color = Color.yellow;

                    UpdateBuildingsPanel(panel, label);
                    UpdateBuildingButtons(panel.Find<UIButton>("MoveNext"));
                    component.height = 130;
                }

            }
        }

        private void AttachCallbacks(UIComponent templateInstance) {
            templateInstance.Find<UIButton>("MoveNext").eventClick += MoveNextBuildingButtonClick;
        }

        private IEnumerator<float> CheckRoadAccess() {
            BuildingManager bm = BuildingManager.instance;
            float searchStep = 1.0f / bm.m_buildings.m_size;
            
            ProgressMessage = $"Processing...0% Pass 1/2";
            Building[] mBuffer = bm.m_buildings.m_buffer;
            for (int i = 1; i < mBuffer.Length; i++) {

                if ((mBuffer[i].m_flags & (Building.Flags.Created | Building.Flags.Deleted)) == Building.Flags.Created &&
                    mBuffer[i].Info
                    ) {
                    mBuffer[i].Info.m_buildingAI.CheckRoadAccess((ushort)i, ref mBuffer[i]);
                }

                float searchProgress = searchStep * i;
                if (i % 64 == 0) {
                    ProgressMessage = $"Processing...{searchProgress * 100:F0}% Pass 1/2";
                    Thread.Sleep(1);
                    _progress = searchProgress;
                }
            }
            ProgressMessage = $"Processing...100% Pass 1/2";
            
            yield return 1.0f;
        }

        private IEnumerator<float> CollectDisconnectedBuildings() {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("[BND] Scanning for disconnected buildings");
            sb.AppendLine("[BND] ");
            sb.AppendLine("[BND] =( Building ID )===( Location )=");
            sb.AppendLine();
            BuildingManager bm = BuildingManager.instance;
            Building[] mBuffer = bm.m_buildings.m_buffer;
            float searchStep = 1.0f / bm.m_buildings.m_size;
            
            ProgressMessage = "Processing...0% Pass 2/2";
            _progress = 0f;
            
            int counter = 0;
            for (int i = 1; i < mBuffer.Length; i++) {

                if ((mBuffer[i].m_flags & (Building.Flags.Created | Building.Flags.Deleted)) == Building.Flags.Created &&
                    mBuffer[i].Info
                    ) {
                    bool notConnected = ((mBuffer[i].m_flags & Building.Flags.RoadAccessFailed) != Building.Flags.None &&
                                         ((mBuffer[i].m_problems.m_Problems1 & (Notification.Problem1.RoadNotConnected | Notification.Problem1.PathNotConnected)) != Notification.Problem1.None ||
                                          (mBuffer[i].m_problems.m_Problems2 & (Notification.Problem2.NotInPedestrianZone | Notification.Problem2.CannotBeReached)) != Notification.Problem2.None));

                    if (notConnected) {
                        counter++;
                        _disconnectedBuildings.Add((ushort)i, mBuffer[i].m_position);
                        sb.AppendLine("==(" + i + ")===" + mBuffer[i].m_position + "==");
                        sb.Append("Building [ItemClass: ")
                            .Append((mBuffer[i].Info ? mBuffer[i].Info.m_class.name : "") + "] [Flags: ")
                            .Append(mBuffer[i].m_flags.ToString()).Append("] [Problems: ").Append(mBuffer[i].m_problems.ToString())
                            .Append("] [Name: ").Append(mBuffer[i].Info.name).Append("]");
                        sb.AppendLine("---------------------------------------");
                    }
                }

                float searchProgress = searchStep * i;
                if (i % 128 == 0) {
                    ProgressMessage = $"Processing...{searchProgress * 100:F0}% Pass 2/2";
                    Thread.Sleep(1);
                    _progress = searchProgress;
                }
            }
            ProgressMessage = $"Processing...100% Pass 2/2";

            Debug.Log("[BND] Disconnected building instances count: " + counter);
            Debug.Log("[BND] Scan report\n" + sb + "\n\n=======================================================");
            yield return 1.0f;
        }

        private void BuildTemplate() {
            _template = new GameObject("DisconnectedBuildingTemplate").AddComponent<UIPanel>();
            _template.transform.SetParent(_defaultGameObject.transform, true);
            _template.width = 400;
            _template.height = 50;
            UILabel label = _template.AddUIComponent<UILabel>();
            label.autoSize = true;
            label.padding = new RectOffset(15, 10, 15, 15);
            label.relativePosition = new Vector3(0, 0);
            label.textScale = 1.2f;
            label.name = "Label";
            UIButton moveNextButton = UIHelpers.CreateButton(
                _template,
                "Move to next disconnected building",
                new Rect(new Vector2(45, 85f),
                    new Vector2(310, 32)),
                MoveNextBuildingButtonClick);
            moveNextButton.name = "MoveNext";
            moveNextButton.Hide();
            
            UIPanel buildingPanel = _template.AddUIComponent<UIPanel>();
            buildingPanel.width = 400;
            buildingPanel.height = 40;
            buildingPanel.relativePosition = new Vector2(15, 45);
            buildingPanel.name = "BuildingPanel";
            
            UILabel buildingId = buildingPanel.AddUIComponent<UILabel>();
            buildingId.processMarkup = true;
            buildingId.prefix = "Building ID: ";
            buildingId.relativePosition = new Vector2(0, 10);
            buildingId.name = "BuildingID";
            buildingId.textScale = 0.8f;
            
            UILabel buildingPos = buildingPanel.AddUIComponent<UILabel>();
            buildingPos.processMarkup = true;
            buildingPos.prefix = "Position: ";
            buildingPos.relativePosition = new Vector2(150, 10);
            buildingPos.name = "BuildingPosition";
            buildingPos.textScale = 0.8f;
            
            buildingPanel.Hide();
        }

        private void ResetState() {
            _buildingsVisited.Clear();
            _disconnectedBuildings.Clear();
            _currentBuilding = 0;
        }

        private void MoveNextBuildingButtonClick(UIComponent component, UIMouseEventParameter eventparam) {
            UIPanel panel = component.parent.Find<UIPanel>("BuildingPanel");
            UILabel label = component.parent.Find<UILabel>("Label");
            
            if (_disconnectedBuildings.Count == 0) {
                UpdateBuildingsPanel(panel, label);
            }

            List<ushort> keys = _disconnectedBuildings.Keys.ToList();
            if (keys.Count > 0) {
                _currentBuilding = keys.Find(s => !_buildingsVisited.Contains(s));
                if (_currentBuilding == 0) {
                    _currentBuilding = keys[0];
                    _buildingsVisited.Clear();
                }
            } else {
                _currentBuilding = 0;
                _buildingsVisited.Clear();
            }

            UpdateBuildingButtons(component);
            UpdateBuildingsPanel(panel, label);

            if (_currentBuilding == 0) return;
            Debug.Log("[BND] Moving to next disconnected building (" + _currentBuilding + ")");
            InstanceID instanceId = default;
            instanceId.Building = _currentBuilding;
            _buildingsVisited.Add(_currentBuilding);

            bool unlimitedCamera = ToolsModifierControl.cameraController.m_unlimitedCamera;
            ToolsModifierControl.cameraController.m_unlimitedCamera = true;
            ToolsModifierControl.cameraController.SetTarget(instanceId, ToolsModifierControl.cameraController.transform.position, true);
            BndResultHighlightManager.instance.Highlight(new HighlightData{BuildingID = _currentBuilding, Type = HighlightType.Building});
            ToolsModifierControl.cameraController.m_unlimitedCamera = unlimitedCamera;
            ToolsModifierControl.cameraController.ClearTarget();
        }

        private void UpdateBuildingsPanel(UIPanel buildingPanel, UILabel label) {
            if (_disconnectedBuildings.Count == 0) {
                ResetState();
                label.text = "Great! Nothing found :-)";
                label.textColor = new Color(0f, 0.81f, 0.05f);
                label.parent.height = 50;
                label.relativePosition = Vector3.zero;
                label.color = Color.white;
                return;
            }

            buildingPanel.Show();

            UILabel buildingId = buildingPanel.Find<UILabel>("BuildingID");
            UILabel buildingPos = buildingPanel.Find<UILabel>("BuildingPosition");
            if (_currentBuilding != 0 && _disconnectedBuildings.TryGetValue(_currentBuilding, out Vector3 position)) {
                buildingId.text = $"<color #FFF000>{_currentBuilding.ToString()}</color>";
                buildingPos.text = $"<color #FFF000>{position.ToString()}</color>";
            } else {
                buildingId.text = " ";
                buildingPos.text = " ";
            }

            label.relativePosition = new Vector3(15, 0);
            label.processMarkup = true;
            label.text = $"<color #FFAA00>{_disconnectedBuildings.Count}</color> possibly disconnected building(s)";
        }

        private void UpdateBuildingButtons(UIComponent moveNextButton) {
            if (_disconnectedBuildings.Count > 0) {
                moveNextButton.Show();
            } else {
                moveNextButton.Hide();
            }
        }
    }
}