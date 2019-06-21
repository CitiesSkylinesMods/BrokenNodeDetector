using RedirectionFramework.Attributes;

namespace BrokenNodeDetector.Patch._NetNode {

    [TargetType(typeof(NetNode))]
    public static class CustomNetNode {
        [RedirectMethod]
        public static void UpdateLaneConnection(ref NetNode netNode, ushort nodeID) {
            //stock code start
            if (netNode.m_flags == NetNode.Flags.None) {
                return;
            }

            NetInfo info = netNode.Info;
            info.m_netAI.UpdateLaneConnection(nodeID, ref netNode);
            //stock code end

            //custom call
            ModService.Instance.OnUpdateLaneConnection(nodeID);
        }
    }
}