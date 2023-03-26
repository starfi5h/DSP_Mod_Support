using HarmonyLib;
using System;
using System.IO;
using System.Reflection.Emit;
using System.Collections.Generic;
using System.Reflection;

using NebulaAPI;
using NebulaWorld;
using NebulaModel.Packets.Factory.Splitter;
using NebulaModel.Packets.Trash;
using UnityEngine;
using System.Collections;

namespace NebulaCompatibilityAssist.Hotfix
{
    public static class NebulaHotfix
    {
        //private const string NAME = "NebulaMultiplayerMod";
        private const string GUID = "dsp.nebula-multiplayer";

        public static void Init(Harmony harmony)
        {
            if (!BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(GUID, out var pluginInfo))
                return;

            try
            {
                System.Version nebulaVersion = pluginInfo.Metadata.Version;
                if (nebulaVersion.Major == 0 && nebulaVersion.Minor == 8 && nebulaVersion.Build == 13)
                {
                    Patch0813(harmony);
                    Log.Info("Nebula hotfix 0.8.13 - OK");                    
                }
                ChatManager.Init(harmony);
                harmony.PatchAll(typeof(Analysis.StacktraceParser));
                Log.Info("Nebula extra features - OK");
            }
            catch (Exception e)
            {
                Log.Warn($"Nebula hotfix patch fail! Current version: " + pluginInfo.Metadata.Version);
                Log.Debug(e);
            }
        }

        private static void Patch0813(Harmony harmony)
        {
            Type classType;
            classType = AccessTools.TypeByName("NebulaWorld.Multiplayer");
            harmony.Patch(AccessTools.Method(classType, "HostGame"), new HarmonyMethod(typeof(NebulaNetworkPatch).GetMethod(nameof(NebulaNetworkPatch.BeforeMultiplayerGame))));
            harmony.Patch(AccessTools.Method(classType, "JoinGame"), new HarmonyMethod(typeof(NebulaNetworkPatch).GetMethod(nameof(NebulaNetworkPatch.BeforeMultiplayerGame))));
        }

        public static bool DisableFunction()
        {
            return false;
        }
    }
}
