using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using ColossalFramework;
using ColossalFramework.UI;
using UnityEngine;

namespace BrokenNodeDetector.UI {
    public class MainPanel : UIPanel {
        private UIPanel _mainPanel;
        private UILabel _title;
        private UILabel _brokenNodesLabel;
        private UIDragHandle _dragHandle;
        private UIButton _closeButton;
        private UIButton _startButton;
        private UIButton _moveNextButton;
        private UIButton _searchGhostNodesButton;
        private UIButton _searchShortSegmentsButton;

        //todo move to separate panel class..
        private UIButton _searchDisconnectedPtStopsButton;
        private UIButton _moveNextPtButton;
        private UIButton _moveNextTransportLineButton;
        private UIButton _removePtStopButton;
        private UIButton _removePtLaneButton;
        private UIButton _searchDisconnectedBuildingsButton;
        private UIButton _moveNextBuildingButton;

        private UIPanel _linePanel;
        private UILabel _lineId;
        private UILabel _lineName;
        private UIColorField _lineColor;
        private UILabel _brokenStopsNumber;


        private UIButton _moveNextSegButton;
        private UIPanel _segmentPanel;
        private UILabel _segmentId;
        private UILabel _segmentType;
        private UILabel _segmentLength;

        private UIPanel _buildingPanel;
        private UILabel _buildingId;
        
        private UIProgressBar _progressBar;

        private List<ushort> _invalidNodes;
        private List<ushort>.Enumerator _invalidNodesEnumerator;
        private readonly List<ushort> _markedForRemoval = new List<ushort>();
        private readonly HashSet<uint> _segmentsVisited = new HashSet<uint>();
        private readonly HashSet<uint> _buildingsVisited = new HashSet<uint>();

        private ushort _currentLine;
        private ushort _currentStop;

        private uint _currentSegment;
        private uint _currentBuilding;
        //todo ----------------------------------
        private Coroutine _runningScan;
        private Coroutine _runningProgress;

