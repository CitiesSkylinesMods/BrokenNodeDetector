using System.Collections.Generic;
using UnityEngine;

namespace BrokenNodeDetector.UI.Tools.SegmentUpdateTool {
    public class SegmentUpdateRequest : Detector {
        public override string Name => "Request segment update";

        public override string Tooltip =>
            "Fix segments and intersections\n" +
            "where blue void is visible instead of road\n" +
            "Simulation may lag for a while after request finished";

        public SegmentUpdateRequest() {
            ShowResultsPanel = false;
        }

        public override IEnumerable<float> Process() {
            IsProcessing = true;
            ProgressMessage = $"Processing...";
            NetManager netManager = NetManager.instance;
            float searchStep = 1.0f / netManager.m_segments.m_size;

            int batch = 0;
            FastList<ushort> segments = new FastList<ushort>();
            for (ushort i = 0; i < netManager.m_segments.m_size; i++) {

                if ((netManager.m_segments.m_buffer[i].m_flags & NetSegment.Flags.Created) != 0) {
                    segments.Add(i);
                }

                if (i % 368 == 0) {
                    float searchProgress = searchStep * i;
                    ProgressMessage = $"Processing...{searchProgress * 100:F0}%";
                    if (segments.m_size > 0) {
                        ushort[] segmentIds = segments.ToArray();
                        SimulationManager.instance.AddAction(() => UpdateSegmentsInternal(segmentIds, batch++));
                    }

                    segments.Clear();
                    yield return searchProgress;
                }
            }

            ProgressMessage = "Processing...100%";
            yield return 1f;
            if (segments.m_size > 0) {
                ushort[] segmentIds = segments.ToArray();
                SimulationManager.instance.AddAction(() => UpdateSegmentsInternal(segmentIds, batch++));
                segments.Clear();
            }
            IsProcessing = true;
        }

        private void UpdateSegmentsInternal(ushort[] segments, int batchNumber) {
            NetManager netManager = NetManager.instance;
            int updated = 0;
            Debug.Log($"Requested update of {segments.Length} segments in batch {batchNumber}");
            for (int i = 0; i < segments.Length; i++) {
                ref NetSegment segment = ref netManager.m_segments.m_buffer[segments[i]];
                if ((segment.m_flags & NetSegment.Flags.Created) != 0 && segment.Info && segment.Info.m_netAI is RoadBaseAI) {
                    netManager.UpdateSegment(segments[i]);
                    updated++;
                }
            }
            Debug.Log($"[BND] Updated {updated} road segments in {batchNumber} batch.");
        }
    }
}