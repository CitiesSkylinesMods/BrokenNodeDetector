using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using ColossalFramework.Threading;
using ColossalFramework.UI;
using UnityEngine;

namespace BrokenNodeDetector.UI.Tools.DisconnectedPublicTransportStopsTool {
    public class DisconnectedPublicTransportStops : Detector {
        private const string STOPS_NUMBER = "StopsNumber";
        private const string LINE_NAME = "LineName";
        private const string LINE_COLOR = "LineColor";
        private const string LINE_ID = "LineID";
        private const string LINE_PANEL = "LinePanel";
        private const string MOVE_NEXT = "MoveNext";
        private const string LABEL = "Label";
        private const string REMOVE_LINE = "RemoveLine";
        private const string FIND_NEXT = "FindNext";
        private const string REMOVE_STOP = "RemoveStop";
        private readonly Dictionary<ushort, LineInfo> InvalidLines = new Dictionary<ushort, LineInfo>();
        private ushort _currentLine;
        private ushort _currentStop;
        private volatile float _progress;

        public override string Name => "Find disconnected PT Stops";

        public override string Tooltip =>
            "Detects disconnected public transport stops\n" +
            "helps with removing, cycle through results";

        public DisconnectedPublicTransportStops() {
            BuildTemplate();
        }
        
        public override IEnumerable<float> Process() {
            IsProcessing = true;
            ResetState();

            _progress = 0;
            AsyncTask<float> asyncTask = SimulationManager.instance.AddAction(ProcessInternal());
            while (!asyncTask.completed) {
                yield return _progress;
            }

            yield return 1.0f;
            IsProcessing = true;
        }

        public IEnumerator<float> ProcessInternal() {
            TransportManager tm = TransportManager.instance;
            float searchStep = 1.0f / tm.m_lines.m_size;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("[BND] Searching for invalid lanes");
            sb.AppendLine("[BND] Stops with '*' are not properly connected to the line");
            sb.AppendLine("[BND] ========('Stop name')=========('Internal ID')========");
            sb.AppendLine();
            TransportLine[] mBuffer = tm.m_lines.m_buffer; //max 256
            for (int i = 0; i < tm.m_lines.m_size; i++) {
                sb.AppendLine("Processing " + i + " line...");

                if ((mBuffer[i].m_flags & TransportLine.Flags.Created) != 0) {
                    ushort lineNumber = (ushort)i;

                    AppendLineInfo(sb, lineNumber);
                    if (GetStopsInfo(sb, lineNumber)) {
                        LineInfo info = new LineInfo(lineNumber);
                        info.RefreshInvalidStops();
                        InvalidLines.Add(lineNumber, info);
                    }

                    sb.AppendLine("---------------------------------------");
                }

                float searchProgress = searchStep * i;
                if (i % 2 == 0) {
                    ProgressMessage = $"Processing...{searchProgress * 100:F0}%";
                    Thread.Sleep(4);
                    _progress = searchProgress;
                }
            }


            Debug.Log("[BND] Invalid lanes number: " + InvalidLines.Keys.Count);
            Debug.Log("[BND] Search report\n" + sb + "\n\n=======================================================");
            
            Debug.Log("[BND] Testing nodes one-by-one...");
            NetNode[] nodes = NetManager.instance.m_nodes.m_buffer;
            for (int i = 0; i < nodes.Length; i++) 
            {
                if ((nodes[i].m_flags & NetNode.Flags.Created) != 0 && (nodes[i].m_problems.m_Problems1 & Notification.Problem1.LineNotConnected) != 0)
                {
                    Debug.Log($"[BND] Broken node {i}, info: {nodes[i].Info?.name}, lane: {nodes[i].m_lane} flags: [{nodes[i].m_flags}]," +
                        $" segments: [{string.Join(",", Enumerable.Range(0,8).Select(n => nodes[i].GetSegment(n)).Where(n => n != 0).Select(n => $"[{n}: s: {NetManager.instance.m_segments.m_buffer[n].m_startNode}, e: {NetManager.instance.m_segments.m_buffer[n].m_endNode}]").ToArray())}]");
                }
            }
            yield return 1.0f;
        }

