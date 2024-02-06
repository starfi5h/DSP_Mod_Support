using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Reflection;

namespace NebulaCompatibilityAssist.Patches
{
    public static class BlueprintTweaks
    {
        public const string NAME = "BlueprintTweaks";
        public const string GUID = "org.kremnev8.plugin.BlueprintTweaks";
        public const string VERSION = "1.6.3";

        public static void Init(Harmony _)
        {
            if (!BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(GUID, out var pluginInfo))
                return;
            Assembly assembly = pluginInfo.Instance.GetType().Assembly;

            try
            {
                // Disable useFastDismantle due to it may crash the host 
                Type classType = assembly.GetType("BlueprintTweaks.BlueprintTweaksPlugin");
                var useFastDismantle = (ConfigEntry<bool>)AccessTools.Field(classType, "useFastDismantle").GetValue(pluginInfo.Instance);
                useFastDismantle.Value = false;

                Log.Info($"{NAME} - OK");
            }
            catch (Exception e)
            {
                Log.Warn($"{NAME} - Fail! Last target version: {VERSION}");
                NC_Patch.ErrorMessage += $"\n{NAME} (last target version: {VERSION})";
                Log.Debug(e);
            }
        }
    }
}
