﻿using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
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
        public const string VERSION = "1.1.0";

        public static ManualLogSource Log;
        static Harmony harmony;

        public void Awake()
        {
            Log = Logger;

            var OrderByName = Config.Bind("AstroBox", "OrderByName", true, "Order list by system name.\n以星系名称排序列表");
            var SystemPrefix = Config.Bind("AstroBox", "SystemPrefix", "<color=yellow>", "Prefix string of star system in the list\n星系名称前缀");
            var SystemPostfix = Config.Bind("AstroBox", "SystemPostfix", "</color>", "Postfix string of star system in the list\n星系名称后缀");
            var PlanetPrefix = Config.Bind("AstroBox", "PlanetPrefix", "ㅤ", "Prefix string of planet in the list\n星球名称前缀"); //U+3164. Normal spaces will not load
            var PlanetPostfix = Config.Bind("AstroBox", "PlanetPostfix", "", "Postfix string of planet in the list\n星球名称后缀");

            var ListWidthOffeset = Config.Bind("StatsUITweaks", "ListWidthOffeset", 80, "Increase width of the list.\n增加列表栏位宽度");
            var HotkeyListUp = Config.Bind("StatsUITweaks", "HotkeyListUp", "PageUp", "Move to previous item in list.\n切换至列表中上一个项目");
            var HotkeyListDown = Config.Bind("StatsUITweaks", "HotkeyListDown", "PageDown", "Move to next item in list.\n切换至列表中下一个项目");
            var NumericPlanetNo = Config.Bind("StatsUITweaks", "NumericPlanetNo", false, "Convert planet no. from Roman numerals to numbers.\n将星球序号从罗马数字转为十进位数字");

            StatsWindowPatch.ListWidthOffeset = ListWidthOffeset.Value;
            StatsWindowPatch.OrderByName = OrderByName.Value;
            StatsWindowPatch.SystemPrefix = SystemPrefix.Value;
            StatsWindowPatch.SystemPostfix = SystemPostfix.Value;
            StatsWindowPatch.PlanetPrefix = PlanetPrefix.Value;
            StatsWindowPatch.PlanetPostfix = PlanetPostfix.Value;
            try
            {
                StatsWindowPatch.HotkeyListUp = (KeyCode)System.Enum.Parse(typeof(KeyCode), HotkeyListUp.Value, true);
                StatsWindowPatch.HotkeyListDown = (KeyCode)System.Enum.Parse(typeof(KeyCode), HotkeyListDown.Value, true);
            }
            catch
            {
                Logger.LogWarning("Can't parse HotkeyListUp or HotkeyListDown. Revert back to default values.");
                StatsWindowPatch.HotkeyListUp = KeyCode.PageUp;
                StatsWindowPatch.HotkeyListDown = KeyCode.PageDown;
            }

            harmony = new Harmony(GUID);
            harmony.PatchAll(typeof(StatsWindowPatch));
            if (NumericPlanetNo.Value)
                harmony.PatchAll(typeof(PlanetNamePatch));
        }

        public void OnDestroy()
        {
            StatsWindowPatch.OnDestory();
            harmony.UnpatchSelf();
            harmony = null;
        }
    }
}