using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using BrokenNodeDetector.UI.Tools.BrokenNodesTool;
using BrokenNodeDetector.UI.Tools.BrokenPropsTool;
using BrokenNodeDetector.UI.Tools.DisconnectedBuildingsTool;
using BrokenNodeDetector.UI.Tools.DisconnectedPublicTransportStopsTool;
using BrokenNodeDetector.UI.Tools.GhostNodesTool;
using BrokenNodeDetector.UI.Tools.SegmentUpdateTool;
using BrokenNodeDetector.UI.Tools.ShortSegmentsTool;

namespace BrokenNodeDetector.UI.Tools {
    public class DetectorFactory : IDisposable {
        private List<Detector> _detectors;
        
        public DetectorFactory() {
            _detectors = new List<Detector> {
                new BrokenNodes(),
                new GhostNodes(),
                new ShortSegments(),
                new DisconnectedBuildings(),
                new DisconnectedPublicTransportStops(),
                new SegmentUpdateRequest(),
                new BrokenProps(),
            };
        }

        public ReadOnlyCollection<Detector> Detectors => new ReadOnlyCollection<Detector>(_detectors);

        public void Dispose() {
            for (var i = 0; i < Detectors.Count; i++) {
                Detectors[i].Dispose();
            }
            _detectors.Clear();
        }
    }
}