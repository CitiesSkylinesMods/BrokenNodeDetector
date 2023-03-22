using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using BrokenNodeDetector.UI.Tools.BrokenNodesTool;
using BrokenNodeDetector.UI.Tools.BrokenPathTool;
#if BROKEN_PROPS_SCANNER
using BrokenNodeDetector.UI.Tools.BrokenPropsTool;
#endif
using BrokenNodeDetector.UI.Tools.DisconnectedBuildingsTool;
using BrokenNodeDetector.UI.Tools.DisconnectedPublicTransportStopsTool;
using BrokenNodeDetector.UI.Tools.StuckCimsTool;
using BrokenNodeDetector.UI.Tools.GhostNodesTool;
using BrokenNodeDetector.UI.Tools.ShortSegmentsTool;
#if BROKEN_PROPS_SCANNER
using EManagersLib.API;
#endif
#if SEGMENT_UPDATER
using BrokenNodeDetector.UI.Tools.SegmentUpdateTool;
#endif
using UnityEngine;

namespace BrokenNodeDetector.UI.Tools {
    public class DetectorFactory : IDisposable {
        private List<Detector> _detectors;
        
        public DetectorFactory() {           
#if BROKEN_PROPS_SCANNER
            PropAPI.Initialize();
#endif
            _detectors = new List<Detector> {
                new BrokenNodes(),
                new GhostNodes(),
                new ShortSegments(),
                new DisconnectedBuildings(),
                new DisconnectedPublicTransportStops(),
#if SEGMENT_UPDATER
                // new SegmentUpdateRequest(),
#endif
#if BROKEN_PROPS_SCANNER
                (PropAPI.m_isEMLInstalled ? (Detector)new BrokenPropsEML() : new BrokenProps()),
#endif
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
            Detector.DisposeDefaultGameObject();
        }
    }
}