        private static void AppendLineInfo(StringBuilder sb, ushort lineNumber) {
            ref TransportLine line = ref TransportManager.instance.m_lines.m_buffer[lineNumber];
            sb.AppendLine("===(" + TransportManager.instance.GetLineName(lineNumber) + ")========(" + lineNumber + ")========")
                .Append("Line ")
                .Append(" " + line.Info.m_class.name + " ")
                .Append(line.m_flags.ToString()).Append(" [building: ").Append(line.m_building).Append("] line number: " + line.m_lineNumber)
                .AppendLine(" length: " + line.m_totalLength);
        }

        private static bool GetStopsInfo(StringBuilder sb, ushort lineNumber) {
            ref TransportLine line = ref TransportManager.instance.m_lines.m_buffer[lineNumber];
            bool allConnected = true;
            ushort firstId = line.m_stops;
            ref NetNode node = ref NetManager.instance.m_nodes.m_buffer[firstId];
            allConnected = allConnected && (node.m_problems.m_Problems1 & Notification.Problem1.LineNotConnected) == 0;
            AppendStopInfo(sb, ref node, firstId);

            if (firstId != 0) {
                ushort next = TransportLine.GetPrevStop(firstId);

                while (next != 0 && firstId != next) {
                    NetNode nextNode = NetManager.instance.m_nodes.m_buffer[next];
                    allConnected = allConnected && (nextNode.m_problems.m_Problems1 & Notification.Problem1.LineNotConnected) == 0;
                    AppendStopInfo(sb, ref nextNode, next);
                    next = TransportLine.GetPrevStop(next);
                }
            } else {
                sb.AppendLine("No stops");
                allConnected = false;
            }

            return !allConnected;
        }

        private static void AppendStopInfo(StringBuilder sb, ref NetNode node, ushort nodeId) {
            sb.Append((node.m_problems.m_Problems1 & Notification.Problem1.LineNotConnected) != 0 ? " * " : "   ")
                .Append("Id: ").Append(nodeId)
                .Append(" Flags: (").Append(node.m_flags.ToString())
                .Append(") Building ID: [").Append(node.m_building)
                .Append("] Class: ")
                .AppendLine(node.Info.m_class.name);
        }

        private void ResetState() {
            InvalidLines.Clear();
            _currentLine = 0;
            _currentStop = 0;
        }

        public override void InitResultsView(UIComponent component) {
            if (component is UIPanel panel) {
                AttachCallbacks(component);
                UILabel label = panel.Find<UILabel>(LABEL); 
                label.textScale = 1.2f;
                if (InvalidLines.Count == 0) {
                    label.text = "Great! Nothing found :-)";
                    component.height = 50;
                    Debug.Log("[BND] No invalid PT lines found :-)");
                } else {
                    // label.relativePosition = new Vector2(20, 0);
                    label.text = $"{InvalidLines.Count} possibly broken PT line(s)\n";
                    label.color = Color.yellow;

                    UIPanel linePanel = component.Find<UIPanel>(LINE_PANEL);
                    UpdatePtButtons(linePanel);
                    UpdateLinePanel(component.Find<UILabel>(LABEL), linePanel);
                    component.height = 200;
                }
            }
        }

