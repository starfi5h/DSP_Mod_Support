using HarmonyLib;
using System;
using System.Reflection;

namespace ModFixerOne.Mods
{
    public static class PersonalLogistics
    {
        public const string NAME = "PersonalLogistics";
        public const string GUID = "semarware.dysonsphereprogram.PersonalLogistics";
        public const string VERSION = "2.9.10";

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
                var postfix = new HarmonyMethod(typeof(PersonalLogistics).GetMethod("Postfix"));

                var classType = assembly.GetType("PersonalLogistics.Scripts.RecycleWindow");
                var methodInfo = AccessTools.Method(classType, "Update");
                harmony.Patch(methodInfo, null, null, transplier);
                methodInfo = AccessTools.Method(classType, "AddShowRecycleCheck");
                harmony.Patch(methodInfo, null, null, transplier);

                classType = assembly.GetType("PersonalLogistics.Scripts.RequesterWindow");
                methodInfo = AccessTools.Method(classType, "Update");
                harmony.Patch(methodInfo, null, null, transplier);

                classType = assembly.GetType("PersonalLogistics.PersonalLogisticsPlugin");
                methodInfo = AccessTools.Method(classType, "InitUi");
                harmony.Patch(methodInfo, null, postfix, transplier);

                Plugin.Log.LogInfo($"{NAME} - OK");
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"{NAME} - Fail! Last target version: {VERSION}");
                Fixer_Patch.ErrorMessage += $"\n{NAME} (last target version: {VERSION})";
                Plugin.Log.LogDebug(e);
            }
        }

        public static void Postfix()
        {
            Plugin.Log.LogDebug("init");
        }
    }
}
