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
                /*
                if (typeof(UIStatisticsWindow).GetMethod("ComputeDisplayEntries") != null)
                {
                    harmony.PatchAll(typeof(Warper));
                    Plugin.Log.LogInfo($"{NAME} - OK");
                }
                */
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
        }
    }
}