        private void BuildTemplate() {
            _template = new GameObject("DisconnectedPTStopTemplate").AddComponent<UIPanel>();
            _template.transform.SetParent(_defaultGameObject.transform, true);
            _template.width = 400;
            _template.height = 50;
            UILabel label = _template.AddUIComponent<UILabel>();
            label.autoSize = true;
            label.padding = new RectOffset(15, 10, 15, 15);
            label.relativePosition = new Vector3(15, 0);
            label.name = LABEL;
            UIButton moveNextButton = UIHelpers.CreateButton(
                _template,
                "Find next broken PT Line",
                new Rect(new Vector2(10, 50f),
                    new Vector2(235, 32)),
                MoveNextPtLineButtonClick);
            moveNextButton.name = MOVE_NEXT;
            moveNextButton.Hide();

            UIButton removePtLaneButton = UIHelpers.CreateButton(
                _template,
                "Remove Line",
                new Rect(new Vector2(255, 50f),
                    new Vector2(135, 32)),
                RemovePtLineClick);
            removePtLaneButton.name = REMOVE_LINE;
            removePtLaneButton.Hide();

            UIButton moveNextPtButton = UIHelpers.CreateButton(
                _template,
                "Find next disconnected stop",
                new Rect(new Vector2(10, 165f),
                    new Vector2(235, 32)),
                MoveNextPtStopClick);
            moveNextPtButton.name = FIND_NEXT;
            moveNextPtButton.Hide();

            UIButton removePtStopButton = UIHelpers.CreateButton(
                _template,
                "Remove",
                new Rect(new Vector2(255, 165f),
                    new Vector2(135, 32)),
                RemovePtStopClick);
            removePtStopButton.name = REMOVE_STOP;
            removePtStopButton.Hide();

            UIPanel linePanel = _template.AddUIComponent<UIPanel>();
            linePanel.width = 390;
            linePanel.height = 80;
            linePanel.relativePosition = new Vector2(10, 85);
            linePanel.name = LINE_PANEL;

            UILabel lineId = linePanel.AddUIComponent<UILabel>();
            lineId.prefix = "Line ID: ";
            lineId.relativePosition = new Vector2(10, 5);
            lineId.name = LINE_ID;

            UIColorField lineColor = linePanel.AddUIComponent<UIColorField>();
            lineColor.selectedColor = Color.black;
            lineColor.relativePosition = new Vector2(355, 0);
            lineColor.normalFgSprite = "ColorPickerColor";
            lineColor.width = 25;
            lineColor.height = 25;
            lineColor.name = LINE_COLOR;

            UILabel lineName = linePanel.AddUIComponent<UILabel>();
            lineName.prefix = "Line name: ";
            lineName.relativePosition = new Vector2(10, 30);
            lineName.name = LINE_NAME;

            UILabel brokenStopsNumber = linePanel.AddUIComponent<UILabel>();
            brokenStopsNumber.prefix = "All stops: N/A";
            brokenStopsNumber.relativePosition = new Vector2(10, 58);
            brokenStopsNumber.name = STOPS_NUMBER;

            linePanel.Hide();
        }

        private void AttachCallbacks(UIComponent templateInstance) {
            UIButton moveNextPtButton = templateInstance.Find<UIButton>(FIND_NEXT);
            UIButton moveNextTransportLineButton = templateInstance.Find<UIButton>(MOVE_NEXT);
            UIButton removePtLaneButton = templateInstance.Find<UIButton>(REMOVE_LINE);
            UIButton removePtStopButton = templateInstance.Find<UIButton>(REMOVE_STOP);
            moveNextPtButton.eventClick += MoveNextPtStopClick;
            moveNextTransportLineButton.eventClick += MoveNextPtLineButtonClick;
            removePtLaneButton.eventClick += RemovePtLineClick;
            removePtStopButton.eventClick += RemovePtStopClick;
        } 
        
        private void MoveNextPtLineButtonClick(UIComponent component, UIMouseEventParameter eventparam) {
            UIPanel linePanel = component.parent.Find<UIPanel>(LINE_PANEL);
            UILabel label = component.parent.Find<UILabel>(LABEL);
            if (InvalidLines.Count == 0) {
                UpdateLinePanel(label, linePanel);
                return;
            }

            List<ushort> keys = InvalidLines.Keys.ToList();
            if (keys.Count > 0) {
                _currentLine = keys.Find(s => s != _currentLine);
                _currentLine = _currentLine != 0 ? _currentLine : keys[0];
                _currentStop = 0;
            } else {
                _currentLine = 0;
                _currentStop = 0;
            }

            UpdatePtButtons(linePanel);
            UpdateLinePanel(label, linePanel);
        }

