using UnityEngine;

namespace BrokenNodeDetector.Highlighter {
    public class CitizenHighlight: IHighlightable {
        private HighlightData _data;
        public bool IsValid => IsValidData();
        public HighlightType Type => HighlightType.Citizen;

        public void SetHighlightData(HighlightData data) {
            _data = data;
        }

        public void Render(RenderManager.CameraInfo cameraInfo) {
            if (!IsValidData()) return;

            ref CitizenInstance citizenInstance = ref CitizenManager.instance.m_instances.m_buffer[_data.CitizenInstanceID];
            Color color = Color.red;
            color.a = 0.7f;
            Vector3 pos = citizenInstance.GetLastFramePosition();
            RenderManager.instance.OverlayEffect.DrawCircle(cameraInfo, color, pos, 2, pos.y - 10f, pos.y + 10f, false, false);
        }

        private bool IsValidData() {
            if (_data != null && _data.Type == HighlightType.Citizen && _data.CitizenInstanceID != 0) {
                if ((CitizenManager.instance.m_instances.m_buffer[_data.CitizenInstanceID].m_flags & (CitizenInstance.Flags.Created | CitizenInstance.Flags.Character)) != 0) {
                    return true;
                }

                _data.Reset(Type);
            }
            
            return false;
        }
    }
}