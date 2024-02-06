using HarmonyLib;
using NebulaCompatibilityAssist.Packets;
using NebulaPatcher.Patches.Dynamic;

namespace NebulaCompatibilityAssist.Hotfix
{
    public static class Warper0900
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ConstructionModuleComponent_Patch), nameof(ConstructionModuleComponent_Patch.RecycleDrone_Postfix))]
        [HarmonyPatch(typeof(DroneComponent_Patch.Get_InternalUpdate), nameof(DroneComponent_Patch.Get_InternalUpdate.InternalUpdate))]
        public static bool Suppression()
        {
            return false;
        }
    }
}
