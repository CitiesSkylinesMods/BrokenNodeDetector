using ColossalFramework.Math;
using UnityEngine;

namespace BrokenNodeDetector.Highlighter {
    public class SegmentHighlight : IHighlightable {
        private HighlightData _data;
        public bool IsValid => IsValidData();
        public HighlightType Type => HighlightType.Segment;

        public void SetHighlightData(HighlightData data) {
            _data = data;
        }

        public void Render(RenderManager.CameraInfo cameraInfo) {
            if (!IsValidData()) return;
            
            NetManager netManager = NetManager.instance;
            ref NetSegment segment = ref netManager.m_segments.m_buffer[_data.SegmentID];
            Color color = Color.red;
            color.a = 0.7f;
            
            Bezier3 bezier;
            bezier.a = NetManager.instance.m_nodes.m_buffer[segment.m_startNode].m_position;
            bezier.d = NetManager.instance.m_nodes.m_buffer[segment.m_endNode].m_position;
            NetSegment.CalculateMiddlePoints(bezier.a, segment.m_startDirection, bezier.d, segment.m_endDirection, false, false, out bezier.b, out bezier.c);
            Bounds bounds = bezier.GetBounds();
            RenderManager.instance.OverlayEffect.DrawBezier(cameraInfo, color, bezier, segment.Info.m_halfWidth * 2f, -100f, -100f, bounds.min.y - 10f, bounds.max.y + 10f, false, true);
        }

        private bool IsValidData() {
            if (_data != null && _data.Type == HighlightType.Segment && _data.SegmentID != 0) {
                if ((NetManager.instance.m_segments.m_buffer[_data.SegmentID].m_flags & NetSegment.Flags.Created) != NetSegment.Flags.None) {
                    return true;
                }

                _data.Reset(Type);
            }
            
            return false;
        }
    }
}