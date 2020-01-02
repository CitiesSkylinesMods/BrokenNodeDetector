using System.Collections;
using System.Collections.Generic;
using System.Text;
using ColossalFramework.UI;
using UnityEngine;

namespace BrokenNodeDetector {
    public class ModService {
        public static ModService Instance { get; private set; }
        public int LastGhostNodesCount { get; private set; }
        public bool IsWorking { get; private set; }

        private readonly Dictionary<ushort, uint> _nodeCalls;
        public List<ushort> Results { get; private set; }
        
        public Dictionary<ushort, LineInfo> InvalidLines = new Dictionary<ushort, LineInfo>();
        
        public float SearchProgress { get; private set; }
        public float SearchStep { get; private set; }
        public bool SearchInProgress { get; private set; }

        static ModService() {
            Instance = new ModService();
        }

        private ModService() {
            _nodeCalls = new Dictionary<ushort, uint>();
            Results = new List<ushort>();
        }

        public IEnumerator SearchForGhostNodes() {
            Debug.Log("[BND] Searching for ghost nodes");
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Ghost/broken nodes: ");
            LastGhostNodesCount = 0;
            for (var i = 0; i < NetManager.instance.m_nodes.m_buffer.Length; i++) {
                NetNode node = NetManager.instance.m_nodes.m_buffer[i];
                if (node.m_flags != 0
                    && (node.m_flags & NetNode.Flags.Untouchable) == 0
                    && node.CountSegments() == 0
                    && (node.m_flags & NetNode.Flags.Created) != 0) {
                    LastGhostNodesCount++;
                    sb.Append("[").Append(i).Append("] - ").Append(node.m_flags.ToString()).Append(" info: ").Append(node.Info.ToString()).AppendLine("]");
                    NetManager.instance.ReleaseNode((ushort) i);
                }
            }

            Debug.Log("[BND] Searching finished. Found and released " + LastGhostNodesCount + " ghost nodes");
            Debug.Log(sb);
            yield return null;
        }

        public IEnumerator SearchForDisconnectedPtStops() {
            SearchProgress = 0.0f;
            TransportManager tm = TransportManager.instance;
            SearchStep = 1.0f / tm.m_lines.m_buffer.Length;
            SearchInProgress = true;
            InvalidLines.Clear();

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("[BND] Searching for invalid lanes");
            sb.AppendLine("[BND] Stops with '*' are not properly connected to the line");
            sb.AppendLine("[BND] ========('Stop name')=========('Internal ID')========");
            sb.AppendLine();
            TransportLine[] mBuffer = tm.m_lines.m_buffer; //max 256
            for (int i = 0; i < mBuffer.Length; i++) {
                sb.AppendLine("Processing " + i + " line...");
                TransportLine line = mBuffer[i];
                
                if ((mBuffer[i].m_flags & TransportLine.Flags.Created) != 0) {
                    sb.AppendLine("===("+TransportManager.instance.GetLineName((ushort)i) +")========(" + i + ")========");
                    sb.Append("Line ").Append(" " + line.Info.m_class.name + " ")
                    .Append(line.m_flags.ToString()).Append(" [building: ").Append(line.m_building).Append("] line number: " + line.m_lineNumber)
                    .AppendLine(" length: " + line.m_totalLength);
                    if (GetStopsInfo(sb, line)) {
                        LineInfo info = new LineInfo((ushort)i);
                        info.RefreshInvalidStops();
                        InvalidLines.Add((ushort)i, info);
                    }

                    sb.AppendLine("---------------------------------------");
                }

                SearchProgress = SearchStep * i;
                if (i % 4 == 0) {
                    yield return null;
                }
            }

            SearchInProgress = false;
            Debug.Log("[BND] Invalid lanes number: " + InvalidLines.Keys.Count);
            Debug.Log("[BND] Search report\n" + sb + "\n\n=======================================================");
        }

        private static bool GetStopsInfo(StringBuilder sb, TransportLine line) {
            bool allConnected = true;
            ushort firstId = line.m_stops;
            ref NetNode node = ref NetManager.instance.m_nodes.m_buffer[firstId];
            allConnected = allConnected && (node.m_problems & Notification.Problem.LineNotConnected) == 0;
            AppendStopInfo(sb, ref node, firstId);

            if (firstId != 0) {
                ushort next = TransportLine.GetPrevStop(firstId);
                
                while (next != 0 && firstId != next) {
                    NetNode nextNode = NetManager.instance.m_nodes.m_buffer[next];
                    allConnected = allConnected && (nextNode.m_problems & Notification.Problem.LineNotConnected) == 0;
                    AppendStopInfo(sb, ref nextNode, next);
                    next = TransportLine.GetPrevStop(next);
                }
            } else {
                sb.AppendLine("No stops");
                allConnected = false;
            }

            return !allConnected;
        }

        private static void AppendStopInfo(StringBuilder sb, ref NetNode node, ushort nodeId) {
            sb.Append((node.m_problems & Notification.Problem.LineNotConnected) != 0 ? " * " : "   ")
                .Append("Id: ").Append(nodeId)
                .Append(" Flags: (").Append(node.m_flags.ToString())
                .Append(") Building ID: [").Append(node.m_building)
                .Append("] Class: ")
                .AppendLine(node.Info.m_class.name);
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

    public class LineInfo {
        public ushort Id { get; }
        public Color Color { get; }
        public string Name { get; }
        
        public string PtInfo { get; }

        public List<ushort> Stops { get; }
        
        public ushort AllStops { get; private set; }

        public LineInfo(ushort id) {
            Id = id;
            TransportManager tm = TransportManager.instance;
            Color = tm.GetLineColor(id);
            Name = tm.GetLineName(id);
            PtInfo = tm.m_lines.m_buffer[id].Info.GetUncheckedLocalizedTitle();
            Stops = new List<ushort>();
        }

        public void RefreshInvalidStops() {
            Stops.Clear();
            AllStops = 0;
            ushort last = TransportManager.instance.m_lines.m_buffer[Id].m_stops;
            if (last == 0) {
                return;
            }

            ushort next = TransportLine.GetPrevStop(last);
            while (next != 0 && last != next) {
                AllStops++;
                ref NetNode nextNode = ref NetManager.instance.m_nodes.m_buffer[next];
                if ((nextNode.m_problems & Notification.Problem.LineNotConnected) != 0) {
                    Stops.Add(next);
                }
                next = TransportLine.GetPrevStop(next);
            }
        }
    }
}