using Bottleneck.Stats;
using HarmonyLib;
using System;
using System.Reflection;

namespace NebulaCompatibilityAssist.Patches
{
    public static class Bottleneck_Patch
    {
        public const string NAME = "Bottleneck";
        public const string GUID = "Bottleneck";
        public const string VERSION = "1.0.15";

        public static void Init(Harmony harmony)
        {
            if (!BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(GUID, out var pluginInfo))
                return;
            Assembly assembly = pluginInfo.Instance.GetType().Assembly;

            try
            {
                Type classType = assembly.GetType("Bottleneck.Stats.ItemCalculationRuntimeSetting");

                // Fix error when client request stats from host
                harmony.Patch(AccessTools.Method(classType, "InputModes"),
                    new HarmonyMethod(typeof(Bottleneck_Patch).GetMethod("InputModes_Prefix")));

                classType = assembly.GetType("Bottleneck.Nebula.NebulaCompat");                
                // Fix error when client request local planet stats from host
                harmony.Patch(AccessTools.Method(classType, "SendRequest"),
                    new HarmonyMethod(typeof(Bottleneck_Patch).GetMethod("SendRequest_Prefix")));


                Log.Info($"{NAME} - OK");
            }
            catch (Exception e)
            {
                Log.Warn($"{NAME} - Fail! Last target version: {VERSION}");
                NC_Patch.ErrorMessage += $"\n{NAME} (last target version: {VERSION})";
                Log.Debug(e);
            }
        }

        public static bool InputModes_Prefix(in int[] productIds, in short[] modes)
        {
            // Somehow client's productIds contians key that doesn't exist in host's pool
            for (int i = 0; i < productIds.Length; i++)
            {
                if (ItemCalculationRuntimeSetting.Pool.TryGetValue(productIds[i], out var setting))
                {
                    setting._enabled = (modes[i] & 1) > 0;
                    setting._mode = ((ItemCalculationMode)(modes[i] >> 1));
                }
            }
            return false;
        }

        public static bool SendRequest_Prefix()
        {
            if (UIRoot.instance.uiGame.statWindow.astroFilter == 0) // Don't send request when astroFilter is local planet (0)
                return false;
            return true;
        }
    }
}