        public void Initialize() {
            if (_mainPanel != null) {
                _mainPanel.OnDestroy();
            }

            isVisible = false;
            _mainPanel = AddUIComponent<UIPanel>();
            _mainPanel.backgroundSprite = "UnlockingPanel2";
            _mainPanel.color = new Color32(75, 75, 135, 255);
            width = 410;
            height = 130;
            _mainPanel.width = 410;
            _mainPanel.height = 170;

            relativePosition = new Vector3(ModSettings.instance.MenuPosX, ModSettings.instance.MenuPosY);
            _mainPanel.relativePosition = Vector3.zero;

            _title = AddUIComponent<UILabel>();
            _title.autoSize = true;
            _title.padding = new RectOffset(10, 10, 5, 15);
            _title.relativePosition = new Vector2(100, 12);
            _title.text = "Broken node detector";

            _dragHandle = AddUIComponent<UIDragHandle>();
            _dragHandle.area = new Vector4(0,0,380, 45);
            // _dragHandle.width = 380;
            // _dragHandle.height = 45;

            _closeButton = _mainPanel.AddUIComponent<UIButton>();
            _closeButton.eventClick += CloseButtonClick;
            _closeButton.relativePosition = new Vector3(width - _closeButton.width - 35, 5f);
            _closeButton.normalBgSprite = "buttonclose";
            _closeButton.hoveredBgSprite = "buttonclosehover";
            _closeButton.pressedBgSprite = "buttonclosepressed";

            _startButton = CreateButton(_mainPanel, "Broken nodes", new Rect(new Vector2(10, 50f), new Vector2(180, 32)), StartButtonClick);
            _searchGhostNodesButton = CreateButton(_mainPanel, "Ghost nodes", new Rect(new Vector2(200, 50f), new Vector2(200, 32)), GhostNodesButtonClick);
            _searchShortSegmentsButton = CreateButton(_mainPanel, "Short segments", new Rect(new Vector2(10, 90f), new Vector2(180, 32)), ShortSegmentsButtonClick);
            _searchDisconnectedPtStopsButton = CreateButton(_mainPanel, "Disconnected PT stops", new Rect(new Vector2(200, 90f), new Vector2(200, 32)), PTLineDetectorClick);
            _searchDisconnectedBuildingsButton = CreateButton(_mainPanel, "Disconnected buildings (experimental)", new Rect(new Vector2(10, 130f), new Vector2(390, 32)), StartScanBuildingsClick);

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
            _brokenNodesLabel.relativePosition = new Vector2(95, 152);

            _moveNextButton = CreateButton(_mainPanel, "Move to next broken node", new Rect(new Vector2(75, 260f), new Vector2(260, 32)), MoveNextBrokeNodeButtonClick);
            _moveNextButton.Hide();

            _moveNextSegButton = CreateButton(_mainPanel, "Move to next segment", new Rect(new Vector2(75, 280f), new Vector2(250, 32)), MoveNextSegmentButtonClick);
            _moveNextSegButton.Hide();
            
            _moveNextBuildingButton = CreateButton(_mainPanel, "Move to next building", new Rect(new Vector2(75, 230f), new Vector2(250, 48)), MoveNextBuildingButtonClick);
            _moveNextBuildingButton.Hide();

            _moveNextTransportLineButton = CreateButton(_mainPanel, "Find next broken PT Line", new Rect(new Vector2(10, 200f), new Vector2(235, 32)), MoveNextPtLineButtonClick);
            _moveNextTransportLineButton.Hide();

            _removePtLaneButton = CreateButton(_mainPanel, "Remove Line", new Rect(new Vector2(255, 200f), new Vector2(135, 32)), RemovePtLineClick);
            _removePtLaneButton.Hide();

            _moveNextPtButton = CreateButton(_mainPanel, "Find next disconnected stop", new Rect(new Vector2(10, 310f), new Vector2(235, 32)), MoveNextPtStopClick);
            _moveNextPtButton.Hide();

            _removePtStopButton = CreateButton(_mainPanel, "Remove", new Rect(new Vector2(255, 310f), new Vector2(135, 32)), RemovePtStopClick);
            _removePtStopButton.Hide();

            _linePanel = _mainPanel.AddUIComponent<UIPanel>();
            _linePanel.width = 390;
            _linePanel.height = 80;
            _linePanel.relativePosition = new Vector2(10, 235);

            _lineId = _linePanel.AddUIComponent<UILabel>();
            _lineId.prefix = "Line ID: ";
            _lineId.relativePosition = new Vector2(10, 5);

            _lineColor = _linePanel.AddUIComponent<UIColorField>();
            _lineColor.selectedColor = Color.black;
            _lineColor.relativePosition = new Vector2(330, 0);
            _lineColor.normalFgSprite = "ColorPickerColor";
            _lineColor.width = 60;
            _lineColor.height = 25;

            _lineName = _linePanel.AddUIComponent<UILabel>();
            _lineName.prefix = "Line name: ";
            _lineName.relativePosition = new Vector2(10, 30);

            _brokenStopsNumber = _linePanel.AddUIComponent<UILabel>();
            _brokenStopsNumber.prefix = "All stops: N/A";
            _brokenStopsNumber.relativePosition = new Vector2(10, 58);

            _linePanel.Hide();
            
            _segmentPanel = _mainPanel.AddUIComponent<UIPanel>();
            _segmentPanel.width = 390;
            _segmentPanel.height = 80;
            _segmentPanel.relativePosition = new Vector2(10, 195);

            _segmentId = _segmentPanel.AddUIComponent<UILabel>();
            _segmentId.prefix = "Segment ID: ";
            _segmentId.relativePosition = new Vector2(10, 5);

            _segmentType = _segmentPanel.AddUIComponent<UILabel>();
            _segmentType.prefix = "Segment type: ";
            _segmentType.relativePosition = new Vector2(10, 30);

            _segmentLength = _segmentPanel.AddUIComponent<UILabel>();
            _segmentLength.prefix = "Length: N/A";
            _segmentLength.relativePosition = new Vector2(10, 58);

            _segmentPanel.Hide();

            _buildingPanel = _mainPanel.AddUIComponent<UIPanel>();
            _buildingPanel.width = 390;
            _buildingPanel.height = 40;
            _buildingPanel.relativePosition = new Vector2(10, 195);
            
            _buildingId = _buildingPanel.AddUIComponent<UILabel>();
            _buildingId.prefix = "Building ID: ";
            _buildingId.relativePosition = new Vector2(10, 5);
            
            _buildingPanel.Hide();

            absolutePosition = new Vector3(ModSettings.instance.MenuPosX, ModSettings.instance.MenuPosY);
        }

