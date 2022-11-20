using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using ColossalFramework.UI;
using UnityEngine;

namespace BrokenNodeDetector.UI.Tools.ShortSegmentsTool {
    public class ShortSegments : Detector {
        private const float MIN_PED_SEGMENT_LENGTH = 0.1f;
        private const float MIN_VEH_SEGMENT_LENGTH = 0.1f;
        private readonly HashSet<uint> _segmentsVisited = new HashSet<uint>();
        private readonly HashSet<string> _skipAssets = new HashSet<string> {"2131871143", "2131871948"};
        private readonly Dictionary<uint, SegmentInfo> _shortSegments = new Dictionary<uint, SegmentInfo>();
        private uint _currentSegment;
        private volatile float _progress;
        
        public ShortSegments() {
            BuildTemplate();
        }

        public override string Name => "Find too short segments";

        public override string Tooltip =>
            "Detects too short segments.\n" +
            "They may cause vehicles to stop for no reason or slow down simulation speed";

        public override IEnumerable<float> Process() {
            IsProcessing = true;
            _shortSegments.Clear();

            _progress = 0;
            AsyncTask<float> asyncTask = SimulationManager.instance.AddAction(ProcessInternal());
            while (!asyncTask.completed) {
                yield return _progress;
            }

            IsProcessing = true;
        }

        private IEnumerator<float> ProcessInternal() {
            ResetState();
            NetManager nm = NetManager.instance;
            float searchStep = 1.0f / nm.m_segments.m_size;
            
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("[BND] Searching for too short segments");
            sb.AppendLine("[BND] ");
            sb.AppendLine("[BND] ========('Segment type')=========('Internal ID')========");
            sb.AppendLine();
            NetSegment[] mBuffer = nm.m_segments.m_buffer;
            for (int i = 0; i < nm.m_segments.m_size; i++) {
                NetSegment segment = mBuffer[i];

                if ((mBuffer[i].m_flags & NetSegment.Flags.Created) != 0
                    && (mBuffer[i].m_flags & NetSegment.Flags.Untouchable) == 0
                    && mBuffer[i].Info
                    && mBuffer[i].Info.m_laneTypes != NetInfo.LaneType.None
                    && !_skipAssets.Contains(mBuffer[i].Info.name.Split('.')[0])
                ) {
                    sb.AppendLine("===("+nm.GetSegmentName((ushort)i) +")========(" + i + ")========");
                    sb.Append("segment ").Append(" " + (segment.Info ? segment.Info.m_class.name: "") + " ")
                    .Append(segment.m_flags.ToString()).Append(" [name: ").Append(segment.Info.name).Append("]")
                    .AppendLine(" length: " + segment.m_averageLength);
                    if (GetSegmentInfo(sb, ref segment, (uint)i)) {
                        SegmentInfo info = new SegmentInfo((uint)i);
                        _shortSegments.Add((uint)i, info);
                    }

                    sb.AppendLine("---------------------------------------");
                }

                float searchProgress = searchStep * i;
                if (i % 32 == 0) {
                    ProgressMessage = $"Processing...{searchProgress * 100:F0}%";
                    Thread.Sleep(1);
                    _progress = searchProgress;
                }
            }
            ProgressMessage = $"Processing...100%";

            Debug.Log($"[BND] Too short segment count: {_shortSegments.Count}");
            Debug.Log($"[BND] Search report\n{sb}\n\n=======================================================");
            yield return 1.0f;
        }

        private void ResetState() {
            _shortSegments.Clear();
            _segmentsVisited.Clear();
            _currentSegment = 0;
        }

        private bool GetSegmentInfo(StringBuilder sb, ref NetSegment segment, uint segmentId) {
            bool tooShort = ((segment.Info.m_class.m_service == ItemClass.Service.Beautification) && segment.m_averageLength < MIN_PED_SEGMENT_LENGTH)
                            || (segment.Info.m_class.m_service == ItemClass.Service.Road && segment.m_averageLength < MIN_VEH_SEGMENT_LENGTH);
            if (tooShort) {
                AppendSegmentInfo(sb, ref segment, segmentId);
            }
            
            return tooShort;
        }

        private static void AppendSegmentInfo(StringBuilder sb, ref NetSegment segment, uint segmentId) {
            sb.Append("Id: ").Append(segmentId)
                .Append(" Flags: (").Append(segment.m_flags.ToString())
                .Append(") Nodes: [").Append(segment.m_startNode).Append(";").Append(segment.m_startNode)
                .Append("] Class: ")
                .AppendLine(segment.Info ? segment.Info.m_class.name: "Info is null!");
        }

