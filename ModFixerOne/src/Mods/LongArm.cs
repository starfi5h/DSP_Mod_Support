using HarmonyLib;
using System;
using System.Reflection;

namespace ModFixerOne.Mods
{
    public static class LongArm
    {
        public const string NAME = "LongArm";
        public const string GUID = "semarware.dysonsphereprogram.LongArm";
        public const string VERSION = "1.4.6";

        public static void Init(Harmony harmony)
        {
            if (!BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(GUID, out var pluginInfo))
                return;
            if (pluginInfo.Metadata.Version.ToString() != VERSION)
                return;
            Assembly assembly = pluginInfo.Instance.GetType().Assembly;

            try
            {
                // replace uiGame.inventory with uiGame.inventoryWindow
                var transplier = new HarmonyMethod(typeof(Common_Patch).GetMethod("UIInventory_Transpiler"));

                var classType = assembly.GetType("LongArm.UI.LongArmUi");
                var methodInfo = AccessTools.Method(classType, "UIGame_On_E_Switch_Postfix");
                harmony.Patch(methodInfo, null, null, transplier);

                Plugin.Log.LogInfo($"{NAME} - OK");
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"{NAME} - Fail! Last target version: {VERSION}");
                Fixer_Patch.ErrorMessage += $"\n{NAME} (last target version: {VERSION})";
                Plugin.Log.LogDebug(e);
            }
        }
    }
}
