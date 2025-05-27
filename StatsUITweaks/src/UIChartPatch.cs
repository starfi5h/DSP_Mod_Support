using HarmonyLib;
using UnityEngine;

namespace StatsUITweaks
{
    public class UIChartPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIChart), nameof(UIChart._OnUpdate))]
        public static void OnUpdate_Postfix(UIChart __instance)
        {
            if (!__instance.mouseInChart) return;

            if (!VFInput.control && !VFInput.alt && Input.GetKeyDown(KeyCode.Tab))
            {
                // From UIChart.CreateDetailSizeMenu
                var array = ChartPresetsDB.GetPresetsArray(__instance.statPlanType);
                if (array == null || array.Length == 0) return;
                int presetIndex = (__instance.chartData.presetIndex + 1) % array.Length;
                __instance.OnSizeMenuButtonClick(presetIndex);
            }
        }
    }
}
