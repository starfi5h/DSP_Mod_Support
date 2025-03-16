using HarmonyLib;
using NebulaAPI;
using System;
using System.Reflection;

namespace NebulaCompatibilityAssist.Patches
{
    public static class Dustbin
    {
        public const string NAME = "Dustbin";
        public const string GUID = "org.soardev.dustbin";
        public const string VERSION = "1.3.3";

        private static Action<int> RemovePlanetSignalBelts;
        private static Action<int, int> SetSignalBelt;

        public static void Init(Harmony harmony)
        {
            if (!BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(GUID, out var pluginInfo))
                return;
            Assembly assembly = pluginInfo.Instance.GetType().Assembly;

            try
            {
                Type classType = assembly.GetType("Dustbin.BeltSignal");
                SetSignalBelt = AccessTools.MethodDelegate<Action<int, int>>(AccessTools.Method(classType, "SetSignalBelt"));
                RemovePlanetSignalBelts = AccessTools.MethodDelegate<Action<int>>(AccessTools.Method(classType, "RemovePlanetSignalBelts"));

                harmony.PatchAll(typeof(Dustbin));
                Log.Info($"{NAME} - OK");
            }
            catch (Exception e)
            {
                Log.Warn($"{NAME} - Fail! Last target version: {VERSION}");
                NC_Patch.ErrorMessage += $"\n{NAME} (last target version: {VERSION})";
                Log.Debug(e);
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.Free))]
        static void Free_Prefix(PlanetFactory __instance)
        {
            // When client unload the planet, remove all signal from _signalBelts hashset 
            RemovePlanetSignalBelts.Invoke(__instance.index);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.Import))]
        static void Import_Postfix(PlanetFactory __instance)
        {
            if (!NC_Patch.IsClient) return;

            // When client load the planet, add dustbin signal to _signalBelts hashset
            // https://github.com/soarqin/DSP_Mods/blob/master/Dustbin/BeltSignal.cs#L68C1-L81
            var factory = __instance;
            var entitySignPool = factory.entitySignPool;
            var cargoTraffic = factory.cargoTraffic;
            var beltPool = cargoTraffic.beltPool;
            for (var i = cargoTraffic.beltCursor - 1; i > 0; i--)
            {
                if (beltPool[i].id != i) continue;
                ref var signal = ref entitySignPool[beltPool[i].entityId];
                var signalId = signal.iconId0;
                if (signalId != 410U) continue;
                SetSignalBelt.Invoke(factory.index, i);
            }
        }
    }
}
