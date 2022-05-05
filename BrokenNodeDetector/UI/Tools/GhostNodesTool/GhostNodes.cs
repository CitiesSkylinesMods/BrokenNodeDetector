using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using ColossalFramework.UI;
using UnityEngine;

namespace BrokenNodeDetector.UI.Tools.GhostNodesTool {
    public class GhostNodes : Detector {
        private int _lastGhostNodesCount = 0;
        public override string Name => "Find ghost nodes";
        public override string Tooltip => "Detects invisible broken nodes";

        public GhostNodes() {
            GenerateDefaultTemplate();
        }
        public override IEnumerable<float> Process() {
            Debug.Log("[BND] Searching for ghost nodes");
            _lastGhostNodesCount = 0;
            AsyncTask task = SimulationManager.instance.AddAction(ProcessInternal());
            int steps = (int)NetManager.instance.m_nodes.m_size / 256 + 1;
            SetTaskProgress(task, steps);
            while (task.progress <= 1.0f) {
                float progress = task.progress;
                yield return progress < 0f ? 0: progress;
            }
            
            yield return 1.0f;
        }

        public override void InitResultsView(UIComponent component) {
            if (component is UILabel label) {
                label.textScale = 1.2f;
                if (_lastGhostNodesCount == 0) {
                    label.text = "Great! No ghost nodes found :-)";
                    label.relativePosition = new Vector3(45, label.relativePosition.y);
                    label.color = Color.white;
                } else {
                    label.text = $"Found and released {_lastGhostNodesCount} ghost nodes :-)";
                    label.relativePosition = new Vector3(20, label.relativePosition.y);
                    label.color = Color.green;
                }    
            }
        }

        private IEnumerator ProcessInternal() {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Ghost/broken nodes list: ");
            int size = (int)NetManager.instance.m_nodes.m_size;
            int step = size / 256;
            for (var i = 0; i < size; i++) {
                NetNode node = NetManager.instance.m_nodes.m_buffer[i];
                if (node.m_flags != 0
                    && (node.m_flags & NetNode.Flags.Untouchable) == 0
                    && node.CountSegments() == 0
                    && (node.m_flags & NetNode.Flags.Created) != 0) {
                    _lastGhostNodesCount++;
                    sb.Append("[").Append(i).Append("] - ").Append(node.m_flags.ToString()).Append(" info: ").Append(node.Info.ToString()).AppendLine("]");
                    NetManager.instance.ReleaseNode((ushort) i);
                }

                if (i % step == 0) {
                    Thread.Sleep(2);
                    yield return null;
                }
            }

            sb.AppendLine($"=================================================");
            Debug.Log("[BND] Searching finished. Found and released " + _lastGhostNodesCount + " ghost nodes");
            Debug.Log(sb);
        }
    }
}