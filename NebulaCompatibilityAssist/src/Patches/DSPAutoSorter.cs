using HarmonyLib;
using NebulaAPI;
using System;
using System.Reflection;

namespace NebulaCompatibilityAssist.Patches
{
    public static class DSPAutoSorter
    {
        public const string NAME = "DSPAutoSorter";
        public const string GUID = "Appun.DSP.plugin.AutoSorter";
        public const string VERSION = "1.2.11";

        public static void Init(Harmony harmony)
        {
            if (!BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(GUID, out var pluginInfo))
                return;
            Assembly assembly = pluginInfo.Instance.GetType().Assembly;

            try
            {
                // Sync new station config
                Type classType = assembly.GetType("DSPAutoSorter.DSPAutoSorter");
                harmony.Patch(AccessTools.Method(classType, "UIStorageWindow_OnOpen_Postfix"),
                    new HarmonyMethod(typeof(DSPAutoSorter).GetMethod("Block")));

                Log.Info($"{NAME} - OK");
            }
            catch (Exception e)
            {
                Log.Warn($"{NAME} - Fail! Last target version: {VERSION}");
                NC_Patch.ErrorMessage += $"\n{NAME} (last target version: {VERSION})";
                Log.Debug(e);
            }
        }

        public static bool Block()
        {
            if (!NebulaModAPI.IsMultiplayerActive)
                return true;

            return false;
        }
    }
}
