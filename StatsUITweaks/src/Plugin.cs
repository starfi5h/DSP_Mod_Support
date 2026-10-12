using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;

[assembly: AssemblyTitle(StatsUITweaks.Plugin.NAME)]
[assembly: AssemblyVersion(StatsUITweaks.Plugin.VERSION)]

namespace StatsUITweaks
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public const string GUID = "starfi5h.plugin.StatsUITweaks";
        public const string NAME = "StatsUITweaks";
        public const string VERSION = "1.6.16";

        public static ManualLogSource Log;
        static Harmony harmony;

        // 宣告 ConfigEntry 變數以便在事件中參考
        private ConfigEntry<bool> orderByNameConfig;
        private ConfigEntry<int> dropDownCountConfig;
        private ConfigEntry<string> systemPrefixConfig;
        private ConfigEntry<string> systemPostfixConfig;
        private ConfigEntry<string> planetPrefixConfig;
        private ConfigEntry<string> planetPostfixConfig;
        private ConfigEntry<KeyCode> hotkeyListUpConfig;
        private ConfigEntry<KeyCode> hotkeyListDownConfig;

        private ConfigEntry<int> timeSliderSliceConfig;
        private ConfigEntry<int> listWidthOffesetConfig;
        private ConfigEntry<int> rateFontSizeConfig;
        private ConfigEntry<bool> excludeTrafficPeakConfig;

        public void Awake()
        {
            Log = Logger;

            // 1. 綁定設定檔
            orderByNameConfig = Config.Bind("AstroBox", "OrderByName", true, "Order list by system name.\n以星系名称排序列表");
            dropDownCountConfig = Config.Bind("AstroBox", "DropDownCount", 15, "Number of items shown in drop-down list.\n下拉列表显示的个数");
            systemPrefixConfig = Config.Bind("AstroBox", "SystemPrefix", "<color=yellow>", "Prefix string of star system in the list\n星系名称前缀");
            systemPostfixConfig = Config.Bind("AstroBox", "SystemPostfix", "</color>", "Postfix string of star system in the list\n星系名称后缀");
            planetPrefixConfig = Config.Bind("AstroBox", "PlanetPrefix", "ㅤ", "Prefix string of planet in the list\n星球名称前缀"); //U+3164
            planetPostfixConfig = Config.Bind("AstroBox", "PlanetPostfix", "", "Postfix string of planet in the list\n星球名称后缀");
            hotkeyListUpConfig = Config.Bind("AstroBox", "HotkeyListUp", KeyCode.PageUp, "Move to previous item in list.\n切换至列表中上一个项目");
            hotkeyListDownConfig = Config.Bind("AstroBox", "HotkeyListDown", KeyCode.PageDown, "Move to next item in list.\n切换至列表中下一个项目");

            timeSliderSliceConfig = Config.Bind("MainWindow", "TimeSliderSlice", 20, "The number of divisions of the time range slider.\n时间范围滑杆的分割数");
            listWidthOffesetConfig = Config.Bind("MainWindow", "ListWidthOffeset", 70, "Increase width of the list.\n增加列表栏位宽度");
            rateFontSizeConfig = Config.Bind("MainWindow", "RateFontSize", 26, "Adjust the font size of production rate. (Vanilla=18)\n生产速率和参考速率的字体大小(原版=18)");
            var refRateTweak = Config.Bind("MainWindow", "RefRateTweak", false, "The reference rate (maximum theoretical value) is always applied proliferator settings, regardless the material.\n参考速率(最大理论值)一律套用增产剂设定，无论原料是否已喷涂");
            var refRateMinerLimit = Config.Bind("MainWindow", "RefRateMinerLimit", 14400, "Set reference rate max limit for pump and oil extractor\n為抽水機和油井的参考速率設定上限");
            excludeTrafficPeakConfig = Config.Bind("MainWindow", "ExcludeTrafficPeak", false, "When calculating the height of a histogram, exclude peak values ​​from input and output.\n计算柱状图高度时, 排除输入和输出的峰值");

            var foldButton = Config.Bind("Other", "FoldButton", true, "Add a button in perforamnce test panel to fold pie chart.\n在性能面板加入一个折叠饼图的按钮");

            // 2. 初始化賦值
            UpdateValues(null, EventArgs.Empty);

            // 3. 註冊 Config 變更事件（實現即時生效）
            orderByNameConfig.SettingChanged += UpdateValues;
            dropDownCountConfig.SettingChanged += UpdateValues;
            systemPrefixConfig.SettingChanged += UpdateValues;
            systemPostfixConfig.SettingChanged += UpdateValues;
            planetPrefixConfig.SettingChanged += UpdateValues;
            planetPostfixConfig.SettingChanged += UpdateValues;
            hotkeyListUpConfig.SettingChanged += UpdateValues;
            hotkeyListDownConfig.SettingChanged += UpdateValues;

            timeSliderSliceConfig.SettingChanged += UpdateValues;
            listWidthOffesetConfig.SettingChanged += UpdateValues;
            rateFontSizeConfig.SettingChanged += UpdateValues;
            excludeTrafficPeakConfig.SettingChanged += UpdateValues;

            harmony = new Harmony(GUID);
            harmony.PatchAll(typeof(StatsWindowPatch));

            if (foldButton.Value)
            {
                harmony.PatchAll(typeof(PerformancePanelPatch));
                harmony.PatchAll(typeof(UIStatisticsPowerDetailPanelPatch));
            }
            harmony.PatchAll(typeof(StatsWindowPatch.Entry_Patch));

            RefProductSpeedPatch.Init(harmony, refRateTweak.Value, refRateMinerLimit.Value);
            harmony.PatchAll(typeof(UIGamePatch));

            harmony.PatchAll(typeof(StatPlanPatch));
            harmony.PatchAll(typeof(UIChartAstroItemProductionPatch));

            // Add extra UILayoutHeights
            var originalMethod = AccessTools.Method(typeof(UIOptionWindow), nameof(UIOptionWindow.CollectUILayoutHeights));
            if (originalMethod != null)
            {
                var harmonyMethod = new HarmonyMethod(AccessTools.Method(typeof(Plugin), nameof(CollectUILayoutHeights_Postfix)));
                harmony.Patch(originalMethod, null, harmonyMethod);
            }
        }

        public static void CollectUILayoutHeights_Postfix(UIOptionWindow __instance)
        {
            var uiLayoutHeights = __instance.uiLayoutHeights;
            uiLayoutHeights.Add(500);
            uiLayoutHeights.Add(600);
            uiLayoutHeights.Add(700);
            uiLayoutHeights.Add(800);
            uiLayoutHeights.Sort();
        }

        private void UpdateValues(object sender, EventArgs e)
        {
            Utils.OrderByName = orderByNameConfig.Value;
            Utils.DropDownCount = dropDownCountConfig.Value;
            Utils.HotkeyListUp = hotkeyListUpConfig.Value;
            Utils.HotkeyListDown = hotkeyListDownConfig.Value;
            Utils.SystemPrefix = systemPrefixConfig.Value;
            Utils.SystemPostfix = systemPostfixConfig.Value;
            Utils.PlanetPrefix = planetPrefixConfig.Value;
            Utils.PlanetPostfix = planetPostfixConfig.Value;

            StatsWindowPatch.TimeSliderSlice = timeSliderSliceConfig.Value;
            StatsWindowPatch.ListWidthOffeset = listWidthOffesetConfig.Value;
            StatsWindowPatch.RateFontSize = rateFontSizeConfig.Value;
            StatsWindowPatch.ExcludeTrafficPeak = excludeTrafficPeakConfig.Value;
        }

#if DEBUG
        public void OnDestroy()
        {
            StatsWindowPatch.OnDestroy();
            PerformancePanelPatch.OnDestroy();
            UIStatisticsPowerDetailPanelPatch.OnDestroy();
            harmony.UnpatchSelf();
            harmony = null;
        }
#endif
    }
}
