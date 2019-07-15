using System.Collections.Generic;
using ColossalFramework.UI;

namespace BrokenNodeDetector {
    public class ModService {
        public static ModService Instance { get; private set; }

        public bool IsWorking { get; private set; }

        private readonly Dictionary<ushort, uint> _nodeCalls;

        public List<ushort> Results { get; private set; }

        static ModService() {
            Instance = new ModService();
        }

        private ModService() {
            _nodeCalls = new Dictionary<ushort, uint>();
            Results = new List<ushort>();
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
        }
    }
}