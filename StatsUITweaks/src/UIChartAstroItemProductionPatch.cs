using HarmonyLib;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace StatsUITweaks
{
    public class UIChartAstroItemProductionPatch
    {
        private static double[] lvDivisors = { 1.0, 10.0, 60.0, 600.0, 6000.0 };

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIChartAstroItemProduction), nameof(UIChartAstroItemProduction.ShowInText))]
        public static void ShowInText_Postfix(UIChartAstroItemProduction __instance, int level)
        {
            // ====== 1×1 (presetIndex == 0, displayTypeParams[0] == 5) ======
            if (__instance.presetIndex == 0)
            {
                int displayType = __instance.chartData.displayTypeParams[0];
                if (displayType == 5 && level != 5)
                {
                    float productRefSpeed, consumeRefSpeed;
                    __instance.statPlan.GetItemsCyclicRefSpeed(out productRefSpeed, out consumeRefSpeed);

                    __instance.consumeText.text = (__instance.ToLevelString((double)productRefSpeed, level).TrimStart() + "<color=#fff5><size=10>/min</size></color>");
                    __instance.consumeText.color = ((productRefSpeed > 0f) ? __instance.productColor : __instance.zeroColor);
                }
                return;
            }

            // ====== 2×1 (presetIndex == 1, displayTypeParams[0] == 5) ======
            if (__instance.presetIndex == 1)
            {
                int displayType = __instance.chartData.displayTypeParams[0];
                if (displayType != 5) return;

                bool flag2 = (__instance.statPlan.timeLevel == 5);
                if (flag2) return; // Ignore Total

                __instance.displayGo0.SetActive(true);
                __instance.displayGo1.SetActive(false);

                // Calculate production and consumption speeds
                long productCount, consumeCount;
                __instance.statPlan.CalculateProductionAndConsumption(out productCount, out consumeCount);
                double productSpeed = (double)productCount / lvDivisors[level];
                double consumeSpeed = (double)consumeCount / lvDivisors[level];

                // Retrieve reference speeds
                float productRefSpeed, consumeRefSpeed;
                __instance.statPlan.GetItemsCyclicRefSpeed(out productRefSpeed, out consumeRefSpeed);
                double productRef = (double)productRefSpeed;
                double consumeRef = (double)consumeRefSpeed;

                // Format strings to K/M etc.
                string productSpeedStr = __instance.ToLevelString(productSpeed, level).TrimStart();
                string consumeSpeedStr = __instance.ToLevelString(consumeSpeed, level).TrimStart();
                string productRefStr = __instance.ToLevelString(productRef, level).TrimStart();
                string consumeRefStr = __instance.ToLevelString(consumeRef, level).TrimStart();

                __instance.productText.text = productSpeedStr + " / " + productRefStr;
                __instance.consumeText.text = consumeSpeedStr + " / " + consumeRefStr;

                __instance.productText.fontSize = 18;
                __instance.consumeText.fontSize = 18;

                __instance.productUnitLabel.text = "/ min";
                __instance.consumeUnitLabel.text = "/ min";
                __instance.productLabel.text = "生产/理论";
                __instance.consumeLabel.text = "消耗/理论";

                __instance.productText.rectTransform.anchoredPosition = new Vector2(-26f, 0f);
                __instance.consumeText.rectTransform.anchoredPosition = new Vector2(-26f, 0f);

                __instance.productText.color = ((productCount > 0L) ? __instance.productColor : __instance.zeroColor);
                __instance.consumeText.color = ((consumeCount > 0L) ? __instance.consumeColor : __instance.zeroColor);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIChartAstroItemProduction), nameof(UIChartAstroItemProduction.CreateLayoutMenu))]
        public static void CreateLayoutMenu_Postfix(UIChartAstroItemProduction __instance, UIPopupMenu __result)
        {
            // Only apply to 1x1 and 2x1 presets
            if ((__instance.presetIndex != 0 && __instance.presetIndex != 1) || __instance.statPlan.timeLevel == 5) return;
            if (__result == null) return;

            string menuText = (__instance.presetIndex == 0) ? "生产与理论速度" : "生产消耗与理论";

            UIPopupMenuButton btn = __result.AddMenuButton(menuText.Translate(),
                (__instance.chartData.displayTypeParams[0] == 5) ? 1 : (-1),
                false);
            btn.data = 5;
            btn.onMenuButtonClick = (Action<int>)Delegate.Combine(btn.onMenuButtonClick, new Action<int>(__instance.OnLayoutMenuButtonClick));
            btn.SetState(true);

            __result.SetState(true);
        }
    }
}