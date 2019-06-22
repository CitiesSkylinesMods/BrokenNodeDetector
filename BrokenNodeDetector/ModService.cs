using System.Collections.Generic;
using ColossalFramework.UI;

namespace BrokenNodeDetector {
    public class ModService {
        public static ModService Instance { get; private set; }

        public bool IsWorking { get; private set; }

        private readonly Dictionary<ushort, uint> _nodeCalls;

        public List<ushort> Results { get; private set; }
//        public List<ushort> SegmentList { get; private set; }

        static ModService() {
            Instance = new ModService();
        }

        private ModService() {
            _nodeCalls = new Dictionary<ushort, uint>();
            Results = new List<ushort>();
//            SegmentList = new List<ushort>();
        }

        public void StartDetector() {
            if (IsWorking) return;
            _nodeCalls.Clear();
            Results.Clear();
            IsWorking = true;
        }

        public void StopDetector() {
            if (!IsWorking) return;

            IsWorking = false;
            AnalyzeData();
        }

        public void OnUpdateLaneConnection(ushort nodeID) {
            if (!IsWorking) {
                return;
            }

            if (_nodeCalls.ContainsKey(nodeID)) {
                _nodeCalls[nodeID] += 1;
            } else {
                _nodeCalls.Add(nodeID, 1);
            }
        }

        private void AnalyzeData() {
            _nodeCalls.ForEach((pair) => {
                if (pair.Value > 50) {
                    Results.Add(pair.Key);
                }
            });
//            CalculateSegments();
        }

//        /// <summary>
//        /// Calculates unique segments ids from list of Result node ids
//        /// </summary>
//        private void CalculateSegments() {
//            SegmentList.Clear();
//            int count = Results.Count;
//            ushort node1 = 0;
//            ushort node2 = 0;
//            for (int i = 0; i < count - 1; i++) {
//                for (int j = i + 1; j < count - 1; j++) {
//                    node1 = Results[i];
//                    node2 = Results[j];
//                    if (!NetManager.instance.m_nodes.m_buffer[node1].IsConnectedTo(node2)) continue;
//                    ushort currentSegment;
//                    for (int k = 0; k < 8; k++) {
//                        currentSegment = NetManager.instance.m_nodes.m_buffer[node1].GetSegment(k);
//                        
//                        if (currentSegment == 0) continue;
//                        
//                        if (node2 == NetManager.instance.m_segments.m_buffer[currentSegment].GetOtherNode(node1)) {
//                            SegmentList.Add(node2);
//                        }
//                    }
//                }
//            }
//        }
    }
}