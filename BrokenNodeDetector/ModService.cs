using System.Collections.Generic;
using ColossalFramework.UI;

namespace BrokenNodeDetector {
    public class ModService {
        public static ModService Instance { get; private set; } = null;

        public bool IsWorking { get; private set; }

        private readonly Dictionary<ushort, uint> nodeCalls;

        public List<ushort> Results { get; private set; }

        static ModService() {
            Instance = new ModService();
        }

        private ModService() {
            nodeCalls = new Dictionary<ushort, uint>();
            Results = new List<ushort>();
        }

        public void StartDetector() {
            if (IsWorking) return;
            nodeCalls.Clear();
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

            if (nodeCalls.ContainsKey(nodeID)) {
                nodeCalls[nodeID] += 1;
            } else {
                nodeCalls.Add(nodeID, 1);
            }
        }

        private void AnalyzeData() {
            nodeCalls.ForEach((pair) => {
                if (pair.Value > 5) {
                    Results.Add(pair.Key);
                }
            });
        }
    }
}