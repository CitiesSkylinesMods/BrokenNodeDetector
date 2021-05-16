using System.Collections.Generic;
using BrokenNodeDetector.Highlighter;
using UnityEngine;

namespace BrokenNodeDetector {
    public class BndResultHighlightManager: SimulationManagerBase<BndResultHighlightManager, ResultHighlightProperties>, IRenderableManager {
        private static bool _instantiated;

        private static BndResultHighlightManager _instance;

        private IHighlightable _highlightable;

        private Dictionary<HighlightType, IHighlightable> _highlightables = new Dictionary<HighlightType, IHighlightable>();
        private void Awake() {
            _highlightables = new Dictionary<HighlightType, IHighlightable> {
                {HighlightType.Building, new BuildingHighlight()},
                {HighlightType.Segment, new SegmentHighlight()},
                {HighlightType.PTStop, new PTStopHighlight()},
                {HighlightType.Node, new BrokenNodeHighlight()}
            };
            _instantiated = true;
        }

        private void OnDestroy() {
            _instance = null;
            _instantiated = false;
            _highlightables.Clear();
        }

        private void OnDisable() {
            _highlightable?.SetHighlightData(default);
        }

        public void Highlight(HighlightData data) {
            if (data.Type == HighlightType.Unknown) return;
            
            Debug.Log($"Highlighting " + data);
            EnsureHighlighter(data);
            enabled = true;
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