        protected override void OnPositionChanged() {
            base.OnPositionChanged();
            
            bool posChanged = ModSettings.instance.MenuPosX != (int)absolutePosition.x
                              || ModSettings.instance.MenuPosY != (int)absolutePosition.y;
            if (posChanged) {
                ModSettings.instance.MenuPosX.value = (int) absolutePosition.x;
                ModSettings.instance.MenuPosY.value = (int) absolutePosition.y;
            }
        }

        internal void ForceUpdateMenuPosition() {
            absolutePosition = new Vector3(ModSettings.instance.MenuPosX, ModSettings.instance.MenuPosY);
        }

        private void CloseButtonClick(UIComponent component, UIMouseEventParameter eventparam) {
            eventparam.Use();
            OnClose();
        }

        private void StartButtonClick(UIComponent component, UIMouseEventParameter eventparam) {
            eventparam.Use();
            OnBeforeStart();

            ModService.Instance.StartDetector();
            _runningScan = StartCoroutine(Countdown(5, () => {
                ModService.Instance.StopDetector();
                OnAfterStop();
                ShowResults();
            }));
        }

        private void GhostNodesButtonClick(UIComponent component, UIMouseEventParameter eventparam) {
            eventparam.Use();
            OnBeforeStart();

            _runningScan = StartCoroutine(ModService.Instance.SearchForGhostNodes());

            _runningProgress = StartCoroutine(Countdown(5, () => {
                ShowGhostNodesResult();
                OnAfterStop();
            }));
        }

        private void ShortSegmentsButtonClick(UIComponent component, UIMouseEventParameter eventparam) {
            eventparam.Use();
            OnBeforeStart();

            _runningScan = StartCoroutine(ModService.Instance.SearchForShortSegments());

            _runningProgress = StartCoroutine(UpdateProgress(() => {
                OnAfterStop();
                ShowShortSegmentsResult();
            }));
        } 
        
        private void StartScanBuildingsClick(UIComponent component, UIMouseEventParameter eventparam) {
            eventparam.Use();
            OnBeforeStart();

            _runningScan = StartCoroutine(ModService.Instance.ScanForDisconnectedBuildings());

            _runningProgress = StartCoroutine(UpdateProgress(() => {
                OnAfterStop();
                ShowBuildingResult();
            }));
        }

