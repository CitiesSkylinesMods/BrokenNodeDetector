using System;
using UnityEngine;

namespace BrokenNodeDetector {
    public class ResultHighlightProperties : MonoBehaviour {
        
    }

    public class HighlightData {
        public uint BuildingID;
        public uint SegmentID;
        public uint NodeID;
        public HighlightType Type;

        public void Reset(HighlightType type) {
            BuildingID = 0;
            SegmentID = 0;
            NodeID = 0;
            Type = type;
        }

        public override string ToString() {
            return $"B: {BuildingID} |S: {SegmentID} |N: {NodeID} |Type: {Type}";
        }
    }

    public enum HighlightType {
        Unknown,
        Segment,
        Node,
        PTStop,
        Building,
    }
}