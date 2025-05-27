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
        public const string VERSION = "1.6.7";

        public static ManualLogSource Log;
        static Harmony harmony;

        public void Awake()
        {
            Log = Logger;

            var OrderByName = Config.Bind("AstroBox", "OrderByName", true, "Order list by system name.\n以星系名称排序列表");
            var DropDownCount = Config.Bind("AstroBox", "DropDownCount", 15, "Number of items shown in drop-down list.\n下拉列表显示的个数");
            var SystemPrefix = Config.Bind("AstroBox", "SystemPrefix", "<color=yellow>", "Prefix string of star system in the list\n星系名称前缀");
            var SystemPostfix = Config.Bind("AstroBox", "SystemPostfix", "</color>", "Postfix string of star system in the list\n星系名称后缀");
            var PlanetPrefix = Config.Bind("AstroBox", "PlanetPrefix", "ㅤ", "Prefix string of planet in the list\n星球名称前缀"); //U+3164. Normal spaces will not load
            var PlanetPostfix = Config.Bind("AstroBox", "PlanetPostfix", "", "Postfix string of planet in the list\n星球名称后缀");
            var HotkeyListUp = Config.Bind("AstroBox", "HotkeyListUp", KeyCode.PageUp, "Move to previous item in list.\n切换至列表中上一个项目");
            var HotkeyListDown = Config.Bind("AstroBox", "HotkeyListDown", KeyCode.PageDown, "Move to next item in list.\n切换至列表中下一个项目");

            //var SignificantDigits = Config.Bind("StatsUITweaks", "SignificantDigits", 0, new ConfigDescription("Significant figures of production/consumption (Default=0)\n产量有效位数(默认=0)", new AcceptableValueRange<int>(0, 10)));
            var TimeSliderSlice = Config.Bind("MainWindow", "TimeSliderSlice", 20, "The number of divisions of the time range slider.\n时间范围滑杆的分割数");
            var ListWidthOffeset = Config.Bind("MainWindow", "ListWidthOffeset", 70, "Increase width of the list.\n增加列表栏位宽度");
            var RateFontSize = Config.Bind("MainWindow", "RateFontSize", 26, "Adjust the font size of production rate. (Vanilla=18)\n生产速率和参考速率的字体大小(原版=18)");
            var RefRateTweak = Config.Bind("MainWindow", "RefRateTweak", false, "The reference rate (maximum theoretical value) is always applied proliferator settings, regardless the material.\n参考速率(最大理论值)一律套用增产剂设定，无论原料是否已喷涂");
            var RefRateMinerLimit = Config.Bind("MainWindow", "RefRateMinerLimit", 14400, "Set reference rate max limit for pump and oil extractor\n為抽水機和油井的参考速率設定上限");

            var NumericPlanetNo = Config.Bind("Other", "NumericPlanetNo", false, "Convert planet no. from Roman numerals to numbers.\n将星球序号从罗马数字转为十进位数字");
            var FoldButton = Config.Bind("Other", "FoldButton", true, "Add a button in perforamnce test panel to fold pie chart.\n在性能面板加入一个折叠饼图的按钮");
            
            var TabSizeSwitch = Config.Bind("Dashboard", "TabSizeSwitch", true, "Tab to switch size when hovering on an item.\n鼠标悬停在某个统计项上时，按Tab键切换尺寸");

            Utils.OrderByName = OrderByName.Value;
            Utils.DropDownCount = DropDownCount.Value;
            Utils.HotkeyListUp = HotkeyListUp.Value;
            Utils.HotkeyListDown = HotkeyListDown.Value;
            Utils.SystemPrefix = SystemPrefix.Value;
            Utils.SystemPostfix = SystemPostfix.Value;
            Utils.PlanetPrefix = PlanetPrefix.Value;
            Utils.PlanetPostfix = PlanetPostfix.Value;

            //StatsWindowPatch.SignificantDigits = SignificantDigits.Value > 0 ? SignificantDigits.Value : 0;
            StatsWindowPatch.TimeSliderSlice = TimeSliderSlice.Value;
            StatsWindowPatch.ListWidthOffeset = ListWidthOffeset.Value;
            StatsWindowPatch.RateFontSize = RateFontSize.Value;

            harmony = new Harmony(GUID);
            harmony.PatchAll(typeof(StatsWindowPatch));
            if (NumericPlanetNo.Value)
                harmony.PatchAll(typeof(PlanetNamePatch));
            if (FoldButton.Value)
            {
                harmony.PatchAll(typeof(PerformancePanelPatch));
                harmony.PatchAll(typeof(UIStatisticsPowerDetailPanelPatch));
            }
            harmony.PatchAll(typeof(UIControlPanelPatch));
            harmony.PatchAll(typeof(StatsWindowPatch.Entry_Patch));

            RefProductSpeedPatch.Init(harmony, RefRateTweak.Value, RefRateMinerLimit.Value);
            harmony.PatchAll(typeof(UIGamePatch));

            harmony.PatchAll(typeof(StatPlanPatch));
            if (TabSizeSwitch.Value)
                harmony.PatchAll(typeof(UIChartPatch));
        }

#if DEBUG
        public void OnDestroy()
        {
            StatsWindowPatch.OnDestory();
            PerformancePanelPatch.OnDestory();
            UIStatisticsPowerDetailPanelPatch.OnDestory();
            harmony.UnpatchSelf();
            harmony = null;
        }
#endif
    }
}
