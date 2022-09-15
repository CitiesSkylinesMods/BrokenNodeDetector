using System;
using UnityEngine;

namespace BrokenNodeDetector {
    public class ResultHighlightProperties : MonoBehaviour {
        
    }

    public class HighlightData {
        public uint BuildingID;
        public uint SegmentID;
        public uint NodeID;
        public uint CitizenInstanceID;
        public HighlightType Type;
        public bool AnimatedColor;
        public Color CurrentColor;

        public void Reset(HighlightType type) {
            BuildingID = 0;
            SegmentID = 0;
            NodeID = 0;
            CitizenInstanceID = 0;
            Type = type;
        }

        public override string ToString() {
            return $"B: {BuildingID} |S: {SegmentID} |N: {NodeID} |C: {CitizenInstanceID} |Type: {Type}";
        }
    }

    public enum HighlightType {
        Unknown,
        Segment,
        Node,
        PTStop,
        Building,
        Citizen,
    }
}