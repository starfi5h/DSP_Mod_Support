using HarmonyLib;
using NebulaAPI;
using System;
using System.Reflection;

namespace NebulaCompatibilityAssist.Patches
{
    public class GalacticScale_Patch
    {
        public const string NAME = "Galactic Scale 2 Plug-In";
        public const string GUID = "dsp.galactic-scale.2";
        public const string VERSION = "2.14.2";

        public static void Init(Harmony harmony)
        {
            if (!BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(GUID, out var pluginInfo))
                return;
            Assembly assembly = pluginInfo.Instance.GetType().Assembly;

            try
            {

                Log.Info($"{NAME} - OK");
            }
            catch (Exception e)
            {
                Log.Warn($"{NAME} - Fail! Last target version: {VERSION}");
                NC_Patch.ErrorMessage += $"\n{NAME} (last target version: {VERSION})";
                Log.Warn(e);
            }
        }
    }
}
