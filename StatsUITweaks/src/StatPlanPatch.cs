using HarmonyLib;
using System;

namespace StatsUITweaks
{
    public class StatPlanPatch
    {
        [HarmonyFinalizer]
        [HarmonyPatch(typeof(SingleProducerStatPlan), "componentId", MethodType.Getter)]
        public static Exception Get_componentId_Finalizer(SingleProducerStatPlan __instance, Exception __exception)
        {
            if (__exception != null) //報錯可能原因: objId超出entityPool
            {
                Plugin.Log.LogWarning("SingleStorageStatPlan.get_componentId throws an error!");
                Plugin.Log.LogWarning(__exception);

                // Reset statplan data
                __instance.objId = 0;
            }
            return null;
        }

        [HarmonyFinalizer]
        [HarmonyPatch(typeof(SingleStorageStatPlan), "componentId", MethodType.Getter)]
        public static Exception Get_componentId_Finalizer(SingleStorageStatPlan __instance, Exception __exception)
        {
            if (__exception != null) //報錯可能原因: objId超出entityPool
            {
                Plugin.Log.LogWarning("SingleStorageStatPlan.get_componentId throws an error!");
                Plugin.Log.LogWarning(__exception);

                // Reset statplan data
                __instance.objId = 0;
            }
            return null;
        }
    }
}