        private void UpdateLinePanel(UILabel label, UIPanel linePanel) {
            if (InvalidLines.Count == 0) {
                ResetState(); //reset everything
                label.Show();
                label.text = "Great! Nothing found :-)";
                label.parent.height = 50;
                label.color = Color.white;
                return;
            }

            linePanel.Show();

            UILabel lineId = linePanel.Find<UILabel>(LINE_ID);
            UIColorField lineColor = linePanel.Find<UIColorField>(LINE_COLOR);
            UILabel lineName = linePanel.Find<UILabel>(LINE_NAME);
            UILabel brokenStopsNumber = linePanel.Find<UILabel>(STOPS_NUMBER);

            if (_currentLine != 0 && InvalidLines.TryGetValue(_currentLine, out LineInfo info)) {
                lineId.text = info.Id.ToString();
                lineId.suffix = " Info: " + info.PtInfo;
                lineName.text = info.Name;
                lineColor.selectedColor = info.Color;
                brokenStopsNumber.prefix = "All stops: " + info.AllStops;
                brokenStopsNumber.text = " Disconnected: " + info.Stops.Count;
            } else {
                lineId.text = " ";
                lineId.suffix = " ";
                lineName.text = " ";
                lineColor.selectedColor = Color.black;
                brokenStopsNumber.prefix = "All stops: ";
                brokenStopsNumber.text = " ";
            }

            label.text = $"Found {InvalidLines.Count} possibly broken PT line(s)\n";
            label.color = Color.yellow;
        }

        private void UpdatePtButtons(UIPanel linePanel) {
            UIButton moveNextTransportLineButton = linePanel.parent.Find<UIButton>(MOVE_NEXT);
            UIButton removePtLaneButton = linePanel.parent.Find<UIButton>(REMOVE_LINE);
            UIButton moveNextPtButton = linePanel.parent.Find<UIButton>(FIND_NEXT);
            UIButton removePtStopButton = linePanel.parent.Find<UIButton>(REMOVE_STOP);

            moveNextTransportLineButton.Show();
            if (_currentLine != 0) {
                removePtLaneButton.Show();

                if (InvalidLines.TryGetValue(_currentLine, out LineInfo info) && info.Stops.Count > 0) {
                    moveNextPtButton.Show();
                    linePanel.parent.height = 230;
                    if (_currentStop != 0) {
                        removePtStopButton.Show();
                    } else {
                        removePtStopButton.Hide();
                    }
                } else {
                    linePanel.parent.height = 50;
                    moveNextPtButton.Hide();
                    _currentStop = 0;
                    removePtStopButton.Hide();
                }
            } else {
                linePanel.parent.height = 50;
                removePtLaneButton.Hide();
                moveNextPtButton.Hide();
                removePtStopButton.Hide();
            }
        }

        private void RemovePtLineClick(UIComponent component, UIMouseEventParameter eventparam) {
            if (_currentLine == 0) {
                return;
            }

            Debug.Log($"[BND] Removing line [{_currentLine}]({InvalidLines[_currentLine].Name})" +
                      $" Line has {InvalidLines[_currentLine].AllStops} stops " +
                      $"with {InvalidLines[_currentLine].Stops.Count} invalid");
            UIHelpers.ShowConfirmDialog(
                "[BND] Remove Public Transport Line",
                $"Are you sure you want to remove line: {InvalidLines[_currentLine].Name}?",
                () => {
                    SimulationManager.instance
                        .AddAction(
                            "Remove PT line",
                            RemoveLine(_currentLine, () => {
                                Debug.Log($"[BND] Line ({_currentLine}) removed successfully.");
                                InvalidLines.Remove(_currentLine);
                                _currentLine = 0;
                                _currentStop = 0;
                                ThreadHelper.dispatcher.Dispatch(() => {
                                    UIPanel linePanel = component.parent.Find<UIPanel>(LINE_PANEL);
                                    UpdatePtButtons(linePanel);
                                    UpdateLinePanel(component.parent.Find<UILabel>(LABEL), linePanel);
                                });
                            }));
                });
        }

        private IEnumerator RemoveLine(ushort lineId, Action onFinished) {
            TransportManager.instance.ReleaseLine(lineId);
            onFinished();
            yield return null;
        }

