using HarmonyLib;
using NebulaModel;
using NebulaModel.DataStructures.Chat;
using NebulaModel.Networking;
using NebulaModel.Packets.GameStates;
using NebulaModel.Packets.Logistics;
using NebulaModel.Utils;
using NebulaNetwork;
using NebulaWorld;
using NebulaWorld.Chat;
using NebulaWorld.Combat;
using NebulaWorld.GameStates;
using NebulaWorld.Logistics;
using NebulaWorld.MonoBehaviours.Local.Chat;
using NebulaWorld.Player;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using TMPro;
using UnityEngine;

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
                
                if (nebulaVersion.Major == 0 && nebulaVersion.Minor == 9 && nebulaVersion.Build == 8)
                {
                    harmony.PatchAll(typeof(Warper098));
                    Log.Info("Nebula hotfix 0.9.8 - OK");
                }

                ChatManager.Init(harmony);
                harmony.PatchAll(typeof(Analysis.StacktraceParser));
                harmony.PatchAll(typeof(SuppressErrors));
                Log.Info("Nebula extra features - OK");
            }
            catch (Exception e)
            {
                Log.Warn($"Nebula hotfix patch fail! Current version: " + pluginInfo.Metadata.Version);
                Log.Debug(e);
            }
        }

        /*
        private static void PatchPacketProcessor(Harmony harmony)
        {
            Type classType = AccessTools.TypeByName("NebulaWorld.Multiplayer");
            harmony.Patch(AccessTools.Method(classType, "HostGame"), new HarmonyMethod(typeof(NebulaNetworkPatch).GetMethod(nameof(NebulaNetworkPatch.BeforeMultiplayerGame))));
            harmony.Patch(AccessTools.Method(classType, "JoinGame"), new HarmonyMethod(typeof(NebulaNetworkPatch).GetMethod(nameof(NebulaNetworkPatch.BeforeMultiplayerGame))));
        }
        */
    }

    public static class SuppressErrors
    {
        static bool suppressed = false;

        [HarmonyPostfix, HarmonyPatch(typeof(GameMain), nameof(GameMain.Begin))]
        public static void OnGameBegin()
        {
            suppressed = false;
        }

        [HarmonyFinalizer]
        [HarmonyPatch(typeof(EnemyDFGroundSystem), nameof(EnemyDFGroundSystem.GameTickLogic))]
        [HarmonyPatch(typeof(EnemyDFGroundSystem), nameof(EnemyDFGroundSystem.KeyTickLogic))]
        [HarmonyPatch(typeof(EnemyDFHiveSystem), nameof(EnemyDFHiveSystem.GameTickLogic))]
        [HarmonyPatch(typeof(EnemyDFHiveSystem), nameof(EnemyDFHiveSystem.KeyTickLogic))]
        [HarmonyPatch(typeof(SkillSystem), nameof(SkillSystem.GameTick))]
        [HarmonyPatch(typeof(DefenseSystem), nameof(DefenseSystem.GameTick))]
        [HarmonyPatch(typeof(EnemyDFGroundSystem), nameof(EnemyDFGroundSystem.CalcFormsSupply))]
        public static Exception EnemyGameTick_Finalizer(Exception __exception)
        {
            if (__exception != null && !suppressed)
            {
                suppressed = true;
                var msg = "NebulaCompatibilityAssist suppressed the following exception: \n" + __exception.ToString();
                ChatManager.ShowWarningInChat(msg);
                Log.Error(msg);
            }
            return null;
        }
    }

    public static class Warper098
    {
        // IndexOutOfRangeException: Index was outside the bounds of the array.
        // at BuildTool.GetPrefabDesc (System.Int32 objId)[0x0000e] ; IL_000E
        // at BuildTool_Path.DeterminePreviews()[0x0008f] ;IL_008F
        // This means BuildTool_Path.startObjectId has a positive id that is exceed entity pool
        // May due to local buildTool affect by other player's build request
        [HarmonyFinalizer]
        [HarmonyPatch(typeof(BuildTool_Path), nameof(BuildTool_Path.DeterminePreviews))]
        public static Exception DeterminePreviews(Exception __exception, BuildTool_Path __instance)
        {            
            if (__exception != null)
            {
                // Reset state
                __instance.startObjectId = 0;
                __instance.startNearestAddonAreaIdx = 0;
                __instance.startTarget = Vector3.zero;
                __instance.pathPointCount = 0;
            }
            return null;
        }

        // IndexOutOfRangeException: Index was outside the bounds of the array.
        // at CargoTraffic.SetBeltState(System.Int32 beltId, System.Int32 state); (IL_002D)
        // at CargoTraffic.SetBeltSelected(System.Int32 beltId); (IL_0000)
        // at PlayerAction_Inspect.GameTick(System.Int64 timei); (IL_053E)
        // 
        // Worst outcome when suppressed: Belt highlight is incorrect
        [HarmonyFinalizer]
        [HarmonyPatch(typeof(CargoTraffic), nameof(CargoTraffic.SetBeltState))]
        public static Exception SetBeltState()
        {
            return null;
        }

        // NullReferenceException: Object reference not set to an instance of an object
        // at BGMController.UpdateLogic();(IL_03BC)
        // at BGMController.LateUpdate(); (IL_0000)
        //
        // This means if (DSPGame.Game.running) is null
        // Worst outcome when suppressed: BGM stops
        [HarmonyFinalizer]
        [HarmonyPatch(typeof(BGMController), nameof(BGMController.UpdateLogic))]
        public static Exception UpdateLogic()
        {
            return null;
        }
    }
}
