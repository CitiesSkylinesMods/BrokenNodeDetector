using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
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
        public Dictionary<uint, SegmentInfo> ShortSegments = new Dictionary<uint, SegmentInfo>();
        public Dictionary<uint, Vector3> DisconnectedBuildings = new Dictionary<uint, Vector3>();
        
        public float SearchProgress { get; private set; }
        public float SearchStep { get; private set; }
        public bool SearchInProgress { get; private set; }
        
        public bool KeybindEditInProgress { get; private set; }

        private float _minPedSegmentLength = 0.1f;
        private float _minVehSegmentLength = 0.1f;

        private HashSet<string> _skipAssets = new HashSet<string> {"2131871143", "2131871948"};
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
            sb.AppendLine("Ghost/broken nodes list: ");
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

            sb.AppendLine("=================================================");
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
        
        public IEnumerator SearchForShortSegments() {
            SearchProgress = 0.0f;
            NetManager nm = NetManager.instance;
            SearchStep = 1.0f / nm.m_segments.m_buffer.Length;
            SearchInProgress = true;
            ShortSegments.Clear();

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("[BND] Searching for too short segments");
            sb.AppendLine("[BND] ");
            sb.AppendLine("[BND] ========('Segment type')=========('Internal ID')========");
            sb.AppendLine();
            NetSegment[] mBuffer = nm.m_segments.m_buffer;
            for (int i = 0; i < mBuffer.Length; i++) {
                NetSegment segment = mBuffer[i];

                if ((mBuffer[i].m_flags & NetSegment.Flags.Created) != 0
                    && (mBuffer[i].m_flags & NetSegment.Flags.Untouchable) == 0
                    && mBuffer[i].Info
                    && mBuffer[i].Info.m_laneTypes != NetInfo.LaneType.None
                    && !_skipAssets.Contains(mBuffer[i].Info.name.Split('.')[0])
                ) {
                    sb.AppendLine("===("+nm.GetSegmentName((ushort)i) +")========(" + i + ")========");
                    sb.Append("segment ").Append(" " + (segment.Info ? segment.Info.m_class.name: "") + " ")
                    .Append(segment.m_flags.ToString()).Append(" [name: ").Append(segment.Info.name).Append("]")
                    .AppendLine(" length: " + segment.m_averageLength);
                    if (GetSegmentInfo(sb, ref segment, (uint)i)) {
                        SegmentInfo info = new SegmentInfo((uint)i);
                        ShortSegments.Add((uint)i, info);
                    }

                    sb.AppendLine("---------------------------------------");
                }

                SearchProgress = SearchStep * i;
                if (i % 128 == 0) {
                    yield return null;
                }
            }

            SearchInProgress = false;
            Debug.Log("[BND] Invalid lanes number: " + InvalidLines.Keys.Count);
            Debug.Log("[BND] Search report\n" + sb + "\n\n=======================================================");
        }
        
        public IEnumerator ScanForDisconnectedBuildings() {
            SearchProgress = 0.0f;
            BuildingManager bm = BuildingManager.instance;
            SearchStep = 1.0f / bm.m_buildings.m_buffer.Length;
            SearchInProgress = true;
            DisconnectedBuildings.Clear();

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("[BND] Scanning for disconnected buildings");
            sb.AppendLine("[BND] ");
            sb.AppendLine("[BND] =( Building ID )===( Location )=");
            sb.AppendLine();
            Building[] mBuffer = bm.m_buildings.m_buffer;
            int counter = 0;
            MethodInfo findRoadAccess = typeof(PlayerBuildingAI).GetMethod("FindRoadAccess", BindingFlags.Instance | BindingFlags.NonPublic);
            for (uint i = 0; i < mBuffer.Length; i++) {
                Building building = mBuffer[i];

                if ((building.m_flags & Building.Flags.Created) != 0 && building.Info) {
                    if (building.Info.m_buildingAI is PlayerBuildingAI) {
                        counter++;
                        PlayerBuildingAI buildingAI = building.Info.m_buildingAI as PlayerBuildingAI;
                        bool connected = true;
                        if ((building.m_flags & Building.Flags.Collapsed) == Building.Flags.None && buildingAI.RequireRoadAccess()) {
                            Vector3 position = buildingAI.m_info.m_zoningMode != BuildingInfo.ZoningMode.CornerLeft
                                ? (buildingAI.m_info.m_zoningMode != BuildingInfo.ZoningMode.CornerRight
                                    ? building.CalculateSidewalkPosition(0.0f, 4f)
                                    : building.CalculateSidewalkPosition(building.Width * -4f, 4f))
                                : building.CalculateSidewalkPosition(building.Width * 4f, 4f);
                            object[] args = {(ushort) i, building, position};
                            if (!(bool) findRoadAccess.Invoke(buildingAI, args))
                                connected = false;
                        }

                        if (!connected) {
                            DisconnectedBuildings.Add(i, building.m_position);
                            sb.AppendLine("==(" + i + ")===(" + building.m_position + ")=");
                            sb.Append("building ")
                                .Append(" " + (building.Info ? building.Info.m_class.name : "") + " ")
                                .Append(building.m_flags.ToString())
                                .Append(" [name: ").Append(building.Info.name).Append("]");
                            sb.AppendLine("---------------------------------------");
                        }
                    } else if ((building.m_flags & Building.Flags.RoadAccessFailed) != 0 || (building.m_problems & Notification.Problem.RoadNotConnected) != 0) {
                        DisconnectedBuildings.Add(i, building.m_position);
                        sb.AppendLine("==(" + i + ")===(" + building.m_position + ")=");
                        sb.Append("building ")
                            .Append(" " + (building.Info ? building.Info.m_class.name : "") + " ")
                            .Append(building.m_flags.ToString())
                            .Append(" [name: ").Append(building.Info.name).Append("]");
                        sb.AppendLine("---------------------------------------");
                    }
                }
                
                SearchProgress = SearchStep * i;
                if (i % 128 == 0) {
                    yield return null;
                }
            }

            SearchInProgress = false;
            Debug.Log("[BND] Disconnected building instances count: " + counter);
            Debug.Log("[BND] Scan report\n" + sb + "\n\n=======================================================");
        }
        
        // public IEnumerator ScanForDisconnectedBuildings() {
        //     SearchProgress = 0.0f;
        //     BuildingManager bm = BuildingManager.instance;
        //     SearchStep = 1.0f / bm.m_buildings.m_buffer.Length;
        //     SearchInProgress = true;
        //     ShortSegments.Clear();
        //
        //     StringBuilder sb = new StringBuilder();
        //     sb.AppendLine("[BND] Dumping Disconnected buildings");
        //     sb.AppendLine("[BND] ");
        //     sb.AppendLine("[BND] ========('Building ID')=========('Citizen ID')========('Count')========");
        //     sb.AppendLine();
            // CitizenManager cm = CitizenManager.instance;
            // Building[] mBuffer = bm.m_buildings.m_buffer;
            // int counter = 0;
            // for (int i = 0; i < mBuffer.Length; i++) {
            //     Building building = mBuffer[i];
            //     
            //     if ((mBuffer[i].m_flags & Building.Flags.Created) != 0 && mBuffer[i].Info) {
            //         counter++;
            //         int unitCounter = 0;
            //         uint next = building.m_citizenUnits;
            //         while (next != 0) {
            //             unitCounter++;
            //             next = cm.m_units.m_buffer[next].m_nextUnit;
            //         }
            //         
            //         sb.AppendLine("===("+ i +")========(" + building.m_citizenUnits + ")========("+ unitCounter+")========");
            //         sb.Append("building ")
            //             .Append(" " + (building.Info ? building.Info.m_class.name : "") + " ")
            //             .Append(building.m_flags.ToString())
            //             .Append(" [name: ").Append(building.Info.name).Append("]");
            //         sb.AppendLine("---------------------------------------");
            //     }
            //
        //         SearchProgress = SearchStep * i;
        //         if (i % 128 == 0) {
        //             yield return null;
        //         }
        //     }
        //
        //     SearchInProgress = false;
        //     Debug.Log("[BND] Building instances count: " + counter);
        //     Debug.Log("[BND] Dump report\n" + sb + "\n\n=======================================================");
        // }

        private bool GetSegmentInfo(StringBuilder sb, ref NetSegment segment, uint segmentId) {
            bool tooShort = ((segment.Info.m_class.m_service == ItemClass.Service.Beautification) && segment.m_averageLength < _minPedSegmentLength)
                || (segment.Info.m_class.m_service == ItemClass.Service.Road && segment.m_averageLength < _minVehSegmentLength);
            if (tooShort) {
                AppendSegmentInfo(sb, ref segment, segmentId);
            }
            
            return tooShort;
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
        
        private static void AppendSegmentInfo(StringBuilder sb, ref NetSegment segment, uint segmentId) {
            sb.Append("Id: ").Append(segmentId)
                .Append(" Flags: (").Append(segment.m_flags.ToString())
                .Append(") Nodes: [").Append(segment.m_startNode).Append(";").Append(segment.m_startNode)
                .Append("] Class: ")
                .AppendLine(segment.Info ? segment.Info.m_class.name: "Info is null!");
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

        public void StartKeybindEdit() {
            KeybindEditInProgress = true;
        }
        
        public void FinishKeybindEdit() {
            KeybindEditInProgress = false;
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

    public class SegmentInfo {
        public uint Id { get; }
        public string Name { get; }
        public Vector3 Position { get; }
        public float Length { get; }

        public SegmentInfo(uint id) {
            Id = id;
            NetManager nm = NetManager.instance;
            Name = nm.m_segments.m_buffer[id].Info ? nm.m_segments.m_buffer[id].Info.name : "Info is null!";
            Position = nm.m_segments.m_buffer[id].m_middlePosition;
            Length = nm.m_segments.m_buffer[id].m_averageLength;
        }
    }
}