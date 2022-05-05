using System.Collections.Generic;
using ColossalFramework.UI;
using UnityEngine;

namespace BrokenNodeDetector {
    public class ModService {
        private readonly Dictionary<ushort, uint> _nodeCalls;
        private bool _prevSimState;

        static ModService() {
            Instance = new ModService();
        }

        private ModService() {
            _nodeCalls = new Dictionary<ushort, uint>();
            Results = new List<ushort>();
        }

        private bool IsWorking { get; set; }
        public static ModService Instance { get; private set; }
        public List<ushort> Results { get; private set; }

        public bool KeybindEditInProgress { get; private set; }

        public void StartDetector() {
            if (IsWorking) return;
            _prevSimState = SimulationManager.instance.SimulationPaused;
            if (_prevSimState) {
                SimulationManager.instance.SelectedSimulationSpeed = 1;
            }

            _nodeCalls.Clear();
            Results.Clear();
            IsWorking = true;
        }

        public void StopDetector() {
            if (!IsWorking) return;

            if (_prevSimState) {
                SimulationManager.instance.SimulationPaused = true;
            }
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
                if (pair.Value > 25) {
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
        public LineInfo(ushort id) {
            Id = id;
            TransportManager tm = TransportManager.instance;
            Color = tm.GetLineColor(id);
            Name = tm.GetLineName(id);
            PtInfo = tm.m_lines.m_buffer[id].Info.GetUncheckedLocalizedTitle();
            Stops = new List<ushort>();
        }

        public ushort Id { get; }
        public Color Color { get; }
        public string Name { get; }

        public string PtInfo { get; }

        public List<ushort> Stops { get; }

        public ushort AllStops { get; private set; }

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
        public SegmentInfo(uint id) {
            Id = id;
            NetManager nm = NetManager.instance;
            Name = nm.m_segments.m_buffer[id].Info ? nm.m_segments.m_buffer[id].Info.name : "Info is null!";
            Position = nm.m_segments.m_buffer[id].m_middlePosition;
            Length = nm.m_segments.m_buffer[id].m_averageLength;
        }

        public uint Id { get; }
        public string Name { get; }
        public Vector3 Position { get; }
        public float Length { get; }
    }
}