        private void MoveNextPtStopClick(UIComponent component, UIMouseEventParameter eventparam) {
            if (_currentLine == 0) {
                return;
            }

            LineInfo info = InvalidLines[_currentLine];
            info.RefreshInvalidStops();
            if (info.Stops.Count > 0) {
                _currentStop = info.Stops.Find(s => s != _currentStop);
                _currentStop = _currentStop != 0 ? _currentStop : info.Stops[0];
            } else {
                _currentStop = 0;
            }

            UIPanel linePanel = component.parent.Find<UIPanel>(LINE_PANEL);
            UpdatePtButtons(linePanel);
            UpdateLinePanel(component.parent.Find<UILabel>(LABEL), linePanel);

            if (_currentStop == 0) return;
            Debug.Log("[BND] Moving to next not connected stop (" + _currentStop + ")");
            InstanceID instanceId = default;
            instanceId.NetNode = _currentStop;

            bool unlimitedCamera = ToolsModifierControl.cameraController.m_unlimitedCamera;
            ToolsModifierControl.cameraController.m_unlimitedCamera = true;
            ToolsModifierControl.cameraController.SetTarget(instanceId, ToolsModifierControl.cameraController.transform.position, true);
            BndResultHighlightManager.instance.Highlight(new HighlightData { NodeID = _currentStop, Type = HighlightType.PTStop });
            ToolsModifierControl.cameraController.m_unlimitedCamera = unlimitedCamera;
            ToolsModifierControl.cameraController.ClearTarget();
        }

        private void RemovePtStopClick(UIComponent component, UIMouseEventParameter eventparam) {
            if (_currentStop == 0 && _currentLine == 0) {
                return;
            }

            UIHelpers.ShowConfirmDialog(
                "[BND] Remove Public Transport Stop",
                "Are you sure you want to remove that stop?",
                () => {
                    TransportLine line = TransportManager.instance.m_lines.m_buffer[_currentLine];
                    int stops = line.CountStops(_currentLine);
                    if (GetStopIndex(line, stops, _currentStop, out int stopIndex)) {
                        Debug.Log($"[BND] Removing stop [{_currentStop}] line: {_currentLine} index: {stopIndex} Line has {stops} stops");

                        //async task on simulation thread 
                        SimulationManager.instance.AddAction("Remove PT stop", RemoveStop(_currentLine, stopIndex, () => {
                            _currentStop = 0;
                            if (stops == 1) {
                                InvalidLines.Remove(_currentLine);
                                _currentLine = 0;
                                Debug.Log($"[BND] Last Stop ({_currentStop}) removed successfully. Removing lane ({_currentLine}) from invalid PT lines");
                            } else {
                                InvalidLines[_currentLine].RefreshInvalidStops();
                                if (InvalidLines[_currentLine].Stops.Count == 0) {
                                    InvalidLines.Remove(_currentLine);
                                    _currentLine = 0;
                                    Debug.Log($"[BND] No more disconnected PT stops. Removing lane ({_currentLine}) from invalid PT lines");
                                }
                            }

                            ThreadHelper.dispatcher.Dispatch(() => {
                                UIPanel linePanel = component.parent.Find<UIPanel>(LINE_PANEL);
                                UpdatePtButtons(linePanel);
                                UpdateLinePanel(component.parent.Find<UILabel>(LABEL), linePanel);
                            });
                        }));
                    } else {
                        Debug.Log($"[BND] Current PT stop ({_currentStop}) not found in line ({_currentLine})!!!");

                        UIPanel linePanel = component.parent.Find<UIPanel>(LINE_PANEL);
                        UpdatePtButtons(linePanel);
                        UpdateLinePanel(component.parent.Find<UILabel>(LABEL), linePanel);
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

        private IEnumerator RemoveStop(ushort lineId, int stopIndex, Action onSuccess) {
            TransportLine line = TransportManager.instance.m_lines.m_buffer[lineId];
            bool success = line.RemoveStop(lineId, stopIndex);
            if (success) {
                onSuccess();
            }

            yield return null;
        }
    }
}