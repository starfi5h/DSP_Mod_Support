using BepInEx.Configuration;
using HarmonyLib;
using System;

namespace UITweaks
{
    public class UILayout_Tweaks
    {
        private static ConfigEntry<bool> enableOverwrite;
        private static ConfigEntry<int> customLayoutHeightEntry;
        private static int customLayoutHeight = 0;

        public static void OnAwake(ConfigFile config)
        {
            enableOverwrite = config.Bind("UI Layout", "Enable Overwrite", false,
                "Enable overwite to the UI layout height setting");
            customLayoutHeightEntry = config.Bind("UI Layout", "UI Layout Height", 900,
                new ConfigDescription("Lower value gets larger layout", new AcceptableValueRange<int>(480, 900)));
            customLayoutHeightEntry.SettingChanged += OnConfigChanged;            
        }

        public static void OnDestroy(ConfigFile _)
        {
            customLayoutHeightEntry.SettingChanged -= OnConfigChanged;
        }

        public static void OnConfigChanged(object sender, EventArgs e)
        {
            if (!enableOverwrite.Value) return;
            Plugin.Log.LogDebug($"uiLayoutHeight changed: {UICanvasScalerHandler.uiLayoutHeight} => {customLayoutHeightEntry.Value}");
            UICanvasScalerHandler.uiLayoutHeight = customLayoutHeightEntry.Value;
            DSPGame.globalOption.uiLayoutHeight = customLayoutHeightEntry.Value;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIOptionWindow), nameof(UIOptionWindow.CollectUILayoutHeights))]
        public static void CollectUILayoutHeights_Postfix(UIOptionWindow __instance)
        {
            // 將自訂值加入選項中, 避免被重設
            if (!enableOverwrite.Value) return;
            var uiLayoutHeights = __instance.uiLayoutHeights;
            if (customLayoutHeight != 0 && customLayoutHeight != customLayoutHeightEntry.Value)
            {
                uiLayoutHeights.Remove(customLayoutHeight); //移除舊的自訂選項
            }
            customLayoutHeight = customLayoutHeightEntry.Value;
            if (!uiLayoutHeights.Contains(customLayoutHeight))
            {
                uiLayoutHeights.Add(customLayoutHeight); //增加新的自訂選項
                uiLayoutHeights.Sort();
            }            
        }
    }
}
