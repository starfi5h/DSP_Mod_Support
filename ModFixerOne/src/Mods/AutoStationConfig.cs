using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace ModFixerOne.Mods
{
    public static class AutoStationConfig
    {
        public const string NAME = "AutoStationConfig";
        public const string GUID = "pasukaru.dsp.AutoStationConfig";
        public const string VERSION = "1.4.0";

        public static void Init(Harmony harmony)
        {
            if (!BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(GUID, out var pluginInfo))
                return;
            if (pluginInfo.Metadata.Version.ToString() != VERSION)
                return;
            Assembly assembly = pluginInfo.Instance.GetType().Assembly;

            try
            {
                // Test if the mod version is fixed or not
                Fixer_Patch.flag = false;

                // Fix for 0.9.27 function name changes
                Type classType = assembly.GetType("Pasukaru.DSP.AutoStationConfig.Extensions");
                harmony.Patch(AccessTools.Method(classType, "FixDuplicateWarperStores"), null, null,
                    new HarmonyMethod(typeof(AutoStationConfig).GetMethod("RefreshTraffic_Transpiler")));

                if (Fixer_Patch.flag)
                {
                    // Skip patch for fixed version
                    Plugin.Log.LogInfo($"{NAME} is already fixed.");
                }
                else
                {
                    // Fix advance miner power usage abnormal
                    harmony.Patch(AccessTools.Method(classType, "SetChargingPower"),
                        new HarmonyMethod(typeof(AutoStationConfig).GetMethod("SetChargingPower_Prefix")));
                    Plugin.Log.LogInfo($"{NAME} - OK");
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"{NAME} - Fail! Last target version: {VERSION}");
                Fixer_Patch.ErrorMessage += $"\n{NAME} (last target version: {VERSION})";
                Plugin.Log.LogDebug(e);
            }
        }

        public static bool SetChargingPower_Prefix(StationComponent __0)
        {
            // Disable set charging power if it is advance miner
            return !__0.isVeinCollector;              
        }

        public static IEnumerable<CodeInstruction> RefreshTraffic_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            try
            {
                // replace transport.RefreshTraffic with transport.RefreshStationTraffic
                var codeMatcher = new CodeMatcher(instructions)
                    .End()
                    .MatchBack(false, new CodeMatch(i => i.opcode == OpCodes.Callvirt && ((MethodInfo)i.operand).Name == "RefreshTraffic"))
                    .SetAndAdvance(OpCodes.Callvirt, AccessTools.Method(typeof(PlanetTransport), nameof(PlanetTransport.RefreshStationTraffic)));

                return codeMatcher.InstructionEnumeration();
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("RefreshTraffic_Transpiler fail!");
#if DEBUG
                Plugin.Log.LogWarning(e);
#endif
                Fixer_Patch.flag = true;
                return instructions;
            }
        }
    }
}