        private void PTLineDetectorClick(UIComponent component, UIMouseEventParameter eventparam) {
            eventparam.Use();
            OnBeforeStart();

            _runningScan = StartCoroutine(ModService.Instance.SearchForDisconnectedPtStops());
            _runningProgress = StartCoroutine(UpdateProgress(() => {
                OnAfterStop();
                ShowPtResults();
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
                bool unlimitedCamera = ToolsModifierControl.cameraController.m_unlimitedCamera;
                ToolsModifierControl.cameraController.m_unlimitedCamera = true;
                ToolsModifierControl.cameraController.SetTarget(instanceId, ToolsModifierControl.cameraController.transform.position, true);
                BndResultHighlightManager.instance.Highlight(new HighlightData{NodeID = nextNodeId, Type = HighlightType.Node});
                ToolsModifierControl.cameraController.m_unlimitedCamera = unlimitedCamera;
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

        private void MoveNextPtLineButtonClick(UIComponent component, UIMouseEventParameter eventparam) {
            if (ModService.Instance.InvalidLines.Count == 0) {
                UpdateLinePanel();
                return;
            }

            List<ushort> keys = ModService.Instance.InvalidLines.Keys.ToList();
            if (keys.Count > 0) {
                _currentLine = keys.Find(s => s != _currentLine);
                _currentLine = _currentLine != 0 ? _currentLine : keys[0];
                _currentStop = 0;
            } else {
                _currentLine = 0;
                _currentStop = 0;
            }

            UpdatePtButtons();
            UpdateLinePanel();
        }

        private void MoveNextPtStopClick(UIComponent component, UIMouseEventParameter eventparam) {
            if (_currentLine == 0) {
                return;
            }

            LineInfo info = ModService.Instance.InvalidLines[_currentLine];
            info.RefreshInvalidStops();
            if (info.Stops.Count > 0) {
                _currentStop = info.Stops.Find(s => s != _currentStop);
                _currentStop = _currentStop != 0 ? _currentStop : info.Stops[0];
            } else {
                _currentStop = 0;
            }

            UpdatePtButtons();
            UpdateLinePanel();

            if (_currentStop == 0) return;
            Debug.Log("[BND] Moving to next not connected stop (" + _currentStop + ")");
            InstanceID instanceId = default;
            instanceId.NetNode = _currentStop;

            bool unlimitedCamera = ToolsModifierControl.cameraController.m_unlimitedCamera;
            ToolsModifierControl.cameraController.m_unlimitedCamera = true;
            ToolsModifierControl.cameraController.SetTarget(instanceId, ToolsModifierControl.cameraController.transform.position, true);
            BndResultHighlightManager.instance.Highlight(new HighlightData{NodeID = _currentStop, Type = HighlightType.PTStop});
            ToolsModifierControl.cameraController.m_unlimitedCamera = unlimitedCamera;
        }

        private void MoveNextSegmentButtonClick(UIComponent component, UIMouseEventParameter eventparam) {
            if (ModService.Instance.ShortSegments.Count == 0) {
                UpdateSegmentsPanel();
            }

            List<uint> keys = ModService.Instance.ShortSegments.Keys.ToList();
            if (keys.Count > 0) {
                _currentSegment = keys.Find(s => !_segmentsVisited.Contains(s));
                if (_currentSegment == 0) {
                    _currentSegment = keys[0];
                    _segmentsVisited.Clear();
                }
            } else {
                _currentSegment = 0;
                _segmentsVisited.Clear();
            }

            UpdateSegmentsButtons();
            UpdateSegmentsPanel();

            if (_currentSegment == 0) return;
            Debug.Log("[BND] Moving to next too short segment (" + _currentSegment + ")");
            InstanceID instanceId = default;
            instanceId.NetSegment = (ushort) _currentSegment;
            _segmentsVisited.Add(_currentSegment);

            bool unlimitedCamera = ToolsModifierControl.cameraController.m_unlimitedCamera;
            ToolsModifierControl.cameraController.m_unlimitedCamera = true;
            ToolsModifierControl.cameraController.SetTarget(instanceId, ToolsModifierControl.cameraController.transform.position, true);
            BndResultHighlightManager.instance.Highlight(new HighlightData{SegmentID = _currentSegment, Type = HighlightType.Segment});
            ToolsModifierControl.cameraController.m_unlimitedCamera = unlimitedCamera;
        } 
        private void MoveNextBuildingButtonClick(UIComponent component, UIMouseEventParameter eventparam) {
            if (ModService.Instance.DisconnectedBuildings.Count == 0) {
                UpdateBuildingsPanel();
            }

            List<uint> keys = ModService.Instance.DisconnectedBuildings.Keys.ToList();
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

            UpdateBuildingButtons();
            UpdateBuildingsPanel();

            if (_currentBuilding == 0) return;
            Debug.Log("[BND] Moving to next disconnected building (" + _currentSegment + ")");
            InstanceID instanceId = default;
            instanceId.Building = (ushort) _currentBuilding;
            _buildingsVisited.Add(_currentBuilding);

            bool unlimitedCamera = ToolsModifierControl.cameraController.m_unlimitedCamera;
            ToolsModifierControl.cameraController.m_unlimitedCamera = true;
            ToolsModifierControl.cameraController.SetTarget(instanceId, ToolsModifierControl.cameraController.transform.position, true);
            BndResultHighlightManager.instance.Highlight(new HighlightData{BuildingID = _currentBuilding, Type = HighlightType.Building});
            ToolsModifierControl.cameraController.m_unlimitedCamera = unlimitedCamera;
        }

        private void RemovePtLineClick(UIComponent component, UIMouseEventParameter eventparam) {
            if (_currentLine == 0) {
                return;
            }

            Debug.Log("[BND] Removing line [" + _currentLine + "](" + ModService.Instance.InvalidLines[_currentLine].Name + ") Line has " + ModService.Instance.InvalidLines[_currentLine].AllStops + " stops with " + ModService.Instance.InvalidLines[_currentLine].Stops.Count + " invalid");
            ShowConfirmDialog(
                "[BND] Remove Public Transport Line",
                $"Are you sure you want to remove line: {ModService.Instance.InvalidLines[_currentLine].Name}?",
                () => {
                    Singleton<SimulationManager>.instance.AddAction("Remove PT line", RemoveLine(_currentLine, () => {
                        Debug.Log("[BND] Line (" + _currentLine + ") removed successfully.");
                        ModService.Instance.InvalidLines.Remove(_currentLine);
                        _currentLine = 0;
                        _currentStop = 0;
                        UpdatePtButtons();
                        UpdateLinePanel();
                    }));
                });
        }

        private static void AssignButtonSprites(UIButton button) {
            button.normalBgSprite = "ButtonMenu";
            button.hoveredBgSprite = "ButtonMenuHovered";
            button.pressedBgSprite = "ButtonMenuPressed";
            button.disabledBgSprite = "ButtonMenuDisabled";
        }

        private IEnumerator RemoveLine(ushort lineId, Action onFinished) {
            TransportManager.instance.ReleaseLine(lineId);
            onFinished();
            yield return null;
        }

        private IEnumerator RemoveStop(ushort lineId, int stopIndex, Action onSuccess) {
            TransportLine line = TransportManager.instance.m_lines.m_buffer[lineId];
            bool success = line.RemoveStop(lineId, stopIndex);
            if (success) {
                onSuccess();
            }

            yield return null;
        }

        private void RemovePtStopClick(UIComponent component, UIMouseEventParameter eventparam) {
            if (_currentStop == 0 && _currentLine == 0) {
                return;
            }

            ShowConfirmDialog(
                "[BND] Remove Public Transport Stop",
                "Are you sure you want to remove that stop?",
                () => {
                    TransportLine line = TransportManager.instance.m_lines.m_buffer[_currentLine];
                    int stops = line.CountStops(_currentLine);
                    if (GetStopIndex(line, stops, _currentStop, out int stopIndex)) {
                        Debug.Log("[BND] Removing stop [" + _currentStop + "] line: " + _currentLine + " index: " + stopIndex + " Line has " + stops + " stops");


                        //async task on simulation thread 
                        AsyncTask a = Singleton<SimulationManager>.instance.AddAction("Remove PT stop", RemoveStop(_currentLine, stopIndex, () => {
                            _currentStop = 0;
                            if (stops == 1) {
                                ModService.Instance.InvalidLines.Remove(_currentLine);
                                _currentLine = 0;
                                Debug.Log("[BND] Last Stop (" + _currentStop + ") removed successfully. Removing lane (" + _currentLine + ") from invalid PT lines");
                            } else {
                                ModService.Instance.InvalidLines[_currentLine].RefreshInvalidStops();
                                if (ModService.Instance.InvalidLines[_currentLine].Stops.Count == 0) {
                                    ModService.Instance.InvalidLines.Remove(_currentLine);
                                    _currentLine = 0;
                                    Debug.Log("[BND] No more disconnected PT stops. Removing lane (" + _currentLine + ") from invalid PT lines");
                                }
                            }

                            UpdatePtButtons();
                            UpdateLinePanel();
                        }));
                    } else {
                        Debug.Log("[BND] Current PT stop (" + _currentStop + ") not found in line (" + _currentLine + ")!!!");
                        UpdatePtButtons();
                        UpdateLinePanel();
                    }
                });
        }

        private bool GetStopIndex(TransportLine line, int stopCount, ushort stopId, out int stopIndex) {
            for (int i = 0; i < stopCount; i++) {
                if (stopId == line.GetStop(i)) {
                    stopIndex = i;
                    return true;
                }
            }

            stopIndex = -1;
            return false;
        }

        private void OnBeforeStart() {
            _markedForRemoval.Clear();
            _brokenNodesLabel.relativePosition = new Vector2(95, 152);
            _brokenNodesLabel.Hide();
            _segmentsVisited.Clear();

            _mainPanel.height = 130;
            _startButton.Hide();
            _searchGhostNodesButton.Hide();
            _searchDisconnectedPtStopsButton.Hide();
            _searchDisconnectedBuildingsButton.Hide();
            _searchShortSegmentsButton.Hide();

            _progressBar.value = 0;
            _progressBar.Show();

            _moveNextButton.Hide();
            _moveNextTransportLineButton.Hide();
            _removePtLaneButton.Hide();
            _moveNextPtButton.Hide();
            _removePtStopButton.Hide();
            _moveNextBuildingButton.Hide();
            _linePanel.Hide();

            _segmentPanel.Hide();
            _moveNextSegButton.Hide();
            _buildingPanel.Hide();
            _currentSegment = 0;
            _currentStop = 0;
            _currentLine = 0;
            _currentBuilding = 0;
            
            if (_runningScan != null) {
                StopCoroutine(_runningScan);
                _runningScan = null;
            }
            if (_runningProgress != null) {
                StopCoroutine(_runningProgress);
                _runningProgress = null;
            }

            BndResultHighlightManager.instance.enabled = false;
        }

        private void OnAfterStop() {
            _startButton.Show();
            _searchGhostNodesButton.Show();
            _brokenNodesLabel.Show();
            _searchDisconnectedPtStopsButton.Show();
            _searchShortSegmentsButton.Show();
            _searchDisconnectedBuildingsButton.Show();

            _progressBar.Hide();
        }

        private void OnClose(bool resetOnly = false) {
            _mainPanel.height = 170;
            _brokenNodesLabel.relativePosition = new Vector2(95, 152);
            _brokenNodesLabel.text = "";
            _brokenNodesLabel.Hide();
            _moveNextButton.Hide();
            _moveNextTransportLineButton.Hide();
            _removePtLaneButton.Hide();
            _moveNextPtButton.Hide();
            _removePtStopButton.Hide();
            _moveNextBuildingButton.Hide();
            _linePanel.Hide();
            _buildingPanel.Hide();
            _currentStop = 0;
            _currentLine = 0;
            if (!resetOnly) {
                Hide();
            }
            if (_runningScan != null) {
                StopCoroutine(_runningScan);
                _runningScan = null;
            }
            if (_runningProgress != null) {
                StopCoroutine(_runningProgress);
                _runningProgress = null;
            }

            BndResultHighlightManager.instance.enabled = false;
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

        IEnumerator UpdateProgress(Action action) {
            _progressBar.minValue = 0f;
            _progressBar.maxValue = 1.0f;
            while (ModService.Instance.SearchInProgress) {
                UpdateProgressBarFloat(ModService.Instance.SearchProgress);
                yield return new WaitForEndOfFrame();
            }

            UpdateProgressBarFloat(1.0f);
            action();
        }

        private void UpdateProgressBar(int value) {
            float currentValue = _progressBar.maxValue - value + 1;
            _progressBar.value = currentValue;
        }

        private void UpdateProgressBarFloat(float value) {
            _progressBar.value = value;
        }

        private void ShowGhostNodesResult() {
            _mainPanel.height = 200;
            if (ModService.Instance.LastGhostNodesCount == 0) {
                _brokenNodesLabel.text = "Great! No ghost nodes found :-)";
                _brokenNodesLabel.relativePosition = new Vector3(55, 152);
            } else {
                _brokenNodesLabel.relativePosition = new Vector3(25, 152);
                _brokenNodesLabel.text = "Found and released " + ModService.Instance.LastGhostNodesCount + " ghost nodes :-)";
            }
        }

        private void ShowResults() {
            if (ModService.Instance.Results.Count == 0) {
                _brokenNodesLabel.text = "Great! Nothing found :-)";
                _mainPanel.height = 200;
                Debug.Log("[BND] Nothing found :-)");
            } else {
                _brokenNodesLabel.relativePosition = new Vector2(10, 152);
                _brokenNodesLabel.text = $"Found {ModService.Instance.Results.Count} possibly broken nodes\n" +
                                         "1. Click on 'Move next' to show node location\n" +
                                         "2. Move node or rebuild path segment\n" +
                                         "3. Repeat 1-2 until nothing new found\n" +
                                         "Run detector again if you want :)";
                _invalidNodes = ModService.Instance.Results;
                Debug.Log($"[BND] Found {_invalidNodes.Count} nodes. ({string.Join(",", _invalidNodes.Select(i => i.ToString()).ToArray())})");
                _invalidNodesEnumerator = _invalidNodes.GetEnumerator();
                _moveNextButton.Show();
                _mainPanel.height = 300;
            }
        }

        private void ShowPtResults() {
            if (ModService.Instance.InvalidLines.Count == 0) {
                _brokenNodesLabel.text = "Great! Nothing found :-)";
                _mainPanel.height = 200;
                Debug.Log("[BND] No invalid PT lines found :-)");
            } else {
                _brokenNodesLabel.relativePosition = new Vector2(10, 152);
                _brokenNodesLabel.text = $"Found {ModService.Instance.InvalidLines.Count} possibly broken PT line(s)\n";

                UpdateLinePanel();
                UpdatePtButtons();
                _mainPanel.height = 360;
            }
        }

        private void ShowShortSegmentsResult() {
            if (ModService.Instance.ShortSegments.Count == 0) {
                _brokenNodesLabel.text = "Great! Nothing found :-)";
                _mainPanel.height = 200;
                Debug.Log("[BND] Too short segments not detected :-)");
            } else {
                _brokenNodesLabel.relativePosition = new Vector2(10, 152);
                _brokenNodesLabel.text = $"Found {ModService.Instance.ShortSegments.Count} possibly too short segment(s)\n";

                UpdateSegmentsPanel();
                UpdateSegmentsButtons();
                _mainPanel.height = 330;
            }
        }
        
        
        private void ShowBuildingResult() {
            if (ModService.Instance.DisconnectedBuildings.Count == 0) {
                _brokenNodesLabel.text = "Great! Nothing found :-)";
                _mainPanel.height = 200;
                Debug.Log("[BND] Disconnected buildings not detected :-)");
            } else {
                _brokenNodesLabel.relativePosition = new Vector2(10, 152);
                _brokenNodesLabel.text = $"Found {ModService.Instance.ShortSegments.Count} disconnected building(s)\n";

                UpdateBuildingsPanel();
                UpdateBuildingButtons();
                _mainPanel.height = 290;
            }
        }

        private void UpdatePtButtons() {
            _moveNextTransportLineButton.Show();
            if (_currentLine != 0) {
                _removePtLaneButton.Show();

                if (ModService.Instance.InvalidLines.TryGetValue(_currentLine, out LineInfo info) && info.Stops.Count > 0) {
                    _moveNextPtButton.Show();
                    if (_currentStop != 0) {
                        _removePtStopButton.Show();
                    } else {
                        _removePtStopButton.Hide();
                    }
                } else {
                    _moveNextPtButton.Hide();
                    _currentStop = 0;
                    _removePtStopButton.Hide();
                }
            } else {
                _removePtLaneButton.Hide();
                _moveNextPtButton.Hide();
                _removePtStopButton.Hide();
            }
        }

        private void UpdateSegmentsButtons() {
            if (ModService.Instance.ShortSegments.Count > 0) {
                _moveNextSegButton.Show();
            } else {
                _moveNextSegButton.Hide();
            }
        }
        private void UpdateBuildingButtons() {
            if (ModService.Instance.DisconnectedBuildings.Count > 0) {
                _moveNextBuildingButton.Show();
            } else {
                _moveNextBuildingButton.Hide();
            }
        }

        private void UpdateLinePanel() {
            if (ModService.Instance.InvalidLines.Count == 0) {
                OnClose(true); //reset everything
                _brokenNodesLabel.Show();
                _brokenNodesLabel.text = "Great! Nothing found :-)";
                _mainPanel.height = 200;
                return;
            }

            _linePanel.Show();

            if (_currentLine != 0 && ModService.Instance.InvalidLines.TryGetValue(_currentLine, out LineInfo info)) {
                _lineId.text = info.Id.ToString();
                _lineId.suffix = " Info: " + info.PtInfo;
                _lineName.text = info.Name;
                _lineColor.selectedColor = info.Color;
                _brokenStopsNumber.prefix = "All stops: " + info.AllStops;
                _brokenStopsNumber.text = " Disconnected: " + info.Stops.Count;
            } else {
                _lineId.text = " ";
                _lineId.suffix = " ";
                _lineName.text = " ";
                _lineColor.selectedColor = Color.black;
                _brokenStopsNumber.prefix = "All stops: ";
                _brokenStopsNumber.text = " ";
            }

            _brokenNodesLabel.text = $"Found {ModService.Instance.InvalidLines.Count} possibly broken PT line(s)\n";
        }

        private void UpdateSegmentsPanel() {
            if (ModService.Instance.ShortSegments.Count == 0) {
                OnClose(true); //reset everything
                _brokenNodesLabel.Show();
                _brokenNodesLabel.text = "Great! Nothing found :-)";
                _mainPanel.height = 200;
                return;
            }

            _segmentPanel.Show();

            if (_currentSegment != 0 && ModService.Instance.ShortSegments.TryGetValue(_currentSegment, out SegmentInfo info)) {
                _segmentId.text = info.Id.ToString();
                _segmentId.suffix = " Pos: " + info.Position;
                _segmentType.text = info.Name;
                _segmentLength.prefix = "Length: ";
                _segmentLength.text = info.Length.ToString();
            } else {
                _segmentId.text = " ";
                _segmentId.suffix = " ";
                _segmentType.text = " ";
                _segmentLength.prefix = "Length: ";
                _segmentLength.text = " ";
            }

            _brokenNodesLabel.text = $"Found {ModService.Instance.ShortSegments.Count} possibly too short segments(s)\n";
        }
        
        private void UpdateBuildingsPanel() {
            if (ModService.Instance.DisconnectedBuildings.Count == 0) {
                OnClose(true); //reset everything
                _brokenNodesLabel.Show();
                _brokenNodesLabel.text = "Great! Nothing found :-)";
                _mainPanel.height = 200;
                return;
            }

            _buildingPanel.Show();

            if (_currentBuilding != 0 && ModService.Instance.DisconnectedBuildings.TryGetValue(_currentBuilding, out Vector3 position)) {
                _buildingId.text = _currentBuilding.ToString();
                _buildingId.suffix = " Pos: " + position;
            } else {
                _buildingId.text = " ";
                _buildingId.suffix = " ";
            }

            _brokenNodesLabel.text = $"Found {ModService.Instance.DisconnectedBuildings.Count} possibly disconnected building(s)\n";
        }

        private new void OnDestroy() {
            OnClose();
            _closeButton.eventClick -= CloseButtonClick;
            _startButton.eventClick -= StartButtonClick;
            _moveNextButton.eventClick -= MoveNextBrokeNodeButtonClick;
            _searchGhostNodesButton.eventClick -= GhostNodesButtonClick;
            _searchDisconnectedPtStopsButton.eventClick -= PTLineDetectorClick;
            _moveNextPtButton.eventClick -= MoveNextPtStopClick;
            _moveNextTransportLineButton.eventClick -= MoveNextPtLineButtonClick;
            _removePtStopButton.eventClick -= RemovePtStopClick;
            _removePtLaneButton.eventClick -= RemovePtLineClick;
            _moveNextBuildingButton.eventClick -= MoveNextBuildingButtonClick;
            _searchDisconnectedBuildingsButton.eventClick -= StartScanBuildingsClick;
            base.OnDestroy();
            if (_mainPanel != null) {
                Destroy(_mainPanel);
            }
        }

        public static UIButton CreateButton(UIComponent parent, string text, Rect relativePosSize, MouseEventHandler clickHandler) {
            UIButton button = parent.AddUIComponent<UIButton>();
            button.eventClick += clickHandler;
            button.relativePosition = relativePosSize.position;
            button.width = relativePosSize.width;
            button.height = relativePosSize.height;
            button.text = text;
            AssignButtonSprites(button);
            return button;
        }

        public static void ShowConfirmDialog(string title, string text, Action onConfirm) {
            ShowConfirmDialog(title, text, onConfirm, () => { });
        }

        public static void ShowConfirmDialog(string title, string text, Action onConfirm, Action onCancel) {
            ConfirmPanel.ShowModal(title, text, (_, result) => {
                if (result != 1) {
                    onCancel();
                } else {
                    onConfirm();
                }
            });
        }
    }
}