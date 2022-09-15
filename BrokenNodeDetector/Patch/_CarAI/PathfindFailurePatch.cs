using HarmonyLib;
using JetBrains.Annotations;

namespace BrokenNodeDetector.Patch._CarAI {
    [HarmonyPatch(typeof(CarAI), "PathfindFailure", new []{typeof(ushort), typeof(Vehicle)}, new []{ArgumentType.Normal, ArgumentType.Ref})]
    public class PathfindFailurePatch {

        [UsedImplicitly]
        public static void Prefix(ushort vehicleID, ref Vehicle data) {
            ModService.Instance.OnPathFindFailure(vehicleID, ref data);
        }
    }
}