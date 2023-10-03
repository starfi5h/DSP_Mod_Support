using BepInEx;
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
        public const string VERSION = "1.0.0";

        public static ManualLogSource Log;
        static Harmony harmony;

        public void Awake()
        {
            Log = Logger;

            var OrderByName = Config.Bind("StatsUITweaks", "OrderByName", true, "Order list by system name.\n以星系名称排序列表");
            var HotkeyListUp = Config.Bind("StatsUITweaks", "HotkeyListUp", "PageUp", "Move to previous item in list.\n切换至列表中上一个项目");
            var HotkeyListDown = Config.Bind("StatsUITweaks", "HotkeyListDown", "PageDown", "Move to next item in list.\n切换至列表中下一个项目");

            harmony = new Harmony(GUID);
            harmony.PatchAll(typeof(StatsWindowPatch));
            StatsWindowPatch.OrderByName = OrderByName.Value;

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
        }

        public void OnDestroy()
        {
            StatsWindowPatch.OnDestory();
            harmony.UnpatchSelf();
            harmony = null;
        }
    }
}
