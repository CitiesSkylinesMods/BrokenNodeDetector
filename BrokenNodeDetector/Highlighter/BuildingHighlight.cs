using UnityEngine;

namespace BrokenNodeDetector.Highlighter {
    public class BuildingHighlight : IHighlightable {
        private HighlightData _data;
        public bool IsValid => IsValidData();
        public HighlightType Type => HighlightType.Building;

        public void SetHighlightData(HighlightData data) {
            _data = data;
        }

        public void Render(RenderManager.CameraInfo cameraInfo) {
            if (!IsValidData()) return;

            BuildingManager buildingManager = BuildingManager.instance;
            uint buildingId = _data.BuildingID;
            BuildingInfo info = buildingManager.m_buildings.m_buffer[buildingId].Info;
            int length = buildingManager.m_buildings.m_buffer[buildingId].Length;
            Vector3 position = buildingManager.m_buildings.m_buffer[buildingId].m_position;
            float angle = buildingManager.m_buildings.m_buffer[buildingId].m_angle;
            Color color = _data.AnimatedColor ? _data.CurrentColor : Color.red;
            color.a = 0.7f;
            BuildingTool.RenderOverlay(cameraInfo, info, length, position, angle, color, false);
        }

        private bool IsValidData() {
            if (_data != null && _data.Type == HighlightType.Building && _data.BuildingID != 0) {
                if ((BuildingManager.instance.m_buildings.m_buffer[_data.BuildingID].m_flags & Building.Flags.Created) != 0) {
                    return true;
                }

                _data.Reset(Type);
            }

            return false;
        }
    }
}