using HarmonyLib;

namespace StatsUITweaks
{
    public class UIControlPanelPatch
    {
        static bool initialized;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIControlPanelFilterPanel), nameof(UIControlPanelFilterPanel._OnOpen))]
        public static void Init(UIControlPanelFilterPanel __instance)
        {
            if (initialized) return;
            initialized = true;
            Utils.EnableRichText(__instance.astroFilterBox);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIControlPanelFilterPanel), nameof(UIControlPanelFilterPanel._OnUpdate))]
        public static void OnUpdate(UIControlPanelFilterPanel __instance)
        {
            Utils.DetermineAstroBoxIndex(__instance.astroFilterBox);
        }

        [HarmonyPostfix, HarmonyPriority(Priority.Low)]
        [HarmonyPatch(typeof(UIControlPanelFilterPanel), nameof(UIControlPanelFilterPanel.ReconstructAstroFilterBox))]
        static void FilterList(UIControlPanelFilterPanel __instance)
        {
            // 在遊戲中 UIControlPanelFilterPanel.ReconstructAstroFilterBox 前兩項是固定的
            // [0]:-1, "查看全星系"
            // [1]:0, "查看当前星球" 或 "查看当前星系"
            // 其餘皆為 星系 + 星球1 + 星球2 ..., 以星系的ID排序

            Utils.UpdateAstroBox(__instance.astroFilterBox, 2, "");
            //Plugin.Log.LogDebug(System.Environment.StackTrace);
        }
    }
}
