using ColossalFramework.Math;
using UnityEngine;

namespace BrokenNodeDetector.Highlighter {
    public class BrokenNodeHighlight: IHighlightable {
        private HighlightData _data;
        public bool IsValid => IsValidData();
        public HighlightType Type => HighlightType.Node;
        
        public void SetHighlightData(HighlightData data) {
            _data = data;
        }

        public void Render(RenderManager.CameraInfo cameraInfo) {
            if (!IsValidData()) return;
            
            NetManager manager = NetManager.instance;
            for (int i = 0; i < 8; i++) {

                uint segmentId = manager.m_nodes.m_buffer[_data.NodeID].GetSegment(i);
                if (segmentId == 0)
                    continue;
                ref NetSegment segment = ref manager.m_segments.m_buffer[segmentId];
                Color color = Color.white;
                color.a = 0.8f;
                Bezier3 bezier;
                bezier.a = NetManager.instance.m_nodes.m_buffer[segment.m_startNode].m_position;
                bezier.d = NetManager.instance.m_nodes.m_buffer[segment.m_endNode].m_position;
                NetSegment.CalculateMiddlePoints(bezier.a, segment.m_startDirection, bezier.d, segment.m_endDirection, false, false, out bezier.b, out bezier.c);
                Bounds bounds = bezier.GetBounds();
                RenderManager.instance.OverlayEffect.DrawBezier(cameraInfo, color, bezier, segment.Info.m_halfWidth * 2f, -100f, -100f, bounds.min.y - 10f, bounds.max.y + 10f, false, true);
            }

            ref NetNode node = ref manager.m_nodes.m_buffer[_data.NodeID];
            Color nodeColor = Color.red;
            nodeColor.a = 0.8f;
            RenderManager.instance.OverlayEffect.DrawCircle(cameraInfo, nodeColor, node.m_position, node.m_bounds.size.x, node.m_position.y - 30f, node.m_position.y + 30f, true, false);
        }

        public bool IsValidData() {
            if (_data != null && _data.Type == HighlightType.Node && _data.NodeID != 0) {
                if ((NetManager.instance.m_nodes.m_buffer[_data.NodeID].m_flags & NetNode.Flags.Created) != 0) {
                    return true;
                }

                _data.Reset(Type);
            }
            
            return false;
        }
    }
}