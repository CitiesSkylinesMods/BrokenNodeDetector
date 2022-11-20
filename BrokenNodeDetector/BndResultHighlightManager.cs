using System;
using System.Collections.Generic;
using System.Reflection;
using BrokenNodeDetector.Highlighter;
using ColossalFramework;
using ColossalFramework.Math;
using UnityEngine;

namespace BrokenNodeDetector {
    public class BndResultHighlightManager: SimulationManagerBase<BndResultHighlightManager, ResultHighlightProperties>, IRenderableManager, ISimulationManager {
        private static bool _instantiated;

        private IHighlightable _highlightable;
        private HighlightData _hoverData;
        private Camera _mainCamera;
        private Ray _mouseRay;
        private Vector3 _rayRight;
        private bool _rayValid;
        private float _mouseRayLength;
        private ushort hoveredBuildingId;

        private Dictionary<HighlightType, IHighlightable> _highlightables = new Dictionary<HighlightType, IHighlightable>();
        protected override void Awake() {
            base.Awake();
            name = "BND_ResultHighlightManager";
            _mainCamera = Camera.main;
            _highlightables = new Dictionary<HighlightType, IHighlightable> {
                {HighlightType.Building, new BuildingHighlight()},
                {HighlightType.Segment, new SegmentHighlight()},
                {HighlightType.PTStop, new PTStopHighlight()},
                {HighlightType.Node, new BrokenNodeHighlight()},
                {HighlightType.Citizen, new CitizenHighlight()}
            };
            _instantiated = true;
        }

        private void OnDestroy() {
            _instantiated = false;
            _highlightable = null;
            _hoverData = null;
            _mainCamera = null;
            _highlightables.Clear();
            GetType().GetField("sInstance", BindingFlags.NonPublic | BindingFlags.Static)
                ?.SetValue(null, null);
        }

        private void OnDisable() {
            _highlightable?.SetHighlightData(default);
        }

        public void Highlight(HighlightData data) {
            if (data.Type == HighlightType.Unknown) return;
            
            EnsureHighlighter(data);
            enabled = true;
        }

        protected override void SimulationStepImpl(int subStep) {
            base.SimulationStepImpl(subStep);
            if (_hoverData != null && _hoverData.Type == HighlightType.Building && _rayValid) {
                ToolBase.RaycastInput input = new ToolBase.RaycastInput(_mouseRay, _mouseRayLength) {
                    m_rayRight = _rayRight,
                    m_netService = new ToolBase.RaycastService(ItemClass.Service.None, ItemClass.SubService.None, ItemClass.Layer.Default)
                };
                input.m_buildingService = input.m_netService;
                input.m_ignoreBuildingFlags = Building.Flags.None;
                ToolBase.RaycastOutput raycastOutput;
                if (RayCast(input, out raycastOutput)) {
                    hoveredBuildingId = raycastOutput.m_building;
                }
                
                _hoverData.BuildingID = raycastOutput.m_building;
            }
        }

        private bool RayCast(ToolBase.RaycastInput input, out ToolBase.RaycastOutput output) {
            Vector3 origin = input.m_ray.origin;
            Vector3 normalized = input.m_ray.direction.normalized;
            Vector3 b = input.m_ray.origin + normalized * input.m_length;
            Segment3 ray = new Segment3(origin, b);
            output.m_hitPos = b;
            output.m_overlayButtonIndex = 0;
            output.m_netNode = 0;
            output.m_netSegment = 0;
            output.m_building = 0;
            output.m_propInstance = 0;
            output.m_treeInstance = 0U;
            output.m_vehicle = 0;
            output.m_parkedVehicle = 0;
            output.m_citizenInstance = 0;
            output.m_transportLine = 0;
            output.m_transportStopIndex = 0;
            output.m_transportSegmentIndex = 0;
            output.m_district = 0;
            output.m_park = 0;
            output.m_disaster = 0;
            output.m_currentEditObject = false;
            float len = input.m_length;
            Vector3 hit;
            bool found = false;
            if (input.m_ignoreBuildingFlags != Building.Flags.All && 
                Singleton<BuildingManager>.instance.RayCast(ray, input.m_buildingService.m_service, input.m_buildingService.m_subService, input.m_buildingService.m_itemLayers, input.m_ignoreBuildingFlags, out hit, out output.m_building))
            {
                float distance = Vector3.Distance(hit, origin);
                if (distance < (double) len)
                {
                    output.m_hitPos = hit;
                    output.m_netNode = 0;
                    output.m_netSegment = 0;
                    found = true;
                }
                else
                    output.m_building = 0;
            }

            return found;
        }

        private void Update() {
            if (Event.current.isMouse && Event.current.button == 0 &&
                _hoverData != null && _hoverData.Type == HighlightType.Building) {
                ModService.Instance.SelectedBuilding = hoveredBuildingId;
            }
        }

        private void LateUpdate() {
            Vector3 mousePosition = Input.mousePosition;
            _mouseRay = _mainCamera.ScreenPointToRay(mousePosition);
            _mouseRayLength = _mainCamera.farClipPlane;
            _rayRight = _mainCamera.transform.TransformDirection(Vector3.right);
            _rayValid = !ToolsModifierControl.toolController.IsInsideUI && Cursor.visible;
        }

        public void StartHoverHighlighter(HighlightData data, HighlightType type) {
            _hoverData = data;
            if (data.Type != type) return;
            EnsureHighlighter(data);
            enabled = true;
        }
        
        public void StopHoverHighlighter() {
            if (_hoverData == null) return;
            
            _hoverData = null;
            _highlightable = null;
        }

        private void EnsureHighlighter(HighlightData data) {
            if (_highlightable == null) {
                _highlightable = _highlightables[data.Type];
            } else {
                if (_highlightable.Type != data.Type) {
                    _highlightable.SetHighlightData(default);
                    _highlightable = _highlightables[data.Type];
                }
            }
            _highlightable.SetHighlightData(data);
        }

        protected override void BeginOverlayImpl(RenderManager.CameraInfo cameraInfo) {
            base.BeginOverlayImpl(cameraInfo);
            if (!IsInstanceValid()) return;

            _highlightable.Render(cameraInfo);
        }
        
        private bool IsInstanceValid() {
            return _highlightable != null && _highlightable.IsValid;
        }
    }
}