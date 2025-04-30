using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Threading;

namespace NebulaCompatibilityAssist.Patches
{
    public static class GalacticScale
    {
        public const string NAME = "Galactic Scale 2 Plug-In";
        public const string GUID = "dsp.galactic-scale.2";
        public const string VERSION = "2.75.10";

        public static void Init(Harmony harmony)
        {
            if (!BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(GUID, out var _))
                return;
            if (AccessTools.Method(typeof(PlanetData), "GetUnloadedCopy") == null) return;

            try
            {               
                harmony.PatchAll(typeof(GalacticScale));

                Log.Info($"{NAME} - OK");
            }
            catch (Exception e)
            {
                Log.Warn($"{NAME} - Fail! Last target version: {VERSION}");
                NC_Patch.ErrorMessage += $"\n{NAME} (last target version: {VERSION})";
                Log.Debug(e);
            }
        }

        // The main issue is PlanetData.GetUnloadedCopy return a different planet
        // which make GS2PlanetAlgorithm.GenerateVeins unable to run veinAlgo because this.gsPlanet.planetData.data is null
        // so the patch revert the vein generation in PlanetFactory.Init() back to 0.10.32.25714 
        // by replacing GetUnloadedCopy and remove ReleaseCopy, so it is operate on the same copy
        // and adding a planetProcessingLock lock in prefix/postfix to avoid thread conflict

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.Init))]
        static void PlanetFactory_Init_Prefix()
        {
            Monitor.Enter(PlanetModelingManager.planetProcessingLock);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.Init))]
        static void PlanetFactory_Init_Postfix()
        {
            Monitor.Exit(PlanetModelingManager.planetProcessingLock);
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.Init))]
        static IEnumerable<CodeInstruction> PlanetFactory_Init_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            try
            {
                var matcher = new CodeMatcher(instructions)
                    .MatchForward(
                        true,
                        new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(PlanetData), nameof(PlanetData.GetUnloadedCopy)))
                    )
                    .SetOperandAndAdvance(AccessTools.Method(typeof(GalacticScale), nameof(GetUnloadedCopy)))
                    .MatchForward(
                        true,
                        new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(PlanetData), nameof(PlanetData.ReleaseCopy)))
                    )
                    .SetAndAdvance(OpCodes.Pop, null);

                return matcher.InstructionEnumeration();
            }
            catch (Exception ex)
            {
                Log.Warn("Transpiler error in PlanetFactory.Init");
                Log.Warn(ex);
            }
            return instructions;
        }

        static PlanetData GetUnloadedCopy(PlanetData p)
        {
            return p;
        }
    }
}