        public override void InitResultsView(UIComponent component) {
            if (component is UIPanel panel) {
                AttachCallbacks(component);
                UILabel label = panel.Find<UILabel>("Label");
                if (_shortSegments.Count == 0) {
                    label.textScale = 1.2f;
                    label.text = "Great! Nothing found :-)";
                    Debug.Log("[BND] Too short segments not detected :-)");
                } else {
                    label.textScale = 1.1f;
                    label.text = $"Found {_shortSegments.Count} possibly too short segment(s)\n";
                    label.color = Color.yellow;
                    component.height = 180;
                    UIPanel segmentsPanel = component.Find<UIPanel>("SegmentPanel");
                    UpdateSegmentsPanel(segmentsPanel, label);
                    UpdateSegmentsButtons(component.Find<UIButton>("MoveNext"));
                }
            }
        }

        private void BuildTemplate() {
            _template = new GameObject("ShortSegmentsTemplate").AddComponent<UIPanel>();
            _template.transform.SetParent(_defaultGameObject.transform, true);
            _template.width = 400;
            _template.height = 50;
            UILabel label = _template.AddUIComponent<UILabel>();
            label.autoSize = true;
            label.padding = new RectOffset(15, 10, 15, 15);
            label.relativePosition = new Vector3(0, 0);
            label.name = "Label";
            UIButton moveNextButton = UIHelpers.CreateButton(
                _template,
                "Move to next too short segment",
                new Rect(new Vector2(55, 140f),
                    new Vector2(290, 32)),
                MoveNextSegmentButtonClick);
            moveNextButton.name = "MoveNext";
            moveNextButton.Hide();
            
            UIPanel segmentPanel = _template.AddUIComponent<UIPanel>();
            segmentPanel.width = 390;
            segmentPanel.height = 80;
            segmentPanel.relativePosition = new Vector2(5, 50);
            segmentPanel.name = "SegmentPanel";

            UILabel segmentId = segmentPanel.AddUIComponent<UILabel>();
            segmentId.prefix = "Segment ID: ";
            segmentId.relativePosition = new Vector2(10, 5);
            segmentId.name = "SegmentID";

            UILabel segmentType = segmentPanel.AddUIComponent<UILabel>();
            segmentType.prefix = "Segment type: ";
            segmentType.relativePosition = new Vector2(10, 30);
            segmentType.name = "SegmentType";

            UILabel segmentLength = segmentPanel.AddUIComponent<UILabel>();
            segmentLength.prefix = "Length: N/A";
            segmentLength.relativePosition = new Vector2(10, 58);
            segmentLength.name = "SegmentLength";

            segmentPanel.Hide();
        }

        private void AttachCallbacks(UIComponent templateInstance) {
            UIButton moveNext = templateInstance.Find<UIButton>("MoveNext");
            moveNext.eventClick += MoveNextSegmentButtonClick;
        }

        private void MoveNextSegmentButtonClick(UIComponent component, UIMouseEventParameter eventparam) {
            UIPanel panel = component.parent.Find<UIPanel>("SegmentPanel");
            UILabel label = component.parent.Find<UILabel>("Label");
            
            if (_shortSegments.Count == 0) {
                UpdateSegmentsPanel(panel, label);
            }

            List<uint> keys = _shortSegments.Keys.ToList();
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

            UpdateSegmentsButtons(component.Find<UIButton>("MoveNext"));
            UpdateSegmentsPanel(panel, label);

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
            ToolsModifierControl.cameraController.ClearTarget();
        }

        private void UpdateSegmentsPanel(UIPanel segmentPanel, UILabel label) {
            if (_shortSegments.Count == 0) {
                // OnClose(true); //reset everything
                ResetState();
                label.Show();
                label.text = "Great! Nothing found :-)";
                label.parent.height = 50f;
                label.color = Color.white;
                return;
            }

            segmentPanel.Show();

            UILabel segmentId = segmentPanel.Find<UILabel>("SegmentID");
            UILabel segmentType = segmentPanel.Find<UILabel>("SegmentType");
            UILabel segmentLength = segmentPanel.Find<UILabel>("SegmentLength");

            if (_currentSegment != 0 && _shortSegments.TryGetValue(_currentSegment, out SegmentInfo info)) {
                segmentId.text = info.Id.ToString();
                segmentId.suffix = " Pos: " + info.Position;
                segmentType.text = info.Name;
                segmentLength.prefix = "Length: ";
                segmentLength.text = info.Length.ToString();
            } else {
                segmentId.text = " ";
                segmentId.suffix = " ";
                segmentType.text = " ";
                segmentLength.prefix = "Length: ";
                segmentLength.text = " ";
            }

            label.text = $"Found {_shortSegments.Count} possibly too short segment(s)\n";
            label.color = Color.yellow;
        }

        private void UpdateSegmentsButtons(UIButton moveNextButton) {
            if (_shortSegments.Count > 0) {
                moveNextButton.Show();
            } else {
                moveNextButton.Hide();
            }
        }
    }
}