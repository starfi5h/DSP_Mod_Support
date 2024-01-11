using HarmonyLib;
using NebulaPatcher.Patches.Transpilers;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace ModFixerOne.Mods
{
    public static class Nebula_Patch
    {
        public const string NAME = "NebulaMultiplayerMod";
        public const string GUID = "dsp.nebula-multiplayer";
        public const string VERSION = "0.9.0";

        public static void OnAwake(Harmony harmony)
        {
            if (!Preloader.Guids.Contains(GUID))
                return;

            try
            {
                if (typeof(UIStatisticsWindow).GetMethod("ComputeDisplayEntries") != null)
                {
                    harmony.PatchAll(typeof(Warper));
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

        private static class Warper
        {
            [HarmonyPrefix]
            [HarmonyPatch(typeof(UIStatisticsWindow_Transpiler), nameof(UIStatisticsWindow_Transpiler.ComputeDisplayEntries_Transpiler))]
            public static bool Stop_Transpiler(IEnumerable<CodeInstruction> instructions, ref IEnumerable<CodeInstruction> __result)
            {
                __result = instructions;
                return false;
            }

            [HarmonyTranspiler]
            [HarmonyPatch(typeof(UIStatisticsWindow), "ComputeDisplayEntries")]
            private static IEnumerable<CodeInstruction> ComputeDisplayEntries_Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                try
                {
                    var codeInstructions = UIStatisticsWindow_Transpiler.ReplaceFactoryCount(instructions);
                    codeInstructions = new CodeMatcher(codeInstructions)
                        .MatchForward(false,
                            new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(PlanetData), nameof(PlanetData.factoryIndex)))
                        )
                        .Repeat(matcher => matcher
                            .SetAndAdvance(OpCodes.Call,
                                AccessTools.Method(typeof(UIStatisticsWindow_Transpiler), nameof(UIStatisticsWindow_Transpiler.GetFactoryIndex)))
                        )
                        .InstructionEnumeration();

                    return new CodeMatcher(codeInstructions)
                        .MatchForward(false,
                            new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(PlanetData), nameof(PlanetData.factory))),
                            new CodeMatch(OpCodes.Brfalse)
                        )
                        .SetAndAdvance(OpCodes.Call, AccessTools.Method(typeof(UIStatisticsWindow_Transpiler), nameof(UIStatisticsWindow_Transpiler.GetFactoryIndex)))
                        .InsertAndAdvance(new CodeInstruction(OpCodes.Ldc_I4_M1))
                        .SetOpcodeAndAdvance(OpCodes.Beq_S)
                        .InstructionEnumeration();
                }
                catch
                {
                    Plugin.Log.LogError("ComputeDisplayEntries_Transpiler failed. Mod version not compatible with game version.");
                    return instructions;
                }
            }
        }
    }
}
