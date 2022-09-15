using HarmonyLib;
using JetBrains.Annotations;

namespace BrokenNodeDetector.Patch._HumanAI {
    [HarmonyPatch(typeof(CitizenAI), "InvalidPath", new []{typeof(ushort), typeof(CitizenInstance)}, new []{ArgumentType.Normal, ArgumentType.Ref})]
    public class InvalidPathPatch {

        [UsedImplicitly]
        public static void Prefix(ushort instanceID, ref CitizenInstance citizenData) {
            ModService.Instance.OnPathfindFailure(citizenData.m_citizen);
        }
    }
}