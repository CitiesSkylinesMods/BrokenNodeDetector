using UnityEngine;

namespace BrokenNodeDetector.Highlighter {
    public class PTStopHighlight: IHighlightable {
        private HighlightData _data;
        public bool IsValid => IsValidData();
        public HighlightType Type => HighlightType.PTStop;

        public void SetHighlightData(HighlightData data) {
            _data = data;
        }

        public void Render(RenderManager.CameraInfo cameraInfo) {
            if (!IsValidData()) return;

            NetNode node = NetManager.instance.m_nodes.m_buffer[_data.NodeID];
            Color color = Color.red;
            color.a = 0.7f;
            RenderManager.instance.OverlayEffect.DrawCircle(cameraInfo, color, node.m_position, node.m_bounds.size.x, node.m_position.y - 20f, node.m_position.y + 20f, false, false);
        }

        private bool IsValidData() {
            if (_data != null && _data.Type == HighlightType.PTStop && _data.NodeID != 0) {
                if ((NetManager.instance.m_nodes.m_buffer[_data.NodeID].m_flags & NetNode.Flags.Created) != 0) {
                    return true;
                }

                _data.Reset(Type);
            }
            
            return false;
        }
    }
}