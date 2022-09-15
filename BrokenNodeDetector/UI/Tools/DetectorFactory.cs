using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using BrokenNodeDetector.UI.Tools.BrokenNodesTool;
using BrokenNodeDetector.UI.Tools.BrokenPathTool;
using BrokenNodeDetector.UI.Tools.BrokenPropsTool;
using BrokenNodeDetector.UI.Tools.DisconnectedBuildingsTool;
using BrokenNodeDetector.UI.Tools.DisconnectedPublicTransportStopsTool;
using BrokenNodeDetector.UI.Tools.StuckCimsTool;
using BrokenNodeDetector.UI.Tools.GhostNodesTool;
using BrokenNodeDetector.UI.Tools.ShortSegmentsTool;
using EManagersLib.API;
#if SEGMENT_UPDATER
using BrokenNodeDetector.UI.Tools.SegmentUpdateTool;
#endif
using UnityEngine;

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
#if SEGMENT_UPDATER
                // new SegmentUpdateRequest(),
#endif
                (PropAPI.m_isEMLInstalled ? (Detector)new BrokenPropsEML() : new BrokenProps()),
                new StuckCims(),
                new BrokenPaths(),
            };
        }

        public ReadOnlyCollection<Detector> Detectors => new ReadOnlyCollection<Detector>(_detectors);

        public void Dispose() {
            for (var i = 0; i < Detectors.Count; i++) {
                try { 
                    Detectors[i].Dispose();
                } catch (Exception e) {
                    Debug.LogException(e);
                }
            }
            _detectors.Clear();
        }
    }
}