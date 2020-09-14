namespace BrokenNodeDetector.Patch._NetNode {
    public static class CustomNetNode {
        internal static void Postfix(ushort nodeID) {
            ModService.Instance.OnUpdateLaneConnection(nodeID);
        }
    }
}