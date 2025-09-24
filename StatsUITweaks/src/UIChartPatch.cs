using HarmonyLib;
using UnityEngine;

namespace StatsUITweaks
{
    public class UIChartPatch
    {
        private static int titleIndex;
        private static int lastStatPlanId;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIChart), nameof(UIChart._OnUpdate))]
        public static void OnUpdate_Postfix(UIChart __instance)
        {
            if (!__instance.mouseInChart) return;

            if (Input.GetKeyDown(KeyCode.Tab))
            {
                if (VFInput.alt) return;
                if (VFInput.control) SwitchTitleName(__instance);
                else SwitchSize(__instance);
            }
        }

        private static void SwitchTitleName(UIChart __instance)
        {
            if (__instance.titleTip != null)
            {
                var substrings = __instance.titleTip.tipTextFormatString.Split(';');
                if (substrings.Length > 0)
                {
                    var statPlan = __instance.charts.statPlans[__instance.chartData.statPlanId];
                    if (lastStatPlanId != __instance.chartData.statPlanId)
                    {
                        titleIndex = 0;
                        lastStatPlanId = __instance.chartData.statPlanId;
                    }
                    titleIndex = (titleIndex + 1) % substrings.Length;
                    var newTitle = substrings[titleIndex].Trim();
                    statPlan.Rename(ref newTitle);
                }
            }
        }

        private static void SwitchSize(UIChart __instance)
        {
            // From UIChart.CreateDetailSizeMenu
            var array = ChartPresetsDB.GetPresetsArray(__instance.statPlanType);
            if (array == null || array.Length == 0) return;
            int presetIndex = (__instance.chartData.presetIndex + 1) % array.Length;
            __instance.OnSizeMenuButtonClick(presetIndex);
        }
    }
}
