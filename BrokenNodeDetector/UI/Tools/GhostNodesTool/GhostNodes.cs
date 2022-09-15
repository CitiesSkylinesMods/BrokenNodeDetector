using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using ColossalFramework.UI;
using UnityEngine;

namespace BrokenNodeDetector.UI.Tools.GhostNodesTool {
    public class GhostNodes : Detector {
        private int _lastGhostNodesCount = 0;
        private volatile float _progress;
        public override string Name => "Find ghost nodes";
        public override string Tooltip => "Detects invisible broken nodes";

        public GhostNodes() {
            GenerateDefaultTemplate();
        }
        public override IEnumerable<float> Process() {
            IsProcessing = true;
            Debug.Log("[BND] Searching for ghost nodes");
            _lastGhostNodesCount = 0;
            _progress = 0;
            AsyncTask task = SimulationManager.instance.AddAction(ProcessInternal());
            while (!task.completed) {
                yield return _progress;
            }
            
            yield return 1.0f;
            IsProcessing = false;
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
            float step = 1.0f / size;
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

                float searchProgress = step * i;
                if (i % 32 == 0) {
                    ProgressMessage = $"Processing...{searchProgress * 100:F0}%";
                    Thread.Sleep(1);
                    _progress = step * i;
                }
            }
            ProgressMessage = "Processing...100%";

            sb.AppendLine($"=================================================");
            Debug.Log("[BND] Searching finished. Found and released " + _lastGhostNodesCount + " ghost nodes");
            Debug.Log(sb);
            yield return null;
        }
    }
}